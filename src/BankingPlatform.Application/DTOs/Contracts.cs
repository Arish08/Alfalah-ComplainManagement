using BankingPlatform.Domain.Enums;

namespace BankingPlatform.Application.DTOs;

public sealed record CreateComplaintRequest(
    Guid CategoryId,
    string Subject,
    string Description,
    string? CustomerReference,
    ComplaintPriority Priority = ComplaintPriority.Normal);

public sealed record ComplaintListItemDto(
    Guid Id,
    string ComplaintNumber,
    string Category,
    string Subject,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    DateTime CreatedAtUtc);

public sealed record ComplaintEventDto(
    DateTime CreatedAtUtc,
    string EventType,
    string Message,
    string? ActorName);

public sealed record ComplaintDetailsDto(
    Guid Id,
    string ComplaintNumber,
    string Category,
    string Subject,
    string Description,
    string? CustomerReference,
    ComplaintStatus Status,
    ComplaintPriority Priority,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<ComplaintEventDto> Timeline);

public sealed record WorkflowNodeRequest(
    string Key,
    string Name,
    WorkflowNodeType Type,
    string? RoleCode,
    int? SlaHours,
    string? EscalationRoleCode,
    decimal X,
    decimal Y,
    string? ConfigJson = null);

public sealed record WorkflowEdgeRequest(
    string SourceKey,
    string TargetKey,
    string? OutcomeKey = null,
    string? Label = null);

public sealed record CreateWorkflowDefinitionRequest(
    string Name,
    Guid DepartmentId,
    Guid CategoryId,
    IReadOnlyList<WorkflowNodeRequest> Nodes,
    IReadOnlyList<WorkflowEdgeRequest> Edges);

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    Guid DepartmentId,
    Guid CategoryId,
    int Version,
    WorkflowDefinitionStatus Status,
    string DesignerJson,
    DateTime? PublishedAtUtc);

public sealed record WorkflowTaskDto(
    Guid Id,
    Guid ComplaintId,
    Guid DepartmentId,
    string ComplaintNumber,
    string ComplaintSubject,
    string NodeName,
    string? AssignedRoleCode,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    DateTime OpenedAtUtc,
    DateTime? DueAtUtc,
    DateTime? EscalatedAtUtc);

public sealed record WorkflowTaskActionDto(
    string? OutcomeKey,
    string Label,
    Guid TargetNodeId,
    string TargetNodeName,
    WorkflowNodeType TargetNodeType,
    string? TargetRoleCode,
    bool RequiresAssignee);

public sealed record CompleteWorkflowTaskRequest(
    string? OutcomeKey,
    Guid? NextAssigneeUserId,
    string? Comment);

public sealed record ReassignWorkflowTaskRequest(Guid UserId, string? Comment);

public sealed record ReferenceItemDto(Guid Id, string Code, string Name);
public sealed record UserReferenceDto(Guid Id, string EmployeeCode, string DisplayName, string Email, string RoleCode);
public sealed record CurrentUserMembershipDto(Guid DepartmentId, string DepartmentCode, string DepartmentName, string RoleCode);
public sealed record CurrentUserDto(Guid Id, string EmployeeCode, string DisplayName, string Email, IReadOnlyList<CurrentUserMembershipDto> Memberships);
