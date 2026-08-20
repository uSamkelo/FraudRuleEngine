using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using FraudEngine.Tests.TestDoubles;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class UnusualCountryRuleTests
    {
        [Fact]
        public async Task EvaluateAsync_CountryDiffersFromAccountHomeCountry_ProducesHighSeverityAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA" });
            var rule = new UnusualCountryRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty, CountryCode = "NG" };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(UnusualCountryRule), alert.RuleName);
            Assert.Equal(AlertSeverity.High, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_CountryMatchesAccountHomeCountry_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            await repo.AddAccountAsync(new Account { AccountId = "acct-1", OwnerId = "owner-1", DefaultCountryCode = "ZA" });
            var rule = new UnusualCountryRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty, CountryCode = "ZA" };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_AccountNotFound_ProducesNoAlert()
        {
            var repo = new InMemoryRepository();
            var rule = new UnusualCountryRule(repo);

            var tx = new TransactionEvent { AccountId = "acct-unknown", Amount = 100m, Metadata = string.Empty, CountryCode = "NG" };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
