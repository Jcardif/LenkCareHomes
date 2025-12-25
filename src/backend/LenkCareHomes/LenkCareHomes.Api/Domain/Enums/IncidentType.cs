namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Types of incidents that can occur at an adult family home.
/// </summary>
public enum IncidentType
{
    /// <summary>
    ///     A client fall incident.
    /// </summary>
    Fall,

    /// <summary>
    ///     A medication administration error or issue.
    /// </summary>
    Medication,

    /// <summary>
    ///     A behavioral incident.
    /// </summary>
    Behavioral,

    /// <summary>
    ///     A medical emergency or health concern.
    /// </summary>
    Medical,

    /// <summary>
    ///     An injury incident.
    /// </summary>
    Injury,

    /// <summary>
    ///     A client wandering or elopement incident.
    /// </summary>
    Elopement,

    /// <summary>
    ///     An incident not covered by other categories.
    /// </summary>
    Other
}