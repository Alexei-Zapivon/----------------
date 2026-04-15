namespace Lab2_EF_Variant9.Models;

// Базовый класс — НАСЛЕДОВАНИЕ (TPH: все хранятся в одной таблице)
public abstract class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }       // добавлено в Migration 2
    public bool IsArchived { get; set; }     // добавлено в Migration 3

    public virtual string GetInfo()
        => $"[{GetType().Name}] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy}";
}
