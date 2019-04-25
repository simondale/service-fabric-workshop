using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly HttpClient http;

        public ProductsController(HttpClient http)
        {
            this.http = http;
        }

        public async Task<IActionResult> Index()
        {
            using (var response = await http.GetAsync(await GetServiceUriAsync("Products", "ProductApi", "/api/products")))
            {
                var products = await DeserializeResponseAsync<IEnumerable<Product>>(response);
                return View(products);
            }
        }

        public IActionResult Create()
        {
            var product = new Product();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product p)
        {
            using (var content = GetJsonContent(p))
            using (var response = await http.PostAsync(await GetServiceUriAsync("Products", "ProductApi", "/api/products"), content))
            {
                var product = await DeserializeResponseAsync<Product>(response);
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Update(Guid id)
        {
            using (var response = await http.GetAsync(await GetServiceUriAsync("Products", "ProductApi", $"/api/products/{id}")))
            {
                var products = await DeserializeResponseAsync<Product>(response);
                return View(products);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Product p)
        {
            using (var content = GetJsonContent(p))
            using (var response = await http.PutAsync(await GetServiceUriAsync("Products", "ProductApi", $"/api/products/{p.Id}"), content))
            {
                var product = await DeserializeResponseAsync<Product>(response);
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            using (var response = await http.DeleteAsync(await GetServiceUriAsync("Products", "ProductApi", $"/api/products/{id}")))
            {
                return RedirectToAction("Index");
            }
        }
    }
}