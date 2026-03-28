using FluentValidation;
using SampleProject.Application.Models.ApiKeys;

namespace SampleProject.Application.Validators.ApiKeys;

public sealed class CreateUserApiKeyRequestValidator : AbstractValidator<CreateUserApiKeyRequest>
{
    public CreateUserApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
