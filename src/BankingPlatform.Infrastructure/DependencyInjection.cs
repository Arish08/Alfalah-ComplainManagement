using BankingPlatform.Application.Abstractions;
using BankingPlatform.Infrastructure.Background;
using BankingPlatform.Infrastructure.Persistence;
using BankingPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("BankingDatabase")));

        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>();
        services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();
        services.AddScoped<IWorkflowRuntime, SqlWorkflowRuntime>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<SlaEscalationWorker>();
        return services;
    }
}
