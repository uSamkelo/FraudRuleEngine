using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class HighAmountRuleTests
    {
        private static HighAmountRule CreateRule(decimal threshold = 10000m)
        {
            return new HighAmountRule(Options.Create(new RuleOptions { HighAmountThreshold = threshold }));
        }

        [Fact]
        public async Task EvaluateAsync_AmountAboveThreshold_ProducesHighSeverityAlert()
        {
            var rule = CreateRule(threshold: 10000m);
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 15000m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(HighAmountRule), alert.RuleName);
            Assert.Equal(tx.Id, alert.TransactionId);
            Assert.Equal(AlertSeverity.High, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_AmountEqualsThreshold_ProducesAlert()
        {
            var rule = CreateRule(threshold: 10000m);
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 10000m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Single(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_AmountBelowThreshold_ProducesNoAlert()
        {
            var rule = CreateRule(threshold: 10000m);
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 500m, Metadata = string.Empty };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
