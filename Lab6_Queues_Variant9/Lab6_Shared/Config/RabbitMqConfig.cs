namespace Lab6Shared.Config;

// Все константы RabbitMQ в одном месте
public static class RabbitMqConfig
{
    // Подключение
    public const string Host     = "localhost";
    public const int    Port     = 5672;
    public const string Username = "guest";
    public const string Password = "guest";

    // Exchange
    public const string Exchange     = "warehouse.exchange";
    public const string ExchangeType = "direct";

    // Очереди
    public const string InvoicesQueue = "invoices.queue";
    public const string ReportsQueue  = "reports.queue";
    public const string InvoicesDlq   = "invoices.dlq";
    public const string ReportsDlq    = "reports.dlq";

    // Routing keys
    public const string InvoiceRoutingKey     = "invoice.created";
    public const string ReportRoutingKey      = "report.ready";
    public const string InvoiceFailedKey      = "invoice.failed";
    public const string ReportFailedKey       = "report.failed";
}
