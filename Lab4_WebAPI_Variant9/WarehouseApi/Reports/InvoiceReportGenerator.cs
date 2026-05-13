using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WarehouseApi.Models;

namespace WarehouseApi.Reports;

// Генератор PDF-отчёта по накладным с использованием QuestPDF
public class InvoiceReportGenerator : IReportGenerator
{
    public byte[] Generate(IEnumerable<Invoice> invoices, IEnumerable<Supplier> suppliers)
    {
        var supplierDict = suppliers.ToDictionary(s => s.Id, s => s.Name);
        var invoiceList = invoices.ToList();
        var generatedAt = DateTime.Now;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                // Заголовок страницы
                page.Header().Column(col =>
                {
                    col.Item()
                        .Text("Отчёт по накладным")
                        .FontSize(20).Bold().AlignCenter();

                    col.Item()
                        .PaddingTop(4)
                        .Text($"Дата генерации: {generatedAt:dd.MM.yyyy HH:mm}")
                        .FontSize(10).AlignCenter();

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CCCCCC");
                });

                // Основное содержимое
                page.Content().PaddingTop(12).Column(col =>
                {
                    // Таблица накладных
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Номер
                            columns.RelativeColumn(2);  // Дата
                            columns.RelativeColumn(4);  // Поставщик
                            columns.RelativeColumn(2);  // Сумма
                            columns.RelativeColumn(2);  // Статус
                        });

                        // Заголовок таблицы
                        table.Header(header =>
                        {
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#2C3E50").Padding(6);

                            header.Cell().Element(HeaderCell)
                                .Text("Номер").FontColor("#FFFFFF").Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Дата").FontColor("#FFFFFF").Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Поставщик").FontColor("#FFFFFF").Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Сумма").FontColor("#FFFFFF").Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Статус").FontColor("#FFFFFF").Bold();
                        });

                        // Строки данных с чередованием цвета
                        var oddRow = false;
                        foreach (var invoice in invoiceList)
                        {
                            oddRow = !oddRow;
                            var bg = oddRow ? "#F8F9FA" : "#FFFFFF";

                            IContainer DataCell(IContainer c) =>
                                c.Background(bg).Padding(5).BorderBottom(1).BorderColor("#E0E0E0");

                            var supplierName = supplierDict.GetValueOrDefault(invoice.SupplierId, "—");
                            var statusText = invoice.Status switch
                            {
                                InvoiceStatus.Draft      => "Черновик",
                                InvoiceStatus.Confirmed  => "Подтверждена",
                                InvoiceStatus.Cancelled  => "Отменена",
                                _                        => invoice.Status.ToString()
                            };
                            var statusColor = invoice.Status switch
                            {
                                InvoiceStatus.Confirmed => "#27AE60",
                                InvoiceStatus.Cancelled => "#E74C3C",
                                _                       => "#F39C12"
                            };

                            table.Cell().Element(DataCell).Text(invoice.Number);
                            table.Cell().Element(DataCell).Text(invoice.Date.ToString("dd.MM.yyyy"));
                            table.Cell().Element(DataCell).Text(supplierName);
                            table.Cell().Element(DataCell).Text($"{invoice.TotalAmount:N2} ₽");
                            table.Cell().Element(DataCell)
                                .Text(statusText).FontColor(statusColor).Bold();
                        }
                    });

                    // Итоговая строка
                    var totalAmount = invoiceList.Sum(i => i.TotalAmount);
                    var confirmedCount = invoiceList.Count(i => i.Status == InvoiceStatus.Confirmed);

                    col.Item().PaddingTop(12)
                        .Background("#EBF5FB")
                        .Border(1).BorderColor("#AED6F1")
                        .Padding(10)
                        .Column(summary =>
                        {
                            summary.Item().Text("Итого:").Bold().FontSize(11);
                            summary.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem()
                                    .Text($"Всего накладных: {invoiceList.Count}");
                                row.RelativeItem()
                                    .Text($"Общая сумма: {totalAmount:N2} ₽").Bold();
                                row.RelativeItem()
                                    .Text($"Подтверждено: {confirmedCount}").FontColor("#27AE60");
                            });
                        });
                });

                // Нижний колонтитул с номером страницы
                page.Footer().PaddingTop(8).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#CCCCCC");
                    col.Item().PaddingTop(4).AlignCenter().Text(text =>
                    {
                        text.Span($"Сгенерировано: {generatedAt:dd.MM.yyyy HH:mm}  |  Страница ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }
}
