namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Severity levels for behavior and mood notes.
/// </summary>
public enum NoteSeverity
{
    /// <summary>
    ///     Low severity - routine observation.
    /// </summary>
    Low,

    /// <summary>
    ///     Medium severity - noteworthy but not urgent.
    /// </summary>
    Medium,

    /// <summary>
    ///     High severity - requires attention.
    /// </summary>
    High
}