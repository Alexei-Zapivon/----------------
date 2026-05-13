using Lab6Shared.Config;
using RabbitMQ.Client;

namespace Lab6Shared.Messaging;

public static class RabbitMqConnectionFactory
{
    // Создаёт подключение с повторными попытками при недоступности брокера
    public static IConnection CreateConnection(string clientName = "Lab6")
    {
        var factory = new ConnectionFactory
        {
            HostName                = RabbitMqConfig.Host,
            Port                    = RabbitMqConfig.Port,
            UserName                = RabbitMqConfig.Username,
            Password                = RabbitMqConfig.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat      = TimeSpan.FromSeconds(60)
        };

        int attempt = 0;
        while (true)
        {
            try
            {
                var conn = factory.CreateConnection(clientName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Подключено к RabbitMQ: {RabbitMqConfig.Host}:{RabbitMqConfig.Port}");
                Console.ResetColor();
                return conn;
            }
            catch (Exception ex)
            {
                attempt++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ! Попытка {attempt}: не удалось подключиться к RabbitMQ.");
                Console.WriteLine($"    {ex.Message}");
                Console.WriteLine("    Повтор через 5 секунд...");
                Console.ResetColor();
                Thread.Sleep(5000);
            }
        }
    }

    // Объявляет exchange, очереди (с DLQ) и все биндинги
    public static void SetupTopology(IModel channel)
    {
        // Exchange
        channel.ExchangeDeclare(
            exchange:    RabbitMqConfig.Exchange,
            type:        RabbitMqConfig.ExchangeType,
            durable:     true,
            autoDelete:  false);

        // DLQ-очереди объявляем первыми (на них ссылаются основные очереди)
        channel.QueueDeclare(RabbitMqConfig.InvoicesDlq, durable: true,
            exclusive: false, autoDelete: false);
        channel.QueueDeclare(RabbitMqConfig.ReportsDlq, durable: true,
            exclusive: false, autoDelete: false);

        // Основные очереди с указанием DLX
        var invoiceArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"]    = RabbitMqConfig.Exchange,
            ["x-dead-letter-routing-key"] = RabbitMqConfig.InvoiceFailedKey
        };
        var reportArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"]    = RabbitMqConfig.Exchange,
            ["x-dead-letter-routing-key"] = RabbitMqConfig.ReportFailedKey
        };

        channel.QueueDeclare(RabbitMqConfig.InvoicesQueue, durable: true,
            exclusive: false, autoDelete: false, arguments: invoiceArgs);
        channel.QueueDeclare(RabbitMqConfig.ReportsQueue, durable: true,
            exclusive: false, autoDelete: false, arguments: reportArgs);

        // Биндинги
        channel.QueueBind(RabbitMqConfig.InvoicesQueue, RabbitMqConfig.Exchange, RabbitMqConfig.InvoiceRoutingKey);
        channel.QueueBind(RabbitMqConfig.ReportsQueue,  RabbitMqConfig.Exchange, RabbitMqConfig.ReportRoutingKey);
        channel.QueueBind(RabbitMqConfig.InvoicesDlq,   RabbitMqConfig.Exchange, RabbitMqConfig.InvoiceFailedKey);
        channel.QueueBind(RabbitMqConfig.ReportsDlq,    RabbitMqConfig.Exchange, RabbitMqConfig.ReportFailedKey);
    }
}
