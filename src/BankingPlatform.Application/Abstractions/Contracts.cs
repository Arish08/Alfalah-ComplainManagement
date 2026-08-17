using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;

namespace BankingPlatform.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IComplaintService
{
    Task<ComplaintDetailsDto> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken);
    Task<ComplaintDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ComplaintListItemDto>> ListAsync(Guid? departmentId, Guid? categoryId, string? status, CancellationToken cancellationToken);
}

public interface IWorkflowDefinitionService
{
    Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> UpdateDraftAsync(Guid id, CreateWorkflowDefinitionRequest request, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> PublishAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto> CreateNewVersionAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowDefinitionDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowDefinitionDto>> ListAsync(Guid? categoryId, CancellationToken cancellationToken);
}

public interface IWorkflowTaskService
{
    Task<IReadOnlyList<WorkflowTaskDto>> GetMyBucketAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowTaskActionDto>> GetActionsAsync(Guid taskId, CancellationToken cancellationToken);
    Task CompleteAsync(Guid taskId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken);
    Task ReassignAsync(Guid taskId, ReassignWorkflowTaskRequest request, CancellationToken cancellationToken);
}

public interface IWorkflowRuntime
{
    Task StartAsync(Complaint complaint, CancellationToken cancellationToken);
    Task CompleteTaskAsync(Guid taskId, Guid actorUserId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken);
}
