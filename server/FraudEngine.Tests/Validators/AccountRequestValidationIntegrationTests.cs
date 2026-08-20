using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using FraudEngine.Api.Controllers;
using FraudEngine.Api.Validators;
using FraudEngine.Core.Repositories;
using FraudEngine.Tests.TestDoubles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FraudEngine.Tests.Validators
{
    /// <summary>
    /// Verifies <see cref="AccountRequestValidator"/> is actually invoked end-to-end
    /// through the real ASP.NET Core model-binding/validation pipeline, rather than
    /// just assuming it's picked up by the existing
    /// <c>AddValidatorsFromAssemblyContaining&lt;TransactionRequestValidator&gt;()</c>
    /// assembly scan in <c>Program.cs</c>. Boots a minimal test host (controllers +
    /// FluentValidation auto-validation + an in-memory repository) instead of the
    /// full app, since <c>Program.cs</c> requires a live Postgres connection that
    /// isn't available in this environment.
    /// </summary>
    public class AccountRequestValidationIntegrationTests
    {
        private static async Task<HttpClient> CreateClientAsync()
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSingleton<IRepository>(new InMemoryRepository());
                        services.AddControllers()
                            .AddApplicationPart(typeof(AccountsController).Assembly);
                        services.AddFluentValidationAutoValidation();
                        services.AddValidatorsFromAssemblyContaining<AccountRequestValidator>();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .StartAsync();

            return host.GetTestClient();
        }

        [Fact]
        public async Task Post_InvalidAccountRequest_ReturnsBadRequest()
        {
            var client = await CreateClientAsync();

            var response = await client.PostAsJsonAsync("/api/accounts", new
            {
                accountId = "",
                ownerId = "",
                defaultCountryCode = "not-a-code"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_ValidAccountRequest_ReturnsCreated()
        {
            var client = await CreateClientAsync();

            var response = await client.PostAsJsonAsync("/api/accounts", new
            {
                accountId = "acct-int-1",
                ownerId = "owner-1",
                defaultCountryCode = "ZA"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}
