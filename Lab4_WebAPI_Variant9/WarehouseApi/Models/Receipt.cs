namespace WarehouseApi.Models;

// Квитанция о получении товара по накладной
public class Receipt
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }           // Ссылка на накладную
    public DateTime ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;  // Кто принял
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
