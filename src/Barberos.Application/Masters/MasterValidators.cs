using FluentValidation;

namespace Barberos.Application.Masters;

public sealed class CreateMasterRequestValidator : AbstractValidator<CreateMasterRequest>
{
    public CreateMasterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.PhotoUrl).MaximumLength(1000);

        // Учётная запись мастера опциональна, но email и пароль задаются вместе.
        When(x => !string.IsNullOrWhiteSpace(x.LoginEmail) || !string.IsNullOrWhiteSpace(x.LoginPassword), () =>
        {
            RuleFor(x => x.LoginEmail).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.LoginPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        });
    }
}

public sealed class UpdateMasterRequestValidator : AbstractValidator<UpdateMasterRequest>
{
    public UpdateMasterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.PhotoUrl).MaximumLength(1000);
    }
}
