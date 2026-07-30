using Ecommerce.Product.Dtos;
using FluentValidation;

namespace Ecommerce.Product.Validation;

public class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
