using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Dashboard;
using LenkCareHomes.Api.Services.Appointments;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Dashboard;

/// <summary>
///     Service implementation for dashboard statistics operations.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IAppointmentService _appointmentService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext dbContext,
        IAppointmentService appointmentService,
        ILogger<DashboardService> logger)
    {
        _dbContext = dbContext;
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        // Get home counts
        var homes = await _dbContext.Homes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalHomes = homes.Count;
        var activeHomes = homes.Count(h => h.IsActive);

        // Get bed counts
        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync(cancellationToken);

        var totalBeds = beds.Count;
        var availableBeds = beds.Count(b => b.Status == BedStatus.Available);
        var occupiedBeds = beds.Count(b => b.Status == BedStatus.Occupied);

        // Get client counts
        var clients = await _dbContext.Clients
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalClients = clients.Count;
        var activeClients = clients.Count(c => c.IsActive);

        // Get caregiver counts
        var caregiverRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.Caregiver, cancellationToken);

        var totalCaregivers = 0;
        var activeCaregivers = 0;

        if (caregiverRole is not null)
        {
            var caregiverUserIds = await _dbContext.UserRoles
                .Where(ur => ur.RoleId == caregiverRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            var caregivers = await _dbContext.Users
                .AsNoTracking()
                .Where(u => caregiverUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            totalCaregivers = caregivers.Count;
            activeCaregivers = caregivers.Count(c => c.IsActive);
        }

        // Get upcoming birthdays (next 30 days)
        var today = DateTime.UtcNow.Date;
        var upcomingBirthdays = clients
            .Where(c => c.IsActive)
            .Select(c =>
            {
                var thisYearBirthday = new DateTime(today.Year, c.DateOfBirth.Month, c.DateOfBirth.Day);
                if (thisYearBirthday < today) thisYearBirthday = thisYearBirthday.AddYears(1);
                var daysUntil = (thisYearBirthday - today).Days;
                var age = today.Year - c.DateOfBirth.Year;
                if (thisYearBirthday > today.AddDays(30)) return null;
                return new { Client = c, DaysUntil = daysUntil, Age = age };
            })
            .Where(x => x is not null && x.DaysUntil <= 30)
            .OrderBy(x => x!.DaysUntil)
            .Take(10)
            .ToList();

        // Get home names for birthdays
        var homeIds = upcomingBirthdays.Select(x => x!.Client.HomeId).Distinct().ToList();
        var homeNames = await _dbContext.Homes
            .AsNoTracking()
            .Where(h => homeIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Name, cancellationToken);

        var birthdayDtos = upcomingBirthdays
            .Select(x => new UpcomingBirthdayDto
            {
                ClientId = x!.Client.Id,
                ClientName = x.Client.FullName,
                DateOfBirth = x.Client.DateOfBirth,
                Age = x.Age,
                DaysUntilBirthday = x.DaysUntil,
                HomeName = homeNames.GetValueOrDefault(x.Client.HomeId, "Unknown")
            })
            .ToList();

        // Get upcoming appointments (next 7 days)
        var upcomingAppointments = await _appointmentService.GetUpcomingAppointmentsAsync(
            7,
            10,
            null, // Admin sees all homes
            cancellationToken);

        return new AdminDashboardStats
        {
            TotalHomes = totalHomes,
            ActiveHomes = activeHomes,
            TotalBeds = totalBeds,
            AvailableBeds = availableBeds,
            OccupiedBeds = occupiedBeds,
            TotalClients = totalClients,
            ActiveClients = activeClients,
            TotalCaregivers = totalCaregivers,
            ActiveCaregivers = activeCaregivers,
            RecentIncidentsCount = 0, // Will be implemented in Phase 4
            UpcomingBirthdays = birthdayDtos,
            UpcomingAppointments = upcomingAppointments
        };
    }

    /// <inheritdoc />
    public async Task<CaregiverDashboardStats> GetCaregiverDashboardStatsAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        // Get assigned homes
        var assignedHomeIds = await _dbContext.CaregiverHomeAssignments
            .AsNoTracking()
            .Where(ca => ca.UserId == caregiverId && ca.IsActive)
            .Select(ca => ca.HomeId)
            .ToListAsync(cancellationToken);

        if (assignedHomeIds.Count == 0)
            return new CaregiverDashboardStats
            {
                AssignedHomesCount = 0,
                ActiveClientsCount = 0,
                AssignedHomes = [],
                Clients = [],
                UpcomingAppointments = []
            };

        // Get home details
        var homes = await _dbContext.Homes
            .AsNoTracking()
            .Where(h => assignedHomeIds.Contains(h.Id) && h.IsActive)
            .ToListAsync(cancellationToken);

        // Get clients in assigned homes
        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .Where(c => assignedHomeIds.Contains(c.HomeId) && c.IsActive)
            .ToListAsync(cancellationToken);

        // Group clients by home for count
        var clientsByHome = clients.GroupBy(c => c.HomeId).ToDictionary(g => g.Key, g => g.Count());

        var homeDtos = homes.Select(h => new CaregiverHomeDto
        {
            Id = h.Id,
            Name = h.Name,
            Address = h.Address,
            City = h.City,
            State = h.State,
            ActiveClientsCount = clientsByHome.GetValueOrDefault(h.Id, 0)
        }).ToList();

        var clientDtos = clients.Select(c => new CaregiverClientDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            HomeId = c.HomeId,
            HomeName = c.Home?.Name ?? "Unknown",
            BedLabel = c.Bed?.Label,
            Allergies = c.Allergies,
            PhotoUrl = c.PhotoUrl,
            DateOfBirth = c.DateOfBirth
        }).ToList();

        // Get upcoming appointments for assigned homes
        var upcomingAppointments = await _appointmentService.GetUpcomingAppointmentsAsync(
            7,
            10,
            assignedHomeIds,
            cancellationToken);

        return new CaregiverDashboardStats
        {
            AssignedHomesCount = homes.Count,
            ActiveClientsCount = clients.Count,
            AssignedHomes = homeDtos,
            Clients = clientDtos,
            UpcomingAppointments = upcomingAppointments
        };
    }
}