using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FraudEngine.Core.Models;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;

namespace FraudEngine.Core.Data
{
    /// <summary>
    /// Seeds a small set of representative transactions through the real
    /// <see cref="RulesEngine"/>, so a fresh environment has browsable transactions
    /// and alerts without requiring any manual API calls first. This is intended for
    /// local development / review environments only - it is a no-op whenever the
    /// database already contains transactions, and callers should avoid invoking it
    /// against a real production database.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(FraudDbContext db, IRepository repository, RulesEngine engine)
        {
            if (await db.Transactions.AnyAsync())
            {
                // Already seeded (or already has real data) - never overwrite/duplicate.
                return;
            }

            var now = DateTimeOffset.UtcNow;

            // A mix of ordinary transactions plus a few designed to trip the
            // built-in rules (HighAmountRule, RapidTransactionsRule), so alerts
            // are visible immediately via GET /api/transactions/alerts.
            var sampleTransactions = new List<TransactionEvent>
            {
                new() { AccountId = "acct-2001", Category = TransactionCategory.Payment, Amount = 45.00m, Metadata = "{\"note\":\"coffee shop\"}" },
                new() { AccountId = "acct-2001", Category = TransactionCategory.Payment, Amount = 60.50m, Metadata = "{\"note\":\"groceries\"}" },
                new() { AccountId = "acct-2002", Category = TransactionCategory.Withdrawal, Amount = 15000.00m, Metadata = "{\"note\":\"large ATM withdrawal\"}" },
                new() { AccountId = "acct-2003", Category = TransactionCategory.Transfer, Amount = 250.00m, Metadata = "{\"note\":\"rent share\"}" },
                new() { AccountId = "acct-2004", Category = TransactionCategory.Deposit, Amount = 500.00m, Metadata = "{\"note\":\"paycheck\"}" },
                new() { AccountId = "acct-2005", Category = TransactionCategory.Payment, Amount = 20.00m, Metadata = "{\"note\":\"subscription 1\"}" },
                new() { AccountId = "acct-2005", Category = TransactionCategory.Payment, Amount = 20.00m, Metadata = "{\"note\":\"subscription 2\"}" },
                new() { AccountId = "acct-2005", Category = TransactionCategory.Payment, Amount = 20.00m, Metadata = "{\"note\":\"subscription 3\"}" },
                new() { AccountId = "acct-2005", Category = TransactionCategory.Payment, Amount = 20.00m, Metadata = "{\"note\":\"subscription 4\"}" },
                new() { AccountId = "acct-2005", Category = TransactionCategory.Payment, Amount = 20.00m, Metadata = "{\"note\":\"subscription 5 - rapid succession\"}" },
                new() { AccountId = "acct-2006", Category = TransactionCategory.Withdrawal, Amount = 12000.00m, Metadata = "{\"note\":\"suspiciously large withdrawal\"}" },
                new() { AccountId = "acct-2007", Category = TransactionCategory.Payment, Amount = 30.00m, Metadata = "{\"note\":\"regular payment\"}" },
            };

            foreach (var tx in sampleTransactions)
            {
                tx.Timestamp = now;

                // Reuse the exact same pipeline the API uses (persist, then evaluate,
                // then persist any resulting alerts) so seeded data is a faithful
                // demonstration of production behavior rather than hand-crafted alerts.
                await repository.AddTransactionAsync(tx);

                var alerts = await engine.EvaluateAsync(tx);
                foreach (var alert in alerts)
                {
                    await repository.AddAlertAsync(alert);
                }
            }
        }
    }
}
