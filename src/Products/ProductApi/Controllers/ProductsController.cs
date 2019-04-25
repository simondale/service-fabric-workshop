using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Model;
using ProductApi.Services;

namespace ProductApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : Controller
    {
        private readonly IProductRepository repository;

        public ProductsController(IProductRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get()
        {
            try
            {
                var query = Request.Query.TryGetValue("q", out var value) ? value.ToArray() : null;
                var products = ((query?.Length ?? 0) > 0) ?
                    await repository.SearchForProductsAsync(query) :
                    await repository.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception e)
            {
                var response = Json(new
                {
                    e.Message
                });
                response.StatusCode = 500;
                return response;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(Guid id)
        {
            try
            {
                var product = await repository.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return Ok(product);
            }
            catch (Exception e)
            {
                var response = Json(new
                {
                    e.Message
                });
                response.StatusCode = 500;
                return response;
            }
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Post([FromBody] Product value)
        {
            try
            {
                var product = await repository.CreateProductAsync(value);
                return Created(GetUri(product.Id), product);
            }
            catch (Exception e)
            {
                var response = Json(new
                {
                    e.Message
                });
                response.StatusCode = 500;
                return response;
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> Put(Guid id, [FromBody] Product value)
        {
            try
            {
                var product = await repository.UpdateProductAsync(id, value);
                return Ok(product);
            }
            catch (Exception e)
            {
                var response = Json(new
                {
                    e.Message
                });
                response.StatusCode = 500;
                return response;
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                await repository.DeleteProductAsync(id);
                return Ok();
            }
            catch (Exception e)
            {
                var response = Json(new
                {
                    e.Message
                });
                response.StatusCode = 500;
                return response;
            }
        }

        private string GetUri(Guid id)
        {
            return Request.Path.Value.EndsWith('/') ?
                $"{Request.Path.Value}{id}" :
                $"{Request.Path.Value}/{id}";
        }
    }
}
