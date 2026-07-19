using FluentValidation;

namespace Barberos.Application.Scheduling;

public sealed class SetScheduleRequestValidator : AbstractValidator<SetScheduleRequest>
{
    public SetScheduleRequestValidator()
    {
        RuleFor(x => x.Entries).NotNull();
        RuleForEach(x => x.Entries).ChildRules(e =>
        {
            e.RuleFor(x => x.DayOfWeek).IsInEnum();
            e.RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Конец интервала должен быть позже начала.");
        });

        // Интервалы одного дня не должны пересекаться.
        RuleFor(x => x.Entries).Must(NoOverlapWithinDay)
            .WithMessage("Рабочие интервалы одного дня не должны пересекаться.");
    }

    private static bool NoOverlapWithinDay(IReadOnlyList<ScheduleEntryDto> entries)
    {
        foreach (var group in entries.GroupBy(e => e.DayOfWeek))
        {
            var ordered = group.OrderBy(e => e.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                    return false;
        }
        return true;
    }
}

public sealed class CreateTimeOffRequestValidator : AbstractValidator<CreateTimeOffRequest>
{
    public CreateTimeOffRequestValidator()
    {
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("Конец периода должен быть позже начала.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
