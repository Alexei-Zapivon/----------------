namespace Lab3_MVVM_Variant9.Models;

public class Bill : Document
{
    public override string TypeName => "Счёт";
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsOverdue { get; set; }
    public string BankAccount { get; set; } = string.Empty;
}
