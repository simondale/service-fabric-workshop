using System;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web.Controllers
{
    public abstract class BaseController : Controller
    {
        private static readonly JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static int index = 0;

        protected StringContent GetJsonContent<T>(T obj)
        {
            var content = new StringContent(JsonConvert.SerializeObject(obj));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        protected Task<Uri> GetServiceUriAsync(string application, string service, string path, Func<ServicePartitionKey> partitionKeyGenerator = null)
        {
            var uri = new Uri($"{path}");
            return Task.FromResult(uri);
        }

        protected async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage message)
        {
            using (var stream = await message.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            using (var json = new JsonTextReader(reader))
            {
                return serializer.Deserialize<T>(json);
            }
        }
    }
}
