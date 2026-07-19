using Barberos.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Barberos.Api.Validation;

/// <summary>
/// Прогоняет зарегистрированные FluentValidation-валидаторы по аргументам action.
/// При ошибках бросает <see cref="ValidationAppException"/> (→ 400 ProblemDetails).
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (result.IsValid)
                continue;

            foreach (var failure in result.Errors)
            {
                var key = failure.PropertyName;
                errors[key] = errors.TryGetValue(key, out var existing)
                    ? [.. existing, failure.ErrorMessage]
                    : [failure.ErrorMessage];
            }
        }

        if (errors.Count > 0)
            throw new ValidationAppException(errors);

        await next();
    }
}
