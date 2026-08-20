using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Api.Middleware
{
    /// <summary>
    /// Catches any exception that escapes controller/action execution and converts it
    /// into a standard RFC 7807 <see cref="ProblemDetails"/> response instead of the
    /// default ASP.NET Core HTML error page (or an unhandled 500 with no body).
    /// Must be registered early in the pipeline (before <c>UseRouting</c>) so it wraps
    /// everything downstream.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = MapException(exception);

            _logger.LogError(exception, "Unhandled exception processing {Method} {Path} -> {StatusCode}",
                context.Request.Method, context.Request.Path, statusCode);

            // If the response has already started (e.g. the exception was thrown
            // mid-stream while writing a large body), headers/status can no longer be
            // modified - attempting to would throw a second InvalidOperationException
            // that masks the original exception. Just log and give up gracefully.
            if (context.Response.HasStarted)
            {
                return;
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // Exception messages can contain internal details (stack traces, SQL,
                // file paths, etc.) - only surface them in Development.
                Detail = _environment.IsDevelopment() ? exception.Message : null,
                Extensions = new Dictionary<string, object?>
                {
                    ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
                }
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }

        private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
    }
}
