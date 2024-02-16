using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TransactionApi.API.Common.Models;

namespace TransactionApi.API.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IMongoCollection<Transaction> _transactionsCollection;
        private readonly IOptions<DbSettings> _transactionDbSettings;

        public TransactionService(
            IOptions<DbSettings> transactionDbSettings)
        {
            _transactionDbSettings = transactionDbSettings;
            var mongoClient = new MongoClient(transactionDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(transactionDbSettings.Value.DatabaseName);
            _transactionsCollection = mongoDatabase.GetCollection<Transaction>(transactionDbSettings.Value.TransactionsCollectionName);
        }

        public async Task CreateTransactionAsync(Transaction transaction)
        {
            try
            {
                await _transactionsCollection.InsertOneAsync(transaction);
            }
            catch (Exception e)
            {
                throw new Exception("Transaction creation error", e);
            }
        }
    }
}

