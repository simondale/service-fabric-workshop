using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;

[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace BasketActor.Interfaces
{
    public interface IBasketActor : IActor
    {
        Task<Product[]> GetProductsInBasket(CancellationToken cancellationToken);
        Task AddProductToBasket(Product product, CancellationToken cancellationToken);
        Task RemoveProductFromBasket(Guid productId, CancellationToken cancellationToken);
        Task ClearBasket(CancellationToken cancellationToken);
    }
}
