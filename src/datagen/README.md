# LenkCare Homes Synthetic Data Generator

This directory contains Python scripts to generate realistic synthetic data for testing the LenkCare Homes application.

## Features

- Generates **2 years** of synthetic data
- **6 homes** that open organically over time (simulating business growth)
- Realistic client admissions and discharges
- Daily care logs (ADLs, vitals, medications, ROM exercises)
- Behavior notes with varying severity
- Group and individual activities
- Incidents of various types (falls, medications, behavioral, etc.)
- Caregivers assigned to homes
- **PDF documents** for each client (Care Plans, Medical Reports, Consent Forms, Insurance, Legal, ID)

## Prerequisites

- Python 3.10+
- Required packages: `faker`, `python-dateutil`, `reportlab`, `azure-storage-blob`

## Installation

```bash
cd src/datagen
pip install -r requirements.txt
```

## Usage

### Generate Data

```bash
python generate.py
```

This will create JSON files in the API project's `SyntheticData` folder:
`src/backend/LenkCareHomes.Server/LenkCareHomes.Api/SyntheticData/`

Generated files:

- `homes.json` - Adult family homes
- `beds.json` - Beds in each home
- `users.json` - Admin, Sysadmin, and Caregivers
- `caregiver_home_assignments.json` - Caregiver-to-home assignments
- `clients.json` - Residents (clients)
- `adl_logs.json` - Activities of Daily Living logs
- `vitals_logs.json` - Vital signs records
- `medication_logs.json` - Medication administration records
- `rom_logs.json` - Range of Motion exercise logs
- `behavior_notes.json` - Behavior and mood notes
- `activities.json` - Recreational/social activities
- `activity_participants.json` - Activity participation records
- `incidents.json` - Incident reports
- `documents.json` - Document metadata (references PDFs in documents/ folder)
- `all_data.json` - Combined file with all data

Additionally, PDF documents are generated in:
`src/backend/LenkCareHomes.Server/LenkCareHomes.Api/SyntheticData/documents/`

### Loading Data

The synthetic data can be loaded into the system via the Settings page (Sysadmin only, Development environment only).

1. Navigate to **Settings** in the application
2. Click **Developer Tools** (visible only to Sysadmin users)
3. Click **Load Synthetic Data**

When loading:
- JSON records are imported into SQL Server
- PDF documents are uploaded to Azure Blob Storage
- Document metadata is linked to clients in the database

⚠️ **Warning**: Loading synthetic data will add records to your database. This feature is disabled in production environments.

### Clearing Data

The Developer Tools page also allows clearing all data:

1. Navigate to **Settings** → **Developer Tools**
2. Click **Clear All Data**
3. Confirm the destructive operation

This will delete:
- All SQL database records (except your user account)
- All documents from Azure Blob Storage
- All audit logs from Cosmos DB

## Data Characteristics

### Homes
- 6 homes open progressively over 2 years
- Each home has 4-6 beds
- Located in Washington State cities

### Clients
- Ages 65-95 at admission
- Realistic diagnoses (dementia, diabetes, hypertension, etc.)
- Medication lists appropriate for conditions
- Some clients discharged during the period (realistic turnover)

### Care Logs
- ADL logs: 1-2 per client per day (90% of days)
- Vitals: 1-2 per client per day (85% of days)
- Medications: Based on client's prescription list
- ROM exercises: Every 2-3 days
- Behavior notes: More frequent for dementia patients

### Incidents
- 1-3 incidents per home per month
- Types: Falls (35%), Medication (15%), Behavioral (15%), Medical (15%), Injury (10%), Other (8%), Elopement (2%)
- Most incidents are closed; some under review

### Documents
Each client receives a set of realistic PDF documents generated at admission:

| Document Type | Description |
|---------------|-------------|
| **Care Plan** | Comprehensive care plan including ADL levels, goals, and emergency contacts |
| **Medical Report** | Admission or annual physical examination results |
| **Consent Form** | General treatment consent and HIPAA acknowledgment |
| **Insurance** | Insurance information summary with primary/secondary coverage |
| **Legal** | Power of Attorney, Advance Directive, or POLST forms |
| **Identification** | ID verification record (Driver's License, Medicare, etc.) |

Documents are professionally formatted with:
- Facility letterhead and branding
- Client-specific information pulled from their record
- Realistic medical terminology and content
- Proper signature sections

## Build Integration

The API project's `.csproj` is configured to copy `SyntheticData/**/*` files (JSON and PDF documents) to the output directory for all builds. This ensures synthetic data is available both locally and when deployed via CI/CD.

```xml
<!-- From LenkCareHomes.Api.csproj -->
<ItemGroup>
  <Content Include="SyntheticData\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```
```

> **Note:** The GitHub Actions workflow generates fresh synthetic data (including PDFs) before each build when `GENERATE_SYNTHETIC_DATA` is set to `'true'`.

## Customization

Edit `config.py` to customize:
- Date range
- City locations
- Diagnosis lists
- Medication lists
- Activity types
- Incident frequencies

## Notes

- All data is **completely synthetic** and contains no real PHI
- Email addresses use `@lenkcare.example.com` domain
- Phone numbers are generated by Faker (not real)
- The data is designed to be realistic for testing and demo purposes
- The synthetic data feature is protected by multiple security layers:
  1. Service only registered in Development environment
  2. `[DevelopmentOnly]` attribute returns 404 in production
  3. `[Authorize(Roles = "Sysadmin")]` role check
  4. Service-level `IsAvailable` check
