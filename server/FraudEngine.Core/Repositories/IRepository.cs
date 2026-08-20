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
        Task<IEnumerable<FraudAlert>> GetAlertsAsync(AlertStatus? status);
        Task<TransactionEvent> GetTransactionAsync(Guid id);
        Task<Account> GetAccountAsync(string accountId);
        Task AddAccountAsync(Account account);
        Task<IEnumerable<TransactionEvent>> GetTransactionsByAccountAsync(string accountId, int page, int pageSize);

        /// <summary>
        /// General-purpose paginated transaction listing with optional filters (all
        /// filter parameters are optional; null means "no filter on that dimension").
        /// Powers <c>GET /api/transactions</c>. Distinct from
        /// <see cref="GetTransactionsByAccountAsync"/>, which mandates an accountId and
        /// is left in place unchanged for existing callers.
        /// </summary>
        Task<(IEnumerable<TransactionEvent> Items, int TotalCount)> GetTransactionsAsync(
            string? accountId, TransactionCategory? category, DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize);

        Task<IEnumerable<string>> GetDistinctCountriesByAccountAsync(string accountId);

        /// <summary>
        /// Updates an alert's review status. Throws <see cref="KeyNotFoundException"/>
        /// if no alert with the given id exists, which <c>GlobalExceptionMiddleware</c>
        /// maps to a 404 response.
        /// </summary>
        Task UpdateAlertStatusAsync(Guid alertId, AlertStatus status, string? reviewedBy);
    }
}
