using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudEngine.Api.Controllers;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using FraudEngine.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FraudEngine.Tests.Controllers
{
    public class AlertsControllerTests
    {
        [Fact]
        public async Task UpdateStatus_ExistingAlert_ReturnsOkWithUpdatedAlertResponse()
        {
            var repo = new InMemoryRepository();
            var alert = new FraudAlert { TransactionId = Guid.NewGuid(), RuleName = "R1", Status = AlertStatus.Open };
            await repo.AddAlertAsync(alert);
            var controller = new AlertsController(repo);

            var result = await controller.UpdateStatus(alert.Id, new UpdateAlertStatusRequest
            {
                Status = AlertStatus.Resolved,
                ReviewedBy = "reviewer-1"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AlertResponse>(ok.Value);
            Assert.Equal(alert.Id, response.Id);
            Assert.Equal(AlertStatus.Resolved, response.Status);
            Assert.Equal("reviewer-1", response.ReviewedBy);
            Assert.NotNull(response.ReviewedAt);
        }

        [Fact]
        public async Task UpdateStatus_MissingAlert_ThrowsKeyNotFoundException()
        {
            // The controller intentionally lets this propagate: GlobalExceptionMiddleware
            // maps KeyNotFoundException to a 404 problem-details response at the
            // pipeline level (see 3.5 in the phase 3 brief).
            var repo = new InMemoryRepository();
            var controller = new AlertsController(repo);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                controller.UpdateStatus(Guid.NewGuid(), new UpdateAlertStatusRequest { Status = AlertStatus.Resolved }));
        }
    }
}
