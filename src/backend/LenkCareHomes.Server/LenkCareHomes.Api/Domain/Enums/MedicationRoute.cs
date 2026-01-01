namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Routes of medication administration.
/// </summary>
public enum MedicationRoute
{
    /// <summary>
    ///     Oral administration (by mouth).
    /// </summary>
    Oral,

    /// <summary>
    ///     Sublingual administration (under the tongue).
    /// </summary>
    Sublingual,

    /// <summary>
    ///     Topical administration (applied to skin).
    /// </summary>
    Topical,

    /// <summary>
    ///     Inhalation (inhaled through mouth or nose).
    /// </summary>
    Inhalation,

    /// <summary>
    ///     Injection (intramuscular, subcutaneous, etc.).
    /// </summary>
    Injection,

    /// <summary>
    ///     Transdermal (through skin patch).
    /// </summary>
    Transdermal,

    /// <summary>
    ///     Rectal administration.
    /// </summary>
    Rectal,

    /// <summary>
    ///     Ophthalmic (eye drops/ointment).
    /// </summary>
    Ophthalmic,

    /// <summary>
    ///     Otic (ear drops).
    /// </summary>
    Otic,

    /// <summary>
    ///     Nasal (nasal spray/drops).
    /// </summary>
    Nasal,

    /// <summary>
    ///     Other route not listed.
    /// </summary>
    Other
}