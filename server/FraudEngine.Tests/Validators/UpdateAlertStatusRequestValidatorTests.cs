using FraudEngine.Api.Dtos;
using FraudEngine.Api.Validators;
using FraudEngine.Core.Models;
using Xunit;

namespace FraudEngine.Tests.Validators
{
    public class UpdateAlertStatusRequestValidatorTests
    {
        private readonly UpdateAlertStatusRequestValidator _validator = new();

        [Fact]
        public void Validate_ValidStatus_HasNoErrors()
        {
            var request = new UpdateAlertStatusRequest { Status = AlertStatus.Resolved };

            var result = _validator.Validate(request);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_OutOfRangeStatus_HasError()
        {
            var request = new UpdateAlertStatusRequest { Status = (AlertStatus)99 };

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAlertStatusRequest.Status));
        }
    }
}
