using SeatSync.Core.DTOs;
using SeatSync.Core.Interfaces;

namespace SeatSync.Core.Services;

public class HoldService : IHoldService
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    private readonly ISeatLockService _seatLockService;
    private readonly ISeatMapNotifier _seatMapNotifier;

    public HoldService(ISeatLockService seatLockService, ISeatMapNotifier seatMapNotifier)
    {
        _seatLockService = seatLockService;
        _seatMapNotifier = seatMapNotifier;
    }

    public async Task<HoldSeatResponse?> HoldSeatAsync(Guid eventId, Guid seatId, Guid userId)
    {
        var acquired = await _seatLockService.TryAcquireHoldAsync(eventId, seatId, userId, HoldDuration);
        if (!acquired) return null;

        await _seatMapNotifier.NotifySeatStatusChangedAsync(eventId, seatId, "Held");

        return new HoldSeatResponse(seatId, DateTime.UtcNow.Add(HoldDuration));
    }
    public async Task ReleaseSeatAsync(Guid eventId, Guid seatId, Guid userId)
    {
        var currentOwner = await _seatLockService.GetHoldOwnerAsync(eventId, seatId);
        if (currentOwner != userId) return; // not yours (or already released/expired) — no-op

        await _seatLockService.ReleaseHoldAsync(eventId, seatId);
        await _seatMapNotifier.NotifySeatStatusChangedAsync(eventId, seatId, "Available");
    }
}