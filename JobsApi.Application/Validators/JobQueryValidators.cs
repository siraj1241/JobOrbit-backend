using FluentValidation;
using JobsApi.Application.Dtos;

namespace JobsApi.Application.Validators;

public class JobsQueryValidator : AbstractValidator<JobsQueryDto>
{
    public JobsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location filter must not exceed 100 characters.");

        RuleFor(x => x.Tag)
            .MaximumLength(50).WithMessage("Tag filter must not exceed 50 characters.");
    }
}

public class ExternalJobsQueryValidator : AbstractValidator<ExternalJobsQueryDto>
{
    public ExternalJobsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location filter must not exceed 100 characters.");
    }
}
