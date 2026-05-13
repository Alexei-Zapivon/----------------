using Lab6Shared.Config;
using Lab6Shared.Messaging;
using Lab6Shared.Models;

namespace Lab6Publisher;

public class Publisher : IDisposable
{
    private readonly MessagePublisher _pub;
    private readonly InvoiceMessage[] _data;
    private int _index;

    public Publisher()
    {
        _pub  = new MessagePublisher("Lab6-Publisher");
        _data = TestDataGenerator.GenerateInvoices();
    }

    // ── Публичные методы меню ──────────────────────────────────────────────

    public void SendOne()
    {
        var inv = NextInvoice();
        var env = Wrap(inv);
        _pub.Publish(env, RabbitMqConfig.InvoiceRoutingKey);
        PrintSent(env.MessageId, inv);
    }

    public void SendBatch(int count, CancellationToken ct)
    {
        Console.WriteLine($"\n  Отправка пакета из {count} накладных...\n");
        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var inv = NextInvoice();
            var env = Wrap(inv);
            _pub.Publish(env, RabbitMqConfig.InvoiceRoutingKey);
            PrintSent(env.MessageId, inv);
            PrintProgress(i + 1, count);
            Thread.Sleep(250);
        }
        Console.WriteLine("\n\n  Пакет отправлен.");
    }

    public void SendWithAttachment()
    {
        var inv  = NextInvoice();
        // Publisher не использует QuestPDF; создаём текстовый PDF-заглушку
        var txt  = $"%PDF-1.4\n%Invoice\nNumber: {inv.Number}\n" +
                   $"Supplier: {inv.SupplierName}\nAmount: {inv.TotalAmount:N2}";
        var att  = Attachment.Create(
                       System.Text.Encoding.UTF8.GetBytes(txt),
                       $"invoice_{inv.Number}.pdf",
                       "application/pdf",
                       "Превью накладной");
        var env  = Wrap(inv, att);
        _pub.Publish(env, RabbitMqConfig.InvoiceRoutingKey);

        PrintSent(env.MessageId, inv);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"  Вложение: {att.FileName} ({att.Size} байт), " +
                          $"хеш: {att.ContentHash[..16]}...");
        Console.ResetColor();
    }

    public void PrintStats()
    {
        var (msgs, bytes) = _pub.GetStats();
        Console.WriteLine("\n  ══════ Статистика отправки ══════");
        Console.WriteLine($"  Отправлено сообщений : {msgs}");
        Console.WriteLine($"  Передано данных      : {bytes:N0} байт ({bytes / 1024.0:F1} КБ)");
    }

    // ── Вспомогательные ───────────────────────────────────────────────────

    private InvoiceMessage NextInvoice() => _data[_index++ % _data.Length];

    private static MessageEnvelope<InvoiceMessage> Wrap(
        InvoiceMessage inv, params Attachment[] attachments) =>
        new()
        {
            MessageId   = Guid.NewGuid(),
            Timestamp   = DateTime.UtcNow,
            Source      = "Lab6_Publisher",
            MessageType = nameof(InvoiceMessage),
            Payload     = inv,
            Attachments = attachments,
            Metadata    = new Dictionary<string, string>
            {
                ["version"]       = "1.0",
                ["correlationId"] = Guid.NewGuid().ToString("N")
            }
        };

    private static void PrintSent(Guid id, InvoiceMessage inv)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ Отправлено сообщение {id} → invoices.queue");
        Console.ResetColor();
        Console.WriteLine($"  Накладная: {inv.Number}, " +
                          $"сумма: {inv.TotalAmount:N2}, " +
                          $"поставщик: {inv.SupplierName}");
    }

    private static void PrintProgress(int current, int total)
    {
        var pct    = (double)current / total;
        var filled = (int)(pct * 30);
        Console.Write($"\r  Прогресс: [");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(new string('█', filled));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', 30 - filled));
        Console.ResetColor();
        Console.Write($"] {pct:P0}  ({current}/{total})  ");
    }

    public void Dispose() => _pub.Dispose();
}
