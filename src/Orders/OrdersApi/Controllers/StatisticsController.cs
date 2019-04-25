using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Newtonsoft.Json;
using OrdersApi.Model;

namespace OrdersApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : Controller
    {
        private static readonly string StatisticsName = "Statistics";
        private readonly IReliableStateManager stateManager;

        public StatisticsController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Statistics>>> Get()
        {
            try
            {
                var statistics = await stateManager.TryGetAsync<IReliableDictionary2<string, string>>(StatisticsName);
                if (!statistics.HasValue)
                {
                    return NotFound();
                }

                var dictionary = new Dictionary<string, IEnumerable<Statistics>>();
                using (var tx = stateManager.CreateTransaction())
                {
                    var enumerable = await statistics.Value.CreateEnumerableAsync(tx);
                    var asyncEnumerator = enumerable.GetAsyncEnumerator();

                    while (await asyncEnumerator.MoveNextAsync(CancellationToken.None))
                    {
                        dictionary[asyncEnumerator.Current.Key] = JsonConvert.DeserializeObject<IEnumerable<Statistics>>(asyncEnumerator.Current.Value);
                    }

                    await tx.CommitAsync();
                }

                return Ok(dictionary.SelectMany(p => p.Value).OrderByDescending(s => s.Id?.Date));
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

        [HttpGet("{id}")]
        public Task<ActionResult<IEnumerable<Statistics>>> Get(string id)
        {
            return Task.FromResult((ActionResult<IEnumerable<Statistics>>)Ok(default(IEnumerable<Statistics>)));
        }
    }
}
