namespace LenkCareHomes.Api.Domain.Constants;

/// <summary>
/// Defines audit action types for logging to Cosmos DB.
/// </summary>
public static class AuditActions
{
    // Authentication actions
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string MfaSetup = "MFA_SETUP";
    public const string MfaVerified = "MFA_VERIFIED";
    public const string MfaFailed = "MFA_FAILED";
    public const string MfaReset = "MFA_RESET";
    public const string BackupCodeUsed = "BACKUP_CODE_USED";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string UserInvited = "USER_INVITED";
    public const string InvitationAccepted = "INVITATION_ACCEPTED";
    public const string AccountSetupCompleted = "ACCOUNT_SETUP_COMPLETED";

    // Passkey/WebAuthn actions
    public const string PasskeyRegistered = "PASSKEY_REGISTERED";
    public const string PasskeyAuthenticated = "PASSKEY_AUTHENTICATED";
    public const string PasskeyAuthFailed = "PASSKEY_AUTH_FAILED";
    public const string PasskeyDeleted = "PASSKEY_DELETED";
    public const string PasskeyUpdated = "PASSKEY_UPDATED";

    // User management actions
    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    public const string UserReactivated = "USER_REACTIVATED";
    public const string UserDeleted = "USER_DELETED";
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRemoved = "ROLE_REMOVED";

    // Home management actions
    public const string HomeCreated = "HOME_CREATED";
    public const string HomeUpdated = "HOME_UPDATED";
    public const string HomeDeactivated = "HOME_DEACTIVATED";
    public const string HomeReactivated = "HOME_REACTIVATED";
    public const string CaregiverAssigned = "CAREGIVER_ASSIGNED";
    public const string CaregiverUnassigned = "CAREGIVER_UNASSIGNED";
    public const string CaregiverHomeAssign = "CAREGIVER_HOME_ASSIGN";

    // Bed management actions
    public const string BedCreated = "BED_CREATED";
    public const string BedUpdated = "BED_UPDATED";
    public const string BedDeactivated = "BED_DEACTIVATED";
    public const string BedReactivated = "BED_REACTIVATED";

    // Client management actions
    public const string ClientAdmitted = "CLIENT_ADMITTED";
    public const string ClientUpdated = "CLIENT_UPDATED";
    public const string ClientDischarged = "CLIENT_DISCHARGED";
    public const string ClientTransferred = "CLIENT_TRANSFERRED";
    public const string ClientViewed = "CLIENT_VIEWED";

    // PHI access actions
    public const string PhiAccessed = "PHI_ACCESSED";
    public const string PhiModified = "PHI_MODIFIED";
    public const string DocumentViewed = "DOCUMENT_VIEWED";
    public const string DocumentUploaded = "DOCUMENT_UPLOADED";
    public const string DocumentDeleted = "DOCUMENT_DELETED";
    public const string DocumentAccessGranted = "DOCUMENT_ACCESS_GRANTED";
    public const string DocumentAccessRevoked = "DOCUMENT_ACCESS_REVOKED";
    public const string DocumentSasGenerated = "DOCUMENT_SAS_GENERATED";

    // Folder management actions
    public const string FolderCreated = "FOLDER_CREATED";
    public const string FolderUpdated = "FOLDER_UPDATED";
    public const string FolderMoved = "FOLDER_MOVED";
    public const string FolderDeleted = "FOLDER_DELETED";

    // Incident actions
    public const string IncidentCreated = "INCIDENT_CREATED";
    public const string IncidentUpdated = "INCIDENT_UPDATED";
    public const string IncidentSubmitted = "INCIDENT_SUBMITTED";
    public const string IncidentDeleted = "INCIDENT_DELETED";
    public const string IncidentViewed = "INCIDENT_VIEWED";
    public const string IncidentStatusUpdated = "INCIDENT_STATUS_UPDATED";
    public const string IncidentFollowUpAdded = "INCIDENT_FOLLOW_UP_ADDED";
    public const string IncidentNotificationSent = "INCIDENT_NOTIFICATION_SENT";

    // Care logging actions
    public const string ADLLogged = "ADL_LOGGED";
    public const string ADLViewed = "ADL_VIEWED";
    public const string VitalsLogged = "VITALS_LOGGED";
    public const string VitalsViewed = "VITALS_VIEWED";
    public const string MedicationLogged = "MEDICATION_LOGGED";
    public const string MedicationViewed = "MEDICATION_VIEWED";
    public const string ROMLogged = "ROM_LOGGED";
    public const string ROMViewed = "ROM_VIEWED";
    public const string BehaviorNoteCreated = "BEHAVIOR_NOTE_CREATED";
    public const string BehaviorNoteViewed = "BEHAVIOR_NOTE_VIEWED";
    public const string ActivityCreated = "ACTIVITY_CREATED";
    public const string ActivityUpdated = "ACTIVITY_UPDATED";
    public const string ActivityDeleted = "ACTIVITY_DELETED";
    public const string ActivityViewed = "ACTIVITY_VIEWED";
    public const string TimelineViewed = "TIMELINE_VIEWED";

    // Appointment actions
    public const string AppointmentCreated = "APPOINTMENT_CREATED";
    public const string AppointmentUpdated = "APPOINTMENT_UPDATED";
    public const string AppointmentCompleted = "APPOINTMENT_COMPLETED";
    public const string AppointmentCancelled = "APPOINTMENT_CANCELLED";
    public const string AppointmentNoShow = "APPOINTMENT_NO_SHOW";
    public const string AppointmentRescheduled = "APPOINTMENT_RESCHEDULED";
    public const string AppointmentDeleted = "APPOINTMENT_DELETED";
    public const string AppointmentViewed = "APPOINTMENT_VIEWED";
}
