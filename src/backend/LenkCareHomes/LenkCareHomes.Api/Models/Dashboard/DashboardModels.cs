using LenkCareHomes.Api.Models.Appointments;

namespace LenkCareHomes.Api.Models.Dashboard;

/// <summary>
///     Dashboard statistics for Admin users.
/// </summary>
public sealed record AdminDashboardStats
{
    public required int TotalHomes { get; init; }
    public required int ActiveHomes { get; init; }
    public required int TotalBeds { get; init; }
    public required int AvailableBeds { get; init; }
    public required int OccupiedBeds { get; init; }
    public required int TotalClients { get; init; }
    public required int ActiveClients { get; init; }
    public required int TotalCaregivers { get; init; }
    public required int ActiveCaregivers { get; init; }
    public required int RecentIncidentsCount { get; init; }
    public required IReadOnlyList<UpcomingBirthdayDto> UpcomingBirthdays { get; init; }
    public required IReadOnlyList<UpcomingAppointmentDto> UpcomingAppointments { get; init; }
}

/// <summary>
///     DTO for upcoming client birthdays.
/// </summary>
public sealed record UpcomingBirthdayDto
{
    public required Guid ClientId { get; init; }
    public required string ClientName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required int Age { get; init; }
    public required int DaysUntilBirthday { get; init; }
    public required string HomeName { get; init; }
}

/// <summary>
///     Dashboard statistics for Caregiver users.
/// </summary>
public sealed record CaregiverDashboardStats
{
    public required int AssignedHomesCount { get; init; }
    public required int ActiveClientsCount { get; init; }
    public required IReadOnlyList<CaregiverHomeDto> AssignedHomes { get; init; }
    public required IReadOnlyList<CaregiverClientDto> Clients { get; init; }
    public required IReadOnlyList<UpcomingAppointmentDto> UpcomingAppointments { get; init; }
}

/// <summary>
///     DTO for caregiver's assigned home summary.
/// </summary>
public sealed record CaregiverHomeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required int ActiveClientsCount { get; init; }
}

/// <summary>
///     DTO for caregiver's client view.
/// </summary>
public sealed record CaregiverClientDto
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public string? BedLabel { get; init; }
    public string? Allergies { get; init; }
    public string? PhotoUrl { get; init; }
    public required DateTime DateOfBirth { get; init; }
}