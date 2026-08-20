using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FraudEngine.Core.Data;
using FraudEngine.Core.Models;

namespace FraudEngine.Core.Repositories
{
    public class EfRepository : IRepository
    {
        private readonly FraudDbContext _db;

        public EfRepository(FraudDbContext db)
        {
            _db = db;
        }

        public async Task AddTransactionAsync(TransactionEvent tx)
        {
            await _db.Transactions.AddAsync(tx);
            await _db.SaveChangesAsync();
        }

        public async Task<TransactionEvent> GetTransactionAsync(Guid id)
        {
            // FindAsync returns null when no matching row exists; callers are expected
            // to check for null (see TransactionsController), so the null-forgiving
            // operator here just keeps the interface signature non-nullable rather
            // than silently masking a bug.
            return (await _db.Transactions.FindAsync(id))!;
        }

        public async Task<IEnumerable<TransactionEvent>> GetRecentTransactionsByAccountAsync(string accountId, TimeSpan lookback)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(lookback);
            return await _db.Transactions
                .Where(t => t.AccountId == accountId && t.Timestamp >= cutoff)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        public async Task AddAlertAsync(FraudAlert alert)
        {
            await _db.Alerts.AddAsync(alert);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<FraudAlert>> GetAlertsAsync()
        {
            return await _db.Alerts.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task<Account> GetAccountAsync(string accountId)
        {
            // See GetTransactionAsync for why the null-forgiving operator is used here.
            return (await _db.Accounts.FindAsync(accountId))!;
        }

        public async Task AddAccountAsync(Account account)
        {
            await _db.Accounts.AddAsync(account);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<TransactionEvent>> GetTransactionsByAccountAsync(string accountId, int page, int pageSize)
        {
            // 1-based page numbering; page <= 0 is treated as the first page.
            var skip = Math.Max(page - 1, 0) * pageSize;
            return await _db.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctCountriesByAccountAsync(string accountId)
        {
            return await _db.Transactions
                .Where(t => t.AccountId == accountId)
                .Select(t => t.CountryCode)
                .Distinct()
                .ToListAsync();
        }
    }
}
