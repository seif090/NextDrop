using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Payments.Application.Commands;
using NextDrop.Modules.Payments.Application.Queries;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IIdempotencyService _idempotencyService;

    public PaymentsController(ISender sender, IIdempotencyService idempotencyService)
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

        if (error.Code.Contains("Conflict") || error.Code.Contains("Invalid") || error.Code.Contains("Already") || error.Code.Contains("Exceeds") || error.Code.Contains("Cannot") || error.Code.Contains("Terminal"))
            return Conflict(new { error = error.Description });

        return BadRequest(new { error = error.Description });
    }

    [HttpGet("{paymentId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var userId = GetUserId();
        var query = new GetPaymentByIdQuery(userId, paymentId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{paymentId:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmPayment(
        Guid paymentId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"confirm_pay:{paymentId}:{userId}";

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

        var command = new ConfirmPaymentCommand(userId, paymentId);
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

    [HttpPost("{paymentId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelPayment(Guid paymentId)
    {
        var userId = GetUserId();
        var command = new CancelPaymentCommand(userId, paymentId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    public record CreateRefundRequest(decimal Amount, string Reason);

    [HttpPost("{paymentId:guid}/refund")]
    [Authorize]
    public async Task<IActionResult> CreateRefund(
        Guid paymentId,
        [FromBody] CreateRefundRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = GetUserId();
        var requestHash = $"refund:{paymentId}:{request.Amount}:{userId}";

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

        var command = new CreateRefundCommand(userId, paymentId, request.Amount, request.Reason);
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

    [HttpPost("webhooks/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        string provider,
        [FromHeader(Name = "X-Webhook-Signature")] string? signature,
        [FromHeader(Name = "X-Webhook-Event-Id")] string? providerEventId)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(signature))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Webhook signature is missing." });

        var evtId = string.IsNullOrWhiteSpace(providerEventId) ? Guid.NewGuid().ToString("N") : providerEventId;
        var command = new ProcessPaymentWebhookCommand(provider, evtId, "payment.updated", signature, payload);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(new { status = "processed" });
    }
}
