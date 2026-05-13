using System.Text.Json;
using Lab6Shared.Config;
using Lab6Shared.Messaging;
using Lab6Shared.Models;

namespace Lab6Processor;

// Слушает invoices.queue → генерирует PDF → отправляет в reports.queue
public class InvoiceProcessor : IDisposable
{
    private readonly MessageConsumer     _consumer;
    private readonly MessagePublisher    _publisher;
    private readonly PdfReportGenerator  _pdfGen;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InvoiceProcessor()
    {
        _consumer  = new MessageConsumer("Lab6-Processor-Consumer");
        _publisher = new MessagePublisher("Lab6-Processor-Publisher");
        _pdfGen    = new PdfReportGenerator();
    }

    public void Start(CancellationToken ct)
    {
        Console.WriteLine($"\n[{Now}] Обработчик запущен.");
        Console.WriteLine($"  Ожидание сообщений из очереди: {RabbitMqConfig.InvoicesQueue}");
        Console.WriteLine("  Нажмите Ctrl+C для остановки.\n");

        _consumer.StartListening(RabbitMqConfig.InvoicesQueue, ProcessMessage);

        // Блокируем поток до отмены
        ct.WaitHandle.WaitOne();
        Console.WriteLine($"\n[{Now}] Остановка обработчика...");
    }

    private bool ProcessMessage(string json, IDictionary<string, object?> _)
    {
        Console.WriteLine($"\n[{Now}] → Получена накладная:");

        MessageEnvelope<InvoiceMessage>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MessageEnvelope<InvoiceMessage>>(json, JsonOpts);
        }
        catch (Exception ex)
        {
            PrintError($"Ошибка десериализации: {ex.Message}");
            return false;
        }

        if (envelope?.Payload is null)
        {
            PrintError("Payload отсутствует → отправка в DLQ");
            return false;
        }

        var inv = envelope.Payload;
        Console.WriteLine($"  MessageId : {envelope.MessageId}");
        Console.WriteLine($"  Накладная №{inv.Number}, поставщик: {inv.SupplierName}");

        // Валидация
        if (!Validate(envelope))
            return false;

        PrintOk("Валидация: ✓");

        // Генерация PDF
        Console.Write("  Генерация PDF... ");
        byte[] pdfBytes;
        try
        {
            var hash = envelope.CalculateHash();
            pdfBytes = _pdfGen.Generate(inv, hash);
        }
        catch (Exception ex)
        {
            PrintError($"Ошибка генерации PDF: {ex.Message}");
            return false;
        }
        PrintOk($"✓ ({pdfBytes.Length:N0} байт)");

        // Формируем ответное сообщение с PDF-вложением
        var reportName  = $"report_{inv.Number}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var attachment  = Attachment.Create(pdfBytes, reportName, "application/pdf");

        var resultEnv = new MessageEnvelope<ReportResultMessage>
        {
            MessageId   = Guid.NewGuid(),
            Timestamp   = DateTime.UtcNow,
            Source      = "Lab6_Processor",
            MessageType = nameof(ReportResultMessage),
            Payload     = new ReportResultMessage
            {
                InvoiceId      = inv.Id,
                ReportFileName = reportName,
                GeneratedAt    = DateTime.UtcNow,
                RecordsCount   = 1
            },
            Attachments = [attachment],
            Metadata    = new Dictionary<string, string>
            {
                ["sourceMessageId"] = envelope.MessageId.ToString()
            }
        };

        _publisher.Publish(resultEnv, RabbitMqConfig.ReportRoutingKey);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{Now}] ✓ Отчёт отправлен → reports.queue: {reportName}");
        Console.ResetColor();

        return true;
    }

    // Валидация входящего сообщения
    private static bool Validate(MessageEnvelope<InvoiceMessage> env)
    {
        // Возраст сообщения не более 1 часа
        if ((DateTime.UtcNow - env.Timestamp).TotalHours > 1)
        {
            PrintError("Сообщение устарело (старше 1 часа) → DLQ");
            return false;
        }

        // Проверка вложений (≤ 10 МБ каждое)
        foreach (var att in env.Attachments)
        {
            if (!att.ValidateSize(10))
            {
                PrintError($"Вложение '{att.FileName}' превышает 10 МБ → DLQ");
                return false;
            }
        }

        return true;
    }

    private static string Now => DateTime.Now.ToString("HH:mm:ss");

    private static void PrintOk(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  {msg}");
        Console.ResetColor();
    }

    private static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {msg}");
        Console.ResetColor();
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _publisher.Dispose();
    }
}
