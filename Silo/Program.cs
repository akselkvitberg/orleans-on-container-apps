// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

await Host.CreateDefaultBuilder(args)
    .UseOrleans(
        (context, builder) =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.UseLocalhostClustering()
                    .AddMemoryGrainStorage("shopping-cart")
                    .AddStartupTask<SeedProductStoreTask>();
            }
            else
            {
                var siloPort = 11111;
                var gatewayPort = 30000;

                // are we running in app service?
                if (!string.IsNullOrEmpty(context.Configuration["WEBSITE_PRIVATE_IP"]) && !string.IsNullOrEmpty(context.Configuration["WEBSITE_PRIVATE_PORTS"]))
                {
                    var endpointAddress =
                        IPAddress.Parse(context.Configuration["WEBSITE_PRIVATE_IP"]);
                    var strPorts =
                        context.Configuration["WEBSITE_PRIVATE_PORTS"].Split(',');
                    if (strPorts.Length < 2)
                        throw new Exception("Insufficient private ports configured.");
                    siloPort = int.Parse(strPorts[0]);
                    gatewayPort = int.Parse(strPorts[1]);

                    builder
                        .ConfigureEndpoints(endpointAddress, siloPort, gatewayPort);
                }
                else // looks like not, presume we're in Azure Container Apps.
                {
                    builder
                        .ConfigureEndpoints(siloPort, gatewayPort);
                }

                var connectionString =
                    context.Configuration["ORLEANS_AZURE_STORAGE_CONNECTION_STRING"];

                builder
                    .Configure<ClusterOptions>(
                        options =>
                        {
                            options.ClusterId = "ShoppingCartCluster";
                            options.ServiceId = nameof(ShoppingCartService);
                        })
                    .UseAzureStorageClustering(
                        options => options.ConfigureTableServiceClient(connectionString))
                    .AddAzureTableGrainStorage("shopping-cart",
                        options => options.ConfigureTableServiceClient(connectionString));
            }
        })
    .ConfigureWebHostDefaults(
        webBuilder => webBuilder.UseStartup<Startup>())
    .RunConsoleAsync();