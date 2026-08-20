using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class RapidTransactionsRuleTests
    {
        private static RapidTransactionsRule CreateRule(InMemoryRepository repo, int countThreshold, TimeSpan window)
        {
            var options = Options.Create(new RuleOptions
            {
                RapidTransactionCount = countThreshold,
                RapidTransactionWindow = window
            });

            return new RapidTransactionsRule(repo, options);
        }

        [Fact]
        public async Task EvaluateAsync_FewerThanThreshold_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            var rule = CreateRule(repo, countThreshold: 5, window: TimeSpan.FromMinutes(1));

            TransactionEvent latest = null!;
            for (var i = 0; i < 3; i++)
            {
                latest = new TransactionEvent { AccountId = "acct-1", Amount = 20m, Metadata = string.Empty };
                await repo.AddTransactionAsync(latest);
            }

            var alerts = await rule.EvaluateAsync(latest);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_ThresholdReached_ProducesMediumSeverityAlert()
        {
            var repo = new InMemoryRepository();
            var rule = CreateRule(repo, countThreshold: 5, window: TimeSpan.FromMinutes(1));

            TransactionEvent latest = null!;
            for (var i = 0; i < 5; i++)
            {
                latest = new TransactionEvent { AccountId = "acct-1", Amount = 20m, Metadata = string.Empty };
                await repo.AddTransactionAsync(latest);
            }

            var alerts = await rule.EvaluateAsync(latest);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(RapidTransactionsRule), alert.RuleName);
            Assert.Equal(AlertSeverity.Medium, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_TransactionsOutsideWindow_AreIgnored()
        {
            var repo = new InMemoryRepository();
            var rule = CreateRule(repo, countThreshold: 3, window: TimeSpan.FromSeconds(30));

            // Old transactions, well outside the lookback window, should not count.
            for (var i = 0; i < 5; i++)
            {
                var oldTx = new TransactionEvent
                {
                    AccountId = "acct-1",
                    Amount = 20m,
                    Metadata = string.Empty,
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
                };
                await repo.AddTransactionAsync(oldTx);
            }

            var recentTx = new TransactionEvent { AccountId = "acct-1", Amount = 20m, Metadata = string.Empty };
            await repo.AddTransactionAsync(recentTx);

            var alerts = await rule.EvaluateAsync(recentTx);

            Assert.Empty(alerts);
        }
    }
}
