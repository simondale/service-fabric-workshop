using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using BasketActor.Interfaces;
using Microsoft.ServiceFabric.Data;

namespace BasketActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class BasketActor : Actor, IBasketActor
    {
        private static readonly string StateName = "Basket";
        private static readonly Product[] EmptyBasket = Array.Empty<Product>();

        public BasketActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync()
        {
            return Task.CompletedTask;
        }

        public Task<Product[]> GetProductsInBasket(CancellationToken cancellationToken)
        {
            return Task.FromResult(default(Product[]));
        }

        public Task AddProductToBasket(Product product, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task RemoveProductFromBasket(Guid productId, CancellationToken cancellationToken)
        {
            var state = await StateManager.TryGetStateAsync<List<Product>>(StateName, cancellationToken);
            if (state.HasValue)
            {
                var item = state.Value.FirstOrDefault(p => p.Id == productId);
                if (item != null)
                {
                    state.Value.Remove(item);
                    await StateManager.SetStateAsync(StateName, state.Value, cancellationToken);
                    await StateManager.SaveStateAsync();
                }
            }
        }

        public async Task ClearBasket(CancellationToken cancellationToken)
        {
            await StateManager.SetStateAsync(StateName, new List<Product>(), cancellationToken);
            await StateManager.SaveStateAsync();
        }
    }
}
