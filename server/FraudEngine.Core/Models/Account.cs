using System;
using System.ComponentModel.DataAnnotations;

namespace FraudEngine.Core.Models
{
    public enum AccountType
    {
        Cheque,
        Savings,
        Credit
    }

    public enum RiskTier
    {
        Low,
        Medium,
        High
    }

    public class Account
    {
        [Key]
        [Required]
        public required string AccountId { get; set; }

        public AccountType AccountType { get; set; }

        public RiskTier RiskTier { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Free-text for now, e.g. "customer-001"
        [Required]
        public required string OwnerId { get; set; }

        // ISO 3166-1 alpha-2, e.g. "ZA"
        [Required]
        public required string DefaultCountryCode { get; set; }
    }
}
