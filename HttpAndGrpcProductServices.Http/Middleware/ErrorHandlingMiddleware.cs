using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HttpAndGrpcProductServices.Http.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ErrorHandlingMiddleware> logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Ошибка валидации запроса");
            await WriteProblemDetailsAsync(context, HttpStatusCode.BadRequest,
                "Ошибка валидации", ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Непредвиденная ошибка");
            await WriteProblemDetailsAsync(context, HttpStatusCode.InternalServerError,
                "Внутренняя ошибка сервера", new[] { "Произошла непредвиденная ошибка" });
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        IEnumerable<string> errors)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = string.Join("; ", errors),
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}