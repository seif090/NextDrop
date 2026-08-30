using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextDrop.Modules.Discovery.Application.DTOs;
using NextDrop.Modules.Discovery.Application.Queries;
using NextDrop.Modules.Discovery.Domain.Enums;
using NextDrop.SharedKernel.Common;

namespace NextDrop.Api.Controllers.v1;

[ApiController]
[Route("api/v1/discovery")]
[AllowAnonymous]
public class DiscoveryController : ControllerBase
{
    private readonly ISender _sender;

    public DiscoveryController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Search and discover active public restaurants with filtering, sorting, and open-now criteria.
    /// </summary>
    [HttpGet("restaurants")]
    [ProducesResponseType(typeof(PagedDiscoveryResultDto<PublicRestaurantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRestaurants(
        [FromQuery] string? searchTerm,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] bool openNow = false,
        [FromQuery] decimal? minOrderAmount = null,
        [FromQuery] decimal? maxDeliveryFee = null,
        [FromQuery] int? minEstDeliveryTimeMinutes = null,
        [FromQuery] int? maxEstDeliveryTimeMinutes = null,
        [FromQuery] DiscoverySort sort = DiscoverySort.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublicRestaurantsQuery(
            searchTerm,
            city,
            district,
            openNow,
            minOrderAmount,
            maxDeliveryFee,
            minEstDeliveryTimeMinutes,
            maxEstDeliveryTimeMinutes,
            sort,
            page,
            pageSize);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get public restaurant by ID.
    /// </summary>
    [HttpGet("restaurants/{restaurantId:guid}")]
    [ProducesResponseType(typeof(PublicRestaurantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRestaurantById(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var query = new GetPublicRestaurantByIdQuery(restaurantId);
        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get public active branches for a restaurant.
    /// </summary>
    [HttpGet("restaurants/{restaurantId:guid}/branches")]
    [ProducesResponseType(typeof(List<PublicBranchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRestaurantBranches(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var query = new GetPublicRestaurantBranchesQuery(restaurantId);
        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Browse published catalog/menu items for a restaurant.
    /// </summary>
    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    [ProducesResponseType(typeof(PagedDiscoveryResultDto<PublicMenuItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRestaurantMenu(
        Guid restaurantId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? searchTerm,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool availableOnly = true,
        [FromQuery] MenuItemSort sort = MenuItemSort.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublicMenuItemsQuery(
            restaurantId,
            branchId,
            categoryId,
            searchTerm,
            minPrice,
            maxPrice,
            availableOnly,
            sort,
            page,
            pageSize);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Global public search for menu items across categories and restaurants.
    /// </summary>
    [HttpGet("menu/search")]
    [ProducesResponseType(typeof(PagedDiscoveryResultDto<PublicMenuItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMenuItems(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] MenuItemSort sort = MenuItemSort.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublicMenuItemsQuery(
            null,
            null,
            categoryId,
            searchTerm,
            minPrice,
            maxPrice,
            AvailableOnly: true,
            sort,
            page,
            pageSize);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(result.Value);
    }

    private IActionResult HandleFailure(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
