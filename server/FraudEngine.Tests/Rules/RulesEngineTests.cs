using System;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    /// <summary>Test double that always throws, to simulate a rule failing mid-evaluation
    /// (e.g. a transient error from a repository call inside the rule).</summary>
    file class ThrowingRule : IFraudRule
    {
        public string Name => nameof(ThrowingRule);

        public Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
            => throw new InvalidOperationException("simulated rule failure");
    }

    /// <summary>Test double that always produces a single alert.</summary>
    file class AlwaysAlertsRule : IFraudRule
    {
        public string Name => nameof(AlwaysAlertsRule);

        public Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
            => Task.FromResult(new[]
            {
                new FraudAlert { TransactionId = tx.Id, RuleName = Name, Severity = AlertSeverity.Low }
            });
    }

    public class RulesEngineTests
    {
        private static TransactionEvent Transaction() => new() { AccountId = "acct-1", Amount = 100m };

        [Fact]
        public async Task EvaluateAsync_OneRuleThrows_OtherRulesStillEvaluatedAndReturned()
        {
            var engine = new RulesEngine(
                new IFraudRule[] { new ThrowingRule(), new AlwaysAlertsRule() },
                NullLogger<RulesEngine>.Instance);

            var alerts = await engine.EvaluateAsync(Transaction());

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(AlwaysAlertsRule), alert.RuleName);
        }

        [Fact]
        public async Task EvaluateAsync_AllRulesThrow_ReturnsEmptyWithoutThrowing()
        {
            var engine = new RulesEngine(
                new IFraudRule[] { new ThrowingRule(), new ThrowingRule() },
                NullLogger<RulesEngine>.Instance);

            var alerts = await engine.EvaluateAsync(Transaction());

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_NoRulesThrow_ReturnsAllAlerts()
        {
            var engine = new RulesEngine(
                new IFraudRule[] { new AlwaysAlertsRule(), new AlwaysAlertsRule() },
                NullLogger<RulesEngine>.Instance);

            var alerts = await engine.EvaluateAsync(Transaction());

            Assert.Equal(2, alerts.Count());
        }
    }
}
