using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Model;

namespace OrdersApi.Services
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<Statistics>> AddOrderAsync(Order order, CancellationToken cancellationToken);
    }
}
