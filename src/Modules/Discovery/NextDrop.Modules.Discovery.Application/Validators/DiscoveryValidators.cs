using FluentValidation;
using NextDrop.Modules.Discovery.Application.Queries;

namespace NextDrop.Modules.Discovery.Application.Validators;

public class GetPublicRestaurantsQueryValidator : AbstractValidator<GetPublicRestaurantsQuery>
{
    public GetPublicRestaurantsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
    }
}

public class GetPublicMenuItemsQueryValidator : AbstractValidator<GetPublicMenuItemsQuery>
{
    public GetPublicMenuItemsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
    }
}
