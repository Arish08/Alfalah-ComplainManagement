using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/complaints")]
public sealed class ComplaintsController(IComplaintService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ComplaintDetailsDto>> Create(CreateComplaintRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplaintDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComplaintListItemDto>>> List(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => Ok(await service.ListAsync(departmentId, categoryId, status, cancellationToken));
}
