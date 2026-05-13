using System.Net.Http.Json;
using System.Text.Json;
using Lab5Client.Exceptions;
using Lab5Client.Models;
using Polly.CircuitBreaker;

namespace Lab5Client.Client;

// Типизированный HTTP-клиент для Lab4 API.
// Polly (retry + circuit breaker) применяется на уровне HttpMessageHandler,
// поэтому исключения здесь — уже итог всех попыток.
public class WarehouseApiClient : IWarehouseApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WarehouseApiClient(HttpClient httpClient) => _httpClient = httpClient;

    // ── Накладные ──────────────────────────────────────────────────────────

    public Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var url = $"/api/invoices?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={status}";
        return GetAsync<PagedResponse<InvoiceResponse>>(url, ct);
    }

    public Task<InvoiceResponse> GetInvoiceByIdAsync(int id, CancellationToken ct = default) =>
        GetAsync<InvoiceResponse>($"/api/invoices/{id}", ct);

    public async Task<InvoiceResponse> CreateInvoiceAsync(
        CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var resp = await ExecuteAsync(() => _httpClient.PostAsJsonAsync("/api/invoices", request, ct), ct);
        return await ReadAsync<InvoiceResponse>(resp, ct);
    }

    public async Task<InvoiceResponse> UpdateInvoiceAsync(
        int id, UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        var resp = await ExecuteAsync(() => _httpClient.PutAsJsonAsync($"/api/invoices/{id}", request, ct), ct);
        return await ReadAsync<InvoiceResponse>(resp, ct);
    }

    public async Task ConfirmInvoiceAsync(int id, CancellationToken ct = default)
    {
        var msg = new HttpRequestMessage(HttpMethod.Patch, $"/api/invoices/{id}/confirm");
        var resp = await ExecuteAsync(() => _httpClient.SendAsync(msg, ct), ct);
        await CheckStatusAsync(resp, ct);
    }

    public async Task DeleteInvoiceAsync(int id, CancellationToken ct = default)
    {
        var resp = await ExecuteAsync(() => _httpClient.DeleteAsync($"/api/invoices/{id}", ct), ct);
        await CheckStatusAsync(resp, ct);
    }

    // ── Квитанции ──────────────────────────────────────────────────────────

    public Task<PagedResponse<ReceiptResponse>> GetReceiptsAsync(
        int? invoiceId, CancellationToken ct = default)
    {
        var url = invoiceId.HasValue
            ? $"/api/receipts?invoiceId={invoiceId.Value}"
            : "/api/receipts";
        return GetAsync<PagedResponse<ReceiptResponse>>(url, ct);
    }

    public async Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request, CancellationToken ct = default)
    {
        var resp = await ExecuteAsync(() => _httpClient.PostAsJsonAsync("/api/receipts", request, ct), ct);
        return await ReadAsync<ReceiptResponse>(resp, ct);
    }

    // ── Поставщики ─────────────────────────────────────────────────────────

    public Task<IEnumerable<SupplierResponse>> GetSuppliersAsync(CancellationToken ct = default) =>
        GetAsync<IEnumerable<SupplierResponse>>("/api/suppliers", ct);

    // ── Вспомогательные методы ─────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        var resp = await ExecuteAsync(() => _httpClient.GetAsync(url, ct), ct);
        return await ReadAsync<T>(resp, ct);
    }

    // Выполняет HTTP-вызов, перехватывает сетевые исключения и преобразует в доменные
    private async Task<HttpResponseMessage> ExecuteAsync(
        Func<Task<HttpResponseMessage>> action, CancellationToken ct)
    {
        try
        {
            return await action();
        }
        catch (BrokenCircuitException ex)
        {
            // Цепь Polly разорвана — сервис недоступен
            throw new ServiceUnavailableException(
                "Цепь разорвана — сервис временно недоступен", 503, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException($"Ошибка сети: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Не отмена пользователем, а превышение таймаута HttpClient
            throw new ApiTimeoutException(
                "Превышено время ожидания ответа от сервера",
                _httpClient.Timeout, ex);
        }
    }

    // Десериализует успешный ответ, иначе бросает доменное исключение
    private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await CheckStatusAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct)
               ?? throw new NetworkException("Сервер вернул пустой ответ");
    }

    // Проверяет HTTP-статус и бросает соответствующее доменное исключение
    private async Task CheckStatusAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        switch ((int)response.StatusCode)
        {
            case 400:
                throw await ParseValidationErrorAsync(response, ct);
            case 404:
                throw new NotFoundException("Запрашиваемый ресурс не найден");
            case 409:
                var conflictBody = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Конфликт операции: {conflictBody}");
            case 503:
                throw new ServiceUnavailableException("Сервис временно недоступен", 503);
            default:
                throw new NetworkException(
                    $"Сервер вернул ошибку: {(int)response.StatusCode} {response.ReasonPhrase}");
        }
    }

    // Парсит тело 400-ответа в ApiValidationException
    private static async Task<ApiValidationException> ParseValidationErrorAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken: ct);

            var errors = new Dictionary<string, string[]>();
            if (body.TryGetProperty("errors", out var errorsEl)
                && errorsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in errorsEl.EnumerateArray())
                {
                    var field = e.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "";
                    var msg   = e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                    if (errors.TryGetValue(field, out var existing))
                        errors[field] = [..existing, msg];
                    else
                        errors[field] = [msg];
                }
            }

            var detail = body.TryGetProperty("detail", out var d)
                ? d.GetString() ?? "Ошибка валидации"
                : "Ошибка валидации";

            return new ApiValidationException(detail, errors);
        }
        catch
        {
            return new ApiValidationException("Ошибка валидации данных", []);
        }
    }
}
