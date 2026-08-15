var builder = DistributedApplication.CreateBuilder(args);

// Shared API key injected into both services — no manual config needed in dev.
// In production, set MANAGEMENT_API_KEY as an environment variable or secret.
var managementApiKey = builder.AddParameter("management-api-key", secret: true);

var managementApi = builder.AddProject<Projects.ZombieManagementApi>("zombie-management-api")
    .WithEnvironment("ManagementAuth__ApiKey", managementApiKey);

builder.AddProject<Projects.ZombieGateway>("zombie-gateway")
    .WithEnvironment("ManagementApi__BaseUrl", managementApi.GetEndpoint("http"))
    .WithEnvironment("ManagementApi__ApiKey", managementApiKey)
    .WaitFor(managementApi);

builder.Build().Run();
