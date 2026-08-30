using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Payments.Application.Commands;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IIdempotencyService _idempotencyService;

    public CheckoutController(ISender sender, IIdempotencyService idempotencyService)
    {
        _sender = sender;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private IActionResult HandleError(Error error)
    {
        if (error.Code.Contains("NotFound"))
            return NotFound(new { error = error.Description });

        if (error.Code.Contains("Forbidden") || error.Code.Contains("Unauthorized"))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = error.Description });

        if (error.Code.Contains("Conflict") || error.Code.Contains("BelowMinimumAmount") || error.Code.Contains("Empty") || error.Code.Contains("Unavailable") || error.Code.Contains("Closed"))
            return Conflict(new { error = error.Description });

        return BadRequest(new { error = error.Description });
    }

    public record TransactionalCheckoutRequest(Guid CartId, Guid DeliveryAddressId);

    [HttpPost]
    public async Task<IActionResult> Checkout(
        [FromBody] TransactionalCheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"{request.CartId}:{request.DeliveryAddressId}:{userId}";

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var isProcessed = await _idempotencyService.IsKeyProcessedAsync(idempotencyKey, requestHash);
            if (isProcessed)
            {
                var cached = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
                if (cached != null)
                    return Content(cached.Body, cached.ContentType);
            }
            else
            {
                var existing = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
                if (existing != null)
                    return Conflict(new { error = "Idempotency key replayed with a different payload." });
            }
        }

        var command = new CheckoutCommand(userId, request.CartId, request.DeliveryAddressId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var responseJson = JsonSerializer.Serialize(result.Value);
            await _idempotencyService.CacheResponseAsync(idempotencyKey, requestHash, 200, "application/json", responseJson);
        }

        return Ok(result.Value);
    }
}
