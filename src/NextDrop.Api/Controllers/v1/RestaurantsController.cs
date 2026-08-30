using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Restaurants.Application.Commands;
using NextDrop.Modules.Restaurants.Application.DTOs;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly ISender _sender;

    public RestaurantsController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(val, out var guid) ? guid : Guid.Empty;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicRestaurants([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? city = null, CancellationToken cancellationToken = default)
    {
        var query = new GetPublicRestaurantsQuery(page, pageSize, city);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetRestaurantByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRestaurantCommand(GetUserId(), request.Name, request.Description, request.PhoneNumber, request.Email);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { detail = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRestaurantRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateRestaurantCommand(id, GetUserId(), request.Name, request.Description, request.PhoneNumber, request.Email);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateRestaurantStatusCommand(id, GetUserId(), request.Status);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/branches")]
    [Authorize]
    public async Task<IActionResult> CreateBranch(Guid id, [FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBranchCommand(
            id, GetUserId(), request.Name, request.PhoneNumber, request.AddressLine1,
            request.AddressLine2, request.City, request.District, request.Latitude, request.Longitude, request.Timezone);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return CreatedAtAction(nameof(GetById), new { id }, result.Value);
    }

    [HttpPut("{id:guid}/branches/{branchId:guid}/operating-hours")]
    [Authorize]
    public async Task<IActionResult> SetOperatingHours(Guid id, Guid branchId, [FromBody] List<RestaurantOperatingHoursDto> hours, CancellationToken cancellationToken)
    {
        var command = new SetBranchOperatingHoursCommand(id, branchId, GetUserId(), hours);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/branches/{branchId:guid}/delivery-zones")]
    [Authorize]
    public async Task<IActionResult> CreateDeliveryZone(Guid id, Guid branchId, [FromBody] CreateDeliveryZoneRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDeliveryZoneCommand(
            id, branchId, GetUserId(), request.Name, request.DeliveryFee, request.MinimumOrderAmount, request.EstimatedDeliveryMinutes);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return CreatedAtAction(nameof(GetById), new { id }, result.Value);
    }

    [HttpPost("{id:guid}/staff")]
    [Authorize]
    public async Task<IActionResult> AddStaff(Guid id, [FromBody] AddStaffRequest request, CancellationToken cancellationToken)
    {
        var command = new AddStaffMemberCommand(id, GetUserId(), request.TargetUserId, request.Role);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }
}

public record CreateRestaurantRequest(string Name, string Description, string PhoneNumber, string Email);
public record UpdateStatusRequest(RestaurantStatus Status);
public record CreateBranchRequest(string Name, string PhoneNumber, string AddressLine1, string? AddressLine2, string City, string District, decimal Latitude, decimal Longitude, string Timezone);
public record CreateDeliveryZoneRequest(string Name, decimal DeliveryFee, decimal MinimumOrderAmount, int EstimatedDeliveryMinutes);
public record AddStaffRequest(Guid TargetUserId, RestaurantStaffRole Role);
