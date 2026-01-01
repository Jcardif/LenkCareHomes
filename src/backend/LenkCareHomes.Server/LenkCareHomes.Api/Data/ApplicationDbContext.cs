using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Data;

/// <summary>
///     Application database context for Azure SQL.
///     Contains all PHI-related data with TDE encryption.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    ///     Gets or sets the homes table.
    /// </summary>
    public DbSet<Home> Homes => Set<Home>();

    /// <summary>
    ///     Gets or sets the beds table.
    /// </summary>
    public DbSet<Bed> Beds => Set<Bed>();

    /// <summary>
    ///     Gets or sets the caregiver home assignments table.
    /// </summary>
    public DbSet<CaregiverHomeAssignment> CaregiverHomeAssignments => Set<CaregiverHomeAssignment>();

    /// <summary>
    ///     Gets or sets the clients table.
    /// </summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>
    ///     Gets or sets the ADL logs table.
    /// </summary>
    public DbSet<ADLLog> ADLLogs => Set<ADLLog>();

    /// <summary>
    ///     Gets or sets the vitals logs table.
    /// </summary>
    public DbSet<VitalsLog> VitalsLogs => Set<VitalsLog>();

    /// <summary>
    ///     Gets or sets the medication logs table.
    /// </summary>
    public DbSet<MedicationLog> MedicationLogs => Set<MedicationLog>();

    /// <summary>
    ///     Gets or sets the ROM logs table.
    /// </summary>
    public DbSet<ROMLog> ROMLogs => Set<ROMLog>();

    /// <summary>
    ///     Gets or sets the behavior notes table.
    /// </summary>
    public DbSet<BehaviorNote> BehaviorNotes => Set<BehaviorNote>();

    /// <summary>
    ///     Gets or sets the activities table.
    /// </summary>
    public DbSet<Activity> Activities => Set<Activity>();

    /// <summary>
    ///     Gets or sets the activity participants table.
    /// </summary>
    public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();

    /// <summary>
    ///     Gets or sets the incidents table.
    /// </summary>
    public DbSet<Incident> Incidents => Set<Incident>();

    /// <summary>
    ///     Gets or sets the incident follow-ups table.
    /// </summary>
    public DbSet<IncidentFollowUp> IncidentFollowUps => Set<IncidentFollowUp>();

    /// <summary>
    ///     Gets or sets the incident photos table.
    /// </summary>
    public DbSet<IncidentPhoto> IncidentPhotos => Set<IncidentPhoto>();

    /// <summary>
    ///     Gets or sets the documents table.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    ///     Gets or sets the document folders table.
    /// </summary>
    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();

    /// <summary>
    ///     Gets or sets the document access permissions table.
    /// </summary>
    public DbSet<DocumentAccessPermission> DocumentAccessPermissions => Set<DocumentAccessPermission>();

    /// <summary>
    ///     Gets or sets the document access history table.
    /// </summary>
    public DbSet<DocumentAccessHistory> DocumentAccessHistory => Set<DocumentAccessHistory>();

    /// <summary>
    ///     Gets or sets the user passkeys table.
    ///     Uses 'new' keyword to hide inherited Identity passkey property in favor of custom UserPasskey entity.
    /// </summary>
    public new DbSet<UserPasskey> UserPasskeys => Set<UserPasskey>();

    /// <summary>
    ///     Gets or sets the appointments table.
    /// </summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity table names for clarity
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.BackupCodesHash).HasMaxLength(2000);
            entity.Property(u => u.InvitationToken).HasMaxLength(500);

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.InvitationToken);

            // Configure passkey-related properties
            entity.Property(u => u.RequiresPasskeySetup).HasDefaultValue(false);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(r => r.Description).HasMaxLength(500);
        });

        // Configure remaining Identity table names for clarity
        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        // Configure Home entity
        builder.Entity<Home>(entity =>
        {
            entity.ToTable("Homes");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(200).IsRequired();
            entity.Property(h => h.Address).HasMaxLength(500).IsRequired();
            entity.Property(h => h.City).HasMaxLength(100).IsRequired();
            entity.Property(h => h.State).HasMaxLength(50).IsRequired();
            entity.Property(h => h.ZipCode).HasMaxLength(20).IsRequired();
            entity.Property(h => h.PhoneNumber).HasMaxLength(20);

            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.IsActive);

            entity.HasMany(h => h.Beds)
                .WithOne(b => b.Home)
                .HasForeignKey(b => b.HomeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(h => h.CaregiverAssignments)
                .WithOne(ca => ca.Home)
                .HasForeignKey(ca => ca.HomeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Bed entity
        builder.Entity<Bed>(entity =>
        {
            entity.ToTable("Beds");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Label).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(BedStatus.Available);

            // Ensure unique bed labels within each home
            entity.HasIndex(b => new { b.HomeId, b.Label }).IsUnique();
        });

        // Configure Client entity
        builder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Gender).HasMaxLength(50).IsRequired();
            entity.Property(c => c.SsnEncrypted).HasMaxLength(500);
            entity.Property(c => c.PrimaryPhysician).HasMaxLength(200);
            entity.Property(c => c.PrimaryPhysicianPhone).HasMaxLength(20);
            entity.Property(c => c.EmergencyContactName).HasMaxLength(200);
            entity.Property(c => c.EmergencyContactPhone).HasMaxLength(20);
            entity.Property(c => c.EmergencyContactRelationship).HasMaxLength(100);
            entity.Property(c => c.Allergies).HasMaxLength(2000);
            entity.Property(c => c.Diagnoses).HasMaxLength(2000);
            entity.Property(c => c.MedicationList).HasMaxLength(4000);
            entity.Property(c => c.PhotoUrl).HasMaxLength(500);
            entity.Property(c => c.Notes).HasMaxLength(4000);
            entity.Property(c => c.DischargeReason).HasMaxLength(500);

            entity.HasIndex(c => c.HomeId);
            entity.HasIndex(c => c.IsActive);
            entity.HasIndex(c => new { c.LastName, c.FirstName });

            entity.HasOne(c => c.Home)
                .WithMany(h => h.Clients)
                .HasForeignKey(c => c.HomeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Bed)
                .WithOne(b => b.CurrentOccupant)
                .HasForeignKey<Client>(c => c.BedId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CaregiverHomeAssignment entity
        builder.Entity<CaregiverHomeAssignment>(entity =>
        {
            entity.ToTable("CaregiverHomeAssignments");
            entity.HasKey(ca => ca.Id);

            // Ensure a caregiver can only be assigned once to a home
            entity.HasIndex(ca => new { ca.UserId, ca.HomeId }).IsUnique();

            entity.HasOne(ca => ca.User)
                .WithMany(u => u.HomeAssignments)
                .HasForeignKey(ca => ca.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ADLLog entity
        builder.Entity<ADLLog>(entity =>
        {
            entity.ToTable("ADLLogs");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Bathing)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Dressing)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Toileting)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Transferring)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Continence)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Feeding)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(a => a.Notes).HasMaxLength(2000);

            entity.HasIndex(a => a.ClientId);
            entity.HasIndex(a => a.CaregiverId);
            entity.HasIndex(a => a.Timestamp);

            entity.HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Caregiver)
                .WithMany()
                .HasForeignKey(a => a.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure VitalsLog entity
        builder.Entity<VitalsLog>(entity =>
        {
            entity.ToTable("VitalsLogs");
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Temperature).HasPrecision(5, 2);
            entity.Property(v => v.TemperatureUnit)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(TemperatureUnit.Fahrenheit);
            entity.Property(v => v.Notes).HasMaxLength(2000);

            entity.HasIndex(v => v.ClientId);
            entity.HasIndex(v => v.CaregiverId);
            entity.HasIndex(v => v.Timestamp);

            entity.HasOne(v => v.Client)
                .WithMany()
                .HasForeignKey(v => v.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Caregiver)
                .WithMany()
                .HasForeignKey(v => v.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure MedicationLog entity
        builder.Entity<MedicationLog>(entity =>
        {
            entity.ToTable("MedicationLogs");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.MedicationName).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Dosage).HasMaxLength(100).IsRequired();
            entity.Property(m => m.Route)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(m => m.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(m => m.PrescribedBy).HasMaxLength(200);
            entity.Property(m => m.Pharmacy).HasMaxLength(200);
            entity.Property(m => m.RxNumber).HasMaxLength(50);
            entity.Property(m => m.Notes).HasMaxLength(2000);

            entity.HasIndex(m => m.ClientId);
            entity.HasIndex(m => m.CaregiverId);
            entity.HasIndex(m => m.Timestamp);

            entity.HasOne(m => m.Client)
                .WithMany()
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Caregiver)
                .WithMany()
                .HasForeignKey(m => m.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ROMLog entity
        builder.Entity<ROMLog>(entity =>
        {
            entity.ToTable("ROMLogs");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.ActivityDescription).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Notes).HasMaxLength(2000);

            entity.HasIndex(r => r.ClientId);
            entity.HasIndex(r => r.CaregiverId);
            entity.HasIndex(r => r.Timestamp);

            entity.HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Caregiver)
                .WithMany()
                .HasForeignKey(r => r.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BehaviorNote entity
        builder.Entity<BehaviorNote>(entity =>
        {
            entity.ToTable("BehaviorNotes");
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Category)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(b => b.NoteText).HasMaxLength(4000).IsRequired();
            entity.Property(b => b.Severity)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(b => b.ClientId);
            entity.HasIndex(b => b.CaregiverId);
            entity.HasIndex(b => b.Timestamp);

            entity.HasOne(b => b.Client)
                .WithMany()
                .HasForeignKey(b => b.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Caregiver)
                .WithMany()
                .HasForeignKey(b => b.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Activity entity
        builder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.ActivityName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(2000);
            entity.Property(a => a.Category)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(a => a.Date);
            entity.HasIndex(a => a.HomeId);
            entity.HasIndex(a => a.CreatedById);

            entity.HasOne(a => a.Home)
                .WithMany()
                .HasForeignKey(a => a.HomeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ActivityParticipant entity
        builder.Entity<ActivityParticipant>(entity =>
        {
            entity.ToTable("ActivityParticipants");
            entity.HasKey(ap => ap.Id);

            // Ensure a client can only be added once to an activity
            entity.HasIndex(ap => new { ap.ActivityId, ap.ClientId }).IsUnique();

            entity.HasOne(ap => ap.Activity)
                .WithMany(a => a.Participants)
                .HasForeignKey(ap => ap.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ap => ap.Client)
                .WithMany()
                .HasForeignKey(ap => ap.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Incident entity
        builder.Entity<Incident>(entity =>
        {
            entity.ToTable("Incidents");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.IncidentNumber).HasMaxLength(50).IsRequired();
            entity.Property(i => i.Location).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(4000).IsRequired();
            entity.Property(i => i.ActionsTaken).HasMaxLength(2000);
            entity.Property(i => i.WitnessNames).HasMaxLength(500);
            entity.Property(i => i.NotifiedParties).HasMaxLength(500);
            entity.Property(i => i.ClosureNotes).HasMaxLength(2000);
            entity.Property(i => i.IncidentType)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(IncidentStatus.Draft);

            entity.HasIndex(i => i.IncidentNumber).IsUnique();
            entity.HasIndex(i => i.ClientId);
            entity.HasIndex(i => i.HomeId);
            entity.HasIndex(i => i.ReportedById);
            entity.HasIndex(i => i.Status);
            entity.HasIndex(i => i.OccurredAt);
            entity.HasIndex(i => i.Severity);

            entity.HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(i => i.Home)
                .WithMany()
                .HasForeignKey(i => i.HomeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.ReportedBy)
                .WithMany()
                .HasForeignKey(i => i.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.ClosedBy)
                .WithMany()
                .HasForeignKey(i => i.ClosedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure IncidentFollowUp entity
        builder.Entity<IncidentFollowUp>(entity =>
        {
            entity.ToTable("IncidentFollowUps");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Note).HasMaxLength(4000).IsRequired();

            entity.HasIndex(f => f.IncidentId);
            entity.HasIndex(f => f.CreatedAt);

            entity.HasOne(f => f.Incident)
                .WithMany(i => i.FollowUps)
                .HasForeignKey(f => f.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.CreatedBy)
                .WithMany()
                .HasForeignKey(f => f.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure IncidentPhoto entity
        builder.Entity<IncidentPhoto>(entity =>
        {
            entity.ToTable("IncidentPhotos");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.BlobPath).HasMaxLength(500).IsRequired();
            entity.Property(p => p.FileName).HasMaxLength(255).IsRequired();
            entity.Property(p => p.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Caption).HasMaxLength(500);

            entity.HasIndex(p => p.IncidentId);
            entity.HasIndex(p => p.CreatedById);
            entity.HasIndex(p => p.CreatedAt);

            entity.HasOne(p => p.Incident)
                .WithMany(i => i.Photos)
                .HasForeignKey(p => p.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Document entity
        builder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(d => d.BlobPath).HasMaxLength(500).IsRequired();
            entity.Property(d => d.Description).HasMaxLength(1000);
            entity.Property(d => d.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(d => d.Scope)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(DocumentScope.Client);

            entity.HasIndex(d => d.ClientId);
            entity.HasIndex(d => d.HomeId);
            entity.HasIndex(d => d.FolderId);
            entity.HasIndex(d => d.UploadedById);
            entity.HasIndex(d => d.IsActive);
            entity.HasIndex(d => d.DocumentType);
            entity.HasIndex(d => d.Scope);

            entity.HasOne(d => d.Client)
                .WithMany()
                .HasForeignKey(d => d.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Home)
                .WithMany()
                .HasForeignKey(d => d.HomeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Folder)
                .WithMany(f => f.Documents)
                .HasForeignKey(d => d.FolderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DocumentFolder entity
        builder.Entity<DocumentFolder>(entity =>
        {
            entity.ToTable("DocumentFolders");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Name).HasMaxLength(200).IsRequired();
            entity.Property(f => f.Description).HasMaxLength(1000);
            entity.Property(f => f.Scope)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(f => f.ParentFolderId);
            entity.HasIndex(f => f.ClientId);
            entity.HasIndex(f => f.HomeId);
            entity.HasIndex(f => f.Scope);
            entity.HasIndex(f => f.IsActive);
            entity.HasIndex(f => f.CreatedById);

            // Unique folder name within same parent and scope
            entity.HasIndex(f => new { f.ParentFolderId, f.Name, f.Scope, f.ClientId, f.HomeId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            entity.HasOne(f => f.ParentFolder)
                .WithMany(f => f.ChildFolders)
                .HasForeignKey(f => f.ParentFolderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Client)
                .WithMany()
                .HasForeignKey(f => f.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Home)
                .WithMany()
                .HasForeignKey(f => f.HomeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.CreatedBy)
                .WithMany()
                .HasForeignKey(f => f.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DocumentAccessPermission entity
        builder.Entity<DocumentAccessPermission>(entity =>
        {
            entity.ToTable("DocumentAccessPermissions");
            entity.HasKey(p => p.Id);

            // Ensure a caregiver can only have one permission per document
            entity.HasIndex(p => new { p.DocumentId, p.CaregiverId }).IsUnique();

            entity.HasOne(p => p.Document)
                .WithMany(d => d.AccessPermissions)
                .HasForeignKey(p => p.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Caregiver)
                .WithMany()
                .HasForeignKey(p => p.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.GrantedBy)
                .WithMany()
                .HasForeignKey(p => p.GrantedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DocumentAccessHistory entity
        builder.Entity<DocumentAccessHistory>(entity =>
        {
            entity.ToTable("DocumentAccessHistory");
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Action).HasMaxLength(20).IsRequired();

            entity.HasIndex(h => h.DocumentId);
            entity.HasIndex(h => h.CaregiverId);
            entity.HasIndex(h => h.PerformedAt);

            entity.HasOne(h => h.Document)
                .WithMany(d => d.AccessHistory)
                .HasForeignKey(h => h.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.Caregiver)
                .WithMany()
                .HasForeignKey(h => h.CaregiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.PerformedBy)
                .WithMany()
                .HasForeignKey(h => h.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UserPasskey entity
        builder.Entity<UserPasskey>(entity =>
        {
            entity.ToTable("UserPasskeys");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.CredentialId).HasMaxLength(500).IsRequired();
            entity.Property(p => p.PublicKey).HasMaxLength(2000).IsRequired();
            entity.Property(p => p.DeviceName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.CredentialType).HasMaxLength(50).IsRequired();
            entity.Property(p => p.AaGuid).HasMaxLength(100);
            entity.Property(p => p.Transports).HasMaxLength(200);

            // Index for fast credential lookup during authentication
            entity.HasIndex(p => p.CredentialId).IsUnique();
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => new { p.UserId, p.IsActive });

            entity.HasOne(p => p.User)
                .WithMany(u => u.Passkeys)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Appointment entity
        builder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Title).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Location).HasMaxLength(300);
            entity.Property(a => a.ProviderName).HasMaxLength(200);
            entity.Property(a => a.ProviderPhone).HasMaxLength(20);
            entity.Property(a => a.Notes).HasMaxLength(2000);
            entity.Property(a => a.TransportationNotes).HasMaxLength(500);
            entity.Property(a => a.OutcomeNotes).HasMaxLength(2000);
            entity.Property(a => a.AppointmentType)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AppointmentStatus.Scheduled);

            entity.HasIndex(a => a.ClientId);
            entity.HasIndex(a => a.HomeId);
            entity.HasIndex(a => a.ScheduledAt);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.AppointmentType);
            entity.HasIndex(a => a.CreatedById);
            entity.HasIndex(a => new { a.HomeId, a.ScheduledAt });
            entity.HasIndex(a => new { a.ClientId, a.ScheduledAt });

            entity.HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Home)
                .WithMany()
                .HasForeignKey(a => a.HomeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.CompletedBy)
                .WithMany()
                .HasForeignKey(a => a.CompletedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}