namespace Lab6Shared.Models;

// Payload результата обработки: Processor → Distributor
public class ReportResultMessage
{
    public int      InvoiceId      { get; set; }
    public string   ReportFileName { get; set; } = "";
    public DateTime GeneratedAt    { get; set; }
    public int      RecordsCount   { get; set; }
}
