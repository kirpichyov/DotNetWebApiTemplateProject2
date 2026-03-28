using FluentValidation;
using SampleProject.Application.Models.ApiKeys;

namespace SampleProject.Application.Validators.ApiKeys;

public sealed class UpdateUserApiKeyRequestValidator : AbstractValidator<UpdateUserApiKeyRequest>
{
    public UpdateUserApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
