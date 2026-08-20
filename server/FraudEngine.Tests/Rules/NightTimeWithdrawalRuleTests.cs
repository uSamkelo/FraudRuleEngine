using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class NightTimeWithdrawalRuleTests
    {
        private static readonly DateTimeOffset TwoAmUtc = new(2026, 1, 1, 2, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset TwoPmUtc = new(2026, 1, 1, 14, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task EvaluateAsync_AtmWithdrawalAt2AmOnHighRiskAccount_ProducesMediumSeverityAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA", RiskTier = RiskTier.High });
            var rule = new NightTimeWithdrawalRule(repo);

            var tx = new TransactionEvent
            {
                AccountId = "acct-1",
                Amount = 500m,
                Metadata = string.Empty,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.ATM,
                Timestamp = TwoAmUtc
            };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(NightTimeWithdrawalRule), alert.RuleName);
            Assert.Equal(AlertSeverity.Medium, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_AtmWithdrawalAt2PmOnHighRiskAccount_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA", RiskTier = RiskTier.High });
            var rule = new NightTimeWithdrawalRule(repo);

            var tx = new TransactionEvent
            {
                AccountId = "acct-1",
                Amount = 500m,
                Metadata = string.Empty,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.ATM,
                Timestamp = TwoPmUtc
            };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_AtmWithdrawalAt2AmOnLowRiskAccount_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA", RiskTier = RiskTier.Low });
            var rule = new NightTimeWithdrawalRule(repo);

            var tx = new TransactionEvent
            {
                AccountId = "acct-1",
                Amount = 500m,
                Metadata = string.Empty,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.ATM,
                Timestamp = TwoAmUtc
            };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_NonAtmChannelAt2AmOnHighRiskAccount_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA", RiskTier = RiskTier.High });
            var rule = new NightTimeWithdrawalRule(repo);

            var tx = new TransactionEvent
            {
                AccountId = "acct-1",
                Amount = 500m,
                Metadata = string.Empty,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.Online,
                Timestamp = TwoAmUtc
            };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
