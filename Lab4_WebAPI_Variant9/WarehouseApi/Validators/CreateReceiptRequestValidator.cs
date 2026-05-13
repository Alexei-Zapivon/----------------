using FluentValidation;
using WarehouseApi.DTOs;

namespace WarehouseApi.Validators;

public class CreateReceiptRequestValidator : AbstractValidator<CreateReceiptRequest>
{
    public CreateReceiptRequestValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("ID накладной должен быть больше 0");

        RuleFor(x => x.ReceivedBy)
            .NotEmpty().WithMessage("Имя получателя обязательно")
            .MinimumLength(2).WithMessage("Имя получателя должно содержать минимум 2 символа");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Сумма квитанции должна быть больше 0");

        RuleFor(x => x.ReceivedDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Дата получения не может быть в будущем");
    }
}
