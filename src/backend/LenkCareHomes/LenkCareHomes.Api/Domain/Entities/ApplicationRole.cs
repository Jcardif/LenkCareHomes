using Microsoft.AspNetCore.Identity;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents an application role with extended properties.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationRole" /> class.
    /// </summary>
    public ApplicationRole()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationRole" /> class with a role name.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    /// <summary>
    ///     Gets or sets a description of the role and its permissions.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets whether this role has access to PHI (Protected Health Information).
    /// </summary>
    public bool HasPhiAccess { get; set; }

    /// <summary>
    ///     Gets or sets when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}