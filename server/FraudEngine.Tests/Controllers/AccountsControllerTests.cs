using System.Threading.Tasks;
using FraudEngine.Api.Controllers;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using FraudEngine.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FraudEngine.Tests.Controllers
{
    public class AccountsControllerTests
    {
        private static AccountRequest ValidRequest(string accountId = "acct-1") => new()
        {
            AccountId = accountId,
            AccountType = AccountType.Savings,
            RiskTier = RiskTier.Low,
            OwnerId = "owner-1",
            DefaultCountryCode = "ZA"
        };

        [Fact]
        public async Task Post_ValidRequest_ReturnsCreatedWithAccountResponse()
        {
            var repo = new InMemoryRepository();
            var controller = new AccountsController(repo);

            var result = await controller.Post(ValidRequest());

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(AccountsController.GetById), created.ActionName);

            var response = Assert.IsType<AccountResponse>(created.Value);
            Assert.Equal("acct-1", response.AccountId);
            Assert.Equal(AccountType.Savings, response.AccountType);
            Assert.Equal(RiskTier.Low, response.RiskTier);
            Assert.Equal("owner-1", response.OwnerId);
            Assert.Equal("ZA", response.DefaultCountryCode);

            var stored = await repo.GetAccountAsync("acct-1");
            Assert.NotNull(stored);
            Assert.Equal("owner-1", stored.OwnerId);
        }

        [Fact]
        public async Task Post_DuplicateAccountId_ReturnsConflictAndLeavesOriginalUntouched()
        {
            var repo = new InMemoryRepository();
            var controller = new AccountsController(repo);

            await controller.Post(ValidRequest());

            var duplicateRequest = ValidRequest();
            duplicateRequest.OwnerId = "owner-2";
            duplicateRequest.RiskTier = RiskTier.High;

            var result = await controller.Post(duplicateRequest);

            Assert.IsType<ConflictObjectResult>(result);

            var stored = await repo.GetAccountAsync("acct-1");
            Assert.NotNull(stored);
            Assert.Equal("owner-1", stored.OwnerId);
            Assert.Equal(RiskTier.Low, stored.RiskTier);
        }

        [Fact]
        public async Task GetById_ExistingAccount_ReturnsOkWithMatchingFields()
        {
            var repo = new InMemoryRepository();
            var controller = new AccountsController(repo);
            await controller.Post(ValidRequest());

            var result = await controller.GetById("acct-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccountResponse>(ok.Value);
            Assert.Equal("acct-1", response.AccountId);
            Assert.Equal(AccountType.Savings, response.AccountType);
            Assert.Equal(RiskTier.Low, response.RiskTier);
            Assert.Equal("owner-1", response.OwnerId);
            Assert.Equal("ZA", response.DefaultCountryCode);
        }

        [Fact]
        public async Task GetById_MissingAccount_ReturnsNotFound()
        {
            var repo = new InMemoryRepository();
            var controller = new AccountsController(repo);

            var result = await controller.GetById("nonexistent");

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
