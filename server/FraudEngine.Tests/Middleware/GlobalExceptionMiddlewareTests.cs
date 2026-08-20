using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FraudEngine.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FraudEngine.Tests.Middleware
{
    /// <summary>
    /// Minimal <see cref="IHostEnvironment"/> test double so tests can control
    /// Development vs. Production behavior without spinning up a full host.
    /// </summary>
    file class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "FraudEngine.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    public class GlobalExceptionMiddlewareTests
    {
        private static async Task<(int StatusCode, ProblemDetails Body)> InvokeAsync(
            RequestDelegate next, IHostEnvironment environment)
        {
            var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance, environment);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body);

            return (context.Response.StatusCode, body!);
        }

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_MapsTo404()
        {
            RequestDelegate next = _ => throw new KeyNotFoundException("alert missing");

            var (statusCode, body) = await InvokeAsync(next, new FakeHostEnvironment());

            Assert.Equal(StatusCodes.Status404NotFound, statusCode);
            Assert.Equal(StatusCodes.Status404NotFound, body.Status);
        }

        [Fact]
        public async Task InvokeAsync_UnhandledException_MapsTo500()
        {
            RequestDelegate next = _ => throw new InvalidOperationException("boom");

            var (statusCode, body) = await InvokeAsync(next, new FakeHostEnvironment());

            Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
            Assert.Equal(StatusCodes.Status500InternalServerError, body.Status);
        }

        [Fact]
        public async Task InvokeAsync_Production_OmitsExceptionMessageFromDetail()
        {
            RequestDelegate next = _ => throw new InvalidOperationException("sensitive internal detail");

            var (_, body) = await InvokeAsync(next, new FakeHostEnvironment { EnvironmentName = Environments.Production });

            Assert.Null(body.Detail);
        }

        [Fact]
        public async Task InvokeAsync_Development_IncludesExceptionMessageInDetail()
        {
            RequestDelegate next = _ => throw new InvalidOperationException("helpful debug detail");

            var (_, body) = await InvokeAsync(next, new FakeHostEnvironment { EnvironmentName = Environments.Development });

            Assert.Equal("helpful debug detail", body.Detail);
        }

        [Fact]
        public async Task InvokeAsync_NoException_PassesThroughUnchanged()
        {
            var wasCalled = false;
            RequestDelegate next = context =>
            {
                wasCalled = true;
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };

            var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance, new FakeHostEnvironment());
            var context = new DefaultHttpContext();

            await middleware.InvokeAsync(context);

            Assert.True(wasCalled);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }
}
