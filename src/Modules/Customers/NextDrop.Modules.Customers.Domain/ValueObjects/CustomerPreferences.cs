using NextDrop.SharedKernel.Common;

namespace NextDrop.Modules.Customers.Domain.ValueObjects;

public class CustomerPreferences : ValueObject
{
    public string PreferredLanguage { get; private set; }
    public string PreferredCurrency { get; private set; }
    public bool AllowMarketingNotifications { get; private set; }
    public bool AllowOrderNotifications { get; private set; }

    private CustomerPreferences()
    {
        PreferredLanguage = "en";
        PreferredCurrency = "USD";
        AllowMarketingNotifications = true;
        AllowOrderNotifications = true;
    }

    public CustomerPreferences(
        string preferredLanguage,
        string preferredCurrency,
        bool allowMarketingNotifications,
        bool allowOrderNotifications)
    {
        PreferredLanguage = string.IsNullOrWhiteSpace(preferredLanguage) ? "en" : preferredLanguage;
        PreferredCurrency = string.IsNullOrWhiteSpace(preferredCurrency) ? "USD" : preferredCurrency;
        AllowMarketingNotifications = allowMarketingNotifications;
        AllowOrderNotifications = allowOrderNotifications;
    }

    public static CustomerPreferences Default => new("en", "USD", true, true);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PreferredLanguage;
        yield return PreferredCurrency;
        yield return AllowMarketingNotifications;
        yield return AllowOrderNotifications;
    }
}
