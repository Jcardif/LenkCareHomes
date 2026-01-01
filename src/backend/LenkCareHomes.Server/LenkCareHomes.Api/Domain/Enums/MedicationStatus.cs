namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Status of medication administration.
/// </summary>
public enum MedicationStatus
{
    /// <summary>
    ///     Medication was administered as scheduled.
    /// </summary>
    Administered,

    /// <summary>
    ///     Client refused the medication.
    /// </summary>
    Refused,

    /// <summary>
    ///     Medication was not available.
    /// </summary>
    NotAvailable,

    /// <summary>
    ///     Medication was held per clinical guidance.
    /// </summary>
    Held,

    /// <summary>
    ///     Medication was given early.
    /// </summary>
    GivenEarly,

    /// <summary>
    ///     Medication was given late.
    /// </summary>
    GivenLate
}