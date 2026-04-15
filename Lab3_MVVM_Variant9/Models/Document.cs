namespace Lab3_MVVM_Variant9.Models;

public abstract class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public abstract string TypeName { get; }

    public virtual string GetInfo()
        => $"[{TypeName}] #{Id} | {Title} | {Amount:N2} руб. | {CreatedAt:dd.MM.yyyy}";
}
