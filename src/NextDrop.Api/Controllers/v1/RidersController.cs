using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Delivery.Application.Commands;
using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Application.Queries;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/riders")]
[Authorize]
public class RidersController : ControllerBase
{
    private readonly ISender _sender;

    public RidersController(ISender sender)
    {
        _sender = sender;
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

        if (error.Code.Contains("Conflict") || error.Code.Contains("Invalid") || error.Code.Contains("Already") || error.Code.Contains("NotEligible") || error.Code.Contains("Terminal") || error.Code.Contains("Inactive"))
            return Conflict(new { error = error.Description });

        return BadRequest(new { error = error.Description });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        var query = new GetRiderProfileQuery(userId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    public record SetAvailabilityRequest(string AvailabilityStatus);

    [HttpPost("me/availability")]
    public async Task<IActionResult> SetAvailability([FromBody] SetAvailabilityRequest request)
    {
        var userId = GetUserId();
        if (!Enum.TryParse<RiderAvailabilityStatus>(request.AvailabilityStatus, true, out var status))
            return BadRequest(new { error = "Invalid availability status." });

        var command = new SetRiderAvailabilityCommand(userId, status);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    public record UpdateLocationRequest(
        decimal Latitude,
        decimal Longitude,
        double? Accuracy,
        double? Heading,
        double? Speed);

    [HttpPost("me/location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateRiderLocationCommand(
            userId,
            request.Latitude,
            request.Longitude,
            request.Accuracy,
            request.Heading,
            request.Speed);

        var result = await _sender.Send(command);
        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRiderProfile([FromBody] CreateRiderCommand request)
    {
        var userId = GetUserId();
        var cmd = request with { UserId = userId };
        var result = await _sender.Send(cmd);

        if (result.IsFailure)
            return HandleError(result.Error);

        return CreatedAtAction(nameof(GetMyProfile), result.Value);
    }

    [HttpPost("{riderId:guid}/activate")]
    public async Task<IActionResult> ActivateRider(Guid riderId)
    {
        var command = new ActivateRiderCommand(riderId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }
}
