using BankingPlatform.Application.Abstractions;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankingPlatform.Infrastructure.Background;

public sealed class SlaEscalationWorker(IServiceScopeFactory scopeFactory, ILogger<SlaEscalationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcessAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "SLA escalation cycle failed."); }
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;

        var overdue = await db.WorkflowTasks
            .Include(x => x.AssignedToUser)
            .Include(x => x.WorkflowNode)
            .Include(x => x.Complaint)
            .Where(x => x.Status == WorkflowTaskStatus.Open && x.DueAtUtc != null && x.DueAtUtc <= now && x.EscalatedAtUtc == null)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var task in overdue)
        {
            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(task.AssignedToUser?.Email)) recipients.Add(task.AssignedToUser.Email);

            if (!string.IsNullOrWhiteSpace(task.WorkflowNode.EscalationRoleCode))
            {
                var escalationEmails = await db.DepartmentMemberships.AsNoTracking()
                    .Where(x => x.DepartmentId == task.DepartmentId && x.RoleCode == task.WorkflowNode.EscalationRoleCode && x.User.IsActive)
                    .Select(x => x.User.Email)
                    .ToListAsync(cancellationToken);
                foreach (var address in escalationEmails.Where(x => !string.IsNullOrWhiteSpace(x))) recipients.Add(address);
            }

            foreach (var address in recipients)
            {
                await email.SendAsync(
                    address,
                    $"SLA escalation - {task.Complaint.ComplaintNumber}",
                    $"Complaint {task.Complaint.ComplaintNumber} is overdue at workflow step '{task.WorkflowNode.Name}'. Due at {task.DueAtUtc:O}.",
                    cancellationToken);
            }

            task.EscalatedAtUtc = now;
            task.UpdatedAtUtc = now;
            db.ComplaintEvents.Add(new ComplaintEvent
            {
                ComplaintId = task.ComplaintId,
                EventType = "SlaEscalated",
                Message = $"SLA breached for step '{task.WorkflowNode.Name}'. Escalation notification sent."
            });
        }

        if (overdue.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }
}
