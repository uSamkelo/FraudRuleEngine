using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class VelocityAmountRuleTests
    {
        private static VelocityAmountRule CreateRule(InMemoryRepository repo, decimal threshold, TimeSpan? window = null)
        {
            var options = Options.Create(new RuleOptions
            {
                VelocityAmountThreshold = threshold,
                VelocityAmountWindow = window ?? TimeSpan.FromHours(24)
            });

            return new VelocityAmountRule(repo, options);
        }

        [Fact]
        public async Task EvaluateAsync_CumulativeAmountExceedsThreshold_ProducesHighSeverityAlert()
        {
            var repo = new InMemoryRepository();
            var rule = CreateRule(repo, threshold: 50000m);

            await repo.AddTransactionAsync(new TransactionEvent { AccountId = "acct-1", Amount = 20000m, Metadata = string.Empty });
            await repo.AddTransactionAsync(new TransactionEvent { AccountId = "acct-1", Amount = 20000m, Metadata = string.Empty });

            // In-flight transaction, not yet persisted: 20000 + 20000 + 15000 = 55000 >= 50000
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 15000m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(VelocityAmountRule), alert.RuleName);
            Assert.Equal(AlertSeverity.High, alert.Severity);
            Assert.Equal(tx.Id, alert.TransactionId);
        }

        [Fact]
        public async Task EvaluateAsync_CumulativeAmountBelowThreshold_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            var rule = CreateRule(repo, threshold: 50000m);

            await repo.AddTransactionAsync(new TransactionEvent { AccountId = "acct-1", Amount = 1000m, Metadata = string.Empty });

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 500m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_DoesNotDoubleCountTheInFlightTransaction()
        {
            var repo = new InMemoryRepository();
            // Correct total is 300 + 200 = 500, which is below this threshold. A rule
            // that mistakenly summed the in-flight transaction's amount twice would
            // compute 700 and incorrectly fire.
            var rule = CreateRule(repo, threshold: 700m);

            await repo.AddTransactionAsync(new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty });
            await repo.AddTransactionAsync(new TransactionEvent { AccountId = "acct-1", Amount = 200m, Metadata = string.Empty });

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 200m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
