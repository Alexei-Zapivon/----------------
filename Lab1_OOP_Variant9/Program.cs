using Lab1_OOP_Variant9.Models;
using Lab1_OOP_Variant9.Services;

var service = new DocumentService();
service.Seed(); // загружаем 15 тестовых записей

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool running = true;
while (running)
{
    Console.WriteLine("\n==========================================");
    Console.WriteLine("   Lab1_OOP_Variant9 — Документы (ООП)");
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
        case "1": ShowAll(); break;
        case "2": AddReceipt(); break;
        case "3": AddInvoice(); break;
        case "4": AddBill(); break;
        case "5": FindById(); break;
        case "6": DeleteById(); break;
        case "7": FilterByAmount(); break;
        case "8": FilterByType(); break;
        case "0": running = false; break;
        default: Console.WriteLine("Неверный выбор."); break;
    }
}

// ─── Методы меню ───────────────────────────────────────────

void ShowAll()
{
    var docs = service.GetAll();
    Console.WriteLine($"\n--- Всего документов: {docs.Count} ---");
    foreach (var d in docs)
    {
        Console.WriteLine(d.GetInfo()); // ПОЛИМОРФИЗМ: вызывается нужный GetInfo()
        Console.WriteLine();
    }
}

void AddReceipt()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Плательщик: "); var payer = Console.ReadLine() ?? "";
    Console.Write("Получатель: "); var receiver = Console.ReadLine() ?? "";
    Console.Write("Способ оплаты (Карта/Наличные/Перевод): "); var method = Console.ReadLine() ?? "";
    Console.Write("Оплачено? (y/n): "); bool paid = Console.ReadLine()?.ToLower() == "y";

    service.Add(new Receipt
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        PayerName = payer, ReceiverName = receiver, PaymentMethod = method,
        IsPaid = paid, PaidAt = paid ? DateTime.Now : null
    });
    Console.WriteLine("Квитанция добавлена.");
}

void AddInvoice()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Поставщик: "); var supplier = Console.ReadLine() ?? "";
    Console.Write("Покупатель: "); var buyer = Console.ReadLine() ?? "";
    Console.Write("Кол-во позиций: "); int.TryParse(Console.ReadLine(), out var items);
    Console.Write("Адрес доставки: "); var address = Console.ReadLine() ?? "";

    service.Add(new Invoice
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        SupplierName = supplier, BuyerName = buyer,
        DeliveryDate = DateTime.Now.AddDays(3), ItemsCount = items, ShippingAddress = address
    });
    Console.WriteLine("Накладная добавлена.");
}

void AddBill()
{
    Console.Write("Название: "); var title = Console.ReadLine() ?? "";
    Console.Write("Сумма: "); decimal.TryParse(Console.ReadLine(), out var amount);
    Console.Write("Описание: "); var desc = Console.ReadLine() ?? "";
    Console.Write("Клиент: "); var customer = Console.ReadLine() ?? "";
    Console.Write("Банковский счёт: "); var bank = Console.ReadLine() ?? "";

    service.Add(new Bill
    {
        Title = title, Amount = amount, Description = desc, CreatedAt = DateTime.Now,
        CustomerName = customer, DueDate = DateTime.Now.AddDays(30),
        TaxAmount = Math.Round(amount * 0.20m, 2), IsOverdue = false, BankAccount = bank
    });
    Console.WriteLine("Счёт добавлен.");
}

void FindById()
{
    Console.Write("Введите Id: ");
    if (int.TryParse(Console.ReadLine(), out var id))
    {
        var doc = service.GetById(id);
        if (doc != null) { Console.WriteLine(); Console.WriteLine(doc.GetInfo()); }
        else Console.WriteLine("Документ не найден.");
    }
}

void DeleteById()
{
    Console.Write("Введите Id для удаления: ");
    if (int.TryParse(Console.ReadLine(), out var id))
        Console.WriteLine(service.Delete(id) ? "Удалено." : "Документ не найден.");
}

void FilterByAmount()
{
    Console.Write("Минимальная сумма: ");
    if (decimal.TryParse(Console.ReadLine(), out var amount))
    {
        var docs = service.GetByAmountGreaterThan(amount);
        Console.WriteLine($"\nНайдено: {docs.Count}");
        foreach (var d in docs) { Console.WriteLine(d.GetInfo()); Console.WriteLine(); }
    }
}

void FilterByType()
{
    Console.WriteLine("Типы: 1-Квитанция, 2-Накладная, 3-Счёт");
    Console.Write("Выберите тип: ");
    var docs = Console.ReadLine() switch
    {
        "1" => service.GetByType<Receipt>(),
        "2" => service.GetByType<Invoice>(),
        "3" => service.GetByType<Bill>(),
        _   => new List<Document>()
    };
    Console.WriteLine($"\nНайдено: {docs.Count}");
    foreach (var d in docs) { Console.WriteLine(d.GetInfo()); Console.WriteLine(); }
}
