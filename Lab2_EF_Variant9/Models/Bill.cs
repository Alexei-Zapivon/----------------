namespace Lab2_EF_Variant9.Models;

public class Bill : Document
{
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsOverdue { get; set; }
    public string BankAccount { get; set; } = string.Empty;

    public override string GetInfo()
    {
        string status = IsOverdue ? "ПРОСРОЧЕН" : "В срок";
        return $"[Счёт] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy} | {CustomerName} | Срок: {DueDate:dd.MM.yyyy} | {status}";
    }
}
