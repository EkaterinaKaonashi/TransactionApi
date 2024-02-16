
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using TransactionApi.API.Common.Models;

namespace TransactionApi.API.Common.BackgroundServices
{
    public class ExchangeRatesBackgroundService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<DbSettings> _transactionDbSettings;
        private readonly IMongoCollection<Transaction> _transactionsCollection;
        private readonly IMongoCollection<ExchangeRate> _ratesCollection;

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
            var filter = Builders<Transaction>.Filter.Exists(x => x.AmountInCurrencies, false);
            var transactions = await _transactionsCollection.Find(filter).ToListAsync();
            if (!transactions.Any())
            {
                return;
            }
            var documents = await _ratesCollection.Find(new BsonDocument()).ToListAsync();
            var ratesDictionary = documents.ToDictionary(model => model.Code, model => model.Rate ?? 0m);
            var bulkUpdates = new List<WriteModel<Transaction>>();

            foreach (var transaction in transactions)
            {
                var amountInCurrencies = new Dictionary<string, decimal>();

                foreach (var currencyRate in ratesDictionary)
                {
                    if (transaction.Currency != currencyRate.Key)
                    {
                        var convertedAmount = transaction.Amount * currencyRate.Value;
                        amountInCurrencies[currencyRate.Key] = convertedAmount;
                    }
                }
                transaction.AmountInCurrencies = new CurrencyAmount()
                {
                    AmountCurrency = amountInCurrencies
                };

                //var updateFilter = Builders<Transaction>.Filter.Eq(x => x.Id, transaction.Id);
                //var update = Builders<Transaction>.Update.Set("AmountInCurrencies", amountInCurrencies);
                //await _transactionsCollection.UpdateOneAsync(updateFilter, transaction);
                //var doc = new BsonDocument(amountInCurrencies);
                //transaction.AmountInCurrencies = doc;


                var updateDefinition = Builders<Transaction>.Update
                      .Set(x => x.AmountInCurrencies, transaction.AmountInCurrencies);

                var updateFilter = Builders<Transaction>.Filter.Eq(x => x.Id, transaction.Id);
                await _transactionsCollection.UpdateOneAsync(updateFilter, updateDefinition);
                //bulkUpdates.Add(updateModel);
            }

            //var response = await _transactionsCollection.BulkWriteAsync(bulkUpdates);
        }
    }
}
