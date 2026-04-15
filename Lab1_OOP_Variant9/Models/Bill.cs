namespace Lab1_OOP_Variant9.Models;

// Счёт — наследует Document (НАСЛЕДОВАНИЕ)
public class Bill : Document
{
    public string CustomerName { get; set; } = string.Empty;  // Клиент
    public DateTime DueDate { get; set; }                      // Срок оплаты
    public decimal TaxAmount { get; set; }                     // Сумма налога (НДС)
    public bool IsOverdue { get; set; }                        // Просрочен?
    public string BankAccount { get; set; } = string.Empty;   // Банковский счёт

    public override string GetTypeName() => "Счёт";

    // ПОЛИМОРФИЗМ — переопределяем метод базового класса
    public override string GetInfo()
    {
        string status = IsOverdue ? "!! ПРОСРОЧЕН !!" : "В срок";
        return $"[Счёт] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy}\n" +
               $"  Клиент: {CustomerName} | Срок: {DueDate:dd.MM.yyyy} | НДС: {TaxAmount:N2} руб. | {status}\n" +
               $"  Счёт: {BankAccount}";
    }
}
