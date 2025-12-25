using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var apiServiceName = "lenkcare-api";
var frontendAppName = "lenkcare-frontend";

// Detect Windows ARM64 (Copilot+ PCs) - SQL Server Docker image is not supported
var isWindowsArm64 = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                     && RuntimeInformation.OSArchitecture == Architecture.Arm64;

// ============================================================================
// Docker Containers for Local Development
// ============================================================================

// SQL Server for PHI data storage
// On Windows ARM64: Use locally installed SQL Server (Docker image not supported)
// On other platforms: Use Docker container (mcr.microsoft.com/mssql/server:2025-latest)
IResourceBuilder<IResourceWithConnectionString> sqlDatabase;

if (isWindowsArm64)
{
    // Windows ARM64 (Copilot+ PCs): Connect to locally installed SQL Server
    // Uses Windows Authentication (Integrated Security) to localhost
    // The connection string "SqlDatabase" must be defined in appsettings.Development.json
    sqlDatabase = builder.AddConnectionString("SqlDatabase");
}
else
{
    // Other platforms: Use SQL Server Docker container
    var sqlPassword = builder.AddParameter("sql-password", true);
    var sqlServer = builder.AddSqlServer("lenkcare-sql", sqlPassword, 1433)
        .WithImageTag("2025-latest")
        .WithImagePullPolicy(ImagePullPolicy.Always)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("lenkcare-sql-data");

    sqlDatabase = sqlServer.AddDatabase("LenkCareHomes");
}

// Azurite (Azure Storage Emulator) for blob storage
// Image: mcr.microsoft.com/azure-storage/azurite:latest
// Ports: 10000 (blob), 10001 (queue), 10002 (table)
var storage = builder.AddAzureStorage("lenkcare-storage")
    .RunAsEmulator(emulator => emulator
        .WithImageTag("latest")
        .WithImagePullPolicy(ImagePullPolicy.Always)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("lenkcare-azurite-data"));

var blobStorage = storage.AddBlobs("blobs");

// Cosmos DB Emulator for audit logging
// Image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
// The vnext-preview version starts much faster than the legacy emulator
// Note: .NET SDK requires HTTPS mode (--protocol https)
// Ports: 8081 (gateway), 1234 (data explorer)
var cosmosDb = builder.AddContainer("lenkcare-cosmosdb", "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator",
        "vnext-preview")
    .WithImagePullPolicy(ImagePullPolicy.Always) // Ensure image is pulled if not present
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8081, 8081, "gateway")
    .WithHttpEndpoint(1234, 1234, "explorer")
    .WithArgs("--protocol", "https"); // Required for .NET SDK

// ============================================================================
// API Service
// ============================================================================
var apiService = builder.AddProject<LenkCareHomes_Api>(apiServiceName)
    .WithReference(sqlDatabase, "SqlDatabase") // Named reference for ConnectionStrings:SqlDatabase
    .WithReference(blobStorage) // Aspire injects ConnectionStrings:blobs with correct dynamic port
    .WaitFor(blobStorage)
    .WaitFor(cosmosDb)
    .WithEnvironment("CosmosDb__ConnectionString",
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
    .WithEnvironment("CosmosDb__DatabaseName", "LenkCareHomes")
    .WithEnvironment("CosmosDb__ContainerName", "AuditLogs")
    .WithEnvironment("BlobStorage__ContainerName", "documents");

// On non-ARM64 platforms, wait for Docker-based SQL Server to be ready
if (!isWindowsArm64) apiService.WaitFor(sqlDatabase);

// ============================================================================
// Frontend Application
// ============================================================================
// Using Aspire 13's AddJavaScriptApp with npm (auto-detected from package.json)
var frontendApp = builder.AddJavaScriptApp(frontendAppName, "../../../frontend")
    .WithNpm()
    .WithRunScript("dev")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpEndpoint(3000, 3000, isProxied: false)
    .WithEnvironment("NEXT_PUBLIC_SHOW_DEV_TOOLS", "true"); // Enable dev tools menu in development

// ============================================================================
// Data Generation Tool (Development Only)
// ============================================================================
// Python script for generating synthetic test data.
// 
// IMPORTANT: Run datagen ONCE before starting Aspire to generate the SyntheticData
// files. The API project copies these files to its output directory at build time
// (see .csproj CopyToOutputDirectory), so they must exist before the API builds.
//
// First-time setup:
//   cd src/datagen && pip install -r requirements.txt && python generate.py
//
// Or run from Aspire dashboard after initial setup when you need to regenerate data.
if (builder.Environment.IsDevelopment())
    builder.AddPythonApp("lenkcare-datagen", "../../../datagen", "generate.py")
        .WithPip()
        .ExcludeFromManifest(); // Don't include in production deployments

builder.Build().Run();