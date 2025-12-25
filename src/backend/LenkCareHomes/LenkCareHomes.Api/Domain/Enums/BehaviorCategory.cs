namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
/// Categories for behavior and mood notes.
/// </summary>
public enum BehaviorCategory
{
    /// <summary>
    /// Behavioral observation (actions, reactions).
    /// </summary>
    Behavior,

    /// <summary>
    /// Mood observation (emotional state).
    /// </summary>
    Mood,

    /// <summary>
    /// General observation that doesn't fit other categories.
    /// </summary>
    General
}
