using BankingPlatform.Application.Abstractions;

namespace BankingPlatform.Api.Security;

public sealed class DevHeaderCurrentUser(IHttpContextAccessor accessor, IWebHostEnvironment environment) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            if (!environment.IsDevelopment())
                throw new UnauthorizedAccessException("Dev header authentication is disabled outside Development.");

            var raw = accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (!Guid.TryParse(raw, out var userId))
                throw new UnauthorizedAccessException("Provide a valid X-User-Id header in local development.");
            return userId;
        }
    }
}
