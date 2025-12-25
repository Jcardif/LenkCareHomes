namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
/// Types of documents stored in the system.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Client care plan document.
    /// </summary>
    CarePlan,

    /// <summary>
    /// Medical report or lab results.
    /// </summary>
    MedicalReport,

    /// <summary>
    /// Consent form signed by client or family.
    /// </summary>
    ConsentForm,

    /// <summary>
    /// Insurance or billing document.
    /// </summary>
    Insurance,

    /// <summary>
    /// Legal document (POA, advance directive, etc.).
    /// </summary>
    Legal,

    /// <summary>
    /// Identification document.
    /// </summary>
    Identification,

    /// <summary>
    /// Document type not covered by other categories.
    /// </summary>
    Other
}
