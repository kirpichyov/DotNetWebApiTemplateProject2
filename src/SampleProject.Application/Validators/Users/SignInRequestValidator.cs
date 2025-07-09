using FluentValidation;
using SampleProject.Application.Models.Auth;

namespace SampleProject.Application.Validators.Users;

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.AuthType)
            .IsInEnum();
    }
}