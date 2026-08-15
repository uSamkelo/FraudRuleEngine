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

    public class TransactionEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public TransactionCategory Category { get; set; } = TransactionCategory.Unknown;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string AccountId { get; set; }

        // Free-form JSON metadata
        public string Metadata { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }
    }
}
