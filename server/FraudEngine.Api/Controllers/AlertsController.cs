using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Repositories;

namespace FraudEngine.Api.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private readonly IRepository _repo;

        public AlertsController(IRepository repo)
        {
            _repo = repo;
        }

        // PATCH api/alerts/{id}/status
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAlertStatusRequest request)
        {
            // Throws KeyNotFoundException (mapped to 404 by GlobalExceptionMiddleware)
            // if no alert with this id exists.
            await _repo.UpdateAlertStatusAsync(id, request.Status, request.ReviewedBy);

            var alerts = await _repo.GetAlertsAsync(status: null);
            var updated = alerts.First(a => a.Id == id);

            return Ok(updated.ToResponse());
        }
    }
}
