using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Lab5Client.Models;

namespace Lab5Client.Export;

public class CsvExporter
{
    public async Task ExportAsync(IEnumerable<InvoiceResponse> invoices, string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true
        };

        await using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
        await using var csv    = new CsvWriter(writer, config);

        // Заголовки на русском
        csv.WriteField("ID");
        csv.WriteField("Номер");
        csv.WriteField("Дата");
        csv.WriteField("Поставщик");
        csv.WriteField("Сумма");
        csv.WriteField("Статус");
        await csv.NextRecordAsync();

        foreach (var inv in invoices)
        {
            csv.WriteField(inv.Id);
            csv.WriteField(inv.Number);
            csv.WriteField(inv.Date.ToString("dd.MM.yyyy"));
            csv.WriteField(inv.SupplierName);
            csv.WriteField(inv.TotalAmount);
            csv.WriteField(inv.Status);
            await csv.NextRecordAsync();
        }
    }
}
