namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents an adult family home in the LenkCare system.
/// </summary>
public sealed class Home
{
    /// <summary>
    ///     Gets or sets the unique identifier for the home.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the home.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Gets or sets the street address.
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    ///     Gets or sets the city.
    /// </summary>
    public required string City { get; set; }

    /// <summary>
    ///     Gets or sets the state.
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    ///     Gets or sets the ZIP code.
    /// </summary>
    public required string ZipCode { get; set; }

    /// <summary>
    ///     Gets or sets the contact phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of beds/capacity.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    ///     Gets or sets whether the home is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Gets or sets when the home was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when the home was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who created this home.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    ///     Navigation property for beds in this home.
    /// </summary>
    public ICollection<Bed> Beds { get; set; } = [];

    /// <summary>
    ///     Navigation property for caregiver assignments to this home.
    /// </summary>
    public ICollection<CaregiverHomeAssignment> CaregiverAssignments { get; set; } = [];

    /// <summary>
    ///     Navigation property for clients in this home.
    /// </summary>
    public ICollection<Client> Clients { get; set; } = [];
}