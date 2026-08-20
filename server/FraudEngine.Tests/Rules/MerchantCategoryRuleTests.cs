using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Rules;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraudEngine.Tests.Rules
{
    public class MerchantCategoryRuleTests
    {
        private static MerchantCategoryRule CreateRule()
        {
            return new MerchantCategoryRule(Options.Create(new RuleOptions()));
        }

        [Fact]
        public async Task EvaluateAsync_HighRiskMcc7995_ProducesHighSeverityAlert()
        {
            var rule = CreateRule();
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty, MerchantCategoryCode = "7995" };

            var alerts = await rule.EvaluateAsync(tx);

            var alert = Assert.Single(alerts);
            Assert.Equal(nameof(MerchantCategoryRule), alert.RuleName);
            Assert.Equal(AlertSeverity.High, alert.Severity);
        }

        [Fact]
        public async Task EvaluateAsync_LowRiskMcc5411_ProducesNoAlert()
        {
            var rule = CreateRule();
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty, MerchantCategoryCode = "5411" };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }

        [Fact]
        public async Task EvaluateAsync_NullMerchantCategoryCode_ProducesNoAlert()
        {
            var rule = CreateRule();
            var tx = new TransactionEvent { AccountId = "acct-1", Amount = 100m, Metadata = string.Empty, MerchantCategoryCode = null };

            var alerts = await rule.EvaluateAsync(tx);

            Assert.Empty(alerts);
        }
    }
}
