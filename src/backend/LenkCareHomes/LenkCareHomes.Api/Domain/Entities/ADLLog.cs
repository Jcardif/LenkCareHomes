using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents an ADL (Activities of Daily Living) log entry based on Katz Index.
///     Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class ADLLog
{
    /// <summary>
    ///     Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the client ID.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the caregiver ID who recorded this entry.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the ADL was observed/performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the bathing independence level.
    /// </summary>
    public ADLLevel? Bathing { get; set; }

    /// <summary>
    ///     Gets or sets the dressing independence level.
    /// </summary>
    public ADLLevel? Dressing { get; set; }

    /// <summary>
    ///     Gets or sets the toileting independence level.
    /// </summary>
    public ADLLevel? Toileting { get; set; }

    /// <summary>
    ///     Gets or sets the transferring independence level.
    /// </summary>
    public ADLLevel? Transferring { get; set; }

    /// <summary>
    ///     Gets or sets the continence level.
    /// </summary>
    public ADLLevel? Continence { get; set; }

    /// <summary>
    ///     Gets or sets the feeding independence level.
    /// </summary>
    public ADLLevel? Feeding { get; set; }

    /// <summary>
    ///     Gets or sets optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    ///     Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }

    /// <summary>
    ///     Calculates the Katz Index score (0-12 for standard, or 0-6 counting only Independent).
    ///     Higher scores indicate greater independence.
    /// </summary>
    public int CalculateKatzScore()
    {
        var score = 0;
        var levels = new[] { Bathing, Dressing, Toileting, Transferring, Continence, Feeding };

        foreach (var level in levels)
            if (level.HasValue && level.Value != ADLLevel.NotApplicable)
                score += (int)level.Value;

        return score;
    }
}