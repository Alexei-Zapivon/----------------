namespace Lab1_OOP_Variant9.Models;

// Базовый абстрактный класс — НАСЛЕДОВАНИЕ
public abstract class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    // Виртуальный метод — ПОЛИМОРФИЗМ (каждый потомок переопределяет)
    public virtual string GetInfo()
    {
        return $"[Документ] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy} | {Description}";
    }

    // Абстрактный метод — обязателен к реализации в каждом потомке
    public abstract string GetTypeName();
}
