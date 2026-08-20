using System;

namespace FraudEngine.Core.Rules
{
    /// <summary>
    /// Centralized, configuration-driven thresholds for all <see cref="IFraudRule"/>
    /// implementations. Bound from the "RuleOptions" section of appsettings.json via
    /// <c>IOptions&lt;RuleOptions&gt;</c>, so thresholds can be tuned per-environment
    /// without code changes.
    /// </summary>
    public class RuleOptions
    {
        public decimal HighAmountThreshold { get; set; } = 10000m;

        public int RapidTransactionCount { get; set; } = 5;

        public TimeSpan RapidTransactionWindow { get; set; } = TimeSpan.FromMinutes(1);

        public decimal VelocityAmountThreshold { get; set; } = 50000m;

        public TimeSpan VelocityAmountWindow { get; set; } = TimeSpan.FromHours(24);

        public int AccountAgeThresholdDays { get; set; } = 30;

        public decimal AccountAgeLargeAmountThreshold { get; set; } = 5000m;

        // ISO 18245 MCCs considered high-risk:
        // 6051 = Non-bank financial (crypto), 7995 = Gambling, 5933 = Pawn shops, 5944 = Jewelry
        public string[] HighRiskMerchantCategoryCodes { get; set; } = { "6051", "7995", "5933", "5944" };
    }
}
