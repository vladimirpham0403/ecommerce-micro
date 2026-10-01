using Ecommerce.Auth.Dtos;
using FluentValidation;

namespace Ecommerce.Auth.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password phải từ 8 ký tự.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password phải có ít nhất một chữ hoa.")
            .Matches("[a-z]").WithMessage("Password phải có ít nhất một chữ thường.")
            .Matches("[0-9]").WithMessage("Password phải có ít nhất một chữ số.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    }
}
