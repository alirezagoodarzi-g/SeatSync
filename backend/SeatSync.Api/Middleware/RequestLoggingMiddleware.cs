using System.Diagnostics;
using SeatSync.Api.Extensions;
using Serilog.Context;

namespace SeatSync.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Only the booking-flow endpoints get this detailed structured logging —
    // matches the spec's "every booking-flow request logged with..." scope,
    // rather than instrumenting every endpoint in the app equally.
    private static readonly string[] TrackedPathPrefixes = { "/api/events", "/api/bookings" };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!TrackedPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var requestId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("Action", $"{context.Request.Method} {path}"))
        {
            await _next(context);
            stopwatch.Stop();

            var userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.TryGetUserId()
                : (Guid?)null;

            var eventId = context.Request.RouteValues.TryGetValue("eventId", out var e) ? e : null;
            var seatId = context.Request.RouteValues.TryGetValue("seatId", out var s) ? s : null;

            _logger.LogInformation(
                "Booking-flow request completed. UserId={UserId} EventId={EventId} SeatId={SeatId} StatusCode={StatusCode} DurationMs={DurationMs}",
                userId, eventId, seatId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}