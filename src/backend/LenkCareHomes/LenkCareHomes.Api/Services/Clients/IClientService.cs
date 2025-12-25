using LenkCareHomes.Api.Models.Clients;

namespace LenkCareHomes.Api.Services.Clients;

/// <summary>
/// Service interface for client management operations.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Gets all clients, optionally filtered by home and/or active status.
    /// </summary>
    Task<IReadOnlyList<ClientSummaryDto>> GetAllClientsAsync(
        Guid? homeId = null,
        bool? isActive = null,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a client by ID.
    /// </summary>
    Task<ClientDto?> GetClientByIdAsync(
        Guid clientId,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admits a new client.
    /// </summary>
    Task<ClientOperationResponse> AdmitClientAsync(
        AdmitClientRequest request,
        Guid admittedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing client.
    /// </summary>
    Task<ClientOperationResponse> UpdateClientAsync(
        Guid clientId,
        UpdateClientRequest request,
        Guid updatedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discharges a client.
    /// </summary>
    Task<ClientOperationResponse> DischargeClientAsync(
        Guid clientId,
        DischargeClientRequest request,
        Guid dischargedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers a client to a different bed.
    /// </summary>
    Task<ClientOperationResponse> TransferClientAsync(
        Guid clientId,
        TransferClientRequest request,
        Guid transferredById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets clients for caregiver view (only from assigned homes).
    /// </summary>
    Task<IReadOnlyList<ClientSummaryDto>> GetClientsByHomeIdsAsync(
        IReadOnlyList<Guid> homeIds,
        CancellationToken cancellationToken = default);
}
