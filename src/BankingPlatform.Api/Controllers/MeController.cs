using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserDto>> Get(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUser.UserId && x.IsActive, cancellationToken);
        if (user is null) return NotFound();

        var memberships = await db.DepartmentMemberships.AsNoTracking()
            .Where(x => x.UserId == currentUser.UserId)
            .OrderBy(x => x.Department.Name)
            .ThenBy(x => x.RoleCode)
            .Select(x => new CurrentUserMembershipDto(x.DepartmentId, x.Department.Code, x.Department.Name, x.RoleCode))
            .ToListAsync(cancellationToken);

        return Ok(new CurrentUserDto(user.Id, user.EmployeeCode, user.DisplayName, user.Email, memberships));
    }
}
