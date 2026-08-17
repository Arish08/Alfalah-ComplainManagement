using BankingPlatform.Application.Abstractions;
using BankingPlatform.Application.DTOs;
using BankingPlatform.Domain.Entities;
using BankingPlatform.Domain.Enums;
using BankingPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankingPlatform.Infrastructure.Services;

public sealed class ComplaintService(AppDbContext db, ICurrentUser currentUser, IWorkflowRuntime workflowRuntime) : IComplaintService
{
   public async Task<ComplaintDetailsDto> CreateAsync(
    CreateComplaintRequest request,
    CancellationToken cancellationToken)
{
    var category = await db.ComplaintCategories
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.Id == request.CategoryId && x.IsActive,
            cancellationToken)
        ?? throw new ArgumentException(
            "Complaint category not found or inactive.");

    var userExists = await db.Users.AnyAsync(
        x => x.Id == currentUser.UserId && x.IsActive,
        cancellationToken);

    if (!userExists)
        throw new UnauthorizedAccessException(
            "Current user is not registered.");

    var complaint = new Complaint
    {
        ComplaintNumber =
            $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",

        CategoryId = request.CategoryId,

        Subject = request.Subject.Trim(),

        Description = request.Description.Trim(),

        CustomerReference =
            request.CustomerReference?.Trim(),

        Priority = request.Priority,

        Status = ComplaintStatus.Open,

        CreatedByUserId = currentUser.UserId
    };

    db.Complaints.Add(complaint);

    db.ComplaintEvents.Add(new ComplaintEvent
    {
        ComplaintId = complaint.Id,

        ActorUserId = currentUser.UserId,

        EventType = "ComplaintCreated",

        Message =
            $"Complaint logged under category '{category.Name}'."
    });

    // PHASE 1
    // Persist Complaint first.
    await db.SaveChangesAsync(cancellationToken);

    // PHASE 2
    // Create WorkflowInstance after Complaint exists.
    await workflowRuntime.StartAsync(
        complaint,
        cancellationToken);

    return (await GetAsync(
        complaint.Id,
        cancellationToken))!;
}
    public async Task<ComplaintDetailsDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var complaint = await db.Complaints.AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (complaint is null) return null;

        var events = await db.ComplaintEvents.AsNoTracking()
            .Where(x => x.ComplaintId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new ComplaintEventDto(
                x.CreatedAtUtc,
                x.EventType,
                x.Message,
                x.ActorUser == null ? null : x.ActorUser.DisplayName))
            .ToListAsync(cancellationToken);

        return new ComplaintDetailsDto(
            complaint.Id,
            complaint.ComplaintNumber,
            complaint.Category.Name,
            complaint.Subject,
            complaint.Description,
            complaint.CustomerReference,
            complaint.Status,
            complaint.Priority,
            complaint.CreatedAtUtc,
            complaint.ResolvedAtUtc,
            events);
    }

    public async Task<IReadOnlyList<ComplaintListItemDto>> ListAsync(Guid? departmentId, Guid? categoryId, string? status, CancellationToken cancellationToken)
    {
        var query = db.Complaints.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (departmentId.HasValue) query = query.Where(x => x.Category.DepartmentId == departmentId.Value);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (Enum.TryParse<ComplaintStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);

        return await query.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ComplaintListItemDto(x.Id, x.ComplaintNumber, x.Category.Name, x.Subject, x.Status, x.Priority, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
