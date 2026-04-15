using Lab2_EF_Variant9.Data;
using Lab2_EF_Variant9.Models;
using Lab2_EF_Variant9.UnitOfWork;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Создаём БД и заполняем тестовыми данными
await using var initCtx = new AppDbContext();
await initCtx.Database.EnsureCreatedAsync();
await DbInitializer.SeedAsync(initCtx);

bool running = true;
while (running)
{
    Console.WriteLine("\n==========================================");
    Console.WriteLine("   Lab2_EF_Variant9 — EF Core + SQLite");
    Console.WriteLine("==========================================");
    Console.WriteLine("1. Показать все документы");
    Console.WriteLine("2. Добавить квитанцию");
    Console.WriteLine("3. Добавить накладную");
    Console.WriteLine("4. Добавить счёт");
    Console.WriteLine("5. Найти по Id");
    Console.WriteLine("6. Удалить по Id");
    Console.WriteLine("7. Фильтр по сумме (больше X)");
    Console.WriteLine("8. Фильтр по типу");
    Console.WriteLine("0. Выход");
    Console.Write("\nВыберите пункт: ");

    switch (Console.ReadLine())
    {
        case "1": await ShowAll(); break;
        case "2": await AddReceipt(); break;
        case "3": await AddInvoice(); break;
        case "4": await AddBill(); break;
        case "5": await FindById(); break;
        case "6": await DeleteById(); break;
        case "7": await FilterByAmount(); break;
        case "8": await FilterByType(); break;
        case "0": running = false; break;
        default: Console.WriteLine("Неверный выбор."); break;
    }
}

// ─── Методы меню ───────────────────────────────────────────

async Task ShowAll()
{
    await using var uow = new UnitOfWork(new AppDbContext());
    var docs = await uow.Documents.GetAllAsync();
    Console.WriteLine($"\n--- Всего документов: {docs.Count} ---");
    foreach (var d in docs) Console.WriteLine(d.GetInfo());
}

async Task AddReceipt()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Плательщик: "); var payer = Console.ReadLine() ?? "";
    Console.Write("Получатель: "); var receiver = Console.ReadLine() ?? "";
    Console.Write("Способ оплаты: "); var method = Console.ReadLine() ?? "";
    Console.Write("Оплачено? (y/n): "); bool paid = Console.ReadLine()?.ToLower() == "y";

    await using var uow = new UnitOfWork(new AppDbContext());
    await uow.Documents.AddAsync(new Receipt
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        PayerName = payer, ReceiverName = receiver, PaymentMethod = method,
        IsPaid = paid, PaidAt = paid ? DateTime.Now : null
    });
    await uow.SaveAsync();
    Console.WriteLine("Квитанция добавлена.");
}

async Task AddInvoice()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Поставщик: "); var supplier = Console.ReadLine() ?? "";
    Console.Write("Покупатель: "); var buyer = Console.ReadLine() ?? "";
    Console.Write("Кол-во позиций: "); int.TryParse(Console.ReadLine(), out var items);
    Console.Write("Адрес доставки: "); var address = Console.ReadLine() ?? "";

    await using var uow = new UnitOfWork(new AppDbContext());
    await uow.Documents.AddAsync(new Invoice
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        SupplierName = supplier, BuyerName = buyer,
        DeliveryDate = DateTime.Now.AddDays(3), ItemsCount = items, ShippingAddress = address
    });
    await uow.SaveAsync();
    Console.WriteLine("Накладная добавлена.");
}

async Task AddBill()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Клиент: "); var customer = Console.ReadLine() ?? "";
    Console.Write("Банковский счёт: "); var bank = Console.ReadLine() ?? "";

    await using var uow = new UnitOfWork(new AppDbContext());
    await uow.Documents.AddAsync(new Bill
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        CustomerName = customer, DueDate = DateTime.Now.AddDays(30),
        TaxAmount = Math.Round(amount * 0.20m, 2), IsOverdue = false, BankAccount = bank
    });
    await uow.SaveAsync();
    Console.WriteLine("Счёт добавлен.");
}

async Task FindById()
{
    Console.Write("Введите Id: ");
    if (int.TryParse(Console.ReadLine(), out var id))
    {
        await using var uow = new UnitOfWork(new AppDbContext());
        var doc = await uow.Documents.GetByIdAsync(id);
        if (doc != null) Console.WriteLine("\n" + doc.GetInfo());
        else Console.WriteLine("Документ не найден.");
    }
}

async Task DeleteById()
{
    Console.Write("Введите Id для удаления: ");
    if (int.TryParse(Console.ReadLine(), out var id))
    {
        await using var uow = new UnitOfWork(new AppDbContext());
        var doc = await uow.Documents.GetByIdAsync(id);
        if (doc == null) { Console.WriteLine("Не найдено."); return; }
        uow.Documents.Delete(doc);
        await uow.SaveAsync();
        Console.WriteLine("Удалено.");
    }
}

async Task FilterByAmount()
{
    Console.Write("Минимальная сумма: ");
    if (decimal.TryParse(Console.ReadLine(), out var amount))
    {
        await using var uow = new UnitOfWork(new AppDbContext());
        var docs = await uow.Documents.GetByAmountGreaterThanAsync(amount);
        Console.WriteLine($"\nНайдено: {docs.Count}");
        foreach (var d in docs) Console.WriteLine(d.GetInfo());
    }
}

async Task FilterByType()
{
    Console.WriteLine("Типы: квитанция, накладная, счёт");
    Console.Write("Введите тип: ");
    var type = Console.ReadLine() ?? "";
    await using var uow = new UnitOfWork(new AppDbContext());
    var docs = await uow.Documents.GetByTypeAsync(type);
    Console.WriteLine($"\nНайдено: {docs.Count}");
    foreach (var d in docs) Console.WriteLine(d.GetInfo());
}
