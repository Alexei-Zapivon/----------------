using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lab6Shared.Config;
using Lab6Shared.Messaging;
using Lab6Shared.Models;

namespace Lab6Distributor;

// Получает готовые отчёты → проверяет → сохраняет на диск → логирует
public class ReportDistributor : IDisposable
{
    private readonly MessageConsumer _consumer;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReportDistributor()
    {
        _consumer = new MessageConsumer("Lab6-Distributor");
    }

    public void Start(CancellationToken ct)
    {
        Console.WriteLine($"\n[{Now}] Распространитель запущен.");
        Console.WriteLine($"  Ожидание отчётов из очереди: {RabbitMqConfig.ReportsQueue}");
        Console.WriteLine("  Нажмите Ctrl+C для остановки.\n");

        _consumer.StartListening(RabbitMqConfig.ReportsQueue, ProcessReport);

        ct.WaitHandle.WaitOne();
        Console.WriteLine($"\n[{Now}] Остановка распространителя...");
    }

    private bool ProcessReport(string json, IDictionary<string, object?> _)
    {
        MessageEnvelope<ReportResultMessage>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MessageEnvelope<ReportResultMessage>>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            PrintError($"Ошибка десериализации: {ex.Message}");
            return false;
        }

        if (envelope?.Payload is null)
        {
            PrintError("Payload отсутствует → DLQ");
            return false;
        }

        Console.WriteLine($"\n[{Now}] → Получен отчёт: {envelope.MessageId}");

        bool allOk = true;

        foreach (var att in envelope.Attachments)
        {
            Console.WriteLine($"  Файл: {att.FileName} ({att.Size:N0} байт)");

            // Проверка MIME
            if (att.MimeType != "application/pdf")
            {
                PrintError($"Неверный MIME-тип: {att.MimeType} → DLQ");
                return false;
            }

            // Проверка размера ≤ 10 МБ
            if (!att.ValidateSize(10))
            {
                PrintError($"Размер превышает 10 МБ → DLQ");
                return false;
            }

            // Проверка хеша
            var actualHash = Convert.ToHexString(SHA256.HashData(att.Content)).ToLowerInvariant();
            var hashMatch  = actualHash == att.ContentHash;

            Console.Write("  Хеш: ");
            if (hashMatch)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ совпадает");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ не совпадает");
                allOk = false;
            }
            Console.ResetColor();

            // Сохранение файла
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var saveDir    = Path.Combine("received_reports", dateFolder);
            Directory.CreateDirectory(saveDir);
            var filePath   = Path.Combine(saveDir, att.FileName);
            File.WriteAllBytes(filePath, att.Content);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{Now}] ✓ Сохранён: ./received_reports/{dateFolder}/{att.FileName}");
            Console.ResetColor();

            WriteLog(envelope, att, filePath, hashMatch);
        }

        return allOk;
    }

    // Запись строки в лог-файл
    private static void WriteLog(
        MessageEnvelope<ReportResultMessage> env,
        Attachment att,
        string savedPath,
        bool hashOk)
    {
        var logDir  = "logs";
        Directory.CreateDirectory(logDir);
        var logFile = Path.Combine(logDir, $"distributor_{DateTime.Now:yyyy-MM-dd}.log");

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                   $"MessageId={env.MessageId} " +
                   $"InvoiceId={env.Payload!.InvoiceId} " +
                   $"File={att.FileName} " +
                   $"Size={att.Size} " +
                   $"HashValid={hashOk} " +
                   $"SavedTo={savedPath}";

        File.AppendAllText(logFile, line + Environment.NewLine, Encoding.UTF8);
    }

    private static string Now => DateTime.Now.ToString("HH:mm:ss");

    private static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {msg}");
        Console.ResetColor();
    }

    public void Dispose() => _consumer.Dispose();
}
