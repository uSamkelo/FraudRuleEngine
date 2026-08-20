using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;

namespace FraudEngine.Core.Rules
{
    // Flags ATM withdrawals made between midnight and 4am UTC on High-risk accounts.
    public class NightTimeWithdrawalRule : IFraudRule
    {
        private readonly IRepository _repo;

        public string Name => nameof(NightTimeWithdrawalRule);

        public NightTimeWithdrawalRule(IRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<FraudAlert[]> EvaluateAsync(TransactionEvent tx)
        {
            if (tx.Category != TransactionCategory.Withdrawal || tx.Channel != Channel.ATM)
            {
                return Array.Empty<FraudAlert>();
            }

            var account = await _repo.GetAccountAsync(tx.AccountId);

            // GetAccountAsync's signature is non-nullable, but the EF implementation
            // can still return null at runtime (see IRepository/EfRepository) - always
            // check regardless of what the compiler thinks it knows.
            if (account is null)
            {
                return Array.Empty<FraudAlert>();
            }

            if (account.RiskTier != RiskTier.High)
            {
                return Array.Empty<FraudAlert>();
            }

            var hour = tx.Timestamp.UtcDateTime.Hour;
            if (hour >= 0 && hour < 4)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"ATM withdrawal at {tx.Timestamp:HH:mm} UTC on a High-risk account",
                    Severity = AlertSeverity.Medium
                };

                return new[] { alert };
            }

            return Array.Empty<FraudAlert>();
        }
    }
}
