using FluentValidation;
using System.Text.Json;

namespace WarehouseApi.Middleware;

// Глобальный обработчик исключений — возвращает Problem Details (RFC 7807)
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Ошибка валидации запроса: {Message}", ex.Message);
            var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "Ошибка валидации", "Один или несколько параметров не прошли проверку", errors);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Ресурс не найден: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Ресурс не найден", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Конфликт операции: {Message}", ex.Message);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Конфликт", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера", "Произошла непредвиденная ошибка. Обратитесь к администратору.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail, object? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var body = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["instance"] = context.Request.Path.Value
        };

        if (errors != null)
            body["errors"] = errors;

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
