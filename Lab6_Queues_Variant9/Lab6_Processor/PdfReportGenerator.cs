using Lab6Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lab6Processor;

// Генерирует PDF-отчёт по накладной с помощью QuestPDF
public class PdfReportGenerator
{
    public byte[] Generate(InvoiceMessage invoice, string messageHash)
    {
        var generatedAt = DateTime.Now;

        var rows = new (string Field, string Value)[]
        {
            ("ID",          invoice.Id.ToString()),
            ("Номер",       invoice.Number),
            ("Дата",        invoice.Date.ToString("dd.MM.yyyy")),
            ("Поставщик",   invoice.SupplierName),
            ("Сумма",       $"{invoice.TotalAmount:N2} руб."),
            ("Статус",      invoice.Status)
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                // Заголовок
                page.Header().Column(col =>
                {
                    col.Item()
                       .Text($"Отчёт по накладной {invoice.Number}")
                       .FontSize(18).Bold().AlignCenter();
                    col.Item()
                       .PaddingTop(4)
                       .Text($"Сгенерировано: {generatedAt:dd.MM.yyyy HH:mm:ss}")
                       .FontSize(9).AlignCenter();
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CCCCCC");
                });

                // Таблица с полями накладной
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);  // Поле
                            cols.RelativeColumn(4);  // Значение
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background("#2C3E50").Padding(6)
                             .Text("Поле").FontColor("#FFFFFF").Bold();
                            h.Cell().Background("#2C3E50").Padding(6)
                             .Text("Значение").FontColor("#FFFFFF").Bold();
                        });

                        for (int i = 0; i < rows.Length; i++)
                        {
                            var bg = i % 2 == 0 ? "#FFFFFF" : "#F4F6F7";
                            table.Cell().Background(bg).Padding(6)
                                 .Text(rows[i].Field).Bold();
                            table.Cell().Background(bg).Padding(6)
                                 .Text(rows[i].Value);
                        }
                    });

                    // Блок с мета-информацией о сообщении
                    col.Item().PaddingTop(16)
                       .Background("#EBF5FB").Border(1).BorderColor("#AED6F1").Padding(10)
                       .Column(info =>
                       {
                           info.Item().Text("Информация о сообщении").Bold().FontSize(10);
                           info.Item().PaddingTop(4)
                               .Text($"Хеш исходного сообщения: {messageHash}")
                               .FontSize(8).FontColor("#555555");
                       });
                });

                // Нижний колонтитул
                page.Footer().PaddingTop(6).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#CCCCCC");
                    col.Item().PaddingTop(4).AlignCenter().Text(text =>
                    {
                        text.Span("Страница ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }
}
