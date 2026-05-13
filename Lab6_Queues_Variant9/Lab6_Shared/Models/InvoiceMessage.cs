namespace Lab6Shared.Models;

// Payload для накладной (соответствует структуре из Lab1_OOP_Variant9 и Lab4)
public class InvoiceMessage
{
    public int      Id           { get; set; }
    public string   Number       { get; set; } = "";
    public DateTime Date         { get; set; }
    public string   SupplierName { get; set; } = "";
    public decimal  TotalAmount  { get; set; }
    public string   Status       { get; set; } = "Draft";
}
