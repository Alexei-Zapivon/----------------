namespace Lab1_OOP_Variant9.Models;

// Накладная — наследует Document (НАСЛЕДОВАНИЕ)
public class Invoice : Document
{
    public string SupplierName { get; set; } = string.Empty;   // Поставщик
    public string BuyerName { get; set; } = string.Empty;      // Покупатель
    public DateTime DeliveryDate { get; set; }                  // Дата поставки
    public int ItemsCount { get; set; }                         // Количество позиций
    public string ShippingAddress { get; set; } = string.Empty; // Адрес доставки

    public override string GetTypeName() => "Накладная";

    // ПОЛИМОРФИЗМ — переопределяем метод базового класса
    public override string GetInfo()
    {
        return $"[Накладная] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy}\n" +
               $"  Поставщик: {SupplierName} → Покупатель: {BuyerName} | Позиций: {ItemsCount} | Доставка: {DeliveryDate:dd.MM.yyyy}\n" +
               $"  Адрес: {ShippingAddress}";
    }
}
