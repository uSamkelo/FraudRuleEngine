using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudEngine.Core.Models;

namespace FraudEngine.Core.Repositories
{
    public interface IRepository
    {
        Task AddTransactionAsync(TransactionEvent tx);
        Task<IEnumerable<TransactionEvent>> GetRecentTransactionsByAccountAsync(string accountId, TimeSpan lookback);
        Task AddAlertAsync(FraudAlert alert);
        Task<IEnumerable<FraudAlert>> GetAlertsAsync();
        Task<TransactionEvent> GetTransactionAsync(Guid id);
    }
}
