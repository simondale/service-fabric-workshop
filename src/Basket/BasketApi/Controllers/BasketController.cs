using System;
using System.Threading;
using System.Threading.Tasks;
using BasketActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace BasketApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : Controller
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(Guid id)
        {
            try
            {
                var actorId = new ActorId($"{id:N}");
                var actor = ActorProxy.Create<IBasketActor>(actorId, new Uri("fabric:/Basket/BasketActorService"));
                return Ok(await actor.GetProductsInBasket(CancellationToken.None));
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

        [HttpPost("{id}/items")]
        public async Task<ActionResult> Post(Guid id, [FromBody] Product value)
        {
            try
            {
                var actorId = new ActorId($"{id:N}");
                var actor = ActorProxy.Create<IBasketActor>(actorId, new Uri("fabric:/Basket/BasketActorService"));
                await actor.AddProductToBasket(value, CancellationToken.None);
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

        [HttpDelete("{id}/items/{productId}")]
        public async Task<ActionResult> Delete(Guid id, Guid productId)
        {
            try
            {
                var actorId = new ActorId($"{id:N}");
                var actor = ActorProxy.Create<IBasketActor>(actorId, new Uri("fabric:/Basket/BasketActorService"));
                await actor.RemoveProductFromBasket(productId, CancellationToken.None);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var actorId = new ActorId($"{id:N}");
                var actor = ActorProxy.Create<IBasketActor>(actorId, new Uri("fabric:/Basket/BasketActorService"));
                await actor.ClearBasket(CancellationToken.None);
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
    }
}
