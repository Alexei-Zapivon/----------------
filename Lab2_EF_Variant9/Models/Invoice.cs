namespace Lab2_EF_Variant9.Models;

public class Invoice : Document
{
    public string SupplierName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public int ItemsCount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;

    public override string GetInfo()
        => $"[Накладная] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy} | {SupplierName} → {BuyerName} | Позиций: {ItemsCount}";
}
