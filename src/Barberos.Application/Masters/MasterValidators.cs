using FluentValidation;

namespace Barberos.Application.Masters;

/// <summary>Общие правила полей мастера, чтобы create/update/contact не разъезжались.</summary>
internal static class MasterRules
{
    /// <summary>Тот же формат, что у GuestPhone в бронях.</summary>
    public const string PhonePattern = @"^\+?[0-9\s\-()]{7,20}$";

    /// <summary>
    /// Правила заполненного публичного телефона. Оборачивается в When(...) на месте
    /// вызова: пустое значение/null допустимы (контакт не указан или снят с витрины).
    /// </summary>
    public static IRuleBuilderOptions<T, string?> PublicPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(20)
            .Matches(PhonePattern).WithMessage("Некорректный номер телефона.");
}

public sealed class CreateMasterRequestValidator : AbstractValidator<CreateMasterRequest>
{
    public CreateMasterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.PhotoUrl).MaximumLength(1000);

        When(x => !string.IsNullOrWhiteSpace(x.PublicPhone), () =>
            RuleFor(x => x.PublicPhone).PublicPhone());

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

        When(x => !string.IsNullOrWhiteSpace(x.PublicPhone), () =>
            RuleFor(x => x.PublicPhone).PublicPhone());

        // Учётка опциональна. Если задан email — он должен быть валиден;
        // если задан пароль — не короче 8. Пустые поля учётку не трогают.
        When(x => !string.IsNullOrWhiteSpace(x.LoginEmail), () =>
            RuleFor(x => x.LoginEmail).EmailAddress().MaximumLength(256));

        When(x => !string.IsNullOrWhiteSpace(x.LoginPassword), () =>
            RuleFor(x => x.LoginPassword).MinimumLength(8).MaximumLength(128));
    }
}

public sealed class UpdateMasterContactRequestValidator : AbstractValidator<UpdateMasterContactRequest>
{
    public UpdateMasterContactRequestValidator()
    {
        // Пустое значение допустимо — так мастер убирает номер с витрины.
        When(x => !string.IsNullOrWhiteSpace(x.PublicPhone), () =>
            RuleFor(x => x.PublicPhone).PublicPhone());
    }
}
