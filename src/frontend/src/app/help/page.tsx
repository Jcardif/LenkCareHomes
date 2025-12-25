'use client';

import React, { useState } from 'react';
import {
  Typography,
  Card,
  Collapse,
  Space,
  Anchor,
  Row,
  Col,
  Input,
  Tag,
  Divider,
  List,
  Table,
  Alert,
  Button,
  message,
  Grid,
} from 'antd';
import {
  QuestionCircleOutlined,
  BookOutlined,
  KeyOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
  ContactsOutlined,
  HomeOutlined,
  ExclamationCircleOutlined,
  FileTextOutlined,
  AuditOutlined,
  SearchOutlined,
  PhoneOutlined,
  MailOutlined,
  PlayCircleOutlined,
  CalendarOutlined,
  CheckCircleOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { ProtectedRoute, AuthenticatedLayout } from '@/components';
import { useTour, useAvailableTours } from '@/contexts/TourContext';

const { Title, Paragraph, Text } = Typography;
const { Search } = Input;
const { useBreakpoint } = Grid;

// FAQ data organized by category
const faqData = [
  {
    category: 'Getting Started',
    icon: <BookOutlined />,
    items: [
      {
        question: 'How do I log in to LenkCare Homes?',
        answer: 'Navigate to the login page and enter your email address and password. You will then be prompted to authenticate with your passkey using your device\'s biometrics (fingerprint, face recognition) or PIN. Passkeys use WebAuthn/FIDO2 technology for phishing-resistant authentication.',
      },
      {
        question: 'I forgot my password. What should I do?',
        answer: 'Click on "Forgot Password" on the login page. Enter your email address and check your inbox for a password reset link. The link expires after 24 hours for security. You\'ll need to re-register your passkey after resetting your password.',
      },
      {
        question: 'How do I set up my passkey?',
        answer: 'Passkeys are set up during your first login or after an MFA reset. Follow the on-screen prompts to register a passkey using your device\'s biometrics (fingerprint or face recognition) or PIN. You can register multiple passkeys on different devices for backup access.',
      },
      {
        question: 'What are backup codes and who can use them?',
        answer: 'Backup codes are emergency recovery codes available only to Sysadmin accounts. They can be used to log in if you lose access to all your passkeys. Each code can only be used once. Admin and Caregiver accounts should contact a Sysadmin to reset their authentication if needed.',
      },
      {
        question: 'What is the guided tour feature?',
        answer: 'The guided tour provides an interactive walkthrough of the key features of LenkCare Homes. New users will see tours automatically on their first visit to different pages. You can restart any tour from this Help page or disable automatic tours in your preferences.',
      },
    ],
  },
  {
    category: 'Home Management',
    icon: <HomeOutlined />,
    items: [
      {
        question: 'How do I add a new care home?',
        answer: 'Only Admins can add new homes. Go to Homes from the sidebar, click "Add Home", and fill out the form with home details including name, address, phone, license number, capacity, and home type (Adult Family Home or Assisted Living Facility). Azure Maps integration provides address autocomplete.',
      },
      {
        question: 'How do I manage beds in a home?',
        answer: 'Bed management is integrated into the home setup. When creating or editing a home, specify the total capacity. Each bed is assigned a unique label. Bed occupancy is automatically tracked when clients are admitted or discharged.',
      },
      {
        question: 'How do I assign caregivers to a home?',
        answer: 'Go to the home\'s detail page and navigate to the Caregivers tab. Click "Assign Caregiver" and select from available caregivers. A caregiver can be assigned to multiple homes, and they will have access to all clients in their assigned homes.',
      },
      {
        question: 'What information is tracked for each home?',
        answer: 'For each home, the system tracks: basic info (name, address, contact), capacity and current occupancy, assigned caregivers, active clients, bed assignments, license information, and home type (AFH/ALF).',
      },
    ],
  },
  {
    category: 'Client Management',
    icon: <ContactsOutlined />,
    items: [
      {
        question: 'How do I admit a new client?',
        answer: 'Navigate to Clients from the sidebar, click "Admit Client", and fill out the comprehensive admission form. Required information includes personal details, emergency contacts, medical history, diagnoses, allergies, medications, and care preferences. Select an available bed during admission. Only Admins can admit clients.',
      },
      {
        question: 'How do I view client information?',
        answer: 'Go to the Clients page and select a client to view their profile. The profile has multiple tabs: Overview (demographics and medical info), Care Log (ADLs, vitals, medications, etc.), Activities, Incidents, Documents, and Appointments. Each tab provides detailed, chronological records.',
      },
      {
        question: 'Who can access client information?',
        answer: 'Admins have full access to all client data across all homes. Caregivers can only access clients in their assigned homes (home-scoped access). All PHI access is automatically logged in the audit trail for HIPAA compliance.',
      },
      {
        question: 'How do I discharge a client?',
        answer: 'Only Admins can discharge clients. From the client profile, click "Discharge Client", enter the discharge date, reason, and destination (if applicable). The bed automatically becomes available for new admissions. Historical records are preserved indefinitely for compliance.',
      },
      {
        question: 'Can I transfer a client to a different bed?',
        answer: 'Yes. Admins can transfer clients between beds within the same home or to a different home. Go to the client profile, click "Transfer Client", select the destination home and available bed. All transfers are logged in the audit trail.',
      },
    ],
  },
  {
    category: 'Caregiver Management',
    icon: <TeamOutlined />,
    items: [
      {
        question: 'How do I add a new caregiver?',
        answer: 'Navigate to Caregivers, click "Invite Caregiver", and enter their email, name, and phone number. An invitation email is sent with a secure setup link (valid for 7 days). The caregiver completes their profile, sets a password, and registers a passkey.',
      },
      {
        question: 'How do I assign homes to a caregiver?',
        answer: 'From the caregiver\'s profile page, use the "Assign Homes" function to select one or multiple homes. Caregivers will automatically have access to all clients in their assigned homes.',
      },
      {
        question: 'What happens when a caregiver leaves?',
        answer: 'Admins should deactivate (not delete) caregiver accounts when staff leave. Deactivation prevents login while preserving all historical records and audit trails associated with that caregiver for compliance purposes.',
      },
      {
        question: 'Can caregivers have different access levels?',
        answer: 'Currently, all caregivers have the same role-based permissions: they can view and log care for clients in their assigned homes but cannot perform administrative functions like admitting clients, managing users, or accessing audit logs.',
      },
    ],
  },
  {
    category: 'Daily Care Logging',
    icon: <FileTextOutlined />,
    items: [
      {
        question: 'What is the Care Log?',
        answer: 'The Care Log is a comprehensive, chronological record of all care activities for a client. It includes ADLs (Activities of Daily Living), vital signs, medications, ROM exercises, behavior notes, and activities. The Timeline tab shows all entries in a unified view.',
      },
      {
        question: 'How do I log ADLs (Activities of Daily Living)?',
        answer: 'From the client profile, go to the Care Log tab and select the ADL subtab. Click "Log ADL" to record the six Katz Index activities: bathing, dressing, toileting, transferring, continence, and feeding. Rate each as Independent, Partial Assist, Dependent, or N/A. Add optional notes.',
      },
      {
        question: 'How do I record vital signs?',
        answer: 'In the Care Log tab, select Vitals and click "Log Vitals". Enter measurements for blood pressure (systolic/diastolic), pulse, temperature, oxygen saturation, weight, and blood glucose. The system validates ranges and flags out-of-range values automatically.',
      },
      {
        question: 'How do I log medications?',
        answer: 'Go to the Medication tab in the Care Log. Click "Log Medication" to record medication administration. Select the medication from the client\'s medication list, enter dosage, route, time administered, and add notes about effectiveness or adverse reactions.',
      },
      {
        question: 'What are ROM exercises and how do I log them?',
        answer: 'ROM (Range of Motion) exercises help maintain joint flexibility. In the ROM tab, click "Log ROM Session" to record exercises performed. Select body parts/joints exercised, duration, repetitions, client tolerance, and any limitations or pain noted.',
      },
      {
        question: 'How do I record behavior and mood observations?',
        answer: 'Use the Behavior Notes tab to document client mood, behavior changes, interactions, or concerns. These qualitative observations complement quantitative data and provide valuable context for care planning. Each note is timestamped and attributed to you.',
      },
      {
        question: 'Can I edit or delete care log entries?',
        answer: 'No. For HIPAA compliance and data integrity, care log entries are immutable once submitted. If you need to correct an error, add a new note explaining the correction. All entries and corrections are preserved in the audit trail.',
      },
      {
        question: 'What is the Quick Log feature?',
        answer: 'Quick Log is a streamlined modal that lets caregivers rapidly log common activities without navigating to different tabs. It\'s ideal for documenting routine care quickly while moving between clients.',
      },
    ],
  },
  {
    category: 'Activity Tracking',
    icon: <PlayCircleOutlined />,
    items: [
      {
        question: 'How do I log recreational activities?',
        answer: 'From the client profile Activities tab, click "Log Activity" to record individual or group activities. Enter the activity name, type (recreational, social, exercise, etc.), duration, and participation level. For group activities, you can log all participants at once.',
      },
      {
        question: 'What types of activities should be tracked?',
        answer: 'Track all engagement activities: recreational (games, crafts, music), social (visiting, group events), exercise (walks, chair exercises), cognitive (puzzles, reading), and spiritual activities. Regular activity participation is important for quality of life and regulatory compliance.',
      },
      {
        question: 'Can I create recurring activities?',
        answer: 'The system supports logging individual activity instances. For recurring activities (like weekly bingo), you can log each occurrence. Future enhancements may include activity scheduling templates.',
      },
    ],
  },
  {
    category: 'Appointments',
    icon: <CalendarOutlined />,
    items: [
      {
        question: 'How do I schedule an appointment?',
        answer: 'Navigate to Appointments from the sidebar or from a client\'s profile. Click "Schedule Appointment", select the client, appointment type (medical, therapy, personal, etc.), date/time, location, provider, and add notes. Appointments appear on the dashboard when approaching.',
      },
      {
        question: 'What types of appointments can I track?',
        answer: 'Track medical appointments (doctor, dentist, specialist), therapy sessions (physical, occupational, speech), lab work, social services meetings, personal appointments (salon, family visits), and any other scheduled events.',
      },
      {
        question: 'How are upcoming appointments displayed?',
        answer: 'Upcoming appointments (next 7 days) appear on both Admin and Caregiver dashboards. The system displays today\'s appointments prominently and uses color-coded tags to highlight urgency. Click any appointment for full details.',
      },
      {
        question: 'Can I mark appointments as completed or missed?',
        answer: 'Yes. From the appointment details, you can update the status to Completed, Missed, or Cancelled. Add notes about the outcome, follow-up needed, or reason for cancellation. All status changes are logged.',
      },
    ],
  },
  {
    category: 'Incident Reporting',
    icon: <ExclamationCircleOutlined />,
    items: [
      {
        question: 'How do I report an incident?',
        answer: 'Navigate to Incidents from the sidebar and click "Report Incident". Fill out the comprehensive form including date/time, incident type, severity, description, involved parties, witnesses, immediate actions taken, and injuries. Submit for Admin review.',
      },
      {
        question: 'What types of incidents should be reported?',
        answer: 'Report all safety-related events: falls, medication errors, behavioral incidents, injuries, elopement attempts, equipment failures, security concerns, environmental hazards, infectious disease exposure, and any event affecting client safety or wellbeing.',
      },
      {
        question: 'What is the incident review workflow?',
        answer: 'New incidents start with status "New" and trigger notifications to Admins. Admins review incidents, add follow-up notes, document corrective actions, and update status to "Reviewed" or "Closed". The complete workflow is tracked in the incident record.',
      },
      {
        question: 'Who can view incident reports?',
        answer: 'Caregivers can view and report incidents for their assigned homes. Admins can view all incidents across all homes and manage the review workflow. Sysadmins can view incidents but not modify them. All access is logged.',
      },
      {
        question: 'Are incident notifications automatic?',
        answer: 'Yes. When a caregiver submits an incident report, Admins receive automatic notifications. Critical incidents (severe injuries, elopement, etc.) can be configured for immediate alerts.',
      },
    ],
  },
  {
    category: 'Document Management',
    icon: <FileTextOutlined />,
    items: [
      {
        question: 'What types of documents can I upload?',
        answer: 'Upload any client-related documents: medical records, consent forms, physician orders, care plans, assessments, incident reports, advance directives, insurance cards, and regulatory documents. Supported formats: PDF, Word, Excel, images (JPEG, PNG). Maximum size: 25MB.',
      },
      {
        question: 'How do I organize documents?',
        answer: 'Documents are organized by client and stored in folders: Medical Records, Care Plans, Consent Forms, Assessments, Insurance, Legal Documents, Photos, and Other. You can create custom folders and move documents between folders.',
      },
      {
        question: 'Can caregivers download documents?',
        answer: 'No. For HIPAA compliance and data protection, caregivers have view-only access to documents they\'re granted permission to see. Only Admins can download documents. All document access (view and download) is logged in the audit trail.',
      },
      {
        question: 'How do document permissions work?',
        answer: 'Admins can grant per-document access to specific caregivers. By default, caregivers cannot see any documents. Admins explicitly grant "view" permission on individual documents. This enables minimum necessary access control.',
      },
      {
        question: 'How long are documents retained?',
        answer: 'Per HIPAA requirements, all documents and records are retained indefinitely (minimum 6 years). Documents cannot be permanently deleted from the system. They can be marked as archived but remain accessible for compliance purposes.',
      },
      {
        question: 'Is there a document viewer?',
        answer: 'Yes. The system includes a built-in PDF viewer for secure in-browser document viewing. PDFs open with download/print buttons disabled for caregivers. For other file types, they can be viewed if the browser supports the format.',
      },
    ],
  },
  {
    category: 'Reporting',
    icon: <FileTextOutlined />,
    items: [
      {
        question: 'What reports are available?',
        answer: 'LenkCare Homes provides Client Summary Reports (comprehensive client history) and Home Summary Reports (home-wide statistics and activities). Reports aggregate data from all care modules into professional PDF documents.',
      },
      {
        question: 'How do I generate a client report?',
        answer: 'Navigate to Reports, select "Client Summary Report", choose the client and date range, then click Generate. The report compiles all ADLs, vitals, medications, activities, incidents, and appointments for that period into a formatted PDF.',
      },
      {
        question: 'What is included in a home report?',
        answer: 'Home Summary Reports include: occupancy statistics, client roster with key demographics, incident summary, activity participation rates, staff assignments, and aggregate care statistics. Useful for management review and regulatory reporting.',
      },
      {
        question: 'Can I customize report date ranges?',
        answer: 'Yes. All reports allow you to specify custom date ranges (last 7 days, last month, last quarter, custom start/end dates). This flexibility supports various reporting needs from weekly reviews to annual summaries.',
      },
      {
        question: 'Are report generations logged?',
        answer: 'Yes. All report generations are logged in the audit trail, including who generated the report, which client/home, date range, and timestamp. This ensures accountability for PHI access through reports.',
      },
    ],
  },
  {
    category: 'Audit Logs',
    id: 'audit-logs',
    icon: <AuditOutlined />,
    items: [
      {
        question: 'What is logged in the audit trail?',
        answer: 'The system logs all PHI access and modifications: user authentication (login, logout, MFA), client access, care log entries (ADLs, vitals, medications), document views, incident reports, administrative actions (user management, client admission/discharge), and system configuration changes. Each entry includes timestamp, user, action, IP address, and outcome.',
      },
      {
        question: 'How do I search audit logs?',
        answer: 'Navigate to Audit Logs (Admin/Sysadmin only). Use advanced filters to search by action type, date range, user, client, resource type, or outcome. Free text search is available. Results can be filtered, sorted, and exported to CSV for external analysis.',
      },
      {
        question: 'Can audit logs be modified or deleted?',
        answer: 'No. Audit logs are immutable and append-only. No user, including Admins and Sysadmins, can modify or delete audit entries. This ensures the integrity of the audit trail for compliance and investigation purposes.',
      },
      {
        question: 'How long are audit logs retained?',
        answer: 'Audit logs are retained indefinitely (minimum 6 years) per HIPAA requirements. They are stored in Azure Cosmos DB with append-only semantics and automated backups. Logs are never automatically purged.',
      },
      {
        question: 'What is the Activity Feed view?',
        answer: 'The Activity Feed provides a user-friendly, chronological view of audit log entries with grouping by date, color-coded action types, and detailed descriptions. It makes it easier to understand what happened when compared to raw log data.',
      },
    ],
  },
  {
    category: 'Security & Privacy',
    icon: <SafetyCertificateOutlined />,
    items: [
      {
        question: 'Is my data secure?',
        answer: 'Yes. LenkCare Homes is HIPAA-compliant with enterprise-grade security: TLS 1.2+ encryption in transit, AES-256 encryption at rest, phishing-resistant passkey authentication, role-based access control, home-scoped permissions, comprehensive audit logging, rate limiting, and security headers.',
      },
      {
        question: 'What is HIPAA compliance?',
        answer: 'HIPAA (Health Insurance Portability and Accountability Act) sets national standards for protecting sensitive patient health information. LenkCare Homes implements all required administrative, physical, and technical safeguards including encryption, access controls, and audit trails.',
      },
      {
        question: 'How is PHI protected?',
        answer: 'Protected Health Information (PHI) is encrypted at rest (Azure SQL TDE, Cosmos DB encryption) and in transit (TLS 1.2+). Access is restricted by role and home assignment. All PHI access is logged. Data is stored in HIPAA-compliant Azure services with Microsoft\'s Business Associate Agreement.',
      },
      {
        question: 'What are passkeys and why are they more secure?',
        answer: 'Passkeys use WebAuthn/FIDO2 technology for phishing-resistant authentication. Unlike passwords or SMS codes, passkeys cannot be intercepted, stolen, or phished. They use public-key cryptography bound to your device\'s biometrics (Face ID, Touch ID, Windows Hello) or PIN.',
      },
      {
        question: 'What is home-scoped access?',
        answer: 'Home-scoped access means caregivers can only view and log care for clients in their explicitly assigned homes. This implements the HIPAA principle of "minimum necessary" access. The system enforces this at both the UI and API level.',
      },
      {
        question: 'How are passwords protected?',
        answer: 'Passwords are hashed using industry-standard bcrypt with high cost factor before storage. Raw passwords are never stored. Password reset links are single-use and expire after 24 hours. All password changes are logged.',
      },
    ],
  },
];

// Keyboard shortcuts
const keyboardShortcuts = [
  { shortcut: 'Tab', action: 'Navigate to next focusable element' },
  { shortcut: 'Shift + Tab', action: 'Navigate to previous focusable element' },
  { shortcut: 'Enter', action: 'Activate focused button or link' },
  { shortcut: 'Space', action: 'Toggle checkbox or activate button' },
  { shortcut: 'Escape', action: 'Close modal or cancel action' },
  { shortcut: 'Arrow Keys', action: 'Navigate within menus and dropdowns' },
];

function HelpContent() {
  const [searchQuery, setSearchQuery] = useState('');
  const { startTour, resetTour, toursEnabled, setToursEnabled } = useTour();
  const availableTours = useAvailableTours();
  const [messageApi, contextHolder] = message.useMessage();
  const screens = useBreakpoint();
  const isMobile = !screens.md;
  const isSmallMobile = !screens.sm;

  const handleRestartTour = async (tourId: string) => {
    await resetTour(tourId);
    startTour(tourId);
    messageApi.success('Tour restarted! Look for the guided walkthrough.');
  };

  const handleToggleTours = (enabled: boolean) => {
    setToursEnabled(enabled);
    messageApi.info(enabled ? 'Tours have been enabled.' : 'Tours have been disabled.');
  };

  // Filter FAQs based on search
  const filteredFaqData = searchQuery
    ? faqData.map(category => ({
        ...category,
        items: category.items.filter(
          item =>
            item.question.toLowerCase().includes(searchQuery.toLowerCase()) ||
            item.answer.toLowerCase().includes(searchQuery.toLowerCase())
        ),
      })).filter(category => category.items.length > 0)
    : faqData;

  return (
    <div role="main" aria-label="Help and Documentation">
      {/* Header */}
      <div style={{ marginBottom: isSmallMobile ? 16 : 32 }}>
        <Space align="center">
          <QuestionCircleOutlined style={{ fontSize: isSmallMobile ? 24 : 28, color: '#5a7a6b' }} aria-hidden="true" />
          <div>
            <Title level={isSmallMobile ? 3 : 2} style={{ margin: 0, color: '#2d3732' }}>Help & Documentation</Title>
            {!isSmallMobile && (
              <Paragraph style={{ color: '#6b7770', marginBottom: 0 }}>
                Find answers to common questions and learn how to use LenkCare Homes
              </Paragraph>
            )}
          </div>
        </Space>
      </div>

      {/* Search */}
      <Card style={{ marginBottom: 24 }} size={isMobile ? 'small' : 'default'}>
        <Search
          placeholder="Search help topics..."
          allowClear
          size={isMobile ? 'middle' : 'large'}
          prefix={<SearchOutlined aria-hidden="true" />}
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{ maxWidth: isMobile ? '100%' : 500 }}
          aria-label="Search help topics"
        />
      </Card>

      <Row gutter={24}>
        {/* Main Content */}
        <Col xs={24} lg={18}>
          {/* Quick Start Guide */}
          <Card 
            title={
              <Space>
                <BookOutlined aria-hidden="true" />
                <span>Quick Start Guide</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="quick-start"
          >
            <Paragraph>
              Welcome to LenkCare Homes! This HIPAA-compliant platform helps you manage adult family homes efficiently and securely.
            </Paragraph>
            
            <Title level={5}>For Administrators</Title>
            <List
              dataSource={[
                'Log in with your credentials and register a passkey using your device biometrics (Face ID, Touch ID, or Windows Hello)',
                'Start with the Dashboard for an at-a-glance view of all homes, occupancy, and recent activity',
                'Set up Homes with details, capacity, and license information',
                'Invite Caregivers via email and assign them to specific homes',
                'Admit Clients and assign them to available beds',
                'Manage Documents with granular per-document access control',
                'Review Incidents reported by caregivers and track resolution',
                'Generate Reports for internal review or regulatory compliance',
                'Monitor system activity through comprehensive Audit Logs',
              ]}
              renderItem={(item, index) => (
                <List.Item>
                  <Space>
                    <Tag color="green">{index + 1}</Tag>
                    <span>{item}</span>
                  </Space>
                </List.Item>
              )}
            />
            
            <Divider />
            
            <Title level={5}>For Caregivers</Title>
            <List
              dataSource={[
                'Log in with your credentials and set up your passkey for secure, biometric authentication',
                'View your Dashboard to see your assigned homes and clients',
                'Access Client profiles to view their care history, documents, and schedules',
                'Log Daily Care using the comprehensive Care Log: ADLs, vitals, medications, ROM exercises, and behavior notes',
                'Track Activities and document client engagement in recreational, social, and exercise programs',
                'Schedule and manage Appointments for medical visits, therapy sessions, and other events',
                'Report Incidents immediately using the detailed incident form',
                'Use the Timeline view for a chronological overview of all care activities',
                'Access your granted Documents for each client (view-only for compliance)',
              ]}
              renderItem={(item, index) => (
                <List.Item>
                  <Space>
                    <Tag color="blue">{index + 1}</Tag>
                    <span>{item}</span>
                  </Space>
                </List.Item>
              )}
            />
            
            <Alert
              message="Security & Compliance"
              description="All your actions are automatically logged in an immutable audit trail. Work confidently knowing your data is encrypted, access-controlled, and fully HIPAA-compliant."
              type="info"
              showIcon
              style={{ marginTop: 16 }}
            />
          </Card>

          {/* Guided Tour */}
          <Card 
            title={
              <Space>
                <PlayCircleOutlined aria-hidden="true" />
                <span>Guided Tour</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="guided-tour"
          >
            {contextHolder}
            <Paragraph>
              Take an interactive tour to learn about the key features of LenkCare Homes. 
              The tour highlights important navigation elements and walks you through the interface.
              Perfect for new users or anyone wanting to explore features they haven&apos;t used yet.
            </Paragraph>
            
            {availableTours.length > 0 ? (
              <Space direction="vertical" style={{ width: '100%' }}>
                {availableTours.map(tour => (
                  <Card 
                    key={tour.id} 
                    size="small"
                    style={{ 
                      borderColor: tour.isCompleted ? '#5a7a6b' : undefined,
                      backgroundColor: tour.isCompleted ? '#f6ffed' : undefined 
                    }}
                  >
                    <Row justify="space-between" align="middle">
                      <Col>
                        <Space direction="vertical" size={0}>
                          <Space>
                            <Text strong>{tour.name}</Text>
                            {tour.isCompleted && <Tag color="success">Completed</Tag>}
                            {tour.isDismissed && !tour.isCompleted && <Tag color="warning">Skipped</Tag>}
                          </Space>
                          <Text type="secondary">{tour.description}</Text>
                          <Text type="secondary" style={{ fontSize: 12 }}>
                            {tour.steps.length} steps • Estimated {Math.ceil(tour.steps.length * 0.5)} minutes
                          </Text>
                        </Space>
                      </Col>
                      <Col>
                        <Button
                          type={tour.isCompleted ? 'default' : 'primary'}
                          icon={<PlayCircleOutlined />}
                          onClick={() => handleRestartTour(tour.id)}
                          aria-label={`${tour.isCompleted ? 'Restart' : 'Start'} ${tour.name}`}
                        >
                          {tour.isCompleted ? 'Restart Tour' : 'Start Tour'}
                        </Button>
                      </Col>
                    </Row>
                  </Card>
                ))}
              </Space>
            ) : (
              <Alert
                message="No tours available"
                description="There are no guided tours available for your role at this time. Tours are customized based on your permissions and assigned responsibilities."
                type="info"
                showIcon
              />
            )}

            <Divider />
            
            <Row justify="space-between" align="middle">
              <Col>
                <Text type="secondary">
                  Tours help new users learn the interface. You can disable automatic tours if you prefer.
                </Text>
              </Col>
              <Col>
                <Button 
                  onClick={() => handleToggleTours(!toursEnabled)}
                  aria-pressed={toursEnabled}
                >
                  {toursEnabled ? 'Disable Auto Tours' : 'Enable Auto Tours'}
                </Button>
              </Col>
            </Row>
          </Card>

          {/* System Features Overview */}
          <Card
            title={
              <Space>
                <SafetyCertificateOutlined aria-hidden="true" />
                <span>System Features Overview</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="features-overview"
          >
            <Row gutter={[16, 16]}>
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="Core Management">
                  <List size="small">
                    <List.Item>
                      <HomeOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Home Management:</strong> Track facilities, beds, capacity, licenses
                    </List.Item>
                    <List.Item>
                      <UserOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Client Management:</strong> Admissions, discharges, transfers, profiles
                    </List.Item>
                    <List.Item>
                      <TeamOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Caregiver Management:</strong> Invitations, assignments, home-scoped access
                    </List.Item>
                  </List>
                </Card>
              </Col>
              
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="Daily Care">
                  <List size="small">
                    <List.Item>
                      <CheckCircleOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>ADL Tracking:</strong> Katz Index-based activities of daily living
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Vitals Monitoring:</strong> BP, pulse, temperature, oxygen saturation
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Medications:</strong> Administration tracking with dosage & routes
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>ROM Exercises:</strong> Range of motion activity logging
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Behavior Notes:</strong> Mood and behavior observations
                    </List.Item>
                  </List>
                </Card>
              </Col>
              
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="Activities & Engagement">
                  <List size="small">
                    <List.Item>
                      <CalendarOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Activity Logging:</strong> Individual and group recreational activities
                    </List.Item>
                    <List.Item>
                      <CalendarOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Appointments:</strong> Medical, therapy, personal scheduling
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Unified Timeline:</strong> Chronological view of all care events
                    </List.Item>
                  </List>
                </Card>
              </Col>
              
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="Documentation & Reporting">
                  <List size="small">
                    <List.Item>
                      <ExclamationCircleOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Incident Reporting:</strong> Comprehensive tracking with review workflow
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Document Storage:</strong> Secure Azure Blob with per-document access
                    </List.Item>
                    <List.Item>
                      <FileTextOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>PDF Reports:</strong> Client summaries, home reports, custom date ranges
                    </List.Item>
                  </List>
                </Card>
              </Col>
              
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="Security & Compliance">
                  <List size="small">
                    <List.Item>
                      <SafetyCertificateOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Passkey Auth:</strong> WebAuthn/FIDO2 phishing-resistant biometrics
                    </List.Item>
                    <List.Item>
                      <AuditOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Audit Logging:</strong> Immutable trail of all PHI access
                    </List.Item>
                    <List.Item>
                      <KeyOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Encryption:</strong> TLS 1.2+ in transit, AES-256 at rest
                    </List.Item>
                    <List.Item>
                      <SafetyCertificateOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>RBAC:</strong> Role-based + home-scoped access control
                    </List.Item>
                  </List>
                </Card>
              </Col>
              
              <Col xs={24} md={12}>
                <Card type="inner" size="small" title="User Experience">
                  <List size="small">
                    <List.Item>
                      <CheckCircleOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Guided Tours:</strong> Interactive walkthroughs for new users
                    </List.Item>
                    <List.Item>
                      <CheckCircleOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Accessibility:</strong> WCAG 2.1 AA compliant, keyboard navigation
                    </List.Item>
                    <List.Item>
                      <CheckCircleOutlined style={{ marginRight: 8, color: '#5a7a6b' }} />
                      <strong>Responsive Design:</strong> Desktop, tablet, mobile optimized
                    </List.Item>
                  </List>
                </Card>
              </Col>
            </Row>
          </Card>

          {/* User Roles */}
          <Card 
            title={
              <Space>
                <TeamOutlined aria-hidden="true" />
                <span>User Roles & Permissions</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="user-roles"
          >
            <Table
              pagination={false}
              dataSource={[
                {
                  key: 'admin',
                  role: 'Admin',
                  description: 'Full access to all features and data',
                  permissions: 'Manage homes, clients, caregivers, documents, reports, audit logs, user accounts',
                },
                {
                  key: 'caregiver',
                  role: 'Caregiver',
                  description: 'Access limited to assigned homes',
                  permissions: 'View assigned clients, log care activities, record vitals, report incidents, view (not download) documents',
                },
                {
                  key: 'sysadmin',
                  role: 'Sysadmin',
                  description: 'System maintenance only',
                  permissions: 'View audit logs, system monitoring - cannot access or modify PHI',
                },
              ]}
              columns={[
                { title: 'Role', dataIndex: 'role', key: 'role', render: (text: string) => <Tag color="blue">{text}</Tag> },
                { title: 'Description', dataIndex: 'description', key: 'description' },
                { title: 'Permissions', dataIndex: 'permissions', key: 'permissions' },
              ]}
              aria-label="User roles and permissions table"
            />
          </Card>

          {/* FAQs */}
          <Card 
            title={
              <Space>
                <QuestionCircleOutlined aria-hidden="true" />
                <span>Frequently Asked Questions</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="faq"
          >
            {filteredFaqData.length === 0 ? (
              <Alert
                message="No results found"
                description="Try adjusting your search terms or browse all categories below."
                type="info"
                showIcon
              />
            ) : (
              filteredFaqData.map((category) => (
                <div key={category.category} id={category.id || category.category.toLowerCase().replace(/\s+/g, '-')} style={{ marginBottom: 24 }}>
                  <Space style={{ marginBottom: 12 }}>
                    {category.icon}
                    <Title level={5} style={{ margin: 0 }}>{category.category}</Title>
                  </Space>
                  <Collapse
                    items={category.items.map((item, index) => ({
                      key: `${category.category}-${index}`,
                      label: item.question,
                      children: <Paragraph style={{ margin: 0 }}>{item.answer}</Paragraph>,
                    }))}
                    expandIconPosition="end"
                  />
                </div>
              ))
            )}
          </Card>

          {/* Best Practices */}
          <Card
            title={
              <Space>
                <CheckCircleOutlined aria-hidden="true" />
                <span>Best Practices for Care Documentation</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="best-practices"
          >
            <Paragraph>
              Follow these best practices to ensure high-quality care documentation and HIPAA compliance:
            </Paragraph>
            
            <Title level={5}>Documentation Guidelines</Title>
            <List
              dataSource={[
                'Document care activities in real-time or as soon as possible after providing care',
                'Be specific and objective in your notes - use factual observations rather than subjective opinions',
                'Include all six ADL categories when logging, even if the client was independent or assistance was not needed',
                'Record vital signs at consistent times each day for accurate trend tracking',
                'Note any unusual observations, changes in condition, or concerns immediately',
                'Use the behavior notes feature to document mood, interactions, and behavioral changes',
                'Document medication administration immediately after giving medications',
                'Record all incidents promptly with detailed descriptions and immediate actions taken',
              ]}
              renderItem={(item) => (
                <List.Item>
                  <CheckCircleOutlined style={{ color: '#5a7a6b', marginRight: 8 }} />
                  {item}
                </List.Item>
              )}
            />
            
            <Divider />
            
            <Title level={5}>Security & Privacy</Title>
            <List
              dataSource={[
                'Always log out when leaving your workstation, even briefly',
                'Never share your login credentials or passkey with anyone',
                'Only access client information when necessary for providing care',
                'Do not discuss client information in public areas or with unauthorized persons',
                'Lock your device screen when stepping away, even momentarily',
                'Report any suspected security incidents or privacy breaches immediately',
                'Review only the documents you\'re explicitly granted access to',
                'Remember: all your actions are logged for compliance and accountability',
              ]}
              renderItem={(item) => (
                <List.Item>
                  <SafetyCertificateOutlined style={{ color: '#5a7a6b', marginRight: 8 }} />
                  {item}
                </List.Item>
              )}
            />
            
            <Divider />
            
            <Title level={5}>Efficiency Tips</Title>
            <List
              dataSource={[
                'Use the Quick Log feature for rapid documentation of routine care',
                'Set up multiple passkeys on different devices for convenient access',
                'Take advantage of the Timeline view to see the complete care history at a glance',
                'Use the Dashboard to check upcoming appointments and birthdays at the start of your shift',
                'Filter and sort client lists to prioritize your work efficiently',
                'Complete guided tours for features you haven\'t used yet',
                'Enable browser notifications to stay informed of important updates',
                'Bookmark frequently accessed client profiles for quick navigation',
              ]}
              renderItem={(item) => (
                <List.Item>
                  <CheckCircleOutlined style={{ color: '#5a7a6b', marginRight: 8 }} />
                  {item}
                </List.Item>
              )}
            />
          </Card>

          {/* Keyboard Shortcuts */}
          <Card 
            title={
              <Space>
                <KeyOutlined aria-hidden="true" />
                <span>Keyboard Shortcuts</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="keyboard-shortcuts"
          >
            <Paragraph>
              LenkCare Homes supports keyboard navigation for accessibility. Use these shortcuts to navigate efficiently:
            </Paragraph>
            <Table
              pagination={false}
              dataSource={keyboardShortcuts.map((item, index) => ({ ...item, key: index }))}
              columns={[
                { 
                  title: 'Shortcut', 
                  dataIndex: 'shortcut', 
                  key: 'shortcut', 
                  render: (text: string) => <Text keyboard>{text}</Text>,
                  width: 150,
                },
                { title: 'Action', dataIndex: 'action', key: 'action' },
              ]}
              aria-label="Keyboard shortcuts table"
            />
          </Card>

          {/* Accessibility Statement */}
          <Card 
            title={
              <Space>
                <SafetyCertificateOutlined aria-hidden="true" />
                <span>Accessibility Statement</span>
              </Space>
            }
            style={{ marginBottom: 24 }}
            id="accessibility"
          >
            <Paragraph>
              LenkCare Homes is committed to ensuring digital accessibility for people with disabilities. 
              We continually improve the user experience for everyone and apply relevant accessibility standards.
            </Paragraph>
            <Title level={5}>Conformance Status</Title>
            <Paragraph>
              We strive to conform to the Web Content Accessibility Guidelines (WCAG) 2.1 level AA. 
              These guidelines explain how to make web content more accessible for people with disabilities.
            </Paragraph>
            <Title level={5}>Accessibility Features</Title>
            <List
              dataSource={[
                'Keyboard navigation support throughout the application',
                'Skip links to bypass navigation and reach main content',
                'ARIA labels and landmarks for screen reader compatibility',
                'Sufficient color contrast ratios for text readability',
                'Focus indicators for interactive elements',
                'Form labels and error messages for screen readers',
                'Responsive design for various screen sizes and devices',
              ]}
              renderItem={(item) => <List.Item>{item}</List.Item>}
            />
            <Title level={5}>Feedback</Title>
            <Paragraph>
              We welcome your feedback on the accessibility of LenkCare Homes. 
              Please contact us if you encounter accessibility barriers or have suggestions for improvement.
            </Paragraph>
          </Card>

          {/* Contact Support */}
          <Card 
            title={
              <Space>
                <PhoneOutlined aria-hidden="true" />
                <span>Contact Support</span>
              </Space>
            }
            id="contact"
          >
            <Row gutter={24}>
              <Col xs={24} sm={12}>
                <Space direction="vertical">
                  <Text strong>Technical Support</Text>
                  <Space>
                    <MailOutlined aria-hidden="true" />
                    <a href="mailto:support@lenkcare.com">support@lenkcare.com</a>
                  </Space>
                  <Space>
                    <PhoneOutlined aria-hidden="true" />
                    <a href="tel:+18001234567">1-800-123-4567</a>
                  </Space>
                  <Text type="secondary">Available Monday - Friday, 8am - 6pm PST</Text>
                </Space>
              </Col>
              <Col xs={24} sm={12}>
                <Space direction="vertical">
                  <Text strong>Emergency Support</Text>
                  <Paragraph>
                    For urgent issues affecting patient safety or system availability, 
                    call our 24/7 emergency line:
                  </Paragraph>
                  <Space>
                    <PhoneOutlined aria-hidden="true" />
                    <a href="tel:+18009876543">1-800-987-6543</a>
                  </Space>
                </Space>
              </Col>
            </Row>
          </Card>
        </Col>

        {/* Sidebar Navigation - hidden on mobile */}
        {!isMobile && (
          <Col xs={24} lg={6}>
            <Card 
              title="On This Page" 
              style={{ position: 'sticky', top: 88 }}
              size="small"
            >
              <Anchor
                items={[
                  { key: 'quick-start', href: '#quick-start', title: 'Quick Start Guide' },
                  { key: 'guided-tour', href: '#guided-tour', title: 'Guided Tour' },
                  { key: 'features-overview', href: '#features-overview', title: 'Features Overview' },
                  { key: 'user-roles', href: '#user-roles', title: 'User Roles' },
                  { 
                    key: 'faq', 
                    href: '#faq', 
                    title: 'FAQ Categories',
                    children: [
                      { key: 'getting-started', href: '#getting-started', title: 'Getting Started' },
                      { key: 'home-management', href: '#home-management', title: 'Home Management' },
                      { key: 'client-management', href: '#client-management', title: 'Client Management' },
                      { key: 'caregiver-management', href: '#caregiver-management', title: 'Caregiver Management' },
                      { key: 'daily-care-logging', href: '#daily-care-logging', title: 'Daily Care Logging' },
                      { key: 'activity-tracking', href: '#activity-tracking', title: 'Activity Tracking' },
                      { key: 'appointments', href: '#appointments', title: 'Appointments' },
                      { key: 'incident-reporting', href: '#incident-reporting', title: 'Incident Reporting' },
                      { key: 'document-management', href: '#document-management', title: 'Document Management' },
                      { key: 'reporting', href: '#reporting', title: 'Reporting' },
                      { key: 'audit-logs', href: '#audit-logs', title: 'Audit Logs' },
                      { key: 'security-privacy', href: '#security-&-privacy', title: 'Security & Privacy' },
                    ],
                  },
                  { key: 'best-practices', href: '#best-practices', title: 'Best Practices' },
                  { key: 'keyboard-shortcuts', href: '#keyboard-shortcuts', title: 'Keyboard Shortcuts' },
                  { key: 'accessibility', href: '#accessibility', title: 'Accessibility' },
                  { key: 'contact', href: '#contact', title: 'Contact Support' },
                ]}
                offsetTop={100}
                aria-label="Page navigation"
              />
            </Card>
          </Col>
        )}
      </Row>
    </div>
  );
}

export default function HelpPage() {
  return (
    <ProtectedRoute>
      <AuthenticatedLayout>
        <HelpContent />
      </AuthenticatedLayout>
    </ProtectedRoute>
  );
}
