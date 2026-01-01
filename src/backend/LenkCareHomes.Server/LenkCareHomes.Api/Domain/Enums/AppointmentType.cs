namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Types of appointments for adult family home residents.
///     Based on common healthcare and support appointments for elderly care.
/// </summary>
public enum AppointmentType
{
    /// <summary>
    ///     General practitioner or primary care physician visit.
    /// </summary>
    GeneralPractice,

    /// <summary>
    ///     Dental checkup or procedure.
    /// </summary>
    Dental,

    /// <summary>
    ///     Eye exam or ophthalmology visit.
    /// </summary>
    Ophthalmology,

    /// <summary>
    ///     Podiatry or foot care appointment.
    /// </summary>
    Podiatry,

    /// <summary>
    ///     Physical therapy session.
    /// </summary>
    PhysicalTherapy,

    /// <summary>
    ///     Occupational therapy session.
    /// </summary>
    OccupationalTherapy,

    /// <summary>
    ///     Speech therapy session.
    /// </summary>
    SpeechTherapy,

    /// <summary>
    ///     Psychiatry or mental health visit.
    /// </summary>
    Psychiatry,

    /// <summary>
    ///     Dermatology appointment.
    /// </summary>
    Dermatology,

    /// <summary>
    ///     Cardiology appointment.
    /// </summary>
    Cardiology,

    /// <summary>
    ///     Neurology appointment.
    /// </summary>
    Neurology,

    /// <summary>
    ///     Lab work or blood draw.
    /// </summary>
    LabWork,

    /// <summary>
    ///     Imaging, X-ray, or radiology.
    /// </summary>
    Imaging,

    /// <summary>
    ///     Audiology or hearing test.
    /// </summary>
    Audiology,

    /// <summary>
    ///     Social worker meeting.
    /// </summary>
    SocialWorker,

    /// <summary>
    ///     Family visit appointment.
    /// </summary>
    FamilyVisit,

    /// <summary>
    ///     Other appointment type not covered above.
    /// </summary>
    Other
}