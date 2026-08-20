using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudEngine.Core.Models
{
    public enum AlertSeverity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum AlertStatus
    {
        Open,
        UnderReview,
        Resolved,
        FalsePositive
    }

    public class FraudAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        public required string RuleName { get; set; }

        public string? Reason { get; set; }

        public AlertSeverity Severity { get; set; } = AlertSeverity.Low;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public AlertStatus Status { get; set; } = AlertStatus.Open;

        public DateTimeOffset? ReviewedAt { get; set; }

        public string? ReviewedBy { get; set; }
    }
}
