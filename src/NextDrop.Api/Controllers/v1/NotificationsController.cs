using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Notifications.Application.Commands;
using NextDrop.Modules.Notifications.Application.Queries;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
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

        if (error.Code.Contains("Conflict") || error.Code.Contains("Disabled"))
            return Conflict(new { error = error.Description });

        return BadRequest(new { error = error.Description });
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var query = new GetNotificationsQuery(userId, page, pageSize);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetUserId();
        var query = new GetUnreadNotificationsQuery(userId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        var command = new MarkNotificationAsReadCommand(userId, id);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        var command = new MarkAllNotificationsAsReadCommand(userId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = GetUserId();
        var command = new DeleteNotificationCommand(userId, id);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var query = new GetNotificationPreferencesQuery(userId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    public record UpdatePreferencesRequest(
        bool AllowOrderNotifications,
        bool AllowMarketingNotifications,
        bool EmailEnabled,
        bool InAppEnabled);

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetUserId();
        var command = new UpdateNotificationPreferencesCommand(
            userId,
            request.AllowOrderNotifications,
            request.AllowMarketingNotifications,
            request.EmailEnabled,
            request.InAppEnabled);

        var result = await _sender.Send(command);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }
}
