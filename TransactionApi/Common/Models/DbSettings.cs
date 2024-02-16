namespace TransactionApi.API.Common.Models
{
    public class DbSettings
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string TransactionsCollectionName { get; set; } = null!;

        public string RatesCollectionName { get; set; } = null!;
    }
}
