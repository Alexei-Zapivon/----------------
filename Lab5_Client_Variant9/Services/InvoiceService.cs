using Lab5Client.Client;
using Lab5Client.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Lab5Client.Services;

// Бизнес-логика клиента + кэширование ответов API
public class InvoiceService
{
    private readonly IWarehouseApiClient _client;
    private readonly IMemoryCache _cache;

    // Отслеживаем ключи кэша списков накладных для массовой инвалидации
    private readonly HashSet<string> _invoiceListKeys = [];

    public InvoiceService(IWarehouseApiClient client, IMemoryCache cache)
    {
        _client = client;
        _cache  = cache;
    }

    // ── Накладные ──────────────────────────────────────────────────────────

    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(
        int page = 1, int pageSize = 10, string? status = null, CancellationToken ct = default)
    {
        var key = $"invoices_p{page}_s{pageSize}_{status ?? "all"}";

        if (_cache.TryGetValue(key, out PagedResponse<InvoiceResponse>? hit))
        {
            PrintCache($"Из кэша: {key}");
            return hit!;
        }

        var result = await _client.GetInvoicesAsync(page, pageSize, status, ct);
        _cache.Set(key, result, TimeSpan.FromMinutes(5));
        _invoiceListKeys.Add(key);
        PrintCache($"Сохранено в кэш: {key}");
        return result;
    }

    public async Task<InvoiceResponse> GetInvoiceByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"invoice_{id}";

        if (_cache.TryGetValue(key, out InvoiceResponse? hit))
        {
            PrintCache($"Из кэша: {key}");
            return hit!;
        }

        var result = await _client.GetInvoiceByIdAsync(id, ct);
        _cache.Set(key, result, TimeSpan.FromMinutes(3));
        PrintCache($"Сохранено в кэш: {key}");
        return result;
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(
        CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var result = await _client.CreateInvoiceAsync(request, ct);
        InvalidateInvoiceLists();
        return result;
    }

    public async Task ConfirmInvoiceAsync(int id, CancellationToken ct = default)
    {
        await _client.ConfirmInvoiceAsync(id, ct);
        _cache.Remove($"invoice_{id}");
        InvalidateInvoiceLists();
    }

    public async Task DeleteInvoiceAsync(int id, CancellationToken ct = default)
    {
        await _client.DeleteInvoiceAsync(id, ct);
        _cache.Remove($"invoice_{id}");
        InvalidateInvoiceLists();
    }

    // Возвращает все накладные (до 200) для экспорта
    public async Task<List<InvoiceResponse>> GetAllForExportAsync(CancellationToken ct = default)
    {
        var paged = await _client.GetInvoicesAsync(1, 200, null, ct);
        return paged.Items.ToList();
    }

    // Массовое создание с прогресс-баром
    public async Task<List<InvoiceResponse>> BulkCreateAsync(
        IList<CreateInvoiceRequest> requests,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<InvoiceResponse>(requests.Count);
        for (int i = 0; i < requests.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var created = await _client.CreateInvoiceAsync(requests[i], ct);
            results.Add(created);
            progress?.Report(i + 1);
        }
        InvalidateInvoiceLists();
        return results;
    }

    // ── Квитанции ──────────────────────────────────────────────────────────

    public Task<PagedResponse<ReceiptResponse>> GetReceiptsAsync(
        int? invoiceId = null, CancellationToken ct = default) =>
        _client.GetReceiptsAsync(invoiceId, ct);

    public Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request, CancellationToken ct = default) =>
        _client.CreateReceiptAsync(request, ct);

    // ── Поставщики ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<SupplierResponse>> GetSuppliersAsync(CancellationToken ct = default)
    {
        const string key = "suppliers";

        if (_cache.TryGetValue(key, out IEnumerable<SupplierResponse>? hit))
        {
            PrintCache($"Из кэша: {key}");
            return hit!;
        }

        var result = await _client.GetSuppliersAsync(ct);
        _cache.Set(key, result, TimeSpan.FromMinutes(10));
        PrintCache($"Сохранено в кэш: {key}");
        return result;
    }

    // ── Вспомогательные ────────────────────────────────────────────────────

    private void InvalidateInvoiceLists()
    {
        foreach (var k in _invoiceListKeys)
            _cache.Remove(k);
        _invoiceListKeys.Clear();
    }

    private static void PrintCache(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ✓ {msg}");
        Console.ResetColor();
    }
}
