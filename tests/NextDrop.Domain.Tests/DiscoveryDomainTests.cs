using FluentAssertions;
using NextDrop.Modules.Discovery.Domain.Enums;
using NextDrop.Modules.Discovery.Domain.ValueObjects;
using Xunit;

namespace NextDrop.Domain.Tests;

public class DiscoveryDomainTests
{
    [Fact]
    public void RestaurantDiscoveryCriteria_Create_WithValidParameters_ShouldSucceed()
    {
        var result = RestaurantDiscoveryCriteria.Create(
            searchTerm: "Pizza",
            city: "Cairo",
            district: "Maadi",
            openNow: true,
            minOrderAmount: 50m,
            maxDeliveryFee: 15m,
            minEstDeliveryTimeMinutes: 20,
            maxEstDeliveryTimeMinutes: 45,
            sort: DiscoverySort.FastestDelivery,
            page: 1,
            pageSize: 20);

        result.IsSuccess.Should().BeTrue();
        var criteria = result.Value;
        criteria.SearchTerm.Should().Be("Pizza");
        criteria.City.Should().Be("Cairo");
        criteria.District.Should().Be("Maadi");
        criteria.OpenNow.Should().BeTrue();
        criteria.MinOrderAmount.Should().Be(50m);
        criteria.MaxDeliveryFee.Should().Be(15m);
        criteria.Sort.Should().Be(DiscoverySort.FastestDelivery);
    }

    [Fact]
    public void RestaurantDiscoveryCriteria_Create_WithLongSearchTerm_ShouldFailValidation()
    {
        var longSearch = new string('a', 101);
        var result = RestaurantDiscoveryCriteria.Create(
            searchTerm: longSearch,
            city: "Cairo",
            district: "Maadi",
            openNow: false,
            minOrderAmount: null,
            maxDeliveryFee: null,
            minEstDeliveryTimeMinutes: null,
            maxEstDeliveryTimeMinutes: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Discovery.SearchTermTooLong");
    }

    [Fact]
    public void RestaurantDiscoveryCriteria_Create_WithInvalidPageSize_ShouldFailValidation()
    {
        var result = RestaurantDiscoveryCriteria.Create(
            searchTerm: "Burger",
            city: null,
            district: null,
            openNow: false,
            minOrderAmount: null,
            maxDeliveryFee: null,
            minEstDeliveryTimeMinutes: null,
            maxEstDeliveryTimeMinutes: null,
            page: 1,
            pageSize: 200);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Discovery.InvalidPageSize");
    }

    [Fact]
    public void MenuItemDiscoveryCriteria_Create_WithValidParameters_ShouldSucceed()
    {
        var rId = Guid.NewGuid();
        var result = MenuItemDiscoveryCriteria.Create(
            restaurantId: rId,
            branchId: null,
            categoryId: null,
            searchTerm: "Burger",
            minPrice: 10m,
            maxPrice: 100m,
            availableOnly: true,
            publishedOnly: true,
            sort: MenuItemSort.PriceAscending,
            page: 1,
            pageSize: 15);

        result.IsSuccess.Should().BeTrue();
        var criteria = result.Value;
        criteria.RestaurantId.Should().Be(rId);
        criteria.SearchTerm.Should().Be("Burger");
        criteria.MinPrice.Should().Be(10m);
        criteria.MaxPrice.Should().Be(100m);
        criteria.Sort.Should().Be(MenuItemSort.PriceAscending);
    }

    [Fact]
    public void MenuItemDiscoveryCriteria_Create_WithInvalidPage_ShouldFailValidation()
    {
        var result = MenuItemDiscoveryCriteria.Create(
            restaurantId: null,
            branchId: null,
            categoryId: null,
            searchTerm: "Fries",
            minPrice: null,
            maxPrice: null,
            page: 0,
            pageSize: 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Discovery.InvalidPage");
    }
}
