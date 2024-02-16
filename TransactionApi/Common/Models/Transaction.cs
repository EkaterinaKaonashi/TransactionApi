using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransactionApi.API.Common.Models
{
    public class Transaction
    {
        public Guid? Id { get; set; }

        public string? ClientId { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        [RegularExpression("^[A-Z]{3}$")]
        public string Currency { get; set; }

        public DateTime CreatedOn { get; set; }

        [JsonIgnore]
        public CurrencyAmount? AmountInCurrencies { get; set; }
    }

    public class CurrencyAmount
    {
        public ObjectId Id { get; set; }
        public Dictionary<string, decimal> AmountCurrency { get; set; }
    }
}
