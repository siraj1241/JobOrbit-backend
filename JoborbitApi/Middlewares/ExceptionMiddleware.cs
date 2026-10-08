using System.Net;
using System.Text.Json;
using JobsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (UnauthorizedException ex)
        {
            await Write(ctx, HttpStatusCode.Unauthorized, "Login failed", ex.Message);
        }
        catch (DomainException ex)
        {
            await Write(ctx, HttpStatusCode.BadRequest, "Domain error", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var msg = env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.";
            await Write(ctx, HttpStatusCode.InternalServerError, "Server error", msg);
        }
    }

    private static async Task Write(HttpContext ctx, HttpStatusCode code, string title, string detail)
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = (int)code;
        var problem = new ProblemDetails
        {
            Status = (int)code,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}