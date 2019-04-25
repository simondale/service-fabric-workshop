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
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            return StateManager.TryAddStateAsync(StateName, new List<Product>());
        }

        protected override Task OnDeactivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor deactivated.");
            return Task.CompletedTask;
        }

        public async Task<Product[]> GetProductsInBasket(CancellationToken cancellationToken)
        {
            var state = await StateManager.TryGetStateAsync<List<Product>>(StateName, cancellationToken);
            return state.HasValue ? state.Value.ToArray() : EmptyBasket;
        }

        public async Task AddProductToBasket(Product product, CancellationToken cancellationToken)
        {
            var state = await StateManager.TryGetStateAsync<List<Product>>(StateName, cancellationToken);
            if (state.HasValue)
            {
                state.Value.Add(product);
            }
            else
            {
                state = new ConditionalValue<List<Product>>(true, new List<Product>() { product });
            }

            await StateManager.SetStateAsync(StateName, state.Value, cancellationToken);
            await StateManager.SaveStateAsync();
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
