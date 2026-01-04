using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Clients;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Clients;

/// <summary>
///     Service implementation for client management operations.
/// </summary>
public sealed class ClientService : IClientService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<ClientService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientSummaryDto>> GetAllClientsAsync(
        Guid? homeId = null,
        bool? isActive = null,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .AsQueryable();

        // Apply home filter
        if (homeId.HasValue) query = query.Where(c => c.HomeId == homeId.Value);

        // Apply active status filter
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);

        // Apply home-scoped authorization for caregivers
        if (allowedHomeIds is { Count: > 0 }) query = query.Where(c => allowedHomeIds.Contains(c.HomeId));

        var clients = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

        return clients.Select(MapToSummaryDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ClientDto?> GetClientByIdAsync(
        Guid clientId,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .Where(c => c.Id == clientId);

        // Apply home-scoped authorization for caregivers
        if (allowedHomeIds is { Count: > 0 }) query = query.Where(c => allowedHomeIds.Contains(c.HomeId));

        var client = await query.FirstOrDefaultAsync(cancellationToken);

        return client is null ? null : MapToDto(client);
    }

    /// <inheritdoc />
    public async Task<ClientOperationResponse> AdmitClientAsync(
        AdmitClientRequest request,
        Guid admittedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return new ClientOperationResponse { Success = false, Error = "First name is required." };

        if (string.IsNullOrWhiteSpace(request.LastName))
            return new ClientOperationResponse { Success = false, Error = "Last name is required." };

        // Verify home exists and is active
        var home = await _dbContext.Homes
            .FirstOrDefaultAsync(h => h.Id == request.HomeId, cancellationToken);

        if (home is null) return new ClientOperationResponse { Success = false, Error = "Home not found." };

        if (!home.IsActive)
            return new ClientOperationResponse { Success = false, Error = "Cannot admit clients to an inactive home." };

        // Check if admitting a client would exceed the home's capacity
        var activeClientCount = await _dbContext.Clients
            .CountAsync(c => c.HomeId == request.HomeId && c.IsActive, cancellationToken);

        if (activeClientCount >= home.Capacity)
            return new ClientOperationResponse
            {
                Success = false,
                Error = $"Cannot admit client. Home capacity is {home.Capacity} and there are already {activeClientCount} active clients."
            };

        // Verify bed exists, is active, and is available
        var bed = await _dbContext.Beds
            .FirstOrDefaultAsync(b => b.Id == request.BedId, cancellationToken);

        if (bed is null) return new ClientOperationResponse { Success = false, Error = "Bed not found." };

        if (bed.HomeId != request.HomeId)
            return new ClientOperationResponse
                { Success = false, Error = "Bed does not belong to the specified home." };

        if (!bed.IsActive)
            return new ClientOperationResponse { Success = false, Error = "Cannot assign client to an inactive bed." };

        if (bed.Status == BedStatus.Occupied)
            return new ClientOperationResponse { Success = false, Error = "Bed is already occupied." };

        // Create the client
        var client = new Client
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            SsnEncrypted = request.Ssn, // In production, this should be encrypted
            AdmissionDate = request.AdmissionDate,
            HomeId = request.HomeId,
            BedId = request.BedId,
            PrimaryPhysician = request.PrimaryPhysician,
            PrimaryPhysicianPhone = request.PrimaryPhysicianPhone,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            EmergencyContactRelationship = request.EmergencyContactRelationship,
            Allergies = request.Allergies,
            Diagnoses = request.Diagnoses,
            MedicationList = request.MedicationList,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedById = admittedById
        };

        // Update bed status
        bed.Status = BedStatus.Occupied;
        bed.UpdatedAt = DateTime.UtcNow;

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(admittedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ClientAdmitted,
            admittedById,
            userEmail ?? "Unknown",
            "Client",
            client.Id.ToString(),
            "Success",
            ipAddress,
            $"Admitted client '{client.FullName}' to home '{home.Name}', bed '{bed.Label}'",
            cancellationToken);

        _logger.LogInformation("Client {ClientId} admitted to home {HomeId} by user {UserId}",
            client.Id, request.HomeId, admittedById);

        // Reload to get navigation properties
        var admittedClient = await _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .FirstOrDefaultAsync(c => c.Id == client.Id, cancellationToken);

        return new ClientOperationResponse
        {
            Success = true,
            Client = admittedClient is null ? null : MapToDto(admittedClient)
        };
    }

    /// <inheritdoc />
    public async Task<ClientOperationResponse> UpdateClientAsync(
        Guid clientId,
        UpdateClientRequest request,
        Guid updatedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _dbContext.Clients
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .Where(c => c.Id == clientId);

        // Apply home-scoped authorization
        if (allowedHomeIds is { Count: > 0 }) query = query.Where(c => allowedHomeIds.Contains(c.HomeId));

        var client = await query.FirstOrDefaultAsync(cancellationToken);

        if (client is null)
            return new ClientOperationResponse { Success = false, Error = "Client not found or access denied." };

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(request.FirstName)) client.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName)) client.LastName = request.LastName;

        if (request.DateOfBirth.HasValue) client.DateOfBirth = request.DateOfBirth.Value;

        if (!string.IsNullOrWhiteSpace(request.Gender)) client.Gender = request.Gender;

        if (request.PrimaryPhysician is not null) client.PrimaryPhysician = request.PrimaryPhysician;

        if (request.PrimaryPhysicianPhone is not null) client.PrimaryPhysicianPhone = request.PrimaryPhysicianPhone;

        if (request.EmergencyContactName is not null) client.EmergencyContactName = request.EmergencyContactName;

        if (request.EmergencyContactPhone is not null) client.EmergencyContactPhone = request.EmergencyContactPhone;

        if (request.EmergencyContactRelationship is not null)
            client.EmergencyContactRelationship = request.EmergencyContactRelationship;

        if (request.Allergies is not null) client.Allergies = request.Allergies;

        if (request.Diagnoses is not null) client.Diagnoses = request.Diagnoses;

        if (request.MedicationList is not null) client.MedicationList = request.MedicationList;

        if (request.Notes is not null) client.Notes = request.Notes;

        if (request.PhotoUrl is not null) client.PhotoUrl = request.PhotoUrl;

        client.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(updatedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ClientUpdated,
            updatedById,
            userEmail ?? "Unknown",
            "Client",
            client.Id.ToString(),
            "Success",
            ipAddress,
            $"Updated client '{client.FullName}'",
            cancellationToken);

        _logger.LogInformation("Client {ClientId} updated by user {UserId}", client.Id, updatedById);

        return new ClientOperationResponse
        {
            Success = true,
            Client = MapToDto(client)
        };
    }

    /// <inheritdoc />
    public async Task<ClientOperationResponse> DischargeClientAsync(
        Guid clientId,
        DischargeClientRequest request,
        Guid dischargedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _dbContext.Clients
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null) return new ClientOperationResponse { Success = false, Error = "Client not found." };

        if (!client.IsActive)
            return new ClientOperationResponse { Success = false, Error = "Client is already discharged." };

        // Validate discharge date is not before admission date
        if (request.DischargeDate.Date < client.AdmissionDate.Date)
            return new ClientOperationResponse { Success = false, Error = "Discharge date cannot be before admission date." };

        // Update client
        client.DischargeDate = request.DischargeDate;
        client.DischargeReason = request.DischargeReason;
        client.IsActive = false;
        client.UpdatedAt = DateTime.UtcNow;

        // Free up the bed
        if (client.Bed is not null)
        {
            client.Bed.Status = BedStatus.Available;
            client.Bed.UpdatedAt = DateTime.UtcNow;
        }

        var previousBedId = client.BedId;
        client.BedId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(dischargedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ClientDischarged,
            dischargedById,
            userEmail ?? "Unknown",
            "Client",
            client.Id.ToString(),
            "Success",
            ipAddress,
            $"Discharged client '{client.FullName}' from home '{client.Home?.Name}'. Reason: {request.DischargeReason}",
            cancellationToken);

        _logger.LogInformation("Client {ClientId} discharged by user {UserId}", client.Id, dischargedById);

        return new ClientOperationResponse
        {
            Success = true,
            Client = MapToDto(client)
        };
    }

    /// <inheritdoc />
    public async Task<ClientOperationResponse> TransferClientAsync(
        Guid clientId,
        TransferClientRequest request,
        Guid transferredById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _dbContext.Clients
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null) return new ClientOperationResponse { Success = false, Error = "Client not found." };

        if (!client.IsActive)
            return new ClientOperationResponse { Success = false, Error = "Cannot transfer a discharged client." };

        // Get the new bed
        var newBed = await _dbContext.Beds
            .FirstOrDefaultAsync(b => b.Id == request.NewBedId, cancellationToken);

        if (newBed is null) return new ClientOperationResponse { Success = false, Error = "New bed not found." };

        if (newBed.HomeId != client.HomeId)
            return new ClientOperationResponse
                { Success = false, Error = "Cannot transfer to a bed in a different home." };

        if (!newBed.IsActive)
            return new ClientOperationResponse { Success = false, Error = "Cannot transfer to an inactive bed." };

        if (newBed.Status == BedStatus.Occupied)
            return new ClientOperationResponse { Success = false, Error = "New bed is already occupied." };

        // Free up the old bed
        var oldBedLabel = client.Bed?.Label ?? "Unknown";
        if (client.Bed is not null)
        {
            client.Bed.Status = BedStatus.Available;
            client.Bed.UpdatedAt = DateTime.UtcNow;
        }

        // Assign to new bed
        client.BedId = newBed.Id;
        newBed.Status = BedStatus.Occupied;
        newBed.UpdatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(transferredById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ClientTransferred,
            transferredById,
            userEmail ?? "Unknown",
            "Client",
            client.Id.ToString(),
            "Success",
            ipAddress,
            $"Transferred client '{client.FullName}' from bed '{oldBedLabel}' to bed '{newBed.Label}'",
            cancellationToken);

        _logger.LogInformation("Client {ClientId} transferred to bed {BedId} by user {UserId}",
            client.Id, newBed.Id, transferredById);

        // Reload to get updated navigation properties
        var updatedClient = await _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .FirstOrDefaultAsync(c => c.Id == client.Id, cancellationToken);

        return new ClientOperationResponse
        {
            Success = true,
            Client = updatedClient is null ? null : MapToDto(updatedClient)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientSummaryDto>> GetClientsByHomeIdsAsync(
        IReadOnlyList<Guid> homeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(homeIds);

        if (homeIds.Count == 0) return [];

        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .Where(c => homeIds.Contains(c.HomeId) && c.IsActive)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

        return clients.Select(MapToSummaryDto).ToList();
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            DateOfBirth = client.DateOfBirth,
            Gender = client.Gender,
            AdmissionDate = client.AdmissionDate,
            DischargeDate = client.DischargeDate,
            DischargeReason = client.DischargeReason,
            HomeId = client.HomeId,
            HomeName = client.Home?.Name ?? "Unknown",
            BedId = client.BedId,
            BedLabel = client.Bed?.Label,
            PrimaryPhysician = client.PrimaryPhysician,
            PrimaryPhysicianPhone = client.PrimaryPhysicianPhone,
            EmergencyContactName = client.EmergencyContactName,
            EmergencyContactPhone = client.EmergencyContactPhone,
            EmergencyContactRelationship = client.EmergencyContactRelationship,
            Allergies = client.Allergies,
            Diagnoses = client.Diagnoses,
            MedicationList = client.MedicationList,
            PhotoUrl = client.PhotoUrl,
            Notes = client.Notes,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }

    private static ClientSummaryDto MapToSummaryDto(Client client)
    {
        return new ClientSummaryDto
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            DateOfBirth = client.DateOfBirth,
            HomeId = client.HomeId,
            HomeName = client.Home?.Name ?? "Unknown",
            BedLabel = client.Bed?.Label,
            Allergies = client.Allergies,
            PhotoUrl = client.PhotoUrl,
            IsActive = client.IsActive,
            AdmissionDate = client.AdmissionDate
        };
    }

    private async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.Email;
    }
}