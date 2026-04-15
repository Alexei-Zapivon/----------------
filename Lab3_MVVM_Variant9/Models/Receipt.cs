namespace Lab3_MVVM_Variant9.Models;

public class Receipt : Document
{
    public override string TypeName => "Квитанция";
    public string PayerName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}
