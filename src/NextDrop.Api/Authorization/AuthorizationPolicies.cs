using Microsoft.AspNetCore.Authorization;
using NextDrop.Modules.Identity.Domain.Aggregates.User;

namespace NextDrop.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string CanManageRestaurant = "CanManageRestaurant";
    public const string CanManageCatalog = "CanManageCatalog";
    public const string CanAssignRider = "CanAssignRider";
    public const string CanRefundOrder = "CanRefundOrder";
    public const string CanViewFinancialReports = "CanViewFinancialReports";
    public const string CanManageUsers = "CanManageUsers";

    public static void AddNextDropAuthorizationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(CanManageRestaurant, policy =>
            policy.RequireRole(
                UserRole.RestaurantOwner.ToString(),
                UserRole.OperationsAdmin.ToString(),
                UserRole.SuperAdmin.ToString()));

        options.AddPolicy(CanManageCatalog, policy =>
            policy.RequireRole(
                UserRole.RestaurantOwner.ToString(),
                UserRole.RestaurantStaff.ToString(),
                UserRole.OperationsAdmin.ToString(),
                UserRole.SuperAdmin.ToString()));

        options.AddPolicy(CanAssignRider, policy =>
            policy.RequireRole(
                UserRole.OperationsAdmin.ToString(),
                UserRole.SupportAgent.ToString(),
                UserRole.SuperAdmin.ToString()));

        options.AddPolicy(CanRefundOrder, policy =>
            policy.RequireRole(
                UserRole.FinanceAdmin.ToString(),
                UserRole.SupportAgent.ToString(),
                UserRole.OperationsAdmin.ToString(),
                UserRole.SuperAdmin.ToString()));

        options.AddPolicy(CanViewFinancialReports, policy =>
            policy.RequireRole(
                UserRole.FinanceAdmin.ToString(),
                UserRole.OperationsAdmin.ToString(),
                UserRole.SuperAdmin.ToString()));

        options.AddPolicy(CanManageUsers, policy =>
            policy.RequireRole(
                UserRole.OperationsAdmin.ToString(),
                UserRole.SuperAdmin.ToString()));
    }
}
