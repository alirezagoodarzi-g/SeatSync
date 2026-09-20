# SeatSync — Architecture Notes

Running notes as the project is built. This becomes both README content later
and my own reference for explaining decisions in an interview.

## Why 3 projects, not 4+

- `SeatSync.Core` — entities, interfaces, business logic services. Zero
  dependencies on anything infrastructure-related (no EF Core, no Npgsql, no
  JWT library, no BCrypt). This is enforced by the compiler, not just a
  convention: Core's .csproj has no PackageReference or ProjectReference
  pointing at Infrastructure.
- `SeatSync.Infrastructure` — implements Core's interfaces using real
  technology (EF Core + Postgres, BCrypt, JWT). References Core.
- `SeatSync.Api` — controllers, Program.cs, DI wiring. References both. This
  is the ONLY project allowed to know about both Core interfaces and
  Infrastructure implementations at the same time — and even here, that
  crossover only happens in Program.cs. Controllers only ever import
  SeatSync.Core namespaces.

Why this matters in practice: AuthServiceTests (Phase 1) mock
IUserRepository, ITokenService, IPasswordHasher and test real business logic
in milliseconds, no Postgres/Docker needed. Same pattern will be used for the
Phase 3 concurrency test against ISeatLockService.

## Phase 1 — Auth

- JWT auth, HMAC-SHA256, claims: sub (user id), email, name, role.
- Role (Customer | Organizer) is stored as a string in Postgres
  (HasConversion<string>()) rather than an int, so the database is
  human-readable when inspected directly with psql.
- Passwords hashed with BCrypt, work factor 12. Never logged, never returned
  in any API response.
- Enum JSON serialization: added JsonStringEnumConverter globally in
  Program.cs so the API accepts/returns "Customer"/"Organizer" instead of
  0/1 — much more usable for Swagger, Postman, and later the Vue frontend.
- Unique index on Users.Email at the DB level, in addition to the
  EmailExistsAsync app-level check in AuthService — belt and suspenders
  against a race between two simultaneous registrations with the same email.

## Decisions / things worth remembering

- Postgres via Docker Compose (postgres:16), volume-mounted so data survives
  container restarts. WSL2 and Docker Desktop's disk relocated to a
  secondary drive (D:) due to limited space on C: — see docker-compose.yml
  comments if this ever needs to move again.
- Swagger (Swashbuckle.AspNetCore) instead of bare OpenAPI, purely for the
  interactive "Try it out" UI during manual testing — nice to have for a
  portfolio project, not load-bearing for the architecture itself.
- .NET 10 target framework throughout.

## Open questions / things to revisit later

- JWT tokens are not currently refreshable — a 60-minute expiry with no
  refresh token flow. Fine for Phase 1-2 development; worth a sentence in
  the README about it being a deliberate scope cut, not an oversight, if it
  never gets built out.
- No rate limiting on /api/auth/login yet (brute-force protection). Might
  add later using the same Redis instance that Phase 3 introduces for seat
  locking, since it'll already be in the stack.

## Phase 2 — Event/Seat/Booking domain

- Event -> Seats is a one-to-many, cascade delete (seats have no meaning
  without their event). Booking -> User/Event is restrict-delete instead —
  never want a delete to silently erase booking history.
- Booking.SeatIds stored as a native Postgres uuid[] column rather than a
  join table. Simpler for this project's scale; a booking's seats are
  always read/written as a whole set, never queried individually, so a
  join table would add complexity without benefit here.
- Unique index on Seat (EventId, Row, Number) — DB-level guarantee against
  duplicate seat definitions within one event.
- IBookingRepository is defined in Core but not implemented in
  Infrastructure yet — no write path exists for real bookings until
  Phase 4's RabbitMQ confirm flow. Deliberately avoiding building an
  implementation with no caller yet.
- EventsController: GET endpoints are [AllowAnonymous] (customers browse
  before logging in), POST is [Authorize(Roles = "Organizer")] — first real
  test that JWT role claims are enforced, not just carried around. Verified
  with curl: a Customer-role token gets 403 on POST /api/events.
- Swagger currently has no "Authorize" button (stripped during the
  Microsoft.OpenApi version conflict fix in Phase 1) — testing
  authenticated endpoints via curl with -H "Authorization: Bearer ..." and
  JSON bodies from file (-d "@payload.json") instead of inline strings,
  since PowerShell mangles escaped quotes in one-liners. Worth revisiting
  the Authorize button once the openapi/swashbuckle version mismatch is
  understood.

## Locale note (relevant later, Phase 6)

- Backend stays UTC / English field names throughout (DateTime, "Customer"/
  "Organizer" etc.) — this is intentional and doesn't change.
- Frontend will render dates in Jalali (Persian) calendar, Iran time, with
  Persian-language UI text — conversion happens only at the Vue layer, most
  likely via a Jalali date library. Nothing to build yet, just don't want to
  forget this when we reach Phase 6.
## Phase 3 — Redis seat-hold mechanism

- Core mechanism: SET seat:{eventId}:{seatId} {userId} NX EX 300 — one
  atomic Redis command does both the race-free acquire (NX) and the
  auto-expiry cleanup (EX), so no separate cleanup job is needed.
- Why NX matters: two separate calls (GET-then-SET) would leave a gap
  between the check and the write where two concurrent requests could both
  see "unheld" before either writes — that gap is the actual race. NX
  collapses check-and-set into one indivisible operation, closing the gap
  entirely.
- Redis holds are NOT written to Postgres. Postgres only gets a row once a
  hold is confirmed (Phase 4). Redis is the sole source of truth for "who's
  currently deciding on this seat right now" — deliberately transient,
  matches the spec ("Redis and RabbitMQ never store data permanently").
- ConnectionMultiplexer registered as a Singleton (not Scoped) — it's
  designed by StackExchange.Redis to be created once and shared for the
  app's lifetime; a new one per request would exhaust connections.
- Concurrency test (SeatLockConcurrencyTests): 50 concurrent users racing
  for the same seat, asserts exactly 1 succeeds. This is a real integration
  test against actual Redis (not mocked) — mocking ISeatLockService here
  would only prove the mock behaves correctly, not that Redis's NX
  semantics actually prevent the race. Requires the redis container running
  to pass. Ran several times consecutively with consistent results
  (concurrency bugs don't always surface on a single run).
- Second test confirms locking is scoped per-seat, not per-event — catches
  a class of bug where the wrong key granularity would make the whole seat
  map unusable.
## Phase 4 — RabbitMQ + booking confirmation

- Flow: hold (Redis) -> POST /api/bookings/confirm publishes a message and
  returns 202 Accepted immediately -> BackgroundService consumer picks it
  up -> writes Booking to Postgres -> flips Seat.Status to Booked ->
  releases the Redis hold -> acks the message.
- Idempotency: ConfirmBookingMessage carries a BookingRequestId (generated
  fresh per confirm request). Booking.BookingRequestId has a unique index.
  Before writing, the consumer checks ExistsByRequestIdAsync — if true,
  it's a RabbitMQ redelivery (e.g. consumer crashed after writing but
  before acking), so it acks and does nothing further rather than creating
  a duplicate booking. This is what makes an at-least-once delivery system
  behave like exactly-once from the caller's perspective.
- Manual ack/nack (autoAck: false) is what makes crash-safety possible at
  all — success acks, any exception in the handler nacks with requeue:
  true, so a transient failure (e.g. Postgres briefly down) retries rather
  than silently losing the confirmation.
- durable: true (queue) + Persistent = true (message) together survive a
  RabbitMQ restart — durable alone only protects the queue definition, not
  in-flight messages.
- BasicQos(prefetchCount: 1) — one message at a time per consumer instance,
  avoids needing to reason about concurrent DbContext access within the
  consumer.
- New DI scope created per message (IServiceScopeFactory) inside the
  singleton BackgroundService — required because SeatSyncDbContext and the
  repositories are Scoped, not Singleton; reusing one scope across messages
  would risk concurrent DbContext usage.
- Docker gotcha hit during this phase: RabbitMQ only applies
  RABBITMQ_DEFAULT_USER/PASS on first container initialization. Editing
  docker-compose.yml after the container already existed doesn't retroactively
  change it — had to `docker compose rm -f rabbitmq` and recreate. Separately,
  a config nesting mistake (RabbitMq section accidentally nested inside
  ConnectionStrings instead of being a sibling) caused RabbitMqOptions to
  bind to all-default values (including the default guest/guest credentials),
  which looked identical to a real auth failure — worth double-checking
  appsettings.json nesting whenever a new Options-bound section is added.
## Phase 5 — SignalR real-time layer

- One hub (SeatMapHub), clients join a group per event ("event-{id}") via
  an explicit JoinEventGroup call rather than joining automatically from a
  URL param — lets one connection switch between viewing different events
  without reconnecting.
- ISeatMapNotifier defined in Core (same pattern as IEventPublisher in
  Phase 4), but its implementation (SignalRSeatMapNotifier) lives in Api,
  not Infrastructure — a deliberate exception to the usual boundary, since
  IHubContext<SeatMapHub> depends on the Hub class itself, which is a web
  layer concern, not swappable backend infrastructure like Postgres/Redis/
  RabbitMQ are.
- Three broadcast triggers: HoldService (-> "Held"), BookingConsumerService
  (-> "Booked"), and a new SeatExpiryListenerService (-> "Available") for
  natural Redis TTL expiry.
- Expiry broadcasting requires Redis keyspace notifications
  (notify-keyspace-events Ex in docker-compose.yml), which is off by
  default. Subscribes to __keyevent@0__:expired and parses the key name
  (seat:{eventId}:{seatId}) via regex, since the expired key's value is
  already gone by the time the notification fires.
- Redis pub/sub is fire-and-forget, unlike RabbitMQ — no redelivery if no
  subscriber is connected at the moment of expiry. Acceptable here because
  it's a UX nicety only: the seat genuinely is available again regardless
  (Redis's TTL already guarantees that); a missed broadcast just means a
  stale-looking seat until another action refreshes it.
- Tested manually with a standalone HTML page (signalr-test.html, outside
  the solution) using the SignalR JS client from a CDN, since the Vue
  frontend doesn't exist yet. Confirmed all three transitions (Held,
  Booked, Available-via-expiry) end to end, including a temporary
  HoldDuration shortened to 15s for faster expiry testing, then reverted.
- CORS: started wide-open (SetIsOriginAllowed(_ => true)) for the test
  page, narrowed to WithOrigins("http://localhost:5173") once the real
  frontend's dev server origin was known.
## Phase 6 — Vue 3 + TypeScript frontend

- Vite + vue-router + Pinia + axios + @microsoft/signalr, Tailwind v4
  (via @tailwindcss/vite, token-based theme in style.css rather than a
  separate config file).
- RTL/Persian throughout: <html lang="fa" dir="rtl">, Vazirmatn for UI
  text, IBM Plex Mono for anything numeric/ticket-like (seat codes, prices,
  dates) — a deliberate "ticket stub" visual language tied to the subject,
  not just a generic data-label convention.
- Dates: backend stays UTC/Gregorian throughout (DateTime, ISO strings).
  Display conversion to Jalali + Iran time uses native Intl.DateTimeFormat
  with calendar: 'persian', timeZone: 'Asia/Tehran' — no extra display
  library needed. Organizer's event-creation form accepts Jalali input
  (jalaali-js for the Jalali->Gregorian conversion, since Intl only
  formats, it doesn't parse) with an explicit Iran-offset conversion
  (iranLocalToUtcIso) before sending to the API — never trusts the
  browser's own timezone.
- Auth: JWT stored in localStorage, decoded client-side via jwt-decode to
  rehydrate user state on refresh (hydrateFromToken). Claim keys use the
  full ASP.NET claim-type URIs (ClaimTypes.Name/Role), matching what the
  backend actually puts in the token — same gotcha as
  ClaimsPrincipalExtensions.GetUserId() on the backend.
- Seat map flow (reworked after initial build): two-phase selection —
  "picking" (local only, no API calls) then a single "ادامه به پرداخت"
  action that holds all picked seats at once and navigates to a dedicated
  Checkout page. This replaced an earlier per-seat-hold-on-click design
  with a per-seat countdown, which was confusing UX with multiple seats
  selected — a single global countdown on checkout reads much more
  clearly.
- Known gap: GET /api/events/{id} only reflects Postgres state
  (Available/Booked), never Redis's in-progress Held state, so a seat
  someone else is actively holding can briefly appear Available on page
  load until SignalR pushes an update or the user attempts to hold it
  (gets a 409). Acceptable scope cut, not a bug — same limitation noted
  when SeatMap.vue was first built in Phase 6.
- PDF receipts: initially attempted client-side (jsPDF + html2canvas
  rendering styled HTML), abandoned due to unreliable Persian/RTL text
  shaping in canvas rasterization. Rebuilt as backend generation
  (QuestPDF, Core/Infrastructure/Api layers, GET /api/bookings/{id}/receipt)
  — more reliable RTL text via HarfBuzz shaping, and keeps the PDF and
  the DB's booking record from ever disagreeing. Confirm is still async
  (RabbitMQ), so the frontend polls GET /api/bookings/by-request/{id}
  briefly until the consumer creates the row, then downloads the receipt
  as a blob.
- Debugging note: IDM (Internet Download Manager)'s browser extension
  intercepts PDF-like responses before page JS can read them, surfacing
  as a confusing false CORS error in the console. Not an actual CORS bug
  — disable IDM's browser integration when developing/testing downloads
  locally.
- CORS narrowed to WithOrigins("http://localhost:5173") (Vite's default
  dev