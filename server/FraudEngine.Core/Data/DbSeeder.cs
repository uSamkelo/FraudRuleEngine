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
    /// Seeds a realistic set of accounts and transactions through the real
    /// <see cref="RulesEngine"/>, so a fresh environment has browsable transactions
    /// and alerts without requiring any manual API calls first. This is intended for
    /// local development / review environments only - it is a no-op whenever the
    /// database already contains transactions or accounts, and callers should avoid
    /// invoking it against a real production database.
    ///
    /// Seeds 20 generic accounts plus 200+ ordinary (non-fraud) transactions spread
    /// over the past 30 days, plus 7 dedicated accounts/transactions - one per
    /// registered <see cref="IFraudRule"/> - engineered to guarantee that rule fires,
    /// so every rule type is represented in the seeded alerts.
    /// </summary>
    public static class DbSeeder
    {
        // Ordinary/benign field pools for the general transaction pool. Deliberately
        // disjoint from RuleOptions.HighRiskMerchantCategoryCodes ("6051", "7995",
        // "5933", "5944") so this pool never accidentally trips MerchantCategoryRule.
        private static readonly TransactionCategory[] OrdinaryCategories =
        {
            TransactionCategory.Payment,
            TransactionCategory.Withdrawal,
            TransactionCategory.Transfer,
            TransactionCategory.Deposit
        };

        private static readonly Channel[] OrdinaryChannels =
        {
            Channel.Online,
            Channel.ATM,
            Channel.POS,
            Channel.Branch
        };

        private static readonly string[] OrdinaryMerchantCategoryCodes =
        {
            "5411", // Grocery stores
            "5812", // Restaurants
            "5541", // Service stations
            "4900", // Utilities
            "5311", // Department stores
            "4899", // Cable/streaming services
            "5661", // Shoe stores
            "8011", // Doctors
            "5999", // Misc retail
        };

        public static async Task SeedAsync(FraudDbContext db, IRepository repository, RulesEngine engine)
        {
            // Guard on both tables so a second invocation against an already-seeded
            // database is a full no-op, rather than skipping transactions while still
            // duplicating accounts.
            if (await db.Transactions.AnyAsync() || await db.Accounts.AnyAsync())
            {
                return;
            }

            var random = new Random(42);
            var now = DateTimeOffset.UtcNow;

            // 1. Accounts first - every transaction below references one of these, and
            // several rules (UnusualCountryRule, NightTimeWithdrawalRule, AccountAgeRule)
            // read the account back out of the repository while evaluating.
            var genericAccounts = BuildGenericAccounts(random, now);
            foreach (var account in genericAccounts)
            {
                await repository.AddAccountAsync(account);
            }

            var scenarioAccounts = BuildScenarioAccounts(now);
            foreach (var account in scenarioAccounts)
            {
                await repository.AddAccountAsync(account);
            }

            // 2. 200+ ordinary transactions across the 20 generic accounts only,
            // spread over the past 30 days. Deliberately kept "boring" (ZA-only,
            // ordinary MCCs, amounts well under every rule threshold) so this pool
            // is not itself a source of alerts - the guaranteed scenarios below are
            // the only alerts this seeding run is designed to produce.
            var genericAccountIds = genericAccounts.ConvertAll(a => a.AccountId);
            foreach (var tx in BuildOrdinaryTransactions(random, now, genericAccountIds))
            {
                await PersistAndEvaluateAsync(tx, repository, engine);
            }

            // 3. Exactly one guaranteed-to-fire scenario per fraud rule, each against
            // its own dedicated account seeded above, so it can't be diluted or
            // polluted by the ordinary pool.
            foreach (var tx in BuildScenarioTransactions())
            {
                await PersistAndEvaluateAsync(tx, repository, engine);
            }
        }

        private static async Task PersistAndEvaluateAsync(TransactionEvent tx, IRepository repository, RulesEngine engine)
        {
            // Reuse the exact same pipeline the API uses (persist, then evaluate,
            // then persist any resulting alerts) so seeded data is a faithful
            // demonstration of production behavior rather than hand-crafted alerts.
            // This ordering is load-bearing: every rule (including VelocityAmountRule)
            // assumes the in-flight transaction is already persisted by the time
            // rules evaluate it.
            await repository.AddTransactionAsync(tx);

            var alerts = await engine.EvaluateAsync(tx);
            foreach (var alert in alerts)
            {
                await repository.AddAlertAsync(alert);
            }
        }

        private static List<Account> BuildGenericAccounts(Random random, DateTimeOffset now)
        {
            var accounts = new List<Account>();
            var customerNumber = 1;

            // 8 Cheque / Low risk, opened 90-365 days ago.
            for (var i = 1; i <= 8; i++)
            {
                accounts.Add(new Account
                {
                    AccountId = $"acct-cheque-{i:D3}",
                    OwnerId = $"customer-{customerNumber++:D3}",
                    AccountType = AccountType.Cheque,
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = now.AddDays(-random.Next(90, 366))
                });
            }

            // 6 Savings / Medium risk, opened 30-90 days ago.
            for (var i = 1; i <= 6; i++)
            {
                accounts.Add(new Account
                {
                    AccountId = $"acct-savings-{i:D3}",
                    OwnerId = $"customer-{customerNumber++:D3}",
                    AccountType = AccountType.Savings,
                    RiskTier = RiskTier.Medium,
                    DefaultCountryCode = "ZA",
                    CreatedAt = now.AddDays(-random.Next(30, 91))
                });
            }

            // 4 Credit / High risk, opened 7-30 days ago.
            for (var i = 1; i <= 4; i++)
            {
                accounts.Add(new Account
                {
                    AccountId = $"acct-credit-{i:D3}",
                    OwnerId = $"customer-{customerNumber++:D3}",
                    AccountType = AccountType.Credit,
                    RiskTier = RiskTier.High,
                    DefaultCountryCode = "ZA",
                    CreatedAt = now.AddDays(-random.Next(7, 31))
                });
            }

            // 2 additional new accounts (< 30 days old) for general variety. Their
            // ordinary transactions stay under AccountAgeLargeAmountThreshold (5,000),
            // so these do not themselves guarantee an AccountAgeRule alert - that
            // guarantee is provided separately by the dedicated acct-newlarge-001
            // scenario account below.
            var newAccountTypes = new[] { AccountType.Cheque, AccountType.Savings };
            for (var i = 1; i <= 2; i++)
            {
                accounts.Add(new Account
                {
                    AccountId = $"acct-new-{i:D3}",
                    OwnerId = $"customer-{customerNumber++:D3}",
                    AccountType = newAccountTypes[i - 1],
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = now.AddDays(-random.Next(1, 30))
                });
            }

            return accounts;
        }

        private static List<Account> BuildScenarioAccounts(DateTimeOffset now)
        {
            // Dated well outside the AccountAgeRule lookback (30 days) so none of
            // these accidentally also trips AccountAgeRule - except
            // acct-newlarge-001, which *is* the AccountAgeRule scenario account.
            var establishedAccountAge = now.AddDays(-200);

            return new List<Account>
            {
                new()
                {
                    AccountId = "acct-high-001",
                    OwnerId = "customer-021",
                    AccountType = AccountType.Cheque,
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-rapid-001",
                    OwnerId = "customer-022",
                    AccountType = AccountType.Cheque,
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-velocity-001",
                    OwnerId = "customer-023",
                    AccountType = AccountType.Savings,
                    RiskTier = RiskTier.Medium,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-foreign-001",
                    OwnerId = "customer-024",
                    AccountType = AccountType.Cheque,
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-night-001",
                    OwnerId = "customer-025",
                    AccountType = AccountType.Credit,
                    RiskTier = RiskTier.High,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-mcc-001",
                    OwnerId = "customer-026",
                    AccountType = AccountType.Cheque,
                    RiskTier = RiskTier.Low,
                    DefaultCountryCode = "ZA",
                    CreatedAt = establishedAccountAge
                },
                new()
                {
                    AccountId = "acct-newlarge-001",
                    OwnerId = "customer-027",
                    AccountType = AccountType.Savings,
                    RiskTier = RiskTier.Medium,
                    DefaultCountryCode = "ZA",
                    CreatedAt = now.AddDays(-10)
                }
            };
        }

        private static List<TransactionEvent> BuildOrdinaryTransactions(Random random, DateTimeOffset now, List<string> accountIds)
        {
            const int count = 220;
            var transactions = new List<TransactionEvent>(count);

            for (var i = 0; i < count; i++)
            {
                var accountId = accountIds[random.Next(accountIds.Count)];
                var amount = Math.Round(20m + (decimal)random.NextDouble() * 1980m, 2);

                transactions.Add(new TransactionEvent
                {
                    AccountId = accountId,
                    Timestamp = now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 23)),
                    Category = OrdinaryCategories[random.Next(OrdinaryCategories.Length)],
                    Channel = OrdinaryChannels[random.Next(OrdinaryChannels.Length)],
                    Amount = amount,
                    Currency = "ZAR",
                    CountryCode = "ZA",
                    MerchantId = $"merchant-{random.Next(1, 60):D3}",
                    MerchantCategoryCode = OrdinaryMerchantCategoryCodes[random.Next(OrdinaryMerchantCategoryCodes.Length)],
                    Metadata = "{\"note\":\"ordinary seeded transaction\"}"
                });
            }

            return transactions;
        }

        private static List<TransactionEvent> BuildScenarioTransactions()
        {
            var transactions = new List<TransactionEvent>();

            // --- HighAmountRule -------------------------------------------------
            // 15,000 >= HighAmountThreshold (10,000). Channel is Branch (not ATM) and
            // RiskTier is Low, so NightTimeWithdrawalRule cannot also fire here.
            transactions.Add(new TransactionEvent
            {
                AccountId = "acct-high-001",
                Timestamp = DateTimeOffset.UtcNow,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.Branch,
                Amount = 15000.00m,
                Currency = "ZAR",
                CountryCode = "ZA",
                Metadata = "{\"note\":\"guaranteed scenario: HighAmountRule\"}"
            });

            // --- RapidTransactionsRule -------------------------------------------
            // 5 payments (== RapidTransactionCount) a few seconds apart - comfortably
            // inside the 1-minute RapidTransactionWindow. GetRecentTransactionsByAccountAsync
            // filters by real wall-clock "now" (not by these Timestamps relative to each
            // other), so the base is captured fresh here rather than reused from earlier
            // in SeedAsync, to stay robust even if seeding the accounts/ordinary pool
            // above took a while against a real database.
            var rapidBase = DateTimeOffset.UtcNow;
            for (var i = 0; i < 5; i++)
            {
                transactions.Add(new TransactionEvent
                {
                    AccountId = "acct-rapid-001",
                    Timestamp = rapidBase.AddSeconds(i * 2),
                    Category = TransactionCategory.Payment,
                    Channel = Channel.POS,
                    Amount = 50.00m,
                    Currency = "ZAR",
                    CountryCode = "ZA",
                    Metadata = $"{{\"note\":\"guaranteed scenario: RapidTransactionsRule ({i + 1}/5)\"}}"
                });
            }
            // Running count as each is persisted-then-evaluated: 1, 2, 3, 4, 5 - the
            // 5th persisted transaction sees count == 5 >= RapidTransactionCount and
            // is the one the alert is attached to.

            // --- VelocityAmountRule -----------------------------------------------
            // 4 transfers of 14,000 each. Running totals as each is
            // persisted-then-evaluated: 14000, 28000, 42000, 56000 - the 4th crosses
            // VelocityAmountThreshold (50,000). All timestamped within the last few
            // hours, comfortably inside the 24h VelocityAmountWindow (whose cutoff is
            // also computed from real wall-clock "now", hence the fresh capture here).
            // Note: each individual 14,000 transfer is also >= HighAmountThreshold
            // (10,000), so HighAmountRule legitimately fires alongside
            // VelocityAmountRule on all 4 of these - that is correct behavior of both
            // rules given the brief's specified amounts, not a bug.
            var velocityBase = DateTimeOffset.UtcNow;
            for (var i = 0; i < 4; i++)
            {
                transactions.Add(new TransactionEvent
                {
                    AccountId = "acct-velocity-001",
                    Timestamp = velocityBase.AddHours(-6 + i * 2),
                    Category = TransactionCategory.Transfer,
                    Channel = Channel.Online,
                    Amount = 14000.00m,
                    Currency = "ZAR",
                    CountryCode = "ZA",
                    Metadata = $"{{\"note\":\"guaranteed scenario: VelocityAmountRule ({i + 1}/4)\"}}"
                });
            }

            // --- UnusualCountryRule -------------------------------------------------
            // Transaction country (NG) differs from the account's home country (ZA).
            transactions.Add(new TransactionEvent
            {
                AccountId = "acct-foreign-001",
                Timestamp = DateTimeOffset.UtcNow,
                Category = TransactionCategory.Payment,
                Channel = Channel.Online,
                Amount = 750.00m,
                Currency = "ZAR",
                CountryCode = "NG",
                Metadata = "{\"note\":\"guaranteed scenario: UnusualCountryRule\"}"
            });

            // --- NightTimeWithdrawalRule --------------------------------------------
            // ATM withdrawal, High-risk account, UTC hour in [0, 4). Built from
            // DateTime.UtcNow.Date (Kind = Utc) rather than
            // DateTimeOffset.UtcNow.Date (which returns Kind = Unspecified, and would
            // be silently reinterpreted as local time by the DateTime -> DateTimeOffset
            // conversion), so tx.Timestamp.UtcDateTime.Hour is exactly 3 regardless of
            // the machine's local timezone.
            var nightTimestamp = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-1).AddHours(3), TimeSpan.Zero);
            transactions.Add(new TransactionEvent
            {
                AccountId = "acct-night-001",
                Timestamp = nightTimestamp,
                Category = TransactionCategory.Withdrawal,
                Channel = Channel.ATM,
                Amount = 500.00m,
                Currency = "ZAR",
                CountryCode = "ZA",
                Metadata = "{\"note\":\"guaranteed scenario: NightTimeWithdrawalRule\"}"
            });

            // --- MerchantCategoryRule ------------------------------------------------
            // MCC 7995 (gambling) is in RuleOptions.HighRiskMerchantCategoryCodes.
            transactions.Add(new TransactionEvent
            {
                AccountId = "acct-mcc-001",
                Timestamp = DateTimeOffset.UtcNow,
                Category = TransactionCategory.Payment,
                Channel = Channel.Online,
                Amount = 300.00m,
                Currency = "ZAR",
                CountryCode = "ZA",
                MerchantId = "merchant-casino-01",
                MerchantCategoryCode = "7995",
                Metadata = "{\"note\":\"guaranteed scenario: MerchantCategoryRule\"}"
            });

            // --- AccountAgeRule -------------------------------------------------------
            // Account is 10 days old (< AccountAgeThresholdDays of 30) and the
            // transaction (8,000) is >= AccountAgeLargeAmountThreshold (5,000). 8,000
            // is deliberately kept below HighAmountThreshold (10,000) so this scenario
            // cleanly exercises only AccountAgeRule.
            transactions.Add(new TransactionEvent
            {
                AccountId = "acct-newlarge-001",
                Timestamp = DateTimeOffset.UtcNow,
                Category = TransactionCategory.Transfer,
                Channel = Channel.Online,
                Amount = 8000.00m,
                Currency = "ZAR",
                CountryCode = "ZA",
                Metadata = "{\"note\":\"guaranteed scenario: AccountAgeRule\"}"
            });

            return transactions;
        }
    }
}
