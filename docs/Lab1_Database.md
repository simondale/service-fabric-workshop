# Creating the Database
The Database is a **Guest Executable** service and will include the necessary files to launch a MongoDB server.

*Currently this is the Windows version of MongoDB. Mac or Linux users will need to download and replace the Mongo files and command line*

In order to create this application in Service Fabric, we will swap to the `database` branch in git, make changes and then deploy locally.

## Get the code
Execute the following command in the root directory of this repository:
```
git checkout database
```

## Review the service
As a guest executable, Service Fabric will launch the specified binary file as a service. This will make use of partitioning, replicas and any 
placement rules applied. In this instance, we will use a singleton partition and an instance count of 1. This will ensure that only a single 
MongoDB instance exists.

### Edit ServiceManifest.xml
This file specifies the service and how it will be launched by Service Fabric. We're interested in the following section and setting the values
shown below:

```XML
<EntryPoint>
    <ExeHost>
    <Program>mongod.exe</Program>
    <Arguments>--noauth --ipv6 --bind_ip_all --dbpath=.</Arguments>
    <WorkingFolder>Work</WorkingFolder>
    <ConsoleRedirection FileRetentionCount="5" FileMaxSizeInKb="2048"/>
    </ExeHost>
</EntryPoint>
```

In the solution, the `<Program/>` element and `<Arguments/>` elements are both unset and the `<ConsoleRedirection/>` element is commented out. Change
the file to the settings shown above.

We also need to configure the service to listen on the default MongoDB port. This is done with the following settings:

```XML
<Endpoints>
    <!-- This endpoint is used by the communication listener to obtain the port on which to 
        listen. Please note that if your service is partitioned, this port is shared with 
        replicas of different partitions that are placed in your code. -->
    <Endpoint Name="MongoTypeEndpoint" Protocol="tcp" Port="27017"/>
</Endpoints>
```

By default, the `Protocol` and `Port` attributes are not set.

### Edit ApplicationManifest.xml
This file specifies the makeup of the application running in Service Fabric. In this instance, this will be a single service. To ensure that the service
is hosted as intended (i.e. Singleton with 1 Instance) the settings should be as below:

```XML
<Parameters>
    <Parameter Name="Mongo_InstanceCount" DefaultValue="1" />
</Parameters>
```

and

```XML
<Service Name="Mongo" ServicePackageActivationMode="ExclusiveProcess">
    <StatelessService ServiceTypeName="MongoType" InstanceCount="[Mongo_InstanceCount]">
        <SingletonPartition />
    </StatelessService>
</Service>
```

The default for a guest executable is `<SingletonPartition/>` as shown above, but the `DefaultValue` parameter for the `<Parameter/>` element is -1, which is 
interpretted by Service Fabric to run the service on each node in the cluster. By overriding the value and changing to 1 we will have exactly 1 instance of the 
service running.

The `<SingletonPartition/>` is also an important setting for a guest executable such as MongoDB where it runs on a well known port. Partitioning determines how
many copies of the service can run on the same node, which would cause an error for the second and subsequent services due to the port already being in use. 

When Service Fabric manages the endpoint itself this is not a problem and Partitioning is an effective way to scale services.

# Deploying the Service
If you are running Visual Studio then the service can be deployed to a local Service Fabric cluster by right 
clicking the Service Fabric Application in Solution Explorer and choosing the Publish option. This will open 
a dialog that allows the cluster to be selected. By default this will be an Azure cluster but there are also
options for Single and Five Node clusters.

![](images/publish-application.png)

Choose the option that matches your local Service Fabric cluster setup and click Publish. The output will be 
displayed in the Output Window in Visual Studio.

# Testing the Service
Included in this repository is a `utils` folder that contains the mongo client. When the service has started in Service Fabric this can be tested by executing the following command:

```
mongo mongodb://localhost:27017
```

If this successfully connects then the service is running.