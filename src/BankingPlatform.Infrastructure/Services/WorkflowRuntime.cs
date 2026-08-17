using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class SqlWorkflowRuntime(AppDbContext db) : IWorkflowRuntime
{
    public async Task StartAsync(Complaint complaint, CancellationToken cancellationToken)
    {
        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .Where(x => x.CategoryId == complaint.CategoryId && x.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No published workflow exists for this complaint category.");

        var instance = new WorkflowInstance
        {
            ComplaintId = complaint.Id,
            Complaint = complaint,
            WorkflowDefinitionId = definition.Id,
            Status = WorkflowInstanceStatus.Running
        };
        db.WorkflowInstances.Add(instance);
        complaint.CurrentWorkflowInstanceId = instance.Id;
        complaint.Status = ComplaintStatus.InProgress;

        var start = definition.Nodes.Single(x => x.Type == WorkflowNodeType.Start);
        await MoveFromNodeAsync(instance, definition, start, outcomeKey: null, nextAssigneeUserId: null, actorUserId: complaint.CreatedByUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteTaskAsync(Guid taskId, Guid actorUserId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.WorkflowTasks
            .Include(x => x.WorkflowNode)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Nodes)
            .Include(x => x.WorkflowInstance)
                .ThenInclude(x => x.WorkflowDefinition)
                    .ThenInclude(x => x.Transitions)
            .Include(x => x.Complaint)
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken)
            ?? throw new KeyNotFoundException("Task not found.");

        if (task.Status != WorkflowTaskStatus.Open)
            throw new InvalidOperationException("Task is no longer open.");

        var canAct = task.AssignedToUserId == actorUserId;
        if (!canAct && task.AssignedToUserId is null && !string.IsNullOrWhiteSpace(task.AssignedRoleCode))
        {
            canAct = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == actorUserId &&
                x.DepartmentId == task.DepartmentId &&
                x.RoleCode == task.AssignedRoleCode, cancellationToken);
        }

        if (!canAct) throw new UnauthorizedAccessException("This task is not assigned to you or your role queue.");

        if (request.NextAssigneeUserId.HasValue)
        {
            var userExists = await db.Users.AnyAsync(x => x.Id == request.NextAssigneeUserId.Value && x.IsActive, cancellationToken);
            if (!userExists) throw new ArgumentException("Selected assignee does not exist or is inactive.");
        }

        task.Status = WorkflowTaskStatus.Completed;
        task.CompletedAtUtc = DateTime.UtcNow;
        task.CompletedByUserId = actorUserId;
        task.CompletionComment = request.Comment;
        task.UpdatedAtUtc = DateTime.UtcNow;

        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = task.ComplaintId,
            ActorUserId = actorUserId,
            EventType = "WorkflowTaskCompleted",
            Message = $"Completed workflow step '{task.WorkflowNode.Name}'."
        });

        task.WorkflowInstance.Complaint = task.Complaint;
        var definition = task.WorkflowInstance.WorkflowDefinition;
        await MoveFromNodeAsync(task.WorkflowInstance, definition, task.WorkflowNode, request.OutcomeKey, request.NextAssigneeUserId, actorUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MoveFromNodeAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        WorkflowNode sourceNode,
        string? outcomeKey,
        Guid? nextAssigneeUserId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var outgoing = definition.Transitions.Where(x => x.SourceNodeId == sourceNode.Id).ToList();
        WorkflowTransition? transition;

        if (!string.IsNullOrWhiteSpace(outcomeKey))
        {
            transition = outgoing.FirstOrDefault(x => string.Equals(x.OutcomeKey, outcomeKey, StringComparison.OrdinalIgnoreCase))
                ?? outgoing.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.OutcomeKey));
        }
        else
        {
            transition = outgoing.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.OutcomeKey));
            if (transition is null && outgoing.Count == 1) transition = outgoing[0];
        }

        if (transition is null)
            throw new InvalidOperationException($"No transition matched outcome '{outcomeKey ?? "<default>"}' from node '{sourceNode.Name}'.");

        var target = definition.Nodes.Single(x => x.Id == transition.TargetNodeId);
        await EnterNodeAsync(instance, definition, target, nextAssigneeUserId, actorUserId, cancellationToken);
    }

    private async Task EnterNodeAsync(
        WorkflowInstance instance,
        WorkflowDefinition definition,
        WorkflowNode node,
        Guid? explicitAssigneeUserId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        instance.CurrentNodeId = node.Id;
        instance.UpdatedAtUtc = DateTime.UtcNow;

        if (node.Type == WorkflowNodeType.End)
        {
            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAtUtc = DateTime.UtcNow;
            instance.Complaint.Status = ComplaintStatus.Resolved;
            instance.Complaint.ResolvedAtUtc = DateTime.UtcNow;
            instance.Complaint.UpdatedAtUtc = DateTime.UtcNow;

            db.ComplaintEvents.Add(new ComplaintEvent
            {
                ComplaintId = instance.ComplaintId,
                ActorUserId = actorUserId,
                EventType = "ComplaintResolved",
                Message = $"Complaint reached end step '{node.Name}'."
            });
            return;
        }

        if (node.Type == WorkflowNodeType.Start)
        {
            await MoveFromNodeAsync(instance, definition, node, null, explicitAssigneeUserId, actorUserId, cancellationToken);
            return;
        }

        if (node.Type != WorkflowNodeType.HumanTask)
            throw new NotSupportedException($"Node type {node.Type} is not implemented yet.");

        if (RequiresSpecificAssignee(node) && !explicitAssigneeUserId.HasValue)
            throw new InvalidOperationException($"Step '{node.Name}' requires a specific user assignment.");

        if (explicitAssigneeUserId.HasValue && !string.IsNullOrWhiteSpace(node.RoleCode))
        {
            var hasRole = await db.DepartmentMemberships.AnyAsync(x =>
                x.UserId == explicitAssigneeUserId.Value &&
                x.DepartmentId == definition.DepartmentId &&
                x.RoleCode == node.RoleCode, cancellationToken);
            if (!hasRole)
                throw new InvalidOperationException($"Selected user does not have role '{node.RoleCode}' in this department.");
        }

        var now = DateTime.UtcNow;
        var task = new WorkflowTask
        {
            WorkflowInstanceId = instance.Id,
            WorkflowNodeId = node.Id,
            ComplaintId = instance.ComplaintId,
            DepartmentId = definition.DepartmentId,
            AssignedRoleCode = node.RoleCode,
            AssignedToUserId = explicitAssigneeUserId,
            Status = WorkflowTaskStatus.Open,
            OpenedAtUtc = now,
            DueAtUtc = node.SlaHours.HasValue ? now.AddHours(node.SlaHours.Value) : null
        };
        db.WorkflowTasks.Add(task);

        db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = instance.ComplaintId,
            ActorUserId = actorUserId,
            EventType = "WorkflowTaskOpened",
            Message = explicitAssigneeUserId.HasValue
                ? $"Step '{node.Name}' assigned to a specific user."
                : $"Step '{node.Name}' entered the '{node.RoleCode}' queue."
        });
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
