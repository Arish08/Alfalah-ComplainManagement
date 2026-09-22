using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/workflow-tasks")]
public sealed class WorkflowTasksController(IWorkflowTaskService service) : ControllerBase
{
    [HttpGet("my-bucket")]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskDto>>> MyBucket(CancellationToken cancellationToken)
        => Ok(await service.GetMyBucketAsync(cancellationToken));

    [HttpGet("{taskId:guid}/actions")]
    public async Task<ActionResult<IReadOnlyList<WorkflowTaskActionDto>>> Actions(Guid taskId, CancellationToken cancellationToken)
        => Ok(await service.GetActionsAsync(taskId, cancellationToken));

    [HttpPost("{taskId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid taskId, CompleteWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        await service.CompleteAsync(taskId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{taskId:guid}/reassign")]
    public async Task<IActionResult> Reassign(Guid taskId, ReassignWorkflowTaskRequest request, CancellationToken cancellationToken)
    {
        await service.ReassignAsync(taskId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{taskId:guid}/form")]
public async Task<ActionResult<WorkflowTaskFormDto>>
    GetForm(
        Guid taskId,
        CancellationToken cancellationToken)
{
    return Ok(
        await service.GetFormAsync(
            taskId,
            cancellationToken));
}
}
