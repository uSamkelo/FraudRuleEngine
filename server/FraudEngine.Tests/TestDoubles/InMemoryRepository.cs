using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;

namespace FraudEngine.Tests.TestDoubles
{
    /// <summary>
    /// Minimal in-memory <see cref="IRepository"/> double used to unit test rules
    /// without needing a real database.
    /// </summary>
    public class InMemoryRepository : IRepository
    {
        private readonly List<TransactionEvent> _transactions = new();
        private readonly List<FraudAlert> _alerts = new();
        private readonly List<Account> _accounts = new();

        public Task AddTransactionAsync(TransactionEvent tx)
        {
            _transactions.Add(tx);
            return Task.CompletedTask;
        }

        public Task<TransactionEvent> GetTransactionAsync(Guid id)
        {
            var tx = _transactions.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(tx!);
        }

        public Task<IEnumerable<TransactionEvent>> GetRecentTransactionsByAccountAsync(string accountId, TimeSpan lookback)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(lookback);
            var result = _transactions
                .Where(t => t.AccountId == accountId && t.Timestamp >= cutoff)
                .OrderByDescending(t => t.Timestamp)
                .AsEnumerable();

            return Task.FromResult(result);
        }

        public Task AddAlertAsync(FraudAlert alert)
        {
            _alerts.Add(alert);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<FraudAlert>> GetAlertsAsync()
        {
            return Task.FromResult(_alerts.OrderByDescending(a => a.CreatedAt).AsEnumerable());
        }

        public Task<Account> GetAccountAsync(string accountId)
        {
            var account = _accounts.FirstOrDefault(a => a.AccountId == accountId);
            return Task.FromResult(account!);
        }

        public Task AddAccountAsync(Account account)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TransactionEvent>> GetTransactionsByAccountAsync(string accountId, int page, int pageSize)
        {
            var skip = Math.Max(page - 1, 0) * pageSize;
            var result = _transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .AsEnumerable();

            return Task.FromResult(result);
        }

        public Task<IEnumerable<string>> GetDistinctCountriesByAccountAsync(string accountId)
        {
            var result = _transactions
                .Where(t => t.AccountId == accountId)
                .Select(t => t.CountryCode)
                .Distinct()
                .AsEnumerable();

            return Task.FromResult(result);
        }
    }
}
