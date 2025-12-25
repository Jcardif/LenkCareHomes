namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     ADL independence level based on Katz Index.
/// </summary>
public enum ADLLevel
{
    /// <summary>
    ///     Client is able to perform the activity independently.
    ///     Score: 2
    /// </summary>
    Independent = 2,

    /// <summary>
    ///     Client requires partial assistance to perform the activity.
    ///     Score: 1
    /// </summary>
    PartialAssist = 1,

    /// <summary>
    ///     Client is fully dependent and requires complete assistance.
    ///     Score: 0
    /// </summary>
    Dependent = 0,

    /// <summary>
    ///     Activity is not applicable to this client.
    ///     Not included in score calculation.
    /// </summary>
    NotApplicable = -1
}