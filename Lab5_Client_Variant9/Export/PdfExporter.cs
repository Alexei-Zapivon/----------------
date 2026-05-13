using Lab5Client.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab5Client.Export;

public class PdfExporter
{
    public Task ExportAsync(IEnumerable<InvoiceResponse> invoices, string filePath)
    {
        var list        = invoices.ToList();
        var generatedAt = DateTime.Now;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                // Заголовок
                page.Header().Column(col =>
                {
                    col.Item()
                       .Text($"Отчёт по накладным — {generatedAt:dd.MM.yyyy}")
                       .FontSize(18).Bold().AlignCenter();
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#CCCCCC");
                });

                // Таблица
                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.5f); // ID
                            cols.RelativeColumn(2.5f); // Номер
                            cols.RelativeColumn(2);    // Дата
                            cols.RelativeColumn(4);    // Поставщик
                            cols.RelativeColumn(2);    // Сумма
                            cols.RelativeColumn(2);    // Статус
                        });

                        // Заголовок таблицы
                        table.Header(h =>
                        {
                            static IContainer HCell(IContainer c) =>
                                c.Background("#2C3E50").Padding(5);

                            foreach (var title in new[] { "ID", "Номер", "Дата", "Поставщик", "Сумма", "Статус" })
                                h.Cell().Element(HCell).Text(title).FontColor("#FFFFFF").Bold();
                        });

                        // Строки
                        bool odd = false;
                        foreach (var inv in list)
                        {
                            odd = !odd;
                            var bg = odd ? "#F8F9FA" : "#FFFFFF";

                            IContainer DCell(IContainer c) =>
                                c.Background(bg).Padding(4).BorderBottom(1).BorderColor("#EEEEEE");

                            var statusColor = inv.Status switch
                            {
                                "Confirmed" => "#27AE60",
                                "Cancelled" => "#E74C3C",
                                _           => "#F39C12"
                            };
                            var statusText = inv.Status switch
                            {
                                "Confirmed" => "Подтверждена",
                                "Cancelled" => "Отменена",
                                _           => "Черновик"
                            };

                            table.Cell().Element(DCell).Text(inv.Id.ToString());
                            table.Cell().Element(DCell).Text(inv.Number);
                            table.Cell().Element(DCell).Text(inv.Date.ToString("dd.MM.yyyy"));
                            table.Cell().Element(DCell).Text(inv.SupplierName);
                            table.Cell().Element(DCell).Text($"{inv.TotalAmount:N2} ₽");
                            table.Cell().Element(DCell)
                                 .Text(statusText).FontColor(statusColor).Bold();
                        }
                    });

                    // Итоги
                    var total     = list.Sum(i => i.TotalAmount);
                    var confirmed = list.Count(i => i.Status == "Confirmed");

                    col.Item().PaddingTop(12)
                       .Background("#EBF5FB").Border(1).BorderColor("#AED6F1").Padding(8)
                       .Column(s =>
                       {
                           s.Item().Text("Итого:").Bold().FontSize(11);
                           s.Item().PaddingTop(4).Row(r =>
                           {
                               r.RelativeItem().Text($"Всего накладных: {list.Count}");
                               r.RelativeItem().Text($"Общая сумма: {total:N2} ₽").Bold();
                               r.RelativeItem().Text($"Подтверждено: {confirmed}").FontColor("#27AE60");
                           });
                       });
                });

                // Нижний колонтитул
                page.Footer().PaddingTop(6).Column(col =>
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
        }).GeneratePdf(filePath);

        return Task.CompletedTask;
    }
}
