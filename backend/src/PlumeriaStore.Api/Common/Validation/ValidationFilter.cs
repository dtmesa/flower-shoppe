using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Common.Validation;

/// <summary>
/// Minimal APIs don't get [ApiController]'s automatic DataAnnotations validation, so endpoints that take a
/// validated body opt in with <c>.AddEndpointFilter&lt;ValidationFilter&lt;TRequest&gt;&gt;()</c>.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is not null)
        {
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(argument, new ValidationContext(argument), results, validateAllProperties: true);

            if (!isValid)
            {
                var errors = results
                    .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, member) => (member, result.ErrorMessage))
                    .GroupBy(entry => entry.member)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(entry => entry.ErrorMessage ?? "Invalid value").ToArray());

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
