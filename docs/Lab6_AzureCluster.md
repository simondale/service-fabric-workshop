# Deploying to an Azure Service Fabric Cluster
Before we can deploy the solution to Azure we need to build a Service Fabric cluster and to then obtain some 
information so that the build pipelines can connect to the cluster and deploy applications.

To assist with this, a deployment script is available [here](../src/scripts/deploy-azure.sh).

To execute the script run the following command:

```bash
. src/scripts/deploy-azure.sh
```

This script is written for bash and has been tested on either Ubuntu Linux but also the [Azure Shell](https://shell.azure.com).

The script starts with an environment block that describes the cluster that will be created:

```bash
: ${CLUSTER_NAME=sfw`date +%Y%m%d%S`}
: ${RESOURCE_GROUP=$CLUSTER_NAME}
: ${LOCATION="westeurope"}
: ${VM_OS="WindowsServer2016Datacenter"}
: ${VM_SKU="Standard_D4_v3"}
: ${CLUSTER_SIZE="5"}
: ${VM_PASSWORD=""}
```

Here we set the cluster and resource group names to be date based since the cluster requires a globally unique name. 
The OS and SKU for the VMs in the cluster can also be configured here along with the cluster size.

**Due to the way that Service Fabric utilises Fault and Upgrade domains the minimum recommended and supportable cluster
size is 5 nodes. This allows nodes to be faulted and upgrading and to still provide a quorum for high availability**

Once the variables have been set then the AZCLI is used to deploy the infrastructure. The command for this is:

```bash
az sf cluster create \
    --name $CLUSTER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --vm-sku $VM_SKU \
    --vm-os $VM_OS \
    --vm-password ${VM_PASSWORD} \
    --certificate-subject-name ${CLUSTER_NAME} \
    --cluster-size ${CLUSTER_SIZE}
```

When this command has completed there are some settings that will be required to establish build pipelines. These are:
* Cluster Endpoint
* Server Certificate Thumbprint
* Client Certificate (PFX)

The first two of these settings are obtained with the following commands:

```bash
: ${DNS_NAME=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.managementEndpoint" | sed -e 's/.*https:\/\///g' | sed -e 's/:.*//g')}
: ${CLIENT_CONNECTION_PORT=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.nodeTypes[*].clientConnectionEndpointPort|[0]")}
: ${THUMBPRINT=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.certificate.thumbprint" | sed -e 's/\"//g')}
```

While the PFX for the client certificate is obtained by executing:

```bash
: ${SECRET_ID=$(az keyvault secret list --vault-name $CLUSTER_NAME --query "[?contentType==\`application/x-pkcs12\`].id | [0]" | sed -e 's/\"//g')}
az keyvault secret download --id $SECRET_ID --file $CLUSTER_NAME.pfx
cat $CLUSTER_NAME.pfx
```

Executing the script will execute all of these commands and output the relevant information to the console.

To install the PFX file the string can be converted to a PFX file with the following Powershell command:

```Powershell
$bytes = [Convert]::FromBase64String("...copy base64 here...")
[System.IO.File]::WriteAllBytes("cert.pfx", $bytes)
```

The PFX file can then be installed and used for Client Certificate Authentication when accessing Service Fabric Explorer.