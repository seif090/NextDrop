using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Orders.Application.Commands;
using NextDrop.Modules.Orders.Application.DTOs;
using NextDrop.Modules.Orders.Application.Queries;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetCustomerOrdersQuery(GetUserId(), page, pageSize);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var query = new GetOrderByIdQuery(GetUserId(), orderId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("{orderId:guid}/status")]
    public async Task<IActionResult> GetOrderStatus(Guid orderId)
    {
        var query = new GetOrderByIdQuery(GetUserId(), orderId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();

            return NotFound(new { error = result.Error.Description });
        }

        var dto = new OrderStatusDto(result.Value.Id, result.Value.OrderNumber, result.Value.Status, result.Value.ConfirmedAtUtc);
        return Ok(dto);
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest request)
    {
        var command = new CancelOrderCommand(GetUserId(), orderId, request.Reason);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });
            if (result.Error.Code.Contains("TerminalState") || result.Error.Code.Contains("CannotCancelInTransit") || result.Error.Code.Contains("Conflict"))
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }
}

public record CancelOrderRequest(string Reason);
