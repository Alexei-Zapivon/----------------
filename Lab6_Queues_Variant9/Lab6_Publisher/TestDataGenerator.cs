using Lab6Shared.Models;

namespace Lab6Publisher;

// 10 тестовых накладных — поставщики и данные берём из Lab1/Lab4
public static class TestDataGenerator
{
    private static readonly (string Name, string[] Contacts)[] Suppliers =
    [
        ("ООО «ТехноСнаб»",    ["Иванов Пётр", "г. Москва"]),
        ("ЗАО «МегаПоставка»", ["Сидорова Анна", "г. Санкт-Петербург"]),
        ("ИП Кузнецов А.Н.",   ["Кузнецов Алексей", "г. Новосибирск"])
    ];

    public static InvoiceMessage[] GenerateInvoices(int count = 10) =>
        Enumerable.Range(1, count).Select(i =>
        {
            var s = Suppliers[i % Suppliers.Length];
            return new InvoiceMessage
            {
                Id           = i,
                Number       = $"НК-2024-{i:D3}",
                Date         = DateTime.Today.AddDays(-i),
                SupplierName = s.Name,
                TotalAmount  = 50_000m * i,
                Status       = i % 3 == 0 ? "Confirmed"
                             : i % 5 == 0 ? "Cancelled"
                             : "Draft"
            };
        }).ToArray();
}
