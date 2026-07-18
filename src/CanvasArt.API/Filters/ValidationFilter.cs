using CanvasArt.API.Models.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CanvasArt.API.Filters;

/// <summary>
/// Runs any registered FluentValidation validator for each action argument and short-circuits
/// with a 400 <see cref="ApiResponse{T}"/> when validation (or model binding) fails.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<string>();

        // Model-binding / JSON parsing failures.
        if (!context.ModelState.IsValid)
        {
            errors.AddRange(context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request payload." : e.ErrorMessage));
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                    errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(
                ApiResponse<object?>.Fail("One or more validation errors occurred.", errors.Distinct().ToList()));
            return;
        }

        await next();
    }
}
