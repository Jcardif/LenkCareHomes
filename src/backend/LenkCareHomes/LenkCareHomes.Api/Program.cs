using LenkCareHomes.Api.Authorization;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Middleware;
using LenkCareHomes.Api.Services.Appointments;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Auth;
using LenkCareHomes.Api.Services.Beds;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.CareLog;
using LenkCareHomes.Api.Services.Clients;
using LenkCareHomes.Api.Services.Dashboard;
using LenkCareHomes.Api.Services.Documents;
using LenkCareHomes.Api.Services.Email;
using LenkCareHomes.Api.Services.Homes;
using LenkCareHomes.Api.Services.Incidents;
using LenkCareHomes.Api.Services.Passkey;
using LenkCareHomes.Api.Services.Reports;
using LenkCareHomes.Api.Services.SyntheticData;
using LenkCareHomes.Api.Services.Users;
using Azure.Identity;
using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Warning(ex, "Failed to connect to Azure Key Vault at {KeyVaultUri}. Continuing without Key Vault in development.", keyVaultUri);
        }
        else
        {
            Log.Fatal(ex, "Failed to connect to Azure Key Vault at {KeyVaultUri}. Application cannot start.", keyVaultUri);
            throw new InvalidOperationException($"Failed to connect to Azure Key Vault at {keyVaultUri}.", ex);
        }
    }
}
else if (!builder.Environment.IsDevelopment())
{
    Log.Fatal("KeyVault:Uri is not configured. Application cannot start in production without Key Vault.");
    throw new InvalidOperationException("KeyVault:Uri configuration is required in non-development environments.");
}
else
{
    Log.Warning("KeyVault:Uri is not configured. Running in development without Key Vault.");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.AddServiceDefaults();

// Add configuration settings
builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection(CosmosDbSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));

// Configure and validate FIDO2 settings - fail fast if missing
var fido2Section = builder.Configuration.GetSection("Fido2");
builder.Services.Configure<Fido2Settings>(fido2Section);

// Validate FIDO2 configuration at startup
var fido2ServerDomain = fido2Section["ServerDomain"];
var fido2ServerName = fido2Section["ServerName"];
var fido2Origins = fido2Section.GetSection("Origins").Get<HashSet<string>>();

if (string.IsNullOrWhiteSpace(fido2ServerDomain))
{
    throw new InvalidOperationException(
        "FIDO2 configuration error: 'Fido2:ServerDomain' is required. " +
        "For local development, configure in appsettings.Development.json. " +
        "For Azure deployments, ensure 'Fido2--ServerDomain' secret exists in Key Vault.");
}

if (string.IsNullOrWhiteSpace(fido2ServerName))
{
    throw new InvalidOperationException(
        "FIDO2 configuration error: 'Fido2:ServerName' is required. " +
        "For local development, configure in appsettings.Development.json. " +
        "For Azure deployments, ensure 'Fido2--ServerName' secret exists in Key Vault.");
}

if (fido2Origins is null || fido2Origins.Count == 0)
{
    throw new InvalidOperationException(
        "FIDO2 configuration error: 'Fido2:Origins' is required and must contain at least one origin. " +
        "For local development, configure in appsettings.Development.json. " +
        "For Azure deployments, ensure 'Fido2--Origins' secret exists in Key Vault.");
}

// Configure FIDO2/WebAuthn for passkey authentication
builder.Services.AddSingleton<IFido2>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<Fido2Settings>>().Value;
    var config = new Fido2Configuration
    {
        ServerDomain = settings.ServerDomain,
        ServerName = settings.ServerName,
        Origins = settings.Origins
    };
    return new Fido2(config);
});

// Configure BlobStorageSettings with Aspire connection string support
// Aspire injects blob storage connection as ConnectionStrings:blobs
builder.Services.Configure<BlobStorageSettings>(options =>
{
    builder.Configuration.GetSection(BlobStorageSettings.SectionName).Bind(options);
    
    // Prefer Aspire-injected connection string (ConnectionStrings:blobs) over BlobStorage:ConnectionString
    var aspireConnectionString = builder.Configuration.GetConnectionString("blobs");
    if (!string.IsNullOrWhiteSpace(aspireConnectionString))
    {
        options.ConnectionString = aspireConnectionString;
    }
});

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlDatabase")));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password policy: 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special char
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout disabled per requirements
    options.Lockout.AllowedForNewUsers = false;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Handled via invitation flow
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // Use SameSite=None for cross-origin requests (frontend on different port)
    // This is required when frontend and backend are on different origins
    options.Cookie.SameSite = SameSiteMode.None;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/auth/login";
    options.AccessDeniedPath = "/api/auth/access-denied";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Add services
builder.Services.AddSingleton<IAuditLogService, CosmosAuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasskeyService, PasskeyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, AzureEmailService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IBedService, BedService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICaregiverService, CaregiverService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add care logging services
builder.Services.AddScoped<IADLLogService, ADLLogService>();
builder.Services.AddScoped<IVitalsLogService, VitalsLogService>();
builder.Services.AddScoped<IMedicationLogService, MedicationLogService>();
builder.Services.AddScoped<IROMLogService, ROMLogService>();
builder.Services.AddScoped<IBehaviorNoteService, BehaviorNoteService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();

// Add incident and document services
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIncidentNotificationService, IncidentNotificationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Add appointment service
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Add reporting services
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<IPdfReportService, PdfReportService>();

// Add synthetic data service
// Note: Service is always registered to avoid DI exceptions when controller is instantiated.
// The service's IsAvailable property returns false in non-development environments,
// and the controller has [DevelopmentOnly] attribute that returns 404 in production.
builder.Services.AddScoped<ISyntheticDataService, SyntheticDataService>();

// Add PHI access authorization
builder.Services.AddPhiAccessAuthorization();

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Add CORS for frontend
var frontendUrl = builder.Configuration["Auth:FrontendBaseUrl"];
if (string.IsNullOrWhiteSpace(frontendUrl))
{
    Log.Fatal("Auth:FrontendBaseUrl is not configured. Application cannot start without a valid frontend URL for CORS.");
    throw new InvalidOperationException("Auth:FrontendBaseUrl configuration is required.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Seed database (only in development - production uses SQL scripts for initial sysadmin)
if (app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

    // Ensure Cosmos DB database and container exist for audit logging
    // Use timeout to prevent hanging if emulator is slow to respond
    var auditService = app.Services.GetRequiredService<IAuditLogService>();
    if (auditService is CosmosAuditLogService cosmosAuditService)
    {
        try
        {
            app.Logger.LogInformation("Initializing Cosmos DB database and container...");
            using var cosmosCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await cosmosAuditService.EnsureCreatedAsync(cosmosCts.Token);
        }
        catch (OperationCanceledException)
        {
            app.Logger.LogWarning("Cosmos DB initialization timed out. Audit logging may fail until emulator is ready.");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to initialize Cosmos DB. Audit logging may not work correctly.");
        }
    }

    // Ensure Blob Storage container exists and configure CORS for development
    var blobSettings = app.Services.GetRequiredService<IOptions<BlobStorageSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(blobSettings.ConnectionString))
    {
        app.Logger.LogInformation("Checking blob storage container...");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(blobSettings.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(blobSettings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cts.Token);
            app.Logger.LogInformation("Blob storage container '{Container}' ensured", blobSettings.ContainerName);
            
            // Configure CORS for blob storage (required for direct browser uploads)
            // This allows the frontend to upload directly to blob storage using SAS URLs
            var corsRule = new Azure.Storage.Blobs.Models.BlobCorsRule
            {
                AllowedOrigins = frontendUrl,
                AllowedMethods = "GET,PUT,OPTIONS,HEAD",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600
            };
            
            var serviceProperties = await blobServiceClient.GetPropertiesAsync(cts.Token);
            serviceProperties.Value.Cors.Clear();
            serviceProperties.Value.Cors.Add(corsRule);
            await blobServiceClient.SetPropertiesAsync(serviceProperties.Value, cts.Token);
            app.Logger.LogInformation("Blob storage CORS configured for {FrontendUrl}", frontendUrl);
        }
        catch (OperationCanceledException)
        {
            app.Logger.LogWarning("Blob storage container check timed out. Will retry on first use.");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to ensure blob storage container exists. Will retry on first use.");
        }
    }
    else
    {
        app.Logger.LogWarning("BlobStorage:ConnectionString not configured, skipping container check");
    }

    app.Logger.LogInformation("Development initialization complete, starting HTTP server...");
}

app.MapDefaultEndpoints();
app.Logger.LogInformation("Mapped default endpoints");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.Logger.LogInformation("Mapped OpenAPI and Scalar endpoints");
}

// Security middleware - add before other middleware for comprehensive protection
app.UseSecurityHeaders();
app.UseRateLimiting();
app.Logger.LogInformation("Security middleware configured");

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.Logger.LogInformation("CORS configured for Frontend");

app.UseAuthentication();
app.UseAuthorization();

// Add audit logging middleware (after authentication so we have user info)
app.UseAuditLogging();

app.MapControllers();
app.Logger.LogInformation("Controllers mapped, starting Kestrel...");

app.Run();
