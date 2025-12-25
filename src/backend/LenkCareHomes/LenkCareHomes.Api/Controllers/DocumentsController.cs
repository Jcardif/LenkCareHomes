using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Documents;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for document management operations.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICaregiverService _caregiverService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ICaregiverService caregiverService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a document upload for a client.
    /// Admin only.
    /// </summary>
    [HttpPost("clients/{clientId:guid}/documents")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentUploadResponse>> InitiateUploadAsync(
        Guid clientId,
        [FromBody] UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.InitiateUploadAsync(
            clientId,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Initiates a document upload for any scope (client, home, business, or general).
    /// Admin only.
    /// </summary>
    [HttpPost("documents/upload")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentUploadResponse>> InitiateGeneralUploadAsync(
        [FromBody] UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.InitiateGeneralUploadAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Confirms that a document upload has completed.
    /// Admin only.
    /// </summary>
    [HttpPost("documents/{id:guid}/confirm")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentOperationResponse>> ConfirmUploadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.ConfirmUploadAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Document not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets documents for a client.
    /// Caregivers only see documents they have permission to access.
    /// </summary>
    [HttpGet("clients/{clientId:guid}/documents")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentSummaryDto>>> GetDocumentsByClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
            if (allowedHomeIds.Count == 0)
            {
                return Ok(Array.Empty<DocumentSummaryDto>());
            }
        }

        var documents = await _documentService.GetDocumentsByClientAsync(
            clientId,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        return Ok(documents);
    }

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    [HttpGet("documents/{id:guid}")]
    [ActionName(nameof(GetDocumentByIdAsync))]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetDocumentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var document = await _documentService.GetDocumentByIdAsync(
            id,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        if (document is null)
        {
            // Check if document exists but user doesn't have access
            var existsButDenied = await _documentService.GetDocumentByIdAsync(id, currentUserId.Value, true, null, cancellationToken);
            if (existsButDenied is not null)
            {
                return Forbid();
            }
            return NotFound();
        }

        return Ok(document);
    }

    /// <summary>
    /// Gets a SAS URL for viewing a document.
    /// Validates permissions before generating URL.
    /// </summary>
    [HttpGet("documents/{id:guid}/sas")]
    [ProducesResponseType(typeof(DocumentViewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentViewResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentViewResponse>> GetViewSasUrlAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _documentService.GetViewSasUrlAsync(
            id,
            currentUserId.Value,
            isAdmin,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Document not found.")
            {
                return NotFound();
            }
            if (response.Error?.Contains("permission") == true || response.Error?.Contains("denied") == true)
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes a document.
    /// Admin only.
    /// </summary>
    [HttpDelete("documents/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentOperationResponse>> DeleteDocumentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.DeleteDocumentAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Document not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Grants document access to caregivers.
    /// Admin only.
    /// </summary>
    [HttpPost("documents/{id:guid}/permissions")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentOperationResponse>> GrantAccessAsync(
        Guid id,
        [FromBody] GrantDocumentAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.GrantAccessAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Document not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Revokes document access from a caregiver.
    /// Admin only.
    /// </summary>
    [HttpDelete("documents/{id:guid}/permissions/{caregiverId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DocumentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentOperationResponse>> RevokeAccessAsync(
        Guid id,
        Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _documentService.RevokeAccessAsync(
            id,
            caregiverId,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Permission not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets the access history for a document.
    /// Admin only.
    /// </summary>
    [HttpGet("documents/{id:guid}/history")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentAccessHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentAccessHistoryDto>>> GetAccessHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var history = await _documentService.GetAccessHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
