using System.Text;
using System.Text.Json;
using Lab6Shared.Models;
using RabbitMQ.Client;

namespace Lab6Shared.Messaging;

// Отправляет сообщения в RabbitMQ. Persistent (durable) сообщения.
public class MessagePublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel      _channel;
    private readonly object      _lock = new();

    private long _totalBytesSent;
    private int  _totalMessagesSent;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MessagePublisher(string clientName = "Lab6-Publisher")
    {
        _connection = RabbitMqConnectionFactory.CreateConnection(clientName);
        _channel    = _connection.CreateModel();
        RabbitMqConnectionFactory.SetupTopology(_channel);
    }

    public void Publish<T>(MessageEnvelope<T> envelope, string routingKey)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOpts);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.Persistent       = true;       // сообщение переживёт перезапуск брокера
        props.ContentType      = "application/json";
        props.ContentEncoding  = "utf-8";
        props.MessageId        = envelope.MessageId.ToString();
        props.Timestamp        = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        lock (_lock)
        {
            _channel.BasicPublish(
                exchange:   Lab6Shared.Config.RabbitMqConfig.Exchange,
                routingKey: routingKey,
                basicProperties: props,
                body:        body);
        }

        Interlocked.Add(ref _totalBytesSent, body.Length);
        Interlocked.Increment(ref _totalMessagesSent);
    }

    public (int Messages, long Bytes) GetStats() =>
        (Volatile.Read(ref _totalMessagesSent), Volatile.Read(ref _totalBytesSent));

    public void Dispose()
    {
        try { _channel?.Dispose(); }    catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
    }
}
