using BankingPlatform.Application.DTOs;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/reference")]
public sealed class ReferenceController(AppDbContext db) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemDto>>> Departments(CancellationToken cancellationToken)
        => Ok(await db.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken));

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemDto>>> Categories([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        var q = db.ComplaintCategories.AsNoTracking().Where(x => x.IsActive);
        if (departmentId.HasValue) q = q.Where(x => x.DepartmentId == departmentId.Value);
        return Ok(await q.OrderBy(x => x.Name).Select(x => new ReferenceItemDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserReferenceDto>>> Users(
        [FromQuery] Guid departmentId,
        [FromQuery] string? roleCode,
        CancellationToken cancellationToken)
    {
        var q = db.DepartmentMemberships.AsNoTracking()
            .Where(x => x.DepartmentId == departmentId && x.User.IsActive);
        if (!string.IsNullOrWhiteSpace(roleCode)) q = q.Where(x => x.RoleCode == roleCode);
        return Ok(await q.OrderBy(x => x.User.DisplayName)
            .Select(x => new UserReferenceDto(x.UserId, x.User.EmployeeCode, x.User.DisplayName, x.User.Email, x.RoleCode))
            .ToListAsync(cancellationToken));
    }
}
