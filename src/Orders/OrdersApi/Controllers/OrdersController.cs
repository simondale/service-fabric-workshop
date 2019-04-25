using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using OrdersApi.Model;

namespace OrdersApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private static readonly string StateName = "Orders";
        private readonly IReliableStateManager stateManager;

        public OrdersController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpPost]
        public Task<ActionResult> Post([FromBody] Order order)
        {
            return Task.FromResult((ActionResult)Ok());
        }
    }
}
