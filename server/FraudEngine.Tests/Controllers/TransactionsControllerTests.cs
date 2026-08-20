using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Api.Controllers;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private static TransactionsController CreateController(
            InMemoryRepository repo, IEnumerable<IFraudRule>? rules = null)
        {
            var engine = new RulesEngine(rules ?? Array.Empty<IFraudRule>(), NullLogger<RulesEngine>.Instance);
            return new TransactionsController(repo, engine);
        }

        private static TransactionRequest ValidRequest(string accountId = "acct-1") => new()
        {
            AccountId = accountId,
            Amount = 100m,
            Category = TransactionCategory.Payment,
            Currency = "ZAR",
            CountryCode = "ZA"
        };

        [Fact]
        public async Task Post_ValidRequest_ReturnsCreatedWithTransactionAndAlerts()
        {
            var repo = new InMemoryRepository();
            var controller = CreateController(repo);

            var result = await controller.Post(ValidRequest());

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(TransactionsController.GetById), created.ActionName);

            var body = created.Value!;
            var transactionProp = body.GetType().GetProperty("transaction");
            var alertsProp = body.GetType().GetProperty("alerts");
            Assert.NotNull(transactionProp);
            Assert.NotNull(alertsProp);

            var transaction = Assert.IsType<TransactionResponse>(transactionProp!.GetValue(body));
            Assert.Equal("acct-1", transaction.AccountId);
            Assert.Equal(100m, transaction.Amount);

            var alerts = Assert.IsAssignableFrom<IEnumerable<AlertResponse>>(alertsProp!.GetValue(body));
            Assert.Empty(alerts);
        }

        [Fact]
        public async Task Post_TriggeringRule_ReturnsAlertsInResponse()
        {
            var repo = new InMemoryRepository();
            var rule = new HighAmountRule(Options.Create(new RuleOptions { HighAmountThreshold = 50m }));
            var controller = CreateController(repo, new[] { rule });

            var result = await controller.Post(ValidRequest());

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var alertsProp = created.Value!.GetType().GetProperty("alerts");
            var alerts = Assert.IsAssignableFrom<IEnumerable<AlertResponse>>(alertsProp!.GetValue(created.Value));

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(HighAmountRule), alert.RuleName);
        }

        [Fact]
        public async Task GetById_ExistingTransaction_ReturnsOkWithTransactionResponse()
        {
            var repo = new InMemoryRepository();
            var controller = CreateController(repo);
            await controller.Post(ValidRequest());

            var listResult = Assert.IsType<OkObjectResult>(await controller.GetAll(null, null, null, null, 1, 20));
            var paged = Assert.IsType<PagedResult<TransactionResponse>>(listResult.Value);
            var id = paged.Items.Single().Id;

            var result = await controller.GetById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<TransactionResponse>(ok.Value);
            Assert.Equal(id, response.Id);
        }

        [Fact]
        public async Task GetById_MissingTransaction_ReturnsNotFound()
        {
            var repo = new InMemoryRepository();
            var controller = CreateController(repo);

            var result = await controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAll_UnknownAccountId_ReturnsOkWithEmptyItems()
        {
            var repo = new InMemoryRepository();
            var controller = CreateController(repo);
            await controller.Post(ValidRequest("acct-1"));

            var result = await controller.GetAll("nonexistent", null, null, null, 1, 20);

            var ok = Assert.IsType<OkObjectResult>(result);
            var paged = Assert.IsType<PagedResult<TransactionResponse>>(ok.Value);
            Assert.Empty(paged.Items);
            Assert.Equal(0, paged.TotalCount);
        }

        [Fact]
        public async Task GetAll_ReturnsPagedResultShapeWithPageAndPageSize()
        {
            var repo = new InMemoryRepository();
            var controller = CreateController(repo);
            await controller.Post(ValidRequest());

            var result = await controller.GetAll(null, null, null, null, 1, 5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var paged = Assert.IsType<PagedResult<TransactionResponse>>(ok.Value);
            Assert.Equal(1, paged.Page);
            Assert.Equal(5, paged.PageSize);
            Assert.Equal(1, paged.TotalCount);
            Assert.Single(paged.Items);
        }

        [Fact]
        public async Task GetAlerts_FiltersByStatus()
        {
            var repo = new InMemoryRepository();
            var rule = new HighAmountRule(Options.Create(new RuleOptions { HighAmountThreshold = 50m }));
            var controller = CreateController(repo, new[] { rule });
            await controller.Post(ValidRequest());

            var openResult = Assert.IsType<OkObjectResult>(await controller.GetAlerts(AlertStatus.Open));
            var openAlerts = Assert.IsAssignableFrom<IEnumerable<AlertResponse>>(openResult.Value);
            Assert.Single(openAlerts);

            var resolvedResult = Assert.IsType<OkObjectResult>(await controller.GetAlerts(AlertStatus.Resolved));
            var resolvedAlerts = Assert.IsAssignableFrom<IEnumerable<AlertResponse>>(resolvedResult.Value);
            Assert.Empty(resolvedAlerts);
        }
    }
}
