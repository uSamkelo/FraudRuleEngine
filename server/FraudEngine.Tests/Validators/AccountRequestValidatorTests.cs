using FraudEngine.Api.Dtos;
using FraudEngine.Api.Validators;
using FraudEngine.Core.Models;
using Xunit;

namespace FraudEngine.Tests.Validators
{
    public class AccountRequestValidatorTests
    {
        private readonly AccountRequestValidator _validator = new();

        private static AccountRequest ValidRequest() => new()
        {
            AccountId = "acct-1",
            AccountType = AccountType.Savings,
            RiskTier = RiskTier.Low,
            OwnerId = "owner-1",
            DefaultCountryCode = "ZA"
        };

        [Fact]
        public void Validate_ValidRequest_HasNoErrors()
        {
            var result = _validator.Validate(ValidRequest());

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_EmptyAccountId_HasError(string accountId)
        {
            var request = ValidRequest();
            request.AccountId = accountId;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountRequest.AccountId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_EmptyOwnerId_HasError(string ownerId)
        {
            var request = ValidRequest();
            request.OwnerId = ownerId;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountRequest.OwnerId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("z")]
        [InlineData("ZAF")]
        [InlineData("za")]
        public void Validate_InvalidDefaultCountryCode_HasError(string? countryCode)
        {
            var request = ValidRequest();
            request.DefaultCountryCode = countryCode!;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountRequest.DefaultCountryCode));
        }

        [Fact]
        public void Validate_ValidDefaultCountryCode_HasNoError()
        {
            var request = ValidRequest();
            request.DefaultCountryCode = "ZA";

            var result = _validator.Validate(request);

            Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(AccountRequest.DefaultCountryCode));
        }

        [Fact]
        public void Validate_InvalidAccountTypeEnumValue_HasError()
        {
            var request = ValidRequest();
            request.AccountType = (AccountType)999;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountRequest.AccountType));
        }

        [Fact]
        public void Validate_InvalidRiskTierEnumValue_HasError()
        {
            var request = ValidRequest();
            request.RiskTier = (RiskTier)999;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(AccountRequest.RiskTier));
        }
    }
}
