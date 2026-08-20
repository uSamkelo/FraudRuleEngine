using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudEngine.Core.Models
{
    public enum TransactionCategory
    {
        Unknown = 0,
        Payment = 1,
        Withdrawal = 2,
        Transfer = 3,
        Deposit = 4
    }

    public enum Channel
    {
        Unknown,
        Online,
        ATM,
        POS,
        Branch
    }

    public class TransactionEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public TransactionCategory Category { get; set; } = TransactionCategory.Unknown;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public required string AccountId { get; set; }

        // Free-form JSON metadata
        public string? Metadata { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }

        // ISO 4217
        public string Currency { get; set; } = "ZAR";

        public string? MerchantId { get; set; }

        public string? MerchantName { get; set; }

        // ISO 18245 MCC, e.g. "5411" = Grocery
        public string? MerchantCategoryCode { get; set; }

        public Channel Channel { get; set; } = Channel.Unknown;

        // ISO 3166-1 alpha-2
        public string CountryCode { get; set; } = "ZA";

        public string? DeviceId { get; set; }

        public string? IpAddress { get; set; }

        public string? CardLast4 { get; set; }
    }
}
