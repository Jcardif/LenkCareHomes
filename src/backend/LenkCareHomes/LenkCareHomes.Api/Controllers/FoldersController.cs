using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Documents;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for document folder management operations.
/// </summary>
[ApiController]
[Route("api/folders")]
[Authorize]
public sealed class FoldersController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;
    private readonly IFolderService _folderService;
    private readonly ILogger<FoldersController> _logger;

    public FoldersController(
        IFolderService folderService,
        ICaregiverService caregiverService,
        ILogger<FoldersController> logger)
    {
        _folderService = folderService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets the folder tree for navigation.
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<FolderTreeNodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FolderTreeNodeDto>>> GetFolderTreeAsync(
        [FromQuery] DocumentScope? scope = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? homeId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var tree = await _folderService.GetFolderTreeAsync(
            scope,
            clientId,
            homeId,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        return Ok(tree);
    }

    /// <summary>
    ///     Gets root-level folders with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FolderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FolderSummaryDto>>> GetRootFoldersAsync(
        [FromQuery] DocumentScope? scope = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? homeId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var folders = await _folderService.GetRootFoldersAsync(
            scope,
            clientId,
            homeId,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        return Ok(folders);
    }

    /// <summary>
    ///     Gets a folder by ID with its contents.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderDto>> GetFolderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var folder = await _folderService.GetFolderByIdAsync(
            id,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        if (folder is null) return NotFound();

        return Ok(folder);
    }

    /// <summary>
    ///     Creates a new folder.
    ///     Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FolderOperationResponse>> CreateFolderAsync(
        [FromBody] CreateFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _folderService.CreateFolderAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Updates a folder.
    ///     Admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderOperationResponse>> UpdateFolderAsync(
        Guid id,
        [FromBody] UpdateFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _folderService.UpdateFolderAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Folder not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Moves a folder to a new parent.
    ///     Admin only.
    /// </summary>
    [HttpPut("{id:guid}/move")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderOperationResponse>> MoveFolderAsync(
        Guid id,
        [FromBody] MoveFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _folderService.MoveFolderAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Folder not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Deletes a folder.
    ///     Admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FolderOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderOperationResponse>> DeleteFolderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _folderService.DeleteFolderAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Folder not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Browse documents with folder/scope filtering.
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(BrowseDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrowseDocumentsResponse>> BrowseDocumentsAsync(
        [FromQuery] DocumentScope? scope = null,
        [FromQuery] Guid? folderId = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? homeId = null,
        [FromQuery] DocumentType? documentType = null,
        [FromQuery] string? searchText = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var query = new BrowseDocumentsQuery
        {
            Scope = scope,
            FolderId = folderId,
            ClientId = clientId,
            HomeId = homeId,
            DocumentType = documentType,
            SearchText = searchText,
            PageNumber = pageNumber,
            PageSize = Math.Min(pageSize, 100)
        };

        var response = await _folderService.BrowseDocumentsAsync(
            query,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

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