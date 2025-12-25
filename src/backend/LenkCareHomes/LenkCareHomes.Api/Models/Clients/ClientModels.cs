namespace LenkCareHomes.Api.Models.Clients;

/// <summary>
/// DTO for client information.
/// </summary>
public sealed record ClientDto
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required DateTime DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required DateTime AdmissionDate { get; init; }
    public DateTime? DischargeDate { get; init; }
    public string? DischargeReason { get; init; }
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public Guid? BedId { get; init; }
    public string? BedLabel { get; init; }
    public string? PrimaryPhysician { get; init; }
    public string? PrimaryPhysicianPhone { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }
    public string? Allergies { get; init; }
    public string? Diagnoses { get; init; }
    public string? MedicationList { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Notes { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Summary DTO for client listings.
/// </summary>
public sealed record ClientSummaryDto
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required DateTime DateOfBirth { get; init; }
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public string? BedLabel { get; init; }
    public string? Allergies { get; init; }
    public string? PhotoUrl { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime AdmissionDate { get; init; }
}

/// <summary>
/// Request model for admitting a new client.
/// </summary>
public sealed record AdmitClientRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public string? Ssn { get; init; }
    public required DateTime AdmissionDate { get; init; }
    public required Guid HomeId { get; init; }
    public required Guid BedId { get; init; }
    public string? PrimaryPhysician { get; init; }
    public string? PrimaryPhysicianPhone { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }
    public string? Allergies { get; init; }
    public string? Diagnoses { get; init; }
    public string? MedicationList { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Request model for updating a client.
/// </summary>
public sealed record UpdateClientRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? PrimaryPhysician { get; init; }
    public string? PrimaryPhysicianPhone { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }
    public string? Allergies { get; init; }
    public string? Diagnoses { get; init; }
    public string? MedicationList { get; init; }
    public string? Notes { get; init; }
    public string? PhotoUrl { get; init; }
}

/// <summary>
/// Request model for discharging a client.
/// </summary>
public sealed record DischargeClientRequest
{
    public required DateTime DischargeDate { get; init; }
    public required string DischargeReason { get; init; }
}

/// <summary>
/// Request model for transferring a client to a different bed.
/// </summary>
public sealed record TransferClientRequest
{
    public required Guid NewBedId { get; init; }
}

/// <summary>
/// Response model for client operations.
/// </summary>
public sealed record ClientOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ClientDto? Client { get; init; }
}
