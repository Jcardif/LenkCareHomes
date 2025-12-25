namespace LenkCareHomes.Api.Models.Homes;

/// <summary>
/// DTO for home information.
/// </summary>
public sealed record HomeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public string? PhoneNumber { get; init; }
    public required int Capacity { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required int TotalBeds { get; init; }
    public required int AvailableBeds { get; init; }
    public required int OccupiedBeds { get; init; }
    public required int ActiveClients { get; init; }
}

/// <summary>
/// Summary DTO for home listings.
/// </summary>
public sealed record HomeSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required bool IsActive { get; init; }
    public required int Capacity { get; init; }
    public required int AvailableBeds { get; init; }
    public required int ActiveClients { get; init; }
}

/// <summary>
/// Request model for creating a home.
/// </summary>
public sealed record CreateHomeRequest
{
    /// <summary>
    /// Gets the name of the home.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the street address.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Gets the state.
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Gets the ZIP code.
    /// </summary>
    public required string ZipCode { get; init; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Gets the maximum capacity (number of beds).
    /// </summary>
    public required int Capacity { get; init; }
}

/// <summary>
/// Request model for updating a home.
/// </summary>
public sealed record UpdateHomeRequest
{
    public string? Name { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? PhoneNumber { get; init; }
    public int? Capacity { get; init; }
}

/// <summary>
/// Response model for home operations.
/// </summary>
public sealed record HomeOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public HomeDto? Home { get; init; }
}
