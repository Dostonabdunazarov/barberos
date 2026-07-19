using FluentValidation;

namespace Barberos.Application.Services;

public sealed class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 24 * 60);
        RuleFor(x => x.BufferMinutes).InclusiveBetween(0, 24 * 60);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 24 * 60);
        RuleFor(x => x.BufferMinutes).InclusiveBetween(0, 24 * 60);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
