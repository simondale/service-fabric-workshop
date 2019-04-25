using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductApi.Model;

namespace ProductApi.Services
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> SearchForProductsAsync(string[] search);
        Task<Product> GetProductByIdAsync(Guid id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Guid id, Product product);
        Task DeleteProductAsync(Guid id);
    }
}
