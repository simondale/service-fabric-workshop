# Creating the Products service
The Products service is a **Stateless Service** and will expose an API microservice that implements CRUD operations 
on entities stored within the MongoDB database. This will be an ASP.NET MVC WebAPI application written in .NET core
so will support Windows, Mac and Linux environments.

The concept for this service is to not maintain state within the service itself but instead to persist data in an 
external repository. This will be the MongoDB instance created in [Lab 1](Lab1_Database.md).

In order to do this, the service will need to identify where the Mongo service is running, establish a connection
and then perform the necessary operations. To allow maximum flexibility in this application we will also include
the option to specify a full MongoDB connection string. All of these settings will be made available through a
**Config** package included with the service.

## Get the code
Execute the following command in the root directory of this repository:

```
git checkout products
```

## Review the service
This service is an API that exposes CRUD operations for Products. In order to test this service we will add support
for **Swagger** and **Swashbuckle** and make these available through the Service Fabric reverse proxy. As already 
stated above, the service persists data to MongoDB so the **MongoDB Driver** is required too.

The configuration settings will be read from a **Config** package and as this can be updated independently to the 
**Code** package then a listener will be added that updates the MongoDB connection. This will be implemented in 
the product repository.

### Edit ProductApi.csproj
We'll start by updating the project file to pull in the dependencies:

```XML
<PackageReference Include="MongoDB.Driver" Version="2.8.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="4.0.1" />
```

### Edit ProductApi.cs
Next we'll update the `WebHostBuilder` in the stateless service implementation file so that the Service Fabric
integration to ASP.NET is aware of the reverse proxy. This is an important setting because it influences how 
`HTTP 400` error codes are handled.

The HTTP Specification specifies that the `404 Not Found` error code is temporary and in the context of Service
Fabric this could mean that a service is not found or could mean that a path within a service is not found. To
support failover to replicas, Service Fabric implements a timeout of 120s before it returns the `404` error to 
the caller with the expectation that the service will be found within this period. Unfortunately, if the service
is available and it is a path within that service that is not found then the same behaviour is triggered unless
a header is included in the response to inform Service Fabric that the request was processed and the resource really
could not be found.

The header is:

```
X-Service-Fabric: ResourceNotFound
```

This can be automatically sent with the `ServiceFabricIntegrationOptions.UseReverseProxyIntegration` flag shown below:

```csharp
return new WebHostBuilder()
    .UseKestrel()
    .ConfigureServices(
        services => services
            .AddSingleton<StatelessServiceContext>(serviceContext))
    .UseContentRoot(Directory.GetCurrentDirectory())
    .UseStartup<Startup>()
    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseReverseProxyIntegration)
    .UseUrls(url)
    .Build();
```

### Edit Startup.cs
This file is used to setup and configure dependencies used by the Web API. This is where we will specify
the product repository and also where we will setup Swagger and Swashbuckle.

The `ConfigureServices` method is relatively boiler-plate for Swagger and also injects the repository as
a singleton. This is so that the connection to MongoDD is reused across all clients.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
    services.AddSwaggerGen(options => options.SwaggerDoc("v1", new Info { Title = "Products", Description = "Products API" }));
    services.AddSingleton<IProductRepository, ProductRepository>();
}
```

The `Configure` method is more involved as we must setup the Swagger integration to handle calls through
the Service Fabric reverse proxy. This is achieved through the use of a `PreSerializeFilter` which checks
the value in the `Referer` header. This allows Swagger to be accessed through either a direct call to the 
service running on a node or through the reverse proxy.

When the call is made through the reverse proxy, as identified by a prefix of `Products/ProductApi` (which
corresponds to `Application/Service`) then a base path is set within the Swagger JSON file so that clients
receive the reverse proxy aware paths to the service.

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseStaticFiles();
    app.UseSwagger(options => options.PreSerializeFilters.Add((d, r) =>
    {
        ServiceEventSource.Current.Message($"Referer: {r.Headers["Referer"]}");
        if (r.Headers.TryGetValue("Referer", out var value))
        {
            var referer = new PathString(new Uri(value.ToString()).AbsolutePath);
            if (referer.StartsWithSegments(new PathString("/swagger")))
            {
                d.BasePath = "/";
            }
            else
            {
                var applicationService = Environment.GetEnvironmentVariable("Fabric_ServiceName").Replace("fabric:", "");
                if (referer.StartsWithSegments(new PathString(applicationService)))
                {
                    d.BasePath = applicationService;
                }
            }
        }
    }));
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Products API"));
    app.UseMvc();
}
```

### Edit ProductRepository.cs
We inject the `StatelessServiceContext` into the product repository to allow access to the `CodePackageActivationContext`
and through that the `ConfigurationPackageObject` containing the configuration settings for MongoDB. So that we can detect
changes to this configuration file should a delta deployment occur we register an event handler that will be invoked when
the configuration package is changed. 

```csharp
public ProductRepository(StatelessServiceContext context)
{
    context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += OnConfigurationPackageModified;
    OnConfigurationPackageModified(this, new PackageModifiedEventArgs<ConfigurationPackage>
    {
        NewPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config")
    });
}
```

The event handler method will check for the presence of MongoDB settings and if found will use these to create a new
connection to the database. If the specified connection string to MongoDB contains the token `{application:service}`
then it will resolve the location of the Mongo service from Service Fabric. This uses the `ServicePartitionResolver` 
class to resolve the address for the primary replica of the Mongo service. The endpoint is a JSON documennt which is 
parsed and the `address:port` pair are extracted.

```csharp
private void OnConfigurationPackageModified(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
{
    var section = e.NewPackage?.Settings?.Sections?["Database"];
    var connectionString = section?.Parameters?["ConnectionString"]?.Value;
    if (connectionString != null)
    {
        if (connectionString.Contains("{application:service}"))
        {
            var application = section.Parameters["Application"].Value;
            var service = section.Parameters["Service"].Value;
            var resolver = ServicePartitionResolver.GetDefault();
            var partition = resolver.ResolveAsync(new Uri($"fabric:/{application}/{service}"), new ServicePartitionKey(), CancellationToken.None).GetAwaiter().GetResult();
            var address = JObject.Parse(partition.Endpoints.Select(ep => ep.Address).First()).SelectToken("Endpoints").ToObject<JObject>().Properties().First().Value.Value<string>();
            connectionString = connectionString.Replace("{application:service}", address);
        }

        var client = new MongoClient(connectionString);
        lock (sync)
        {
            database = client.GetDatabase(section.Parameters["Database"].Value);
            collection = section.Parameters["Collection"].Value;
        }
    }
}
```

### Edit Settings.xml
The settings for the MongoDB database are shown below. As can be seen, the token `{application:service}`
is specified so the location will be resolved by requesting the location of the `Application` and `Service`
pair. The `Database` and `Collection` settings will always be used once the connection string is known.

```XML
<Section Name="Database">
    <Parameter Name="ConnectionString" Value="mongodb://{application:service}"/>
    <Parameter Name="Application" Value="Database" />
    <Parameter Name="Service" Value="Mongo"/>
    <Parameter Name="Database" Value="Products"/>
    <Parameter Name="Collection" Value="Product"/>
</Section>
```

## Application and Service Manifests
There are no significant settings made in either the `ApplicationManifest.xml` or `ServiceManifest.xml`
files so we will move onto deployment.

# Deploying the Service
If you are running Visual Studio then the service can be deployed to a local Service Fabric cluster by right 
clicking the Service Fabric Application in Solution Explorer and choosing the Publish option. This will open 
a dialog that allows the cluster to be selected. By default this will be an Azure cluster but there are also
options for Single and Five Node clusters.

![](images/publish-application.png)

Choose the option that matches your local Service Fabric cluster setup and click Publish. The output will be 
displayed in the Output Window in Visual Studio.

# Testing the Service
In order to test the service we can either use the Swagger UI and submit requests through a web page, or we can use
the command line tool `curl`. The Swagger UI page is available here: [http://localhost:19081/Products/ProductApi/swagger](http://localhost:19081/Products/ProductApi/swagger)

## Create products
For the rest of this workshop we are going to need some products in the MongoDB database so we will create them here.
Issue requests to the following URL with the Headers and Body data shown below:

### URL
`POST` [http://localhost:19081/Products/ProductApi/api/products](http://localhost:19081/Products/ProductApi/api/products)

### Headers
```
Content-Type: application/json
```

### Body
```JSON
{
  "name": "Apples",
  "price": 0.80
}
```

```JSON
{
  "name": "Bread",
  "price": 1.30
}
```

```JSON
{
  "name": "Milk",
  "price": 0.65
}
```

```JSON
{
  "name": "Soup",
  "price": 1.10
}
```

## Query products
Now we'll confirm that the products we have created are persisted in the database. Again, using either the Swagger UI or curl, the details are below:

### URI
`GET` [http://localhost:19081/Products/ProductApi/api/products](http://localhost:19081/Products/ProductApi/api/products)

### Headers
```
Accept: application/json
```