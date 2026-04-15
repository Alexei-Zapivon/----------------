using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab3_MVVM_Variant9.Models;
using Lab3_MVVM_Variant9.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Lab3_MVVM_Variant9.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly DocumentService _service;

    [ObservableProperty] private PlotModel _barModel = new();
    [ObservableProperty] private PlotModel _pieModel = new();
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private int _receiptCount;
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private int _billCount;

    public StatisticsViewModel(DocumentService service)
    {
        _service = service;
        Refresh();
    }

    public void Refresh()
    {
        var docs = _service.GetAll();
        TotalCount  = docs.Count;
        TotalAmount = docs.Sum(d => d.Amount);
        ReceiptCount = docs.OfType<Receipt>().Count();
        InvoiceCount = docs.OfType<Invoice>().Count();
        BillCount    = docs.OfType<Bill>().Count();

        BuildBarChart(docs);
        BuildPieChart(docs);
    }

    // Столбчатая диаграмма — количество документов по типам
    private void BuildBarChart(List<Document> docs)
    {
        var model = new PlotModel { Title = "Количество документов по типам" };

        var series = new BarSeries { LabelPlacement = LabelPlacement.Inside, LabelFormatString = "{0}" };
        series.Items.Add(new BarItem(docs.OfType<Receipt>().Count()) { Color = OxyColors.SteelBlue });
        series.Items.Add(new BarItem(docs.OfType<Invoice>().Count()) { Color = OxyColors.SeaGreen });
        series.Items.Add(new BarItem(docs.OfType<Bill>().Count())    { Color = OxyColors.Tomato });

        var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
        categoryAxis.Labels.Add("Квитанции");
        categoryAxis.Labels.Add("Накладные");
        categoryAxis.Labels.Add("Счета");

        model.Axes.Add(categoryAxis);
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            AbsoluteMinimum = 0,
            MajorStep = 1,
            MinorStep = 1,
            MinimumPadding = 0,
            StringFormat = "0"
        });
        model.Series.Add(series);

        BarModel = model;
    }

    // Круговая диаграмма — суммы по типам
    private void BuildPieChart(List<Document> docs)
    {
        var model = new PlotModel { Title = "Суммы по типам (руб.)" };

        var series = new PieSeries
        {
            StrokeThickness = 1.5,
            InsideLabelPosition = 0.5,
            AngleSpan = 360,
            StartAngle = 0
        };

        double receiptSum = (double)docs.OfType<Receipt>().Sum(d => d.Amount);
        double invoiceSum = (double)docs.OfType<Invoice>().Sum(d => d.Amount);
        double billSum    = (double)docs.OfType<Bill>().Sum(d => d.Amount);

        if (receiptSum > 0) series.Slices.Add(new PieSlice("Квитанции", receiptSum) { Fill = OxyColors.SteelBlue });
        if (invoiceSum > 0) series.Slices.Add(new PieSlice("Накладные", invoiceSum) { Fill = OxyColors.SeaGreen });
        if (billSum    > 0) series.Slices.Add(new PieSlice("Счета",     billSum)    { Fill = OxyColors.Tomato });

        model.Series.Add(series);
        PieModel = model;
    }

    // Экспорт графика в PNG на рабочий стол
    [RelayCommand]
    private void ExportBarChart()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "bar_chart.png");
        using var stream = File.Create(path);
        var exporter = new OxyPlot.Wpf.PngExporter { Width = 800, Height = 400 };
        exporter.Export(BarModel, stream);
        System.Windows.MessageBox.Show($"Сохранено: {path}", "Экспорт");
    }

    [RelayCommand]
    private void ExportPieChart()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "pie_chart.png");
        using var stream = File.Create(path);
        var exporter = new OxyPlot.Wpf.PngExporter { Width = 600, Height = 600 };
        exporter.Export(PieModel, stream);
        System.Windows.MessageBox.Show($"Сохранено: {path}", "Экспорт");
    }
}
