using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data.Collections;
using OrdersApi.Services;
using OrdersApi.Model;
using Newtonsoft.Json;

namespace OrdersApi
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class OrdersApi : StatefulService
    {
        private static readonly string StateName = "Orders";
        private static readonly string StatisticsName = "Statistics";

        private readonly IOrdersRepository repository;

        public OrdersApi(StatefulServiceContext context, IOrdersRepository repository)
            : base(context)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton(serviceContext)
                                            .AddSingleton(StateManager)
                                            .AddSingleton(repository))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl | ServiceFabricIntegrationOptions.UseReverseProxyIntegration)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var queue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Order>>(StateName);
            var statistics = await StateManager.GetOrAddAsync<IReliableDictionary2<string, string>>(StatisticsName);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = StateManager.CreateTransaction())
                {
                    var result = await queue.TryDequeueAsync(tx, cancellationToken);
                    if (result.HasValue)
                    {
                        var currentStatistics = await repository.AddOrderAsync(result.Value, cancellationToken);
                        if (currentStatistics != null)
                        {
                            var json = JsonConvert.SerializeObject(currentStatistics);
                            await statistics.AddOrUpdateAsync(tx, $"{result.Value.OrderDateTime:yyyyMMdd}", json, (id, stats) => json);
                        }

                        await tx.CommitAsync();
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
            }
        }
    }
}
