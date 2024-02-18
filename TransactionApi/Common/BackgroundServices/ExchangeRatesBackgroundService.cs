
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TransactionApi.API.Common.Models;

namespace TransactionApi.API.Common.BackgroundServices
{
    public class ExchangeRatesBackgroundService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<DbSettings> _transactionDbSettings;
        private readonly IMongoCollection<Transaction> _transactionsCollection;
        private readonly IMongoCollection<ExchangeRate> _ratesCollection;
        private readonly List<string> _currencies = new List<string> { "USD", "RUB", "EUR", "CNY" };

        public ExchangeRatesBackgroundService(HttpClient httpClient,
            IOptions<DbSettings> transactionDbSettings)
        {
            _httpClient = httpClient;
            _transactionDbSettings = transactionDbSettings;
            var mongoClient = new MongoClient(transactionDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(transactionDbSettings.Value.DatabaseName);
            _transactionsCollection = mongoDatabase.GetCollection<Transaction>(transactionDbSettings.Value.TransactionsCollectionName);
            _ratesCollection = mongoDatabase.GetCollection<ExchangeRate>(transactionDbSettings.Value.RatesCollectionName);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdatetTansactionRates();
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        public async Task UpdatetTansactionRates()
        {
            var filter = Builders<Transaction>.Filter.Eq("AmountInCurrencies", BsonNull.Value);
            var transactions = await _transactionsCollection.Find(filter).ToListAsync();

            var result = await _transactionsCollection.Find(filter).ToListAsync();
            if (!transactions.Any())
            {
                return;
            }
            var documentRates = await _ratesCollection.Find(new BsonDocument()).ToListAsync();
            var ratesDictionary = documentRates.ToDictionary(model => model.Code, model => model.Rate ?? 0m);
            var bulkUpdateOperations = new List<WriteModel<Transaction>>();

            foreach (var transaction in transactions)
            {
                var currencyConvertedList = new List<BsonDocument>();
                foreach (var currency in _currencies)
                {
                    var currencyConverted = Convert(ratesDictionary, transaction, currency);
                    currencyConvertedList.Add(currencyConverted);
                }
                transaction.AmountInCurrencies = currencyConvertedList;
                var update = Builders<Transaction>.Update.Set(t => t.AmountInCurrencies, transaction.AmountInCurrencies);

                bulkUpdateOperations.Add(new UpdateOneModel<Transaction>(filter, update));
            }

           await _transactionsCollection.BulkWriteAsync(bulkUpdateOperations);
        }

        public BsonDocument Convert(Dictionary<string, decimal> rates, Transaction transaction, string toCurrency)
        {
            var result = new BsonDocument();

            var fromAmount = transaction.Currency == "RUB" ? transaction.Amount :
                transaction.Amount * rates[transaction.Currency];
            var toAmount = toCurrency == "RUB" ? fromAmount :
                fromAmount / rates[toCurrency];

            return result.Add(toCurrency, decimal.Round(toAmount,2));
        }
    }
}
