using System.Text;
using Lab6Shared.Config;
using Lab6Shared.Messaging;

namespace Lab6Distributor;

// Слушает обе DLQ и архивирует ошибочные сообщения в JSON-файлы
public class DlqProcessor : IDisposable
{
    private readonly MessageConsumer _invoiceDlq;
    private readonly MessageConsumer _reportDlq;

    public DlqProcessor()
    {
        _invoiceDlq = new MessageConsumer("Lab6-DLQ-Invoices");
        _reportDlq  = new MessageConsumer("Lab6-DLQ-Reports");
    }

    public void Start(CancellationToken ct)
    {
        Console.WriteLine($"[{Now}] DLQ-обработчик запущен.");
        Console.WriteLine($"  Слушаем: {RabbitMqConfig.InvoicesDlq}, {RabbitMqConfig.ReportsDlq}");

        _invoiceDlq.StartListening(RabbitMqConfig.InvoicesDlq,
            (msg, _) => Archive(msg, "invoice"));

        _reportDlq.StartListening(RabbitMqConfig.ReportsDlq,
            (msg, _) => Archive(msg, "report"));

        ct.WaitHandle.WaitOne();
    }

    // Сохраняет сообщение как JSON-файл в ./failed_messages/{date}/
    private static bool Archive(string json, string type)
    {
        try
        {
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var dir        = Path.Combine("failed_messages", dateFolder);
            Directory.CreateDirectory(dir);

            var fileName = $"{type}_{DateTime.Now:HHmmss}_{Guid.NewGuid():N[..8]}.json";
            var filePath = Path.Combine(dir, fileName);
            File.WriteAllText(filePath, json, Encoding.UTF8);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{Now}] ! DLQ [{type}]: архивировано → {filePath}");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Ошибка архивации DLQ: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private static string Now => DateTime.Now.ToString("HH:mm:ss");

    public void Dispose()
    {
        _invoiceDlq.Dispose();
        _reportDlq.Dispose();
    }
}
