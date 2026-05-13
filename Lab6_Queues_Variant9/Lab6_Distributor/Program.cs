using System.Text;
using Lab6Distributor;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "Lab6 — Распространитель отчётов";

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Lab6 Distributor — запуск...");
Console.WriteLine("Подключение к RabbitMQ...");

using var distributor = new ReportDistributor();
using var dlq         = new DlqProcessor();

// DLQ-обработчик запускаем в фоновом потоке
var dlqThread = new Thread(() => dlq.Start(cts.Token)) { IsBackground = true, Name = "DLQ-Thread" };
dlqThread.Start();

// Основной поток — обработка отчётов
distributor.Start(cts.Token);

Console.WriteLine("Распространитель остановлен. До свидания!");
