using Microsoft.Extensions.Options;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using TransactionApi.API.Common.Models;
using MongoDB.Driver;

namespace TransactionApi.API.Common.BackgroundServices
{
    public class RatesGetterBackgroundService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<DbSettings> _ratesDbSettings;
        private readonly IMongoCollection<ExchangeRate> _exchangeRateCollection;
        private const string _url = "https://www.cbr.ru/DailyInfoWebServ/DailyInfo.asmx";
        private const string _action = "http://web.cbr.ru/GetCursOnDate";
        private readonly List<string> _currencies = new List<string> { "USD", "RUB", "EUR", "CNY" };

        public RatesGetterBackgroundService(HttpClient httpClient,
            IOptions<DbSettings> ratesDbSettings)
        {
            _httpClient = httpClient;
            _ratesDbSettings = ratesDbSettings;
            var mongoClient = new MongoClient(ratesDbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(ratesDbSettings.Value.DatabaseName);
            _exchangeRateCollection = mongoDatabase.GetCollection<ExchangeRate>(ratesDbSettings.Value.RatesCollectionName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CreateExchangeRatesAsync();
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        public async Task CreateExchangeRatesAsync()
        {
            var response = await GetExchangeRatesAsync();
            var exchangeRates = response?.Where(rate => _currencies.Contains(rate.Code));
            await _exchangeRateCollection.DeleteManyAsync(Builders<ExchangeRate>.Filter.Empty);
            await _exchangeRateCollection.InsertManyAsync(exchangeRates);
        }

        public async Task<List<ExchangeRate>> GetExchangeRatesAsync()
        {
            var exchangeRate = new List<ExchangeRate>();

            try
            {
                var soapEnvelopeXml = CreateSoapEnvelope();
                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Headers.Add("SOAPAction", _action);
                request.Content = new StringContent(soapEnvelopeXml.OuterXml, Encoding.UTF8, "text/xml");

                using var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var soapResponse = await response.Content.ReadAsStringAsync();
                var createdOn = DateTime.Now;
                exchangeRate = XDocument.Parse(soapResponse)?
                       .Descendants("ValuteCursOnDate")?
                       .Select(o => new ExchangeRate
                       {
                           Name = (string)o?.Element("Vname"),
                           Rate = (decimal)o?.Element("Vcurs"),
                           Code = (string)o?.Element("VchCode"),
                           CreatedOn = createdOn
                       })?.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return exchangeRate;
        }

        private  XmlDocument CreateSoapEnvelope()
        {
            DateTime curDate = DateTime.Now;
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                        <GetCursOnDate xmlns=""http://web.cbr.ru/"">
                            <On_date>{curDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}</On_date>
                        </GetCursOnDate>
                    </soap:Body>
                </soap:Envelope>");

            return soapEnvelopeDocument;
        }
    }
}
