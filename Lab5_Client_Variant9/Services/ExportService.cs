using Lab5Client.Export;
using Lab5Client.Models;

namespace Lab5Client.Services;

// Оркестрирует экспорт данных в разные форматы
public class ExportService
{
    private readonly CsvExporter   _csv;
    private readonly ExcelExporter _excel;
    private readonly PdfExporter   _pdf;

    public ExportService(CsvExporter csv, ExcelExporter excel, PdfExporter pdf)
    {
        _csv   = csv;
        _excel = excel;
        _pdf   = pdf;
    }

    public Task ExportToCsvAsync(IEnumerable<InvoiceResponse> invoices) =>
        _csv.ExportAsync(invoices, "invoices_export.csv");

    public Task ExportToExcelAsync(IEnumerable<InvoiceResponse> invoices) =>
        _excel.ExportAsync(invoices, "invoices_export.xlsx");

    public Task ExportToPdfAsync(IEnumerable<InvoiceResponse> invoices) =>
        _pdf.ExportAsync(invoices, "invoices_export.pdf");
}
