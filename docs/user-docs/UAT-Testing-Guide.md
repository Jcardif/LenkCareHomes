---
title: "LenkCare Homes - User Acceptance Testing (UAT) Guide"
author: "LenkCare Homes"
date: "December 22, 2025"
version: "1.0"
geometry: margin=1in
fontsize: 12pt
documentclass: article
toc: true
toc-depth: 3
numbersections: true
colorlinks: true
linkcolor: NavyBlue
urlcolor: NavyBlue
toccolor: NavyBlue
mainfont: "Times New Roman"
monofont: "Menlo"
header-includes:
  - \usepackage{fancyhdr}
  - \pagestyle{fancy}
  - \fancyhead[L]{LenkCare Homes}
  - \fancyhead[R]{UAT Testing Guide v2.3}
  - \fancyfoot[C]{\thepage}
  - \usepackage{booktabs}
  - \usepackage{longtable}
  - \usepackage{array}
  - \usepackage{multirow}
  - \usepackage{xcolor}
  - \definecolor{Light}{HTML}{F4F4F4}
  - \let\oldtexttt\texttt
  - \renewcommand{\texttt}[1]{\colorbox{Light}{\oldtexttt{#1}}}
  - \renewcommand{\arraystretch}{1.3}
---

\newpage

# Introduction

## What is LenkCare Homes?

LenkCare Homes is a HIPAA-compliant web application designed to help manage adult family homes. It allows staff to:

<!-- Keep blank line before lists for Pandoc -->

- **Manage Homes and Beds** -- Track which homes have available beds and their occupancy
- **Manage Clients (Residents)** -- Admit, track, transfer, and discharge residents
- **Manage Caregivers** -- Invite caregivers via email and assign them to specific homes
- **Log Daily Care Activities** -- Record ADLs, vitals, medications, ROM exercises, behavior notes, and activities
- **Schedule and Track Appointments** -- Manage medical appointments for clients
- **Report Incidents** -- Document, review, and track any incidents that occur
- **Upload and View Documents** -- Store important documents with scope-based organization (Client, Home, Business, General)
- **Generate Reports** -- Create PDF reports for individual clients or entire homes
- **View Audit Logs** -- Track who accessed or changed data with Activity Feed and Technical views (Admin and Sysadmin)
- **Access Help and Guided Tours** -- Built-in help system with interactive walkthroughs

## Purpose of This Testing

You are helping us verify that the system works correctly before it goes live. Your feedback is essential to ensure the application meets the needs of real users and complies with HIPAA requirements for protecting health information.

## Your Role

You will test the system as different types of users:

1. **Admin** -- Has full access to manage everything: homes, clients, caregivers, documents, reports, audit logs, and users
2. **Caregiver** -- Has limited access to view assigned clients, log care activities, report incidents, and view permitted documents (home-scoped access only)
3. **Sysadmin** -- Has system maintenance access only: audit logs, user management, developer tools; **cannot access or modify Protected Health Information (PHI)**

\newpage

# Getting Started

## Accessing the System

<!-- Keep blank line before lists for Pandoc -->

1. Open your web browser (Chrome, Edge, or Firefox recommended)
2. Go to the application URL: **https://staging.homes.lenkcare.com/**
3. You will see the login page

## Logging In

LenkCare Homes uses **Passkey authentication** (biometrics like fingerprint or face recognition) for enhanced security. This is more secure and convenient than traditional passwords with authenticator apps.

### First-Time Login (New Account Setup)

If you received an invitation email:

<!-- Keep blank line before lists for Pandoc -->

1. Click the link in your invitation email
2. You will be directed to the **Account Setup** page
3. Create your password
4. Set up your **Passkey**:
   - Click **Create Passkey**
   - Follow your device's prompts to register your biometric (fingerprint, face, or security key)
   - Give your passkey a recognizable name (e.g., "MacBook Touch ID" or "Windows Hello")
5. Your account is now ready

### Regular Login

<!-- Keep blank line before lists for Pandoc -->

1. Enter your **email address**
2. Enter your **password**
3. Click **Sign In**
4. You will be prompted to verify with your **Passkey**:
   - Use your fingerprint, face recognition, or security key as configured
5. Upon successful verification, you will be logged in

### Lost Passkey Recovery

If you lose access to your passkey device:

<!-- Keep blank line before lists for Pandoc -->

1. On the passkey verification screen, click **"Lost your passkey?"**
2. Enter your email address
3. Check your email for a recovery link
4. Follow the link to set up a new passkey

### Backup Codes (Sysadmin Only)

**Note:** Backup codes are only available for users with the **Sysadmin** role.

<!-- Keep blank line before lists for Pandoc -->

1. If you are a Sysadmin, you can click **"Use a backup code instead"** on the passkey screen
2. Enter one of your previously saved 8-character backup codes
3. Each backup code can only be used once

**Tip for Sysadmins:** Generate and securely store backup codes from **Settings > Security** before you need them.

## Understanding the Navigation

Once logged in, you will see:

<!-- Keep blank line before lists for Pandoc -->

- **Left Sidebar** -- Main menu to navigate between sections (collapsed on mobile)
- **Top Header** -- Breadcrumbs, search, and your profile/logout options
- **Main Area** -- The content for the current page
- **Help Button** -- Access to documentation, guided tours, and keyboard shortcuts

\newpage

# Understanding the System

## User Roles

| Role          | What They Can Do                                                                                     |
|:--------------|:-----------------------------------------------------------------------------------------------------|
| **Admin**     | Everything: manage homes, beds, clients, caregivers, appointments, documents, reports, view audit logs, and manage users |
| **Caregiver** | View assigned clients, log care activities (ADLs, vitals, medications, ROM, behavior notes, activities), schedule appointments, report incidents, view permitted documents (home-scoped access only) |
| **Sysadmin**  | System maintenance only: view audit logs, manage users, access developer tools; **cannot access or modify PHI** |

## Key Terms

| Term          | Meaning                                                              |
|:--------------|:---------------------------------------------------------------------|
| **Home**      | An adult family home facility                                        |
| **Bed**       | A physical bed in a home where a client stays                        |
| **Client**    | A resident/patient living in a home                                  |
| **Caregiver** | A staff member who provides care to clients                          |
| **ADL**       | Activities of Daily Living (bathing, dressing, toileting, etc.)      |
| **Vitals**    | Health measurements (blood pressure, pulse, temperature, oxygen level, weight, blood sugar) |
| **ROM**       | Range of Motion exercises                                            |
| **Incident**  | An unusual event that needs to be documented (fall, injury, etc.)    |
| **Appointment** | A scheduled medical or professional visit for a client             |
| **Passkey**   | A biometric authentication method (fingerprint, face, security key)  |
| **PHI**       | Protected Health Information (any data that can identify a patient)  |

\newpage

# Test Scenarios for Admin Users

Log in with the **Admin account** to perform these tests.

## Dashboard (ADMIN-01)

### Steps

1. After logging in, you should land on the Dashboard
2. Observe the main statistics and sections shown

### What to Check

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Dashboard shows **Active Homes** count | $\square$ | $\square$ |
| Dashboard shows **Beds** (occupied / total) with percentage | $\square$ | $\square$ |
| Dashboard shows **Active Clients** count | $\square$ | $\square$ |
| Dashboard shows **Active Caregivers** count | $\square$ | $\square$ |
| Dashboard shows **Upcoming Birthdays** section (clients with birthdays in next 30 days) | $\square$ | $\square$ |
| Dashboard shows **Upcoming Appointments** section (appointments in next 7 days) | $\square$ | $\square$ |
| **Quick Actions** section shows buttons for: Manage homes, Manage clients, Manage caregivers | $\square$ | $\square$ |
| Clicking **Refresh** button updates the data | $\square$ | $\square$ |
| Statistics cards are clickable and navigate to their respective pages | $\square$ | $\square$ |
| Clicking an upcoming appointment navigates to the appointment details | $\square$ | $\square$ |
| Clicking a birthday client name navigates to their profile | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Home Management (ADMIN-02)

### View Homes List

**Steps:**

1. Click **Homes** in the left menu
2. Review the list of homes shown

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| You can see a list of existing homes | $\square$ | $\square$ |
| Each home shows: Name, Location (City, State), Capacity, Available Beds, Active Clients, Status (Active/Inactive) | $\square$ | $\square$ |
| You can toggle **"Show inactive homes"** to see deactivated homes | $\square$ | $\square$ |
| Inactive homes display with visual distinction (e.g., grayed out or with badge) | $\square$ | $\square$ |
| Clicking on a home row opens the home details page | $\square$ | $\square$ |
| The **Add Home** button is visible and accessible | $\square$ | $\square$ |

### Create a New Home

**Steps:**

1. Click the **Add Home** button
2. Fill in the form:
   - Home Name: "Test Home - [Your Name]"
   - Address: Start typing an address and select from suggestions (uses Azure Maps autocomplete)
   - City, State, Zip: Should auto-fill when you select an address
   - Phone Number: (555) 123-4567
   - Capacity: 4
3. Click **Create Home**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| The address autocomplete works (suggestions appear as you type) | $\square$ | $\square$ |
| Selecting an address auto-fills city, state, and zip fields | $\square$ | $\square$ |
| The form validates required fields (name, address, phone, capacity) | $\square$ | $\square$ |
| Capacity accepts any positive number (no maximum limit) | $\square$ | $\square$ |
| Phone number field accepts only valid phone formats | $\square$ | $\square$ |
| Success message appears after creation | $\square$ | $\square$ |
| New home appears in the list | $\square$ | $\square$ |
| Home is created with 0 beds (beds must be added manually via "Add Bed" button) | $\square$ | $\square$ |

### View Home Details

**Steps:**

1. Click on a home to view its details

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Home details page shows name, address, phone, capacity | $\square$ | $\square$ |
| Beds section displays all beds with their status (Available/Occupied) | $\square$ | $\square$ |
| Occupied beds show the assigned client's name (clickable link) | $\square$ | $\square$ |
| Available beds are clearly marked as "Available" | $\square$ | $\square$ |
| Edit and Deactivate/Reactivate buttons are visible | $\square$ | $\square$ |

### Edit a Home

**Steps:**

1. Click on a home to view its details
2. Click the **Edit** button (pencil icon)
3. Change the phone number
4. Click **Save Changes**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Edit form opens with existing data pre-filled | $\square$ | $\square$ |
| Can modify all editable fields | $\square$ | $\square$ |
| Changes are saved successfully | $\square$ | $\square$ |
| Updated information shows immediately on the home details page | $\square$ | $\square$ |
| Success notification appears | $\square$ | $\square$ |

### Manage Beds

**Steps:**

1. On the home details page, look at the **Beds** section
2. Click **Add Bed** to add a new bed
3. Observe which beds are occupied vs available
4. Click on an occupied bed's client name

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Beds list shows each bed with its label (e.g., Bed A, Bed B) and status | $\square$ | $\square$ |
| **Add Bed** button is disabled when total active beds equals home capacity | $\square$ | $\square$ |
| Tooltip shows capacity information when Add Bed is disabled | $\square$ | $\square$ |
| Cannot add more beds than the home's capacity | $\square$ | $\square$ |
| Occupied beds show the assigned client's name as a clickable link | $\square$ | $\square$ |
| Clicking client name navigates to that client's profile | $\square$ | $\square$ |
| Available beds are clearly marked as "Available" | $\square$ | $\square$ |

### Deactivate a Home

**Steps:**

1. On a home's details page, click **Deactivate Home** button
2. Confirm the deactivation when prompted

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning message explains the consequences of deactivation | $\square$ | $\square$ |
| Home with active clients shows a warning (may require clients to be transferred first) | $\square$ | $\square$ |
| After confirmation, home status changes to "Inactive" | $\square$ | $\square$ |
| Deactivated home no longer appears in regular homes list | $\square$ | $\square$ |
| Deactivated home appears when "Show inactive homes" is toggled on | $\square$ | $\square$ |

### Reactivate a Home

**Steps:**

1. Enable **"Show inactive homes"** on the homes list
2. Click on a deactivated home
3. Click the **Reactivate Home** button

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Reactivate button is visible on inactive home details | $\square$ | $\square$ |
| After reactivation, home status changes to "Active" | $\square$ | $\square$ |
| Home appears in the regular homes list again | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Client Management (ADMIN-03)

### View Clients List

**Steps:**

1. Click **Clients** in the left menu
2. Review the list of clients

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| You can see client names and photos (avatar with initials if no photo) | $\square$ | $\square$ |
| Each row shows: Name, Home, Bed, Admission Date, Status | $\square$ | $\square$ |
| Allergies are shown with a red/warning tag if present | $\square$ | $\square$ |
| Status shows Active or Discharged | $\square$ | $\square$ |
| You can filter by home using the dropdown filter | $\square$ | $\square$ |
| Toggle **"Show discharged"** to see inactive clients | $\square$ | $\square$ |
| Clicking on a client row opens their details | $\square$ | $\square$ |
| **Admit Client** button is visible | $\square$ | $\square$ |

### Admit a New Client

**Steps:**

1. Click **Admit Client** button
2. Fill in the form:
   - First Name: "Test"
   - Last Name: "Client [Your Name]"
   - Date of Birth: Select a date (e.g., 01/15/1945)
   - Gender: Select Male, Female, or Other
   - Home: Select an existing home
   - Bed: Select an available bed
   - Admission Date: Today's date
   - Primary Physician: Dr. Smith
   - Physician Phone: (555) 234-5678
   - Allergies: Enter comma-separated allergies (e.g., Penicillin, Shellfish)
   - Diagnoses: Enter comma-separated diagnoses (e.g., Diabetes Type 2, Hypertension)
   - Current Medications: Free text for medication list
   - Emergency Contacts: Add one or more contacts (Name, Phone, Relationship each)
   - Additional Notes: Any relevant notes (optional)
3. Click **Admit Client**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Form validates required fields (name, DOB, home, bed, admission date) | $\square$ | $\square$ |
| Bed dropdown only shows available beds for the selected home | $\square$ | $\square$ |
| Changing home selection updates available beds | $\square$ | $\square$ |
| Bed dropdown is empty when all beds are occupied (cannot admit) | $\square$ | $\square$ |
| Cannot admit client when home is at capacity (backend validation) | $\square$ | $\square$ |
| Multiple allergies can be entered (comma-separated) | $\square$ | $\square$ |
| Multiple diagnoses can be entered | $\square$ | $\square$ |
| Can add multiple emergency contacts (click Add button) | $\square$ | $\square$ |
| Can remove emergency contacts | $\square$ | $\square$ |
| Success message appears after admission | $\square$ | $\square$ |
| Client appears in the clients list | $\square$ | $\square$ |
| Client details page opens after admission | $\square$ | $\square$ |
| The selected bed shows as occupied on the home details page | $\square$ | $\square$ |

### View Client Details - Overview Tab

**Steps:**

1. Click on a client to view their profile
2. You should see the **Overview** tab by default

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Client photo/avatar is displayed | $\square$ | $\square$ |
| **Placement Information**: Home name, Bed, Admission date are shown | $\square$ | $\square$ |
| **Medical Information**: Physician name, phone, allergies, diagnoses are shown | $\square$ | $\square$ |
| **Medications**: Current medications list is displayed | $\square$ | $\square$ |
| **Emergency Contacts**: All contacts with name, phone, relationship are shown | $\square$ | $\square$ |
| All tabs are visible: Overview, Care Log, Incidents, Appointments, Documents | $\square$ | $\square$ |
| Action buttons visible: Edit, Transfer, Discharge | $\square$ | $\square$ |

### View Client Details - All Tabs

**Steps:**

1. Navigate through each tab on the client profile

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| **Overview** tab shows personal, placement, and medical info | $\square$ | $\square$ |
| **Care Log** tab shows sub-tabs: ADL, Vitals, Medications, ROM, Behavior, Activities, Timeline | $\square$ | $\square$ |
| **Incidents** tab shows list of incidents for this client | $\square$ | $\square$ |
| **Appointments** tab shows scheduled appointments for this client | $\square$ | $\square$ |
| **Documents** tab shows documents associated with this client | $\square$ | $\square$ |

### Edit a Client

**Steps:**

1. On client profile, click **Edit** button
2. Change the allergies field (add or remove an allergy)
3. Update emergency contact phone number
4. Click **Save Changes**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Edit form opens with existing data pre-filled | $\square$ | $\square$ |
| Can modify all editable fields | $\square$ | $\square$ |
| Changes are saved successfully | $\square$ | $\square$ |
| Changes are reflected immediately on the overview | $\square$ | $\square$ |
| Success notification appears | $\square$ | $\square$ |

### Transfer a Client

**Steps:**

1. On a client's profile, click **Transfer**
2. Select a different home (or same home with different bed)
3. Select an available bed in the destination home
4. Add transfer notes (optional)
5. Click **Confirm Transfer**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Transfer modal shows current home and bed | $\square$ | $\square$ |
| Home dropdown shows all active homes | $\square$ | $\square$ |
| Bed dropdown shows only available beds in selected home | $\square$ | $\square$ |
| Cannot select currently occupied bed (if same home) | $\square$ | $\square$ |
| Transfer completes successfully | $\square$ | $\square$ |
| Client's profile shows new home and bed location | $\square$ | $\square$ |
| Previous bed now shows as "Available" on old home | $\square$ | $\square$ |
| New bed shows as occupied on new home | $\square$ | $\square$ |

### Discharge a Client

**Steps:**

1. On a client's profile, click **Discharge**
2. Select today's date as discharge date
3. Select a reason (e.g., "Family Decision", "Medical Transfer", "Deceased", "Other")
4. Add discharge notes (optional)
5. Click **Confirm Discharge**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning message explains what will happen | $\square$ | $\square$ |
| Discharge date picker is available | $\square$ | $\square$ |
| Reason dropdown contains appropriate options | $\square$ | $\square$ |
| Discharge completes successfully | $\square$ | $\square$ |
| Client's status changes to "Discharged" | $\square$ | $\square$ |
| Discharge date is shown on client profile | $\square$ | $\square$ |
| Previous bed becomes available | $\square$ | $\square$ |
| Client no longer appears in default clients list | $\square$ | $\square$ |
| Client appears when "Show discharged" is enabled | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Caregiver Management (ADMIN-04)

### View Caregivers List

**Steps:**

1. Click **Caregivers** in the left menu
2. Review the list of caregivers

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Caregiver names and emails are displayed | $\square$ | $\square$ |
| Status shows Active or Inactive | $\square$ | $\square$ |
| Invitation status shows: Accepted, Pending, or Expired | $\square$ | $\square$ |
| Assigned homes count is shown for each caregiver | $\square$ | $\square$ |
| Toggle **"Show inactive"** to see deactivated caregivers | $\square$ | $\square$ |
| **Invite Caregiver** button is visible | $\square$ | $\square$ |

### Invite a New Caregiver

**Steps:**

1. Click **Invite Caregiver**
2. Fill in the form:
   - Assign to Homes: Select at least one home (checkboxes for active homes)
   - Email: [Use a test email you can access]
   - First Name: "Test"
   - Last Name: "Caregiver"
3. Click **Send Invitation**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Form validates email format | $\square$ | $\square$ |
| Home selection allows selecting multiple homes | $\square$ | $\square$ |
| Only active homes appear in the selection | $\square$ | $\square$ |
| Success message appears after sending invitation | $\square$ | $\square$ |
| New caregiver appears in the list with "Pending Invitation" status | $\square$ | $\square$ |

**Note:** To fully test the invitation process, check the email inbox and complete account setup (create password, set up passkey).

### View Caregiver Details

**Steps:**

1. Click on a caregiver's name to view their details

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Caregiver profile shows name, email, status | $\square$ | $\square$ |
| Invitation status is clearly displayed | $\square$ | $\square$ |
| List of assigned homes is shown | $\square$ | $\square$ |
| **Manage Assignments** button is visible | $\square$ | $\square$ |
| **Deactivate** button is visible (for active caregivers) | $\square$ | $\square$ |

### Manage Caregiver Home Assignments

**Steps:**

1. Click **Manage Assignments** on a caregiver's profile
2. View their current home assignments
3. Add a new home assignment (select from available homes)
4. Remove an existing home assignment
5. Click **Save Assignments**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Current home assignments are listed with checkboxes or tags | $\square$ | $\square$ |
| Can add new home assignments | $\square$ | $\square$ |
| Can remove existing assignments | $\square$ | $\square$ |
| Only active homes are available for assignment | $\square$ | $\square$ |
| Changes save successfully | $\square$ | $\square$ |
| Updated assignments reflect immediately on the caregiver profile | $\square$ | $\square$ |

### Resend Invitation

**Steps:**

1. Find a caregiver with "Pending Invitation" status
2. Click **Resend Invitation**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Resend option is available for pending invitations | $\square$ | $\square$ |
| Success message confirms invitation was resent | $\square$ | $\square$ |

### Deactivate a Caregiver

**Steps:**

1. On a caregiver's profile, click **Deactivate**
2. Confirm the deactivation

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Confirmation prompt appears | $\square$ | $\square$ |
| Caregiver status changes to "Inactive" | $\square$ | $\square$ |
| Caregiver no longer appears in default list | $\square$ | $\square$ |
| Caregiver appears when "Show inactive" is enabled | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Care Log Activities (ADMIN-05)

While testing as Admin, you can log care activities for any client. The Care Log is organized into sub-tabs for different types of entries.

### Access the Care Log

**Steps:**

1. Go to a client's profile
2. Click on the **Care Log** tab
3. Observe the sub-tabs: ADL, Vitals, Medications, ROM, Behavior, Activities, Timeline

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Care Log tab displays with all sub-tabs visible | $\square$ | $\square$ |
| **Quick Log** button is prominently displayed | $\square$ | $\square$ |
| Each sub-tab shows historical entries when clicked | $\square$ | $\square$ |
| Timeline sub-tab shows all entries in chronological order | $\square$ | $\square$ |

### Quick Log Modal

**Steps:**

1. Click the **Quick Log** button
2. Observe the modal that appears with tabs for different entry types

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Quick Log modal opens with tabs: ADL, Vitals, Medications, ROM, Behavior, Activities | $\square$ | $\square$ |
| Modal can be closed with X button or clicking outside | $\square$ | $\square$ |
| Can switch between tabs within the modal | $\square$ | $\square$ |

### Log ADL (Activities of Daily Living)

**Steps:**

1. In the Quick Log modal, select the **ADL** tab
2. Fill in the form:
   - Select which ADL tasks you're logging (Bathing, Dressing, Toileting, Transferring, Continence, Feeding)
   - For each selected task, choose the assistance level:
     - **No Assistance** (green tag) - Client does task independently
     - **Some Assistance** (orange tag) - Partial assistance needed
     - **Full Assistance** (red tag) - Dependent, staff does task
   - Add any notes in the notes field
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| All 6 ADL task categories are displayed with icons | $\square$ | $\square$ |
| Each task has 3 assistance level options (No/Some/Full Assistance) | $\square$ | $\square$ |
| Can select different levels for each category | $\square$ | $\square$ |
| Notes field accepts free text | $\square$ | $\square$ |
| Entry saves successfully with confirmation message | $\square$ | $\square$ |
| Entry appears in the ADL sub-tab and Timeline | $\square$ | $\square$ |
| Entry shows date, time, and who logged it | $\square$ | $\square$ |

### Log Vitals

**Steps:**

1. Click **Quick Log** and select **Vitals** tab
2. Fill in:
   - Blood Pressure: Systolic (e.g., 120) and Diastolic (e.g., 80) - in mmHg
   - Pulse/Heart Rate: (e.g., 72 bpm)
   - Oxygen Saturation: (e.g., 98%)
   - Temperature: (e.g., 98.6) with unit selector (Fahrenheit or Celsius)
   - Notes: Any observations (optional)
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| All vital fields are available and clearly labeled | $\square$ | $\square$ |
| Form shows appropriate units (mmHg, bpm, %) | $\square$ | $\square$ |
| Temperature has unit selector (Fahrenheit/Celsius) | $\square$ | $\square$ |
| Form validates reasonable ranges | $\square$ | $\square$ |
| All fields are optional (no required fields) | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in the Vitals sub-tab with color-coded status tags | $\square$ | $\square$ |

### Log Medications

**Steps:**

1. Click **Quick Log** and select **Medications** tab
2. Fill in:
   - Date: Select the date
   - Time: Select the time given
   - Medication Name: (e.g., "Lisinopril")
   - Dosage: (e.g., "10mg" or "1 tablet")
   - Route: Select from dropdown (Oral, Sublingual, Topical, Inhalation, Injection, Transdermal, Rectal, Ophthalmic, Otic, Nasal, Other)
   - Administration Status: Select status (Administered, Refused, Not Available, Held, Given Early, Given Late)
   - Scheduled Time: (optional) Original scheduled time if different
   - Prescription Info: (optional) Prescribed By, Pharmacy, Rx Number
   - Notes: Any observations
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Medication name field accepts text input | $\square$ | $\square$ |
| Route dropdown has 11 administration route options | $\square$ | $\square$ |
| Status dropdown has 6 administration status options | $\square$ | $\square$ |
| Date/time pickers work correctly | $\square$ | $\square$ |
| Optional prescription fields are available | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in the Medications sub-tab with status tag | $\square$ | $\square$ |

### Log ROM Exercises

**Steps:**

1. Click **Quick Log** and select **ROM** tab
2. Fill in:
   - Date: Select the date
   - Time: Select the time
   - Activity Description: Enter ROM exercise details (e.g., "Passive ROM - Upper Extremities, shoulder flexion/extension")
   - Duration: Enter in minutes (e.g., 15)
   - Repetitions: (optional) Number of repetitions
   - Notes: Any observations about tolerance or range
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Activity description is a free text field | $\square$ | $\square$ |
| Duration field accepts number input in minutes | $\square$ | $\square$ |
| Repetitions field is optional | $\square$ | $\square$ |
| Notes field accepts free text | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in the ROM sub-tab | $\square$ | $\square$ |

### Log Behavior Notes

**Steps:**

1. Click **Quick Log** and select **Behavior** tab
2. Fill in:
   - Category: Select from dropdown (Behavior, Mood, General)
   - Severity: Select from dropdown (Low, Medium, High)
   - Note: Detailed observation (e.g., "Client was calm and cooperative during morning care. Engaged in conversation about family.")
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Mood/behavior selection is available | $\square$ | $\square$ |
| Notes field accepts detailed free-text entry | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Notes appear in the Behavior sub-tab | $\square$ | $\square$ |

### Log Activities (Recreational/Social)

**Steps:**

1. Click **Quick Log** and select **Activities** tab
2. Fill in:
   - Activity Name: (e.g., "Bingo Night", "Garden Walk", "Music Therapy")
   - Description: (optional details about the activity)
   - Date: Select the activity date
   - Category: Select from dropdown (Recreational, Social, Exercise, Other)
   - Start Time: (optional)
   - Duration: Hours and/or minutes
   - Check "This is a group activity" if multiple clients participated
   - If group activity, select additional participants from the client list
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Activity name field accepts text | $\square$ | $\square$ |
| Category dropdown has 4 options: Recreational, Social, Exercise, Other | $\square$ | $\square$ |
| Duration can be entered in hours and minutes | $\square$ | $\square$ |
| Group activity checkbox enables participant selection | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in the Activities sub-tab with type tag (Individual/Group) | $\square$ | $\square$ |

### View Timeline

**Steps:**

1. Click on the **Timeline** sub-tab within Care Log

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Timeline shows all care log entries in chronological order (newest first) | $\square$ | $\square$ |
| Each entry shows: type icon, timestamp, who logged it, summary | $\square$ | $\square$ |
| Different entry types are visually distinguished (icons or colors) | $\square$ | $\square$ |
| Can scroll through historical entries | $\square$ | $\square$ |
| Date range filter is available (if implemented) | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Appointment Management (ADMIN-06)

Appointments allow tracking of medical visits, therapy sessions, and other scheduled events for clients.

### View Appointments List

**Steps:**

1. Click **Appointments** in the left menu
2. Review the list of appointments

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Appointments page loads successfully | $\square$ | $\square$ |
| List shows: Date/Time, Client Name, Title, Type, Status | $\square$ | $\square$ |
| Upcoming appointments are highlighted or shown first | $\square$ | $\square$ |
| Can filter by status (Scheduled, Completed, Cancelled) | $\square$ | $\square$ |
| Can filter by client | $\square$ | $\square$ |
| Can filter by date range | $\square$ | $\square$ |
| **Schedule Appointment** button is visible | $\square$ | $\square$ |
| Clicking on an appointment opens its details | $\square$ | $\square$ |

### Schedule a New Appointment

**Steps:**

1. Click **Schedule Appointment** button
2. Fill in the form:
   - Client: Select a client from dropdown
   - Appointment Type: Select from dropdown (General Practice, Dental, Cardiology, Physical Therapy, Lab Work, etc.)
   - Title: "Annual Physical Exam"
   - Date & Time: Select a future date and time (e.g., 10:00 AM)
   - Duration: Enter minutes (e.g., 60)
   - Location: "123 Medical Plaza" (optional)
   - Provider Name: "Dr. Smith" (optional)
   - Provider Phone: "(555) 123-4567" (optional)
   - Transportation Notes: "Wheelchair van needed" (optional)
   - Notes: "Fasting required - no food after midnight" (optional)
3. Click **Schedule**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Client dropdown shows all active clients | $\square$ | $\square$ |
| Appointment type dropdown has all 17 appointment types | $\square$ | $\square$ |
| Date & Time picker works correctly | $\square$ | $\square$ |
| Duration field accepts number in minutes | $\square$ | $\square$ |
| Location, Provider Name, and Provider Phone are optional | $\square$ | $\square$ |
| Transportation Notes field is available | $\square$ | $\square$ |
| Form validates required fields (client, type, title, date/time, duration) | $\square$ | $\square$ |
| Appointment is created successfully with confirmation | $\square$ | $\square$ |
| Appointment appears in the list | $\square$ | $\square$ |
| Appointment appears on client's Appointments tab | $\square$ | $\square$ |
| Appointment appears on Dashboard "Upcoming Appointments" (if within 7 days) | $\square$ | $\square$ |

### View Appointment Details

**Steps:**

1. Click on an existing appointment to view details

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Appointment details page shows all entered information | $\square$ | $\square$ |
| Client name is clickable (links to client profile) | $\square$ | $\square$ |
| Status is clearly displayed | $\square$ | $\square$ |
| Edit and Cancel buttons are available | $\square$ | $\square$ |
| Mark Complete button is available for scheduled appointments | $\square$ | $\square$ |

### Edit an Appointment

**Steps:**

1. On appointment details, click **Edit**
2. Change the date or time
3. Update the notes
4. Click **Save Changes**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Edit form opens with existing data pre-filled | $\square$ | $\square$ |
| Can modify date, time, and other fields | $\square$ | $\square$ |
| Changes save successfully | $\square$ | $\square$ |
| Updated information displays correctly | $\square$ | $\square$ |

### Mark Appointment as Complete

**Steps:**

1. On a scheduled appointment, click **Mark Complete**
2. Optionally add completion notes (e.g., "Client attended. Next follow-up in 6 months.")
3. Confirm completion

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Mark Complete prompts for optional notes | $\square$ | $\square$ |
| Appointment status changes to "Completed" | $\square$ | $\square$ |
| Completed appointments are visually distinct | $\square$ | $\square$ |

### Cancel an Appointment

**Steps:**

1. On an appointment, click **Cancel**
2. Provide a cancellation reason (optional)
3. Confirm cancellation

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Confirmation prompt appears before cancellation | $\square$ | $\square$ |
| Appointment status changes to "Cancelled" | $\square$ | $\square$ |
| Cancelled appointments are visually distinct | $\square$ | $\square$ |
| Cancelled appointments can be filtered out | $\square$ | $\square$ |

### View Appointments from Client Profile

**Steps:**

1. Go to a client's profile
2. Click on the **Appointments** tab

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Tab shows all appointments for this specific client | $\square$ | $\square$ |
| Can schedule a new appointment directly from this tab | $\square$ | $\square$ |
| Client is pre-selected when scheduling from this tab | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Incident Reporting (ADMIN-07)

### View Incidents List

**Steps:**

1. Click **Incidents** in the left menu
2. Review the list of incidents

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Incidents page loads successfully | $\square$ | $\square$ |
| List shows: Incident #, Date/Time, Home, Client Name, Type, Severity, Status | $\square$ | $\square$ |
| Status badges show: Draft (gray), Submitted (blue), Under Review (yellow), Closed (gray) | $\square$ | $\square$ |
| Can filter by home | $\square$ | $\square$ |
| Can filter by status | $\square$ | $\square$ |
| Can filter by incident type | $\square$ | $\square$ |
| Can filter by date range | $\square$ | $\square$ |
| **Report Incident** button is visible | $\square$ | $\square$ |
| Clicking on an incident row shows details | $\square$ | $\square$ |

### Create a New Incident Report

**Steps:**

1. Click **Report Incident** button
2. Fill in the form:
   - Home: Select the home where the incident occurred (required)
   - Client: Select the client involved (optional - leave empty for home-level incidents)
   - Incident Type: Select type (Fall, Medication, Behavioral, Medical, Injury, Elopement, Other)
   - Severity: Select level (1-5 slider: 1=Minor, 3=Moderate, 5=Severe)
   - Date/Time: Select when the incident occurred (defaults to now)
   - Location: Where in the home it occurred (e.g., "Bathroom", "Bedroom")
   - Description: "Test incident - Client fell while walking to bathroom. Was attempting to walk unassisted."
   - Actions Taken: "Staff helped client up, checked for injuries. Applied ice to left elbow. Notified family."
   - Witnesses: (optional) Names of witnesses
3. Click **Submit Report** or **Save as Draft**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Home dropdown shows all active homes | $\square$ | $\square$ |
| Client dropdown is optional and shows clients from selected home | $\square$ | $\square$ |
| Date/time picker allows selecting past dates/times | $\square$ | $\square$ |
| All incident types are available | $\square$ | $\square$ |
| Severity slider shows 1-5 scale with labeled endpoints | $\square$ | $\square$ |
| Location field accepts free text | $\square$ | $\square$ |
| Form validates required fields (home, type, severity, date, description, actions) | $\square$ | $\square$ |
| Can submit immediately or save as draft | $\square$ | $\square$ |
| Incident is created successfully with confirmation | $\square$ | $\square$ |
| New incident appears in the list with correct status | $\square$ | $\square$ |
| Incident appears on client's Incidents tab (if client selected) | $\square$ | $\square$ |

### View Incident Details

**Steps:**

1. Click on an existing incident to view details

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| All incident details are displayed | $\square$ | $\square$ |
| Client name is clickable (links to client profile) | $\square$ | $\square$ |
| Reporter name and submission time are shown | $\square$ | $\square$ |
| Status and severity are clearly displayed | $\square$ | $\square$ |
| Description and action taken are fully visible | $\square$ | $\square$ |
| Follow-up notes section is visible | $\square$ | $\square$ |

### Review and Update an Incident

**Steps:**

1. Open an existing incident with "Submitted" status
2. Click **Start Review** to change status to "Under Review"
3. Add a follow-up note using the **Add Follow-Up** button
4. Click **Close Incident** when ready to finalize

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can change status from Submitted → Under Review | $\square$ | $\square$ |
| Can add follow-up notes at any stage | $\square$ | $\square$ |
| Follow-up notes show timestamp and who added them | $\square$ | $\square$ |
| Follow-up notes appear in a timeline view | $\square$ | $\square$ |
| Can close the incident from Under Review status | $\square$ | $\square$ |
| Closing requires entering closure notes | $\square$ | $\square$ |
| Closed incidents cannot be modified | $\square$ | $\square$ |
| All status changes are saved correctly | $\square$ | $\square$ |

### Upload Photos to an Incident

**Steps:**

1. Open an incident (not in Draft or Closed status)
2. Find the **Photos** section
3. Click **Upload Photos** or drag and drop image files
4. View the uploaded photos in the gallery

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Photos section is visible on incident details | $\square$ | $\square$ |
| Can upload image files (JPEG, PNG) | $\square$ | $\square$ |
| Upload progress indicator is shown | $\square$ | $\square$ |
| Uploaded photos appear in a gallery grid | $\square$ | $\square$ |
| Can click on a photo to view it larger | $\square$ | $\square$ |
| Photos cannot be uploaded to closed incidents | $\square$ | $\square$ |

### Export Incident as PDF

**Steps:**

1. Open a non-draft incident
2. Click the **Export PDF** button in the header

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Export PDF button is visible for submitted incidents | $\square$ | $\square$ |
| Export PDF button is NOT visible for draft incidents | $\square$ | $\square$ |
| PDF downloads successfully | $\square$ | $\square$ |
| PDF includes all incident details | $\square$ | $\square$ |
| PDF includes photos (if any) | $\square$ | $\square$ |
| PDF includes follow-up notes (if any) | $\square$ | $\square$ |
| PDF filename is descriptive (includes incident number and date) | $\square$ | $\square$ |

### View Incidents from Client Profile

**Steps:**

1. Go to a client's profile
2. Click on the **Incidents** tab

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Tab shows all incidents for this specific client | $\square$ | $\square$ |
| Can report a new incident directly from this tab | $\square$ | $\square$ |
| Client is pre-selected when reporting from this tab | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Document Management (ADMIN-08)

Documents in LenkCare Homes are organized by **scope**: Client, Home, Business, and General. Documents can be organized into folders for better management. The document system includes folder navigation with breadcrumbs.

### View Documents

**Steps:**

1. Click **Documents** in the left menu
2. Observe the document organization

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Documents page loads successfully | $\square$ | $\square$ |
| Document scope tabs are visible: Client, Home, Business, General | $\square$ | $\square$ |
| Can switch between scopes using tabs | $\square$ | $\square$ |
| Breadcrumb navigation shows current folder path | $\square$ | $\square$ |
| Documents and folders are displayed in a list or grid | $\square$ | $\square$ |
| Each document shows: Name, Type, Upload Date, Uploaded By | $\square$ | $\square$ |
| **Upload Document** button is visible | $\square$ | $\square$ |
| **Create Folder** button is visible (Business and General scopes only) | $\square$ | $\square$ |

### Understanding Document Scopes

| Scope | Description | Folder Creation |
|:------|:------------|:----------------|
| **Client** | Documents specific to a single client (care plans, medical records) | Admin only, requires client selection |
| **Home** | Documents for a specific home (policies, emergency contacts) | Admin only, requires home selection |
| **Business** | Business-wide documents (HR policies, contracts) | Admin only |
| **General** | General documents accessible to all staff | Admin only |

### Navigate Folders

**Steps:**

1. Click on a folder to enter it
2. Observe the breadcrumb trail updating
3. Use breadcrumbs to navigate back to parent folders

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Clicking a folder opens its contents | $\square$ | $\square$ |
| Breadcrumb trail shows the navigation path | $\square$ | $\square$ |
| Clicking a breadcrumb navigates to that folder level | $\square$ | $\square$ |
| Root breadcrumb returns to the scope's root folder | $\square$ | $\square$ |

### Create a Folder

**Note:** Folders can only be created in Business and General scopes. Client and Home scopes organize documents automatically.

**Steps:**

1. Navigate to Business or General scope
2. Click **Create Folder** button
3. Enter folder name: "Medical Records - 2026"
4. Click **Create**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Create Folder button is visible in Business and General scopes | $\square$ | $\square$ |
| Create Folder button is NOT visible in Client and Home scopes | $\square$ | $\square$ |
| Folder name field accepts text | $\square$ | $\square$ |
| Folder is created successfully | $\square$ | $\square$ |
| Folder appears in the current directory | $\square$ | $\square$ |
| Can create nested folders (folders inside folders) | $\square$ | $\square$ |

### Upload a Document

**Steps:**

1. Click **Upload Document** button
2. Select a file from your computer (PDF, images supported)
3. Enter document name: "Test Document"
4. Select document type: Care Plan, Medical, Legal, Financial, Assessment, Photo, or Other
5. Select scope (Client, Home, Business, or General)
6. If Client scope, select the client
7. If Home scope, select the home
8. Optionally select a destination folder
9. Click **Upload**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| File picker opens and accepts files | $\square$ | $\square$ |
| Can enter a descriptive document name | $\square$ | $\square$ |
| Document type dropdown has 7 options: Care Plan, Medical, Legal, Financial, Assessment, Photo, Other | $\square$ | $\square$ |
| Scope selection changes available options | $\square$ | $\square$ |
| Client documents require client selection | $\square$ | $\square$ |
| Home documents require home selection | $\square$ | $\square$ |
| Folder selection is optional | $\square$ | $\square$ |
| Upload progress indicator is shown | $\square$ | $\square$ |
| Success message appears after upload | $\square$ | $\square$ |
| Document appears in the correct scope/folder | $\square$ | $\square$ |
| Document shows the correct type tag | $\square$ | $\square$ |

### View a Document

**Steps:**

1. In the Documents list, click on a document to view it

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Document viewer opens in a modal or overlay | $\square$ | $\square$ |
| PDF documents are displayed correctly | $\square$ | $\square$ |
| Images are displayed correctly | $\square$ | $\square$ |
| Can scroll through multi-page documents | $\square$ | $\square$ |
| Zoom controls are available | $\square$ | $\square$ |
| Document metadata is displayed (name, type, upload date, uploader) | $\square$ | $\square$ |
| Download option is available for Admins | $\square$ | $\square$ |
| Close button returns to document list | $\square$ | $\square$ |

### View Documents from Client Profile

**Steps:**

1. Go to a client's profile
2. Click on the **Documents** tab

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Tab shows only documents scoped to this client | $\square$ | $\square$ |
| Can upload new document directly (pre-scoped to client) | $\square$ | $\square$ |
| Documents show correct type tags | $\square$ | $\square$ |

### Delete a Document

**Steps:**

1. Click on a document's options menu (...) or select the document
2. Select **Delete**
3. Confirm deletion

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Delete option is visible for Admin users | $\square$ | $\square$ |
| Confirmation prompt appears with document name | $\square$ | $\square$ |
| Document is removed from the list | $\square$ | $\square$ |
| Success message confirms deletion | $\square$ | $\square$ |
| Deletion is logged in audit logs | $\square$ | $\square$ |

### Delete a Folder

**Steps:**

1. Click on a folder's options menu (...)
2. Select **Delete**
3. Confirm deletion

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Delete option is visible for empty folders | $\square$ | $\square$ |
| Cannot delete folders that contain documents | $\square$ | $\square$ |
| Confirmation prompt appears | $\square$ | $\square$ |
| Folder is removed from the list | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Reports (ADMIN-09)

Reports allow generating PDF summaries of client care or home operations for a specified date range.

### Access Reports Page

**Steps:**

1. Click **Reports** in the left menu
2. Observe the report options available

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Reports page loads successfully | $\square$ | $\square$ |
| Two report types are available: Client Summary Report, Home Summary Report | $\square$ | $\square$ |
| Report type selector/tabs are clearly labeled | $\square$ | $\square$ |

### Generate Client Summary Report

**Steps:**

1. Select Report Type: **Client Summary Report**
2. Select a client from the dropdown
3. Choose a date range:
   - Start Date: 30 days ago
   - End Date: Today
4. Click **Generate Report** or **Download PDF**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Client dropdown shows **active** clients only | $\square$ | $\square$ |
| Optionally filter clients by home first | $\square$ | $\square$ |
| Date range pickers work correctly | $\square$ | $\square$ |
| Date presets available: Last 7 Days, Last 30 Days, Last 90 Days, This Month, Last Month, This Year | $\square$ | $\square$ |
| Loading indicator appears while generating | $\square$ | $\square$ |
| Report downloads as PDF | $\square$ | $\square$ |
| PDF opens correctly in PDF viewer | $\square$ | $\square$ |
| PDF filename is descriptive (includes client name and date range) | $\square$ | $\square$ |

**What to Verify in the PDF:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Client information header (name, DOB, home, bed) | $\square$ | $\square$ |
| Date range is clearly stated | $\square$ | $\square$ |
| ADL summary for the period | $\square$ | $\square$ |
| Vitals summary (with trends if available) | $\square$ | $\square$ |
| Medication administration records | $\square$ | $\square$ |
| Behavior and activity notes | $\square$ | $\square$ |
| Incidents during the period | $\square$ | $\square$ |
| Appointments during the period | $\square$ | $\square$ |
| Report generation timestamp | $\square$ | $\square$ |

### Generate Home Summary Report

**Steps:**

1. Select Report Type: **Home Summary Report**
2. Select a home from the dropdown
3. Choose a date range:
   - Start Date: 30 days ago
   - End Date: Today
4. Click **Generate Report** or **Download PDF**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Home dropdown shows all homes | $\square$ | $\square$ |
| Date range pickers work correctly | $\square$ | $\square$ |
| Loading indicator appears while generating | $\square$ | $\square$ |
| Report downloads as PDF | $\square$ | $\square$ |
| PDF opens correctly | $\square$ | $\square$ |

**What to Verify in the PDF:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Home information header (name, address, capacity) | $\square$ | $\square$ |
| Date range is clearly stated | $\square$ | $\square$ |
| Occupancy summary (beds occupied/available) | $\square$ | $\square$ |
| List of all clients during the period | $\square$ | $\square$ |
| Summary statistics for each client | $\square$ | $\square$ |
| Incidents at this home during the period | $\square$ | $\square$ |
| Staffing/caregiver assignments | $\square$ | $\square$ |
| Report generation timestamp | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Audit Logs (ADMIN-10)

Audit logs track all user actions in the system for HIPAA compliance. The audit logs feature two views: **Activity** (user-friendly) and **Technical** (detailed).

### View Activity Log

**Steps:**

1. Click **Audit Logs** in the left menu
2. Ensure you are on the **Activity** view (icon toggle in header)

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Audit logs page loads successfully | $\square$ | $\square$ |
| Activity view is selected by default for Admins | $\square$ | $\square$ |
| Statistics cards show: Total Events (24h), Login Events (24h), Failed Logins (24h) | $\square$ | $\square$ |
| Each entry shows: Time, User, Action description in plain language | $\square$ | $\square$ |
| Success/Failure status is indicated with icon or color | $\square$ | $\square$ |
| Entries are sorted by most recent first | $\square$ | $\square$ |
| Can scroll through historical entries with pagination | $\square$ | $\square$ |

### View Technical Logs

**Steps:**

1. Click on the **Technical** view toggle (code icon)

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Technical view shows more detailed information | $\square$ | $\square$ |
| Columns include: Timestamp, Action, User, Resource Type, Resource ID, Outcome, IP Address | $\square$ | $\square$ |
| Can click on an entry to see full details | $\square$ | $\square$ |
| Detail view shows request/response data (if available) | $\square$ | $\square$ |
| Sysadmins default to Technical view | $\square$ | $\square$ |

### Filter Audit Logs

**Activity View Filters:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can search by keyword/text | $\square$ | $\square$ |
| Can filter by type (Sign-ins, Client Records, Documents, Incidents, User Management) | $\square$ | $\square$ |
| Can filter by date range | $\square$ | $\square$ |
| Refresh button reloads data | $\square$ | $\square$ |
| Clear button resets filters | $\square$ | $\square$ |

**Technical View Filters (Advanced):**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can filter by specific action type (Login Success, PHI Accessed, etc.) | $\square$ | $\square$ |
| Can filter by date/time range (with time selector) | $\square$ | $\square$ |
| Advanced Filters section expands to show additional options | $\square$ | $\square$ |
| Can filter by resource type (Clients, Documents, Incidents, Users, Homes, Caregivers) | $\square$ | $\square$ |
| Can filter by outcome (Success, Failure, Denied) | $\square$ | $\square$ |
| Can filter by user | $\square$ | $\square$ |
| Multiple filters can be combined | $\square$ | $\square$ |
| Clear filters button resets all filters | $\square$ | $\square$ |

### Export Audit Logs

**Steps:**

1. Apply desired filters (optional)
2. Click **Export to CSV** button

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Export button is visible and accessible | $\square$ | $\square$ |
| CSV file downloads successfully | $\square$ | $\square$ |
| CSV filename is descriptive (includes date range) | $\square$ | $\square$ |
| CSV opens correctly in Excel/spreadsheet application | $\square$ | $\square$ |
| Exported data matches the filtered view | $\square$ | $\square$ |
| All relevant columns are included in export | $\square$ | $\square$ |

### Verify Specific Actions Are Logged

After performing various actions in the system, verify they appear in audit logs:

| Action | Logged? |
|:-------|:-------:|
| Login success | $\square$ |
| Login failure (wrong password) | $\square$ |
| Passkey verification | $\square$ |
| Client admit | $\square$ |
| Client transfer | $\square$ |
| Client discharge | $\square$ |
| Care log entry (ADL, Vitals, etc.) | $\square$ |
| Document upload | $\square$ |
| Document view | $\square$ |
| Incident report created | $\square$ |
| Incident status changed | $\square$ |
| Home created/edited | $\square$ |
| Caregiver invited | $\square$ |
| Report generated | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Settings (ADMIN-11)

The Settings page provides access to profile, security, notifications, and administrative features through a card-based menu. Each section opens as a subpage.

### Access Settings

**Steps:**

1. Click on your profile icon/name in the top right
2. Click **Settings** or navigate via the left menu

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Settings page loads successfully | $\square$ | $\square$ |
| Card menu shows: Profile, Security, Notifications | $\square$ | $\square$ |
| User Management card visible (Admin and Sysadmin only) | $\square$ | $\square$ |
| Developer Tools card visible (Sysadmin only, dev environment) | $\square$ | $\square$ |
| Each card shows an icon, title, and description | $\square$ | $\square$ |

### Profile Settings

**Steps:**

1. Click on the **Profile** card
2. Review your profile information

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Profile page loads with "Back to Settings" link | $\square$ | $\square$ |
| Current profile information is displayed (First Name, Last Name, Email) | $\square$ | $\square$ |
| All fields are currently read-only (disabled) | $\square$ | $\square$ |
| Message indicates profile editing will be available in a future update | $\square$ | $\square$ |

### Security Settings - Manage Passkeys

**Steps:**

1. Click on the **Security** card
2. View the Passkeys section

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Security page loads with "Back to Settings" link | $\square$ | $\square$ |
| Passkeys section shows a table of registered passkeys | $\square$ | $\square$ |
| Each passkey shows: Device icon, Name, Added date, Last used date, Status (Active) | $\square$ | $\square$ |
| **Add Passkey** button is visible in the card header | $\square$ | $\square$ |
| Rename button (edit icon) is visible for each passkey | $\square$ | $\square$ |
| Delete button is visible (disabled if only one passkey exists) | $\square$ | $\square$ |
| Password section shows "Change Password" is coming in a future update | $\square$ | $\square$ |

### Security Settings - Add a New Passkey

**Steps:**

1. Click **Add Passkey** button in the Passkeys card header
2. Follow your device's prompts to register biometric/security key
3. Enter a name for the new passkey (e.g., "MacBook Touch ID")

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Device prompts for biometric/security key registration | $\square$ | $\square$ |
| Passkey name input appears after successful registration | $\square$ | $\square$ |
| Can enter a descriptive name for the passkey | $\square$ | $\square$ |
| New passkey appears in the table | $\square$ | $\square$ |
| Success message confirms registration | $\square$ | $\square$ |

### Security Settings - Rename a Passkey

**Steps:**

1. In the Passkeys table, click the **Rename** button (edit icon) for a passkey
2. Enter a new name
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Rename button opens a modal or inline edit | $\square$ | $\square$ |
| Current name is pre-filled | $\square$ | $\square$ |
| Can enter a new descriptive name | $\square$ | $\square$ |
| Name updates successfully | $\square$ | $\square$ |
| Updated name appears in the table | $\square$ | $\square$ |

### Security Settings - Delete a Passkey

**Steps:**

1. If you have more than one passkey, click the **Delete** button for one
2. Confirm the deletion

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Delete button is visible for each passkey | $\square$ | $\square$ |
| Delete button is DISABLED if only one passkey exists | $\square$ | $\square$ |
| Confirmation prompt appears before deletion | $\square$ | $\square$ |
| Passkey is removed from the table | $\square$ | $\square$ |
| Cannot delete your last remaining passkey | $\square$ | $\square$ |

### Security Settings - Backup Codes (Sysadmin Only)

**Note:** This section is only available for users with the **Sysadmin** role.

**Steps:**

1. On the Security page, look for the **Backup Codes** section
2. Click **Regenerate Codes**
3. Confirm by typing "regenerate" when prompted
4. Save the new codes securely

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Backup codes section is visible (Sysadmin only) | $\square$ | $\square$ |
| Backup codes section is NOT visible for Admin or Caregiver | $\square$ | $\square$ |
| Can regenerate a new set of backup codes | $\square$ | $\square$ |
| Must type "regenerate" to confirm | $\square$ | $\square$ |
| New codes are displayed (8-character format) | $\square$ | $\square$ |
| Warning indicates codes should be saved securely | $\square$ | $\square$ |
| Warning indicates previous codes are invalidated | $\square$ | $\square$ |

### Notification Preferences

**Note:** Notification preferences are currently disabled and marked as a future feature.

**Steps:**

1. Click on the **Notifications** card
2. Review notification settings

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Notifications page loads with "Back to Settings" link | $\square$ | $\square$ |
| Notification categories are listed (Email, Incidents, Care Reminders) | $\square$ | $\square$ |
| All toggle switches are visible but disabled | $\square$ | $\square$ |
| Message indicates this feature is coming in a future update | $\square$ | $\square$ |

### User Management (Admin and Sysadmin)

**Steps:**

1. Click on the **User Management** card
2. View list of all users in the system

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Card is visible for Admin and Sysadmin users | $\square$ | $\square$ |
| List of all users is displayed in a table | $\square$ | $\square$ |
| Each user shows: Name, Email, Role, Status (Active/Pending/Inactive) | $\square$ | $\square$ |
| Can search users by name or email | $\square$ | $\square$ |
| **Invite User** button is visible in the header | $\square$ | $\square$ |
| Clicking on a user row opens their detail modal | $\square$ | $\square$ |

### Invite Admin or Sysadmin User

**Note:** Caregiver invitations are done from the Caregivers page with home assignments. User Management is for Admin and Sysadmin roles only.

**Steps:**

1. Click **Invite User** button
2. Fill in the form:
   - Email: Enter the new user's email
   - First Name: Enter first name
   - Last Name: Enter last name
   - Role: Select Admin or Sysadmin
3. Click **Send Invitation**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Invite form shows email, first name, last name, and role fields | $\square$ | $\square$ |
| Role dropdown only shows Admin and Sysadmin (not Caregiver) | $\square$ | $\square$ |
| Form validates email format | $\square$ | $\square$ |
| Success message appears after sending | $\square$ | $\square$ |
| New user appears in the list with "Pending" status | $\square$ | $\square$ |
| Invitation email is sent to the user | $\square$ | $\square$ |

### Edit User Details

**Steps:**

1. Click on an existing user in the list
2. Click **Edit** on their detail modal
3. Change the first name or last name
4. Click **Save Changes**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Edit button is visible on user details | $\square$ | $\square$ |
| Can modify first name and last name | $\square$ | $\square$ |
| Email and role cannot be changed | $\square$ | $\square$ |
| Changes save successfully | $\square$ | $\square$ |
| Updated name appears in the user list | $\square$ | $\square$ |

### Deactivate and Reactivate a User

**Steps:**

1. Click on an active user
2. Click **Deactivate**
3. Confirm the deactivation
4. Later, click **Reactivate** to restore access

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Deactivate button is visible for active users | $\square$ | $\square$ |
| Confirmation prompt appears before deactivating | $\square$ | $\square$ |
| User status changes to "Inactive" | $\square$ | $\square$ |
| Reactivate button appears for inactive users | $\square$ | $\square$ |
| User can be reactivated successfully | $\square$ | $\square$ |

### Resend Invitation

**Steps:**

1. Find a user with "Pending" status (invitation not yet accepted)
2. Click on the user to view details
3. Click **Resend Invitation**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Resend Invitation button is visible for pending users | $\square$ | $\square$ |
| Success message confirms invitation was resent | $\square$ | $\square$ |
| New invitation email is sent | $\square$ | $\square$ |

### Delete a User (Admin Only)

**Note:** Only Admin users can delete other users. Sysadmins cannot delete users.

**Steps:**

1. Click on a user to view details
2. Click **Delete** (if visible)
3. Confirm the deletion

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Delete button is visible for Admin users | $\square$ | $\square$ |
| Delete button is NOT visible for Sysadmin users | $\square$ | $\square$ |
| Confirmation prompt warns about permanent deletion | $\square$ | $\square$ |
| User is removed from the list after deletion | $\square$ | $\square$ |
| Cannot delete yourself | $\square$ | $\square$ |

### Reset User MFA (Sysadmin Only)

**Note:** Only Sysadmin users can reset MFA for other users. This removes all passkeys and forces the user to set up new ones.

**Steps:**

1. As Sysadmin, click on a user to view details
2. Click **Reset MFA**
3. Enter a reason for the reset
4. Enter your email for verification
5. Confirm the reset

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Reset MFA button is visible for Sysadmin users only | $\square$ | $\square$ |
| Reset MFA button is NOT visible for Admin users | $\square$ | $\square$ |
| Must enter a reason for the reset | $\square$ | $\square$ |
| Must confirm your email address | $\square$ | $\square$ |
| Reset completes successfully with confirmation | $\square$ | $\square$ |
| User's passkeys are cleared (they must set up new ones) | $\square$ | $\square$ |

### Developer Tools (Sysadmin Only, Development Environment)

**Note:** This section is only visible for Sysadmin users in the development environment. It is not available in production.

**Steps:**

1. As Sysadmin, click on the **Developer Tools** card
2. Observe the available options

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Developer Tools card is visible for Sysadmin in dev environment | $\square$ | $\square$ |
| Developer Tools card is NOT visible for Admin users | $\square$ | $\square$ |
| Developer Tools card is NOT visible in production environment | $\square$ | $\square$ |
| **Synthetic Data Operations** section is visible | $\square$ | $\square$ |
| **Database Statistics** section shows table counts | $\square$ | $\square$ |

### Load Synthetic Data

**Steps:**

1. In Developer Tools, click **Load Synthetic Data**
2. Observe the progress modal

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning message explains this will add test data | $\square$ | $\square$ |
| Progress modal shows loading status | $\square$ | $\square$ |
| Progress shows which entities are being loaded | $\square$ | $\square$ |
| Success message appears when complete | $\square$ | $\square$ |
| Database statistics update to reflect new data | $\square$ | $\square$ |

### Clear All Data

**Steps:**

1. In Developer Tools, click **Clear All Data**
2. Confirm the action when prompted

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning message explains this is destructive and irreversible | $\square$ | $\square$ |
| Must confirm the action with a typed confirmation | $\square$ | $\square$ |
| All data is cleared (tables show 0 records) | $\square$ | $\square$ |
| Success message appears when complete | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Help and Documentation (ADMIN-12)

The Help page provides comprehensive documentation, FAQ, guided tours, keyboard shortcuts, and accessibility information.

### Access Help Page

**Steps:**

1. Click **Help** in the left menu or the help icon (?)
2. Review the help content sections

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Help page loads successfully | $\square$ | $\square$ |
| Search bar for FAQ is visible at the top | $\square$ | $\square$ |
| **Frequently Asked Questions** section with category tabs | $\square$ | $\square$ |
| **Guided Tours** section is visible | $\square$ | $\square$ |
| **System Features Overview** cards are visible | $\square$ | $\square$ |
| **User Roles & Permissions** table is visible | $\square$ | $\square$ |
| **Best Practices** section is visible | $\square$ | $\square$ |
| **Keyboard Shortcuts** section is visible | $\square$ | $\square$ |
| **Accessibility Statement** section is visible | $\square$ | $\square$ |
| **Contact Support** section is visible | $\square$ | $\square$ |

### Browse and Search FAQ

**Steps:**

1. In the FAQ section, observe the category tabs
2. Click on different category tabs to filter questions
3. Use the search bar to find a specific topic
4. Click on a question to expand the answer

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| FAQ categories are shown as tabs (12 categories available) | $\square$ | $\square$ |
| Clicking a category tab filters the questions | $\square$ | $\square$ |
| Search bar filters questions as you type | $\square$ | $\square$ |
| Search matches both question titles and content | $\square$ | $\square$ |
| Clicking a question expands the answer | $\square$ | $\square$ |
| Clicking again collapses the answer | $\square$ | $\square$ |
| "Clear" button resets search and category filter | $\square$ | $\square$ |

### Guided Tours

**Steps:**

1. In the Guided Tours section, view available tours
2. Select a tour (e.g., "Dashboard Tour" or "Client Management Tour")
3. Click the **Restart Tour** button

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| List of available tours is displayed | $\square$ | $\square$ |
| Each tour shows a name and brief description | $\square$ | $\square$ |
| **Restart Tour** button starts the selected tour | $\square$ | $\square$ |
| Tour highlights UI elements with step-by-step explanations | $\square$ | $\square$ |
| Can navigate through tour steps (Next/Previous/Skip) | $\square$ | $\square$ |
| Can exit tour at any time | $\square$ | $\square$ |
| **Enable Auto Tours** toggle controls whether tours start automatically | $\square$ | $\square$ |

### System Features Overview

**Steps:**

1. Scroll to the System Features Overview section
2. Review the feature cards

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Feature cards display key system capabilities | $\square$ | $\square$ |
| Each card has an icon, title, and description | $\square$ | $\square$ |
| Features include: Client Management, Care Logging, Incident Reporting, etc. | $\square$ | $\square$ |

### User Roles & Permissions

**Steps:**

1. Scroll to the User Roles & Permissions section
2. Review the permissions table

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Table shows all three roles: Admin, Caregiver, Sysadmin | $\square$ | $\square$ |
| Permissions are clearly marked for each role | $\square$ | $\square$ |
| Table explains what each role can and cannot do | $\square$ | $\square$ |

### Best Practices

**Steps:**

1. Scroll to the Best Practices section
2. Review the care documentation guidance

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Best practices for care documentation are listed | $\square$ | $\square$ |
| Tips are actionable and clear | $\square$ | $\square$ |

### View Keyboard Shortcuts

**Steps:**

1. Scroll to the Keyboard Shortcuts section
2. Review available shortcuts
3. Test a shortcut (e.g., Ctrl/Cmd + K for quick search)

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Table of keyboard shortcuts is displayed | $\square$ | $\square$ |
| Shortcuts show both Windows/Linux and Mac versions | $\square$ | $\square$ |
| Testing a shortcut performs the expected action | $\square$ | $\square$ |
| Shortcut for opening help (Ctrl/Cmd + /) works | $\square$ | $\square$ |

### Accessibility Statement

**Steps:**

1. Scroll to the Accessibility Statement section
2. Review the WCAG compliance information

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Accessibility statement is displayed | $\square$ | $\square$ |
| WCAG 2.1 AA compliance is mentioned | $\square$ | $\square$ |
| Contact information for accessibility concerns is provided | $\square$ | $\square$ |

### Contact Support

**Steps:**

1. Scroll to the Contact Support section
2. Review contact options

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Support contact information is displayed | $\square$ | $\square$ |
| Email or phone contact options are available | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

# Test Scenarios for Caregiver Users

**Log out as Admin, then log in with the Caregiver account.**

**Important:** Caregivers can only see clients in homes they are assigned to. This is called **home-scoped access**.

## Caregiver Dashboard (CG-01)

**Steps:**

1. After logging in as caregiver, observe the dashboard

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Dashboard loads successfully | $\square$ | $\square$ |
| Dashboard shows **Assigned Homes** count | $\square$ | $\square$ |
| Dashboard shows **Active Clients** count (only in assigned homes) | $\square$ | $\square$ |
| **My Assigned Homes** section lists only homes you're assigned to | $\square$ | $\square$ |
| **Upcoming Appointments** shows appointments for clients in assigned homes | $\square$ | $\square$ |
| **My Clients** section shows clients in your assigned homes | $\square$ | $\square$ |
| Each home shows the number of active clients as a badge | $\square$ | $\square$ |
| Clicking on a client name opens their profile | $\square$ | $\square$ |
| **Refresh** button updates the dashboard data | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Viewing Clients (CG-02)

**Steps:**

1. Click **Clients** in the left menu
2. Review the clients shown

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Only clients in your assigned homes are visible | $\square$ | $\square$ |
| Cannot see clients from homes you're not assigned to | $\square$ | $\square$ |
| Can click on a client to view their profile | $\square$ | $\square$ |
| Home filter (if available) only shows your assigned homes | $\square$ | $\square$ |

### View Client Profile

**Steps:**

1. Click on a client to view their profile
2. Review the information available

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can see **Overview** tab with client info | $\square$ | $\square$ |
| Can see **Care Log** tab with all sub-tabs (ADL, Vitals, Medications, ROM, Behavior, Activities, Timeline) | $\square$ | $\square$ |
| Can see **Incidents** tab | $\square$ | $\square$ |
| Can see **Appointments** tab | $\square$ | $\square$ |
| Can see **Documents** tab | $\square$ | $\square$ |
| **Cannot** see Edit button (Admin only) | $\square$ | $\square$ |
| **Cannot** see Transfer button (Admin only) | $\square$ | $\square$ |
| **Cannot** see Discharge button (Admin only) | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Logging Care Activities (CG-03)

Caregivers can log all types of care activities for clients in their assigned homes.

### Access Care Log

**Steps:**

1. Go to a client's profile
2. Click on the **Care Log** tab
3. Observe the sub-tabs: ADL, Vitals, Medications, ROM, Behavior, Activities, Timeline

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Care Log tab is accessible | $\square$ | $\square$ |
| All sub-tabs are visible | $\square$ | $\square$ |
| **Quick Log** button is visible and functional | $\square$ | $\square$ |
| Historical entries are visible in each sub-tab | $\square$ | $\square$ |

### Log ADL (Activities of Daily Living)

**Steps:**

1. Click **Quick Log** button
2. Select the **ADL** tab in the modal
3. For each ADL category, select the appropriate assistance level
4. Add any notes
5. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can access ADL form | $\square$ | $\square$ |
| All 6 categories visible: Bathing, Dressing, Toileting, Transferring, Continence, Feeding | $\square$ | $\square$ |
| Can select assistance levels for each ADL | $\square$ | $\square$ |
| Notes field accepts text | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in the ADL sub-tab and Timeline | $\square$ | $\square$ |
| Entry shows your name as the logger | $\square$ | $\square$ |

### Log Vitals

**Steps:**

1. Click **Quick Log** and select **Vitals**
2. Enter: blood pressure, pulse, temperature, oxygen level
3. Optionally enter: weight, blood sugar
4. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can enter all vital measurements | $\square$ | $\square$ |
| Form shows appropriate units | $\square$ | $\square$ |
| Form validates reasonable values | $\square$ | $\square$ |
| Entry saves and appears in Vitals sub-tab | $\square$ | $\square$ |

### Log Medications

**Steps:**

1. Click **Quick Log** and select **Medications**
2. Enter medication name, dosage, route, time given
3. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can enter medication details | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in Medications sub-tab | $\square$ | $\square$ |

### Log ROM Exercises

**Steps:**

1. Click **Quick Log** and select **ROM**
2. Enter activity description (e.g., "Passive ROM - Upper Extremities")
3. Enter duration (in minutes) and repetitions (optional)
4. Add notes if needed
5. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can enter activity description | $\square$ | $\square$ |
| Can enter duration and repetitions | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in ROM sub-tab | $\square$ | $\square$ |

### Log Behavior Notes

**Steps:**

1. Click **Quick Log** and select **Behavior**
2. Select Category (Behavior, Mood, or General)
3. Select Severity (Low, Medium, or High)
4. Enter detailed notes in the text area
5. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can select category from dropdown | $\square$ | $\square$ |
| Can select severity from dropdown | $\square$ | $\square$ |
| Can enter detailed notes | $\square$ | $\square$ |
| Notes save successfully | $\square$ | $\square$ |
| Notes appear in Behavior sub-tab | $\square$ | $\square$ |

### Log Activities

**Steps:**

1. Click **Quick Log** and select **Activities**
2. Enter activity name and description
3. Select category (Recreational, Social, Exercise, or Other)
4. Enter start time and duration
5. Check "Group Activity" if applicable and select participants
6. Click **Save**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can enter activity name and description | $\square$ | $\square$ |
| Can select activity category | $\square$ | $\square$ |
| Can toggle Group Activity checkbox | $\square$ | $\square$ |
| Entry saves successfully | $\square$ | $\square$ |
| Entry appears in Activities sub-tab | $\square$ | $\square$ |

### View Timeline

**Steps:**

1. Click on the **Timeline** sub-tab

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Timeline shows all your logged entries | $\square$ | $\square$ |
| Entries from other caregivers are also visible | $\square$ | $\square$ |
| Entries are in chronological order | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Reporting Incidents (CG-04)

Caregivers can report incidents for clients in their assigned homes.

**Steps:**

1. Go to a client's profile
2. Click on the **Incidents** tab
3. Click **Report Incident**
4. Fill in the form (client is pre-selected):
   - Incident Type (Fall, Medication, Behavioral, Medical, Injury, Elopement, Other)
   - Severity (1-5 slider: 1=Minor, 3=Moderate, 5=Severe)
   - Date/Time of incident
   - Location where it occurred
   - Description of what happened
   - Actions Taken
   - Witnesses (optional)
5. Click **Submit Report** or **Save as Draft**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can access incident form | $\square$ | $\square$ |
| Client is pre-selected (cannot change from this context) | $\square$ | $\square$ |
| All required fields are clearly marked | $\square$ | $\square$ |
| Can select incident type and severity | $\square$ | $\square$ |
| Location field accepts free text | $\square$ | $\square$ |
| Incident is created successfully | $\square$ | $\square$ |
| Incident appears in the client's Incidents tab | $\square$ | $\square$ |
| Incident shows your name as the reporter | $\square$ | $\square$ |

### View Incident Details

**Steps:**

1. Click on an existing incident to view details

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can view incident details | $\square$ | $\square$ |
| Can see status and any follow-up notes added by Admin | $\square$ | $\square$ |
| **Cannot** change incident status (Admin only) | $\square$ | $\square$ |
| **Cannot** add follow-up notes (Admin only) | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Viewing Appointments (CG-05)

Caregivers can view and schedule appointments for clients in their assigned homes.

### View Client Appointments

**Steps:**

1. Go to a client's profile
2. Click on the **Appointments** tab

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can see list of appointments for this client | $\square$ | $\square$ |
| Appointments show: Date, Time, Title, Type, Status | $\square$ | $\square$ |
| Upcoming appointments are highlighted | $\square$ | $\square$ |
| Past appointments are visible with appropriate status | $\square$ | $\square$ |

### Schedule an Appointment (if permitted)

**Steps:**

1. On client's Appointments tab, click **Schedule Appointment**
2. Fill in appointment details
3. Click **Schedule**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Schedule button is visible (if caregivers have permission) | $\square$ | $\square$ |
| Can enter appointment details | $\square$ | $\square$ |
| Appointment is created successfully | $\square$ | $\square$ |
| Appointment appears in the list | $\square$ | $\square$ |

**Note:** Some organizations may restrict appointment scheduling to Admins only. Verify expected behavior with your administrator.

### View Appointments from Main Menu

**Steps:**

1. Click **Appointments** in the left menu (if available)

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Only appointments for clients in assigned homes are shown | $\square$ | $\square$ |
| Cannot see appointments for clients in other homes | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Viewing Documents (CG-06)

Caregivers have view-only access to permitted documents **through client profile tabs only**. The main Documents page is Admin-only. Caregivers cannot upload or download documents.

### View Client Documents

**Steps:**

1. Go to a client's profile
2. Click on the **Documents** tab
3. Try to view a document

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can see list of documents (only those you have permission for) | $\square$ | $\square$ |
| Can click on a document to view it | $\square$ | $\square$ |
| Document viewer opens and displays the content | $\square$ | $\square$ |
| **Cannot** download documents (view only) | $\square$ | $\square$ |
| **Cannot** see documents you don't have permission for | $\square$ | $\square$ |
| **Cannot** upload new documents (Admin only) | $\square$ | $\square$ |
| **Cannot** delete documents (Admin only) | $\square$ | $\square$ |

### Main Documents Page Access

**Steps:**

1. Click **Documents** in the left menu
2. Observe what happens

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Main Documents page shows access denied or redirects | $\square$ | $\square$ |
| You are informed that this feature requires Admin access | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Access Restrictions (CG-07)

Verify that as a Caregiver you **cannot** access Admin-only features. This is critical for HIPAA compliance.

| Feature | How to Test | Expected Result | Pass | Fail |
|:--------|:------------|:----------------|:----:|:----:|
| Homes Management | Try to click "Homes" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Add/Edit Home | Look for "Add Home" or "Edit" buttons | Buttons should not exist | $\square$ | $\square$ |
| Caregivers Management | Try to click "Caregivers" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Documents Page | Click "Documents" in menu | Should show access denied (use client profile Documents tab instead) | $\square$ | $\square$ |
| Reports | Try to click "Reports" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Audit Logs | Try to click "Audit Logs" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| User Management | Go to Settings, look for User Management card | Card should not exist | $\square$ | $\square$ |
| Developer Tools | Go to Settings, look for Developer Tools card | Card should not exist | $\square$ | $\square$ |
| Edit Client | On client profile, look for Edit button | Button should not exist | $\square$ | $\square$ |
| Admit Client | On clients page, look for "Admit Client" button | Button should not exist | $\square$ | $\square$ |
| Transfer Client | On client profile, look for Transfer button | Button should not exist | $\square$ | $\square$ |
| Discharge Client | On client profile, look for Discharge button | Button should not exist | $\square$ | $\square$ |
| Upload Documents | On Documents tab, look for Upload button | Button should not exist | $\square$ | $\square$ |
| Download Documents | When viewing a document, look for Download button | Button should not exist | $\square$ | $\square$ |
| Delete Documents | On Documents tab, look for Delete option | Option should not exist | $\square$ | $\square$ |
| Review Incidents | On incident details, look for status change options | Options should not exist | $\square$ | $\square$ |
| View Other Homes' Clients | Try direct URL to client in non-assigned home | Should show access denied | $\square$ | $\square$ |

### URL Manipulation Test

Try accessing restricted pages directly by typing URLs:

**Steps:**

1. Note the URL pattern for the homes page (e.g., `/homes`)
2. Try navigating directly to `/homes` in the browser address bar
3. Try accessing a client from a non-assigned home by guessing their URL

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Direct URL to `/homes` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/caregivers` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/documents` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/audit-logs` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/reports` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to client in non-assigned home shows access denied | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Caregiver Settings (CG-08)

Caregivers can manage their own profile and security settings through a card-based menu.

### Access Settings

**Steps:**

1. Click on your profile icon/name
2. Click **Settings**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Settings page loads with card menu | $\square$ | $\square$ |
| Profile card is visible and accessible | $\square$ | $\square$ |
| Security card is visible and accessible | $\square$ | $\square$ |
| Notifications card is visible and accessible | $\square$ | $\square$ |
| User Management card is **not** visible (Admin and Sysadmin only) | $\square$ | $\square$ |
| Developer Tools card is **not** visible (Sysadmin only) | $\square$ | $\square$ |

### View Profile

**Steps:**

1. Click on the **Profile** card
2. View your profile information

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Profile page loads | $\square$ | $\square$ |
| Current profile information is displayed | $\square$ | $\square$ |
| Profile fields are read-only (editing coming in future update) | $\square$ | $\square$ |

### Manage Passkeys

**Steps:**

1. Click on the **Security** card
2. View your registered passkeys
3. Add a new passkey (if testing multiple devices)

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can view registered passkeys in a table | $\square$ | $\square$ |
| Can add new passkey | $\square$ | $\square$ |
| Can rename existing passkey | $\square$ | $\square$ |
| Backup Codes section is **not** visible (Sysadmin only) | $\square$ | $\square$ |
| Password change is shown as coming in a future update | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

# Note on Sysadmin Testing

The **Sysadmin** role is designed for system maintenance personnel who should **not** have access to Protected Health Information (PHI). The following section provides detailed test scenarios for Sysadmin users.

\newpage

# Test Scenarios for Sysadmin Users

**Log out and log in with the Sysadmin account.**

**Important:** Sysadmins can manage user accounts and view audit logs but **cannot access any PHI** (clients, homes, caregivers, documents, reports, appointments, or incidents).

## Sysadmin Dashboard (SYS-01)

**Steps:**

1. After logging in as Sysadmin, observe the dashboard

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Dashboard loads successfully | $\square$ | $\square$ |
| Dashboard does NOT show client or home statistics | $\square$ | $\square$ |
| Dashboard shows system-level information only | $\square$ | $\square$ |
| No PHI is visible on the dashboard | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Audit Logs (SYS-02)

Sysadmins have full access to audit logs and default to the Technical view.

### View Technical Logs

**Steps:**

1. Click **Audit Logs** in the left menu
2. Observe the default view

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Audit logs page loads successfully | $\square$ | $\square$ |
| Defaults to **Technical** view (not Activity Feed) | $\square$ | $\square$ |
| Statistics cards show: Total Events, Login Events, Failed Logins | $\square$ | $\square$ |
| Technical table shows: Timestamp, Action, User, Resource Type, Outcome | $\square$ | $\square$ |
| Can click on an entry to see full details | $\square$ | $\square$ |
| Detail modal shows complete audit information | $\square$ | $\square$ |

### Filter Audit Logs

**Steps:**

1. Use the search field to search for specific text
2. Apply filters for action type, resource type, outcome, and date range
3. Click **Advanced Filters** to expand additional options

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Search filters results by keyword | $\square$ | $\square$ |
| Action type filter works correctly | $\square$ | $\square$ |
| Resource type filter works correctly | $\square$ | $\square$ |
| Outcome filter (Success/Failure/Denied) works | $\square$ | $\square$ |
| Date range filter works correctly | $\square$ | $\square$ |
| Advanced Filters section expands/collapses | $\square$ | $\square$ |
| Clear button resets all filters | $\square$ | $\square$ |

### Export Audit Logs

**Steps:**

1. Apply desired filters
2. Click **Export to CSV**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Export button is visible and accessible | $\square$ | $\square$ |
| CSV file downloads successfully | $\square$ | $\square$ |
| CSV contains all filtered data | $\square$ | $\square$ |
| CSV filename includes date range | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## User Management (SYS-03)

Sysadmins can manage users including inviting new Admin/Sysadmin users and resetting MFA.

### Access User Management

**Steps:**

1. Click **Settings** in the menu
2. Click on the **User Management** card

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| User Management card is visible | $\square$ | $\square$ |
| List of all users is displayed | $\square$ | $\square$ |
| Can search users by name or email | $\square$ | $\square$ |
| Invite User button is visible | $\square$ | $\square$ |

### Invite Admin or Sysadmin

**Steps:**

1. Click **Invite User**
2. Fill in email, first name, last name
3. Select role (Admin or Sysadmin)
4. Click **Send Invitation**

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can invite Admin users | $\square$ | $\square$ |
| Can invite Sysadmin users | $\square$ | $\square$ |
| Cannot invite Caregiver from this page (use Caregivers page) | $\square$ | $\square$ |
| Invitation is sent successfully | $\square$ | $\square$ |

### Reset User MFA

**Note:** Only Sysadmins can reset MFA for other users.

**Steps:**

1. Click on a user to view their details
2. Click **Reset MFA**
3. Enter a reason for the reset
4. Enter your email for verification
5. Confirm the reset

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Reset MFA button is visible | $\square$ | $\square$ |
| Must enter a reason for the reset | $\square$ | $\square$ |
| Must confirm email address | $\square$ | $\square$ |
| Reset completes successfully | $\square$ | $\square$ |
| User's passkeys are cleared | $\square$ | $\square$ |

### User Deactivation and Reactivation

**Steps:**

1. Click on an active user
2. Click **Deactivate**
3. Confirm deactivation
4. Later, click **Reactivate** to restore

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Can deactivate active users | $\square$ | $\square$ |
| Can reactivate inactive users | $\square$ | $\square$ |
| Status updates correctly | $\square$ | $\square$ |

### Delete User Restriction

**Steps:**

1. Click on a user to view their details
2. Look for a Delete button

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Delete button is NOT visible for Sysadmin | $\square$ | $\square$ |
| Only Admin users can delete users | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Security Settings (SYS-04)

Sysadmins have access to backup codes in addition to standard passkey management.

### Manage Passkeys

**Steps:**

1. Go to **Settings > Security**
2. View registered passkeys

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Passkeys table shows registered devices | $\square$ | $\square$ |
| Can add new passkey | $\square$ | $\square$ |
| Can rename existing passkey | $\square$ | $\square$ |
| Cannot delete last remaining passkey | $\square$ | $\square$ |

### Backup Codes

**Steps:**

1. On Security page, find the **Backup Codes** section
2. Click **Regenerate Codes**
3. Type "regenerate" to confirm
4. Save the new codes securely

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Backup Codes section is visible (Sysadmin only) | $\square$ | $\square$ |
| Can regenerate backup codes | $\square$ | $\square$ |
| Must type confirmation word | $\square$ | $\square$ |
| New codes are displayed (8-character format) | $\square$ | $\square$ |
| Warning about saving codes securely is shown | $\square$ | $\square$ |
| Previous codes are invalidated | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## Developer Tools (SYS-05)

**Note:** Developer Tools are only available in the development environment, not production.

### Access Developer Tools

**Steps:**

1. Go to **Settings**
2. Look for the **Developer Tools** card

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Developer Tools card is visible in dev environment | $\square$ | $\square$ |
| Developer Tools card is NOT visible in production | $\square$ | $\square$ |
| Page shows Synthetic Data Operations section | $\square$ | $\square$ |
| Page shows Database Statistics section | $\square$ | $\square$ |

### Load Synthetic Data

**Steps:**

1. Click **Load Synthetic Data**
2. Observe the progress modal

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning explains this will add test data | $\square$ | $\square$ |
| Progress modal shows loading status | $\square$ | $\square$ |
| Progress indicates which entities are being loaded | $\square$ | $\square$ |
| Success message appears when complete | $\square$ | $\square$ |
| Database statistics update after loading | $\square$ | $\square$ |

### View Database Statistics

**Steps:**

1. Review the Database Statistics section

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Table counts are displayed for each entity | $\square$ | $\square$ |
| Refresh button updates statistics | $\square$ | $\square$ |

### Clear All Data

**Steps:**

1. Click **Clear All Data**
2. Type confirmation when prompted
3. Confirm the action

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Warning explains this is destructive | $\square$ | $\square$ |
| Must type confirmation to proceed | $\square$ | $\square$ |
| All data is cleared | $\square$ | $\square$ |
| Database statistics show 0 records | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

## PHI Access Restrictions (SYS-06)

Verify that Sysadmins **cannot** access PHI-related pages. This is critical for HIPAA compliance.

| Feature | How to Test | Expected Result | Pass | Fail |
|:--------|:------------|:----------------|:----:|:----:|
| Homes | Click "Homes" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Clients | Click "Clients" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Caregivers | Click "Caregivers" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Documents | Click "Documents" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Reports | Click "Reports" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Incidents | Click "Incidents" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |
| Appointments | Click "Appointments" in menu | Should not appear in menu or show access denied | $\square$ | $\square$ |

### URL Manipulation Test

Try accessing restricted pages directly by typing URLs:

**Steps:**

1. Try navigating directly to `/clients` in the browser address bar
2. Try navigating to `/homes`, `/caregivers`, `/documents`, `/reports`, `/incidents`, `/appointments`

**What to Check:**

| Item | Pass | Fail |
|:-----|:----:|:----:|
| Direct URL to `/clients` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/homes` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/caregivers` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/documents` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/reports` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/incidents` shows access denied or redirects | $\square$ | $\square$ |
| Direct URL to `/appointments` shows access denied or redirects | $\square$ | $\square$ |

### Notes

\vspace{2cm}

\newpage

# Reporting Issues

## How to Report a Problem

When you find an issue, please document it with:

1. **Test ID** -- Which test scenario you were performing (e.g., ADMIN-03, CG-05)
2. **Description** -- What went wrong
3. **Steps to Reproduce** -- Exactly what you did, step by step
4. **Expected Result** -- What should have happened
5. **Actual Result** -- What actually happened
6. **Browser/Device** -- What browser and device you were using
7. **Screenshot** -- If possible, take a screenshot (use Print Screen or Cmd+Shift+4 on Mac)

## Issue Severity Levels

| Severity     | Description                                    | Example                                      |
|:-------------|:-----------------------------------------------|:---------------------------------------------|
| **Critical** | System is unusable, data is lost, or PHI exposed | Cannot log in, data not saving, security breach |
| **High**     | Major feature not working, no workaround       | Cannot admit clients, reports not generating, passkey fails |
| **Medium**   | Feature partially working or workaround exists | Button placement wrong, slow loading, minor display issues |
| **Low**      | Cosmetic issues or suggestions                 | Typos, color preferences, enhancement requests |

## Issue Template

Use this template when reporting issues:

```
Test ID: [e.g., ADMIN-03]
Severity: [Critical/High/Medium/Low]
Summary: [Brief description]

Steps to Reproduce:
1. 
2. 
3. 

Expected Result:
[What should happen]

Actual Result:
[What actually happened]

Browser: [e.g., Chrome 120, Safari 17, Edge 120]
Device: [e.g., Windows 11 laptop, MacBook Pro, iPad]

Additional Notes:
[Any other observations, screenshots attached, etc.]
```

## Known Limitations

The following are known limitations in the staging environment:

- Email delivery may be delayed in staging environment
- Synthetic test data may have inconsistent dates
- Some features may be in development and marked as "Coming Soon"

\newpage

# Sign-Off

## Tester Information

| Field           | Value |
|:----------------|:------|
| Tester Name     |       |
| Testing Date(s) |       |
| Browser Used    |       |
| Device/OS Used  |       |

## Test Summary

### Admin Test Scenarios

| Section                       | Tests Passed | Tests Failed | Notes |
|:------------------------------|:-------------|:-------------|:------|
| Dashboard (ADMIN-01)          |              |              |       |
| Home Management (ADMIN-02)    |              |              |       |
| Client Management (ADMIN-03)  |              |              |       |
| Caregiver Management (ADMIN-04) |            |              |       |
| Care Log Activities (ADMIN-05) |             |              |       |
| Appointment Management (ADMIN-06) |          |              |       |
| Incident Reporting (ADMIN-07) |              |              |       |
| Document Management (ADMIN-08) |             |              |       |
| Reports (ADMIN-09)            |              |              |       |
| Audit Logs (ADMIN-10)         |              |              |       |
| Settings (ADMIN-11)           |              |              |       |
| Help and Documentation (ADMIN-12) |          |              |       |

### Caregiver Test Scenarios

| Section                       | Tests Passed | Tests Failed | Notes |
|:------------------------------|:-------------|:-------------|:------|
| Dashboard (CG-01)             |              |              |       |
| Viewing Clients (CG-02)       |              |              |       |
| Logging Care Activities (CG-03) |            |              |       |
| Reporting Incidents (CG-04)   |              |              |       |
| Viewing Appointments (CG-05)  |              |              |       |
| Viewing Documents (CG-06)     |              |              |       |
| Access Restrictions (CG-07)   |              |              |       |
| Settings (CG-08)              |              |              |       |

### Sysadmin Test Scenarios

| Section                       | Tests Passed | Tests Failed | Notes |
|:------------------------------|:-------------|:-------------|:------|
| Dashboard (SYS-01)            |              |              |       |
| Audit Logs (SYS-02)           |              |              |       |
| User Management (SYS-03)      |              |              |       |
| Security Settings (SYS-04)    |              |              |       |
| Developer Tools (SYS-05)      |              |              |       |
| PHI Access Restrictions (SYS-06) |           |              |       |

## Overall Assessment

Mark one:

| Assessment | Check |
|:-----------|:-----:|
| **Ready for Production** -- All tests passed, no critical or high issues | $\square$ |
| **Needs Minor Fixes** -- Some medium/low issues to address | $\square$ |
| **Needs Major Fixes** -- Critical or high issues must be resolved first | $\square$ |

## HIPAA Compliance Verification

| Requirement | Verified | Notes |
|:------------|:--------:|:------|
| Caregivers can only access their assigned homes | $\square$ |       |
| PHI is not accessible to unauthorized users | $\square$ |       |
| All actions are logged in audit logs | $\square$ |       |
| Passkey authentication works correctly | $\square$ |       |
| Session times out after inactivity | $\square$ |       |
| No PHI visible in URLs or browser history | $\square$ |       |

## Comments

Overall feedback and observations:

\vspace{4cm}

## Signatures

| Role         | Name | Signature | Date |
|:-------------|:-----|:----------|:-----|
| Tester       |      |           |      |
| Project Lead |      |           |      |
| Security Officer (if applicable) | | |    |

\newpage

# Appendix A: Quick Reference - Navigation

| Menu Item    | Who Can Access       | What It Does                       |
|:-------------|:---------------------|:-----------------------------------|
| Dashboard    | Everyone             | Shows overview, statistics, upcoming birthdays and appointments |
| Homes        | Admin only           | Manage adult family homes and beds |
| Clients      | Admin and Caregivers | View and manage residents (caregivers: assigned homes only) |
| Caregivers   | Admin only           | Manage staff, invitations, and home assignments |
| Appointments | Admin and Caregivers | View and schedule medical appointments |
| Incidents    | Admin and Caregivers | View and create incident reports   |
| Documents    | Admin (page); Caregivers (client profile tab only) | View and manage documents |
| Reports      | Admin only           | Generate PDF reports for clients or homes |
| Audit Logs   | Admin and Sysadmin   | View system activity logs with Activity Feed and Technical views |
| Settings     | Everyone             | Personal profile, security (passkeys), and notifications |
| Help         | Everyone             | FAQ, guided tours, keyboard shortcuts |

## Settings Access by Role

| Settings Section   | Admin | Caregiver | Sysadmin |
|:-------------------|:-----:|:---------:|:--------:|
| Profile            | Yes     | Yes         | Yes        |
| Security           | Yes     | Yes         | Yes        |
| Notifications      | Yes     | Yes         | Yes        |
| User Management    | Yes     | No         | Yes        |
| Developer Tools    | No     | No         | Yes (dev only) |
| Backup Codes       | No     | No         | Yes        |

## Document Scopes

| Scope     | Description                                | Who Can Access |
|:----------|:-------------------------------------------|:---------------|
| Client    | Documents for a specific client            | Admin (full), Caregivers (view, if permitted) |
| Home      | Documents for a specific home              | Admin (full), Assigned Caregivers (view) |
| Business  | Business-wide documents                    | Admin only     |
| General   | General documents for all staff            | Everyone       |

## Document Types

| Type           | Description                                         |
|:---------------|:----------------------------------------------------|
| **Care Plan**  | Client care plans and service agreements            |
| **Medical**    | Medical records, physician orders, lab results      |
| **Legal**      | Legal documents, guardianship papers, POA           |
| **Financial**  | Financial records, billing documents                |
| **Assessment** | Assessment forms, evaluations                       |
| **Photo**      | Photos and images                                   |
| **Other**      | Any document that doesn't fit above categories      |

\newpage

# Appendix B: ADL Categories

## Activities of Daily Living

| Category         | Description                                             |
|:-----------------|:--------------------------------------------------------|
| **Bathing**      | Getting into/out of shower or tub, washing body         |
| **Dressing**     | Putting on and taking off clothing, including fasteners |
| **Toileting**    | Getting to toilet, using toilet, cleaning self          |
| **Transferring** | Moving between bed, chair, wheelchair, standing         |
| **Continence**   | Control of bladder and bowel                            |
| **Feeding**      | Getting food from plate to mouth, chewing, swallowing   |

## Assistance Levels

| Level                    | Display Color | Description                             |
|:-------------------------|:--------------|:----------------------------------------|
| **No Assistance**        | Green         | Client does task alone without help     |
| **Some Assistance**      | Orange        | Client needs partial help with task     |
| **Full Assistance**      | Red           | Client cannot do task, staff does 100%  |

\newpage

# Appendix C: Vital Signs Reference

## Normal Ranges (General Adult)

| Vital Sign           | Normal Range               | Unit        | Accepted Range in System |
|:---------------------|:---------------------------|:------------|:-------------------------|
| **Blood Pressure**   | 90/60 - 120/80             | mmHg        | Systolic: 50-300, Diastolic: 30-200 |
| **Pulse/Heart Rate** | 60 - 100                   | bpm         | 30-200 |
| **Oxygen Saturation**| 95 - 100                   | %           | 70-100 |
| **Temperature (°F)** | 97.8 - 99.1                | °F          | 90-110 |
| **Temperature (°C)** | 36.6 - 37.3                | °C          | 32-43 |

**Note:** Normal ranges may vary for elderly patients. Always follow physician guidelines for individual clients. The system displays color-coded status tags (green = normal, orange = borderline, red = abnormal) based on these ranges.

\newpage

# Appendix D: Incident Types and Severity

## Incident Types

| Type                 | Description                                  |
|:---------------------|:---------------------------------------------|
| **Fall**             | Client fell or slipped                       |
| **Medication**       | Medication error or adverse reaction         |
| **Behavioral**       | Aggressive or disruptive behavior            |
| **Medical**          | Medical emergency or health concern          |
| **Injury**           | Physical injury from any cause               |
| **Elopement**        | Client wandered or left unsupervised         |
| **Other**            | Any event that doesn't fit above types       |

## Severity Scale (1-5)

| Level | Label    | Description                                  |
|:------|:---------|:---------------------------------------------|
| **1** | Minor    | No injury or minimal impact                  |
| **2** |          | Low concern                                  |
| **3** | Moderate | Minor injury or significant concern          |
| **4** |          | High concern                                 |
| **5** | Severe   | Serious injury or requires emergency response|

\newpage

# Appendix E: Appointment Types

| Type                  | Description                                  |
|:----------------------|:---------------------------------------------|
| **General Practice**  | Primary care physician visits, follow-ups    |
| **Dental**            | Dental checkups, procedures                  |
| **Ophthalmology**     | Eye exams, vision care                       |
| **Podiatry**          | Foot care, podiatrist visits                 |
| **Physical Therapy**  | PT sessions, rehabilitation                  |
| **Occupational Therapy** | OT sessions, daily living skills          |
| **Speech Therapy**    | Speech and language therapy sessions         |
| **Psychiatry**        | Mental health appointments                   |
| **Dermatology**       | Skin care, dermatologist visits              |
| **Cardiology**        | Heart health, cardiologist visits            |
| **Neurology**         | Neurological care, neurologist visits        |
| **Lab Work**          | Blood work, lab tests                        |
| **Imaging**           | X-rays, MRI, CT scans                        |
| **Audiology**         | Hearing tests, audiologist visits            |
| **Social Worker**     | Social services meetings                     |
| **Family Visit**      | Scheduled family visits                      |
| **Other**             | Any appointment that doesn't fit above       |

\newpage

# Appendix F: Keyboard Shortcuts

| Shortcut (Windows/Linux) | Shortcut (Mac) | Action                    |
|:-------------------------|:---------------|:--------------------------|
| `Ctrl + K`               | `Cmd + K`      | Open quick search         |
| `Ctrl + /`               | `Cmd + /`      | Show keyboard shortcuts   |
| `G then D`               | `G then D`     | Go to Dashboard           |
| `G then C`               | `G then C`     | Go to Clients             |
| `G then H`               | `G then H`     | Go to Homes               |
| `Escape`                 | `Escape`       | Close modal/dialog        |

**Note:** Keyboard shortcuts may vary. Check the Help page for the current list.

\newpage

# Appendix G: Glossary

| Term                 | Definition                                                    |
|:---------------------|:--------------------------------------------------------------|
| **ADL**              | Activities of Daily Living - basic self-care tasks            |
| **Caregiver**        | Staff member providing care to clients                        |
| **Client**           | Resident/patient living in an adult family home               |
| **Discharge**        | Process of ending a client's stay at a home                   |
| **Home**             | Adult family home facility                                    |
| **Home-scoped**      | Access restricted to only data from assigned homes            |
| **Passkey**          | Biometric authentication (fingerprint, face, security key)    |
| **PHI**              | Protected Health Information (HIPAA term)                     |
| **Quick Log**        | Modal for rapidly entering care log entries                   |
| **ROM**              | Range of Motion - exercises to maintain joint flexibility     |
| **Sysadmin**         | System administrator with technical access only               |
| **Transfer**         | Moving a client from one bed/home to another                  |
| **UAT**              | User Acceptance Testing                                       |
| **WebAuthn/FIDO2**   | Web authentication standard used for passkeys                 |

\vspace{2cm}

---

*End of Document*
