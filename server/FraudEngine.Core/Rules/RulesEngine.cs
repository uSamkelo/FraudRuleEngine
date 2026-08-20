using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Core.Rules
{
    public class RulesEngine
    {
        private readonly IEnumerable<IFraudRule> _rules;
        private readonly ILogger<RulesEngine> _logger;

        public RulesEngine(IEnumerable<IFraudRule> rules, ILogger<RulesEngine> logger)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<FraudAlert>> EvaluateAsync(TransactionEvent tx)
        {
            var results = new List<FraudAlert>();
            foreach (var rule in _rules)
            {
                // Isolate each rule's failure: by the time we get here, the
                // transaction has already been persisted by the caller, so letting
                // one rule's exception (e.g. a transient DB error) escape would
                // abort evaluation of every remaining rule with no chance of retry.
                // Log and move on instead.
                FraudAlert[]? alerts;
                try
                {
                    alerts = await rule.EvaluateAsync(tx);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rule {RuleName} threw during evaluation for account {AccountId}", rule.GetType().Name, tx.AccountId);
                    continue;
                }

                if (alerts != null && alerts.Length > 0)
                {
                    foreach (var alert in alerts)
                    {
                        _logger.LogInformation(
                            "Rule {RuleName} triggered for account {AccountId} — severity {Severity}",
                            alert.RuleName, tx.AccountId, alert.Severity);
                    }

                    results.AddRange(alerts);
                }
            }

            return results;
        }
    }
}
