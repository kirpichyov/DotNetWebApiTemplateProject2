using System.Text.RegularExpressions;
using FluentValidation;

namespace SampleProject.Application.Extensions;

public static class ValidationExtensions
{
    public static void ApplyPasswordValidationRules<TModel>(this IRuleBuilderInitial<TModel, string> initial)
    {
        initial.Cascade(CascadeMode.Stop);
        
        initial
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(32);

        initial
            .Must(password => !password.Contains(' '))
            .WithMessage("Can't contain a whitespace")
            .Must(password => Regex.IsMatch(password, @"(?=.*[\W_])", RegexOptions.Compiled, TimeSpan.FromSeconds(2)))
            .WithMessage("Must have at least 1 special character")
            .Must(password => Regex.IsMatch(password, @"(?=.*\d)", RegexOptions.Compiled, TimeSpan.FromSeconds(2)))
            .WithMessage("Must have at least 1 number")
            .Must(password => Regex.IsMatch(password, @"(?=.*[A-Z])", RegexOptions.Compiled, TimeSpan.FromSeconds(2)))
            .WithMessage("Must have at least 1 upper case character");
    }
}