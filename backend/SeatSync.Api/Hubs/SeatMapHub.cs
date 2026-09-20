using Microsoft.AspNetCore.SignalR;

namespace SeatSync.Api.Hubs;

public class SeatMapHub : Hub
{
    // Clients call this after connecting to join the group for the event
    // they're currently viewing. Group name convention: "event-{eventId}".
    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(eventId));
    }

    public static string GroupName(string eventId) => $"event-{eventId}";
}