"""
Entity generators for synthetic data creation.
Each generator creates realistic records for specific entity types.
"""

import random
import uuid
from datetime import datetime, timedelta
from typing import Optional
from faker import Faker

from config import (
    WA_CITIES, HOME_NAME_PREFIXES, HOME_NAME_SUFFIXES,
    DIAGNOSES, ALLERGIES, MEDICATIONS, ROM_EXERCISES, ACTIVITIES,
    INCIDENT_LOCATIONS, BEHAVIOR_OBSERVATIONS, PHYSICIANS,
    RELATIONSHIPS, DISCHARGE_REASONS, MEDICATION_TIMES,
    ADL_LEVELS, INCIDENT_TYPES, APPOINTMENT_TYPES, APPOINTMENT_STATUSES,
    APPOINTMENT_LOCATIONS, APPOINTMENT_DURATIONS, TRANSPORTATION_OPTIONS,
    weighted_choice, random_date_between, random_working_hour_time
)

fake = Faker()
Faker.seed(42)  # For reproducibility
random.seed(42)


class HomeGenerator:
    """Generates adult family homes with organic opening dates."""
    
    def __init__(self, start_date: datetime, end_date: datetime):
        self.start_date = start_date
        self.end_date = end_date
        self.used_names = set()
    
    def generate(self, open_date: datetime, created_by_id: Optional[str] = None) -> dict:
        """Generate a single home opening on the specified date.
        
        Args:
            open_date: When the home opened
            created_by_id: ID of the user who created the home record
        """
        # Generate unique name
        while True:
            prefix = random.choice(HOME_NAME_PREFIXES)
            suffix = random.choice(HOME_NAME_SUFFIXES)
            name = f"{prefix} {suffix}"
            if name not in self.used_names:
                self.used_names.add(name)
                break
        
        location = random.choice(WA_CITIES)
        capacity = random.choice([4, 5, 6])  # WA AFH typically 2-6 beds
        
        return {
            "id": str(uuid.uuid4()),
            "name": name,
            "address": fake.street_address(),
            "city": location["city"],
            "state": location["state"],
            "zipCode": f"{location['zip_prefix']}{random.randint(10, 99)}",
            "phoneNumber": fake.phone_number()[:14],
            "capacity": capacity,
            "isActive": True,
            "createdAt": open_date.isoformat(),
            "createdById": created_by_id,
        }
    
    def generate_beds(self, home: dict) -> list[dict]:
        """Generate beds for a home."""
        beds = []
        for i in range(home["capacity"]):
            beds.append({
                "id": str(uuid.uuid4()),
                "homeId": home["id"],
                "label": f"Room {i + 1}",
                "status": "Available",
                "isActive": True,
                "createdAt": home["createdAt"],
            })
        return beds


class UserGenerator:
    """Generates users (caregivers, admins, sysadmins)."""
    
    def __init__(self):
        self.used_emails = set()
    
    def generate_admin(self, created_at: datetime) -> dict:
        """Generate an admin user."""
        first = fake.first_name()
        last = fake.last_name()
        email = self._unique_email(first, last)
        
        return {
            "id": str(uuid.uuid4()),
            "email": email,
            "firstName": first,
            "lastName": last,
            "isActive": True,
            "isMfaSetupComplete": True,
            "invitationAccepted": True,
            "roles": ["Admin"],
            "createdAt": created_at.isoformat(),
        }
    
    def generate_sysadmin(self, created_at: datetime) -> dict:
        """Generate a sysadmin user."""
        first = fake.first_name()
        last = fake.last_name()
        email = self._unique_email(first, last)
        
        return {
            "id": str(uuid.uuid4()),
            "email": email,
            "firstName": first,
            "lastName": last,
            "isActive": True,
            "isMfaSetupComplete": True,
            "invitationAccepted": True,
            "roles": ["Sysadmin"],
            "createdAt": created_at.isoformat(),
        }
    
    def generate_caregiver(
        self,
        created_at: datetime,
        home_ids: list[str],
        assigned_by_id: Optional[str] = None,
    ) -> dict:
        """Generate a caregiver user with home assignments.
        
        Args:
            created_at: When the caregiver was created
            home_ids: List of home IDs to assign the caregiver to
            assigned_by_id: ID of the admin who assigned the caregiver
        """
        first = fake.first_name()
        last = fake.last_name()
        email = self._unique_email(first, last)
        
        caregiver = {
            "id": str(uuid.uuid4()),
            "email": email,
            "firstName": first,
            "lastName": last,
            "isActive": True,
            "isMfaSetupComplete": True,
            "invitationAccepted": True,
            "roles": ["Caregiver"],
            "createdAt": created_at.isoformat(),
        }
        
        # Generate home assignments
        assignments = []
        for home_id in home_ids:
            assignments.append({
                "id": str(uuid.uuid4()),
                "userId": caregiver["id"],
                "homeId": home_id,
                "assignedAt": created_at.isoformat(),
                "assignedById": assigned_by_id,
                "isActive": True,
            })
        caregiver["homeAssignments"] = assignments
        
        return caregiver
    
    def _unique_email(self, first: str, last: str) -> str:
        """Generate a unique email address."""
        base = f"{first.lower()}.{last.lower()}@lenkcare.example.com"
        email = base
        counter = 1
        while email in self.used_emails:
            email = f"{first.lower()}.{last.lower()}{counter}@lenkcare.example.com"
            counter += 1
        self.used_emails.add(email)
        return email


class ClientGenerator:
    """Generates client (resident) records."""
    
    def __init__(self):
        self.used_names = set()
    
    def generate(
        self,
        home_id: str,
        bed_id: str,
        admission_date: datetime,
        caregiver_id: str,
        discharged: bool = False,
        discharge_date: Optional[datetime] = None,
    ) -> dict:
        """Generate a client record."""
        gender = random.choice(["Male", "Female"])
        first = fake.first_name_male() if gender == "Male" else fake.first_name_female()
        last = fake.last_name()
        
        # Age 65-95 at admission
        age_at_admission = random.randint(65, 95)
        dob = admission_date - timedelta(days=age_at_admission * 365 + random.randint(0, 364))
        
        # Select random diagnoses (1-4)
        num_diagnoses = random.randint(1, 4)
        diagnoses = random.sample(DIAGNOSES, num_diagnoses)
        
        # Select random allergies (0-3)
        num_allergies = random.randint(0, 3)
        allergies = random.sample(ALLERGIES, num_allergies) if num_allergies > 0 else []
        
        # Select random medications (3-8)
        num_meds = random.randint(3, 8)
        meds = random.sample(MEDICATIONS, num_meds)
        med_list = [f"{m['name']} {m['dosage']}" for m in meds]
        
        # Select physician
        physician = random.choice(PHYSICIANS)
        
        # Emergency contact
        ec_first = fake.first_name()
        ec_last = last if random.random() > 0.3 else fake.last_name()
        
        client = {
            "id": str(uuid.uuid4()),
            "firstName": first,
            "lastName": last,
            "dateOfBirth": dob.date().isoformat(),
            "gender": gender,
            "admissionDate": admission_date.date().isoformat(),
            "homeId": home_id,
            "bedId": bed_id,
            "primaryPhysician": physician["name"],
            "primaryPhysicianPhone": physician["phone"],
            "emergencyContactName": f"{ec_first} {ec_last}",
            "emergencyContactPhone": fake.phone_number()[:14],
            "emergencyContactRelationship": random.choice(RELATIONSHIPS),
            "allergies": ", ".join(allergies) if allergies else None,
            "diagnoses": ", ".join(diagnoses),
            "medicationList": ", ".join(med_list),
            "isActive": not discharged,
            "createdAt": admission_date.isoformat(),
            "createdById": caregiver_id,
        }
        
        if discharged and discharge_date:
            client["dischargeDate"] = discharge_date.date().isoformat()
            client["dischargeReason"] = random.choice(DISCHARGE_REASONS)
            client["isActive"] = False
        
        # Store medications for later use in medication logs
        client["_medications"] = meds
        
        return client


class ADLLogGenerator:
    """Generates ADL (Activities of Daily Living) log entries."""
    
    def generate(
        self,
        client_id: str,
        caregiver_id: str,
        timestamp: datetime,
        independence_level: str = "Independent",
    ) -> dict:
        """Generate an ADL log entry."""
        # Adjust levels based on overall independence
        levels = self._get_levels(independence_level)
        
        return {
            "id": str(uuid.uuid4()),
            "clientId": client_id,
            "caregiverId": caregiver_id,
            "timestamp": timestamp.isoformat(),
            "bathing": random.choice(levels),
            "dressing": random.choice(levels),
            "toileting": random.choice(levels),
            "transferring": random.choice(levels),
            "continence": random.choice(levels),
            "feeding": random.choice(levels),
            "notes": self._generate_note(),
            "createdAt": timestamp.isoformat(),
        }
    
    def _get_levels(self, independence: str) -> list[str]:
        """Get ADL levels based on overall independence."""
        if independence == "Independent":
            return ["Independent", "Independent", "PartialAssist"]
        elif independence == "PartialAssist":
            return ["Independent", "PartialAssist", "PartialAssist", "Dependent"]
        else:
            return ["PartialAssist", "Dependent", "Dependent"]
    
    def _generate_note(self) -> Optional[str]:
        """Generate an optional note."""
        if random.random() > 0.7:
            notes = [
                "Resident cooperative during care.",
                "Required extra assistance today.",
                "Good day overall.",
                "Resident fatigued this morning.",
                "Completed all tasks with minimal assistance.",
            ]
            return random.choice(notes)
        return None


class VitalsLogGenerator:
    """Generates vitals log entries."""
    
    def generate(
        self,
        client_id: str,
        caregiver_id: str,
        timestamp: datetime,
        has_hypertension: bool = False,
        has_diabetes: bool = False,
    ) -> dict:
        """Generate a vitals log entry."""
        # Base vital ranges
        systolic_base = 140 if has_hypertension else 120
        systolic = systolic_base + random.randint(-15, 20)
        diastolic = 80 + random.randint(-10, 15)
        
        pulse = 72 + random.randint(-12, 18)
        temp = round(98.6 + random.uniform(-0.8, 1.2), 1)
        o2 = min(100, max(88, 97 + random.randint(-5, 3)))
        
        return {
            "id": str(uuid.uuid4()),
            "clientId": client_id,
            "caregiverId": caregiver_id,
            "timestamp": timestamp.isoformat(),
            "systolicBP": systolic,
            "diastolicBP": diastolic,
            "pulse": pulse,
            "temperature": temp,
            "temperatureUnit": "Fahrenheit",
            "oxygenSaturation": o2,
            "notes": self._generate_note(systolic, o2),
            "createdAt": timestamp.isoformat(),
        }
    
    def _generate_note(self, systolic: int, o2: int) -> Optional[str]:
        """Generate notes for abnormal vitals."""
        notes = []
        if systolic > 150:
            notes.append("Elevated BP noted, will monitor.")
        if o2 < 92:
            notes.append("O2 sat lower than usual, encouraged deep breathing.")
        if notes:
            return " ".join(notes)
        return None


class MedicationLogGenerator:
    """Generates medication administration log entries."""
    
    def generate(
        self,
        client_id: str,
        caregiver_id: str,
        timestamp: datetime,
        medication: dict,
    ) -> dict:
        """Generate a medication log entry."""
        # Most medications are administered successfully
        status_weights = [
            ("Administered", 0.92),
            ("Refused", 0.04),
            ("GivenLate", 0.02),
            ("GivenEarly", 0.01),
            ("Held", 0.01),
        ]
        status = weighted_choice(status_weights)
        
        # Pick a scheduled time and convert to full datetime
        scheduled_time_str = random.choice(MEDICATION_TIMES)
        hour, minute = map(int, scheduled_time_str.split(":"))
        scheduled_datetime = timestamp.replace(hour=hour, minute=minute, second=0, microsecond=0)
        
        return {
            "id": str(uuid.uuid4()),
            "clientId": client_id,
            "caregiverId": caregiver_id,
            "timestamp": timestamp.isoformat(),
            "medicationName": medication["name"],
            "dosage": medication["dosage"],
            "route": medication["route"],
            "status": status,
            "scheduledTime": scheduled_datetime.isoformat(),
            "notes": self._generate_note(status) if status != "Administered" else None,
            "createdAt": timestamp.isoformat(),
        }
    
    def _generate_note(self, status: str) -> str:
        """Generate note for non-standard administration."""
        if status == "Refused":
            return "Resident refused medication. Physician notified."
        elif status == "Held":
            return "Medication held per physician order."
        elif status == "GivenLate":
            return "Administered late due to schedule conflict."
        elif status == "GivenEarly":
            return "Given early per resident request."
        return ""


class ROMLogGenerator:
    """Generates ROM (Range of Motion) log entries."""
    
    def generate(
        self,
        client_id: str,
        caregiver_id: str,
        timestamp: datetime,
    ) -> dict:
        """Generate a ROM log entry."""
        exercise = random.choice(ROM_EXERCISES)
        duration = random.choice([5, 10, 15, 20])
        reps = random.choice([None, 5, 10, 15, 20])
        
        return {
            "id": str(uuid.uuid4()),
            "clientId": client_id,
            "caregiverId": caregiver_id,
            "timestamp": timestamp.isoformat(),
            "activityDescription": exercise,
            "duration": duration,
            "repetitions": reps,
            "notes": self._generate_note(),
            "createdAt": timestamp.isoformat(),
        }
    
    def _generate_note(self) -> Optional[str]:
        """Generate optional note."""
        if random.random() > 0.8:
            notes = [
                "Good range of motion maintained.",
                "Slight stiffness noted, gentle stretching performed.",
                "Resident tolerated exercises well.",
                "Modified exercises due to fatigue.",
            ]
            return random.choice(notes)
        return None


class BehaviorNoteGenerator:
    """Generates behavior notes."""
    
    def generate(
        self,
        client_id: str,
        caregiver_id: str,
        timestamp: datetime,
    ) -> dict:
        """Generate a behavior note."""
        observation = random.choice(BEHAVIOR_OBSERVATIONS)
        severity_weights = [
            ("Low", 0.70),
            ("Medium", 0.25),
            ("High", 0.05),
        ]
        severity = weighted_choice(severity_weights)
        
        return {
            "id": str(uuid.uuid4()),
            "clientId": client_id,
            "caregiverId": caregiver_id,
            "timestamp": timestamp.isoformat(),
            "category": observation["category"],
            "noteText": observation["text"],
            "severity": severity,
            "createdAt": timestamp.isoformat(),
        }


class ActivityGenerator:
    """Generates activity records."""
    
    def __init__(self):
        self.activity_counter = 0
    
    def generate(
        self,
        home_id: str,
        caregiver_id: str,
        activity_date: datetime,
        client_ids: list[str],
    ) -> dict:
        """Generate an activity record."""
        activity_info = random.choice(ACTIVITIES)
        
        # Determine participants
        if activity_info["group"]:
            # Group activity - include 2-6 clients
            num_participants = min(len(client_ids), random.randint(2, 6))
            participants = random.sample(client_ids, num_participants)
        else:
            # Individual activity
            participants = [random.choice(client_ids)]
        
        start_hour = random.randint(9, 16)
        duration_minutes = random.choice([30, 45, 60, 90])
        start_time = activity_date.replace(hour=start_hour, minute=0)
        end_time = start_time + timedelta(minutes=duration_minutes)
        
        activity_id = str(uuid.uuid4())
        created_at = activity_date
        
        activity = {
            "id": activity_id,
            "homeId": home_id,
            "activityName": activity_info["name"],
            "description": f"{activity_info['name']} activity session",
            "date": activity_date.date().isoformat(),
            "startTime": start_time.strftime("%H:%M"),
            "endTime": end_time.strftime("%H:%M"),
            "duration": duration_minutes,
            "category": activity_info["category"],
            "isGroupActivity": activity_info["group"],
            "createdById": caregiver_id,
            "createdAt": created_at.isoformat(),
            "updatedAt": None,  # Activities typically aren't updated after creation
        }
        
        # Generate participant records with createdAt
        activity["participants"] = [
            {
                "id": str(uuid.uuid4()),
                "activityId": activity_id,
                "clientId": cid,
                "createdAt": created_at.isoformat(),
            }
            for cid in participants
        ]
        
        return activity


class IncidentGenerator:
    """Generates incident records."""
    
    # Available incident images in the images directory
    INCIDENT_IMAGES = [
        "image-001.png",
        "image-002.png",
        "image-003.png",
        "image-004.png",
        "image-005.png",
        "image-006.png",
        "image-007.png",
    ]
    
    def __init__(self):
        self.incident_counter_by_home = {}  # Track incident count per home
        self.home_sequence_map = {}  # Track home sequence numbers
    
    def generate(
        self,
        home_id: str,
        client_id: Optional[str],
        reporter_id: str,
        occurred_at: datetime,
        home_sequence: Optional[int] = None,
        admin_user_ids: Optional[list[str]] = None,
    ) -> dict:
        """Generate an incident record.
        
        Args:
            home_id: Home where incident occurred
            client_id: Client involved (or None for home-level incident)
            reporter_id: User reporting the incident
            occurred_at: When the incident occurred
            home_sequence: Sequence number of the home (for incident number generation)
            admin_user_ids: List of admin user IDs who can close incidents
        """
        # Track incident count for this home
        if home_id not in self.incident_counter_by_home:
            self.incident_counter_by_home[home_id] = 0
        self.incident_counter_by_home[home_id] += 1
        
        # Track home sequence if provided
        if home_sequence is not None and home_id not in self.home_sequence_map:
            self.home_sequence_map[home_id] = home_sequence
        
        incident_type = weighted_choice(INCIDENT_TYPES)
        
        # Severity based on incident type
        severity_map = {
            "Fall": [2, 3, 3, 4],
            "Medication": [2, 2, 3],
            "Behavioral": [1, 2, 2, 3],
            "Medical": [2, 3, 3, 4],
            "Injury": [2, 3, 3, 4],
            "Elopement": [4, 4, 5],
            "Other": [1, 2, 2],
        }
        severity = random.choice(severity_map.get(incident_type, [2, 3]))
        
        location = random.choice(INCIDENT_LOCATIONS)
        
        # Most incidents are submitted and closed
        status_weights = [
            ("Closed", 0.75),
            ("UnderReview", 0.15),
            ("Submitted", 0.08),
            ("Draft", 0.02),
        ]
        status = weighted_choice(status_weights)
        
        incident_id = str(uuid.uuid4())
        
        # Generate incident number matching API format: {T}IR{HH}{NNNN}{C}
        incident_number = self._generate_incident_number(
            home_id,
            incident_type,
            self.incident_counter_by_home[home_id]
        )
        
        incident = {
            "id": incident_id,
            "incidentNumber": incident_number,
            "clientId": client_id,
            "homeId": home_id,
            "incidentType": incident_type,
            "severity": severity,
            "status": status,
            "occurredAt": occurred_at.isoformat(),
            "location": location,
            "description": self._generate_description(incident_type, location),
            "actionsTaken": self._generate_actions(incident_type),
            "reportedById": reporter_id,
            "createdAt": occurred_at.isoformat(),
            "updatedAt": None,  # Set below if status is not Draft
        }
        
        # Non-draft incidents have been updated at least once (when submitted)
        if status != "Draft":
            update_offset = random.randint(1, 24)  # 1-24 hours after creation
            updated_at = occurred_at + timedelta(hours=update_offset)
            incident["updatedAt"] = updated_at.isoformat()
        
        if status == "Closed":
            close_date = occurred_at + timedelta(days=random.randint(1, 7))
            incident["closedAt"] = close_date.isoformat()
            incident["closureNotes"] = "Investigation complete. Preventive measures implemented."
            # Closed by an admin user (if provided) or the reporter
            if admin_user_ids:
                incident["closedById"] = random.choice(admin_user_ids)
            else:
                incident["closedById"] = reporter_id
            # Update updatedAt to match closure time
            incident["updatedAt"] = close_date.isoformat()
        
        # Generate photos for the incident (1-3 photos per incident)
        incident["photos"] = self._generate_photos(incident_id, home_id, reporter_id, occurred_at)
        
        return incident
    
    def _generate_photos(
        self,
        incident_id: str,
        home_id: str,
        uploader_id: str,
        occurred_at: datetime,
    ) -> list[dict]:
        """Generate 1-3 random photos for an incident."""
        num_photos = random.randint(1, 3)
        selected_images = random.sample(self.INCIDENT_IMAGES, min(num_photos, len(self.INCIDENT_IMAGES)))
        
        photos = []
        for i, image_file in enumerate(selected_images):
            photo_id = str(uuid.uuid4())
            blob_path = f"incident-photos/{home_id}/{incident_id}/{photo_id}.png"
            
            # Generate captions based on incident type
            captions = [
                "Photo of incident location",
                "Documentation of affected area",
                "Evidence of incident conditions",
                None,  # Sometimes no caption
            ]
            
            photos.append({
                "id": photo_id,
                "incidentId": incident_id,
                "blobPath": blob_path,
                "fileName": image_file,
                "contentType": "image/png",
                "fileSizeBytes": random.randint(50000, 500000),  # Realistic file size range
                "displayOrder": i,
                "caption": random.choice(captions),
                "createdAt": occurred_at.isoformat(),
                "createdById": uploader_id,
                "_sourceFile": image_file,  # Internal reference for the synthetic data loader
            })
        
        return photos
    
    def _generate_description(self, incident_type: str, location: str) -> str:
        """Generate incident description."""
        descriptions = {
            "Fall": f"Resident found on floor in {location}. Stated they lost balance while attempting to stand.",
            "Medication": f"Medication administration error discovered during {random.choice(['morning', 'evening'])} med pass.",
            "Behavioral": f"Resident exhibited agitated behavior in {location}. Verbal intervention attempted.",
            "Medical": f"Resident complained of {random.choice(['chest discomfort', 'shortness of breath', 'dizziness', 'severe headache'])} in {location}.",
            "Injury": f"Resident sustained minor {random.choice(['skin tear', 'bruise', 'abrasion'])} in {location}.",
            "Elopement": "Resident found attempting to leave facility through main entrance.",
            "Other": f"Incident occurred in {location}. See detailed notes.",
        }
        return descriptions.get(incident_type, f"Incident occurred in {location}.")
    
    def _generate_actions(self, incident_type: str) -> str:
        """Generate actions taken."""
        actions = {
            "Fall": "Assessed for injuries. Vitals checked. Family notified. Incident documented. Fall risk reassessed.",
            "Medication": "Physician notified. Medication reconciliation performed. Additional staff training scheduled.",
            "Behavioral": "De-escalation techniques applied. Care plan reviewed. Family notified of behavior changes.",
            "Medical": "Vitals monitored. Physician contacted. Emergency services notified. Family updated on condition.",
            "Injury": "First aid administered. Wound care provided. Physician informed. Incident documented.",
            "Elopement": "Resident safely returned. Security measures reviewed. Family and physician notified.",
            "Other": "Appropriate intervention provided. Supervisor notified. Incident documented per policy.",
        }
        return actions.get(incident_type, "Appropriate actions taken. Incident documented.")
    
    def _generate_incident_number(self, home_id: str, incident_type: str, incident_count: int) -> str:
        """Generate incident number in format: {T}IR{HH}{NNNN}{C}
        
        Format breakdown:
        - T: Type code (F=Fall, M=Medication, B=Behavioral, X=Medical, I=Injury, E=Elopement, O=Other)
        - IR: Fixed prefix (Incident Report)
        - HH: Home code (2-char base-36 encoded home sequence)
        - NNNN: Incident sequence (4-char base-36 encoded incident count for home)
        - C: Checksum (Luhn mod 36)
        """
        # Get type code
        type_code = self._get_incident_type_code(incident_type)
        
        # Get home sequence (use tracked value or default to 1)
        home_sequence = self.home_sequence_map.get(home_id, 1)
        home_code = self._to_base36(home_sequence).zfill(2).upper()
        
        # Get incident sequence for this home
        sequence = self._to_base36(incident_count).zfill(4).upper()
        
        # Build payload: HomeCode + Sequence
        payload = f"{home_code}{sequence}"
        
        # Calculate Luhn mod 36 checksum over full reference (type + IR + payload)
        full_payload = f"{type_code}IR{payload}"
        checksum = self._calculate_luhn_mod36_checksum(full_payload)
        
        return f"{type_code}IR{payload}{checksum}"
    
    @staticmethod
    def _get_incident_type_code(incident_type: str) -> str:
        """Get single character code for incident type."""
        type_map = {
            "Fall": "F",
            "Medication": "M",
            "Behavioral": "B",
            "Medical": "X",
            "Injury": "I",
            "Elopement": "E",
            "Other": "O",
        }
        return type_map.get(incident_type, "O")
    
    @staticmethod
    def _to_base36(value: int) -> str:
        """Convert integer to base-36 string (0-9, A-Z)."""
        chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        if value == 0:
            return "0"
        
        result = []
        while value > 0:
            result.insert(0, chars[value % 36])
            value //= 36
        return "".join(result)
    
    @staticmethod
    def _calculate_luhn_mod36_checksum(input_str: str) -> str:
        """Calculate Luhn mod 36 checksum character.
        
        This is an industry-standard algorithm used for validation.
        """
        chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        sum_val = 0
        factor = 2
        
        for i in range(len(input_str) - 1, -1, -1):
            code_point = chars.index(input_str[i].upper())
            addend = factor * code_point
            
            # Sum digits of addend (in base 36)
            addend = (addend // 36) + (addend % 36)
            sum_val += addend
            
            # Alternate factor between 1 and 2
            factor = 1 if factor == 2 else 2
        
        remainder = sum_val % 36
        check_code_point = (36 - remainder) % 36
        
        return chars[check_code_point]


class AppointmentGenerator:
    """Generates appointment records."""
    
    # Appointment type to title templates
    TITLE_TEMPLATES = {
        "GeneralPractice": ["Primary care checkup", "Follow-up visit", "Annual physical", "Wellness visit"],
        "Dental": ["Dental checkup", "Teeth cleaning", "Dental exam", "Oral health screening"],
        "Ophthalmology": ["Eye exam", "Vision screening", "Ophthalmology follow-up", "Glaucoma check"],
        "Podiatry": ["Podiatry visit", "Foot care appointment", "Diabetic foot check", "Nail care"],
        "PhysicalTherapy": ["PT session", "Physical therapy", "Gait training", "Strength assessment"],
        "OccupationalTherapy": ["OT session", "Occupational therapy", "ADL assessment", "Home safety review"],
        "SpeechTherapy": ["Speech therapy session", "Swallowing evaluation", "Communication therapy"],
        "Psychiatry": ["Psychiatry follow-up", "Mental health check", "Medication review - Psych"],
        "Dermatology": ["Skin check", "Dermatology appointment", "Rash evaluation", "Skin cancer screening"],
        "Cardiology": ["Cardiology follow-up", "Heart checkup", "Cardiac evaluation", "Pacemaker check"],
        "Neurology": ["Neurology appointment", "Cognitive assessment", "Tremor evaluation", "Memory clinic"],
        "LabWork": ["Lab work - routine", "Blood draw", "Lab tests", "Fasting lab work"],
        "Imaging": ["X-ray", "CT scan", "MRI", "Ultrasound", "Bone density scan"],
        "Audiology": ["Hearing test", "Audiology check", "Hearing aid adjustment", "Ear exam"],
        "SocialWorker": ["Social services meeting", "Care coordination", "Benefits review"],
        "FamilyVisit": ["Family care conference", "Family meeting", "Care planning meeting"],
        "Other": ["Medical appointment", "Specialist visit", "Healthcare appointment"],
    }
    
    # Outcome note templates by status
    OUTCOME_TEMPLATES = {
        "Completed": [
            "Appointment completed as scheduled. No concerns noted.",
            "Visit went well. Follow-up scheduled in 3 months.",
            "Good outcome. Medication adjusted as needed.",
            "Resident tolerated appointment well. Results pending.",
            "Stable condition noted. Continue current care plan.",
            "Provider satisfied with progress. No changes to treatment.",
        ],
        "Cancelled": [
            "Cancelled due to resident illness.",
            "Cancelled - weather conditions.",
            "Cancelled by provider office - rescheduled.",
            "Family requested cancellation.",
            "Cancelled - transportation unavailable.",
        ],
        "NoShow": [
            "Resident refused to go to appointment.",
            "Missed appointment - scheduling error.",
            "Could not attend due to acute illness.",
        ],
        "Rescheduled": [
            "Rescheduled to better accommodate resident's needs.",
            "Provider requested reschedule.",
            "Rescheduled due to conflicting appointment.",
        ],
    }
    
    def __init__(self):
        self.appointment_counter = 0
    
    def generate(
        self,
        client_id: str,
        home_id: str,
        created_by_id: str,
        scheduled_at: datetime,
        is_past: bool = False,
    ) -> dict:
        """Generate an appointment record."""
        self.appointment_counter += 1
        
        appointment_type = weighted_choice(APPOINTMENT_TYPES)
        
        # Get title based on type
        titles = self.TITLE_TEMPLATES.get(appointment_type, self.TITLE_TEMPLATES["Other"])
        title = random.choice(titles)
        
        # Get location (prefer relevant locations for the appointment type)
        location_info = self._get_location_for_type(appointment_type)
        
        # Duration varies by type
        duration = self._get_duration_for_type(appointment_type)
        
        # Transportation notes
        transport = random.choice(TRANSPORTATION_OPTIONS)
        
        # Provider from physicians list or location
        if location_info["phone"]:
            provider_phone = location_info["phone"]
        else:
            provider_phone = random.choice(PHYSICIANS)["phone"] if random.random() > 0.3 else None
        
        provider_name = self._get_provider_name(appointment_type) if random.random() > 0.3 else None
        
        appointment_id = str(uuid.uuid4())
        
        appointment = {
            "id": appointment_id,
            "clientId": client_id,
            "homeId": home_id,
            "appointmentType": appointment_type,
            "status": "Scheduled",  # Default to scheduled
            "title": title,
            "scheduledAt": scheduled_at.isoformat(),
            "durationMinutes": duration,
            "location": location_info["name"] if location_info["address"] else "Home visit",
            "providerName": provider_name,
            "providerPhone": provider_phone,
            "notes": self._generate_notes(appointment_type),
            "transportationNotes": transport,
            "reminderSent": is_past,  # Reminders would have been sent for past appointments
            "createdById": created_by_id,
            "createdAt": (scheduled_at - timedelta(days=random.randint(3, 21))).isoformat(),
        }
        
        # If past appointment, determine outcome
        if is_past:
            status = weighted_choice(APPOINTMENT_STATUSES)
            appointment["status"] = status
            
            if status == "Completed":
                completed_at = scheduled_at + timedelta(minutes=duration)
                appointment["completedAt"] = completed_at.isoformat()
                appointment["completedById"] = created_by_id
                appointment["outcomeNotes"] = random.choice(self.OUTCOME_TEMPLATES["Completed"])
            elif status in ["Cancelled", "NoShow", "Rescheduled"]:
                # These have outcome notes explaining what happened
                appointment["outcomeNotes"] = random.choice(self.OUTCOME_TEMPLATES.get(status, []))
        
        return appointment
    
    def _get_location_for_type(self, appointment_type: str) -> dict:
        """Get an appropriate location for the appointment type."""
        # Some types are more likely to be home visits or specific locations
        if appointment_type in ["PhysicalTherapy", "OccupationalTherapy", "SpeechTherapy"]:
            # 40% chance of home visit for therapy
            if random.random() < 0.4:
                return {"name": "Home visit", "address": None, "phone": None}
        
        if appointment_type == "LabWork":
            labs = [loc for loc in APPOINTMENT_LOCATIONS if "Quest" in loc["name"] or "LabCorp" in loc["name"]]
            if labs:
                return random.choice(labs)
        
        if appointment_type == "Imaging":
            imaging = [loc for loc in APPOINTMENT_LOCATIONS if "Imaging" in loc["name"]]
            if imaging:
                return random.choice(imaging)
        
        if appointment_type == "Dental":
            dental = [loc for loc in APPOINTMENT_LOCATIONS if "Dental" in loc["name"]]
            if dental:
                return random.choice(dental)
        
        if appointment_type == "Ophthalmology":
            eye = [loc for loc in APPOINTMENT_LOCATIONS if "Eye" in loc["name"]]
            if eye:
                return random.choice(eye)
        
        if appointment_type == "Podiatry":
            podiatry = [loc for loc in APPOINTMENT_LOCATIONS if "Podiatry" in loc["name"]]
            if podiatry:
                return random.choice(podiatry)
        
        if appointment_type == "Audiology":
            audio = [loc for loc in APPOINTMENT_LOCATIONS if "Audiology" in loc["name"]]
            if audio:
                return random.choice(audio)
        
        if appointment_type == "Cardiology":
            cardio = [loc for loc in APPOINTMENT_LOCATIONS if "Cardiology" in loc["name"]]
            if cardio:
                return random.choice(cardio)
        
        # Default: general medical centers
        general = [loc for loc in APPOINTMENT_LOCATIONS if "Medical" in loc["name"] or "MultiCare" in loc["name"]]
        if general:
            return random.choice(general)
        
        return random.choice(APPOINTMENT_LOCATIONS)
    
    def _get_duration_for_type(self, appointment_type: str) -> int:
        """Get appropriate duration for appointment type."""
        duration_ranges = {
            "GeneralPractice": [30, 45, 60],
            "Dental": [30, 45, 60],
            "Ophthalmology": [30, 45],
            "Podiatry": [30, 45],
            "PhysicalTherapy": [45, 60],
            "OccupationalTherapy": [45, 60],
            "SpeechTherapy": [30, 45],
            "Psychiatry": [30, 45, 60],
            "Dermatology": [15, 30],
            "Cardiology": [30, 45, 60],
            "Neurology": [45, 60],
            "LabWork": [15, 30],
            "Imaging": [30, 60, 90],
            "Audiology": [30, 45, 60],
            "SocialWorker": [45, 60],
            "FamilyVisit": [60, 90, 120],
        }
        durations = duration_ranges.get(appointment_type, [30, 45, 60])
        return random.choice(durations)
    
    def _get_provider_name(self, appointment_type: str) -> str:
        """Generate a provider name."""
        specialties = {
            "GeneralPractice": ["", "", ""],  # Use general physicians
            "Dental": ["DDS", "DMD"],
            "Ophthalmology": ["MD, Ophthalmology", "OD"],
            "Podiatry": ["DPM"],
            "PhysicalTherapy": ["PT, DPT"],
            "OccupationalTherapy": ["OT, OTR/L"],
            "SpeechTherapy": ["SLP, CCC"],
            "Psychiatry": ["MD, Psychiatry"],
            "Cardiology": ["MD, Cardiology"],
            "Neurology": ["MD, Neurology"],
            "Dermatology": ["MD, Dermatology"],
            "Audiology": ["AuD"],
        }
        
        suffix_list = specialties.get(appointment_type, [""])
        suffix = random.choice(suffix_list) if suffix_list else ""
        
        first = fake.first_name()
        last = fake.last_name()
        
        if suffix:
            return f"Dr. {first} {last}, {suffix}"
        return f"Dr. {first} {last}"
    
    def _generate_notes(self, appointment_type: str) -> Optional[str]:
        """Generate optional appointment notes."""
        if random.random() > 0.5:
            return None
        
        notes_by_type = {
            "GeneralPractice": [
                "Please bring current medication list.",
                "Fasting required if labs ordered.",
                "Bring insurance card and ID.",
            ],
            "Dental": [
                "No eating 30 minutes before appointment.",
                "Bring list of current medications.",
            ],
            "LabWork": [
                "Fasting required - nothing to eat or drink after midnight.",
                "Stay well hydrated before blood draw.",
            ],
            "Imaging": [
                "No metal objects - remove jewelry before appointment.",
                "Contrast dye may be used - notify of any allergies.",
            ],
            "Cardiology": [
                "Bring list of all heart medications.",
                "Wear comfortable loose clothing.",
            ],
            "PhysicalTherapy": [
                "Wear comfortable clothing and sturdy shoes.",
                "Bring walker/cane if used.",
            ],
        }
        
        type_notes = notes_by_type.get(appointment_type, ["No special instructions."])
        return random.choice(type_notes)


# Convenience function to get all generators
def create_generators(start_date: datetime, end_date: datetime):
    """Create and return all generator instances."""
    return {
        "home": HomeGenerator(start_date, end_date),
        "user": UserGenerator(),
        "client": ClientGenerator(),
        "adl": ADLLogGenerator(),
        "vitals": VitalsLogGenerator(),
        "medication": MedicationLogGenerator(),
        "rom": ROMLogGenerator(),
        "behavior": BehaviorNoteGenerator(),
        "activity": ActivityGenerator(),
        "incident": IncidentGenerator(),
        "appointment": AppointmentGenerator(),
    }
