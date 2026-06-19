using EventosVivos.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace EventosVivos.Api.Middleware;

/// <summary>
/// Middleware centralizado de manejo de errores.
/// Traduce excepciones de dominio a respuestas HTTP semánticas y
/// protege detalles de implementación de clientes externos.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var (statusCode, title, detail, extra) = exception switch
        {
            EntityNotFoundException e =>
                (HttpStatusCode.NotFound, "Recurso no encontrado", e.Message, (object?)null),

            BusinessRuleViolationException e =>
                (HttpStatusCode.UnprocessableEntity, "Regla de negocio violada", e.Message,
                 new { ruleCode = e.RuleCode }),

            InvalidStateTransitionException e =>
                (HttpStatusCode.Conflict, "Transición de estado inválida", e.Message, (object?)null),

            _ =>
                (HttpStatusCode.InternalServerError, "Error interno del servidor",
                 "Ocurrió un error inesperado. Por favor intente nuevamente.", (object?)null)
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var problemDetails = new
        {
            status = (int)statusCode,
            title,
            detail,
            extra,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
