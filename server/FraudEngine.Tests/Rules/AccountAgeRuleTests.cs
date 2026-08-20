using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class AccountAgeRuleTests
    {
        private static AccountAgeRule CreateRule(InMemoryRepository repo)
        {
            var options = Options.Create(new RuleOptions
            {
                AccountAgeThresholdDays = 30,
                AccountAgeLargeAmountThreshold = 5000m
            });

            return new AccountAgeRule(repo, options);
        }

        [Fact]
        public async Task EvaluateAsync_TenDayOldAccountWithLargeAmount_ProducesHighSeverityAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account
            {
                AccountId = "acct-1",
                OwnerId = "owner-1",
                DefaultCountryCode = "ZA",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            });
            var rule = CreateRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 6000m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(AccountAgeRule), alert.RuleName);
            Assert.Equal(AlertSeverity.High, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_SixtyDayOldAccountWithLargeAmount_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account
            {
                AccountId = "acct-1",
                OwnerId = "owner-1",
                DefaultCountryCode = "ZA",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-60)
            });
            var rule = CreateRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 6000m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_NewAccountWithSmallAmount_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account
            {
                AccountId = "acct-1",
                OwnerId = "owner-1",
                DefaultCountryCode = "ZA",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            });
            var rule = CreateRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
