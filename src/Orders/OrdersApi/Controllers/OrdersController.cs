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
        public async Task<ActionResult> Post([FromBody] Order order)
        {
            try
            {
                var state = await stateManager.GetOrAddAsync<IReliableConcurrentQueue<Order>>(StateName);

                using (var tx = stateManager.CreateTransaction())
                {
                    await state.EnqueueAsync(tx, order);
                    await tx.CommitAsync();
                    return Ok();
                }
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
    }
}
