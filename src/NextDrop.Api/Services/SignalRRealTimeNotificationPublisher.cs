using Microsoft.AspNetCore.SignalR;
using NextDrop.Api.Hubs;
using NextDrop.Modules.Notifications.Application.Abstractions;

namespace NextDrop.Api.Services;

public class SignalRRealTimeNotificationPublisher : IRealTimeNotificationPublisher
{
    private readonly IHubContext<OrderTrackingHub> _hubContext;

    public SignalRRealTimeNotificationPublisher(IHubContext<OrderTrackingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishOrderNotificationAsync(Guid userId, Guid orderId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user:{userId}").SendAsync(eventName, payload, cancellationToken);
        if (orderId != Guid.Empty)
        {
            await _hubContext.Clients.Group($"order:{orderId}").SendAsync(eventName, payload, cancellationToken);
        }
    }

    public async Task PublishRiderLocationAsync(Guid orderId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (orderId != Guid.Empty)
        {
            await _hubContext.Clients.Group($"order:{orderId}").SendAsync("RiderLocationUpdated", new { OrderId = orderId, Latitude = latitude, Longitude = longitude }, cancellationToken);
        }
    }
}
