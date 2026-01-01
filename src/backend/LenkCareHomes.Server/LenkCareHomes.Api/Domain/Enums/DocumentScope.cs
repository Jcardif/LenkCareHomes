namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Defines the scope of a document or folder.
/// </summary>
public enum DocumentScope
{
    /// <summary>
    ///     Document/folder belongs to a specific client.
    /// </summary>
    Client,

    /// <summary>
    ///     Document/folder belongs to a specific home/facility.
    /// </summary>
    Home,

    /// <summary>
    ///     Business-wide document/folder (e.g., policies, procedures).
    /// </summary>
    Business,

    /// <summary>
    ///     General/miscellaneous documents not tied to a specific scope.
    /// </summary>
    General
}