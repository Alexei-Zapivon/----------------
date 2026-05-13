using Lab5Client.Client;
using Lab5Client.Exceptions;
using Lab5Client.Export;
using Lab5Client.Models;
using Lab5Client.Services;
using Lab5Client.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using QuestPDF.Infrastructure;

// ── Лицензии ───────────────────────────────────────────────────────────────
QuestPDF.Settings.License = LicenseType.Community;
Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Ctrl+C → отмена операции ───────────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n  Операция отменена пользователем.");
};

// ── Polly: политика повтора ────────────────────────────────────────────────
// 3 попытки с экспоненциальной задержкой: 1с → 2с → 4с
IAsyncPolicy<HttpResponseMessage> retryPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => (int)r.StatusCode >= 500)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
        onRetry: (_, timespan, retryCount, _) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  Повтор попытки №{retryCount} через {timespan.TotalSeconds:F0}с...");
            Console.ResetColor();
        });

// ── Polly: circuit breaker ────────────────────────────────────────────────
// Разрыв после 5 последовательных ошибок, восстановление через 30 сек
IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => (int)r.StatusCode >= 500)
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (_, _) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  Цепь разорвана! Сервис недоступен. Повтор через 30 сек.");
            Console.ResetColor();
        },
        onReset: () =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Цепь восстановлена. Соединение активно.");
            Console.ResetColor();
        },
        onHalfOpen: () => Console.WriteLine("  Проверка доступности сервиса..."));

// PolicyWrap: retry (внешняя) оборачивает circuit breaker (внутренняя)
IAsyncPolicy<HttpResponseMessage> combinedPolicy =
    Policy.WrapAsync<HttpResponseMessage>(retryPolicy, circuitBreakerPolicy);

// ── DI-контейнер ───────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddMemoryCache();

// Типизированный HTTP-клиент с применением обеих политик
services.AddHttpClient<IWarehouseApiClient, WarehouseApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout     = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddPolicyHandler(combinedPolicy);

services.AddSingleton<InvoiceService>();
services.AddSingleton<ExportService>();
services.AddSingleton<CsvExporter>();
services.AddSingleton<ExcelExporter>();
services.AddSingleton<PdfExporter>();

var sp = services.BuildServiceProvider();

var invoiceSvc = sp.GetRequiredService<InvoiceService>();
var exportSvc  = sp.GetRequiredService<ExportService>();

// ── Главное меню ───────────────────────────────────────────────────────────
while (!cts.IsCancellationRequested)
{
    PrintMenu();

    var choice = Console.ReadLine()?.Trim();
    if (choice == "0") break;

    Console.WriteLine();
    try
    {
        await (choice switch
        {
            "1"  => MenuShowInvoicesAsync(invoiceSvc, cts.Token),
            "2"  => MenuShowInvoiceByIdAsync(invoiceSvc, cts.Token),
            "3"  => MenuCreateInvoiceAsync(invoiceSvc, cts.Token),
            "4"  => MenuConfirmInvoiceAsync(invoiceSvc, cts.Token),
            "5"  => MenuDeleteInvoiceAsync(invoiceSvc, cts.Token),
            "6"  => MenuShowReceiptsAsync(invoiceSvc, cts.Token),
            "7"  => MenuCreateReceiptAsync(invoiceSvc, cts.Token),
            "8"  => MenuShowSuppliersAsync(invoiceSvc, cts.Token),
            "9"  => MenuBulkCreateAsync(invoiceSvc, cts.Token),
            "10" => MenuExportCsvAsync(invoiceSvc, exportSvc, cts.Token),
            "11" => MenuExportExcelAsync(invoiceSvc, exportSvc, cts.Token),
            "12" => MenuExportPdfAsync(invoiceSvc, exportSvc, cts.Token),
            _    => Task.Run(() => PrintWarn("Неверный пункт меню"))
        });
    }
    catch (OperationCanceledException)
    {
        PrintWarn("Операция отменена пользователем.");
    }
    catch (NotFoundException ex)
    {
        PrintError($"Не найдено: {ex.Message}");
    }
    catch (ApiValidationException ex)
    {
        PrintError($"Ошибка валидации: {ex.Message}");
        foreach (var (field, msgs) in ex.Errors)
            PrintError($"  [{field}]: {string.Join(", ", msgs)}");
    }
    catch (ServiceUnavailableException ex)
    {
        PrintError($"Сервис недоступен ({ex.StatusCode}): {ex.Message}");
    }
    catch (ApiTimeoutException ex)
    {
        PrintError($"Таймаут ({ex.TimeoutDuration.TotalSeconds:F0}с): {ex.Message}");
    }
    catch (NetworkException ex)
    {
        PrintError($"Ошибка сети: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
        PrintError($"Конфликт: {ex.Message}");
    }
    catch (Exception ex)
    {
        PrintError($"Неожиданная ошибка: {ex.Message}");
    }

    if (!cts.IsCancellationRequested)
    {
        Console.WriteLine("\n  Нажмите Enter для продолжения...");
        Console.ReadLine();
    }
}

Console.WriteLine("\n  До свидания!");

// ═══════════════════════════════════════════════════════════════════════════
// ПУНКТЫ МЕНЮ
// ═══════════════════════════════════════════════════════════════════════════

static async Task MenuShowInvoicesAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  Страница (Enter = 1): ");
    int.TryParse(Console.ReadLine(), out int page);
    if (page < 1) page = 1;

    Console.Write("  Статус (Draft/Confirmed/Cancelled, Enter = все): ");
    var status = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(status)) status = null;

    var result = await svc.GetInvoicesAsync(page, 5, status, ct);

    PrintHeader($"Накладные — страница {result.Page}/{result.TotalPages} (всего: {result.TotalCount})");
    PrintInvoiceHeader();
    foreach (var inv in result.Items)
        PrintInvoiceRow(inv);

    if (result.HasNextPage) PrintInfo("  → есть следующая страница");
    if (result.HasPreviousPage) PrintInfo("  ← есть предыдущая страница");
}

static async Task MenuShowInvoiceByIdAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  ID накладной: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    { PrintWarn("Неверный ID"); return; }

    var inv = await svc.GetInvoiceByIdAsync(id, ct);
    PrintHeader("Накладная");
    PrintInvoiceHeader();
    PrintInvoiceRow(inv);
}

static async Task MenuCreateInvoiceAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  Номер накладной: ");
    var number = Console.ReadLine()?.Trim() ?? "";

    Console.Write("  Дата (dd.MM.yyyy, Enter = сегодня): ");
    var dateStr = Console.ReadLine()?.Trim();
    var date = string.IsNullOrWhiteSpace(dateStr)
        ? DateTime.Today
        : DateTime.ParseExact(dateStr, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture);

    Console.Write("  ID поставщика: ");
    int.TryParse(Console.ReadLine(), out int supplierId);

    Console.Write("  Сумма: ");
    decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out decimal amount);

    var created = await svc.CreateInvoiceAsync(
        new CreateInvoiceRequest(number, date, supplierId, amount), ct);

    PrintSuccess($"Накладная создана: ID={created.Id}, №{created.Number}");
}

static async Task MenuConfirmInvoiceAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  ID накладной для подтверждения: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    { PrintWarn("Неверный ID"); return; }

    await svc.ConfirmInvoiceAsync(id, ct);
    PrintSuccess($"Накладная #{id} подтверждена.");
}

static async Task MenuDeleteInvoiceAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  ID накладной для удаления: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    { PrintWarn("Неверный ID"); return; }

    Console.Write($"  Удалить накладную #{id}? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "y")
    { PrintWarn("Отменено."); return; }

    await svc.DeleteInvoiceAsync(id, ct);
    PrintSuccess($"Накладная #{id} удалена.");
}

static async Task MenuShowReceiptsAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  Фильтр по ID накладной (Enter = все): ");
    var raw = Console.ReadLine()?.Trim();
    int? invoiceId = int.TryParse(raw, out int v) ? v : null;

    var result = await svc.GetReceiptsAsync(invoiceId, ct);
    PrintHeader($"Квитанции (всего: {result.TotalCount})");

    Console.WriteLine($"  {"ID",-5} {"Накл.",-7} {"Дата",-12} {"Получил",-25} {"Сумма",12}  Примечания");
    Console.WriteLine(new string('─', 80));
    foreach (var r in result.Items)
        Console.WriteLine($"  {r.Id,-5} {r.InvoiceId,-7} {r.ReceivedDate:dd.MM.yyyy}  " +
                          $"{r.ReceivedBy,-25} {r.Amount,12:N2}  {r.Notes ?? "—"}");
}

static async Task MenuCreateReceiptAsync(InvoiceService svc, CancellationToken ct)
{
    Console.Write("  ID накладной: ");
    int.TryParse(Console.ReadLine(), out int invoiceId);

    Console.Write("  Дата получения (dd.MM.yyyy, Enter = сегодня): ");
    var dateStr = Console.ReadLine()?.Trim();
    var date = string.IsNullOrWhiteSpace(dateStr)
        ? DateTime.Today
        : DateTime.ParseExact(dateStr, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture);

    Console.Write("  Кто принял: ");
    var receivedBy = Console.ReadLine()?.Trim() ?? "";

    Console.Write("  Сумма: ");
    decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out decimal amount);

    Console.Write("  Примечание (Enter = нет): ");
    var notes = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(notes)) notes = null;

    var created = await svc.CreateReceiptAsync(
        new CreateReceiptRequest(invoiceId, date, receivedBy, amount, notes), ct);

    PrintSuccess($"Квитанция создана: ID={created.Id}, сумма={created.Amount:N2} ₽");
}

static async Task MenuShowSuppliersAsync(InvoiceService svc, CancellationToken ct)
{
    var suppliers = (await svc.GetSuppliersAsync(ct)).ToList();
    PrintHeader($"Поставщики ({suppliers.Count})");

    Console.WriteLine($"  {"ID",-5} {"Название",-30} {"Контакт",-25} {"Телефон",-18} Адрес");
    Console.WriteLine(new string('─', 100));
    foreach (var s in suppliers)
        Console.WriteLine($"  {s.Id,-5} {s.Name,-30} {s.ContactPerson,-25} {s.Phone,-18} {s.Address}");
}

static async Task MenuBulkCreateAsync(InvoiceService svc, CancellationToken ct)
{
    // Получаем список поставщиков, чтобы подставить корректный SupplierId
    var suppliers = (await svc.GetSuppliersAsync(ct)).ToList();
    if (suppliers.Count == 0)
    {
        PrintWarn("Нет поставщиков. Создайте хотя бы одного через API.");
        return;
    }

    Console.Write("  Сколько накладных создать? (2-20): ");
    if (!int.TryParse(Console.ReadLine(), out int count) || count < 2 || count > 20)
    { PrintWarn("Некорректное число. Создаю 5."); count = 5; }

    var requests = Enumerable.Range(1, count).Select(i => new CreateInvoiceRequest(
        Number:      $"BULK-{DateTime.Now:yyyyMMdd}-{i:D3}",
        Date:        DateTime.Today.AddDays(-i),
        SupplierId:  suppliers[i % suppliers.Count].Id,
        TotalAmount: 10_000m * i
    )).ToList();

    PrintHeader($"Массовое создание {count} накладных");
    var progress = new ColoredProgressReporter(count);
    var results  = await svc.BulkCreateAsync(requests, progress, ct);
    PrintSuccess($"\n  Создано {results.Count} накладных.");
}

static async Task MenuExportCsvAsync(InvoiceService svc, ExportService exp, CancellationToken ct)
{
    PrintInfo("  Получаем данные...");
    var invoices = await svc.GetAllForExportAsync(ct);
    await exp.ExportToCsvAsync(invoices);
    PrintSuccess($"  CSV сохранён: {Path.GetFullPath("invoices_export.csv")}");
}

static async Task MenuExportExcelAsync(InvoiceService svc, ExportService exp, CancellationToken ct)
{
    PrintInfo("  Получаем данные...");
    var invoices = await svc.GetAllForExportAsync(ct);
    await exp.ExportToExcelAsync(invoices);
    PrintSuccess($"  Excel сохранён: {Path.GetFullPath("invoices_export.xlsx")}");
}

static async Task MenuExportPdfAsync(InvoiceService svc, ExportService exp, CancellationToken ct)
{
    PrintInfo("  Получаем данные...");
    var invoices = await svc.GetAllForExportAsync(ct);
    await exp.ExportToPdfAsync(invoices);
    PrintSuccess($"  PDF сохранён: {Path.GetFullPath("invoices_export.pdf")}");
}

// ═══════════════════════════════════════════════════════════════════════════
// ВСПОМОГАТЕЛЬНЫЕ: ВЫВОД В КОНСОЛЬ
// ═══════════════════════════════════════════════════════════════════════════

static void PrintMenu()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("  ╔═══════════════════════════════════════╗");
    Console.WriteLine("  ║    Складской учёт — Клиент API        ║");
    Console.WriteLine("  ╚═══════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine("   1. Показать накладные (пагинация)");
    Console.WriteLine("   2. Показать накладную по ID");
    Console.WriteLine("   3. Создать накладную");
    Console.WriteLine("   4. Подтвердить накладную");
    Console.WriteLine("   5. Удалить накладную");
    Console.WriteLine("   6. Показать квитанции");
    Console.WriteLine("   7. Создать квитанцию");
    Console.WriteLine("   8. Показать поставщиков");
    Console.WriteLine("   9. Массовое создание накладных");
    Console.WriteLine("  10. Экспорт в CSV");
    Console.WriteLine("  11. Экспорт в Excel");
    Console.WriteLine("  12. Экспорт в PDF");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("   0. Выход");
    Console.ResetColor();
    Console.Write("\n  Выберите пункт: ");
}

static void PrintHeader(string title)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine($"\n  ── {title} ──");
    Console.ResetColor();
}

static void PrintInvoiceHeader()
{
    Console.WriteLine($"  {"ID",-5} {"Номер",-18} {"Дата",-12} {"Поставщик",-22} {"Сумма",12}  Статус");
    Console.WriteLine(new string('─', 85));
}

static void PrintInvoiceRow(InvoiceResponse inv)
{
    var statusColor = inv.Status switch
    {
        "Confirmed" => ConsoleColor.Green,
        "Cancelled" => ConsoleColor.Red,
        _           => ConsoleColor.Yellow
    };
    var statusRu = inv.Status switch
    {
        "Confirmed" => "Подтверждена",
        "Cancelled" => "Отменена",
        _           => "Черновик"
    };

    Console.Write($"  {inv.Id,-5} {inv.Number,-18} {inv.Date:dd.MM.yyyy}  " +
                  $"{inv.SupplierName,-22} {inv.TotalAmount,12:N2}  ");
    Console.ForegroundColor = statusColor;
    Console.WriteLine(statusRu);
    Console.ResetColor();
}

static void PrintSuccess(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n  ✓ {msg}");
    Console.ResetColor();
}

static void PrintError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  ✗ {msg}");
    Console.ResetColor();
}

static void PrintWarn(string msg)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"  ! {msg}");
    Console.ResetColor();
}

static void PrintInfo(string msg)
{
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine(msg);
    Console.ResetColor();
}
