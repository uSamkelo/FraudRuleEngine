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
            return await _db.Transactions.FindAsync(id);
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
    }
}
