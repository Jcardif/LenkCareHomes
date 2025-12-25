#!/usr/bin/env python3
"""
LenkCare Homes Synthetic Data Generator

Generates realistic synthetic data for adult family home management
covering a 2-year period with organic business growth.

The data simulates:
- 6 homes opening organically over 2 years
- Clients being admitted and some discharged
- Daily care activities (ADLs, vitals, medications, ROM)
- Behavior notes and activities
- Incidents of various types
- Caregivers assigned to homes

Output: JSON files in the API project's SyntheticData folder
        (copied to output directory during Debug build)
"""

import json
import random
import shutil
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any

from config import START_DATE, END_DATE, random_date_between, random_working_hour_time
from generators import create_generators
from document_generators import DocumentGenerator


# Output configuration - directly to API project's SyntheticData folder
OUTPUT_DIR = Path(__file__).parent.parent / "backend" / "LenkCareHomes" / "LenkCareHomes.Api" / "SyntheticData"
# Source images directory
IMAGES_DIR = Path(__file__).parent / "images"


def ensure_output_dir():
    """Create output directory if it doesn't exist."""
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def copy_images():
    """Copy images from datagen/images to SyntheticData/images."""
    dest_images_dir = OUTPUT_DIR / "images"
    if IMAGES_DIR.exists():
        # Remove existing images directory if it exists
        if dest_images_dir.exists():
            shutil.rmtree(dest_images_dir)
        # Copy the entire images directory
        shutil.copytree(IMAGES_DIR, dest_images_dir)
        image_count = len(list(dest_images_dir.glob("*")))
        print(f"  Copied {image_count} images to {dest_images_dir.absolute()}")
    else:
        print(f"  Warning: Images directory not found at {IMAGES_DIR.absolute()}")


def save_json(data: Any, filename: str):
    """Save data to JSON file."""
    filepath = OUTPUT_DIR / filename
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, default=str)
    print(f"  Saved {filename} ({len(data) if isinstance(data, list) else 1} records)")


def generate_synthetic_data():
    """Main function to generate all synthetic data."""
    print("=" * 60)
    print("LenkCare Homes Synthetic Data Generator")
    print("=" * 60)
    print(f"Generating data from {START_DATE.date()} to {END_DATE.date()}")
    print()
    
    ensure_output_dir()
    
    # Initialize generators
    generators = create_generators(START_DATE, END_DATE)
    
    # Data containers
    all_data = {
        "homes": [],
        "beds": [],
        "users": [],
        "caregiverHomeAssignments": [],
        "clients": [],
        "adlLogs": [],
        "vitalsLogs": [],
        "medicationLogs": [],
        "romLogs": [],
        "behaviorNotes": [],
        "activities": [],
        "activityParticipants": [],
        "incidents": [],
        "appointments": [],
        "documents": [],
    }
    
    # Document generator
    documents_dir = OUTPUT_DIR / "documents"
    doc_generator = DocumentGenerator(documents_dir)
    
    # =========================================================================
    # Phase 1: Generate homes with organic opening schedule
    # =========================================================================
    print("Phase 1: Generating homes...")
    
    # We need to create the admin user first so we can reference their ID
    # for home creation
    admin_user = generators["user"].generate_admin(START_DATE)
    sysadmin_user = generators["user"].generate_sysadmin(START_DATE)
    all_data["users"].append(admin_user)
    all_data["users"].append(sysadmin_user)
    
    # Track admin user IDs for incident closure
    admin_user_ids = [admin_user["id"]]
    
    # 6 homes opening organically over 2 years
    # First home: Near start date (company launch)
    # Subsequent homes: Every 3-5 months as business grows
    home_open_dates = [
        START_DATE + timedelta(days=14),                    # Home 1: 2 weeks after start
        START_DATE + timedelta(days=random.randint(90, 120)),   # Home 2: ~3-4 months
        START_DATE + timedelta(days=random.randint(180, 240)),  # Home 3: ~6-8 months
        START_DATE + timedelta(days=random.randint(300, 360)),  # Home 4: ~10-12 months
        START_DATE + timedelta(days=random.randint(420, 480)),  # Home 5: ~14-16 months
        START_DATE + timedelta(days=random.randint(540, 600)),  # Home 6: ~18-20 months
    ]
    
    homes = []
    beds_by_home = {}
    
    for open_date in home_open_dates:
        home = generators["home"].generate(open_date, created_by_id=admin_user["id"])
        homes.append(home)
        
        home_beds = generators["home"].generate_beds(home)
        beds_by_home[home["id"]] = home_beds
        all_data["beds"].extend(home_beds)
    
    all_data["homes"] = homes
    print(f"  Created {len(homes)} homes with {len(all_data['beds'])} total beds")
    
    # =========================================================================
    # Phase 2: Generate users (caregivers)
    # Admin and Sysadmin were already created in Phase 1
    # =========================================================================
    print("Phase 2: Generating caregivers...")
    
    # Caregivers: 2-3 per home, hired around home opening
    caregivers_by_home = {}
    
    for home in homes:
        home_open = datetime.fromisoformat(home["createdAt"])
        num_caregivers = random.randint(2, 3)
        
        caregivers_by_home[home["id"]] = []
        
        for i in range(num_caregivers):
            # Hire caregivers within 2 weeks before/after home opening
            hire_offset = random.randint(-14, 14)
            hire_date = home_open + timedelta(days=hire_offset)
            hire_date = max(hire_date, START_DATE)  # Can't hire before company start
            
            caregiver = generators["user"].generate_caregiver(
                hire_date,
                [home["id"]],
                assigned_by_id=admin_user["id"]
            )
            all_data["users"].append(caregiver)
            caregivers_by_home[home["id"]].append(caregiver)
            
            # Extract assignments
            for assignment in caregiver.get("homeAssignments", []):
                all_data["caregiverHomeAssignments"].append(assignment)
    
    # Some caregivers work at multiple homes (as company grows)
    for home in homes[3:]:  # For later homes
        if random.random() > 0.5:
            # Assign an existing caregiver from an earlier home
            earlier_home = random.choice(homes[:3])
            earlier_caregiver = random.choice(caregivers_by_home[earlier_home["id"]])
            
            assignment = {
                "id": str(random.getrandbits(128).to_bytes(16, "big").hex()[:8] + "-" + 
                         random.getrandbits(128).to_bytes(16, "big").hex()[:4] + "-" +
                         random.getrandbits(128).to_bytes(16, "big").hex()[:4] + "-" +
                         random.getrandbits(128).to_bytes(16, "big").hex()[:4] + "-" +
                         random.getrandbits(128).to_bytes(16, "big").hex()[:12]),
                "userId": earlier_caregiver["id"],
                "homeId": home["id"],
                "assignedAt": home["createdAt"],
                "assignedById": admin_user["id"],
                "isActive": True,
            }
            all_data["caregiverHomeAssignments"].append(assignment)
            caregivers_by_home[home["id"]].append(earlier_caregiver)
    
    print(f"  Created {len(all_data['users'])} users ({len(all_data['caregiverHomeAssignments'])} home assignments)")
    
    # =========================================================================
    # Phase 3: Generate clients (residents)
    # =========================================================================
    print("Phase 3: Generating clients...")
    
    clients = []
    clients_by_home = {h["id"]: [] for h in homes}
    active_clients_by_bed = {}  # Track which bed is occupied when
    
    for home in homes:
        home_id = home["id"]
        home_open = datetime.fromisoformat(home["createdAt"])
        home_beds = beds_by_home[home_id]
        home_caregivers = caregivers_by_home[home_id]
        
        # Calculate how long this home has been open
        days_open = (END_DATE - home_open).days
        
        # Initial occupancy: 1-2 clients within first month
        initial_clients = random.randint(1, 2)
        
        # Track bed usage
        bed_occupancy = {bed["id"]: [] for bed in home_beds}  # List of (start, end, client_id)
        
        def find_available_bed(date: datetime) -> str | None:
            """Find an available bed on the given date."""
            for bed_id, occupancy in bed_occupancy.items():
                is_available = True
                for start, end, _ in occupancy:
                    if start <= date <= (end or END_DATE):
                        is_available = False
                        break
                if is_available:
                    return bed_id
            return None
        
        # Admit initial clients
        for _ in range(initial_clients):
            admit_date = home_open + timedelta(days=random.randint(0, 30))
            bed_id = find_available_bed(admit_date)
            if bed_id:
                caregiver = random.choice(home_caregivers)
                client = generators["client"].generate(
                    home_id, bed_id, admit_date, caregiver["id"]
                )
                clients.append(client)
                clients_by_home[home_id].append(client)
                bed_occupancy[bed_id].append((admit_date, None, client["id"]))
        
        # Gradual fill over time (simulate word-of-mouth referrals)
        current_date = home_open + timedelta(days=45)
        while current_date < END_DATE:
            # Check if we should admit a new client
            occupied_beds = sum(1 for occ in bed_occupancy.values() 
                               if any(s <= current_date <= (e or END_DATE) for s, e, _ in occ))
            
            if occupied_beds < len(home_beds) and random.random() > 0.7:
                bed_id = find_available_bed(current_date)
                if bed_id:
                    caregiver = random.choice(home_caregivers)
                    
                    # Decide if this client will be discharged
                    stay_duration = random.randint(60, 700)  # 2 months to 2 years
                    discharge_date = current_date + timedelta(days=stay_duration)
                    is_discharged = discharge_date < END_DATE and random.random() > 0.6
                    
                    client = generators["client"].generate(
                        home_id, bed_id, current_date, caregiver["id"],
                        discharged=is_discharged,
                        discharge_date=discharge_date if is_discharged else None,
                    )
                    clients.append(client)
                    clients_by_home[home_id].append(client)
                    
                    end_date = discharge_date if is_discharged else None
                    bed_occupancy[bed_id].append((current_date, end_date, client["id"]))
            
            # Random discharges for long-term residents
            for bed_id, occupancy in bed_occupancy.items():
                for i, (start, end, client_id) in enumerate(occupancy):
                    if end is None and (current_date - start).days > 180:
                        if random.random() > 0.98:  # ~2% monthly discharge rate
                            # Find and update client
                            for c in clients:
                                if c["id"] == client_id:
                                    c["dischargeDate"] = current_date.date().isoformat()
                                    c["dischargeReason"] = random.choice([
                                        "Transferred to skilled nursing facility",
                                        "Returned home with family",
                                        "Transferred to another AFH",
                                    ])
                                    c["isActive"] = False
                                    break
                            occupancy[i] = (start, current_date, client_id)
            
            current_date += timedelta(days=random.randint(7, 21))
    
    all_data["clients"] = clients
    print(f"  Created {len(clients)} clients ({sum(1 for c in clients if c['isActive'])} currently active)")
    
    # =========================================================================
    # Phase 4: Generate care logs (ADLs, vitals, medications, ROM, behavior)
    # =========================================================================
    print("Phase 4: Generating care logs...")
    
    for client in clients:
        home_id = client["homeId"]
        client_id = client["id"]
        home_caregivers = caregivers_by_home[home_id]
        
        admission = datetime.fromisoformat(client["admissionDate"])
        discharge = datetime.fromisoformat(client["dischargeDate"]) if client.get("dischargeDate") else END_DATE
        
        # Determine client's overall condition
        diagnoses = client.get("diagnoses", "")
        has_hypertension = "Hypertension" in diagnoses
        has_diabetes = "Diabetes" in diagnoses
        has_dementia = "dementia" in diagnoses.lower() or "Alzheimer" in diagnoses
        
        # Independence level (may decline over time for dementia patients)
        base_independence = random.choice(["Independent", "PartialAssist", "Dependent"])
        
        current_date = admission
        while current_date < discharge:
            caregiver = random.choice(home_caregivers)
            
            # Daily ADL log (1-2 per day)
            if random.random() > 0.1:  # 90% of days have ADL logs
                for _ in range(random.randint(1, 2)):
                    adl_time = random_working_hour_time(current_date)
                    adl = generators["adl"].generate(
                        client_id, caregiver["id"], adl_time, base_independence
                    )
                    all_data["adlLogs"].append(adl)
            
            # Vitals (1-2 times per day)
            if random.random() > 0.15:  # 85% of days have vitals
                for _ in range(random.randint(1, 2)):
                    vitals_time = random_working_hour_time(current_date)
                    vitals = generators["vitals"].generate(
                        client_id, caregiver["id"], vitals_time,
                        has_hypertension, has_diabetes
                    )
                    all_data["vitalsLogs"].append(vitals)
            
            # Medications (based on client's medication list)
            meds = client.get("_medications", [])
            for med in meds:
                if random.random() > 0.02:  # 98% compliance
                    med_time = random_working_hour_time(current_date)
                    med_log = generators["medication"].generate(
                        client_id, caregiver["id"], med_time, med
                    )
                    all_data["medicationLogs"].append(med_log)
            
            # ROM exercises (every 2-3 days)
            if random.random() > 0.6:
                rom_time = random_working_hour_time(current_date)
                rom = generators["rom"].generate(client_id, caregiver["id"], rom_time)
                all_data["romLogs"].append(rom)
            
            # Behavior notes (varies, more frequent for dementia)
            note_probability = 0.3 if has_dementia else 0.15
            if random.random() < note_probability:
                note_time = random_working_hour_time(current_date)
                note = generators["behavior"].generate(
                    client_id, caregiver["id"], note_time
                )
                all_data["behaviorNotes"].append(note)
            
            current_date += timedelta(days=1)
    
    print(f"  Created {len(all_data['adlLogs'])} ADL logs")
    print(f"  Created {len(all_data['vitalsLogs'])} vitals logs")
    print(f"  Created {len(all_data['medicationLogs'])} medication logs")
    print(f"  Created {len(all_data['romLogs'])} ROM logs")
    print(f"  Created {len(all_data['behaviorNotes'])} behavior notes")
    
    # =========================================================================
    # Phase 5: Generate activities
    # =========================================================================
    print("Phase 5: Generating activities...")
    
    for home in homes:
        home_id = home["id"]
        home_open = datetime.fromisoformat(home["createdAt"])
        home_caregivers = caregivers_by_home[home_id]
        
        current_date = home_open + timedelta(days=7)  # Activities start after first week
        while current_date < END_DATE:
            # Get active clients at this home on this date
            active_client_ids = [
                c["id"] for c in clients_by_home[home_id]
                if (datetime.fromisoformat(c["admissionDate"]) <= current_date and
                    (not c.get("dischargeDate") or 
                     datetime.fromisoformat(c["dischargeDate"]) > current_date))
            ]
            
            if active_client_ids:
                # 2-5 activities per week per home
                activities_this_week = random.randint(2, 5)
                for _ in range(activities_this_week):
                    activity_date = current_date + timedelta(days=random.randint(0, 6))
                    if activity_date < END_DATE:
                        caregiver = random.choice(home_caregivers)
                        activity = generators["activity"].generate(
                            home_id, caregiver["id"], activity_date, active_client_ids
                        )
                        all_data["activities"].append(activity)
                        all_data["activityParticipants"].extend(activity.pop("participants"))
            
            current_date += timedelta(days=7)
    
    print(f"  Created {len(all_data['activities'])} activities with {len(all_data['activityParticipants'])} participations")
    
    # =========================================================================
    # Phase 6: Generate incidents
    # =========================================================================
    print("Phase 6: Generating incidents...")
    
    for home_index, home in enumerate(homes):
        home_id = home["id"]
        home_open = datetime.fromisoformat(home["createdAt"])
        home_caregivers = caregivers_by_home[home_id]
        home_sequence = home_index + 1  # 1-based sequence matching creation order
        
        # Incident rate: ~1-3 per month per home
        current_date = home_open + timedelta(days=random.randint(14, 45))
        while current_date < END_DATE:
            # Get active clients
            active_client_ids = [
                c["id"] for c in clients_by_home[home_id]
                if (datetime.fromisoformat(c["admissionDate"]) <= current_date and
                    (not c.get("dischargeDate") or 
                     datetime.fromisoformat(c["dischargeDate"]) > current_date))
            ]
            
            if active_client_ids and random.random() > 0.6:
                # Most incidents involve a specific client
                client_id = random.choice(active_client_ids) if random.random() > 0.1 else None
                reporter = random.choice(home_caregivers)
                
                incident = generators["incident"].generate(
                    home_id, client_id, reporter["id"], current_date, home_sequence,
                    admin_user_ids=admin_user_ids
                )
                all_data["incidents"].append(incident)
            
            current_date += timedelta(days=random.randint(10, 30))
    
    print(f"  Created {len(all_data['incidents'])} incidents")
    
    # =========================================================================
    # Phase 7: Generate appointments
    # =========================================================================
    print("Phase 7: Generating appointments...")
    
    for client in clients:
        home_id = client["homeId"]
        client_id = client["id"]
        home_caregivers = caregivers_by_home[home_id]
        
        admission = datetime.fromisoformat(client["admissionDate"])
        discharge = datetime.fromisoformat(client["dischargeDate"]) if client.get("dischargeDate") else END_DATE
        
        # Each client has 1-3 appointments per month on average
        current_date = admission + timedelta(days=random.randint(7, 30))  # First appointment after admission
        
        while current_date < discharge:
            caregiver = random.choice(home_caregivers)
            
            # Generate appointment at a reasonable hour (8 AM - 5 PM)
            hour = random.randint(8, 17)
            minute = random.choice([0, 15, 30, 45])
            appointment_time = current_date.replace(hour=hour, minute=minute, second=0, microsecond=0)
            
            is_past = appointment_time < datetime.now()
            
            appointment = generators["appointment"].generate(
                client_id=client_id,
                home_id=home_id,
                created_by_id=caregiver["id"],
                scheduled_at=appointment_time,
                is_past=is_past,
            )
            all_data["appointments"].append(appointment)
            
            # Next appointment in 2-6 weeks
            current_date += timedelta(days=random.randint(14, 42))
    
    # Generate some upcoming appointments for active clients (next 30 days)
    for client in clients:
        if client["isActive"]:
            home_id = client["homeId"]
            client_id = client["id"]
            home_caregivers = caregivers_by_home[home_id]
            
            # 0-2 upcoming appointments per active client
            num_upcoming = random.randint(0, 2)
            for _ in range(num_upcoming):
                caregiver = random.choice(home_caregivers)
                days_ahead = random.randint(1, 30)
                hour = random.randint(8, 17)
                minute = random.choice([0, 15, 30, 45])
                appointment_time = (datetime.now() + timedelta(days=days_ahead)).replace(
                    hour=hour, minute=minute, second=0, microsecond=0
                )
                
                appointment = generators["appointment"].generate(
                    client_id=client_id,
                    home_id=home_id,
                    created_by_id=caregiver["id"],
                    scheduled_at=appointment_time,
                    is_past=False,
                )
                all_data["appointments"].append(appointment)
    
    print(f"  Created {len(all_data['appointments'])} appointments")
    
    # =========================================================================
    # Phase 8: Generate client documents (PDFs)
    # =========================================================================
    print("Phase 8: Generating client documents...")
    
    # Build home lookup for document generation
    homes_by_id = {h["id"]: h for h in homes}
    
    document_count = 0
    for client in clients:
        home = homes_by_id.get(client["homeId"])
        if not home:
            continue
        
        # Use admission date as base for document creation
        admission_date = datetime.fromisoformat(client["admissionDate"])
        
        # Generate documents for this client
        client_docs = doc_generator.generate_documents_for_client(
            client, home, admission_date
        )
        
        for filepath, metadata in client_docs:
            all_data["documents"].append(metadata)
            document_count += 1
    
    print(f"  Created {document_count} PDF documents in {documents_dir.absolute()}")
    
    # =========================================================================
    # Phase 9: Save all data
    # =========================================================================
    print("\nPhase 9: Saving data files...")
    
    # Remove internal fields before saving
    for client in all_data["clients"]:
        client.pop("_medications", None)
    
    for user in all_data["users"]:
        user.pop("homeAssignments", None)
    
    # Save individual files
    save_json(all_data["homes"], "homes.json")
    save_json(all_data["beds"], "beds.json")
    save_json(all_data["users"], "users.json")
    save_json(all_data["caregiverHomeAssignments"], "caregiver_home_assignments.json")
    save_json(all_data["clients"], "clients.json")
    save_json(all_data["adlLogs"], "adl_logs.json")
    save_json(all_data["vitalsLogs"], "vitals_logs.json")
    save_json(all_data["medicationLogs"], "medication_logs.json")
    save_json(all_data["romLogs"], "rom_logs.json")
    save_json(all_data["behaviorNotes"], "behavior_notes.json")
    save_json(all_data["activities"], "activities.json")
    save_json(all_data["activityParticipants"], "activity_participants.json")
    save_json(all_data["incidents"], "incidents.json")
    save_json(all_data["appointments"], "appointments.json")
    save_json(all_data["documents"], "documents.json")
    
    # Also save a combined file for easier import
    save_json(all_data, "all_data.json")
    
    # Copy images for incident photos
    print("\nPhase 10: Copying incident photo images...")
    copy_images()
    
    # =========================================================================
    # Summary
    # =========================================================================
    print("\n" + "=" * 60)
    print("Generation Complete!")
    print("=" * 60)
    print(f"\nSummary:")
    print(f"  - Homes: {len(all_data['homes'])}")
    print(f"  - Beds: {len(all_data['beds'])}")
    print(f"  - Users: {len(all_data['users'])}")
    print(f"  - Clients: {len(all_data['clients'])} ({sum(1 for c in all_data['clients'] if c['isActive'])} active)")
    print(f"  - ADL Logs: {len(all_data['adlLogs']):,}")
    print(f"  - Vitals Logs: {len(all_data['vitalsLogs']):,}")
    print(f"  - Medication Logs: {len(all_data['medicationLogs']):,}")
    print(f"  - ROM Logs: {len(all_data['romLogs']):,}")
    print(f"  - Behavior Notes: {len(all_data['behaviorNotes']):,}")
    print(f"  - Activities: {len(all_data['activities'])}")
    print(f"  - Incidents: {len(all_data['incidents'])}")
    print(f"  - Incident Photos: {sum(len(i.get('photos', [])) for i in all_data['incidents'])}")
    print(f"  - Appointments: {len(all_data['appointments'])} ({sum(1 for a in all_data['appointments'] if a['status'] == 'Scheduled')} scheduled)")
    print(f"  - Documents: {len(all_data['documents'])} PDFs")
    print(f"\nOutput directory: {OUTPUT_DIR.absolute()}")
    print(f"Documents directory: {(OUTPUT_DIR / 'documents').absolute()}")
    print(f"Images directory: {(OUTPUT_DIR / 'images').absolute()}")
    
    return all_data


if __name__ == "__main__":
    generate_synthetic_data()
