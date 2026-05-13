namespace WarehouseApi.Models;

// Статус накладной
public enum InvoiceStatus
{
    Draft,      // Черновик
    Confirmed,  // Подтверждена
    Cancelled   // Отменена
}

// Накладная — основной документ склада
public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;  // Номер накладной
    public DateTime Date { get; set; }
    public int SupplierId { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
}
