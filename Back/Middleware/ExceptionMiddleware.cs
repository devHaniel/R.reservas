using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Reservas.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteProblemResponseAsync(context, exception);
        }
    }

    private async Task WriteProblemResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                exception.Message),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                exception.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (
                StatusCodes.Status409Conflict,
                "Conflicto de datos",
                "Ya existe un registro con esos datos (email o teléfono duplicado)."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation } } => (
                StatusCodes.Status409Conflict,
                "Conflicto de horario",
                "El recurso ya está reservado en el horario indicado."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } => (
                StatusCodes.Status409Conflict,
                "Conflicto de datos",
                "No se puede completar la operación porque el registro está en uso."),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "No tienes permiso para realizar esta operación."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "Ocurrió un error inesperado.")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}