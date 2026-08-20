using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FraudEngine.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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

    /// <summary>
    /// <see cref="DefaultHttpContext"/>'s built-in <see cref="IHttpResponseFeature"/>
    /// always reports <c>HasStarted == false</c> (there's no real transport backing
    /// it), so it can't be used to simulate "the response already started sending".
    /// This stand-in lets tests force that state deliberately.
    /// </summary>
    file class StartedHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
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
        public async Task InvokeAsync_ExceptionAfterResponseStarted_DoesNotThrowOrModifyStatusCode()
        {
            // Simulates an exception thrown mid-stream, after the response has
            // already started sending (e.g. partway through a large PagedResult).
            // At that point headers/status are locked in, so HandleExceptionAsync
            // must not attempt to touch them - doing so would throw a second
            // InvalidOperationException that masks the original exception.
            RequestDelegate next = _ => throw new InvalidOperationException("boom mid-stream");

            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());

            var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance, new FakeHostEnvironment());

            // The key assertion is implicit: this must not throw. Without the
            // HasStarted guard, HandleExceptionAsync would try to set StatusCode on
            // a response that (per our fake feature) has already started, which in
            // a real server throws InvalidOperationException and masks the original
            // "boom mid-stream" exception.
            await middleware.InvokeAsync(context);

            Assert.True(context.Response.HasStarted);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
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
