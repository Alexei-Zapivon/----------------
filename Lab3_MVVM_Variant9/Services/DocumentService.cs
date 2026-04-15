using Lab3_MVVM_Variant9.Models;

namespace Lab3_MVVM_Variant9.Services;

// Сервис с мок-данными (без БД)
public class DocumentService
{
    private readonly List<Document> _documents = new();
    private int _nextId = 1;

    public DocumentService() => Seed();

    public List<Document> GetAll() => _documents.ToList();

    public void Add(Document doc)
    {
        doc.Id = _nextId++;
        _documents.Add(doc);
    }

    public bool Delete(int id)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == id);
        if (doc == null) return false;
        _documents.Remove(doc);
        return true;
    }

    public List<Document> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return GetAll();
        query = query.ToLower();
        return _documents
            .Where(d => d.Title.ToLower().Contains(query) || d.Description.ToLower().Contains(query))
            .ToList();
    }

    private void Seed()
    {
        var docs = new List<Document>
        {
            new Receipt { Title = "Квитанция #1",  CreatedAt = DateTime.Now.AddDays(-10), Amount = 1500,  Description = "Оплата услуг",        PayerName = "Иванов И.И.",  ReceiverName = "ООО Сервис",   PaymentMethod = "Карта",    IsPaid = true,  PaidAt = DateTime.Now.AddDays(-9) },
            new Receipt { Title = "Квитанция #2",  CreatedAt = DateTime.Now.AddDays(-8),  Amount = 2300,  Description = "Покупка товаров",      PayerName = "Петров П.П.", ReceiverName = "ИП Магазин",  PaymentMethod = "Наличные", IsPaid = true,  PaidAt = DateTime.Now.AddDays(-8) },
            new Receipt { Title = "Квитанция #3",  CreatedAt = DateTime.Now.AddDays(-5),  Amount = 800,   Description = "Аренда",               PayerName = "Сидоров С.С.",ReceiverName = "ООО Аренда",  PaymentMethod = "Перевод",  IsPaid = false },
            new Receipt { Title = "Квитанция #4",  CreatedAt = DateTime.Now.AddDays(-3),  Amount = 5000,  Description = "Ремонт",               PayerName = "Козлов К.К.", ReceiverName = "ИП Мастер",   PaymentMethod = "Карта",    IsPaid = true,  PaidAt = DateTime.Now.AddDays(-2) },
            new Receipt { Title = "Квитанция #5",  CreatedAt = DateTime.Now.AddDays(-1),  Amount = 1200,  Description = "Коммунальные услуги",  PayerName = "Новиков Н.Н.",ReceiverName = "ЖКХ Сервис", PaymentMethod = "Онлайн",   IsPaid = false },
            new Invoice { Title = "Накладная #1",  CreatedAt = DateTime.Now.AddDays(-12), Amount = 15000, Description = "Поставка оборудования",SupplierName = "ООО Поставщик",BuyerName = "ЗАО Покупатель",  DeliveryDate = DateTime.Now.AddDays(-10), ItemsCount = 10,  ShippingAddress = "г. Москва, ул. Ленина, 1" },
            new Invoice { Title = "Накладная #2",  CreatedAt = DateTime.Now.AddDays(-9),  Amount = 8500,  Description = "Поставка материалов",  SupplierName = "ИП Снабженец", BuyerName = "ООО Производство",DeliveryDate = DateTime.Now.AddDays(-7),  ItemsCount = 25,  ShippingAddress = "г. СПб, пр. Невский, 5" },
            new Invoice { Title = "Накладная #3",  CreatedAt = DateTime.Now.AddDays(-6),  Amount = 22000, Description = "Поставка комплектующих",SupplierName = "ООО ТехСнаб",BuyerName = "АО Завод",         DeliveryDate = DateTime.Now.AddDays(-4),  ItemsCount = 50,  ShippingAddress = "г. Казань, ул. Пушкина, 10" },
            new Invoice { Title = "Накладная #4",  CreatedAt = DateTime.Now.AddDays(-4),  Amount = 3200,  Description = "Поставка канцтоваров", SupplierName = "ООО Офис",    BuyerName = "ИП Малый",         DeliveryDate = DateTime.Now.AddDays(-3),  ItemsCount = 100, ShippingAddress = "г. Новосибирск, ул. Мира, 7" },
            new Invoice { Title = "Накладная #5",  CreatedAt = DateTime.Now.AddDays(-2),  Amount = 47000, Description = "Поставка электроники", SupplierName = "АО ЭлекТех", BuyerName = "ООО Ретейл",       DeliveryDate = DateTime.Now.AddDays(2),   ItemsCount = 15,  ShippingAddress = "г. Екатеринбург, ул. Гагарина, 3" },
            new Bill   { Title = "Счёт #1",        CreatedAt = DateTime.Now.AddDays(-15), Amount = 9500,  Description = "Счёт за услуги",       CustomerName = "Алексеев А.А.",DueDate = DateTime.Now.AddDays(5),   TaxAmount = 1900, IsOverdue = false, BankAccount = "40702810000000001234" },
            new Bill   { Title = "Счёт #2",        CreatedAt = DateTime.Now.AddDays(-10), Amount = 4200,  Description = "Счёт за аренду",       CustomerName = "Михайлов М.М.",DueDate = DateTime.Now.AddDays(-2),  TaxAmount = 840,  IsOverdue = true,  BankAccount = "40702810000000005678" },
            new Bill   { Title = "Счёт #3",        CreatedAt = DateTime.Now.AddDays(-7),  Amount = 18000, Description = "Счёт за оборудование", CustomerName = "Фёдоров Ф.Ф.", DueDate = DateTime.Now.AddDays(10),  TaxAmount = 3600, IsOverdue = false, BankAccount = "40702810000000009012" },
            new Bill   { Title = "Счёт #4",        CreatedAt = DateTime.Now.AddDays(-3),  Amount = 750,   Description = "Счёт за консультацию", CustomerName = "Дмитриев Д.Д.",DueDate = DateTime.Now.AddDays(-5),  TaxAmount = 150,  IsOverdue = true,  BankAccount = "40702810000000003456" },
            new Bill   { Title = "Счёт #5",        CreatedAt = DateTime.Now.AddDays(-1),  Amount = 31500, Description = "Счёт за проект",       CustomerName = "Васильев В.В.",DueDate = DateTime.Now.AddDays(20),  TaxAmount = 6300, IsOverdue = false, BankAccount = "40702810000000007890" },
        };
        foreach (var d in docs) Add(d);
    }
}
