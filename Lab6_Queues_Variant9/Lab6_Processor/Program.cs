using System.Text;
using Lab6Processor;
using QuestPDF.Infrastructure;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "Lab6 — Обработчик накладных";

// Лицензия QuestPDF Community
QuestPDF.Settings.License = LicenseType.Community;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Lab6 Processor — запуск...");
Console.WriteLine("Подключение к RabbitMQ...");

using var processor = new InvoiceProcessor();
processor.Start(cts.Token);

Console.WriteLine("Обработчик остановлен. До свидания!");
