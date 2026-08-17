using System.Text.Json;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Persistence;

public static class DbSeeder
{
    public static readonly Guid ComplaintsDepartmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AtmCategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid AlfaMallCategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid TransactionCategoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid UnitHeadId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TeamLeadId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Officer1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Officer2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Departments.AnyAsync(cancellationToken))
        {
            db.Departments.Add(new Department { Id = ComplaintsDepartmentId, Code = "CMU", Name = "Complaint Management Unit" });
            db.ComplaintCategories.AddRange(
                new ComplaintCategory { Id = AtmCategoryId, DepartmentId = ComplaintsDepartmentId, Code = "ATM", Name = "ATM Issue" },
                new ComplaintCategory { Id = AlfaMallCategoryId, DepartmentId = ComplaintsDepartmentId, Code = "ALFA_MALL", Name = "Alfa Mall" },
                new ComplaintCategory { Id = TransactionCategoryId, DepartmentId = ComplaintsDepartmentId, Code = "TRANSACTION", Name = "Transaction Issue" });
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.AddRange(
                new AppUser { Id = UnitHeadId, EmployeeCode = "UH001", DisplayName = "Unit Head", Email = "unithead@bank.local" },
                new AppUser { Id = TeamLeadId, EmployeeCode = "TL001", DisplayName = "Team Lead", Email = "teamlead@bank.local" },
                new AppUser { Id = Officer1Id, EmployeeCode = "OF001", DisplayName = "Officer One", Email = "officer1@bank.local" },
                new AppUser { Id = Officer2Id, EmployeeCode = "OF002", DisplayName = "Officer Two", Email = "officer2@bank.local" });
            db.DepartmentMemberships.AddRange(
                new DepartmentMembership { UserId = UnitHeadId, DepartmentId = ComplaintsDepartmentId, RoleCode = "UnitHead" },
                new DepartmentMembership { UserId = UnitHeadId, DepartmentId = ComplaintsDepartmentId, RoleCode = "DeptAdmin" },
                new DepartmentMembership { UserId = TeamLeadId, DepartmentId = ComplaintsDepartmentId, RoleCode = "TeamLead" },
                new DepartmentMembership { UserId = Officer1Id, DepartmentId = ComplaintsDepartmentId, RoleCode = "Officer" },
                new DepartmentMembership { UserId = Officer2Id, DepartmentId = ComplaintsDepartmentId, RoleCode = "Officer" });
        }

        await db.SaveChangesAsync(cancellationToken);

        await SeedWorkflowAsync(db, new CreateWorkflowDefinitionRequest(
            "ATM Complaint Workflow",
            ComplaintsDepartmentId,
            AtmCategoryId,
            new[]
            {
                new WorkflowNodeRequest("start", "Start", WorkflowNodeType.Start, null, null, null, 80, 180),
                new WorkflowNodeRequest("unitHead", "Unit Head Review", WorkflowNodeType.HumanTask, "UnitHead", 4, "UnitHead", 300, 180),
                new WorkflowNodeRequest("teamLead", "Team Lead Assignment", WorkflowNodeType.HumanTask, "TeamLead", 8, "UnitHead", 530, 180),
                new WorkflowNodeRequest("officer", "Officer Resolution", WorkflowNodeType.HumanTask, "Officer", 48, "TeamLead", 760, 180, "{\"assignmentMode\":\"specific\"}"),
                new WorkflowNodeRequest("end", "Resolved", WorkflowNodeType.End, null, null, null, 1000, 180)
            },
            new[]
            {
                new WorkflowEdgeRequest("start", "unitHead"),
                new WorkflowEdgeRequest("unitHead", "teamLead", "forward", "Forward to team lead"),
                new WorkflowEdgeRequest("teamLead", "officer", "assign", "Assign officer"),
                new WorkflowEdgeRequest("officer", "end", "resolve", "Resolve complaint")
            }), cancellationToken);

        await SeedWorkflowAsync(db, new CreateWorkflowDefinitionRequest(
            "Alfa Mall Complaint Workflow",
            ComplaintsDepartmentId,
            AlfaMallCategoryId,
            new[]
            {
                new WorkflowNodeRequest("start", "Start", WorkflowNodeType.Start, null, null, null, 80, 180),
                new WorkflowNodeRequest("teamLead", "Team Lead Review", WorkflowNodeType.HumanTask, "TeamLead", 12, "UnitHead", 360, 180),
                new WorkflowNodeRequest("officer", "Officer Resolution", WorkflowNodeType.HumanTask, "Officer", 48, "TeamLead", 650, 180, "{\"assignmentMode\":\"specific\"}"),
                new WorkflowNodeRequest("end", "Resolved", WorkflowNodeType.End, null, null, null, 940, 180)
            },
            new[]
            {
                new WorkflowEdgeRequest("start", "teamLead"),
                new WorkflowEdgeRequest("teamLead", "officer", "assign", "Assign officer"),
                new WorkflowEdgeRequest("officer", "end", "resolve", "Resolve complaint")
            }), cancellationToken);

        await SeedWorkflowAsync(db, new CreateWorkflowDefinitionRequest(
            "Transaction Complaint Workflow",
            ComplaintsDepartmentId,
            TransactionCategoryId,
            new[]
            {
                new WorkflowNodeRequest("start", "Start", WorkflowNodeType.Start, null, null, null, 80, 180),
                new WorkflowNodeRequest("unitHead", "Unit Head Review", WorkflowNodeType.HumanTask, "UnitHead", 2, "UnitHead", 300, 180),
                new WorkflowNodeRequest("teamLead", "Team Lead Assignment", WorkflowNodeType.HumanTask, "TeamLead", 8, "UnitHead", 530, 180),
                new WorkflowNodeRequest("officer", "Officer Resolution", WorkflowNodeType.HumanTask, "Officer", 24, "TeamLead", 760, 180, "{\"assignmentMode\":\"specific\"}"),
                new WorkflowNodeRequest("end", "Resolved", WorkflowNodeType.End, null, null, null, 1000, 180)
            },
            new[]
            {
                new WorkflowEdgeRequest("start", "unitHead"),
                new WorkflowEdgeRequest("unitHead", "teamLead", "forward", "Forward to team lead"),
                new WorkflowEdgeRequest("teamLead", "officer", "assign", "Assign officer"),
                new WorkflowEdgeRequest("officer", "end", "resolve", "Resolve complaint")
            }), cancellationToken);
    }

    private static async Task SeedWorkflowAsync(AppDbContext db, CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken)
    {
        if (await db.WorkflowDefinitions.AnyAsync(x => x.CategoryId == request.CategoryId, cancellationToken)) return;

        var definition = new WorkflowDefinition
        {
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            CategoryId = request.CategoryId,
            Version = 1,
            Status = WorkflowDefinitionStatus.Published,
            CreatedByUserId = UnitHeadId,
            PublishedAtUtc = DateTime.UtcNow,
            DesignerJson = JsonSerializer.Serialize(request)
        };

        var nodes = request.Nodes.ToDictionary(x => x.Key, x => new WorkflowNode
        {
            WorkflowDefinitionId = definition.Id,
            NodeKey = x.Key,
            Name = x.Name,
            Type = x.Type,
            RoleCode = x.RoleCode,
            SlaHours = x.SlaHours,
            EscalationRoleCode = x.EscalationRoleCode,
            PositionX = x.X,
            PositionY = x.Y,
            ConfigJson = x.ConfigJson
        });

        foreach (var node in nodes.Values) definition.Nodes.Add(node);
        foreach (var edge in request.Edges)
            definition.Transitions.Add(new WorkflowTransition
            {
                WorkflowDefinitionId = definition.Id,
                SourceNodeId = nodes[edge.SourceKey].Id,
                TargetNodeId = nodes[edge.TargetKey].Id,
                OutcomeKey = edge.OutcomeKey,
                Label = edge.Label
            });

        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
    }
}
