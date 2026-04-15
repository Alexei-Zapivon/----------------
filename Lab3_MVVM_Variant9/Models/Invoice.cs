namespace Lab3_MVVM_Variant9.Models;

public class Invoice : Document
{
    public override string TypeName => "Накладная";
    public string SupplierName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public int ItemsCount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
}
