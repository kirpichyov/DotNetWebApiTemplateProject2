using FluentValidation;
using SampleProject.Application.Extensions;
using SampleProject.Application.Models.Auth;

namespace SampleProject.Application.Validators.Users;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(x => x.NewPassword)
            .ApplyPasswordValidationRules();
    }
}