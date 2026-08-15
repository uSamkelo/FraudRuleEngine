using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudEngine.Core.Models
{
    public enum AlertSeverity
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public class FraudAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TransactionId { get; set; }

        [Required]
        public string RuleName { get; set; }

        public string Reason { get; set; }

        public AlertSeverity Severity { get; set; } = AlertSeverity.Low;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
