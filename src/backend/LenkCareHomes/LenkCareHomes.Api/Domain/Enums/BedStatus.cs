namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Represents the occupancy status of a bed.
/// </summary>
public enum BedStatus
{
    /// <summary>
    ///     Bed is available for assignment.
    /// </summary>
    Available = 0,

    /// <summary>
    ///     Bed is currently occupied by a client.
    /// </summary>
    Occupied = 1
}