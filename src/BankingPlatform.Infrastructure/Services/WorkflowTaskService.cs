using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class WorkflowTaskService(AppDbContext db, ICurrentUser currentUser, IWorkflowRuntime runtime) : IWorkflowTaskService
{
    public async Task<IReadOnlyList<WorkflowTaskDto>> GetMyBucketAsync(CancellationToken cancellationToken)
    {
        var memberships = await db.DepartmentMemberships.AsNoTracking()
            .Where(x => x.UserId == currentUser.UserId)
            .Select(x => new { x.DepartmentId, x.RoleCode })
            .ToListAsync(cancellationToken);

        var roleKeys = memberships.Select(x => x.DepartmentId + "|" + x.RoleCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tasks = await db.WorkflowTasks.AsNoTracking()
            .Include(x => x.Complaint)
            .Include(x => x.WorkflowNode)
            .Include(x => x.AssignedToUser)
            .Where(x => x.Status == WorkflowTaskStatus.Open && (x.AssignedToUserId == currentUser.UserId || x.AssignedToUserId == null))
            .OrderBy(x => x.DueAtUtc ?? DateTime.MaxValue)
            .ToListAsync(cancellationToken);

        return tasks
            .Where(x => x.AssignedToUserId == currentUser.UserId ||
                        (x.AssignedToUserId is null && x.AssignedRoleCode is not null && roleKeys.Contains(x.DepartmentId + "|" + x.AssignedRoleCode)))
            .Select(x => new WorkflowTaskDto(
                x.Id,
                x.ComplaintId,
                x.DepartmentId,
                x.Complaint.ComplaintNumber,
                x.Complaint.Subject,
                x.WorkflowNode.Name,
                x.AssignedRoleCode,
                x.AssignedToUserId,
                x.AssignedToUser?.DisplayName,
                x.OpenedAtUtc,
                x.DueAtUtc,
                x.EscalatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<WorkflowTaskActionDto>> GetActionsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks.AsNoTracking()
            .Include(x => x.WorkflowNode)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Nodes)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            return Array.Empty<WorkflowTaskActionDto>();

        var canAct = task.AssignedToUserId == currentUser.UserId;
        if (!canAct && task.AssignedToUserId is null && !string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            canAct = await db.DepartmentMemberships.AsNoTracking().AnyAsync(x =>
                x.UserId == currentUser.UserId &&
                x.DepartmentId == task.DepartmentId &&
                x.RoleCode == task.AssignedRoleCode, cancellationToken);
        }
        if (!canAct) throw new UnauthorizedAccessException("This task is not assigned to you or your role queue.");

        var definition = task.WorkflowInstance.WorkflowDefinition;
        var outgoing = definition.Transitions.Where(x => x.SourceNodeId == task.WorkflowNodeId).ToList();
        var actions = new List<WorkflowTaskActionDto>();

        foreach (var transition in outgoing)
        {
            var target = definition.Nodes.Single(x => x.Id == transition.TargetNodeId);
            actions.Add(new WorkflowTaskActionDto(
                transition.OutcomeKey,
                string.IsNullOrWhiteSpace(transition.Label)
                    ? BuildDefaultActionLabel(transition.OutcomeKey, target.Name)
                    : transition.Label!,
                target.Id,
                target.Name,
                target.Type,
                target.RoleCode,
                RequiresSpecificAssignee(target)));
        }

        return actions;
    }

    public Task CompleteAsync(Guid taskId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken) =>
        runtime.CompleteTaskAsync(taskId, currentUser.UserId, request, cancellationToken);

    public async Task ReassignAsync(Guid taskId, ReassignWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks.Include(x => x.WorkflowNode).FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");
        if (task.Status != WorkflowTaskStatus.Open) throw new InvalidOperationException("Only open tasks can be reassigned.");

        var actorCanManage = await db.DepartmentMemberships.AnyAsync(x =>
            x.UserId == currentUser.UserId && x.DepartmentId == task.DepartmentId &&
            (x.RoleCode == "TeamLead" || x.RoleCode == "UnitHead"), cancellationToken);
        if (!actorCanManage) throw new UnauthorizedAccessException("Only a TeamLead or UnitHead can reassign this task.");

        if (!string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            var targetHasRole = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == request.UserId && x.DepartmentId == task.DepartmentId && x.RoleCode == task.AssignedRoleCode, cancellationToken);
            if (!targetHasRole) throw new InvalidOperationException($"Target user does not have role '{task.AssignedRoleCode}'.");
        }

        task.AssignedToUserId = request.UserId;
        task.UpdatedAtUtc = DateTime.UtcNow;
        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = task.ComplaintId,
            ActorUserId = currentUser.UserId,
            EventType = "WorkflowTaskReassigned",
            Message = string.IsNullOrWhiteSpace(request.Comment)
                ? $"Step '{task.WorkflowNode.Name}' was reassigned."
                : $"Step '{task.WorkflowNode.Name}' was reassigned. Comment: {request.Comment}"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildDefaultActionLabel(string? outcomeKey, string targetName)
    {
        if (!string.IsNullOrWhiteSpace(outcomeKey))
        {
            var readable = string.Concat(outcomeKey.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
            return char.ToUpperInvariant(readable[0]) + readable[1..];
        }
        return $"Continue to {targetName}";
    }

    private static bool RequiresSpecificAssignee(WorkflowNode node)
    {
        if (string.IsNullOrWhiteSpace(node.ConfigJson)) return false;
        try
        {
            using var document = JsonDocument.Parse(node.ConfigJson);
            if (!document.RootElement.TryGetProperty("assignmentMode", out var value)) return false;
            return string.Equals(value.GetString(), "specific", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
