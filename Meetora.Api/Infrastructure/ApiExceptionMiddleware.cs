using System.Text.Json;

namespace Meetora.Api.Infrastructure;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var payload = new
            {
                error = "internal_error",
                traceId = context.TraceIdentifier,
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));

            // Let the host logger capture details.
            context.RequestServices
                .GetRequiredService<ILogger<ApiExceptionMiddleware>>()
                .LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);
        }
    }
}
