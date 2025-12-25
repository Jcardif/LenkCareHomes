namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a client (resident) in an adult family home.
/// Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class Client
{
    /// <summary>
    /// Gets or sets the unique identifier for the client.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the client's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the client's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the client's date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the client's gender.
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Gets or sets the encrypted SSN (last 4 digits only for display).
    /// Stored encrypted at rest.
    /// </summary>
    public string? SsnEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the admission date.
    /// </summary>
    public DateTime AdmissionDate { get; set; }

    /// <summary>
    /// Gets or sets the discharge date (null if currently admitted).
    /// </summary>
    public DateTime? DischargeDate { get; set; }

    /// <summary>
    /// Gets or sets the discharge reason.
    /// </summary>
    public string? DischargeReason { get; set; }

    /// <summary>
    /// Gets or sets the home ID.
    /// </summary>
    public Guid HomeId { get; set; }

    /// <summary>
    /// Gets or sets the bed ID.
    /// </summary>
    public Guid? BedId { get; set; }

    /// <summary>
    /// Gets or sets the primary physician name.
    /// </summary>
    public string? PrimaryPhysician { get; set; }

    /// <summary>
    /// Gets or sets the primary physician phone.
    /// </summary>
    public string? PrimaryPhysicianPhone { get; set; }

    /// <summary>
    /// Gets or sets the emergency contact name.
    /// </summary>
    public string? EmergencyContactName { get; set; }

    /// <summary>
    /// Gets or sets the emergency contact phone.
    /// </summary>
    public string? EmergencyContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the emergency contact relationship.
    /// </summary>
    public string? EmergencyContactRelationship { get; set; }

    /// <summary>
    /// Gets or sets the known allergies (JSON array or comma-separated).
    /// </summary>
    public string? Allergies { get; set; }

    /// <summary>
    /// Gets or sets the diagnoses (JSON array or comma-separated).
    /// </summary>
    public string? Diagnoses { get; set; }

    /// <summary>
    /// Gets or sets the current medication list (JSON array or text).
    /// </summary>
    public string? MedicationList { get; set; }

    /// <summary>
    /// Gets or sets the URL/path to the client's photo.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the client.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the client is currently active (admitted).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the client record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the client record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created this record.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets the client's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Navigation property for the home.
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    /// Navigation property for the bed.
    /// </summary>
    public Bed? Bed { get; set; }
}
