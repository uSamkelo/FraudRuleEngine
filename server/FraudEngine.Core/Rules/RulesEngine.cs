using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudEngine.Core.Models;

namespace FraudEngine.Core.Rules
{
    public class RulesEngine
    {
        private readonly IEnumerable<IFraudRule> _rules;

        public RulesEngine(IEnumerable<IFraudRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public async Task<IEnumerable<FraudAlert>> EvaluateAsync(TransactionEvent tx)
        {
            var results = new List<FraudAlert>();
            foreach (var rule in _rules)
            {
                var alerts = await rule.EvaluateAsync(tx);
                if (alerts != null && alerts.Length > 0)
                {
                    results.AddRange(alerts);
                }
            }

            return results;
        }
    }
}
