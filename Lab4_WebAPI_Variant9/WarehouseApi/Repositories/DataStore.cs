using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

// Синглтон-хранилище всех данных в памяти.
// Репозитории (Scoped) держат ссылку на этот синглтон, данные живут весь lifetime приложения.
public class DataStore
{
    private int _invoiceCounter;
    private int _receiptCounter;
    private int _supplierCounter;

    public List<Invoice> Invoices { get; } = [];
    public List<Receipt> Receipts { get; } = [];
    public List<Supplier> Suppliers { get; } = [];

    public int NextInvoiceId() => Interlocked.Increment(ref _invoiceCounter);
    public int NextReceiptId() => Interlocked.Increment(ref _receiptCounter);
    public int NextSupplierId() => Interlocked.Increment(ref _supplierCounter);
}
