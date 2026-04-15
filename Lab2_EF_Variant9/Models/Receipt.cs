namespace Lab2_EF_Variant9.Models;

public class Receipt : Document
{
    public string PayerName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    public override string GetInfo()
    {
        string status = IsPaid ? $"Оплачено {PaidAt:dd.MM.yyyy}" : "Не оплачено";
        return $"[Квитанция] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy} | {PayerName} → {ReceiverName} | {status}";
    }
}
