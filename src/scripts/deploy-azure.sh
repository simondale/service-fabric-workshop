#!/bin/bash

: ${CLUSTER_NAME=sfw`date +%Y%m%d%S`}
: ${RESOURCE_GROUP=$CLUSTER_NAME}
: ${LOCATION="westeurope"}
: ${VM_OS="WindowsServer2016Datacenter"}
: ${VM_SKU="Standard_D4_v3"}
: ${CLUSTER_SIZE="5"}
: ${VM_PASSWORD=""}

# create the service fabric cluster
az sf cluster create \
    --name $CLUSTER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --vm-sku $VM_SKU \
    --vm-os $VM_OS \
    --vm-password ${VM_PASSWORD} \
    --certificate-subject-name ${CLUSTER_NAME} \
    --cluster-size ${CLUSTER_SIZE}

# download the PFX file for the cluster
: ${SECRET_ID=$(az keyvault secret list --vault-name $CLUSTER_NAME --query "[?contentType==\`application/x-pkcs12\`].id | [0]" | sed -e 's/\"//g')}
az keyvault secret download --id $SECRET_ID --file $CLUSTER_NAME.pfx

# get the node type
: ${PIP_ID=$(az resource list --resource-group $RESOURCE_GROUP --resource-type Microsoft.Network/publicIPAddresses --query "[?tags.clusterName==\`$CLUSTER_NAME\`].id|[0]" | sed -e 's/\"//g')}
: ${IP_ADDRESS=$(az resource show --id $PIP_ID --query "properties.ipAddress" | sed -e 's/\"//g')}
: ${ENDPOINT=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.managementEndpoint" | sed -e 's/\"//g')}
: ${DNS_NAME=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.managementEndpoint" | sed -e 's/.*https:\/\///g' | sed -e 's/:.*//g')}
: ${CLIENT_CONNECTION_PORT=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.nodeTypes[*].clientConnectionEndpointPort|[0]")}
: ${THUMBPRINT=$(az resource show --resource-group $RESOURCE_GROUP --name $CLUSTER_NAME --resource-type Microsoft.ServiceFabric/clusters --query "properties.certificate.thumbprint" | sed -e 's/\"//g')}

echo Cluster Endpoint: tcp://$DNS_NAME:$CLIENT_CONNECTION_PORT
echo Server Certificate Thumbprint: $THUMBPRINT
echo Client Certificate:
cat $CLUSTER_NAME.pfx
