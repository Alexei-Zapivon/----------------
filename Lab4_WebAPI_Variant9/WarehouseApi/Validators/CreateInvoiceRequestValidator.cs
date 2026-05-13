using FluentValidation;
using WarehouseApi.DTOs;

namespace WarehouseApi.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Номер накладной обязателен")
            .MinimumLength(3).WithMessage("Номер накладной должен содержать минимум 3 символа")
            .MaximumLength(50).WithMessage("Номер накладной не должен превышать 50 символов");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Дата накладной не может быть в будущем");

        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("ID поставщика должен быть больше 0");

        RuleFor(x => x.TotalAmount)
            .InclusiveBetween(0.01m, 10_000_000m)
            .WithMessage("Сумма накладной должна быть от 0.01 до 10 000 000");
    }
}
