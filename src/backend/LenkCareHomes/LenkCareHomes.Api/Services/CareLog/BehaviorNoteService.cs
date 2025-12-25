using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service implementation for behavior note operations.
/// </summary>
public sealed class BehaviorNoteService : IBehaviorNoteService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditService;
    private readonly ILogger<BehaviorNoteService> _logger;

    private const int MaxNoteLength = 4000;

    public BehaviorNoteService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<BehaviorNoteService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BehaviorNoteOperationResponse> CreateBehaviorNoteAsync(
        Guid clientId,
        CreateBehaviorNoteRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.NoteText))
        {
            return new BehaviorNoteOperationResponse
            {
                Success = false,
                Error = "Note text is required."
            };
        }

        if (request.NoteText.Length > MaxNoteLength)
        {
            return new BehaviorNoteOperationResponse
            {
                Success = false,
                Error = $"Note text must not exceed {MaxNoteLength} characters."
            };
        }

        // Verify client exists
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
        {
            return new BehaviorNoteOperationResponse
            {
                Success = false,
                Error = "Client not found."
            };
        }

        var note = new BehaviorNote
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CaregiverId = caregiverId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Category = request.Category,
            NoteText = request.NoteText,
            Severity = request.Severity,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.BehaviorNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var caregiverName = await GetUserNameAsync(caregiverId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.BehaviorNoteCreated,
            caregiverId,
            caregiverName,
            "BehaviorNote",
            note.Id.ToString(),
            "Success",
            ipAddress,
            $"Created {request.Category} note for client '{client.FirstName} {client.LastName}'",
            cancellationToken);

        _logger.LogInformation(
            "Behavior note created for client {ClientId} by caregiver {CaregiverId}",
            clientId, caregiverId);

        return new BehaviorNoteOperationResponse
        {
            Success = true,
            BehaviorNote = MapToDto(note, caregiverName)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BehaviorNoteDto>> GetBehaviorNotesAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BehaviorNotes
            .AsNoTracking()
            .Include(b => b.Caregiver)
            .Where(b => b.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.Timestamp <= toDate.Value);
        }

        var notes = await query
            .OrderByDescending(b => b.Timestamp)
            .ToListAsync(cancellationToken);

        return notes.Select(n => MapToDto(n, GetCaregiverName(n.Caregiver))).ToList();
    }

    /// <inheritdoc />
    public async Task<BehaviorNoteDto?> GetBehaviorNoteByIdAsync(
        Guid clientId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.BehaviorNotes
            .AsNoTracking()
            .Include(b => b.Caregiver)
            .FirstOrDefaultAsync(b => b.Id == noteId && b.ClientId == clientId, cancellationToken);

        return note is null ? null : MapToDto(note, GetCaregiverName(note.Caregiver));
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static string GetCaregiverName(ApplicationUser? user)
    {
        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static BehaviorNoteDto MapToDto(BehaviorNote note, string caregiverName)
    {
        return new BehaviorNoteDto
        {
            Id = note.Id,
            ClientId = note.ClientId,
            CaregiverId = note.CaregiverId,
            CaregiverName = caregiverName,
            Timestamp = note.Timestamp,
            Category = note.Category,
            NoteText = note.NoteText,
            Severity = note.Severity,
            CreatedAt = note.CreatedAt
        };
    }
}
