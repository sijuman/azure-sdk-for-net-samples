﻿namespace Network
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    
    using Authorization;
    using Profile2018Network = Microsoft.Azure.Management.Profiles.hybrid_2018_03_01.Network;
    using Microsoft.Rest.Azure;

    public class NetworkController
    {
        private const string ComponentName = "DotnetSDK_NetworkController";
        private readonly CustomLoginCredentials credential;
        private readonly string subscriotionId;
        private Uri baseUri;
        private static Profile2018Network.NetworkManagementClient client;

        public NetworkController(
            Uri baseUri,
            CustomLoginCredentials credentials,
            string subscriptionIdentifier
            )
        {
            this.baseUri = baseUri;
            this.credential = credentials;
            this.subscriotionId = subscriptionIdentifier;

            GetNetworkClient();
        }

        private void GetNetworkClient()
        {
            if (client != null)
            {
                return;
            }
            client = new Profile2018Network.NetworkManagementClient(baseUri: this.baseUri, credentials: this.credential);
            client.SubscriptionId = this.subscriotionId;
            client.SetUserAgent(ComponentName);
        }

        public async Task<AzureOperationResponse<Profile2018Network.Models.PublicIPAddress>> CreatePublicIpAddress(
            string publicIpName,
            string resourceGroupName,
            string location,
            string allocationMethod = "Dynamic",
            string publicIpAddress = null,
            IList<(string, string)> tags = null)
        {
            if (client == null)
            {
                return new AzureOperationResponse<Profile2018Network.Models.PublicIPAddress>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.ExpectationFailed,
                        ReasonPhrase = "Client is not instantiated"
                    }
                };
            }

            var publicIp = new Profile2018Network.Models.PublicIPAddress
            {
                Location = location,
            };

            if (String.Equals("dynamic", allocationMethod, StringComparison.InvariantCultureIgnoreCase))
            {
                allocationMethod = Profile2018Network.Models.IPAllocationMethod.Dynamic;
            }
            else
            {
                allocationMethod = Profile2018Network.Models.IPAllocationMethod.Static;
                if (!String.IsNullOrEmpty(publicIpAddress))
                {
                    publicIp.IpAddress = publicIpAddress;
                }
            }
            publicIp.PublicIPAllocationMethod = allocationMethod;

            foreach (var tag in tags ?? new List<(string, string)>())
            {
                var t = tag.Item1;

                publicIp.Tags.Add(tag.Item1, tag.Item2);
            }

            try
            {
                var publicIpTask = await client.PublicIPAddresses.CreateOrUpdateWithHttpMessagesAsync(
                    resourceGroupName: resourceGroupName,
                    publicIpAddressName: publicIpName,
                    parameters: publicIp);
                return publicIpTask;
            }
            catch (Exception ex){
                return new AzureOperationResponse<Profile2018Network.Models.PublicIPAddress>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        ReasonPhrase = ex.Message
                    }
                };
            }
        }

        public async Task<AzureOperationResponse<Profile2018Network.Models.VirtualNetwork>> CreateVirtualNetwork(
            string virtualNetworkName,
            IList<string> vnetAddressSpaces,
            Dictionary<string, string> subnets,
            string resourceGroupName,
            string location)
        {
            if (client == null)
            {
                return new AzureOperationResponse<Profile2018Network.Models.VirtualNetwork>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.ExpectationFailed,
                        ReasonPhrase = "Client is not instantiated"
                    }
                };
            }

            var subnetsParameters = new List<Profile2018Network.Models.Subnet>();
            foreach (var subnet in subnets ?? new Dictionary<string, string>())
            {
                if (string.IsNullOrEmpty(subnet.Value))
                {
                    return new AzureOperationResponse<Profile2018Network.Models.VirtualNetwork>
                    {
                        Response = new HttpResponseMessage
                        {
                            StatusCode = System.Net.HttpStatusCode.BadRequest,
                            ReasonPhrase = string.Format("Subnet address space is not valid. Subnet: {0}", subnet.Key)
                        }
                    };
                }
                subnetsParameters.Add(new Profile2018Network.Models.Subnet
                {
                    Name = subnet.Key,
                    AddressPrefix = subnet.Value
                });
            }

            var vnet = new Profile2018Network.Models.VirtualNetwork
            {
                Location = location,
                AddressSpace = new Profile2018Network.Models.AddressSpace
                {
                    AddressPrefixes = vnetAddressSpaces
                },
                Subnets = subnetsParameters
            };

            try
            {
                var vnetTask = await client.VirtualNetworks.CreateOrUpdateWithHttpMessagesAsync(
                    resourceGroupName: resourceGroupName,
                    virtualNetworkName: virtualNetworkName,
                    parameters: vnet);
                return vnetTask;
            }
            catch (Exception ex)
            {
                return new AzureOperationResponse<Profile2018Network.Models.VirtualNetwork>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        ReasonPhrase = ex.Message
                    }
                };
            }
        }

        public async Task<AzureOperationResponse<Profile2018Network.Models.Subnet>> AddSubnet(
            string subnetName,
            string virtualNetworkName,
            string subnetAddressSpace,
            string resourceGroupName)
        {
            if (client == null)
            {
                return new AzureOperationResponse<Profile2018Network.Models.Subnet>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.ExpectationFailed,
                        ReasonPhrase = "Client is not instantiated"
                    }
                };
            }
            try
            {
                var vnet = await client.VirtualNetworks.GetWithHttpMessagesAsync(resourceGroupName, virtualNetworkName);
                if (!vnet.Response.IsSuccessStatusCode || vnet.Body == null)
                {
                    return null;
                }
                var subnetParams = new Profile2018Network.Models.Subnet
                {
                    AddressPrefix = subnetAddressSpace
                };
                var subnetTask = await client.Subnets.CreateOrUpdateWithHttpMessagesAsync(resourceGroupName, virtualNetworkName, subnetName, subnetParams);

                if (!subnetTask.Response.IsSuccessStatusCode)
                {
                    return null;
                }
                return subnetTask;
            }
            catch (Exception ex)
            {
                return new AzureOperationResponse<Profile2018Network.Models.Subnet>
                {
                    Response = new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        ReasonPhrase = ex.Message
                    }
                };
            }
            
        }
    }
}