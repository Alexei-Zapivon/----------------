namespace Lab1_OOP_Variant9.Models;

// Квитанция — наследует Document (НАСЛЕДОВАНИЕ)
public class Receipt : Document
{
    public string PayerName { get; set; } = string.Empty;      // Плательщик
    public string ReceiverName { get; set; } = string.Empty;   // Получатель
    public string PaymentMethod { get; set; } = string.Empty;  // Способ оплаты
    public bool IsPaid { get; set; }                            // Оплачено?
    public DateTime? PaidAt { get; set; }                       // Дата оплаты

    public override string GetTypeName() => "Квитанция";

    // ПОЛИМОРФИЗМ — переопределяем метод базового класса
    public override string GetInfo()
    {
        string status = IsPaid ? $"Оплачено {PaidAt:dd.MM.yyyy}" : "Не оплачено";
        return $"[Квитанция] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy}\n" +
               $"  Плательщик: {PayerName} → Получатель: {ReceiverName} | {PaymentMethod} | {status}";
    }
}
