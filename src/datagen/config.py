"""
Configuration data for synthetic data generation.
Contains realistic lists for names, medications, diagnoses, etc.
"""

import random
from datetime import datetime, timedelta
from typing import Any

# Date range for data generation (2 years back from now)
END_DATE = datetime.now()
START_DATE = END_DATE - timedelta(days=730)  # ~2 years

# Washington State cities for adult family homes
WA_CITIES = [
    {"city": "Seattle", "state": "WA", "zip_prefix": "981"},
    {"city": "Tacoma", "state": "WA", "zip_prefix": "984"},
    {"city": "Spokane", "state": "WA", "zip_prefix": "992"},
    {"city": "Bellevue", "state": "WA", "zip_prefix": "980"},
    {"city": "Kent", "state": "WA", "zip_prefix": "980"},
    {"city": "Everett", "state": "WA", "zip_prefix": "982"},
    {"city": "Renton", "state": "WA", "zip_prefix": "980"},
    {"city": "Federal Way", "state": "WA", "zip_prefix": "980"},
    {"city": "Olympia", "state": "WA", "zip_prefix": "985"},
    {"city": "Bellingham", "state": "WA", "zip_prefix": "982"},
]

# Home name templates
HOME_NAME_PREFIXES = [
    "Sunrise", "Golden", "Peaceful", "Caring", "Harmony",
    "Comfort", "Heritage", "Grace", "Haven", "Serenity",
    "Evergreen", "Mountain View", "Lakeside", "Cedar", "Oak"
]

HOME_NAME_SUFFIXES = [
    "Adult Family Home", "Care Home", "Residential Care",
    "Home Care", "Family Home"
]

# Common elderly diagnoses (ICD-10 based)
DIAGNOSES = [
    "Alzheimer's disease",
    "Vascular dementia",
    "Parkinson's disease",
    "Type 2 Diabetes",
    "Hypertension",
    "Congestive Heart Failure",
    "COPD",
    "Osteoarthritis",
    "Chronic Kidney Disease",
    "Atrial Fibrillation",
    "Stroke history",
    "Depression",
    "Anxiety disorder",
    "Osteoporosis",
    "Hypothyroidism",
    "Anemia",
    "GERD",
    "Urinary Incontinence",
    "Hearing Loss",
    "Macular Degeneration",
]

# Common allergies
ALLERGIES = [
    "Penicillin",
    "Sulfa drugs",
    "Aspirin",
    "NSAIDs",
    "Latex",
    "Shellfish",
    "Peanuts",
    "Tree nuts",
    "Eggs",
    "Milk",
    "Soy",
    "Wheat",
    "Codeine",
    "Morphine",
    "Contrast dye",
    "Bee stings",
]

# Common medications for elderly care
MEDICATIONS = [
    {"name": "Lisinopril", "dosage": "10mg", "route": "Oral"},
    {"name": "Lisinopril", "dosage": "20mg", "route": "Oral"},
    {"name": "Metformin", "dosage": "500mg", "route": "Oral"},
    {"name": "Metformin", "dosage": "1000mg", "route": "Oral"},
    {"name": "Amlodipine", "dosage": "5mg", "route": "Oral"},
    {"name": "Amlodipine", "dosage": "10mg", "route": "Oral"},
    {"name": "Metoprolol", "dosage": "25mg", "route": "Oral"},
    {"name": "Metoprolol", "dosage": "50mg", "route": "Oral"},
    {"name": "Omeprazole", "dosage": "20mg", "route": "Oral"},
    {"name": "Losartan", "dosage": "50mg", "route": "Oral"},
    {"name": "Atorvastatin", "dosage": "20mg", "route": "Oral"},
    {"name": "Atorvastatin", "dosage": "40mg", "route": "Oral"},
    {"name": "Levothyroxine", "dosage": "50mcg", "route": "Oral"},
    {"name": "Levothyroxine", "dosage": "75mcg", "route": "Oral"},
    {"name": "Gabapentin", "dosage": "300mg", "route": "Oral"},
    {"name": "Sertraline", "dosage": "50mg", "route": "Oral"},
    {"name": "Donepezil", "dosage": "5mg", "route": "Oral"},
    {"name": "Donepezil", "dosage": "10mg", "route": "Oral"},
    {"name": "Memantine", "dosage": "10mg", "route": "Oral"},
    {"name": "Carbidopa-Levodopa", "dosage": "25-100mg", "route": "Oral"},
    {"name": "Furosemide", "dosage": "20mg", "route": "Oral"},
    {"name": "Furosemide", "dosage": "40mg", "route": "Oral"},
    {"name": "Warfarin", "dosage": "5mg", "route": "Oral"},
    {"name": "Aspirin", "dosage": "81mg", "route": "Oral"},
    {"name": "Vitamin D3", "dosage": "1000 IU", "route": "Oral"},
    {"name": "Calcium Carbonate", "dosage": "500mg", "route": "Oral"},
    {"name": "Insulin Glargine", "dosage": "10 units", "route": "Injection"},
    {"name": "Insulin Glargine", "dosage": "20 units", "route": "Injection"},
    {"name": "Albuterol", "dosage": "2 puffs", "route": "Inhalation"},
    {"name": "Fluticasone", "dosage": "2 puffs", "route": "Inhalation"},
    {"name": "Nitroglycerin", "dosage": "0.4mg", "route": "Sublingual"},
    {"name": "Lorazepam", "dosage": "0.5mg", "route": "Oral"},
    {"name": "Acetaminophen", "dosage": "500mg", "route": "Oral"},
    {"name": "Bisacodyl", "dosage": "10mg", "route": "Oral"},
    {"name": "Docusate", "dosage": "100mg", "route": "Oral"},
    {"name": "Eye Drops (Artificial Tears)", "dosage": "2 drops", "route": "Ophthalmic"},
]

# ROM (Range of Motion) exercise descriptions
ROM_EXERCISES = [
    "Passive arm raises",
    "Ankle circles",
    "Knee flexion exercises",
    "Shoulder rotations",
    "Wrist flexion and extension",
    "Hip abduction exercises",
    "Elbow flexion exercises",
    "Neck stretches",
    "Finger flexion exercises",
    "Ankle pumps",
    "Leg raises (supine)",
    "Seated leg extensions",
    "Arm circles",
    "Hand grip exercises",
    "Toe curls",
]

# Activity names and categories
ACTIVITIES = [
    {"name": "Bingo Night", "category": "Recreational", "group": True},
    {"name": "Movie Afternoon", "category": "Recreational", "group": True},
    {"name": "Music Therapy Session", "category": "Recreational", "group": True},
    {"name": "Arts and Crafts", "category": "Recreational", "group": True},
    {"name": "Puzzle Time", "category": "Recreational", "group": False},
    {"name": "Reading Time", "category": "Recreational", "group": False},
    {"name": "Card Games", "category": "Social", "group": True},
    {"name": "Family Visit", "category": "Social", "group": False},
    {"name": "Birthday Celebration", "category": "Social", "group": True},
    {"name": "Holiday Party", "category": "Social", "group": True},
    {"name": "Group Exercise Class", "category": "Exercise", "group": True},
    {"name": "Walking Program", "category": "Exercise", "group": False},
    {"name": "Chair Yoga", "category": "Exercise", "group": True},
    {"name": "Balance Exercises", "category": "Exercise", "group": False},
    {"name": "Pet Therapy Visit", "category": "Other", "group": True},
    {"name": "Garden Activity", "category": "Other", "group": True},
    {"name": "Cooking Class", "category": "Other", "group": True},
    {"name": "Religious Service", "category": "Other", "group": True},
]

# Incident location templates
INCIDENT_LOCATIONS = [
    "Bedroom", "Bathroom", "Living Room", "Dining Room",
    "Kitchen", "Hallway", "Front Entrance", "Back Yard",
    "Common Area", "Medication Room"
]

# Behavior note templates
BEHAVIOR_OBSERVATIONS = [
    {"category": "Behavior", "text": "Resident was cooperative during morning care routine."},
    {"category": "Behavior", "text": "Resident showed signs of agitation, redirected successfully."},
    {"category": "Behavior", "text": "Resident engaged well with other residents during group activity."},
    {"category": "Behavior", "text": "Resident refused to participate in activity, respected preference."},
    {"category": "Behavior", "text": "Resident wandered during evening hours, gently redirected to room."},
    {"category": "Mood", "text": "Resident appeared cheerful and talkative today."},
    {"category": "Mood", "text": "Resident seemed withdrawn, encouraged social interaction."},
    {"category": "Mood", "text": "Resident expressed missing family, provided emotional support."},
    {"category": "Mood", "text": "Resident slept well and woke in good spirits."},
    {"category": "Mood", "text": "Resident anxious about upcoming doctor visit."},
    {"category": "General", "text": "Resident ate well at all meals today."},
    {"category": "General", "text": "Resident had good appetite at breakfast, less at dinner."},
    {"category": "General", "text": "Resident enjoyed watching favorite TV show."},
    {"category": "General", "text": "Resident participated in physical therapy exercises."},
    {"category": "General", "text": "Family called to check on resident, relayed message."},
]

# Physician names (synthetic)
PHYSICIANS = [
    {"name": "Dr. Sarah Mitchell", "phone": "(206) 555-0101"},
    {"name": "Dr. James Chen", "phone": "(206) 555-0102"},
    {"name": "Dr. Maria Rodriguez", "phone": "(253) 555-0103"},
    {"name": "Dr. Michael Thompson", "phone": "(206) 555-0104"},
    {"name": "Dr. Emily Watson", "phone": "(425) 555-0105"},
    {"name": "Dr. David Kim", "phone": "(206) 555-0106"},
    {"name": "Dr. Jennifer Lee", "phone": "(253) 555-0107"},
    {"name": "Dr. Robert Johnson", "phone": "(206) 555-0108"},
    {"name": "Dr. Lisa Patel", "phone": "(425) 555-0109"},
    {"name": "Dr. William Garcia", "phone": "(360) 555-0110"},
]

# Emergency contact relationships
RELATIONSHIPS = [
    "Son", "Daughter", "Spouse", "Brother", "Sister",
    "Nephew", "Niece", "Grandson", "Granddaughter",
    "Friend", "Power of Attorney", "Guardian"
]

# Discharge reasons
DISCHARGE_REASONS = [
    "Transferred to skilled nursing facility",
    "Returned home with family",
    "Hospitalized - higher level of care needed",
    "Transferred to memory care facility",
    "Deceased",
    "Family relocation",
    "Transferred to another AFH",
]

# Schedule times for medications (common times)
MEDICATION_TIMES = [
    "06:00", "07:00", "08:00", "09:00",
    "12:00", "13:00", "14:00",
    "17:00", "18:00", "19:00",
    "21:00", "22:00"
]

# ADL Level weights (more independent at admission, may decline)
ADL_LEVELS = ["Independent", "PartialAssist", "Dependent", "NotApplicable"]

# Incident types and their relative frequency
INCIDENT_TYPES = [
    ("Fall", 0.35),
    ("Medication", 0.15),
    ("Behavioral", 0.15),
    ("Medical", 0.15),
    ("Injury", 0.10),
    ("Other", 0.08),
    ("Elopement", 0.02),
]

# Appointment types and their relative frequency
APPOINTMENT_TYPES = [
    ("GeneralPractice", 0.25),      # Primary care visits
    ("Dental", 0.10),               # Dental checkups
    ("Ophthalmology", 0.08),        # Eye exams
    ("Podiatry", 0.08),             # Foot care
    ("PhysicalTherapy", 0.08),      # PT sessions
    ("OccupationalTherapy", 0.05),  # OT sessions
    ("Cardiology", 0.07),           # Heart specialists
    ("Neurology", 0.05),            # Brain/nerve specialists
    ("Dermatology", 0.04),          # Skin issues
    ("Psychiatry", 0.04),           # Mental health
    ("LabWork", 0.06),              # Blood tests etc.
    ("Imaging", 0.03),              # X-rays, MRIs
    ("Audiology", 0.03),            # Hearing tests
    ("SpeechTherapy", 0.02),        # Speech/swallowing therapy
    ("SocialWorker", 0.01),         # Social services
    ("FamilyVisit", 0.01),          # Scheduled family meetings
]

# Appointment statuses for completed appointments
APPOINTMENT_STATUSES = [
    ("Completed", 0.80),
    ("Cancelled", 0.12),
    ("NoShow", 0.05),
    ("Rescheduled", 0.03),
]

# Appointment locations
APPOINTMENT_LOCATIONS = [
    {"name": "Overlake Medical Center", "address": "1035 116th Ave NE, Bellevue", "phone": "(425) 555-0200"},
    {"name": "Virginia Mason Medical Center", "address": "1100 9th Ave, Seattle", "phone": "(206) 555-0201"},
    {"name": "Providence Regional Medical Center", "address": "1321 Colby Ave, Everett", "phone": "(425) 555-0202"},
    {"name": "MultiCare Tacoma General", "address": "315 Martin Luther King Jr Way, Tacoma", "phone": "(253) 555-0203"},
    {"name": "Puget Sound Dental Clinic", "address": "2500 3rd Ave, Seattle", "phone": "(206) 555-0210"},
    {"name": "Northwest Eye Associates", "address": "4700 Pt Fosdick Dr, Gig Harbor", "phone": "(253) 555-0211"},
    {"name": "Seattle Podiatry Center", "address": "1600 E Jefferson St, Seattle", "phone": "(206) 555-0212"},
    {"name": "Evergreen Physical Therapy", "address": "12040 NE 128th St, Kirkland", "phone": "(425) 555-0213"},
    {"name": "Sound Cardiology Associates", "address": "1600 116th Ave NE, Bellevue", "phone": "(425) 555-0214"},
    {"name": "Pacific Audiology Center", "address": "1500 Metropolitan Park Dr, Tacoma", "phone": "(253) 555-0215"},
    {"name": "Cascade Imaging Center", "address": "1145 Broadway, Tacoma", "phone": "(253) 555-0216"},
    {"name": "Quest Diagnostics", "address": "3100 Northup Way, Bellevue", "phone": "(425) 555-0217"},
    {"name": "LabCorp Patient Service Center", "address": "600 University St, Seattle", "phone": "(206) 555-0218"},
    {"name": "Home Visit", "address": None, "phone": None},  # For in-home appointments
]

# Appointment duration options (in minutes)
APPOINTMENT_DURATIONS = [15, 30, 45, 60, 90, 120]

# Transportation options for appointments
TRANSPORTATION_OPTIONS = [
    "Family member will provide transportation",
    "Medical transport arranged via Hopelink",
    "Wheelchair accessible van scheduled",
    "Staff member to accompany resident",
    "Telehealth - no transportation needed",
    "Home visit - provider coming to facility",
    None,  # No transportation notes
]


def weighted_choice(choices: list[tuple[Any, float]]) -> Any:
    """Select from weighted choices."""
    values, weights = zip(*choices)
    return random.choices(values, weights=weights, k=1)[0]


def random_date_between(start: datetime, end: datetime) -> datetime:
    """Generate a random datetime between start and end."""
    delta = end - start
    random_days = random.randint(0, delta.days)
    random_seconds = random.randint(0, 86400)
    return start + timedelta(days=random_days, seconds=random_seconds)


def random_working_hour_time(base_date: datetime) -> datetime:
    """Generate a random time during working hours (6 AM - 10 PM)."""
    hour = random.randint(6, 22)
    minute = random.randint(0, 59)
    return base_date.replace(hour=hour, minute=minute, second=0, microsecond=0)
