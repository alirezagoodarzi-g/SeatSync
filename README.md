# SeatSync — Real-Time Event Booking Platform


A seat-booking system built to explore one real distributed-systems problem
properly: **safely handling concurrent access to a limited resource in real
time**. Two users click the same seat within milliseconds — only one should
get it, cleanly, with no double-booking and no manual cleanup.

the Redis locking strategy, the RabbitMQ ack/nack semantics, the Core/
Infrastructure boundary — was made and understood deliberately, not accepted
blindly. See `docs/architecture.md` for the running decision log kept
throughout the build.

## The core mechanism
User clicks a seat
API asks Redis: SET seat:{eventId}:{seatId} {userId} NX EX 300
→ atomic acquire + 5-minute auto-expiry, one command, no race window
Win → seat held for 5 min, broadcast "Held" via SignalR
Lose → 409 Conflict
User confirms → API publishes to RabbitMQ, returns immediately
Background consumer persists the booking to Postgres,
releases the Redis lock, broadcasts "Booked"
Never confirmed → Redis TTL expires → seat is available again,
no cleanup job needed, broadcast "Available"

Proven under real concurrent load: `SeatLockConcurrencyTests` fires 50
simultaneous hold requests at the same seat against real Redis (not mocked)
and asserts exactly one wins — this is the single test that substantiates
the project's core claim.

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core (.NET 10), JWT auth, Swagger |
| Domain logic | Clean 3-project split: Core (zero infra deps) → Infrastructure → Api |
| Database | PostgreSQL + EF Core |
| Concurrency lock | Redis (`SET NX EX`) + keyspace notifications for expiry |
| Async messaging | RabbitMQ, hosted `BackgroundService` consumer, manual ack/nack |
| Real-time | SignalR, one hub, group-per-event |
| PDF receipts | QuestPDF (backend-generated, Persian/RTL font embedded) |
| Frontend | Vue 3 + TypeScript, Vite, Pinia, Tailwind v4 |
| Locale | Persian UI (RTL), Jalali calendar + Iran time for display; backend stays UTC/Gregorian throughout |
| Tests | xUnit — service logic (mocked deps) + one real integration test against Redis |
| Containerization | Docker Compose — `docker compose up` runs the full stack |

## Architecture

backend/
├── SeatSync.Api/ # Controllers, SignalR hub, Program.cs (DI wiring)
├── SeatSync.Core/ # Entities, interfaces, business logic — zero infra dependencies
├── SeatSync.Infrastructure/ # EF Core, Redis, RabbitMQ, QuestPDF — implements Core's interfaces
└── SeatSync.Tests/ # xUnit
frontend/
└── seatsync-web/ # Vue 3 + TS, Persian/RTL, role-guarded routing
docker-compose.yml # postgres, redis, rabbitmq, api, frontend
.env.example # template for Docker Compose secrets — copy to .env
docs/architecture.md # decision log kept throughout the build


The one rule that matters: **`SeatSync.Core` has zero dependencies on
Infrastructure.** Core defines interfaces (`ISeatLockService`,
`IBookingRepository`, `IEventPublisher`); Infrastructure implements them.
This is enforced by the compiler (no PackageReference/ProjectReference from
Core outward), not just convention — and it's what let every service
(`AuthService`, `EventService`, `HoldService`, `BookingService`) be unit
tested against mocked interfaces, with zero Postgres/Redis/RabbitMQ needed
for the majority of the test suite.

## Setup

Two separate configuration mechanisms exist, depending on how you run the
project — they don't affect each other:

- **`.env`** (root folder) — read only by Docker Compose, for the
  containerized `api`/`frontend`/infra services.
- **`backend/SeatSync.Api/appsettings.json`** — read only by a native
  `dotnet run`, outside of Docker.

**Before running either way**, copy the env template and fill in real values:
```bash
cp .env.example .env
```
Edit `.env` and replace the placeholder `JWT_SECRET_KEY` with your own
random 32+ character string (letters, numbers, symbols).

**If you'll also run the backend natively** (not via Docker — see Option B
below), separately edit `backend/SeatSync.Api/appsettings.json` and replace
its `CHANGE_ME`/placeholder values the same way. This file is never read by
Docker Compose, so it needs its own values filled in independently.

## Running it

**Option A — everything in Docker (simplest):**
```bash
docker compose up
```
Then open `http://localhost:5173`.

**Option B — infra in Docker, api/frontend natively (for active development):**
```bash
docker compose up -d postgres redis rabbitmq
dotnet run --project backend/SeatSync.Api
cd frontend/seatsync-web && npm run dev
```

First run needs a database migration:
```bash
dotnet ef database update --project backend/SeatSync.Infrastructure --startup-project backend/SeatSync.Api
```

RabbitMQ's management UI (useful for watching messages move through the
confirm queue live): `http://localhost:15672` (username from `.env`,
default `seatsync` / password from `.env`)

## Known scope cuts (deliberate, not oversights)

- `GET /api/events/{id}` reflects Postgres state only (`Available`/`Booked`);
  an in-progress Redis hold from another user can briefly show as
  `Available` until SignalR pushes an update or a hold attempt returns 409.
- No refresh-token flow — JWTs expire after 60 minutes with no silent renewal.
- No rate limiting on `/api/auth/login` yet.
- Redis pub/sub (used for expiry broadcasts) is fire-and-forget — a missed
  notification just means a seat looks stale until the next state-changing
  action touches it; the underlying lock state in Redis is unaffected.


## Tests

```bash
dotnet test
```
14 tests: service-level logic against mocked dependencies (auth, events,
holds, bookings), plus one real integration test firing 50 concurrent
requests at Redis to prove the locking guarantee.
