using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lab6Shared.Models;

// Универсальная обёртка для любого сообщения в очереди
public class MessageEnvelope<T>
{
    public Guid       MessageId   { get; set; } = Guid.NewGuid();
    public DateTime   Timestamp   { get; set; } = DateTime.UtcNow;
    public string     Source      { get; set; } = "";
    public string     MessageType { get; set; } = "";
    public T?         Payload     { get; set; }
    public Attachment[] Attachments { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = new();

    // SHA256 от JSON-представления всего envelope
    public string CalculateHash()
    {
        var json  = JsonSerializer.Serialize(this);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
