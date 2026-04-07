using Azure.Identity;
using Dataverse.Showcase.FunctionApp;
using Dataverse.Showcase.Http;
using Dataverse.Showcase.Http.UserManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(c => c.AddUserSecrets<Program>(optional: true))
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var baseUrl = configuration["Dataverse:BaseUrl"]!;
        var scope = configuration["Dataverse:Scope"]!;
        var tenantId = configuration["Dataverse:TenantId"]!;

        var userA = new DataverseUser("user-a", new ClientSecretCredential(tenantId,
            configuration["Dataverse:UserA:ClientId"],
            configuration["Dataverse:UserA:ClientSecret"]));

        var userB = new DataverseUser("user-b", new ClientSecretCredential(tenantId,
            configuration["Dataverse:UserB:ClientId"],
            configuration["Dataverse:UserB:ClientSecret"]));

        // multi-user rotation with 429 handling + span tagging
        services.AddDataverseHttpClient<SampleDataverseClient>(baseUrl, scope, userA, userB);

        // single-user alternative:
        // services.AddDataverseHttpClient<SampleDataverseClient>(baseUrl, tenantId, 
        //     configuration["Dataverse:ClientId"]!, 
        //     configuration["Dataverse:ClientSecret"]!, 
        //     scope);
    })
    .Build();

host.Run();
