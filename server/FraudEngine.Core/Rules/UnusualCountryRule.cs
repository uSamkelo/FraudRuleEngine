using System;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;

namespace FraudEngine.Core.Rules
{
    // Flags a transaction originating in a country other than the account's
    // registered home country.
    public class UnusualCountryRule : IFraudRule
    {
        private readonly IRepository _repo;

        public string Name => nameof(UnusualCountryRule);

        public UnusualCountryRule(IRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
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

            if (tx.CountryCode != account.DefaultCountryCode)
            {
                var alert = new FraudAlert
                {
                    TransactionId = tx.Id,
                    RuleName = Name,
                    Reason = $"Transaction in {tx.CountryCode} differs from account home country {account.DefaultCountryCode}",
                    Severity = AlertSeverity.High
                };

                return new[] { alert };
            }

            return Array.Empty<FraudAlert>();
        }
    }
}
