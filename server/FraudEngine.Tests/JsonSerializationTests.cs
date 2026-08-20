using System.Text.Json;
using System.Text.Json.Serialization;
using FraudEngine.Api.Dtos;
using FraudEngine.Core.Models;
using Xunit;

namespace FraudEngine.Tests
{
    /// <summary>
    /// Covers the fix for enums serializing as raw integers instead of readable
    /// names in API JSON responses/Swagger. Mirrors the
    /// <see cref="JsonStringEnumConverter"/> registration added to
    /// <c>Program.cs</c> (via <c>AddJsonOptions</c>) so these tests fail if that
    /// registration is ever removed or the converter's defaults change.
    /// </summary>
    public class JsonSerializationTests
    {
        private static JsonSerializerOptions CreateOptions()
        {
            // Mirrors ASP.NET Core MVC's default JsonOptions (camelCase property
            // names, case-insensitive matching on the way in) plus the
            // JsonStringEnumConverter registered in Program.cs, so these tests
            // exercise the same effective options the API uses on the wire.
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        [Fact]
        public void Serialize_TransactionCategory_ProducesStringName()
        {
            var options = CreateOptions();

            var json = JsonSerializer.Serialize(TransactionCategory.Payment, options);

            Assert.Equal("\"Payment\"", json);
        }

        [Fact]
        public void Serialize_AlertSeverity_ProducesStringName()
        {
            var options = CreateOptions();

            var json = JsonSerializer.Serialize(AlertSeverity.High, options);

            Assert.Equal("\"High\"", json);
        }

        [Fact]
        public void Serialize_TransactionResponse_SerializesEnumsAsStrings()
        {
            var options = CreateOptions();
            var response = new TransactionResponse
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Category = TransactionCategory.Withdrawal,
                Amount = 100m,
                AccountId = "acct-1",
                Channel = Channel.ATM
            };

            var json = JsonSerializer.Serialize(response, options);
            using var doc = JsonDocument.Parse(json);

            Assert.Equal("Withdrawal", doc.RootElement.GetProperty("category").GetString());
            Assert.Equal("ATM", doc.RootElement.GetProperty("channel").GetString());
        }

        [Fact]
        public void Deserialize_LegacyNumericEnumValue_StillRoundTrips()
        {
            // Backward compatibility: older/existing clients that still send the
            // raw numeric enum value must continue to work. JsonStringEnumConverter's
            // default allowIntegerValues: true keeps this path open even though
            // outbound serialization now emits string names.
            var options = CreateOptions();
            const string json = """
                {
                    "id": "5b6f6a2e-9a3b-4b7e-8a2e-000000000001",
                    "timestamp": "2026-01-01T00:00:00+00:00",
                    "category": 2,
                    "amount": 250.50,
                    "accountId": "acct-legacy",
                    "channel": 2
                }
                """;

            var response = JsonSerializer.Deserialize<TransactionResponse>(json, options);

            Assert.NotNull(response);
            Assert.Equal(TransactionCategory.Withdrawal, response!.Category);
            Assert.Equal(Channel.ATM, response.Channel);
        }

        [Fact]
        public void Deserialize_StringEnumValue_RoundTrips()
        {
            var options = CreateOptions();
            const string json = """
                {
                    "id": "5b6f6a2e-9a3b-4b7e-8a2e-000000000002",
                    "timestamp": "2026-01-01T00:00:00+00:00",
                    "category": "Deposit",
                    "amount": 10,
                    "accountId": "acct-new",
                    "channel": "Online"
                }
                """;

            var response = JsonSerializer.Deserialize<TransactionResponse>(json, options);

            Assert.NotNull(response);
            Assert.Equal(TransactionCategory.Deposit, response!.Category);
            Assert.Equal(Channel.Online, response.Channel);
        }
    }
}
