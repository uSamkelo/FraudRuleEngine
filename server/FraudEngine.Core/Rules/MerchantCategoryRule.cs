using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using Microsoft.Extensions.Options;

namespace FraudEngine.Core.Rules
{
    // Flags transactions against merchant category codes considered high-risk
    // (e.g. crypto, gambling, pawn shops, jewelry).
    public class MerchantCategoryRule : IFraudRule
    {
        private readonly RuleOptions _options;

        public string Name => nameof(MerchantCategoryRule);

        public MerchantCategoryRule(IOptions<RuleOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options.Value;
        }

        public Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            if (string.IsNullOrEmpty(tx.MerchantCategoryCode))
            {
                return Task.FromResult(Array.Empty<FraudAlert>());
            }

            if (_options.HighRiskMerchantCategoryCodes.Contains(tx.MerchantCategoryCode))
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Merchant category {tx.MerchantCategoryCode} is flagged as high-risk",
                    Severity = AlertSeverity.High
                };

                return Task.FromResult(new[] { alert });
            }

            return Task.FromResult(Array.Empty<FraudAlert>());
        }
    }
}
