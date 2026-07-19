using FluentValidation;

namespace Barberos.Application.Bookings;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.GuestName)
            .NotEmpty().WithMessage("Укажите имя.")
            .MaximumLength(120);

        RuleFor(x => x.GuestPhone)
            .NotEmpty().WithMessage("Укажите телефон.")
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-()]{7,20}$").WithMessage("Некорректный номер телефона.");

        RuleFor(x => x.MasterId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty();
    }
}

public sealed class UpdateBookingStatusRequestValidator : AbstractValidator<UpdateBookingStatusRequest>
{
    public UpdateBookingStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class RescheduleBookingRequestValidator : AbstractValidator<RescheduleBookingRequest>
{
    public RescheduleBookingRequestValidator()
    {
        RuleFor(x => x.NewStartAt).NotEmpty();
    }
}
