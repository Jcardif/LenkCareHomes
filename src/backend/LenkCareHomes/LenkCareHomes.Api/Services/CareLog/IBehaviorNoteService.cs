using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service interface for behavior note operations.
/// </summary>
public interface IBehaviorNoteService
{
    /// <summary>
    ///     Creates a new behavior note.
    /// </summary>
    Task<BehaviorNoteOperationResponse> CreateBehaviorNoteAsync(
        Guid clientId,
        CreateBehaviorNoteRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets behavior notes for a client.
    /// </summary>
    Task<IReadOnlyList<BehaviorNoteDto>> GetBehaviorNotesAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific behavior note by ID.
    /// </summary>
    Task<BehaviorNoteDto?> GetBehaviorNoteByIdAsync(
        Guid clientId,
        Guid noteId,
        CancellationToken cancellationToken = default);
}