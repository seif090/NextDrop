using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Orders.Application.Commands;
using NextDrop.Modules.Orders.Application.DTOs;
using NextDrop.Modules.Orders.Application.Queries;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/carts")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IIdempotencyService _idempotencyService;

    public CartsController(ISender sender, IIdempotencyService idempotencyService)
    {
        _sender = sender;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request)
    {
        var command = new CreateCartCommand(GetUserId(), request.RestaurantId, request.RestaurantBranchId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });
            if (result.Error.Code.Contains("Unavailable"))
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetCartById), new { cartId = result.Value.Id }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var query = new GetCartQuery(GetUserId());
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("{cartId:guid}")]
    public async Task<IActionResult> GetCartById(Guid cartId)
    {
        var query = new GetCartQuery(GetUserId());
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        if (result.Value.Id != cartId)
            return NotFound(new { error = "Cart not found." });

        return Ok(result.Value);
    }

    [HttpPost("{cartId:guid}/items")]
    public async Task<IActionResult> AddCartItem(Guid cartId, [FromBody] AddCartItemRequest request)
    {
        var command = new AddCartItemCommand(GetUserId(), cartId, request.MenuItemId, request.VariantId, request.Quantity, request.Notes);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });
            if (result.Error.Code.Contains("Unavailable"))
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost("{cartId:guid}/checkout")]
    public async Task<IActionResult> CheckoutCart(
        Guid cartId,
        [FromBody] CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "Idempotency-Key header is required for checkout." });

        var requestHash = $"{cartId}:{request.DeliveryAddressId}";

        // Idempotency replay check
        var isProcessed = await _idempotencyService.IsKeyProcessedAsync(idempotencyKey, requestHash);
        if (isProcessed)
        {
            var cached = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
            if (cached != null)
            {
                return Content(cached.Body, cached.ContentType);
            }
        }
        else
        {
            var existingCached = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
            if (existingCached != null)
            {
                return Conflict(new { error = "Idempotency key replayed with a different payload." });
            }
        }

        var command = new CheckoutCartCommand(GetUserId(), cartId, request.DeliveryAddressId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });
            if (result.Error.Code.Contains("Conflict") || result.Error.Code.Contains("BelowMinimumAmount") || result.Error.Code.Contains("Unavailable") || result.Error.Code.Contains("Closed"))
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        var responseJson = JsonSerializer.Serialize(result.Value);
        await _idempotencyService.CacheResponseAsync(idempotencyKey, requestHash, 200, "application/json", responseJson);

        return Ok(result.Value);
    }
}

public record CreateCartRequest(Guid RestaurantId, Guid RestaurantBranchId);
public record AddCartItemRequest(Guid MenuItemId, Guid? VariantId, int Quantity, string? Notes);
public record CheckoutRequest(Guid DeliveryAddressId);
