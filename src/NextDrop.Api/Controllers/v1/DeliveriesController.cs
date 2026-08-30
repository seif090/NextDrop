using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Delivery.Application.Commands;
using NextDrop.Modules.Delivery.Application.Queries;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IIdempotencyService _idempotencyService;

    public DeliveriesController(ISender sender, IIdempotencyService idempotencyService)
    {
        _sender = sender;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string? GetIdempotencyKey() => Request.Headers["Idempotency-Key"].FirstOrDefault();

    private IActionResult HandleError(Error error)
    {
        if (error.Code.Contains("NotFound"))
            return NotFound(new { error = error.Description });

        if (error.Code.Contains("Forbidden") || error.Code.Contains("Unauthorized"))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = error.Description });

        if (error.Code.Contains("Conflict") || error.Code.Contains("Invalid") || error.Code.Contains("Already") || error.Code.Contains("NotEligible") || error.Code.Contains("Terminal") || error.Code.Contains("Cannot") || error.Code.Contains("Inactive"))
            return Conflict(new { error = error.Description });

        return BadRequest(new { error = error.Description });
    }

    [HttpGet("{deliveryId:guid}")]
    public async Task<IActionResult> GetDeliveryById(Guid deliveryId)
    {
        var userId = GetUserId();
        var query = new GetDeliveryByIdQuery(userId, deliveryId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{deliveryId:guid}/accept")]
    public async Task<IActionResult> AcceptDelivery(
        Guid deliveryId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"accept:{deliveryId}:{userId}";

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

        var command = new AcceptDeliveryCommand(userId, deliveryId);
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

    public record RejectDeliveryRequest(string Reason);

    [HttpPost("{deliveryId:guid}/reject")]
    public async Task<IActionResult> RejectDelivery(Guid deliveryId, [FromBody] RejectDeliveryRequest request)
    {
        var userId = GetUserId();
        var command = new RejectDeliveryCommand(userId, deliveryId, request.Reason);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpPost("{deliveryId:guid}/arrive")]
    public async Task<IActionResult> ArriveAtRestaurant(Guid deliveryId)
    {
        var userId = GetUserId();
        var command = new ArriveAtRestaurantCommand(userId, deliveryId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpPost("{deliveryId:guid}/pickup")]
    public async Task<IActionResult> ConfirmPickup(
        Guid deliveryId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"pickup:{deliveryId}:{userId}";

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var isProcessed = await _idempotencyService.IsKeyProcessedAsync(idempotencyKey, requestHash);
            if (isProcessed)
            {
                var cached = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
                if (cached != null)
                    return Content(cached.Body, cached.ContentType);
            }
        }

        var command = new ConfirmPickupCommand(userId, deliveryId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _idempotencyService.CacheResponseAsync(idempotencyKey, requestHash, 204, "application/json", "");
        }

        return NoContent();
    }

    [HttpPost("{deliveryId:guid}/start")]
    public async Task<IActionResult> StartDelivery(Guid deliveryId)
    {
        var userId = GetUserId();
        var command = new StartDeliveryCommand(userId, deliveryId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpPost("{deliveryId:guid}/complete")]
    public async Task<IActionResult> CompleteDelivery(
        Guid deliveryId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"complete:{deliveryId}:{userId}";

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var isProcessed = await _idempotencyService.IsKeyProcessedAsync(idempotencyKey, requestHash);
            if (isProcessed)
            {
                var cached = await _idempotencyService.GetCachedResponseAsync(idempotencyKey);
                if (cached != null)
                    return Content(cached.Body, cached.ContentType);
            }
        }

        var command = new CompleteDeliveryCommand(userId, deliveryId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _idempotencyService.CacheResponseAsync(idempotencyKey, requestHash, 204, "application/json", "");
        }

        return NoContent();
    }

    public record FailDeliveryRequest(string Reason);

    [HttpPost("{deliveryId:guid}/fail")]
    public async Task<IActionResult> FailDelivery(Guid deliveryId, [FromBody] FailDeliveryRequest request)
    {
        var userId = GetUserId();
        var command = new FailDeliveryCommand(userId, deliveryId, request.Reason);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }
}
