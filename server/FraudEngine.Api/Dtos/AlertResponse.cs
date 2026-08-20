using System;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// What alert endpoints return. Mirrors <see cref="FraudAlert"/> without exposing
    /// the EF entity directly as part of the public API contract.
    /// </summary>
    public class AlertResponse
    {
        public Guid Id { get; set; }

        public Guid TransactionId { get; set; }

        public required string RuleName { get; set; }

        public string? Reason { get; set; }

        public AlertSeverity Severity { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public AlertStatus Status { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public string? ReviewedBy { get; set; }
    }
}
