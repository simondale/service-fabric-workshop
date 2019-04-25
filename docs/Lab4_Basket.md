# Creating the Basket application
The Basket application comprises of two services. Firstly a **Stateless Service** serves as an API that accepts
requests from the client before identifying a **Reliable Actor** that can process the request.

The **Reliable Actor** is a lightweight, partitioned service that implements the **Actor Pattern**. Each actor 
is a specialisation of a stateful service but with additional synchronisation guarantees, handled through a 
messaginig protocol. This ensures that messages are processed one at a time reducing synchronisation issues.
Actors also have lifetime management and will timeout after a short period, making them ideal for transient
data such as a web site session data. It is also possible to detect when an actor is activated or deactivated
and use these events to load/save state, thus having highly-available and low-latency access to 'warm' data
yet still retaining cold data.

## Get the code
Execute the following command in the root directory of this repository:

```
git checkout basket
```

## Review the Appication
The Basket application contains two services the Basket API and the Basket Actor. In addition there is a 
shared Interfaces project that includes the strongly typed actor interface and data contracts.

### Edit BasketApi.csproj
In order to access the Basket Actor, the API project requires a reference to the Actor Interfaces.

```XML
<ItemGroup>
    <ProjectReference Include="..\BasketActor.Interfaces\BasketActor.Interfaces.csproj" />
</ItemGroup>
```

### Edit BasketController.cs in BasketApi
The Basket Controller will issue requests to Basket Actor instances identified by the ID passed to the method
as a parameter. Each actor will then update its internal state to maintain the product list.

In order to access the Actor, an `ActorId` is required to pass to `ActorProxy.Create<T>()`. This method accepts
both the ID and also a service type URI to reference the actor. The value returned will be a proxy to the actor 
instance and is exposed through an interface, `IBasketActor` in this case.

```csharp
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
```

### Edit BasketActor.cs in BasketActor
As previously mentioned, the Actor can handle activation and deactivation. In this example we simply
raise an event indicating this has occurred, but these methods could equally be used to persist state
to an enduring data source or to load state previously persisted.

Called when the Actor is activated:

```csharp
protected override Task OnActivateAsync()
{
    ActorEventSource.Current.ActorMessage(this, "Actor activated.");
    return StateManager.TryAddStateAsync(StateName, new List<Product>());
}
```

Called when the Actor is deallocated:

```csharp
protected override Task OnDeactivateAsync()
{
    ActorEventSource.Current.ActorMessage(this, "Actor deactivated.");
    return Task.CompletedTask;
}
```

In addition to activation and deactivation methods, the Actor itself implements the methods defined 
by its interface.

Here is an exmple of accessing state through the `StateManager` class and if the state is found it
is simply returned.

```csharp
public async Task<Product[]> GetProductsInBasket(CancellationToken cancellationToken)
{
    var state = await StateManager.TryGetStateAsync<List<Product>>(StateName, cancellationToken);
    return state.HasValue ? state.Value.ToArray() : EmptyBasket;
}
```

State can also be updated through the `StateManager` and when this occurs it is important to understand 
how state is managed. The data is serialized into the StateManager, meaning that a value previously
obtained will not update the underlying store. Therefore it is important to always persist the updated
state in its entirity back to the StateManager.

It is also important to save the changes to state with a call to `SaveStateAsync`.

```csharp
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
```

# Deploying the Service
If you are running Visual Studio then the service can be deployed to a local Service Fabric cluster by right 
clicking the Service Fabric Application in Solution Explorer and choosing the Publish option. This will open 
a dialog that allows the cluster to be selected. By default this will be an Azure cluster but there are also
options for Single and Five Node clusters.

![](images/publish-application.png)

Choose the option that matches your local Service Fabric cluster setup and click Publish. The output will be 
displayed in the Output Window in Visual Studio.

# Updating the Web Application
The ASP.NET MVC application from [Lab 3](Lab3_WebApplication.md) will also be updated to support adding products
to the basket or removing items from the basket.

## Review Service
The products view will be updated to provide a link to add products to a basket. Each basket will be identified
by a GUID stored in a cookie. A view will also be added to allow the contents of the basket to be viewed and 
existing items in the basket to be removed.

### Edit BaseController.cs

The BaseController class will be updated to include methods to get and set cookies used to identify the basket.

Get an existing cookie or set a new value if the cookie is not present:

```csharp
protected string GetBasketId()
{
    if (!Request.Cookies.TryGetValue("Basket", out var basketId))
    {
        basketId = $"{Guid.NewGuid()}";
        Response.Cookies.Append("Basket", basketId);
    }

    return basketId;
}
```

Remove an existing cookie when a basket is deleted:

```csharp
protected void ClearBasketId()
{
    Response.Cookies.Delete("Basket");
}
```

## Deploying the update
It is necessary to update the `CodePackage` version for the service in order to carry out an update. This follows
the same process as a typical deployment. The `Manifest Versions` button can be clicked to view the updated code
version, which should appear as follows:

![](images/edit-versions.png)

Set the `New Version` for the `Code` package to be `2.0.0` and ensure that the checkbox to update the application 
and service versions is ticked. Once the versions have been updated then click `Save` and then `Publish` to deploy
the updated application.

# Testing the Service

When the Basket application has been deployed and Application has been updated then the new basket functionality can be
tested. This can either be via the Swagger UI, going directly to the Basket API or can be through the updated UI.

The Swagger endpoint for the Basket API is [http://localhost:19081/Basket/BasketApi/swagger](http://localhost:19081/Basket/BasketApi/swagger)

The UI is accessible through [http://localhost](http://localhost) and a link on the updated home page.