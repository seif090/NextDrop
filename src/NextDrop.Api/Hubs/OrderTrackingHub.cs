using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Orders.Domain.ValueObjects;

namespace NextDrop.Api.Hubs;

[Authorize]
public class OrderTrackingHub : Hub
{
    private readonly NextDropDbContext _dbContext;

    public OrderTrackingHub(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task SubscribeToOrder(Guid orderId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            throw new HubException("Unauthorized.");

        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == new OrderId(orderId));
        if (order == null)
            throw new HubException("Order not found.");

        // BOLA Authorization Check: Order Customer OR Restaurant Staff OR Rider assigned
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId);
        var isCustomerOwner = customer != null && customer.UserId == userId;

        if (!isCustomerOwner)
        {
            // Check if rider
            var rider = await _dbContext.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
            var isAssignedRider = false;
            if (rider != null)
            {
                var delivery = await _dbContext.Deliveries.FirstOrDefaultAsync(d => d.OrderId == order.Id);
                isAssignedRider = delivery != null && delivery.RiderId == rider.Id;
            }

            if (!isAssignedRider)
            {
                throw new HubException("Not authorized to subscribe to this order stream.");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }

    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }
}
