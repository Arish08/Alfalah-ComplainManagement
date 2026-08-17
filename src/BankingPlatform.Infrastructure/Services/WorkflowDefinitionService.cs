using System.Text.Json;
using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class WorkflowDefinitionService(AppDbContext db, ICurrentUser currentUser) : IWorkflowDefinitionService
{
    public async Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureDepartmentAdminAsync(request.DepartmentId, cancellationToken);

        var category = await db.ComplaintCategories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId && x.DepartmentId == request.DepartmentId, cancellationToken)
            ?? throw new InvalidOperationException("Category does not belong to the selected department.");

        var nextVersion = (await db.WorkflowDefinitions
            .Where(x => x.CategoryId == request.CategoryId)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;

        var definition = new WorkflowDefinition
        {
            Name = request.Name.Trim(),
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            Version = nextVersion,
            Status = WorkflowDefinitionStatus.Draft,
            CreatedByUserId = currentUser.UserId,
            DesignerJson = JsonSerializer.Serialize(request)
        };

        BuildGraph(definition, request);
        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    public async Task<WorkflowDefinitionDto> UpdateDraftAsync(Guid id, CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(definition.DepartmentId, cancellationToken);
        if (request.DepartmentId != definition.DepartmentId || request.CategoryId != definition.CategoryId)
            throw new InvalidOperationException("A draft version cannot be moved to another department/category. Create a new workflow there instead.");

        if (definition.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException("Published workflows are immutable. Create a new version instead.");

        definition.Name = request.Name.Trim();
        definition.DepartmentId = request.DepartmentId;
        definition.CategoryId = request.CategoryId;
        definition.DesignerJson = JsonSerializer.Serialize(request);
        definition.UpdatedAtUtc = DateTime.UtcNow;

        db.WorkflowTransitions.RemoveRange(definition.Transitions);
        db.WorkflowNodes.RemoveRange(definition.Nodes);
        definition.Transitions.Clear();
        definition.Nodes.Clear();
        BuildGraph(definition, request);

        await db.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    public async Task<WorkflowDefinitionDto> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await db.WorkflowDefinitions
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(definition.DepartmentId, cancellationToken);
        if (definition.Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException("Only draft workflows can be published.");

        var oldPublished = await db.WorkflowDefinitions
            .Where(x => x.CategoryId == definition.CategoryId && x.Status == WorkflowDefinitionStatus.Published)
            .ToListAsync(cancellationToken);

        foreach (var old in oldPublished)
        {
            old.Status = WorkflowDefinitionStatus.Archived;
            old.UpdatedAtUtc = DateTime.UtcNow;
        }

        definition.Status = WorkflowDefinitionStatus.Published;
        definition.PublishedAtUtc = DateTime.UtcNow;
        definition.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Map(definition);
    }

    public async Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await db.WorkflowDefinitions.AsNoTracking()
            .Include(x => x.Nodes)
            .Include(x => x.Transitions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        await EnsureDepartmentAdminAsync(source.DepartmentId, cancellationToken);
        var request = JsonSerializer.Deserialize<CreateWorkflowDefinitionRequest>(source.DesignerJson)
            ?? throw new InvalidOperationException("Stored designer JSON is invalid.");
        return await CreateAsync(request with { Name = source.Name }, cancellationToken);
    }

    public async Task<WorkflowDefinitionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var x = await db.WorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return x is null ? null : Map(x);
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = db.WorkflowDefinitions.AsNoTracking().AsQueryable();
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        var items = await query.OrderBy(x => x.CategoryId).ThenByDescending(x => x.Version).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    private async Task EnsureDepartmentAdminAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var allowed = await db.DepartmentMemberships.AsNoTracking().AnyAsync(x =>
            x.UserId == currentUser.UserId && x.DepartmentId == departmentId && x.RoleCode == "DeptAdmin", cancellationToken);
        if (!allowed) throw new UnauthorizedAccessException("Only a department admin can create, edit or publish workflows for this department.");
    }

    private static WorkflowDefinitionDto Map(WorkflowDefinition x) =>
        new(x.Id, x.Name, x.DepartmentId, x.CategoryId, x.Version, x.Status, x.DesignerJson, x.PublishedAtUtc);

    private static void BuildGraph(WorkflowDefinition definition, CreateWorkflowDefinitionRequest request)
    {
        var byKey = new Dictionary<string, WorkflowNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in request.Nodes)
        {
            var node = new WorkflowNode
            {
                WorkflowDefinitionId = definition.Id,
                NodeKey = item.Key.Trim(),
                Name = item.Name.Trim(),
                Type = item.Type,
                RoleCode = item.RoleCode?.Trim(),
                SlaHours = item.SlaHours,
                EscalationRoleCode = item.EscalationRoleCode?.Trim(),
                PositionX = item.X,
                PositionY = item.Y,
                ConfigJson = item.ConfigJson
            };
            definition.Nodes.Add(node);
            byKey[node.NodeKey] = node;
        }

        foreach (var edge in request.Edges)
        {
            definition.Transitions.Add(new WorkflowTransition
            {
                WorkflowDefinitionId = definition.Id,
                SourceNodeId = byKey[edge.SourceKey].Id,
                TargetNodeId = byKey[edge.TargetKey].Id,
                OutcomeKey = string.IsNullOrWhiteSpace(edge.OutcomeKey) ? null : edge.OutcomeKey.Trim(),
                Label = edge.Label?.Trim()
            });
        }
    }

    private static void Validate(CreateWorkflowDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Workflow name is required.");
        if (request.Nodes.Count == 0) throw new ArgumentException("Workflow needs at least one node.");
        if (request.Nodes.Count(x => x.Type == WorkflowNodeType.Start) != 1) throw new ArgumentException("Workflow must contain exactly one Start node.");
        if (request.Nodes.All(x => x.Type != WorkflowNodeType.End)) throw new ArgumentException("Workflow needs at least one End node.");

        var keys = request.Nodes.Select(x => x.Key.Trim()).ToList();
        if (keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Every node needs a key.");
        if (keys.Count != keys.Distinct(StringComparer.OrdinalIgnoreCase).Count()) throw new ArgumentException("Node keys must be unique.");
        var keySet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in request.Edges)
        {
            if (!keySet.Contains(edge.SourceKey) || !keySet.Contains(edge.TargetKey))
                throw new ArgumentException($"Edge {edge.SourceKey} -> {edge.TargetKey} references a missing node.");
        }

        foreach (var node in request.Nodes.Where(x => x.Type == WorkflowNodeType.HumanTask))
        {
            if (string.IsNullOrWhiteSpace(node.RoleCode)) throw new ArgumentException($"Human task '{node.Name}' needs a roleCode.");
            if (node.SlaHours is <= 0) throw new ArgumentException($"SLA hours for '{node.Name}' must be positive.");
        }

        var start = request.Nodes.Single(x => x.Type == WorkflowNodeType.Start).Key;
        var adjacency = request.Edges.GroupBy(x => x.SourceKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(e => e.TargetKey).ToList(), StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var q = new Queue<string>();
        q.Enqueue(start);
        visited.Add(start);
        while (q.Count > 0)
        {
            var current = q.Dequeue();
            if (!adjacency.TryGetValue(current, out var next)) continue;
            foreach (var n in next)
                if (visited.Add(n)) q.Enqueue(n);
        }

        var unreachable = keys.Where(k => !visited.Contains(k)).ToList();
        if (unreachable.Count > 0) throw new ArgumentException("Unreachable nodes: " + string.Join(", ", unreachable));
    }
}
