using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lab6Shared.Messaging;

// Подписывается на очередь и вызывает handler для каждого сообщения.
// Возвращает true → BasicAck, false → BasicNack (уйдёт в DLQ).
public class MessageConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel      _channel;

    public MessageConsumer(string clientName = "Lab6-Consumer", ushort prefetchCount = 1)
    {
        _connection = RabbitMqConnectionFactory.CreateConnection(clientName);
        _channel    = _connection.CreateModel();
        RabbitMqConnectionFactory.SetupTopology(_channel);

        // Ручное подтверждение, обрабатываем по одному сообщению за раз
        _channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
    }

    // handler(rawJson, headers) → true=ack, false=nack
    public void StartListening(
        string queue,
        Func<string, IDictionary<string, object?>, bool> handler)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (_, ea) =>
        {
            bool success = false;
            try
            {
                var json    = Encoding.UTF8.GetString(ea.Body.ToArray());
                var headers = (IDictionary<string, object?>?)ea.BasicProperties?.Headers
                              ?? new Dictionary<string, object?>();
                success = handler(json, headers);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Исключение в обработчике: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                if (success)
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                else
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        Console.WriteLine($"  Подписка на очередь: {queue}");
    }

    public void Dispose()
    {
        try { _channel?.Dispose(); }    catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
    }
}
