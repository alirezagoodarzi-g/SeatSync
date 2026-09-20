namespace SeatSync.Core.Interfaces;

public interface ISeatMapNotifier
{
    Task NotifySeatStatusChangedAsync(Guid eventId, Guid seatId, string status);
}