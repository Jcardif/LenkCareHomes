namespace LenkCareHomes.Api.Services.SyntheticData;

/// <summary>
///     Service for loading synthetic data into the database.
///     Only available in development environment.
/// </summary>
public interface ISyntheticDataService
{
    /// <summary>
    ///     Checks if synthetic data can be loaded (dev environment only).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    ///     Gets summary statistics about existing data in the database.
    /// </summary>
    Task<DataStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads synthetic data from JSON files in the datagen output folder.
    /// </summary>
    /// <param name="userId">The ID of the user loading the data.</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the load operation.</returns>
    Task<LoadSyntheticDataResult> LoadDataAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads synthetic data with progress reporting via callback.
    /// </summary>
    /// <param name="userId">The ID of the user loading the data.</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="progressCallback">Callback invoked for each progress update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the load operation.</returns>
    Task<LoadSyntheticDataResult> LoadDataWithProgressAsync(
        Guid userId,
        string? ipAddress,
        Func<LoadProgressUpdate, Task> progressCallback,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all non-system data from the database.
    ///     Use with extreme caution - this is destructive.
    /// </summary>
    /// <param name="userId">The ID of the user clearing the data.</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClearDataResult> ClearDataAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all non-system data with progress reporting via callback.
    /// </summary>
    /// <param name="userId">The ID of the user clearing the data.</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="progressCallback">Callback invoked for each progress update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ClearDataResult> ClearDataWithProgressAsync(
        Guid userId,
        string? ipAddress,
        Func<LoadProgressUpdate, Task> progressCallback,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Statistics about data in the database.
/// </summary>
public sealed record DataStatistics
{
    public int HomeCount { get; init; }
    public int BedCount { get; init; }
    public int UserCount { get; init; }
    public int ClientCount { get; init; }
    public int ActiveClientCount { get; init; }
    public int AdlLogCount { get; init; }
    public int VitalsLogCount { get; init; }
    public int MedicationLogCount { get; init; }
    public int RomLogCount { get; init; }
    public int BehaviorNoteCount { get; init; }
    public int ActivityCount { get; init; }
    public int IncidentCount { get; init; }
    public int DocumentCount { get; init; }
    public int AppointmentCount { get; init; }
}

/// <summary>
///     Result of loading synthetic data.
/// </summary>
public sealed record LoadSyntheticDataResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int HomesLoaded { get; init; }
    public int BedsLoaded { get; init; }
    public int UsersLoaded { get; init; }
    public int ClientsLoaded { get; init; }
    public int CareLogsLoaded { get; init; }
    public int ActivitiesLoaded { get; init; }
    public int IncidentsLoaded { get; init; }
    public int DocumentsLoaded { get; init; }
    public int AppointmentsLoaded { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
///     Result of clearing data.
/// </summary>
public sealed record ClearDataResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int RecordsDeleted { get; init; }
    public string? Details { get; init; }
}

/// <summary>
///     Progress update during synthetic data loading.
/// </summary>
public sealed record LoadProgressUpdate
{
    /// <summary>
    ///     The current phase being executed.
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    ///     Description of the current phase.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     Current step number (1-based).
    /// </summary>
    public int CurrentStep { get; init; }

    /// <summary>
    ///     Total number of steps.
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    ///     Progress percentage (0-100).
    /// </summary>
    public int PercentComplete => TotalSteps > 0 ? (int)(CurrentStep / (double)TotalSteps * 100) : 0;

    /// <summary>
    ///     Number of items processed in this phase.
    /// </summary>
    public int ItemsProcessed { get; init; }

    /// <summary>
    ///     Whether this is the final update.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    ///     Whether an error occurred.
    /// </summary>
    public bool IsError { get; init; }

    /// <summary>
    ///     Error message if IsError is true.
    /// </summary>
    public string? ErrorMessage { get; init; }
}