using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;


var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("BankingDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'BankingDatabase' was not found.");

builder.Services.AddElsa(elsa =>
{
    // Workflow definitions / management persistence.
    elsa.UseWorkflowManagement(management =>
    {
        management.UseEntityFrameworkCore(ef =>
        {
            ef.UseSqlServer(connectionString);
            ef.RunMigrations = true;
        });
    });

    // Workflow execution / runtime persistence.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore(ef =>
        {
            ef.UseSqlServer(connectionString);
            ef.RunMigrations = true;
        });
    });

    // Timers, Delay, StartAt, etc.
    elsa.UseScheduling();

    // HTTP workflow activities / endpoints.
    elsa.UseHttp();

    // Elsa REST API.
    elsa.UseWorkflowsApi();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("x-elsa-workflow-instance-id");
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();

app.UseRouting();

// Mount Elsa REST endpoints.
app.UseWorkflowsApi();

// Required for HTTP workflow activities.
app.UseWorkflows();

app.MapHealthChecks("/health");

app.Run();