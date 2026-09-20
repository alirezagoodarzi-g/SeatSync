using Microsoft.AspNetCore.SignalR;
using SeatSync.Core.Interfaces;

namespace SeatSync.Api.Hubs;

public class SignalRSeatMapNotifier : ISeatMapNotifier
{
    private readonly IHubContext<SeatMapHub> _hubContext;

    public SignalRSeatMapNotifier(IHubContext<SeatMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySeatStatusChangedAsync(Guid eventId, Guid seatId, string status)
    {
        await _hubContext.Clients
            .Group(SeatMapHub.GroupName(eventId.ToString()))
            .SendAsync("SeatStatusChanged", seatId, status);
    }
}