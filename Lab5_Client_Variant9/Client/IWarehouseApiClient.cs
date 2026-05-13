using Lab5Client.Models;

namespace Lab5Client.Client;

public interface IWarehouseApiClient
{
    Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(
        int page, int pageSize, string? status, CancellationToken ct = default);

    Task<InvoiceResponse> GetInvoiceByIdAsync(int id, CancellationToken ct = default);

    Task<InvoiceResponse> CreateInvoiceAsync(
        CreateInvoiceRequest request, CancellationToken ct = default);

    Task<InvoiceResponse> UpdateInvoiceAsync(
        int id, UpdateInvoiceRequest request, CancellationToken ct = default);

    Task ConfirmInvoiceAsync(int id, CancellationToken ct = default);

    Task DeleteInvoiceAsync(int id, CancellationToken ct = default);

    Task<PagedResponse<ReceiptResponse>> GetReceiptsAsync(
        int? invoiceId, CancellationToken ct = default);

    Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request, CancellationToken ct = default);

    Task<IEnumerable<SupplierResponse>> GetSuppliersAsync(CancellationToken ct = default);
}
