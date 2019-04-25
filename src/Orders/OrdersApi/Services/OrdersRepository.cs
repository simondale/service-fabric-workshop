using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OrdersApi.Model;

namespace OrdersApi.Services
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly object sync = new object();
        private IMongoDatabase database;
        private string collection;
        private string statistics;

        public OrdersRepository(StatefulServiceContext context)
        {
            context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += OnConfigurationPackageModified;
            OnConfigurationPackageModified(this, new PackageModifiedEventArgs<ConfigurationPackage>
            {
                NewPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config")
            });
        }

        private void OnConfigurationPackageModified(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            var section = e.NewPackage?.Settings?.Sections?["Database"];
            var connectionString = section?.Parameters?["ConnectionString"]?.Value;
            if (connectionString != null)
            {
                if (connectionString.Contains("{application:service}"))
                {
                    var application = section.Parameters["Application"].Value;
                    var service = section.Parameters["Service"].Value;
                    var resolver = ServicePartitionResolver.GetDefault();
                    var partition = resolver.ResolveAsync(new Uri($"fabric:/{application}/{service}"), new ServicePartitionKey(), CancellationToken.None).GetAwaiter().GetResult();
                    var address = JObject.Parse(partition.Endpoints.Select(ep => ep.Address).First()).SelectToken("Endpoints").ToObject<JObject>().Properties().First().Value.Value<string>();
                    connectionString = connectionString.Replace("{application:service}", address);
                }

                var client = new MongoClient(connectionString);
                lock (sync)
                {
                    database = client.GetDatabase(section.Parameters["Database"].Value);
                    collection = section.Parameters["Collection"].Value;
                    statistics = section.Parameters["Statistics"].Value;
                }
            }
        }

        private IMongoCollection<Order> GetCollection()
        {
            lock (sync)
            {
                return database.GetCollection<Order>(collection);
            }
        }

        private IMongoCollection<Statistics> GetStatisticsCollection()
        {
            lock (sync)
            {
                return database.GetCollection<Statistics>(statistics);
            }
        }

        public async Task<IEnumerable<Statistics>> AddOrderAsync(Order order, CancellationToken cancellationToken)
        {
            var collection = GetCollection();
            await collection.InsertOneAsync(order, new InsertOneOptions(), cancellationToken);

            var date = $"{order.OrderDateTime:yyyyMMdd}";
            var statistics = GetStatisticsCollection();
            foreach (var p in order.Products)
            {
                var filter = Builders<Statistics>.Filter.Eq(x => x.Id, new StatisticsId { Date = date, Product = $"{p.Id}" });
                var update = Builders<Statistics>.Update
                    .Set(x => x.Name, p.Name)
                    .Inc(x => x.OrdersCount, 1)
                    .Inc(x => x.OrdersValue, p.Price);
                await statistics.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
            }

            var results = await statistics.FindAsync<Statistics>(Builders<Statistics>.Filter.Eq(x => x.Id.Date, date), null, cancellationToken);
            var list = await results.ToListAsync();

            return list;
        }
    }
}
