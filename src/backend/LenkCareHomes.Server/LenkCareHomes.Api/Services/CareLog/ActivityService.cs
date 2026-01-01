using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service implementation for activity operations.
/// </summary>
public sealed class ActivityService : IActivityService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<ActivityService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ActivityOperationResponse> CreateActivityAsync(
        CreateActivityRequest request,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ActivityName))
            return new ActivityOperationResponse
            {
                Success = false,
                Error = "Activity name is required."
            };

        if (request.ClientIds.Count == 0)
            return new ActivityOperationResponse
            {
                Success = false,
                Error = "At least one participant is required."
            };

        // Verify all clients exist
        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => request.ClientIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (clients.Count != request.ClientIds.Count)
            return new ActivityOperationResponse
            {
                Success = false,
                Error = "One or more clients not found."
            };

        // For group activities, get the home from clients or request
        var homeId = request.HomeId;
        if (request.IsGroupActivity && !homeId.HasValue)
            // Use the home of the first client
            homeId = clients.FirstOrDefault()?.HomeId;

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            ActivityName = request.ActivityName,
            Description = request.Description,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Duration = request.Duration,
            Category = request.Category,
            IsGroupActivity = request.IsGroupActivity || request.ClientIds.Count > 1,
            HomeId = homeId,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Activities.Add(activity);

        // Add participants
        foreach (var clientId in request.ClientIds)
        {
            var participant = new ActivityParticipant
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ActivityParticipants.Add(participant);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(userId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ActivityCreated,
            userId,
            userName,
            "Activity",
            activity.Id.ToString(),
            "Success",
            ipAddress,
            $"Created activity '{request.ActivityName}' with {request.ClientIds.Count} participant(s)",
            cancellationToken);

        _logger.LogInformation(
            "Activity {ActivityId} created by user {UserId}",
            activity.Id, userId);

        // Reload with navigation properties
        return await GetActivityResponseAsync(activity.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityOperationResponse> UpdateActivityAsync(
        Guid activityId,
        UpdateActivityRequest request,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var activity = await _dbContext.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
            return new ActivityOperationResponse
            {
                Success = false,
                Error = "Activity not found."
            };

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(request.ActivityName)) activity.ActivityName = request.ActivityName;

        if (request.Description is not null) activity.Description = request.Description;

        if (request.Date.HasValue) activity.Date = request.Date.Value;

        if (request.StartTime.HasValue) activity.StartTime = request.StartTime.Value;

        if (request.EndTime.HasValue) activity.EndTime = request.EndTime.Value;

        if (request.Duration.HasValue) activity.Duration = request.Duration.Value;

        if (request.Category.HasValue) activity.Category = request.Category.Value;

        if (request.IsGroupActivity.HasValue) activity.IsGroupActivity = request.IsGroupActivity.Value;

        // Update participants if provided
        if (request.ClientIds is not null)
        {
            if (request.ClientIds.Count == 0)
                return new ActivityOperationResponse
                {
                    Success = false,
                    Error = "At least one participant is required."
                };

            // Verify all new clients exist
            var clients = await _dbContext.Clients
                .AsNoTracking()
                .Where(c => request.ClientIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            if (clients.Count != request.ClientIds.Count)
                return new ActivityOperationResponse
                {
                    Success = false,
                    Error = "One or more clients not found."
                };

            // Remove existing participants
            _dbContext.ActivityParticipants.RemoveRange(activity.Participants);

            // Add new participants
            foreach (var clientId in request.ClientIds)
            {
                var participant = new ActivityParticipant
                {
                    Id = Guid.NewGuid(),
                    ActivityId = activity.Id,
                    ClientId = clientId,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.ActivityParticipants.Add(participant);
            }

            activity.IsGroupActivity = request.ClientIds.Count > 1;
        }

        activity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(userId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ActivityUpdated,
            userId,
            userName,
            "Activity",
            activity.Id.ToString(),
            "Success",
            ipAddress,
            $"Updated activity '{activity.ActivityName}'",
            cancellationToken);

        _logger.LogInformation(
            "Activity {ActivityId} updated by user {UserId}",
            activity.Id, userId);

        return await GetActivityResponseAsync(activity.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityOperationResponse> DeleteActivityAsync(
        Guid activityId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var activity = await _dbContext.Activities
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
            return new ActivityOperationResponse
            {
                Success = false,
                Error = "Activity not found."
            };

        var activityName = activity.ActivityName;

        _dbContext.ActivityParticipants.RemoveRange(activity.Participants);
        _dbContext.Activities.Remove(activity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userName = await GetUserNameAsync(userId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ActivityDeleted,
            userId,
            userName,
            "Activity",
            activityId.ToString(),
            "Success",
            ipAddress,
            $"Deleted activity '{activityName}'",
            cancellationToken);

        _logger.LogInformation(
            "Activity {ActivityId} deleted by user {UserId}",
            activityId, userId);

        return new ActivityOperationResponse { Success = true };
    }

    /// <inheritdoc />
    public async Task<ActivityDto?> GetActivityByIdAsync(
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        var activity = await _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.Home)
            .Include(a => a.CreatedBy)
            .Include(a => a.Participants)
            .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        return activity is null ? null : MapToDto(activity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesByClientAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.Home)
            .Include(a => a.CreatedBy)
            .Include(a => a.Participants)
            .ThenInclude(p => p.Client)
            .Where(a => a.Participants.Any(p => p.ClientId == clientId));

        if (fromDate.HasValue) query = query.Where(a => a.Date >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(a => a.Date <= toDate.Value);

        var activities = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(cancellationToken);

        return activities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesByHomeAsync(
        Guid homeId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.Home)
            .Include(a => a.CreatedBy)
            .Include(a => a.Participants)
            .ThenInclude(p => p.Client)
            .Where(a => a.HomeId == homeId);

        if (fromDate.HasValue) query = query.Where(a => a.Date >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(a => a.Date <= toDate.Value);

        var activities = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(cancellationToken);

        return activities.Select(MapToDto).ToList();
    }

    private async Task<ActivityOperationResponse> GetActivityResponseAsync(
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var dto = await GetActivityByIdAsync(activityId, cancellationToken);

        return new ActivityOperationResponse
        {
            Success = dto is not null,
            Activity = dto,
            Error = dto is null ? "Activity not found after operation." : null
        };
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static ActivityDto MapToDto(Activity activity)
    {
        return new ActivityDto
        {
            Id = activity.Id,
            ActivityName = activity.ActivityName,
            Description = activity.Description,
            Date = activity.Date,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Duration = activity.Duration,
            Category = activity.Category,
            IsGroupActivity = activity.IsGroupActivity,
            HomeId = activity.HomeId,
            HomeName = activity.Home?.Name,
            CreatedById = activity.CreatedById,
            CreatedByName = activity.CreatedBy is not null
                ? $"{activity.CreatedBy.FirstName} {activity.CreatedBy.LastName}"
                : "Unknown",
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt,
            Participants = activity.Participants
                .Where(p => p.Client is not null)
                .Select(p => new ActivityParticipantDto
                {
                    ClientId = p.ClientId,
                    ClientName = $"{p.Client!.FirstName} {p.Client.LastName}"
                })
                .ToList()
        };
    }
}