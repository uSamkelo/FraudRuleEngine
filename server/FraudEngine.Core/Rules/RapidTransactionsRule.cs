using System;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using Microsoft.Extensions.Options;

namespace FraudEngine.Core.Rules
{
    // Flags when more than N transactions occurred for the same account within a time window
    public class RapidTransactionsRule : IFraudRule
    {
        private readonly IRepository _repo;
        private readonly int _countThreshold;
        private readonly TimeSpan _window;

        public string Name => nameof(RapidTransactionsRule);

        public RapidTransactionsRule(IRepository repo, IOptions<RuleOptions> options)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _countThreshold = options.Value.RapidTransactionCount;
            _window = options.Value.RapidTransactionWindow;
        }

        public async Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            var recent = await _repo.GetRecentTransactionsByAccountAsync(tx.AccountId, _window);
            var count = recent?.Count() ?? 0;

            if (count >= _countThreshold)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Detected {count} transactions for account {tx.AccountId} within {_window.TotalSeconds} seconds",
                    Severity = AlertSeverity.Medium
                };

                return new[] { alert };
            }

            return Array.Empty<FraudAlert>();
        }
    }
}
