namespace LenkCareHomes.Api.Models.Caregivers;

/// <summary>
///     DTO for caregiver information with home assignments.
/// </summary>
public sealed record CaregiverDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required bool IsActive { get; init; }
    public required bool IsMfaSetupComplete { get; init; }
    public required bool InvitationAccepted { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required IReadOnlyList<CaregiverHomeAssignmentDto> HomeAssignments { get; init; }
}

/// <summary>
///     DTO for caregiver home assignment.
/// </summary>
public sealed record CaregiverHomeAssignmentDto
{
    public required Guid Id { get; init; }
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public required DateTime AssignedAt { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
///     Request model for assigning homes to a caregiver.
/// </summary>
public sealed record AssignHomesRequest
{
    /// <summary>
    ///     Gets the list of home IDs to assign.
    /// </summary>
    public required IReadOnlyList<Guid> HomeIds { get; init; }
}

/// <summary>
///     Response model for caregiver operations.
/// </summary>
public sealed record CaregiverOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public CaregiverDto? Caregiver { get; init; }
}

/// <summary>
///     Summary DTO for caregiver listings.
/// </summary>
public sealed record CaregiverSummaryDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required bool IsActive { get; init; }
    public required bool InvitationAccepted { get; init; }
    public required int AssignedHomesCount { get; init; }
}