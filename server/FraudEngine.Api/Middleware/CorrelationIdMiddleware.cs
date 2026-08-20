using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace FraudEngine.Api.Middleware
{
    /// <summary>
    /// Ensures every request carries a correlation id: reads it from the incoming
    /// <c>X-Correlation-Id</c> request header (generating a new one if absent),
    /// stores it on <see cref="HttpContext.Items"/>, pushes it onto the Serilog
    /// <see cref="LogContext"/> so it's attached to every log entry produced while
    /// handling the request, and echoes it back on the response.
    /// Registered as the very first middleware in the pipeline (before
    /// <c>GlobalExceptionMiddleware</c> and <c>UseRouting</c>) so the correlation id
    /// is present even for requests that end up in the exception handler.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private const string ItemsKey = "CorrelationId";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
                                 !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

            context.Items[ItemsKey] = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
