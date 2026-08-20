using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Core.Models;
using Xunit;

namespace FraudEngine.Tests.TestDoubles
{
    /// <summary>
    /// Exercises the Phase 3 additions to <see cref="InMemoryRepository"/>
    /// (the general paginated listing, alert status filtering, and alert status
    /// updates), since these are shared contract behaviors that <see cref="FraudEngine.Core.Repositories.EfRepository"/>
    /// must mirror.
    /// </summary>
    public class InMemoryRepositoryTests
    {
        private static TransactionEvent MakeTransaction(string accountId, TransactionCategory category, DateTimeOffset timestamp) => new()
        {
            AccountId = accountId,
            Category = category,
            Amount = 10m,
            Timestamp = timestamp
        };

        [Fact]
        public async Task GetTransactionsAsync_NoFilters_ReturnsAllOrderedByTimestampDescending()
        {
            var repo = new InMemoryRepository();
            var now = DateTimeOffset.UtcNow;
            var older = MakeTransaction("acct-1", TransactionCategory.Payment, now.AddMinutes(-10));
            var newer = MakeTransaction("acct-2", TransactionCategory.Withdrawal, now);
            await repo.AddTransactionAsync(older);
            await repo.AddTransactionAsync(newer);

            var (items, totalCount) = await repo.GetTransactionsAsync(null, null, null, null, page: 1, pageSize: 20);

            Assert.Equal(2, totalCount);
            Assert.Equal(new[] { newer.Id, older.Id }, items.Select(t => t.Id).ToArray());
        }

        [Fact]
        public async Task GetTransactionsAsync_FiltersByAccountId()
        {
            var repo = new InMemoryRepository();
            var now = DateTimeOffset.UtcNow;
            await repo.AddTransactionAsync(MakeTransaction("acct-1", TransactionCategory.Payment, now));
            await repo.AddTransactionAsync(MakeTransaction("acct-2", TransactionCategory.Payment, now));

            var (items, totalCount) = await repo.GetTransactionsAsync("acct-1", null, null, null, page: 1, pageSize: 20);

            Assert.Equal(1, totalCount);
            Assert.All(items, t => Assert.Equal("acct-1", t.AccountId));
        }

        [Fact]
        public async Task GetTransactionsAsync_UnknownAccountId_ReturnsEmptyNotError()
        {
            var repo = new InMemoryRepository();
            await repo.AddTransactionAsync(MakeTransaction("acct-1", TransactionCategory.Payment, DateTimeOffset.UtcNow));

            var (items, totalCount) = await repo.GetTransactionsAsync("nonexistent", null, null, null, page: 1, pageSize: 20);

            Assert.Equal(0, totalCount);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetTransactionsAsync_FiltersByCategory()
        {
            var repo = new InMemoryRepository();
            var now = DateTimeOffset.UtcNow;
            await repo.AddTransactionAsync(MakeTransaction("acct-1", TransactionCategory.Payment, now));
            await repo.AddTransactionAsync(MakeTransaction("acct-1", TransactionCategory.Withdrawal, now));

            var (items, totalCount) = await repo.GetTransactionsAsync(null, TransactionCategory.Withdrawal, null, null, page: 1, pageSize: 20);

            Assert.Equal(1, totalCount);
            Assert.All(items, t => Assert.Equal(TransactionCategory.Withdrawal, t.Category));
        }

        [Fact]
        public async Task GetTransactionsAsync_FiltersByDateRange()
        {
            var repo = new InMemoryRepository();
            var now = DateTimeOffset.UtcNow;
            var tooOld = MakeTransaction("acct-1", TransactionCategory.Payment, now.AddDays(-5));
            var inRange = MakeTransaction("acct-1", TransactionCategory.Payment, now.AddDays(-1));
            var tooNew = MakeTransaction("acct-1", TransactionCategory.Payment, now.AddDays(1));
            await repo.AddTransactionAsync(tooOld);
            await repo.AddTransactionAsync(inRange);
            await repo.AddTransactionAsync(tooNew);

            var (items, totalCount) = await repo.GetTransactionsAsync(
                null, null, from: now.AddDays(-2), to: now, page: 1, pageSize: 20);

            Assert.Equal(1, totalCount);
            Assert.Equal(inRange.Id, Assert.Single(items).Id);
        }

        [Fact]
        public async Task GetTransactionsAsync_Paginates_ReturnsTotalCountAcrossAllPages()
        {
            var repo = new InMemoryRepository();
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < 5; i++)
            {
                await repo.AddTransactionAsync(MakeTransaction("acct-1", TransactionCategory.Payment, now.AddMinutes(i)));
            }

            var (items, totalCount) = await repo.GetTransactionsAsync(null, null, null, null, page: 2, pageSize: 2);

            Assert.Equal(5, totalCount);
            Assert.Equal(2, items.Count());
        }

        [Fact]
        public async Task GetAlertsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
        {
            var repo = new InMemoryRepository();
            var open = new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R1", Status = AlertStatus.Open };
            var resolved = new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R2", Status = AlertStatus.Resolved };
            await repo.AddAlertAsync(open);
            await repo.AddAlertAsync(resolved);

            var result = await repo.GetAlertsAsync(AlertStatus.Resolved);

            Assert.Equal(resolved.Id, Assert.Single(result).Id);
        }

        [Fact]
        public async Task GetAlertsAsync_NoArgOverload_ReturnsAllStatuses()
        {
            var repo = new InMemoryRepository();
            await repo.AddAlertAsync(new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R1", Status = AlertStatus.Open });
            await repo.AddAlertAsync(new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R2", Status = AlertStatus.Resolved });

            var result = await repo.GetAlertsAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateAlertStatusAsync_ExistingAlert_UpdatesStatusReviewedAtAndReviewedBy()
        {
            var repo = new InMemoryRepository();
            var alert = new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R1", Status = AlertStatus.Open };
            await repo.AddAlertAsync(alert);

            await repo.UpdateAlertStatusAsync(alert.Id, AlertStatus.Resolved, "reviewer-1");

            var updated = Assert.Single(await repo.GetAlertsAsync());
            Assert.Equal(AlertStatus.Resolved, updated.Status);
            Assert.Equal("reviewer-1", updated.ReviewedBy);
            Assert.NotNull(updated.ReviewedAt);
        }

        [Fact]
        public async Task UpdateAlertStatusAsync_MissingAlert_ThrowsKeyNotFoundException()
        {
            var repo = new InMemoryRepository();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => repo.UpdateAlertStatusAsync(Guid.NewGuid(), AlertStatus.Resolved, null));
        }
    }
}
