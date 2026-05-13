using System.Security.Cryptography;

namespace Lab6Shared.Models;

public class Attachment
{
    public string FileName    { get; set; } = "";
    public string MimeType    { get; set; } = "";
    public byte[] Content     { get; set; } = [];
    public string Description { get; set; } = "";
    public long   Size        { get; set; }

    // SHA256 от Content — вычисляется при создании
    public string ContentHash { get; set; } = "";

    // Проверяет, что размер не превышает maxSizeMb мегабайт
    public bool ValidateSize(double maxSizeMb) =>
        Size <= maxSizeMb * 1024 * 1024;

    // Фабричный метод: создаёт вложение и вычисляет хеш
    public static Attachment Create(
        byte[] content, string fileName, string mimeType, string description = "")
    {
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        return new Attachment
        {
            FileName    = fileName,
            MimeType    = mimeType,
            Content     = content,
            Description = description,
            Size        = content.Length,
            ContentHash = hash
        };
    }
}
