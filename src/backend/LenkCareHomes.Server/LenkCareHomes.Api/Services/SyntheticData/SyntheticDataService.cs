using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Documents;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ActivityEntity = LenkCareHomes.Api.Domain.Entities.Activity;

namespace LenkCareHomes.Api.Services.SyntheticData;

/// <summary>
///     Service implementation for loading synthetic data.
///     Only available in development environment.
/// </summary>
public sealed class SyntheticDataService : ISyntheticDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IAuditLogService _auditService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SyntheticDataService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public SyntheticDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditService,
        IBlobStorageService blobStorageService,
        IWebHostEnvironment environment,
        ILogger<SyntheticDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
        _blobStorageService = blobStorageService;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsAvailable => _environment.IsDevelopment();

    /// <inheritdoc />
    public async Task<DataStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return new DataStatistics
        {
            HomeCount = await _context.Homes.CountAsync(cancellationToken),
            BedCount = await _context.Beds.CountAsync(cancellationToken),
            UserCount = await _context.Users.CountAsync(cancellationToken),
            ClientCount = await _context.Clients.CountAsync(cancellationToken),
            ActiveClientCount = await _context.Clients.CountAsync(c => c.IsActive, cancellationToken),
            AdlLogCount = await _context.ADLLogs.CountAsync(cancellationToken),
            VitalsLogCount = await _context.VitalsLogs.CountAsync(cancellationToken),
            MedicationLogCount = await _context.MedicationLogs.CountAsync(cancellationToken),
            RomLogCount = await _context.ROMLogs.CountAsync(cancellationToken),
            BehaviorNoteCount = await _context.BehaviorNotes.CountAsync(cancellationToken),
            ActivityCount = await _context.Activities.CountAsync(cancellationToken),
            IncidentCount = await _context.Incidents.CountAsync(cancellationToken),
            DocumentCount = await _context.Documents.CountAsync(d => d.IsActive, cancellationToken),
            AppointmentCount = await _context.Appointments.CountAsync(cancellationToken)
        };
    }

    /// <inheritdoc />
    public Task<LoadSyntheticDataResult> LoadDataAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Call the progress version with a no-op callback
        return LoadDataWithProgressAsync(userId, ipAddress, _ => Task.CompletedTask, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LoadSyntheticDataResult> LoadDataWithProgressAsync(
        Guid userId,
        string? ipAddress,
        Func<LoadProgressUpdate, Task> progressCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progressCallback);

        if (!IsAvailable)
            return new LoadSyntheticDataResult
            {
                Success = false,
                Error = "Synthetic data loading is only available in development environment."
            };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting synthetic data load by user {UserId}", userId);

            // Increase command timeout for bulk insert operations (5 minutes)
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Initialization",
                Message = "Locating synthetic data files...",
                CurrentStep = 1,
                TotalSteps = 15
            });

            // Find the data files
            var dataPath = FindDataPath();
            if (dataPath is null)
            {
                await progressCallback(new LoadProgressUpdate
                {
                    Phase = "Error",
                    Message = "Synthetic data files not found",
                    CurrentStep = 1,
                    TotalSteps = 15,
                    IsError = true,
                    ErrorMessage =
                        "Synthetic data files not found. Run the Python generator first: cd src/datagen && python generate.py"
                });

                return new LoadSyntheticDataResult
                {
                    Success = false,
                    Error =
                        "Synthetic data files not found. Run the Python generator first: cd src/datagen && python generate.py"
                };
            }

            var allDataFile = Path.Combine(dataPath, "all_data.json");
            if (!File.Exists(allDataFile))
            {
                await progressCallback(new LoadProgressUpdate
                {
                    Phase = "Error",
                    Message = "Data file not found",
                    CurrentStep = 1,
                    TotalSteps = 15,
                    IsError = true,
                    ErrorMessage = $"Data file not found: {allDataFile}"
                });

                return new LoadSyntheticDataResult
                {
                    Success = false,
                    Error = $"Data file not found: {allDataFile}"
                };
            }

            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Parsing",
                Message = "Reading and parsing data files...",
                CurrentStep = 2,
                TotalSteps = 15
            });

            // Read and parse the data
            var jsonContent = await File.ReadAllTextAsync(allDataFile, cancellationToken);
            var data = JsonSerializer.Deserialize<SyntheticDataSet>(jsonContent, JsonOptions);

            if (data is null)
            {
                await progressCallback(new LoadProgressUpdate
                {
                    Phase = "Error",
                    Message = "Failed to parse data file",
                    CurrentStep = 2,
                    TotalSteps = 15,
                    IsError = true,
                    ErrorMessage = "Failed to parse synthetic data file."
                });

                return new LoadSyntheticDataResult
                {
                    Success = false,
                    Error = "Failed to parse synthetic data file."
                };
            }

            // Load data in correct order (respecting foreign key relationships)
            var result = await LoadDataInternalAsync(data, userId, dataPath, progressCallback, cancellationToken);

            stopwatch.Stop();
            result = result with { Duration = stopwatch.Elapsed };

            // Send completion update
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Complete",
                Message = $"Successfully loaded all data in {result.Duration.TotalSeconds:F1} seconds",
                CurrentStep = 15,
                TotalSteps = 15,
                IsComplete = true,
                ItemsProcessed = result.HomesLoaded + result.ClientsLoaded + result.CareLogsLoaded +
                                 result.DocumentsLoaded
            });

            // Log audit event
            await _auditService.LogPhiAccessAsync(
                "SyntheticData.Load",
                userId,
                "",
                "System",
                "SyntheticData",
                result.Success ? "Success" : "Failure",
                ipAddress,
                $"Loaded synthetic data: {result.HomesLoaded} homes, {result.UsersLoaded} users, {result.ClientsLoaded} clients, {result.CareLogsLoaded} care logs, {result.DocumentsLoaded} documents",
                cancellationToken);

            _logger.LogInformation(
                "Synthetic data load completed in {Duration}ms: {Homes} homes, {Users} users, {Clients} clients, {Documents} documents",
                stopwatch.ElapsedMilliseconds,
                result.HomesLoaded,
                result.UsersLoaded,
                result.ClientsLoaded,
                result.DocumentsLoaded);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load synthetic data");

            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Error",
                Message = "Failed to load synthetic data",
                CurrentStep = 0,
                TotalSteps = 15,
                IsError = true,
                ErrorMessage = ex.Message
            });

            return new LoadSyntheticDataResult
            {
                Success = false,
                Error = $"Failed to load synthetic data: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc />
    public Task<ClearDataResult> ClearDataAsync(
        Guid userId,
        string? ipAddress,
        IReadOnlyList<Guid>? userIdsToKeep = null,
        CancellationToken cancellationToken = default)
    {
        // Call the progress version with a no-op callback
        return ClearDataWithProgressAsync(userId, ipAddress, _ => Task.CompletedTask, userIdsToKeep, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ClearDataResult> ClearDataWithProgressAsync(
        Guid userId,
        string? ipAddress,
        Func<LoadProgressUpdate, Task> progressCallback,
        IReadOnlyList<Guid>? userIdsToKeep = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progressCallback);

        // Ensure the current user is always kept
        var preservedUserIds = new HashSet<Guid> { userId };
        if (userIdsToKeep is not null)
        {
            foreach (var id in userIdsToKeep)
                preservedUserIds.Add(id);
        }

        if (!IsAvailable)
            return new ClearDataResult
            {
                Success = false,
                Error = "Data clearing is only available in development environment."
            };

        const int totalSteps = 8;

        try
        {
            _logger.LogWarning("Starting data clear by user {UserId}", userId);

            // Increase command timeout for bulk delete operations (5 minutes)
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var sqlRecordsDeleted = 0;
            var blobsDeleted = 0;
            var auditLogsDeleted = 0;

            // ================================================================
            // Step 1: Delete care logs
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Care Logs",
                Message = "Deleting care logs (ADLs, vitals, medications, etc.)...",
                CurrentStep = 1,
                TotalSteps = totalSteps
            });

            sqlRecordsDeleted += await _context.ActivityParticipants.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.Activities.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.IncidentFollowUps.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.IncidentPhotos.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.Incidents.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.Appointments.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.BehaviorNotes.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.ROMLogs.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.MedicationLogs.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.VitalsLogs.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.ADLLogs.ExecuteDeleteAsync(cancellationToken);

            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Care Logs",
                Message = $"Deleted {sqlRecordsDeleted:N0} care log records",
                CurrentStep = 2,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            // ================================================================
            // Step 2: Delete documents (SQL records)
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Documents",
                Message = "Deleting document records...",
                CurrentStep = 3,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            sqlRecordsDeleted += await _context.DocumentAccessHistory.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.DocumentAccessPermissions.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.Documents.ExecuteDeleteAsync(cancellationToken);
            // DocumentFolders can have parent-child relationships, delete all
            sqlRecordsDeleted += await _context.DocumentFolders.ExecuteDeleteAsync(cancellationToken);

            // ================================================================
            // Step 3: Delete clients
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Clients",
                Message = "Deleting client records...",
                CurrentStep = 4,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            sqlRecordsDeleted += await _context.Clients.ExecuteDeleteAsync(cancellationToken);

            // ================================================================
            // Step 4: Delete caregiver assignments
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Assignments",
                Message = "Removing caregiver assignments...",
                CurrentStep = 5,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            sqlRecordsDeleted += await _context.CaregiverHomeAssignments
                .Where(ca => !preservedUserIds.Contains(ca.UserId))
                .ExecuteDeleteAsync(cancellationToken);

            // Beds and homes
            sqlRecordsDeleted += await _context.Beds.ExecuteDeleteAsync(cancellationToken);
            sqlRecordsDeleted += await _context.Homes.ExecuteDeleteAsync(cancellationToken);

            // ================================================================
            // Step 5: Delete users (except preserved users)
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Users",
                Message = $"Deleting user accounts (keeping {preservedUserIds.Count} selected users)...",
                CurrentStep = 6,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            var usersToDelete = await _context.Users
                .Where(u => !preservedUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in usersToDelete)
            {
                await _userManager.DeleteAsync(user);
                sqlRecordsDeleted++;
            }

            _logger.LogInformation("SQL data cleared: {Count} records deleted, {Kept} users preserved", 
                sqlRecordsDeleted, preservedUserIds.Count);

            // ================================================================
            // Step 6: Clear Azure Blob Storage
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Blob Storage",
                Message = "Clearing document files from blob storage...",
                CurrentStep = 7,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted
            });

            try
            {
                blobsDeleted = await _blobStorageService.DeleteAllBlobsAsync();
                _logger.LogInformation("Blob storage cleared: {Count} blobs deleted", blobsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear blob storage - continuing with data clear");
            }

            // ================================================================
            // Step 7: Clear Cosmos DB audit logs
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Audit Logs",
                Message = "Clearing audit logs from Cosmos DB...",
                CurrentStep = 8,
                TotalSteps = totalSteps,
                ItemsProcessed = sqlRecordsDeleted + blobsDeleted
            });

            try
            {
                auditLogsDeleted = await _auditService.ClearAllLogsAsync(cancellationToken);
                _logger.LogInformation("Cosmos DB audit logs cleared: {Count} entries deleted", auditLogsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear Cosmos DB audit logs - continuing with data clear");
            }

            var totalDeleted = sqlRecordsDeleted + blobsDeleted + auditLogsDeleted;

            // ================================================================
            // Complete
            // ================================================================
            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Complete",
                Message = $"Successfully deleted {totalDeleted:N0} records",
                CurrentStep = 8,
                TotalSteps = totalSteps,
                ItemsProcessed = totalDeleted,
                IsComplete = true
            });

            _logger.LogWarning(
                "Data clear completed: {SqlCount} SQL records, {BlobCount} blobs, {AuditCount} audit logs deleted",
                sqlRecordsDeleted, blobsDeleted, auditLogsDeleted);

            return new ClearDataResult
            {
                Success = true,
                RecordsDeleted = totalDeleted,
                Details = $"SQL: {sqlRecordsDeleted}, Blobs: {blobsDeleted}, Audit Logs: {auditLogsDeleted}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear data");

            await progressCallback(new LoadProgressUpdate
            {
                Phase = "Error",
                Message = "Failed to clear data",
                CurrentStep = 0,
                TotalSteps = totalSteps,
                IsError = true,
                ErrorMessage = ex.Message
            });

            return new ClearDataResult
            {
                Success = false,
                Error = $"Failed to clear data: {ex.Message}"
            };
        }
    }

    private string? FindDataPath()
    {
        // Try multiple possible locations for the data files
        // Priority: 1. Output directory (copied during build), 2. Source directory
        var possiblePaths = new[]
        {
            // Primary: SyntheticData folder copied to output directory during Debug build
            Path.Combine(AppContext.BaseDirectory, "SyntheticData"),
            // Fallback: Source SyntheticData folder (when running before build or via Aspire)
            // This allows datagen to run after API starts and still be found
            Path.Combine(_environment.ContentRootPath, "SyntheticData")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            _logger.LogDebug("Checking for synthetic data at: {Path}", fullPath);

            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "all_data.json")))
            {
                _logger.LogInformation("Found synthetic data at: {Path}", fullPath);
                return fullPath;
            }
        }

        _logger.LogWarning("Could not find synthetic data files in any expected location. Tried: {Paths}",
            string.Join(", ", possiblePaths.Select(Path.GetFullPath)));
        return null;
    }

    private string? FindImagesPath()
    {
        // Try multiple possible locations for the images directory
        // Images are now copied to SyntheticData/images by the generator
        var possiblePaths = new[]
        {
            // Primary: SyntheticData/images folder copied during build (from generator)
            Path.Combine(AppContext.BaseDirectory, "SyntheticData", "images"),
            // Source SyntheticData/images folder (when running before build or via Aspire)
            Path.Combine(_environment.ContentRootPath, "SyntheticData", "images"),
            // Fallback: Source datagen/images folder - navigate from API project
            Path.Combine(_environment.ContentRootPath, "..", "..", "datagen", "images")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            _logger.LogDebug("Checking for images at: {Path}", fullPath);

            if (Directory.Exists(fullPath))
            {
                var hasImages = Directory.GetFiles(fullPath, "*.png").Length > 0
                                || Directory.GetFiles(fullPath, "*.jpg").Length > 0;
                if (hasImages)
                {
                    _logger.LogInformation("Found images at: {Path}", fullPath);
                    return fullPath;
                }
            }
        }

        _logger.LogWarning("Could not find images directory in any expected location. Tried: {Paths}",
            string.Join(", ", possiblePaths.Select(Path.GetFullPath)));
        return null;
    }

    private static async Task<long> GetFileSizeAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<LoadSyntheticDataResult> LoadDataInternalAsync(
        SyntheticDataSet data,
        Guid currentUserId,
        string dataPath,
        Func<LoadProgressUpdate, Task> progressCallback,
        CancellationToken cancellationToken)
    {
        const int totalSteps = 16;
        var homesLoaded = 0;
        var bedsLoaded = 0;
        var usersLoaded = 0;
        var clientsLoaded = 0;
        var careLogsLoaded = 0;
        var activitiesLoaded = 0;
        var incidentsLoaded = 0;

        // Map old IDs to new IDs for foreign key relationships
        var homeIdMap = new Dictionary<string, Guid>();
        var bedIdMap = new Dictionary<string, Guid>();
        var userIdMap = new Dictionary<string, Guid>();
        var clientIdMap = new Dictionary<string, Guid>();
        var activityIdMap = new Dictionary<string, Guid>();

        // 1. Load users FIRST (so we can reference them in homes, assignments, etc.)
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Users",
            Message = $"Loading {data.Users?.Count ?? 0} users...",
            CurrentStep = 3,
            TotalSteps = totalSteps
        });
        foreach (var userData in data.Users ?? [])
        {
            // Skip if user already exists
            var existingUser = await _userManager.FindByEmailAsync(userData.Email);
            if (existingUser is not null)
            {
                userIdMap[userData.Id] = existingUser.Id;
                continue;
            }

            var newId = Guid.NewGuid();
            userIdMap[userData.Id] = newId;

            var user = new ApplicationUser
            {
                Id = newId,
                UserName = userData.Email,
                Email = userData.Email,
                FirstName = userData.FirstName,
                LastName = userData.LastName,
                IsActive = userData.IsActive,
                IsMfaSetupComplete = true,
                InvitationAccepted = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "Synthetic123!");
            if (result.Succeeded)
            {
                // Assign roles
                foreach (var role in userData.Roles ?? [])
                    if (Roles.All.Contains(role))
                        await _userManager.AddToRoleAsync(user, role);

                usersLoaded++;
            }
        }

        // 2. Load homes (now we can reference user IDs for createdById)
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Homes",
            Message = $"Loading {data.Homes?.Count ?? 0} homes...",
            CurrentStep = 4,
            TotalSteps = totalSteps,
            ItemsProcessed = usersLoaded
        });
        foreach (var homeData in data.Homes ?? [])
        {
            var newId = Guid.NewGuid();
            homeIdMap[homeData.Id] = newId;

            // Use createdById from data if available, otherwise fall back to current user
            var createdById = currentUserId;
            if (!string.IsNullOrEmpty(homeData.CreatedById) &&
                userIdMap.TryGetValue(homeData.CreatedById, out var mappedCreatedById)) createdById = mappedCreatedById;

            var home = new Home
            {
                Id = newId,
                Name = homeData.Name,
                Address = homeData.Address,
                City = homeData.City,
                State = homeData.State,
                ZipCode = homeData.ZipCode,
                PhoneNumber = homeData.PhoneNumber,
                Capacity = homeData.Capacity,
                IsActive = homeData.IsActive,
                CreatedAt = ParseDateTime(homeData.CreatedAt),
                CreatedById = createdById
            };
            _context.Homes.Add(home);
            homesLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Load beds
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Beds",
            Message = $"Loading {data.Beds?.Count ?? 0} beds...",
            CurrentStep = 5,
            TotalSteps = totalSteps,
            ItemsProcessed = usersLoaded + homesLoaded
        });
        foreach (var bedData in data.Beds ?? [])
        {
            if (!homeIdMap.TryGetValue(bedData.HomeId, out var homeId))
                continue;

            var newId = Guid.NewGuid();
            bedIdMap[bedData.Id] = newId;

            var bed = new Bed
            {
                Id = newId,
                HomeId = homeId,
                Label = bedData.Label,
                Status = Enum.TryParse<BedStatus>(bedData.Status, out var status) ? status : BedStatus.Available,
                IsActive = bedData.IsActive,
                CreatedAt = ParseDateTime(bedData.CreatedAt)
            };
            _context.Beds.Add(bed);
            bedsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Load caregiver home assignments
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Assignments",
            Message = "Assigning caregivers to homes...",
            CurrentStep = 6,
            TotalSteps = totalSteps,
            ItemsProcessed = homesLoaded + bedsLoaded + usersLoaded
        });
        foreach (var assignment in data.CaregiverHomeAssignments ?? [])
        {
            if (!userIdMap.TryGetValue(assignment.UserId, out var userId))
                continue;
            if (!homeIdMap.TryGetValue(assignment.HomeId, out var homeId))
                continue;

            // Check if assignment already exists
            var exists = await _context.CaregiverHomeAssignments
                .AnyAsync(a => a.UserId == userId && a.HomeId == homeId, cancellationToken);
            if (exists)
                continue;

            // Resolve assignedById - use from data or fall back to current user
            var assignedById = currentUserId;
            if (!string.IsNullOrEmpty(assignment.AssignedById) &&
                userIdMap.TryGetValue(assignment.AssignedById, out var mappedAssignedById))
                assignedById = mappedAssignedById;

            var entity = new CaregiverHomeAssignment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HomeId = homeId,
                AssignedAt = ParseDateTime(assignment.AssignedAt),
                AssignedById = assignedById,
                IsActive = assignment.IsActive
            };
            _context.CaregiverHomeAssignments.Add(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Load clients
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Clients",
            Message = $"Loading {data.Clients?.Count ?? 0} clients (residents)...",
            CurrentStep = 7,
            TotalSteps = totalSteps,
            ItemsProcessed = homesLoaded + bedsLoaded + usersLoaded
        });
        foreach (var clientData in data.Clients ?? [])
        {
            if (!homeIdMap.TryGetValue(clientData.HomeId, out var homeId))
                continue;

            var newId = Guid.NewGuid();
            clientIdMap[clientData.Id] = newId;

            Guid? bedId = null;
            if (!string.IsNullOrEmpty(clientData.BedId) && bedIdMap.TryGetValue(clientData.BedId, out var mappedBedId))
                bedId = mappedBedId;

            var client = new Client
            {
                Id = newId,
                FirstName = clientData.FirstName,
                LastName = clientData.LastName,
                DateOfBirth = ParseDateOnly(clientData.DateOfBirth),
                Gender = clientData.Gender,
                AdmissionDate = ParseDateOnly(clientData.AdmissionDate),
                DischargeDate = string.IsNullOrEmpty(clientData.DischargeDate)
                    ? null
                    : ParseDateOnly(clientData.DischargeDate),
                DischargeReason = clientData.DischargeReason,
                HomeId = homeId,
                BedId = clientData.IsActive ? bedId : null, // Discharged clients should not be assigned to beds
                PrimaryPhysician = clientData.PrimaryPhysician,
                PrimaryPhysicianPhone = clientData.PrimaryPhysicianPhone,
                EmergencyContactName = clientData.EmergencyContactName,
                EmergencyContactPhone = clientData.EmergencyContactPhone,
                EmergencyContactRelationship = clientData.EmergencyContactRelationship,
                Allergies = clientData.Allergies,
                Diagnoses = clientData.Diagnoses,
                MedicationList = clientData.MedicationList,
                IsActive = clientData.IsActive,
                CreatedAt = ParseDateTime(clientData.CreatedAt),
                CreatedById = userIdMap.GetValueOrDefault(clientData.CreatedById, currentUserId)
            };
            _context.Clients.Add(client);

            // Update bed status if occupied (only if client is active and bed isn't already occupied)
            if (bedId.HasValue && clientData.IsActive)
            {
                var bed = await _context.Beds.FindAsync([bedId.Value], cancellationToken);
                if (bed is not null && bed.Status != BedStatus.Occupied) 
                    bed.Status = BedStatus.Occupied;
            }

            clientsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Validate and fix: ensure no home has more active clients than capacity
        _logger.LogInformation("Validating client capacity constraints...");
        var homesWithClients = await _context.Homes
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .ToListAsync(cancellationToken);

        foreach (var home in homesWithClients)
        {
            var activeClients = home.Clients.Where(c => c.IsActive).ToList();
            var activeBeds = home.Beds.Where(b => b.IsActive).ToList();

            if (activeClients.Count > home.Capacity)
            {
                _logger.LogWarning(
                    "Home {HomeName} has {ActiveCount} active clients but capacity is {Capacity}. Discharging excess.",
                    home.Name, activeClients.Count, home.Capacity);

                var excess = activeClients.Count - home.Capacity;
                foreach (var client in activeClients.Take(excess))
                {
                    client.IsActive = false;
                    client.DischargeDate = DateTime.UtcNow;
                    client.DischargeReason = "Capacity adjustment";
                    client.BedId = null;
                }
            }

            // Also validate bed count vs active clients
            if (activeClients.Count > activeBeds.Count)
            {
                _logger.LogWarning(
                    "Home {HomeName} has {ActiveCount} active clients but only {BedCount} beds. Discharging excess.",
                    home.Name, activeClients.Count, activeBeds.Count);

                var excess = activeClients.Count - activeBeds.Count;
                foreach (var client in activeClients.Take(excess))
                {
                    if (client.IsActive) // May have been handled above
                    {
                        client.IsActive = false;
                        client.DischargeDate = DateTime.UtcNow;
                        client.DischargeReason = "Bed availability adjustment";
                        client.BedId = null;
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 6. Load ADL logs
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "ADL Logs",
            Message = $"Loading {data.AdlLogs?.Count ?? 0:N0} ADL logs...",
            CurrentStep = 8,
            TotalSteps = totalSteps,
            ItemsProcessed = homesLoaded + bedsLoaded + usersLoaded + clientsLoaded
        });
        foreach (var log in data.AdlLogs ?? [])
        {
            if (!clientIdMap.TryGetValue(log.ClientId, out var clientId))
                continue;

            var entity = new ADLLog
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CaregiverId = userIdMap.GetValueOrDefault(log.CaregiverId, currentUserId),
                Timestamp = ParseDateTime(log.Timestamp),
                Bathing = ParseADLLevel(log.Bathing),
                Dressing = ParseADLLevel(log.Dressing),
                Toileting = ParseADLLevel(log.Toileting),
                Transferring = ParseADLLevel(log.Transferring),
                Continence = ParseADLLevel(log.Continence),
                Feeding = ParseADLLevel(log.Feeding),
                Notes = log.Notes,
                CreatedAt = ParseDateTime(log.CreatedAt)
            };
            _context.ADLLogs.Add(entity);
            careLogsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 7. Load vitals logs
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Vitals Logs",
            Message = $"Loading {data.VitalsLogs?.Count ?? 0:N0} vitals logs...",
            CurrentStep = 9,
            TotalSteps = totalSteps,
            ItemsProcessed = careLogsLoaded
        });
        foreach (var log in data.VitalsLogs ?? [])
        {
            if (!clientIdMap.TryGetValue(log.ClientId, out var clientId))
                continue;

            var entity = new VitalsLog
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CaregiverId = userIdMap.GetValueOrDefault(log.CaregiverId, currentUserId),
                Timestamp = ParseDateTime(log.Timestamp),
                SystolicBP = log.SystolicBP,
                DiastolicBP = log.DiastolicBP,
                Pulse = log.Pulse,
                Temperature = log.Temperature,
                TemperatureUnit = Enum.TryParse<TemperatureUnit>(log.TemperatureUnit, out var unit)
                    ? unit
                    : TemperatureUnit.Fahrenheit,
                OxygenSaturation = log.OxygenSaturation,
                Notes = log.Notes,
                CreatedAt = ParseDateTime(log.CreatedAt)
            };
            _context.VitalsLogs.Add(entity);
            careLogsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 8. Load medication logs
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Medication Logs",
            Message = $"Loading {data.MedicationLogs?.Count ?? 0:N0} medication logs...",
            CurrentStep = 10,
            TotalSteps = totalSteps,
            ItemsProcessed = careLogsLoaded
        });
        foreach (var log in data.MedicationLogs ?? [])
        {
            if (!clientIdMap.TryGetValue(log.ClientId, out var clientId))
                continue;

            var entity = new MedicationLog
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CaregiverId = userIdMap.GetValueOrDefault(log.CaregiverId, currentUserId),
                Timestamp = ParseDateTime(log.Timestamp),
                MedicationName = log.MedicationName,
                Dosage = log.Dosage,
                Route = Enum.TryParse<MedicationRoute>(log.Route, out var route) ? route : MedicationRoute.Oral,
                Status = Enum.TryParse<MedicationStatus>(log.Status, out var status)
                    ? status
                    : MedicationStatus.Administered,
                ScheduledTime = ParseNullableDateTime(log.ScheduledTime),
                Notes = log.Notes,
                CreatedAt = ParseDateTime(log.CreatedAt)
            };
            _context.MedicationLogs.Add(entity);
            careLogsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 9. Load ROM logs
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "ROM Logs",
            Message = $"Loading {data.RomLogs?.Count ?? 0:N0} ROM exercise logs...",
            CurrentStep = 11,
            TotalSteps = totalSteps,
            ItemsProcessed = careLogsLoaded
        });
        foreach (var log in data.RomLogs ?? [])
        {
            if (!clientIdMap.TryGetValue(log.ClientId, out var clientId))
                continue;

            var entity = new ROMLog
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CaregiverId = userIdMap.GetValueOrDefault(log.CaregiverId, currentUserId),
                Timestamp = ParseDateTime(log.Timestamp),
                ActivityDescription = log.ActivityDescription,
                Duration = log.Duration,
                Repetitions = log.Repetitions,
                Notes = log.Notes,
                CreatedAt = ParseDateTime(log.CreatedAt)
            };
            _context.ROMLogs.Add(entity);
            careLogsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 10. Load behavior notes
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Behavior Notes",
            Message = $"Loading {data.BehaviorNotes?.Count ?? 0:N0} behavior notes...",
            CurrentStep = 12,
            TotalSteps = totalSteps,
            ItemsProcessed = careLogsLoaded
        });
        foreach (var note in data.BehaviorNotes ?? [])
        {
            if (!clientIdMap.TryGetValue(note.ClientId, out var clientId))
                continue;

            var entity = new BehaviorNote
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                CaregiverId = userIdMap.GetValueOrDefault(note.CaregiverId, currentUserId),
                Timestamp = ParseDateTime(note.Timestamp),
                Category = Enum.TryParse<BehaviorCategory>(note.Category, out var cat) ? cat : BehaviorCategory.General,
                NoteText = note.NoteText,
                Severity = Enum.TryParse<NoteSeverity>(note.Severity, out var sev) ? sev : NoteSeverity.Low,
                CreatedAt = ParseDateTime(note.CreatedAt)
            };
            _context.BehaviorNotes.Add(entity);
            careLogsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 11. Load activities
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Activities",
            Message = $"Loading {data.Activities?.Count ?? 0} activities...",
            CurrentStep = 13,
            TotalSteps = totalSteps,
            ItemsProcessed = careLogsLoaded
        });
        foreach (var activity in data.Activities ?? [])
        {
            if (!homeIdMap.TryGetValue(activity.HomeId, out var homeId))
                continue;

            var newId = Guid.NewGuid();
            activityIdMap[activity.Id] = newId;

            var entity = new ActivityEntity
            {
                Id = newId,
                HomeId = homeId,
                ActivityName = activity.ActivityName,
                Description = activity.Description,
                Date = ParseDateOnly(activity.Date),
                StartTime = ParseTimeSpan(activity.StartTime),
                EndTime = ParseTimeSpan(activity.EndTime),
                Duration = activity.Duration,
                Category = Enum.TryParse<ActivityCategory>(activity.Category, out var cat)
                    ? cat
                    : ActivityCategory.Other,
                IsGroupActivity = activity.IsGroupActivity,
                CreatedById = userIdMap.GetValueOrDefault(activity.CreatedById, currentUserId),
                CreatedAt = ParseDateTime(activity.CreatedAt),
                UpdatedAt = ParseNullableDateTime(activity.UpdatedAt)
            };
            _context.Activities.Add(entity);
            activitiesLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 12. Load activity participants
        foreach (var participant in data.ActivityParticipants ?? [])
        {
            if (!activityIdMap.TryGetValue(participant.ActivityId, out var activityId))
                continue;
            if (!clientIdMap.TryGetValue(participant.ClientId, out var clientId))
                continue;

            var entity = new ActivityParticipant
            {
                Id = Guid.NewGuid(),
                ActivityId = activityId,
                ClientId = clientId,
                CreatedAt = ParseDateTime(participant.CreatedAt)
            };
            _context.ActivityParticipants.Add(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 13. Load incidents with photos
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Incidents",
            Message = $"Loading {data.Incidents?.Count ?? 0} incidents with photos...",
            CurrentStep = 14,
            TotalSteps = totalSteps,
            ItemsProcessed = activitiesLoaded
        });

        // Find images directory for incident photos
        var imagesPath = FindImagesPath();
        var incidentIdMap = new Dictionary<string, Guid>();
        var photosLoaded = 0;

        foreach (var incident in data.Incidents ?? [])
        {
            if (!homeIdMap.TryGetValue(incident.HomeId, out var homeId))
                continue;

            Guid? clientId = null;
            if (!string.IsNullOrEmpty(incident.ClientId) &&
                clientIdMap.TryGetValue(incident.ClientId, out var mappedClientId)) clientId = mappedClientId;

            var newIncidentId = Guid.NewGuid();
            incidentIdMap[incident.Id] = newIncidentId;

            // Resolve closedById if incident is closed
            Guid? closedById = null;
            if (!string.IsNullOrEmpty(incident.ClosedById) &&
                userIdMap.TryGetValue(incident.ClosedById, out var mappedClosedById)) closedById = mappedClosedById;

            var entity = new Incident
            {
                Id = newIncidentId,
                IncidentNumber = incident.IncidentNumber,
                ClientId = clientId,
                HomeId = homeId,
                IncidentType = Enum.TryParse<IncidentType>(incident.IncidentType, out var incType)
                    ? incType
                    : IncidentType.Other,
                Severity = incident.Severity,
                Status = Enum.TryParse<IncidentStatus>(incident.Status, out var incStatus)
                    ? incStatus
                    : IncidentStatus.Submitted,
                OccurredAt = ParseDateTime(incident.OccurredAt),
                Location = incident.Location,
                Description = incident.Description,
                ActionsTaken = incident.ActionsTaken,
                ReportedById = userIdMap.GetValueOrDefault(incident.ReportedById, currentUserId),
                ClosedById = closedById,
                ClosedAt = string.IsNullOrEmpty(incident.ClosedAt) ? null : ParseDateTime(incident.ClosedAt),
                ClosureNotes = incident.ClosureNotes,
                CreatedAt = ParseDateTime(incident.CreatedAt),
                UpdatedAt = ParseNullableDateTime(incident.UpdatedAt)
            };
            _context.Incidents.Add(entity);
            incidentsLoaded++;

            // Load incident photos
            if (incident.Photos is { Count: > 0 } && imagesPath is not null)
                foreach (var photoData in incident.Photos)
                {
                    // Determine source file - check _sourceFile first, then fileName
                    var sourceFileName = photoData.SourceFile ?? photoData.FileName;
                    var sourcePath = Path.Combine(imagesPath, sourceFileName);

                    if (!File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Incident photo source file not found: {Path}", sourcePath);
                        continue;
                    }

                    var photoId = Guid.NewGuid();
                    // Path within incident-photos container: {homeId}/{incidentId}/{photoId}.png
                    var blobPath = $"{homeId}/{newIncidentId}/{photoId}.png";

                    // Upload photo to blob storage (incident-photos container)
                    try
                    {
                        var photoBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
                        await _blobStorageService.UploadBlobAsync(blobPath, photoBytes, photoData.ContentType,
                            BlobContainers.IncidentPhotos);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to upload incident photo {FileName} to blob storage",
                            sourceFileName);
                        continue;
                    }

                    // Create photo record in SQL
                    var photoEntity = new IncidentPhoto
                    {
                        Id = photoId,
                        IncidentId = newIncidentId,
                        BlobPath = blobPath,
                        FileName = photoData.FileName,
                        ContentType = photoData.ContentType,
                        FileSizeBytes = photoData.FileSizeBytes > 0
                            ? photoData.FileSizeBytes
                            : await GetFileSizeAsync(sourcePath),
                        DisplayOrder = photoData.DisplayOrder,
                        Caption = photoData.Caption,
                        CreatedAt = ParseDateTime(photoData.CreatedAt),
                        CreatedById = userIdMap.GetValueOrDefault(photoData.CreatedById, currentUserId)
                    };
                    _context.IncidentPhotos.Add(photoEntity);
                    photosLoaded++;
                }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Loaded {IncidentCount} incidents with {PhotoCount} photos", incidentsLoaded,
            photosLoaded);

        // Report incident photos progress
        if (imagesPath is null) _logger.LogWarning("Images path not found - incident photos were not uploaded");

        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Incident Photos",
            Message = imagesPath is not null
                ? $"Uploaded {photosLoaded} incident photos to blob storage"
                : "Skipped incident photos (images directory not found)",
            CurrentStep = 14,
            TotalSteps = totalSteps,
            ItemsProcessed = photosLoaded
        });

        // 14. Load documents (PDFs to blob storage + metadata to SQL)
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Documents",
            Message = $"Uploading {data.Documents?.Count ?? 0} PDF documents to blob storage...",
            CurrentStep = 15,
            TotalSteps = totalSteps,
            ItemsProcessed = incidentsLoaded
        });
        var documentsLoaded = 0;
        var documentsPath = Path.Combine(dataPath ?? "", "documents");

        foreach (var docData in data.Documents ?? [])
        {
            if (!clientIdMap.TryGetValue(docData.ClientId, out var clientId))
                continue;

            // Find the PDF file
            var pdfPath = Path.Combine(documentsPath, docData.FileName);
            if (!File.Exists(pdfPath))
            {
                _logger.LogWarning("Document PDF not found: {Path}", pdfPath);
                continue;
            }

            var documentId = Guid.NewGuid();
            var blobPath = $"clients/{clientId}/{docData.FileName}";

            // Upload PDF to blob storage
            try
            {
                var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
                await _blobStorageService.UploadBlobAsync(blobPath, pdfBytes, docData.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload document {FileName} to blob storage", docData.FileName);
                continue;
            }

            // Create document record in SQL
            var documentEntity = new Document
            {
                Id = documentId,
                ClientId = clientId,
                FileName = docData.FileName,
                OriginalFileName = docData.OriginalFileName,
                BlobPath = blobPath,
                ContentType = docData.ContentType,
                DocumentType = Enum.TryParse<DocumentType>(docData.DocumentType, out var docType)
                    ? docType
                    : DocumentType.Other,
                Description = docData.Description,
                FileSizeBytes = docData.FileSizeBytes,
                UploadedById = currentUserId,
                UploadedAt = ParseDateTime(docData.CreatedAt),
                IsActive = true,
                CreatedAt = ParseDateTime(docData.CreatedAt)
            };
            _context.Documents.Add(documentEntity);
            documentsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 15. Load appointments
        await progressCallback(new LoadProgressUpdate
        {
            Phase = "Appointments",
            Message = $"Loading {data.Appointments?.Count ?? 0} appointments...",
            CurrentStep = 16,
            TotalSteps = totalSteps,
            ItemsProcessed = documentsLoaded
        });
        var appointmentsLoaded = 0;

        foreach (var appt in data.Appointments ?? [])
        {
            if (!clientIdMap.TryGetValue(appt.ClientId, out var clientId))
                continue;
            if (!homeIdMap.TryGetValue(appt.HomeId, out var homeId))
                continue;

            Guid? completedById = null;
            if (!string.IsNullOrEmpty(appt.CompletedById) &&
                userIdMap.TryGetValue(appt.CompletedById, out var mappedCompletedById))
                completedById = mappedCompletedById;

            var entity = new Appointment
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                HomeId = homeId,
                AppointmentType = Enum.TryParse<AppointmentType>(appt.AppointmentType, out var apptType)
                    ? apptType
                    : AppointmentType.GeneralPractice,
                Status = Enum.TryParse<AppointmentStatus>(appt.Status, out var apptStatus)
                    ? apptStatus
                    : AppointmentStatus.Scheduled,
                Title = appt.Title,
                ScheduledAt = ParseDateTime(appt.ScheduledAt),
                DurationMinutes = appt.DurationMinutes,
                Location = appt.Location,
                ProviderName = appt.ProviderName,
                ProviderPhone = appt.ProviderPhone,
                Notes = appt.Notes,
                TransportationNotes = appt.TransportationNotes,
                ReminderSent = appt.ReminderSent,
                CreatedById = userIdMap.GetValueOrDefault(appt.CreatedById, currentUserId),
                CreatedAt = ParseDateTime(appt.CreatedAt),
                OutcomeNotes = appt.OutcomeNotes,
                CompletedById = completedById,
                CompletedAt = ParseNullableDateTime(appt.CompletedAt)
            };
            _context.Appointments.Add(entity);
            appointmentsLoaded++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Loaded {AppointmentCount} appointments", appointmentsLoaded);

        return new LoadSyntheticDataResult
        {
            Success = true,
            HomesLoaded = homesLoaded,
            BedsLoaded = bedsLoaded,
            UsersLoaded = usersLoaded,
            ClientsLoaded = clientsLoaded,
            CareLogsLoaded = careLogsLoaded,
            ActivitiesLoaded = activitiesLoaded,
            IncidentsLoaded = incidentsLoaded,
            DocumentsLoaded = documentsLoaded,
            AppointmentsLoaded = appointmentsLoaded
        };
    }

    private static DateTime ParseDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DateTime.UtcNow;

        return DateTime.TryParse(value, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : DateTime.UtcNow;
    }

    private static DateTime? ParseNullableDateTime(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return DateTime.TryParse(value, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : null;
    }

    private static DateTime ParseDateOnly(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DateTime.UtcNow.Date;

        if (DateOnly.TryParse(value, out var date))
            return date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return DateTime.TryParse(value, out var dt)
            ? DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc)
            : DateTime.UtcNow.Date;
    }

    private static TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (TimeOnly.TryParse(value, out var time))
            return time.ToTimeSpan();

        return TimeSpan.TryParse(value, out var ts) ? ts : null;
    }

    private static ADLLevel? ParseADLLevel(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Enum.TryParse<ADLLevel>(value, out var level) ? level : null;
    }
}

#region Data Models for JSON Parsing

internal sealed class SyntheticDataSet
{
    public List<HomeData>? Homes { get; set; }
    public List<BedData>? Beds { get; set; }
    public List<UserData>? Users { get; set; }
    public List<CaregiverHomeAssignmentData>? CaregiverHomeAssignments { get; set; }
    public List<ClientData>? Clients { get; set; }
    public List<AdlLogData>? AdlLogs { get; set; }
    public List<VitalsLogData>? VitalsLogs { get; set; }
    public List<MedicationLogData>? MedicationLogs { get; set; }
    public List<RomLogData>? RomLogs { get; set; }
    public List<BehaviorNoteData>? BehaviorNotes { get; set; }
    public List<ActivityData>? Activities { get; set; }
    public List<ActivityParticipantData>? ActivityParticipants { get; set; }
    public List<IncidentData>? Incidents { get; set; }
    public List<DocumentData>? Documents { get; set; }
    public List<AppointmentData>? Appointments { get; set; }
}

internal sealed class HomeData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedAt { get; set; }
    public string? CreatedById { get; set; }
}

internal sealed class BedData
{
    public string Id { get; set; } = "";
    public string HomeId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class UserData
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsActive { get; set; }
    public List<string>? Roles { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class CaregiverHomeAssignmentData
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string HomeId { get; set; } = "";
    public string? AssignedAt { get; set; }
    public string? AssignedById { get; set; }
    public bool IsActive { get; set; }
}

internal sealed class ClientData
{
    public string Id { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string? AdmissionDate { get; set; }
    public string? DischargeDate { get; set; }
    public string? DischargeReason { get; set; }
    public string HomeId { get; set; } = "";
    public string? BedId { get; set; }
    public string? PrimaryPhysician { get; set; }
    public string? PrimaryPhysicianPhone { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Allergies { get; set; }
    public string? Diagnoses { get; set; }
    public string? MedicationList { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedAt { get; set; }
    public string CreatedById { get; set; } = "";
}

internal sealed class AdlLogData
{
    public string ClientId { get; set; } = "";
    public string CaregiverId { get; set; } = "";
    public string? Timestamp { get; set; }
    public string? Bathing { get; set; }
    public string? Dressing { get; set; }
    public string? Toileting { get; set; }
    public string? Transferring { get; set; }
    public string? Continence { get; set; }
    public string? Feeding { get; set; }
    public string? Notes { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class VitalsLogData
{
    public string ClientId { get; set; } = "";
    public string CaregiverId { get; set; } = "";
    public string? Timestamp { get; set; }
    public int? SystolicBP { get; set; }
    public int? DiastolicBP { get; set; }
    public int? Pulse { get; set; }
    public decimal? Temperature { get; set; }
    public string TemperatureUnit { get; set; } = "Fahrenheit";
    public int? OxygenSaturation { get; set; }
    public string? Notes { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class MedicationLogData
{
    public string ClientId { get; set; } = "";
    public string CaregiverId { get; set; } = "";
    public string? Timestamp { get; set; }
    public string MedicationName { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Route { get; set; } = "Oral";
    public string Status { get; set; } = "Administered";
    public string? ScheduledTime { get; set; }
    public string? Notes { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class RomLogData
{
    public string ClientId { get; set; } = "";
    public string CaregiverId { get; set; } = "";
    public string? Timestamp { get; set; }
    public string ActivityDescription { get; set; } = "";
    public int? Duration { get; set; }
    public int? Repetitions { get; set; }
    public string? Notes { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class BehaviorNoteData
{
    public string ClientId { get; set; } = "";
    public string CaregiverId { get; set; } = "";
    public string? Timestamp { get; set; }
    public string Category { get; set; } = "General";
    public string NoteText { get; set; } = "";
    public string Severity { get; set; } = "Low";
    public string? CreatedAt { get; set; }
}

internal sealed class ActivityData
{
    public string Id { get; set; } = "";
    public string HomeId { get; set; } = "";
    public string ActivityName { get; set; } = "";
    public string? Description { get; set; }
    public string? Date { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? Duration { get; set; }
    public string Category { get; set; } = "Other";
    public bool IsGroupActivity { get; set; }
    public string CreatedById { get; set; } = "";
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

internal sealed class ActivityParticipantData
{
    public string ActivityId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? CreatedAt { get; set; }
}

internal sealed class IncidentData
{
    public string Id { get; set; } = "";
    public string IncidentNumber { get; set; } = "";
    public string? ClientId { get; set; }
    public string HomeId { get; set; } = "";
    public string IncidentType { get; set; } = "Other";
    public int Severity { get; set; }
    public string Status { get; set; } = "Submitted";
    public string? OccurredAt { get; set; }
    public string Location { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ActionsTaken { get; set; }
    public string ReportedById { get; set; } = "";
    public string? ClosedById { get; set; }
    public string? ClosedAt { get; set; }
    public string? ClosureNotes { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public List<IncidentPhotoData>? Photos { get; set; }
}

internal sealed class IncidentPhotoData
{
    public string Id { get; set; } = "";
    public string IncidentId { get; set; } = "";
    public string BlobPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "image/png";
    public long FileSizeBytes { get; set; }
    public int DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public string? CreatedAt { get; set; }
    public string CreatedById { get; set; } = "";

    /// <summary>
    ///     Internal reference to the source image file in the images directory.
    /// </summary>
    [JsonPropertyName("_sourceFile")]
    public string? SourceFile { get; set; }
}

internal sealed class DocumentData
{
    public string Id { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public string DocumentType { get; set; } = "Other";
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class AppointmentData
{
    public string Id { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string HomeId { get; set; } = "";
    public string AppointmentType { get; set; } = "GeneralPractice";
    public string Status { get; set; } = "Scheduled";
    public string Title { get; set; } = "";
    public string? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Location { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderPhone { get; set; }
    public string? Notes { get; set; }
    public string? TransportationNotes { get; set; }
    public bool ReminderSent { get; set; }
    public string CreatedById { get; set; } = "";
    public string? CreatedAt { get; set; }
    public string? OutcomeNotes { get; set; }
    public string? CompletedById { get; set; }
    public string? CompletedAt { get; set; }
}

#endregion