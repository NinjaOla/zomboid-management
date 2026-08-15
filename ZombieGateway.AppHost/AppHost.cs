var builder = DistributedApplication.CreateBuilder(args);

var managementApi = builder.AddProject<Projects.ZombieManagementApi>("zombie-management-api");

builder.AddProject<Projects.ZombieGateway>("zombie-gateway")
    .WithEnvironment("ManagementApi__BaseUrl", managementApi.GetEndpoint("http"))
    .WaitFor(managementApi);

builder.Build().Run();
