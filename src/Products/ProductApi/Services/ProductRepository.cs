using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ProductApi.Model;

namespace ProductApi.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly object sync = new object();
        private IMongoDatabase database;
        private string collection;

        public ProductRepository(StatelessServiceContext context)
        {
        }

        private void OnConfigurationPackageModified(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
        }

        private IMongoCollection<Product> GetCollection()
        {
            lock (sync)
            {
                return database.GetCollection<Product>(collection);
            }
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var collection = GetCollection();
            var products = await collection.FindAsync(Builders<Product>.Filter.Empty);
            return await products.ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchForProductsAsync(string[] search)
        {
            var collection = GetCollection();

            var filter = (search?.Length ?? 0) > 0 ?
                Builders<Product>.Filter.Or(search.Select(s => Builders<Product>.Filter.Regex(p => p.Name, BsonRegularExpression.Create(s)))) :
                Builders<Product>.Filter.Empty;

            var products = await collection.FindAsync(filter);
            return await products.ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(Guid id)
        {
            var collection = GetCollection();
            var products = await collection.FindAsync(Builders<Product>.Filter.Eq(p => p.Id, id));
            var list = await products.ToListAsync();
            return list.Single();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var collection = GetCollection();
            if (product.Id == Guid.Empty)
            {
                product.Id = Guid.NewGuid();
            }

            await collection.InsertOneAsync(product);

            return product;
        }

        public async Task<Product> UpdateProductAsync(Guid id, Product product)
        {
            var collection = GetCollection();
            var result = await collection.FindOneAndUpdateAsync(
                Builders<Product>.Filter.Eq(p => p.Id, id),
                Builders<Product>.Update.Set(p => p.Name, product.Name).Set(p => p.Price, product.Price));

            return result;
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var collection = GetCollection();
            await collection.DeleteOneAsync(Builders<Product>.Filter.Eq(p => p.Id, id));
        }
    }
}
