namespace PlumeriaStore.Api.Common.Validation;

/// <summary>
/// A request body that checks its own shape. This replaces DataAnnotations: <c>Validator</c>
/// discovers attributes by reflection, which Native AOT trimming can't see through, so each
/// request states its rules in code instead. The response is unchanged - still an RFC 7807
/// validation problem keyed by field name.
/// </summary>
public interface IValidatableRequest
{
    void Validate(ValidationErrors errors);
}

/// <summary>Collects per-field messages while a request validates itself.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public void Add(string member, string message)
    {
        if (!_errors.TryGetValue(member, out var messages))
        {
            messages = [];
            _errors[member] = messages;
        }

        messages.Add(message);
    }

    /// <summary>Present and not blank. Whitespace-only is treated as missing, as it was before.</summary>
    public void Required(string member, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(member, $"{member} is required");
        }
    }

    public void AtLeast(string member, decimal value, decimal minimum, string message)
    {
        if (value < minimum)
        {
            Add(member, message);
        }
    }

    public void MaxLength(string member, string? value, int maximum)
    {
        if (value is not null && value.Length > maximum)
        {
            Add(member, $"{member} must be {maximum} characters or fewer");
        }
    }

    public void NotEmpty<T>(string member, IReadOnlyCollection<T>? value, string message)
    {
        if (value is null || value.Count == 0)
        {
            Add(member, message);
        }
    }

    public Dictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());
}

/// <summary>
/// Endpoints taking a validated body opt in with
/// <c>.AddEndpointFilter&lt;ValidationFilter&lt;TRequest&gt;&gt;()</c>.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : IValidatableRequest
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is not null)
        {
            var errors = new ValidationErrors();
            argument.Validate(errors);

            if (!errors.IsValid)
            {
                return Results.ValidationProblem(errors.ToDictionary());
            }
        }

        return await next(context);
    }
}
