using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Web.Models;

namespace Web.Controllers
{
    public class ReportController : BaseController
    {
        private readonly HttpClient http;

        public ReportController(HttpClient http)
        {
            this.http = http;
        }

        public async Task<IActionResult> Index()
        {
            using (var response = await http.GetAsync(await GetServiceUriAsync("Orders", "OrdersApi", $"/api/statistics", () => new ServicePartitionKey(1))))
            {
                var products = await DeserializeResponseAsync<IEnumerable<Statistics>>(response);
                return View(products);
            }
        }
    }
}