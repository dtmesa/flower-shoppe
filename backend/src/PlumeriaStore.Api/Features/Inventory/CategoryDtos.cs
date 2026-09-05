using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Inventory;

public record CategoryCreateRequest(CategoryKind Kind, string Name, string Code) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(Name), Name);
        errors.Required(nameof(Code), Code);
        errors.MaxLength(nameof(Code), Code, 4);
    }
}

public record CategoryUpdateRequest(string Name, string Code) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(Name), Name);
        errors.Required(nameof(Code), Code);
        errors.MaxLength(nameof(Code), Code, 4);
    }
}

public record CategoryResponse(int Id, CategoryKind Kind, string Name, string Code);
