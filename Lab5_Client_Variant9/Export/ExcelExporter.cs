using System.Drawing;
using Lab5Client.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Lab5Client.Export;

public class ExcelExporter
{
    public Task ExportAsync(IEnumerable<InvoiceResponse> invoices, string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package  = new ExcelPackage();
        var invoiceList    = invoices.ToList();

        BuildInvoicesSheet(package, invoiceList);
        BuildSummarySheet(package, invoiceList);

        package.SaveAs(new FileInfo(filePath));
        return Task.CompletedTask;
    }

    private static void BuildInvoicesSheet(ExcelPackage pkg, List<InvoiceResponse> list)
    {
        var ws      = pkg.Workbook.Worksheets.Add("Накладные");
        var headers = new[] { "ID", "Номер", "Дата", "Поставщик", "Сумма", "Статус" };

        // Заголовки: синий фон, белый шрифт, жирный
        for (int c = 1; c <= headers.Length; c++)
        {
            var cell = ws.Cells[1, c];
            cell.Value = headers[c - 1];
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.RoyalBlue);
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // Данные
        for (int i = 0; i < list.Count; i++)
        {
            var inv = list[i];
            int row = i + 2;

            ws.Cells[row, 1].Value = inv.Id;
            ws.Cells[row, 2].Value = inv.Number;
            ws.Cells[row, 3].Value = inv.Date.ToString("dd.MM.yyyy");
            ws.Cells[row, 4].Value = inv.SupplierName;
            ws.Cells[row, 5].Value = inv.TotalAmount;
            ws.Cells[row, 6].Value = inv.Status;

            // Условное форматирование суммы по статусу
            var amountCell = ws.Cells[row, 5];
            if (inv.Status == "Confirmed")
                amountCell.Style.Font.Color.SetColor(Color.ForestGreen);
            else if (inv.Status == "Cancelled")
                amountCell.Style.Font.Color.SetColor(Color.Crimson);
        }

        ws.Cells.AutoFitColumns();
    }

    private static void BuildSummarySheet(ExcelPackage pkg, List<InvoiceResponse> list)
    {
        var ws = pkg.Workbook.Worksheets.Add("Сводка");

        ws.Cells[1, 1].Value = "Статус";
        ws.Cells[1, 2].Value = "Количество";
        ws.Cells[1, 1].Style.Font.Bold = true;
        ws.Cells[1, 2].Style.Font.Bold = true;

        var groups = list.GroupBy(i => i.Status).ToList();
        for (int i = 0; i < groups.Count; i++)
        {
            ws.Cells[i + 2, 1].Value = groups[i].Key;
            ws.Cells[i + 2, 2].Value = groups[i].Count();
        }

        int totalRow = groups.Count + 3;
        ws.Cells[totalRow, 1].Value = "Итого:";
        ws.Cells[totalRow, 2].Value = list.Count;
        ws.Cells[totalRow, 1].Style.Font.Bold = true;
        ws.Cells[totalRow, 2].Style.Font.Bold = true;

        ws.Cells.AutoFitColumns();
    }
}
