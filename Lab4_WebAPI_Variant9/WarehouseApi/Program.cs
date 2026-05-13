using FluentValidation;
using QuestPDF.Infrastructure;
using WarehouseApi.DTOs;
using WarehouseApi.Middleware;
using WarehouseApi.Models;
using WarehouseApi.Repositories;
using WarehouseApi.Reports;
using WarehouseApi.Validators;

// Лицензия QuestPDF для некоммерческого/учебного использования
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Регистрация сервисов
// ---------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Складской учёт API",
        Version = "v1",
        Description = "REST API для управления накладными, квитанциями и поставщиками. Вариант 9."
    });
});

// Хранилище данных в памяти — синглтон
builder.Services.AddSingleton<DataStore>();

// Репозитории — Scoped (новый экземпляр на каждый HTTP-запрос)
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

// Генератор PDF-отчётов — Transient
builder.Services.AddTransient<IReportGenerator, InvoiceReportGenerator>();

// FluentValidation — автоматическая регистрация всех валидаторов из текущей сборки
builder.Services.AddValidatorsFromAssemblyContaining<CreateInvoiceRequestValidator>();

var app = builder.Build();

// ---------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------

// Глобальный обработчик исключений — должен быть первым в pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Наполнение хранилища тестовыми данными при старте
SeedData(app.Services);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Складской учёт API v1");
    options.RoutePrefix = string.Empty; // Swagger UI доступен на корне /
    options.DocumentTitle = "Складской учёт API";
});

// ---------------------------------------------------------------
// Маппинг эндпоинтов
// ---------------------------------------------------------------

MapInvoiceEndpoints(app);
MapReceiptEndpoints(app);
MapSupplierEndpoints(app);
MapReportEndpoints(app);

app.Run();

// ===============================================================
// ЭНДПОИНТЫ: НАКЛАДНЫЕ  /api/invoices
// ===============================================================
static void MapInvoiceEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/invoices").WithTags("Накладные");

    // GET /api/invoices — список с пагинацией и фильтрацией
    group.MapGet("/", (
        IInvoiceRepository invoiceRepo,
        ISupplierRepository supplierRepo,
        int page = 1,
        int pageSize = 10,
        string? status = null,
        int? supplierId = null,
        string? number = null) =>
    {
        var paged = invoiceRepo.GetAll(page, pageSize, status, supplierId, number);
        var supplierNames = supplierRepo.GetAll().ToDictionary(s => s.Id, s => s.Name);

        var items = paged.Items.Select(i => new InvoiceResponse(
            i.Id, i.Number, i.Date, i.SupplierId,
            supplierNames.GetValueOrDefault(i.SupplierId, "—"),
            i.TotalAmount, i.Status.ToString()
        )).ToList();

        return Results.Ok(new PagedResponse<InvoiceResponse>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        });
    })
    .WithSummary("Список накладных")
    .WithDescription("Возвращает страницу накладных. Параметры: page, pageSize, status (Draft/Confirmed/Cancelled), supplierId, number (поиск по номеру).")
    .Produces<PagedResponse<InvoiceResponse>>()
    .WithOpenApi();

    // GET /api/invoices/{id}
    group.MapGet("/{id:int}", (int id, IInvoiceRepository invoiceRepo, ISupplierRepository supplierRepo) =>
    {
        var invoice = invoiceRepo.GetById(id);
        var supplier = supplierRepo.TryGetById(invoice.SupplierId);

        return Results.Ok(new InvoiceResponse(
            invoice.Id, invoice.Number, invoice.Date, invoice.SupplierId,
            supplier?.Name ?? "—", invoice.TotalAmount, invoice.Status.ToString()
        ));
    })
    .WithSummary("Получить накладную по ID")
    .Produces<InvoiceResponse>()
    .Produces(404)
    .WithOpenApi();

    // POST /api/invoices
    group.MapPost("/", async (
        CreateInvoiceRequest request,
        IInvoiceRepository invoiceRepo,
        ISupplierRepository supplierRepo,
        IValidator<CreateInvoiceRequest> validator) =>
    {
        await validator.ValidateAndThrowAsync(request);

        var supplier = supplierRepo.TryGetById(request.SupplierId)
            ?? throw new KeyNotFoundException($"Поставщик с ID {request.SupplierId} не найден");

        var invoice = new Invoice
        {
            Number = request.Number,
            Date = request.Date,
            SupplierId = request.SupplierId,
            TotalAmount = request.TotalAmount,
            Status = InvoiceStatus.Draft
        };

        var created = invoiceRepo.Create(invoice);

        return Results.Created($"/api/invoices/{created.Id}", new InvoiceResponse(
            created.Id, created.Number, created.Date, created.SupplierId,
            supplier.Name, created.TotalAmount, created.Status.ToString()
        ));
    })
    .WithSummary("Создать накладную")
    .WithDescription("Создаёт новую накладную со статусом Draft. SupplierId должен существовать.")
    .Produces<InvoiceResponse>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

    // PUT /api/invoices/{id}
    group.MapPut("/{id:int}", (
        int id,
        UpdateInvoiceRequest request,
        IInvoiceRepository invoiceRepo,
        ISupplierRepository supplierRepo) =>
    {
        var supplier = supplierRepo.TryGetById(request.SupplierId)
            ?? throw new KeyNotFoundException($"Поставщик с ID {request.SupplierId} не найден");

        if (!Enum.TryParse<InvoiceStatus>(request.Status, true, out var newStatus))
            throw new InvalidOperationException($"Недопустимый статус: '{request.Status}'. Допустимые: Draft, Confirmed, Cancelled");

        var updated = invoiceRepo.Update(id, new Invoice
        {
            Number = request.Number,
            Date = request.Date,
            SupplierId = request.SupplierId,
            TotalAmount = request.TotalAmount,
            Status = newStatus
        });

        return Results.Ok(new InvoiceResponse(
            updated.Id, updated.Number, updated.Date, updated.SupplierId,
            supplier.Name, updated.TotalAmount, updated.Status.ToString()
        ));
    })
    .WithSummary("Обновить накладную")
    .Produces<InvoiceResponse>()
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

    // PATCH /api/invoices/{id}/confirm — подтвердить накладную
    group.MapMethods("/{id:int}/confirm", ["PATCH"], (int id, IInvoiceRepository invoiceRepo) =>
    {
        var invoice = invoiceRepo.GetById(id);

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Нельзя подтвердить отменённую накладную");

        if (invoice.Status == InvoiceStatus.Confirmed)
            throw new InvalidOperationException("Накладная уже подтверждена");

        // Invoice — ссылочный тип, прямое изменение отражается в DataStore
        invoice.Status = InvoiceStatus.Confirmed;

        return Results.Ok(new { message = "Накладная успешно подтверждена", id = invoice.Id, status = "Confirmed" });
    })
    .WithSummary("Подтвердить накладную")
    .WithDescription("Переводит накладную из статуса Draft в Confirmed. Нельзя применить к Cancelled.")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict)
    .WithOpenApi();

    // DELETE /api/invoices/{id}
    group.MapDelete("/{id:int}", (int id, IInvoiceRepository invoiceRepo) =>
    {
        invoiceRepo.Delete(id);
        return Results.NoContent();
    })
    .WithSummary("Удалить накладную")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();
}

// ===============================================================
// ЭНДПОИНТЫ: КВИТАНЦИИ  /api/receipts
// ===============================================================
static void MapReceiptEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/receipts").WithTags("Квитанции");

    // GET /api/receipts
    group.MapGet("/", (
        IReceiptRepository receiptRepo,
        int page = 1,
        int pageSize = 10,
        int? invoiceId = null) =>
    {
        var paged = receiptRepo.GetAll(page, pageSize, invoiceId);

        var items = paged.Items.Select(r => new ReceiptResponse(
            r.Id, r.InvoiceId, r.ReceivedDate, r.ReceivedBy, r.Amount, r.Notes
        )).ToList();

        return Results.Ok(new PagedResponse<ReceiptResponse>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        });
    })
    .WithSummary("Список квитанций")
    .WithDescription("Возвращает страницу квитанций. Параметры: page, pageSize, invoiceId (фильтр по накладной).")
    .Produces<PagedResponse<ReceiptResponse>>()
    .WithOpenApi();

    // GET /api/receipts/{id}
    group.MapGet("/{id:int}", (int id, IReceiptRepository receiptRepo) =>
    {
        var receipt = receiptRepo.GetById(id);
        return Results.Ok(new ReceiptResponse(
            receipt.Id, receipt.InvoiceId, receipt.ReceivedDate,
            receipt.ReceivedBy, receipt.Amount, receipt.Notes
        ));
    })
    .WithSummary("Получить квитанцию по ID")
    .Produces<ReceiptResponse>()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

    // POST /api/receipts
    group.MapPost("/", async (
        CreateReceiptRequest request,
        IReceiptRepository receiptRepo,
        IInvoiceRepository invoiceRepo,
        IValidator<CreateReceiptRequest> validator) =>
    {
        await validator.ValidateAndThrowAsync(request);

        // Проверяем, что накладная существует (бросает 404 если нет)
        invoiceRepo.GetById(request.InvoiceId);

        var receipt = new Receipt
        {
            InvoiceId = request.InvoiceId,
            ReceivedDate = request.ReceivedDate,
            ReceivedBy = request.ReceivedBy,
            Amount = request.Amount,
            Notes = request.Notes
        };

        var created = receiptRepo.Create(receipt);

        return Results.Created($"/api/receipts/{created.Id}", new ReceiptResponse(
            created.Id, created.InvoiceId, created.ReceivedDate,
            created.ReceivedBy, created.Amount, created.Notes
        ));
    })
    .WithSummary("Создать квитанцию")
    .WithDescription("Создаёт квитанцию о получении товара. InvoiceId должен ссылаться на существующую накладную.")
    .Produces<ReceiptResponse>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

    // DELETE /api/receipts/{id}
    group.MapDelete("/{id:int}", (int id, IReceiptRepository receiptRepo) =>
    {
        receiptRepo.Delete(id);
        return Results.NoContent();
    })
    .WithSummary("Удалить квитанцию")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();
}

// ===============================================================
// ЭНДПОИНТЫ: ПОСТАВЩИКИ  /api/suppliers
// ===============================================================
static void MapSupplierEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/suppliers").WithTags("Поставщики");

    // GET /api/suppliers
    group.MapGet("/", (ISupplierRepository supplierRepo) =>
    {
        var suppliers = supplierRepo.GetAll()
            .Select(s => new SupplierResponse(s.Id, s.Name, s.ContactPerson, s.Phone, s.Address))
            .ToList();

        return Results.Ok(suppliers);
    })
    .WithSummary("Список всех поставщиков")
    .Produces<List<SupplierResponse>>()
    .WithOpenApi();

    // POST /api/suppliers
    group.MapPost("/", (CreateSupplierRequest request, ISupplierRepository supplierRepo) =>
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Address = request.Address
        };

        var created = supplierRepo.Create(supplier);

        return Results.Created($"/api/suppliers/{created.Id}",
            new SupplierResponse(created.Id, created.Name, created.ContactPerson, created.Phone, created.Address));
    })
    .WithSummary("Добавить поставщика")
    .Produces<SupplierResponse>(StatusCodes.Status201Created)
    .WithOpenApi();
}

// ===============================================================
// ЭНДПОИНТЫ: ОТЧЁТЫ  /api/reports
// ===============================================================
static void MapReportEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/reports").WithTags("Отчёты");

    // POST /api/reports/invoices — генерация PDF
    group.MapPost("/invoices", (
        GenerateInvoiceReportRequest request,
        IInvoiceRepository invoiceRepo,
        ISupplierRepository supplierRepo,
        IReportGenerator reportGenerator) =>
    {
        var invoices = invoiceRepo.GetFiltered(request.DateFrom, request.DateTo, request.Status);
        var suppliers = supplierRepo.GetAll();
        var pdfBytes = reportGenerator.Generate(invoices, suppliers);

        var fileName = $"report-invoices-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    })
    .WithSummary("Генерация PDF-отчёта по накладным")
    .WithDescription("Создаёт PDF-отчёт с таблицей накладных и итоговой строкой. Поддерживает фильтрацию по dateFrom, dateTo, status.")
    .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf")
    .WithOpenApi();
}

// ===============================================================
// ТЕСТОВЫЕ ДАННЫЕ
// ===============================================================
static void SeedData(IServiceProvider services)
{
    var store = services.GetRequiredService<DataStore>();

    // 3 поставщика
    store.Suppliers.AddRange([
        new Supplier
        {
            Id = store.NextSupplierId(),
            Name = "ООО «ТехноСнаб»",
            ContactPerson = "Иванов Пётр Сергеевич",
            Phone = "+7-495-123-45-67",
            Address = "г. Москва, ул. Промышленная, 12"
        },
        new Supplier
        {
            Id = store.NextSupplierId(),
            Name = "ЗАО «МегаПоставка»",
            ContactPerson = "Сидорова Анна Викторовна",
            Phone = "+7-812-987-65-43",
            Address = "г. Санкт-Петербург, пр. Невский, 100"
        },
        new Supplier
        {
            Id = store.NextSupplierId(),
            Name = "ИП Кузнецов А.Н.",
            ContactPerson = "Кузнецов Алексей Николаевич",
            Phone = "+7-383-456-78-90",
            Address = "г. Новосибирск, ул. Ленина, 5"
        }
    ]);

    // 8 накладных с разными статусами и поставщиками
    store.Invoices.AddRange([
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-001", Date = DateTime.Today.AddDays(-30), SupplierId = 1, TotalAmount = 150_000.00m, Status = InvoiceStatus.Confirmed },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-002", Date = DateTime.Today.AddDays(-25), SupplierId = 2, TotalAmount =  87_500.50m, Status = InvoiceStatus.Confirmed },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-003", Date = DateTime.Today.AddDays(-20), SupplierId = 1, TotalAmount = 320_000.00m, Status = InvoiceStatus.Draft     },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-004", Date = DateTime.Today.AddDays(-15), SupplierId = 3, TotalAmount =  45_000.75m, Status = InvoiceStatus.Cancelled  },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-005", Date = DateTime.Today.AddDays(-10), SupplierId = 2, TotalAmount = 210_000.00m, Status = InvoiceStatus.Confirmed },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-006", Date = DateTime.Today.AddDays(-7),  SupplierId = 3, TotalAmount =  95_000.00m, Status = InvoiceStatus.Draft     },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-007", Date = DateTime.Today.AddDays(-5),  SupplierId = 1, TotalAmount = 180_000.00m, Status = InvoiceStatus.Draft     },
        new Invoice { Id = store.NextInvoiceId(), Number = "НК-2024-008", Date = DateTime.Today.AddDays(-3),  SupplierId = 2, TotalAmount =  67_000.00m, Status = InvoiceStatus.Draft     }
    ]);

    // 4 квитанции по подтверждённым накладным
    store.Receipts.AddRange([
        new Receipt { Id = store.NextReceiptId(), InvoiceId = 1, ReceivedDate = DateTime.Today.AddDays(-28), ReceivedBy = "Смирнов Дмитрий Александрович", Amount = 150_000.00m, Notes = "Полная оплата" },
        new Receipt { Id = store.NextReceiptId(), InvoiceId = 2, ReceivedDate = DateTime.Today.AddDays(-23), ReceivedBy = "Козлова Мария Ивановна",         Amount =  50_000.00m, Notes = "Частичная оплата (1/2)" },
        new Receipt { Id = store.NextReceiptId(), InvoiceId = 2, ReceivedDate = DateTime.Today.AddDays(-18), ReceivedBy = "Козлова Мария Ивановна",         Amount =  37_500.50m, Notes = "Доплата, закрытие счёта" },
        new Receipt { Id = store.NextReceiptId(), InvoiceId = 5, ReceivedDate = DateTime.Today.AddDays(-8),  ReceivedBy = "Петров Николай Сергеевич",       Amount = 210_000.00m, Notes = null }
    ]);
}
