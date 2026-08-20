using System;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using Microsoft.Extensions.Options;

namespace FraudEngine.Core.Rules
{
    // Flags when the cumulative spend for an account over a rolling window (including
    // the in-flight transaction) exceeds a threshold, catching "structuring" patterns
    // that individual transactions (e.g. HighAmountRule) would miss.
    public class VelocityAmountRule : IFraudRule
    {
        private readonly IRepository _repo;
        private readonly decimal _threshold;
        private readonly TimeSpan _window;

        public string Name => nameof(VelocityAmountRule);

        public VelocityAmountRule(IRepository repo, IOptions<RuleOptions> options)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _threshold = options.Value.VelocityAmountThreshold;
            _window = options.Value.VelocityAmountWindow;
        }

        public async Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            // Every real caller (TransactionsController, DbSeeder) persists the current
            // transaction before evaluating rules against it, so it is already included
            // in the query result below - do not add tx.Amount again here.
            var recent = await _repo.GetRecentTransactionsByAccountAsync(tx.AccountId, _window);
            var total = recent?.Sum(t => t.Amount) ?? 0m;

            if (total >= _threshold)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Total spend of {total} in {_window.TotalHours}h exceeds threshold {_threshold}",
                    Severity = AlertSeverity.High
                };

                return new[] { alert };
            }

            return Array.Empty<FraudAlert>();
        }
    }
}
