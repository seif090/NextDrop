using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Customers.Application.Commands;
using NextDrop.Modules.Customers.Application.DTOs;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(val, out var guid) ? guid : Guid.Empty;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var query = new GetCustomerProfileQuery(GetUserId());
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] CreateOrUpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrUpdateCustomerProfileCommand(GetUserId(), request.FirstName, request.LastName, request.PhoneNumber);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("me/addresses")]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        var query = new GetCustomerAddressesQuery(GetUserId());
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { detail = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost("me/addresses")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request, CancellationToken cancellationToken)
    {
        var command = new AddCustomerAddressCommand(
            GetUserId(), request.Label, request.RecipientName, request.PhoneNumber, request.AddressLine1,
            request.AddressLine2, request.City, request.District, request.BuildingNumber, request.Floor,
            request.Apartment, request.Latitude, request.Longitude, request.MakeDefault);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { detail = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetAddresses), result.Value);
    }

    [HttpPost("me/addresses/{id:guid}/set-default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken cancellationToken)
    {
        var command = new SetDefaultCustomerAddressCommand(GetUserId(), id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return NoContent();
    }

    [HttpDelete("me/addresses/{id:guid}")]
    public async Task<IActionResult> DeactivateAddress(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeactivateCustomerAddressCommand(GetUserId(), id);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(new { detail = result.Error.Description }),
                ErrorType.Conflict => Conflict(new { detail = result.Error.Description }),
                _ => BadRequest(new { detail = result.Error.Description })
            };
        }

        return NoContent();
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] CustomerPreferencesDto request, CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerPreferencesCommand(
            GetUserId(), request.PreferredLanguage, request.PreferredCurrency,
            request.AllowMarketingNotifications, request.AllowOrderNotifications);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { detail = result.Error.Description });
        }

        return NoContent();
    }
}

public record CreateOrUpdateCustomerProfileRequest(string FirstName, string LastName, string PhoneNumber);

public record AddAddressRequest(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? BuildingNumber,
    string? Floor,
    string? Apartment,
    decimal Latitude,
    decimal Longitude,
    bool MakeDefault);
