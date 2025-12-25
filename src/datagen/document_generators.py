"""
Document generators for synthetic PDF creation.
Creates realistic adult family home documents for clients.
"""

import io
import os
import random
import uuid
from datetime import datetime, timedelta
from pathlib import Path
from typing import Optional

from faker import Faker
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, HRFlowable, ListFlowable, ListItem
)

fake = Faker()
Faker.seed(42)
random.seed(42)


class DocumentGenerator:
    """Generates realistic PDF documents for adult family home clients."""

    def __init__(self, output_dir: Path):
        """
        Initialize the document generator.
        
        Args:
            output_dir: Directory to save generated PDFs.
        """
        self.output_dir = output_dir
        self.output_dir.mkdir(parents=True, exist_ok=True)
        self.styles = getSampleStyleSheet()
        self._setup_custom_styles()

    def _setup_custom_styles(self):
        """Set up custom paragraph styles."""
        self.styles.add(ParagraphStyle(
            name='DocumentTitle',
            parent=self.styles['Heading1'],
            fontSize=16,
            spaceAfter=20,
            alignment=TA_CENTER,
            textColor=colors.darkblue
        ))
        self.styles.add(ParagraphStyle(
            name='SectionTitle',
            parent=self.styles['Heading2'],
            fontSize=12,
            spaceBefore=12,
            spaceAfter=8,
            textColor=colors.darkblue
        ))
        self.styles.add(ParagraphStyle(
            name='BodyJustified',
            parent=self.styles['Normal'],
            fontSize=10,
            alignment=TA_JUSTIFY,
            spaceAfter=8
        ))
        self.styles.add(ParagraphStyle(
            name='SmallText',
            parent=self.styles['Normal'],
            fontSize=8,
            textColor=colors.grey
        ))
        self.styles.add(ParagraphStyle(
            name='Signature',
            parent=self.styles['Normal'],
            fontSize=10,
            spaceBefore=30,
            spaceAfter=8
        ))

    def generate_care_plan(self, client: dict, home: dict, created_at: datetime) -> tuple[str, dict]:
        """
        Generate a comprehensive Care Plan PDF.
        
        Returns:
            Tuple of (file_path, document_metadata)
        """
        doc_id = str(uuid.uuid4())
        filename = f"care_plan_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph("INDIVIDUALIZED CARE PLAN", self.styles['DocumentTitle']))
        story.append(Paragraph(f"{home['name']}", self.styles['Heading2']))
        story.append(Paragraph(f"{home['address']}, {home['city']}, {home['state']} {home['zipCode']}",
                              self.styles['Normal']))
        story.append(Spacer(1, 20))

        # Client Information
        story.append(Paragraph("CLIENT INFORMATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))

        client_info = [
            ["Name:", f"{client['firstName']} {client['lastName']}",
             "Date of Birth:", client.get('dateOfBirth', 'N/A')],
            ["Admission Date:", client.get('admissionDate', 'N/A'),
             "Room:", client.get('bedId', 'N/A')[:8] if client.get('bedId') else 'N/A'],
            ["Primary Physician:", client.get('primaryPhysician', 'N/A'),
             "Phone:", client.get('primaryPhysicianPhone', 'N/A')],
        ]
        table = Table(client_info, colWidths=[1.3*inch, 2*inch, 1.3*inch, 2*inch])
        table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTNAME', (2, 0), (2, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ]))
        story.append(table)
        story.append(Spacer(1, 15))

        # Medical Conditions
        story.append(Paragraph("MEDICAL CONDITIONS & DIAGNOSES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        diagnoses = client.get('diagnoses', 'None documented').split(', ')
        for diag in diagnoses:
            story.append(Paragraph(f"• {diag}", self.styles['Normal']))
        story.append(Spacer(1, 10))

        # Allergies
        story.append(Paragraph("ALLERGIES & SENSITIVITIES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        allergies = client.get('allergies', 'No known allergies')
        if allergies:
            for allergy in allergies.split(', '):
                story.append(Paragraph(f"• {allergy}", self.styles['Normal']))
        else:
            story.append(Paragraph("No known allergies", self.styles['Normal']))
        story.append(Spacer(1, 10))

        # Medications
        story.append(Paragraph("CURRENT MEDICATIONS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        meds = client.get('medicationList', 'None').split(', ')
        for med in meds[:10]:  # Limit to 10 medications
            story.append(Paragraph(f"• {med}", self.styles['Normal']))
        story.append(Spacer(1, 10))

        # Care Goals
        story.append(Paragraph("CARE GOALS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        goals = [
            "Maintain optimal health status and prevent complications",
            "Ensure safety and comfort in daily activities",
            "Promote independence in Activities of Daily Living (ADLs)",
            "Support emotional well-being and social engagement",
            "Monitor and manage chronic conditions effectively",
        ]
        for i, goal in enumerate(goals, 1):
            story.append(Paragraph(f"{i}. {goal}", self.styles['Normal']))
        story.append(Spacer(1, 10))

        # ADL Assistance Levels
        story.append(PageBreak())
        story.append(Paragraph("ACTIVITIES OF DAILY LIVING (ADL) ASSESSMENT", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        adl_levels = ["Independent", "Partial Assist", "Dependent"]
        adl_data = [
            ["Activity", "Level of Assistance", "Special Instructions"],
            ["Bathing", random.choice(adl_levels), "Warm water preferred; skin inspection daily"],
            ["Dressing", random.choice(adl_levels), "Clothing laid out each morning"],
            ["Toileting", random.choice(adl_levels), "Regular schedule; encourage hydration"],
            ["Transferring", random.choice(adl_levels), "Use gait belt if needed"],
            ["Eating", random.choice(adl_levels), "Mechanical soft diet; monitor intake"],
            ["Mobility", random.choice(adl_levels), "Assistive device as needed"],
        ]
        adl_table = Table(adl_data, colWidths=[1.5*inch, 1.5*inch, 3.5*inch])
        adl_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.darkblue),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('GRID', (0, 0), (-1, -1), 1, colors.black),
            ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ]))
        story.append(adl_table)
        story.append(Spacer(1, 15))

        # Emergency Contacts
        story.append(Paragraph("EMERGENCY CONTACTS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        ec_data = [
            ["Name:", client.get('emergencyContactName', 'N/A')],
            ["Relationship:", client.get('emergencyContactRelationship', 'N/A')],
            ["Phone:", client.get('emergencyContactPhone', 'N/A')],
        ]
        ec_table = Table(ec_data, colWidths=[1.5*inch, 4*inch])
        ec_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(ec_table)
        story.append(Spacer(1, 20))

        # Signatures
        story.append(Paragraph("SIGNATURES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 30))
        sig_data = [
            ["_" * 40, "", "_" * 40],
            ["Resident/Representative Signature", "", "Date"],
            ["", "", ""],
            ["_" * 40, "", "_" * 40],
            ["Administrator Signature", "", "Date"],
        ]
        sig_table = Table(sig_data, colWidths=[2.5*inch, 0.5*inch, 2.5*inch])
        sig_table.setStyle(TableStyle([
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
        ]))
        story.append(sig_table)

        # Footer
        story.append(Spacer(1, 30))
        story.append(Paragraph(f"Care Plan Effective Date: {created_at.strftime('%B %d, %Y')}",
                              self.styles['SmallText']))
        story.append(Paragraph("This care plan will be reviewed and updated quarterly or as needed.",
                              self.styles['SmallText']))

        doc.build(story)
        
        # Write to file
        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"Care_Plan_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "CarePlan",
            "description": f"Individualized care plan for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def generate_medical_report(self, client: dict, home: dict, created_at: datetime,
                                report_type: str = "Annual Physical") -> tuple[str, dict]:
        """Generate a Medical Report PDF."""
        doc_id = str(uuid.uuid4())
        filename = f"medical_report_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph("MEDICAL EXAMINATION REPORT", self.styles['DocumentTitle']))
        story.append(Paragraph(f"Report Type: {report_type}", self.styles['Heading3']))
        story.append(Spacer(1, 10))

        # Physician Info
        physician = client.get('primaryPhysician', 'Dr. ' + fake.last_name())
        story.append(Paragraph(f"Attending Physician: {physician}", self.styles['Normal']))
        story.append(Paragraph(f"Examination Date: {created_at.strftime('%B %d, %Y')}", self.styles['Normal']))
        story.append(Spacer(1, 15))

        # Patient Information
        story.append(Paragraph("PATIENT INFORMATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Paragraph(f"Name: {client['firstName']} {client['lastName']}", self.styles['Normal']))
        story.append(Paragraph(f"Date of Birth: {client.get('dateOfBirth', 'N/A')}", self.styles['Normal']))
        story.append(Paragraph(f"Facility: {home['name']}", self.styles['Normal']))
        story.append(Spacer(1, 15))

        # Vital Signs
        story.append(Paragraph("VITAL SIGNS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        vitals = [
            ["Blood Pressure:", f"{random.randint(110, 145)}/{random.randint(65, 90)} mmHg"],
            ["Heart Rate:", f"{random.randint(60, 88)} bpm"],
            ["Temperature:", f"{round(random.uniform(97.5, 98.9), 1)}°F"],
            ["Respiratory Rate:", f"{random.randint(14, 20)} breaths/min"],
            ["Oxygen Saturation:", f"{random.randint(95, 99)}%"],
            ["Weight:", f"{random.randint(110, 180)} lbs"],
            ["Height:", f"{random.randint(60, 72)} inches"],
        ]
        vitals_table = Table(vitals, colWidths=[2*inch, 2*inch])
        vitals_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(vitals_table)
        story.append(Spacer(1, 15))

        # Physical Examination
        story.append(Paragraph("PHYSICAL EXAMINATION FINDINGS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        exam_findings = [
            ("General Appearance", "Patient appears well-nourished and in no acute distress."),
            ("Cardiovascular", "Regular rate and rhythm. No murmurs, rubs, or gallops noted."),
            ("Respiratory", "Clear to auscultation bilaterally. No wheezes or crackles."),
            ("Neurological", "Alert and oriented x3. Cranial nerves intact. Motor strength 5/5 in all extremities."),
            ("Musculoskeletal", "No joint swelling or deformity. Range of motion within normal limits."),
            ("Skin", "Skin intact. No pressure ulcers or concerning lesions noted."),
        ]
        for system, finding in exam_findings:
            story.append(Paragraph(f"<b>{system}:</b> {finding}", self.styles['BodyJustified']))
        story.append(Spacer(1, 15))

        # Assessment
        story.append(Paragraph("ASSESSMENT & RECOMMENDATIONS", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        diagnoses = client.get('diagnoses', '').split(', ')
        for diag in diagnoses[:5]:
            if diag:
                story.append(Paragraph(f"• {diag} - stable, continue current management", self.styles['Normal']))
        story.append(Spacer(1, 10))
        story.append(Paragraph("Recommendations:", self.styles['Normal']))
        story.append(Paragraph("• Continue current medications as prescribed", self.styles['Normal']))
        story.append(Paragraph("• Encourage adequate hydration and nutrition", self.styles['Normal']))
        story.append(Paragraph("• Monitor for any changes in condition", self.styles['Normal']))
        story.append(Paragraph("• Follow-up in 3 months or as needed", self.styles['Normal']))

        # Signature
        story.append(Spacer(1, 40))
        story.append(Paragraph("_" * 40, self.styles['Normal']))
        story.append(Paragraph(f"{physician}", self.styles['Normal']))
        story.append(Paragraph(f"Date: {created_at.strftime('%B %d, %Y')}", self.styles['Normal']))

        doc.build(story)

        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"Medical_Report_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "MedicalReport",
            "description": f"{report_type} examination report for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def generate_consent_form(self, client: dict, home: dict, created_at: datetime,
                             consent_type: str = "General Treatment") -> tuple[str, dict]:
        """Generate a Consent Form PDF."""
        doc_id = str(uuid.uuid4())
        filename = f"consent_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph("CONSENT FOR CARE AND TREATMENT", self.styles['DocumentTitle']))
        story.append(Paragraph(home['name'], self.styles['Heading2']))
        story.append(Spacer(1, 20))

        # Consent Type
        story.append(Paragraph(f"Type: {consent_type}", self.styles['Heading3']))
        story.append(Spacer(1, 15))

        # Resident Information
        story.append(Paragraph("RESIDENT INFORMATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Paragraph(f"Name: {client['firstName']} {client['lastName']}", self.styles['Normal']))
        story.append(Paragraph(f"Date of Birth: {client.get('dateOfBirth', 'N/A')}", self.styles['Normal']))
        story.append(Spacer(1, 15))

        # Consent Text
        story.append(Paragraph("CONSENT AGREEMENT", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))

        consent_text = f"""
        I, the undersigned, hereby authorize {home['name']} and its designated caregivers to provide 
        personal care services and general health oversight to the above-named resident. This consent 
        includes, but is not limited to:

        • Assistance with Activities of Daily Living (bathing, dressing, grooming, toileting, mobility)
        • Medication administration as prescribed by the resident's physician
        • Monitoring of vital signs and general health status
        • Coordination with healthcare providers for medical appointments
        • Emergency medical care when necessary
        • Participation in recreational and therapeutic activities

        I understand that {home['name']} is not a medical facility and that complex medical procedures 
        will be referred to appropriate healthcare providers. I have been informed of the resident's 
        rights and the facility's policies regarding care and treatment.

        I understand that this consent is valid until revoked in writing by the undersigned or upon 
        discharge from the facility.
        """
        story.append(Paragraph(consent_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 30))

        # HIPAA Acknowledgment
        story.append(Paragraph("HIPAA ACKNOWLEDGMENT", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        hipaa_text = """
        I acknowledge that I have received a copy of the facility's Notice of Privacy Practices. 
        I understand how my health information may be used and disclosed, and I consent to such 
        uses and disclosures as described in the Notice.
        """
        story.append(Paragraph(hipaa_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 30))

        # Signatures
        story.append(Paragraph("SIGNATURES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 20))

        sig_data = [
            ["_" * 40, "", "_" * 25],
            ["Resident/Legal Representative Signature", "", "Date"],
            ["", "", ""],
            ["Print Name: _" + "_" * 30, "", ""],
            ["Relationship to Resident: _" + "_" * 20, "", ""],
            ["", "", ""],
            ["_" * 40, "", "_" * 25],
            ["Witness Signature", "", "Date"],
            ["", "", ""],
            ["Print Name: _" + "_" * 30, "", ""],
        ]
        sig_table = Table(sig_data, colWidths=[3.5*inch, 0.5*inch, 2*inch])
        sig_table.setStyle(TableStyle([
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
        ]))
        story.append(sig_table)

        # Footer
        story.append(Spacer(1, 30))
        story.append(Paragraph(f"Form Date: {created_at.strftime('%B %d, %Y')}", self.styles['SmallText']))

        doc.build(story)

        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"Consent_Form_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "ConsentForm",
            "description": f"{consent_type} consent form for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def generate_insurance_document(self, client: dict, home: dict, created_at: datetime) -> tuple[str, dict]:
        """Generate an Insurance Information PDF."""
        doc_id = str(uuid.uuid4())
        filename = f"insurance_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph("INSURANCE INFORMATION SUMMARY", self.styles['DocumentTitle']))
        story.append(Paragraph(home['name'], self.styles['Heading2']))
        story.append(Spacer(1, 20))

        # Resident Information
        story.append(Paragraph("RESIDENT INFORMATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        res_info = [
            ["Full Legal Name:", f"{client['firstName']} {client['lastName']}"],
            ["Date of Birth:", client.get('dateOfBirth', 'N/A')],
            ["Social Security:", "XXX-XX-" + str(random.randint(1000, 9999))],
            ["Admission Date:", client.get('admissionDate', 'N/A')],
        ]
        res_table = Table(res_info, colWidths=[2*inch, 4*inch])
        res_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(res_table)
        story.append(Spacer(1, 20))

        # Primary Insurance
        story.append(Paragraph("PRIMARY INSURANCE", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        insurance_companies = [
            "Medicare", "Blue Cross Blue Shield", "Aetna", "UnitedHealthcare",
            "Humana", "Kaiser Permanente", "Cigna"
        ]
        primary_ins = [
            ["Insurance Company:", random.choice(insurance_companies)],
            ["Policy Number:", f"{random.choice('ABCDEF')}{random.randint(100000000, 999999999)}"],
            ["Group Number:", f"GRP{random.randint(10000, 99999)}"],
            ["Policy Holder:", f"{client['firstName']} {client['lastName']}"],
            ["Effective Date:", (created_at - timedelta(days=random.randint(365, 1825))).strftime('%m/%d/%Y')],
        ]
        ins_table = Table(primary_ins, colWidths=[2*inch, 4*inch])
        ins_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(ins_table)
        story.append(Spacer(1, 15))

        # Secondary Insurance (Medicare/Medicaid for many seniors)
        if random.random() > 0.3:  # 70% have secondary
            story.append(Paragraph("SECONDARY INSURANCE", self.styles['SectionTitle']))
            story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
            secondary_ins = [
                ["Insurance Company:", "Medicaid" if random.random() > 0.5 else "Medicare Supplement"],
                ["Policy Number:", f"MCD{random.randint(10000000, 99999999)}"],
                ["State:", "Washington"],
            ]
            sec_table = Table(secondary_ins, colWidths=[2*inch, 4*inch])
            sec_table.setStyle(TableStyle([
                ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
                ('FONTSIZE', (0, 0), (-1, -1), 10),
                ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
            ]))
            story.append(sec_table)
            story.append(Spacer(1, 15))

        # Billing Contact
        story.append(Paragraph("RESPONSIBLE PARTY FOR BILLING", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        bill_info = [
            ["Name:", client.get('emergencyContactName', 'N/A')],
            ["Relationship:", client.get('emergencyContactRelationship', 'N/A')],
            ["Phone:", client.get('emergencyContactPhone', 'N/A')],
            ["Address:", fake.address().replace('\n', ', ')],
        ]
        bill_table = Table(bill_info, colWidths=[2*inch, 4*inch])
        bill_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(bill_table)

        # Footer
        story.append(Spacer(1, 30))
        story.append(Paragraph(f"Last Updated: {created_at.strftime('%B %d, %Y')}", self.styles['SmallText']))
        story.append(Paragraph("Insurance information verified by facility administrator.",
                              self.styles['SmallText']))

        doc.build(story)

        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"Insurance_Info_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "Insurance",
            "description": f"Insurance information summary for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def generate_legal_document(self, client: dict, home: dict, created_at: datetime,
                                doc_type: str = "Power of Attorney") -> tuple[str, dict]:
        """Generate a Legal Document PDF (POA, Advance Directive, etc.)."""
        doc_id = str(uuid.uuid4())
        safe_type = doc_type.lower().replace(' ', '_')
        filename = f"legal_{safe_type}_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph(doc_type.upper(), self.styles['DocumentTitle']))
        story.append(Paragraph("STATE OF WASHINGTON", self.styles['Heading2']))
        story.append(Spacer(1, 20))

        if doc_type == "Power of Attorney":
            self._add_poa_content(story, client, home, created_at)
        elif doc_type == "Advance Directive":
            self._add_advance_directive_content(story, client, home, created_at)
        else:  # POLST
            self._add_polst_content(story, client, home, created_at)

        doc.build(story)

        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"{doc_type.replace(' ', '_')}_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "Legal",
            "description": f"{doc_type} document for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def _add_poa_content(self, story, client, home, created_at):
        """Add Power of Attorney content to the document."""
        story.append(Paragraph("DURABLE POWER OF ATTORNEY FOR HEALTH CARE", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 10))

        story.append(Paragraph("PART 1: DESIGNATION OF HEALTH CARE AGENT", self.styles['Heading3']))

        poa_text = f"""
        I, <b>{client['firstName']} {client['lastName']}</b>, hereby designate and appoint the 
        following individual as my Health Care Agent to make health care decisions for me if I become 
        unable to make such decisions for myself:
        """
        story.append(Paragraph(poa_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 10))

        agent_info = [
            ["Name:", client.get('emergencyContactName', 'N/A')],
            ["Relationship:", client.get('emergencyContactRelationship', 'N/A')],
            ["Phone:", client.get('emergencyContactPhone', 'N/A')],
        ]
        agent_table = Table(agent_info, colWidths=[1.5*inch, 4*inch])
        agent_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(agent_table)
        story.append(Spacer(1, 15))

        story.append(Paragraph("PART 2: AUTHORITY OF HEALTH CARE AGENT", self.styles['Heading3']))
        authority_text = """
        My Health Care Agent is authorized to make any and all health care decisions for me, including:
        
        • Consent to, refuse, or withdraw any treatment, service, or procedure
        • Select or discharge health care providers and institutions
        • Access my medical records and authorize disclosure
        • Make decisions regarding life-sustaining treatment
        • Make anatomical gift decisions after my death
        
        This power of attorney shall not be affected by my subsequent disability or incapacity.
        """
        story.append(Paragraph(authority_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 20))

        # Signature section
        story.append(Paragraph("SIGNATURES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 20))

        sig_data = [
            ["_" * 40, "", "_" * 25],
            ["Principal Signature", "", "Date"],
            ["", "", ""],
            [f"Print Name: {client['firstName']} {client['lastName']}", "", ""],
            ["", "", ""],
            ["STATE OF WASHINGTON", "", ""],
            [f"County of {home['city']}", "", ""],
            ["", "", ""],
            ["_" * 40, "", "_" * 25],
            ["Notary Public Signature", "", "Date"],
        ]
        sig_table = Table(sig_data, colWidths=[3.5*inch, 0.5*inch, 2*inch])
        sig_table.setStyle(TableStyle([
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
        ]))
        story.append(sig_table)

    def _add_advance_directive_content(self, story, client, home, created_at):
        """Add Advance Directive content to the document."""
        story.append(Paragraph("HEALTH CARE DIRECTIVE (LIVING WILL)", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 10))

        directive_text = f"""
        I, <b>{client['firstName']} {client['lastName']}</b>, make this directive to exercise my 
        right to determine my own health care. If at any time I am incapacitated and unable to 
        communicate my wishes regarding health care, this directive shall guide decisions about 
        my care.
        """
        story.append(Paragraph(directive_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 15))

        story.append(Paragraph("TREATMENT PREFERENCES", self.styles['Heading3']))
        story.append(Spacer(1, 5))

        # Random preferences
        cpr_choice = random.choice(["[ X ] YES, attempt CPR", "[ X ] NO, do not attempt CPR (DNR)"])
        vent_choice = random.choice([
            "[ X ] Use ventilator if needed",
            "[ X ] Use ventilator only for comfort",
            "[ X ] No ventilator support"
        ])
        tube_choice = random.choice([
            "[ X ] Provide nutrition/hydration via tube",
            "[ X ] Trial period only",
            "[ X ] No artificial nutrition/hydration"
        ])

        story.append(Paragraph(f"<b>CPR (Cardiopulmonary Resuscitation):</b> {cpr_choice}",
                              self.styles['Normal']))
        story.append(Paragraph(f"<b>Mechanical Ventilation:</b> {vent_choice}",
                              self.styles['Normal']))
        story.append(Paragraph(f"<b>Tube Feeding/IV Hydration:</b> {tube_choice}",
                              self.styles['Normal']))
        story.append(Spacer(1, 15))

        story.append(Paragraph("COMFORT CARE", self.styles['Heading3']))
        story.append(Paragraph(
            "I want to receive comfort care and pain management at all times, even if it might "
            "hasten my death. I want my family and caregivers to know that my comfort and dignity "
            "are of utmost importance.",
            self.styles['BodyJustified']
        ))
        story.append(Spacer(1, 20))

        # Signature section
        story.append(Paragraph("SIGNATURES", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 15))
        sig_data = [
            ["_" * 40, "", "_" * 25],
            ["Declarant Signature", "", "Date"],
            ["", "", ""],
            ["WITNESS 1:", "", ""],
            ["_" * 40, "", "_" * 25],
            ["Signature", "", "Date"],
            ["Print Name: _" + "_" * 25, "", ""],
            ["", "", ""],
            ["WITNESS 2:", "", ""],
            ["_" * 40, "", "_" * 25],
            ["Signature", "", "Date"],
            ["Print Name: _" + "_" * 25, "", ""],
        ]
        sig_table = Table(sig_data, colWidths=[3.5*inch, 0.5*inch, 2*inch])
        sig_table.setStyle(TableStyle([
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 3),
        ]))
        story.append(sig_table)

    def _add_polst_content(self, story, client, home, created_at):
        """Add POLST form content to the document."""
        story.append(Paragraph("PHYSICIAN ORDERS FOR LIFE-SUSTAINING TREATMENT (POLST)",
                              self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 10))

        story.append(Paragraph(f"Patient: {client['firstName']} {client['lastName']}", self.styles['Normal']))
        story.append(Paragraph(f"Date of Birth: {client.get('dateOfBirth', 'N/A')}", self.styles['Normal']))
        story.append(Spacer(1, 15))

        # Section A
        story.append(Paragraph("SECTION A: CARDIOPULMONARY RESUSCITATION (CPR)", self.styles['Heading3']))
        cpr_order = random.choice([
            "[ X ] ATTEMPT RESUSCITATION/CPR",
            "[ X ] DO NOT ATTEMPT RESUSCITATION (DNR/DNAR)"
        ])
        story.append(Paragraph(cpr_order, self.styles['Normal']))
        story.append(Spacer(1, 10))

        # Section B
        story.append(Paragraph("SECTION B: MEDICAL INTERVENTIONS", self.styles['Heading3']))
        intervention = random.choice([
            "[ X ] FULL TREATMENT - Use all available treatments",
            "[ X ] SELECTIVE TREATMENT - Avoid invasive measures; may use IV fluids/antibiotics",
            "[ X ] COMFORT-FOCUSED TREATMENT - Comfort measures only"
        ])
        story.append(Paragraph(intervention, self.styles['Normal']))
        story.append(Spacer(1, 10))

        # Section C
        story.append(Paragraph("SECTION C: ARTIFICIALLY ADMINISTERED NUTRITION", self.styles['Heading3']))
        nutrition = random.choice([
            "[ X ] Long-term tube feeding",
            "[ X ] Trial period of tube feeding",
            "[ X ] No artificial nutrition"
        ])
        story.append(Paragraph(nutrition, self.styles['Normal']))
        story.append(Spacer(1, 20))

        # Signatures
        story.append(Paragraph("PHYSICIAN/APRN/PA SIGNATURE", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 15))
        sig_data = [
            ["Physician/APRN/PA:", "_" * 35],
            ["Phone:", "_" * 35],
            ["Signature:", "_" * 35],
            ["Date:", "_" * 15],
        ]
        sig_table = Table(sig_data, colWidths=[1.5*inch, 4*inch])
        sig_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ]))
        story.append(sig_table)

    def generate_identification_document(self, client: dict, home: dict,
                                         created_at: datetime) -> tuple[str, dict]:
        """Generate an Identification document copy placeholder PDF."""
        doc_id = str(uuid.uuid4())
        filename = f"id_copy_{client['firstName'].lower()}_{client['lastName'].lower()}_{doc_id[:8]}.pdf"
        filepath = self.output_dir / filename

        buffer = io.BytesIO()
        doc = SimpleDocTemplate(buffer, pagesize=letter,
                               rightMargin=inch, leftMargin=inch,
                               topMargin=inch, bottomMargin=inch)

        story = []

        # Header
        story.append(Paragraph("IDENTIFICATION DOCUMENT COPY", self.styles['DocumentTitle']))
        story.append(Paragraph(home['name'], self.styles['Heading2']))
        story.append(Spacer(1, 30))

        # Copy notice
        story.append(Paragraph("NOTICE: COPY OF IDENTIFICATION ON FILE", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 15))

        notice_text = """
        This document certifies that a copy of the following identification document(s) is on 
        file for the resident listed below. The original documents have been verified and 
        copies are maintained in the resident's secure file.
        """
        story.append(Paragraph(notice_text, self.styles['BodyJustified']))
        story.append(Spacer(1, 20))

        # Resident Information
        story.append(Paragraph("RESIDENT INFORMATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        res_info = [
            ["Full Legal Name:", f"{client['firstName']} {client['lastName']}"],
            ["Date of Birth:", client.get('dateOfBirth', 'N/A')],
        ]
        res_table = Table(res_info, colWidths=[2*inch, 4*inch])
        res_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
        ]))
        story.append(res_table)
        story.append(Spacer(1, 20))

        # ID Documents on file
        story.append(Paragraph("IDENTIFICATION DOCUMENTS ON FILE", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 10))

        id_types = random.sample([
            ("Washington State Driver's License", f"WDL{random.randint(100000, 999999)}"),
            ("Washington State ID Card", f"WID{random.randint(100000, 999999)}"),
            ("U.S. Passport", f"P{random.randint(10000000, 99999999)}"),
            ("Medicare Card", f"1EG4-TE5-MK{random.randint(10, 99)}"),
            ("Social Security Card", "XXX-XX-" + str(random.randint(1000, 9999))),
        ], k=random.randint(2, 3))

        id_data = [["Document Type", "ID Number", "Verified"]]
        for id_type, id_num in id_types:
            id_data.append([id_type, id_num, "✓ Yes"])

        id_table = Table(id_data, colWidths=[3*inch, 2*inch, 1*inch])
        id_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), colors.darkblue),
            ('TEXTCOLOR', (0, 0), (-1, 0), colors.white),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('GRID', (0, 0), (-1, -1), 1, colors.black),
            ('ALIGN', (2, 0), (2, -1), 'CENTER'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ]))
        story.append(id_table)
        story.append(Spacer(1, 30))

        # Verification
        story.append(Paragraph("VERIFICATION", self.styles['SectionTitle']))
        story.append(HRFlowable(width="100%", thickness=1, color=colors.darkblue))
        story.append(Spacer(1, 15))

        verify_data = [
            ["Verified By:", "_" * 30],
            ["Title:", "_" * 30],
            ["Date:", created_at.strftime('%m/%d/%Y')],
            ["Signature:", "_" * 30],
        ]
        verify_table = Table(verify_data, colWidths=[1.5*inch, 4*inch])
        verify_table.setStyle(TableStyle([
            ('FONTNAME', (0, 0), (0, -1), 'Helvetica-Bold'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ]))
        story.append(verify_table)

        # Footer
        story.append(Spacer(1, 30))
        story.append(Paragraph(
            "NOTE: Actual copies of identification documents are stored separately in the "
            "resident's secure file. This form serves as an index and verification record.",
            self.styles['SmallText']
        ))

        doc.build(story)

        with open(filepath, 'wb') as f:
            f.write(buffer.getvalue())

        metadata = {
            "id": doc_id,
            "clientId": client["id"],
            "fileName": filename,
            "originalFileName": f"ID_Verification_{client['firstName']}_{client['lastName']}.pdf",
            "contentType": "application/pdf",
            "documentType": "Identification",
            "description": f"Identification documents on file for {client['firstName']} {client['lastName']}",
            "fileSizeBytes": len(buffer.getvalue()),
            "createdAt": created_at.isoformat(),
        }

        return str(filepath), metadata

    def generate_documents_for_client(self, client: dict, home: dict,
                                      base_date: datetime) -> list[tuple[str, dict]]:
        """
        Generate all relevant documents for a client.
        
        Args:
            client: Client data dictionary.
            home: Home data dictionary.
            base_date: Base date for document creation (usually admission date).
        
        Returns:
            List of (file_path, metadata) tuples.
        """
        documents = []

        # Always create Care Plan (at admission)
        filepath, metadata = self.generate_care_plan(client, home, base_date)
        documents.append((filepath, metadata))

        # Create Consent Form (at admission)
        consent_date = base_date + timedelta(days=random.randint(-7, 0))
        filepath, metadata = self.generate_consent_form(client, home, consent_date,
                                                        consent_type="General Treatment")
        documents.append((filepath, metadata))

        # Create Insurance document (at admission)
        ins_date = base_date + timedelta(days=random.randint(-14, 0))
        filepath, metadata = self.generate_insurance_document(client, home, ins_date)
        documents.append((filepath, metadata))

        # Create Identification document (at admission)
        id_date = base_date + timedelta(days=random.randint(-7, 0))
        filepath, metadata = self.generate_identification_document(client, home, id_date)
        documents.append((filepath, metadata))

        # Create Legal documents (POA, Advance Directive)
        legal_types = random.sample([
            "Power of Attorney",
            "Advance Directive",
            "POLST"
        ], k=random.randint(1, 2))

        for legal_type in legal_types:
            legal_date = base_date + timedelta(days=random.randint(-30, 7))
            filepath, metadata = self.generate_legal_document(client, home, legal_date,
                                                              doc_type=legal_type)
            documents.append((filepath, metadata))

        # Create Medical Report (annual physical or admission physical)
        med_date = base_date + timedelta(days=random.randint(-30, 14))
        filepath, metadata = self.generate_medical_report(client, home, med_date,
                                                          report_type="Admission Physical Exam")
        documents.append((filepath, metadata))

        # Additional medical reports for long-term residents
        admission_date = datetime.fromisoformat(client.get('admissionDate', base_date.isoformat()))
        days_since_admission = (base_date - admission_date).days
        
        if days_since_admission > 365:
            # Add annual physical
            annual_date = admission_date + timedelta(days=365 + random.randint(-14, 14))
            filepath, metadata = self.generate_medical_report(client, home, annual_date,
                                                              report_type="Annual Physical Exam")
            documents.append((filepath, metadata))

        # Additional consent forms for specific treatments
        if random.random() > 0.5:
            spec_consent_date = base_date + timedelta(days=random.randint(7, 90))
            consent_types = [
                "Flu Vaccination",
                "COVID-19 Vaccination",
                "Medication Change",
                "Physical Therapy",
                "Photography/Media Release"
            ]
            filepath, metadata = self.generate_consent_form(
                client, home, spec_consent_date,
                consent_type=random.choice(consent_types)
            )
            documents.append((filepath, metadata))

        return documents
