namespace LenkCareHomes.Api.Models.Beds;

/// <summary>
///     DTO for bed information.
/// </summary>
public sealed record BedDto
{
    public required Guid Id { get; init; }
    public required Guid HomeId { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? CurrentOccupantId { get; init; }
    public string? CurrentOccupantName { get; init; }
}

/// <summary>
///     Request model for creating a bed.
/// </summary>
public sealed record CreateBedRequest
{
    /// <summary>
    ///     Gets the static label for the bed.
    /// </summary>
    public required string Label { get; init; }
}

/// <summary>
///     Request model for updating a bed.
/// </summary>
public sealed record UpdateBedRequest
{
    public string? Label { get; init; }
    public bool? IsActive { get; init; }
}

/// <summary>
///     Response model for bed operations.
/// </summary>
public sealed record BedOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public BedDto? Bed { get; init; }
}