using FluentValidation;
using HttpAndGrpcProductServices.Application.Dtos;
using HttpAndGrpcProductServices.Domain.Enums;

namespace HttpAndGrpcProductServices.Application.Validators;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование товара обязательно")
            .MaximumLength(500).WithMessage("Наименование не должно превышать 500 символов");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Цена должна быть больше 0");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Вес должен быть больше 0");

        RuleFor(x => x.Type)
            .Must(t => Enum.IsDefined(typeof(ProductType), t))
            .WithMessage("Некорректный вид товара. Допустимые значения: 0, 1, 2, 3");

        RuleFor(x => x.WarehouseNumber)
            .GreaterThan(0).WithMessage("Номер склада должен быть больше 0");
    }
}