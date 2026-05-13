namespace Lab5Client.Exceptions;

// Базовое абстрактное исключение всех клиентских ошибок
public abstract class ClientException : Exception
{
    protected ClientException(string message, Exception? inner = null)
        : base(message, inner) { }
}

// Ошибка сети: нет соединения, DNS, сокет и т.д.
public class NetworkException : ClientException
{
    public NetworkException(string message, Exception? inner = null)
        : base(message, inner) { }
}

// Превышено время ожидания ответа
public class ApiTimeoutException : ClientException
{
    public TimeSpan TimeoutDuration { get; }

    public ApiTimeoutException(string message, TimeSpan timeoutDuration, Exception? inner = null)
        : base(message, inner)
    {
        TimeoutDuration = timeoutDuration;
    }
}

// Сервис недоступен: 503 или разорванная цепь Polly
public class ServiceUnavailableException : ClientException
{
    public int StatusCode { get; }

    public ServiceUnavailableException(string message, int statusCode, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

// Ошибка валидации: 400 с полем errors из API
public class ApiValidationException : ClientException
{
    public Dictionary<string, string[]> Errors { get; }

    public ApiValidationException(string message, Dictionary<string, string[]> errors, Exception? inner = null)
        : base(message, inner)
    {
        Errors = errors;
    }
}

// Ресурс не найден: 404
public class NotFoundException : ClientException
{
    public NotFoundException(string message, Exception? inner = null)
        : base(message, inner) { }
}
