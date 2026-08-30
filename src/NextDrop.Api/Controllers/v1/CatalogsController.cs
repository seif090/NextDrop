using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Catalog.Application.Commands;
using NextDrop.Modules.Catalog.Application.DTOs;
using NextDrop.Modules.Catalog.Application.Queries;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1")]
public class CatalogsController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogsController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }

    [HttpPost("restaurants/{restaurantId:guid}/catalog")]
    [Authorize]
    public async Task<IActionResult> CreateCatalog(Guid restaurantId, [FromBody] CreateCatalogRequest request)
    {
        var command = new CreateCatalogCommand(GetUserId(), restaurantId, request.Name, request.Description);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized") || result.Error.Code.Contains("Forbidden"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });
            if (result.Error.Code.Contains("AlreadyExists"))
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetCatalogById), new { catalogId = result.Value.Id }, result.Value);
    }

    [HttpGet("restaurants/{restaurantId:guid}/catalog")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicCatalog(Guid restaurantId)
    {
        var query = new GetPublicCatalogQuery(restaurantId);
        var result = await _sender.Send(query);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotPublished") || result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpGet("catalogs/{catalogId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetCatalogById(Guid catalogId)
    {
        var query = new GetCatalogByIdQuery(catalogId, GetUserId());
        var result = await _sender.Send(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost("catalogs/{catalogId:guid}/publish")]
    [Authorize]
    public async Task<IActionResult> PublishCatalog(Guid catalogId)
    {
        var command = new PublishCatalogCommand(GetUserId(), catalogId);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [HttpPost("catalogs/{catalogId:guid}/categories")]
    [Authorize]
    public async Task<IActionResult> CreateCategory(Guid catalogId, [FromBody] CreateCategoryRequest request)
    {
        var command = new CreateCategoryCommand(GetUserId(), catalogId, request.Name, request.Description, request.DisplayOrder);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return CreatedAtAction(nameof(GetCatalogById), new { catalogId }, result.Value);
    }

    [HttpPost("categories/{categoryId:guid}/items")]
    [Authorize]
    public async Task<IActionResult> CreateMenuItem(Guid categoryId, [FromBody] CreateMenuItemRequest request)
    {
        var command = new CreateMenuItemCommand(GetUserId(), categoryId, request.Name, request.Description, request.BasePrice, request.DisplayOrder);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(new { error = result.Error.Description });
        }

        return Created($"/api/v1/menu-items/{result.Value.Id}", result.Value);
    }

    [HttpPut("menu-items/{menuItemId:guid}/price")]
    [Authorize]
    public async Task<IActionResult> ChangeMenuItemPrice(Guid menuItemId, [FromBody] ChangePriceRequest request)
    {
        var command = new ChangeMenuItemPriceCommand(GetUserId(), menuItemId, request.NewPrice);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("Unauthorized"))
                return Forbid();
            if (result.Error.Code.Contains("NotFound"))
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }
}

public record CreateCatalogRequest(string Name, string? Description);
public record CreateCategoryRequest(string Name, string? Description, int DisplayOrder);
public record CreateMenuItemRequest(string Name, string? Description, decimal BasePrice, int DisplayOrder);
public record ChangePriceRequest(decimal NewPrice);
