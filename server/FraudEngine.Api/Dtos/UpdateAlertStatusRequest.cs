using System.ComponentModel.DataAnnotations;
using FraudEngine.Core.Models;

namespace FraudEngine.Api.Dtos
{
    /// <summary>
    /// What callers PATCH to update an alert's review status.
    /// </summary>
    public class UpdateAlertStatusRequest
    {
        [Required]
        public AlertStatus Status { get; set; }

        public string? ReviewedBy { get; set; }
    }
}
