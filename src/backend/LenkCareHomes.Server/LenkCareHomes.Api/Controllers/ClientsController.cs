using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Clients;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for client (resident) management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClientsController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;
    private readonly IClientService _clientService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        IClientService clientService,
        ICaregiverService caregiverService,
        ILogger<ClientsController> logger)
    {
        _clientService = clientService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets all clients with optional filters.
    ///     Caregivers only see clients from their assigned homes.
    /// </summary>
    /// <param name="homeId">Optional home ID filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of clients.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientSummaryDto>>> GetAllClientsAsync(
        [FromQuery] Guid? homeId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
            if (allowedHomeIds.Count == 0) return Ok(Array.Empty<ClientSummaryDto>());
        }

        var clients = await _clientService.GetAllClientsAsync(homeId, isActive, allowedHomeIds, cancellationToken);
        return Ok(clients);
    }

    /// <summary>
    ///     Gets a client by ID.
    ///     Caregivers can only access clients from their assigned homes.
    /// </summary>
    /// <param name="id">Client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Client details.</returns>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetClientByIdAsync))]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var client = await _clientService.GetClientByIdAsync(id, allowedHomeIds, cancellationToken);
        if (client is null)
        {
            // Check if client exists but user doesn't have access
            var existsButDenied = await _clientService.GetClientByIdAsync(id, null, cancellationToken);
            if (existsButDenied is not null) return Forbid();
            return NotFound();
        }

        return Ok(client);
    }

    /// <summary>
    ///     Admits a new client.
    ///     Admin only.
    /// </summary>
    /// <param name="request">Admit client request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Admitted client.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientOperationResponse>> AdmitClientAsync(
        [FromBody] AdmitClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _clientService.AdmitClientAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return CreatedAtAction(
            nameof(GetClientByIdAsync),
            new { id = response.Client!.Id },
            response);
    }

    /// <summary>
    ///     Updates a client.
    ///     Admin can update any client. Caregivers can only update clients in their assigned homes.
    /// </summary>
    /// <param name="id">Client ID.</param>
    /// <param name="request">Update client request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated client.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientOperationResponse>> UpdateClientAsync(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var response = await _clientService.UpdateClientAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Client not found or access denied.")
            {
                // Check if client exists but user doesn't have access
                var existsButDenied = await _clientService.GetClientByIdAsync(id, null, cancellationToken);
                if (existsButDenied is not null) return Forbid();
                return NotFound();
            }

            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Discharges a client.
    ///     Admin only.
    /// </summary>
    /// <param name="id">Client ID.</param>
    /// <param name="request">Discharge client request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discharged client.</returns>
    [HttpPost("{id:guid}/discharge")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientOperationResponse>> DischargeClientAsync(
        Guid id,
        [FromBody] DischargeClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _clientService.DischargeClientAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Client not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Transfers a client to a different bed.
    ///     Admin only.
    /// </summary>
    /// <param name="id">Client ID.</param>
    /// <param name="request">Transfer client request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transferred client.</returns>
    [HttpPost("{id:guid}/transfer")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ClientOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientOperationResponse>> TransferClientAsync(
        Guid id,
        [FromBody] TransferClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _clientService.TransferClientAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Client not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',').First().Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}