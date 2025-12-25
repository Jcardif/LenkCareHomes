using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a bed within an adult family home.
///     Beds have static labels that persist across client admissions/discharges.
/// </summary>
public sealed class Bed
{
    /// <summary>
    ///     Gets or sets the unique identifier for the bed.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the home this bed belongs to.
    /// </summary>
    public Guid HomeId { get; set; }

    /// <summary>
    ///     Gets or sets the static label for the bed (e.g., "Room 1 - Bed A").
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    ///     Gets or sets the occupancy status of the bed.
    /// </summary>
    public BedStatus Status { get; set; } = BedStatus.Available;

    /// <summary>
    ///     Gets or sets whether the bed is currently active/in service.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Gets or sets when the bed record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when the bed record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Navigation property for the parent home.
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    ///     Navigation property for the current occupant (if any).
    /// </summary>
    public Client? CurrentOccupant { get; set; }
}