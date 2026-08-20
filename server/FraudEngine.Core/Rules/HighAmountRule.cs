using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using Microsoft.Extensions.Options;

namespace FraudEngine.Core.Rules
{
    public class HighAmountRule : IFraudRule
    {
        private readonly decimal _threshold;
        public string Name => nameof(HighAmountRule);

        public HighAmountRule(IOptions<RuleOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _threshold = options.Value.HighAmountThreshold;
        }

        public Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            if (tx.Amount >= _threshold)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Amount {tx.Amount} exceeds threshold {_threshold}",
                    Severity = AlertSeverity.High
                };

                return Task.FromResult(new[] { alert });
            }

            return Task.FromResult(Array.Empty<FraudAlert>());
        }
    }
}
