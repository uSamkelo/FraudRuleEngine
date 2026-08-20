using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using Microsoft.Extensions.Options;

namespace FraudEngine.Core.Rules
{
    // Flags large transactions on recently-opened accounts, a common pattern for
    // mule/fraud accounts opened specifically to move money quickly.
    public class AccountAgeRule : IFraudRule
    {
        private readonly IRepository _repo;
        private readonly RuleOptions _options;

        public string Name => nameof(AccountAgeRule);

        public AccountAgeRule(IRepository repo, IOptions<RuleOptions> options)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options.Value;
        }

        public async Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            var account = await _repo.GetAccountAsync(tx.AccountId);

            // GetAccountAsync's signature is non-nullable, but the EF implementation
            // can still return null at runtime (see IRepository/EfRepository) - always
            // check regardless of what the compiler thinks it knows.
            if (account is null)
            {
                return Array.Empty<FraudAlert>();
            }

            var accountAgeDays = (DateTimeOffset.UtcNow - account.CreatedAt).TotalDays;

            if (accountAgeDays < _options.AccountAgeThresholdDays && tx.Amount >= _options.AccountAgeLargeAmountThreshold)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Account {tx.AccountId} is {accountAgeDays:F0} days old; large transaction of {tx.Amount} flagged",
                    Severity = AlertSeverity.High
                };

                return new[] { alert };
            }

            return Array.Empty<FraudAlert>();
        }
    }
}
