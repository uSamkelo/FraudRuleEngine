using FraudEngine.Api.Dtos;
using FraudEngine.Api.Validators;
using FraudEngine.Core.Models;
using Xunit;

namespace FraudEngine.Tests.Validators
{
    public class TransactionRequestValidatorTests
    {
        private readonly TransactionRequestValidator _validator = new();

        private static TransactionRequest ValidRequest() => new()
        {
            AccountId = "acct-1",
            Amount = 100m,
            Category = TransactionCategory.Payment,
            Currency = "ZAR",
            CountryCode = "ZA"
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
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransactionRequest.AccountId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Validate_NonPositiveAmount_HasError(decimal amount)
        {
            var request = ValidRequest();
            request.Amount = amount;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransactionRequest.Amount));
        }

        [Theory]
        [InlineData("usd")]
        [InlineData("US")]
        [InlineData("USDD")]
        public void Validate_InvalidCurrency_HasError(string currency)
        {
            var request = ValidRequest();
            request.Currency = currency;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransactionRequest.Currency));
        }

        [Theory]
        [InlineData("z")]
        [InlineData("ZAF")]
        [InlineData("za")]
        public void Validate_InvalidCountryCode_HasError(string countryCode)
        {
            var request = ValidRequest();
            request.CountryCode = countryCode;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransactionRequest.CountryCode));
        }

        [Fact]
        public void Validate_InvalidCategoryEnumValue_HasError()
        {
            var request = ValidRequest();
            request.Category = (TransactionCategory)999;

            var result = _validator.Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(TransactionRequest.Category));
        }
    }
}
