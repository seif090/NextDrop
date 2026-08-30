using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Api.Middleware;
using NextDrop.Modules.Identity.Application.DTOs;
using NextDrop.Modules.Identity.Application.Queries.GetCurrentUser;
using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetCurrentUserQuery(new UserId(userIdGuid)), cancellationToken);
        if (result.IsFailure)
        {
            var correlationId = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdHeaderName]?.ToString();
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = result.Error.Code,
                Detail = result.Error.Description,
                Instance = HttpContext.Request.Path
            };
            if (correlationId != null) problemDetails.Extensions["correlationId"] = correlationId;
            return NotFound(problemDetails);
        }

        return Ok(result.Value);
    }
}
