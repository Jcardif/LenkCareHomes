namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents the assignment of a caregiver to a specific home.
///     Caregivers can only access clients in homes they are assigned to.
/// </summary>
public sealed class CaregiverHomeAssignment
{
    /// <summary>
    ///     Gets or sets the unique identifier for the assignment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the caregiver user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Gets or sets the home ID.
    /// </summary>
    public Guid HomeId { get; set; }

    /// <summary>
    ///     Gets or sets when the assignment was created.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the ID of the admin who made the assignment.
    /// </summary>
    public Guid AssignedById { get; set; }

    /// <summary>
    ///     Gets or sets whether the assignment is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Navigation property for the caregiver user.
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    ///     Navigation property for the assigned home.
    /// </summary>
    public Home? Home { get; set; }
}