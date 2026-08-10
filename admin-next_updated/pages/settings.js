import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  OutlinedInput,
  Select as MuiSelect,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

const documentTransportOptions = [
  { value: 0, label: 'Manual verification' },
  { value: 1, label: 'JSON base64 payload' },
  { value: 2, label: 'Multipart form upload' },
  { value: 3, label: 'Blob/object reference' },
  { value: 4, label: 'Public document URL' },
];

const fieldTransportOptions = [
  { value: 0, label: 'Manual verification' },
  { value: 5, label: 'Field value only' },
];

const commonFileExtensions = [
  '.pdf',
  '.doc',
  '.docx',
  '.jpg',
  '.jpeg',
  '.png',
  '.csv',
  '.xls',
  '.xlsx',
  '.txt',
  '.zip',
];

const integrationFieldOptions = [
  { value: 'kraPin', label: 'KRA PIN' },
  { value: 'licenseNumber', label: 'License number' },
  { value: 'businessRegistrationNumber', label: 'Business registration number' },
  { value: 'professionalLicenseNumber', label: 'Professional license number' },
  { value: 'nationalId', label: 'National ID' },
];

const conditionTargets = [
  { value: 'statusCode', label: 'HTTP status code' },
  { value: 'body.status', label: 'Response body status' },
  { value: 'body.message', label: 'Response body message' },
  { value: 'body.reference', label: 'Response body reference' },
];

const conditionOperators = [
  { value: 'equals', label: 'Equals' },
  { value: 'contains', label: 'Contains' },
  { value: 'in', label: 'In list' },
  { value: 'exists', label: 'Exists' },
];

const verificationStageOptions = [
  { value: 0, label: 'Registration' },
  { value: 1, label: 'Profile completion' },
  { value: 2, label: 'Job application' },
  { value: 3, label: 'Employer publishing' },
  { value: 4, label: 'Admin review' },
];

const verificationPolicyModeOptions = [
  { value: 0, label: 'Verified status gate' },
  { value: 1, label: 'Mandatory documents gate' },
  { value: 2, label: 'Document integration policy' },
  { value: 3, label: 'Field integration policy' },
];

const verificationActionOptions = {
  '0:0': [{ value: 'RegisterProfessional', label: 'Professional registration' }],
  '0:1': [{ value: 'UpdateProfessionalProfile', label: 'Save professional profile' }],
  '0:2': [{ value: 'ApplyForJob', label: 'Apply for job' }],
  '0:4': [{ value: 'ReviewProfessional', label: 'Admin professional review' }],
  '1:0': [{ value: 'RegisterEmployer', label: 'Employer registration' }],
  '1:3': [{ value: 'PublishJob', label: 'Publish employer job' }],
  '1:4': [{ value: 'ReviewEmployer', label: 'Admin employer review' }],
};

const targetTypeLabel = (value) => String(value) === '0' || String(value) === 'Professional' ? 'Professional' : 'Employer';
const stageLabel = (value) => verificationStageOptions.find((item) => Number(item.value) === Number(value) || item.label === value)?.label || String(value);
const policyModeLabel = (value) => verificationPolicyModeOptions.find((item) => Number(item.value) === Number(value) || item.label === value)?.label || String(value);
const transportLabel = (value) => [...documentTransportOptions, ...fieldTransportOptions].find((item) => Number(item.value) === Number(value))?.label || String(value);
const channelLabel = (value) => ({ 0: 'Email', 1: 'SMS', 2: 'WhatsApp' }[Number(value)] || String(value));
const subjectLabel = (value) => ({ Document: 'Document upload', EmployerField: 'Employer field', ProfessionalField: 'Professional field' }[value] || value);
const blankPair = () => ({ key: '', value: '' });
const blankMapRule = () => ({ source: '', target: '' });
const blankCondition = () => ({ target: 'statusCode', operator: 'equals', expected: '' });

const parseEntries = (raw, fallback) => {
  if (!raw) return fallback;
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed?.entries)) return parsed.entries;
    if (Array.isArray(parsed)) return parsed;
  } catch {
    return fallback;
  }
  return fallback;
};

export default function SettingsPage() {
  const [user, setUser] = useState(null);
  const [configuration, setConfiguration] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState('communications');
  const [activePlatformSection, setActivePlatformSection] = useState('categories');
  const [editing, setEditing] = useState({ type: '', id: '' });
  const [category, setCategory] = useState({ name: '', slug: '', isActive: true });
  const [jobEngagementType, setJobEngagementType] = useState({ name: '', slug: '', description: '', allowsShiftPattern: false, isActive: true, displayOrder: 0 });
  const emptyPlan = { name: '', slug: '', description: '', priceAmount: 0, currency: 'USD', billingInterval: 'Monthly', maxPublishedJobs: 1, maxTeamMembers: 1, maxCandidateInvitesPerPeriod: 0, maxMessagesPerPeriod: 0, canAccessJobPostingModule: true, canAccessApplicantReviewModule: true, canAccessTalentSearchModule: false, canAccessReportsModule: false, canAccessCommunicationsModule: false, canViewProfessionalProfiles: true, canViewProfessionalContactDetails: false, canViewProfessionalDocuments: false, canViewProfessionalVerificationStatus: true, canInviteCandidates: true, canMessageCandidates: true, canUseEmailCommunications: true, canUseSmsCommunications: false, canUseWhatsAppCommunications: false, requiresEmployerVerificationToPublishJobs: true, isDefault: false };
  const [plan, setPlan] = useState(emptyPlan);
  const [rule, setRule] = useState({ targetType: 0, appliesToCategoryOrFacilityType: '', documentType: '', isMandatory: true });
  const [policy, setPolicy] = useState({ name: '', subjectType: 0, stage: 1, actionKey: 'UpdateProfessionalProfile', policyMode: 0, documentType: '', fieldName: '', integrationConfigId: '', requireVerifiedStatusForAction: true, requireAllMandatoryDocuments: true, blockOnPending: true, blockOnFailure: true, bypassWhenIntegrationMissing: true, allowManualOverride: true, notes: '' });
  const [documentType, setDocumentType] = useState({ name: '', slug: '', targetType: 1, description: '', allowedExtensions: '.pdf,.doc,.docx,.jpg,.jpeg,.png', maxFileSizeMb: 10, isActive: true });
  const [integration, setIntegration] = useState({ name: '', subject: 'Document', documentType: '', fieldName: '', transportMode: 0, endpointUrl: '', httpMethod: 'POST', apiKeySecret: '', authenticationType: 'None', requestHeadersJson: '', queryParametersJson: '', requestBodyTemplate: '', requestFieldMapJson: '', successConditionsJson: '', failureConditionsJson: '', responseMapJson: '', timeoutSeconds: 30, retryCount: 0, retryDelaySeconds: 0, retryOnTimeout: false, retryOn5xx: true, parseJsonResponse: true, storeRawRequestResponse: true, isEnabled: false, allowManualOverride: true });
  const [declarations, setDeclarations] = useState([]);
  const [declaration, setDeclaration] = useState({ flowKey: 'job-posting', title: '', body: '', isRequired: true, isActive: true, displayOrder: 0 });
  const [integrationAccordion, setIntegrationAccordion] = useState('target');
  const [requestHeaders, setRequestHeaders] = useState([blankPair()]);
  const [queryParameters, setQueryParameters] = useState([blankPair()]);
  const [requestMappings, setRequestMappings] = useState([blankMapRule()]);
  const [responseMappings, setResponseMappings] = useState([blankMapRule()]);
  const [successConditions, setSuccessConditions] = useState([blankCondition()]);
  const [failureConditions, setFailureConditions] = useState([blankCondition()]);
  const [passwordPolicy, setPasswordPolicy] = useState({ minLength: 8, requireUppercase: false, requireLowercase: false, requireDigit: false, requireSymbol: false });
  const [passwordForm, setPasswordForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [communicationConfigs, setCommunicationConfigs] = useState([]);
  const [communicationForm, setCommunicationForm] = useState({ channel: 0, providerName: 'SMTP', isEnabled: false, baseUrl: '', senderId: '', apiKeySecret: '', accountSid: '', templateNamespace: '', simulateWhenDisabled: true });
  const [testMessage, setTestMessage] = useState({ channel: 0, recipient: '', subject: '', body: '' });
  const [appearance, setAppearance] = useState({ accent: '#8b004a', sidebarCollapsed: false, density: 'comfortable' });
  const [paymentConfigs, setPaymentConfigs] = useState([]);
  const [paymentTransactions, setPaymentTransactions] = useState([]);
  const [paymentForm, setPaymentForm] = useState({ provider: 0, displayName: 'M-Pesa', isEnabled: false, isSandbox: true, apiBaseUrl: 'https://sandbox.safaricom.co.ke', clientId: '', clientSecret: '', businessShortCode: '', passKey: '', receiverAccount: '', callbackUrl: '', callbackVerificationToken: '', currency: 'KES', promptFieldsJson: '[{"key":"phoneNumber","label":"Mobile number","required":true}]' });
  const [paygoRules, setPaygoRules] = useState([]);
  const [paygoCharges, setPaygoCharges] = useState([]);
  const [paygoRule, setPaygoRule] = useState({ action: 0, isEnabled: false, freeUnitsPerPeriod: 10, unitPrice: 0, currency: 'USD', periodKey: 'Monthly', requirePaymentBeforeAction: true, description: '' });
  const [contentPages, setContentPages] = useState([]);
  const [contentPage, setContentPage] = useState({ slug: 'privacy', title: 'Privacy Policy', htmlContent: '', cssContent: '', isPublished: true });

  useEffect(() => {
    if (typeof window === 'undefined') return;
    setAppearance({
      accent: localStorage.getItem('medsocial.admin.accent') || '#8b004a',
      sidebarCollapsed: localStorage.getItem('medsocial.admin.sidebarCollapsed') === 'true',
      density: localStorage.getItem('medsocial.admin.density') || 'comfortable',
    });
  }, []);

  const load = async () => {
    const [currentUser, config, comms, policyConfig, payments, transactions, declarationConfigs, paygoRuleConfigs, paygoChargeRows, legalPages] = await Promise.all([adminApi.getCurrentUser(), adminApi.getConfiguration(), adminApi.getCommunicationConfigs(), adminApi.getPasswordPolicy(), adminApi.getPaymentConfigs(), adminApi.getPaymentTransactions(), adminApi.getDeclarations(), adminApi.getPayAsYouGoRules(), adminApi.getPayAsYouGoCharges(), adminApi.getContentPages()]);
    setUser(currentUser);
    setConfiguration(config);
    setCommunicationConfigs(comms || []);
    setPasswordPolicy(policyConfig || passwordPolicy);
    setPaymentConfigs(payments || []);
    setPaymentTransactions(transactions || []);
    setDeclarations(declarationConfigs || []);
    setPaygoRules(paygoRuleConfigs || []);
    setPaygoCharges(paygoChargeRows || []);
    setContentPages(legalPages || []);
  };

  useEffect(() => {
    load().catch(() => undefined);
  }, []);

  const submit = async (fn, payload, successText) => {
    setError('');
    try {
      await fn(payload);
      setMessage(successText);
      setTimeout(() => setMessage(''), 3200);
      await load();
    } catch (requestError) {
      setError(requestError.message || 'Unable to save configuration.');
      setTimeout(() => setError(''), 4200);
    }
  };

  const saveConfig = async (type, createFn, updateFn, payload, successText, resetFn) => {
    const isEditing = editing.type === type && editing.id;
    await submit(
      isEditing ? (body) => updateFn(editing.id, body) : createFn,
      payload,
      successText
    );
    if (resetFn) resetFn();
    setEditing({ type: '', id: '' });
  };

  const saveCommunicationConfig = async (event) => {
    event.preventDefault();
    setError('');
    try {
      await adminApi.saveCommunicationConfig({ ...communicationForm, channel: Number(communicationForm.channel) });
      setMessage('Communication provider configuration saved.');
      setCommunicationForm((current) => ({ ...current, apiKeySecret: '' }));
      await load();
    } catch (requestError) {
      setError(requestError.message || 'Unable to save communication provider.');
    }
  };

  const sendTestMessage = async (event) => {
    event.preventDefault();
    setError('');
    try {
      await adminApi.sendCommunication({
        ...testMessage,
        channel: Number(testMessage.channel),
        tenantId: user?.tenantId,
        userId: user?.id,
        templateKey: 'admin-test',
        relatedEntityName: 'AdminConfiguration',
        relatedEntityId: user?.id,
      });
      setMessage('Test communication recorded/sent successfully.');
      setTestMessage({ channel: 0, recipient: '', subject: '', body: '' });
    } catch (requestError) {
      setError(requestError.message || 'Unable to send test communication.');
    }
  };

  const changePassword = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    try {
      await adminApi.changePassword(passwordForm);
      setMessage('Password updated successfully.');
      setPasswordForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
    } catch (requestError) {
      setError(requestError.message || 'Unable to update password.');
    }
  };

  const savePasswordPolicy = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    try {
      const saved = await adminApi.updatePasswordPolicy({ ...passwordPolicy, minLength: Number(passwordPolicy.minLength || 1) });
      setPasswordPolicy(saved);
      setMessage('Password policy updated successfully.');
      setTimeout(() => setMessage(''), 3200);
    } catch (requestError) {
      setError(requestError.message || 'Unable to update password policy.');
      setTimeout(() => setError(''), 4200);
    }
  };

  const saveAppearance = (event) => {
    event.preventDefault();
    localStorage.setItem('medsocial.admin.accent', appearance.accent);
    localStorage.setItem('medsocial.admin.sidebarCollapsed', String(appearance.sidebarCollapsed));
    localStorage.setItem('medsocial.admin.density', appearance.density);
    document.documentElement.style.setProperty('--accent', appearance.accent);
    setMessage('Admin appearance preferences saved.');
    setTimeout(() => setMessage(''), 3200);
  };

  const listCard = (key, title, meta, onEdit) => (
    <button key={key} type="button" className="config-list-card interactive" onClick={onEdit}>
      <strong>{title}</strong>
      <span>{meta}</span>
    </button>
  );

  const integrationSubjectIsDocument = integration.subject === 'Document';
  const availableTransportOptions = integrationSubjectIsDocument ? documentTransportOptions : fieldTransportOptions;
  const integrationIsManual = Number(integration.transportMode) === 0;
  const selectedDocumentExtensions = (documentType.allowedExtensions || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
  const integrationPayload = useMemo(() => ({
    ...integration,
    requestHeadersJson: JSON.stringify({ entries: requestHeaders.filter((item) => item.key || item.value) }),
    queryParametersJson: JSON.stringify({ entries: queryParameters.filter((item) => item.key || item.value) }),
    requestFieldMapJson: JSON.stringify({ entries: requestMappings.filter((item) => item.source || item.target) }),
    successConditionsJson: JSON.stringify({ entries: successConditions.filter((item) => item.expected || item.operator === 'exists') }),
    failureConditionsJson: JSON.stringify({ entries: failureConditions.filter((item) => item.expected || item.operator === 'exists') }),
    responseMapJson: JSON.stringify({ entries: responseMappings.filter((item) => item.source || item.target) }),
  }), [integration, requestHeaders, queryParameters, requestMappings, successConditions, failureConditions, responseMappings]);

  const availablePolicyActions = verificationActionOptions[`${policy.subjectType}:${policy.stage}`] || [];
  const availablePolicyIntegrations = (configuration?.verificationIntegrations || []).filter((item) => {
    if (Number(policy.policyMode) === 2) {
      return item.subject === 'Document' && (!policy.documentType || item.documentType === policy.documentType);
    }
    if (Number(policy.policyMode) === 3) {
      const expectedSubject = Number(policy.subjectType) === 0 ? 'ProfessionalField' : 'EmployerField';
      return item.subject === expectedSubject && (!policy.fieldName || item.fieldName === policy.fieldName);
    }
    return true;
  });

  const resetIntegrationForm = () => {
    setIntegration({
      name: '',
      subject: 'Document',
      documentType: '',
      fieldName: '',
      transportMode: 0,
      endpointUrl: '',
      httpMethod: 'POST',
      apiKeySecret: '',
      authenticationType: 'None',
      requestHeadersJson: '',
      queryParametersJson: '',
      requestBodyTemplate: '',
      requestFieldMapJson: '',
      successConditionsJson: '',
      failureConditionsJson: '',
      responseMapJson: '',
      timeoutSeconds: 30,
      retryCount: 0,
      retryDelaySeconds: 0,
      retryOnTimeout: false,
      retryOn5xx: true,
      parseJsonResponse: true,
      storeRawRequestResponse: true,
      isEnabled: false,
      allowManualOverride: true,
    });
    setRequestHeaders([blankPair()]);
    setQueryParameters([blankPair()]);
    setRequestMappings([blankMapRule()]);
    setResponseMappings([blankMapRule()]);
    setSuccessConditions([blankCondition()]);
    setFailureConditions([blankCondition()]);
    setIntegrationAccordion('target');
  };

  const updateCollection = (setter, rows, index, key, value) => {
    setter(rows.map((item, currentIndex) => (currentIndex === index ? { ...item, [key]: value } : item)));
  };

  const platformSections = [
    { key: 'categories', label: 'Categories', count: configuration?.categories?.length || 0 },
    { key: 'jobTypes', label: 'Job types', count: configuration?.jobEngagementTypes?.length || 0 },
    { key: 'plans', label: 'Plans', count: configuration?.subscriptionPlans?.length || 0 },
    { key: 'documents', label: 'Documents', count: configuration?.documentTypes?.length || 0 },
    { key: 'rules', label: 'Document rules', count: configuration?.requiredDocumentRules?.length || 0 },
    { key: 'verification', label: 'Verification', count: configuration?.verificationPolicies?.length || 0 },
    { key: 'declarations', label: 'Declarations', count: declarations.length },
    { key: 'legal', label: 'Legal pages', count: contentPages.length },
    { key: 'integrations', label: 'Integrations', count: configuration?.verificationIntegrations?.length || 0 },
  ];

  return (
    <AdminShell user={user} title="Settings and Configuration" subtitle="Admin-managed rules for integrations, categories, plans, document requirements, and verification policy.">
      {(message || error) && <div className="toast-stack"><div className={`toast ${error ? 'error' : 'success'}`}>{error || message}</div></div>}

      <div className="tabs" style={{ marginTop: 20 }}>
        {[
          ['communications', 'Communications'],
          ['payments', 'Payments'],
          ['platform', 'Platform rules'],
          ['content', 'Content and theme'],
          ['appearance', 'Admin appearance'],
          ['account', 'Account security'],
        ].map(([key, label]) => (
          <button key={key} className={`tab-button ${activeTab === key ? 'active' : ''}`} onClick={() => setActiveTab(key)}>{label}</button>
        ))}
      </div>

      {activeTab === 'account' && (
        <details className="collapsible" open>
          <summary>Password and account security</summary>
          <div className="collapsible-body">
            <p className="panel-subtitle">Rotate the signed-in admin password without leaving the console.</p>
            <form className="form-grid" style={{ marginTop: 16 }} onSubmit={changePassword}>
              <label className="field-label">Current password<input className="input" type="password" value={passwordForm.currentPassword} onChange={(event) => setPasswordForm({ ...passwordForm, currentPassword: event.target.value })} /></label>
              <label className="field-label">New password<input className="input" type="password" value={passwordForm.newPassword} onChange={(event) => setPasswordForm({ ...passwordForm, newPassword: event.target.value })} /></label>
              <label className="field-label">Confirm new password<input className="input" type="password" value={passwordForm.confirmNewPassword} onChange={(event) => setPasswordForm({ ...passwordForm, confirmNewPassword: event.target.value })} /></label>
              <div className="button-row"><button className="btn-primary" type="submit">Update password</button></div>
            </form>
          </div>
        </details>
      )}

      {activeTab === 'account' && (
        <details className="collapsible" open>
          <summary>Platform password policy</summary>
          <div className="collapsible-body">
            <p className="panel-subtitle">Configure the password rules used for registration, self-service password changes, and admin password resets.</p>
            <form className="form-grid" style={{ marginTop: 16 }} onSubmit={savePasswordPolicy}>
              <label className="field-label">Minimum password length<input className="input" type="number" min="1" max="128" value={passwordPolicy.minLength} onChange={(event) => setPasswordPolicy({ ...passwordPolicy, minLength: Number(event.target.value) })} /></label>
              <label className="switch-card"><input type="checkbox" checked={passwordPolicy.requireUppercase} onChange={(event) => setPasswordPolicy({ ...passwordPolicy, requireUppercase: event.target.checked })} /> Require uppercase</label>
              <label className="switch-card"><input type="checkbox" checked={passwordPolicy.requireLowercase} onChange={(event) => setPasswordPolicy({ ...passwordPolicy, requireLowercase: event.target.checked })} /> Require lowercase</label>
              <label className="switch-card"><input type="checkbox" checked={passwordPolicy.requireDigit} onChange={(event) => setPasswordPolicy({ ...passwordPolicy, requireDigit: event.target.checked })} /> Require number</label>
              <label className="switch-card"><input type="checkbox" checked={passwordPolicy.requireSymbol} onChange={(event) => setPasswordPolicy({ ...passwordPolicy, requireSymbol: event.target.checked })} /> Require symbol</label>
              <div className="button-row"><button className="btn-primary" type="submit">Save password policy</button></div>
            </form>
          </div>
        </details>
      )}

      {activeTab === 'communications' && (
        <>
          <div className="panel-grid" style={{ marginTop: 18 }}>
            <details className="collapsible" open>
              <summary>Provider configuration</summary>
              <div className="collapsible-body">
                <p className="panel-subtitle">Configure email, SMS, and WhatsApp provider credentials. Disabled providers are recorded as simulated sends.</p>
                <form className="form-grid" style={{ marginTop: 16 }} onSubmit={saveCommunicationConfig}>
                  <label className="field-label">Channel<select className="select" value={communicationForm.channel} onChange={(event) => setCommunicationForm({ ...communicationForm, channel: Number(event.target.value) })}>
                    <option value={0}>Email</option>
                    <option value={1}>SMS</option>
                    <option value={2}>WhatsApp</option>
                  </select></label>
                  <label className="field-label">Provider name<input className="input" value={communicationForm.providerName} onChange={(event) => setCommunicationForm({ ...communicationForm, providerName: event.target.value })} /></label>
                  <label className="field-label">Base URL or SMTP host<input className="input" value={communicationForm.baseUrl} onChange={(event) => setCommunicationForm({ ...communicationForm, baseUrl: event.target.value })} /></label>
                  <label className="field-label">Sender ID or from address<input className="input" value={communicationForm.senderId} onChange={(event) => setCommunicationForm({ ...communicationForm, senderId: event.target.value })} /></label>
                  <label className="field-label">API key or password secret<input className="input" type="password" value={communicationForm.apiKeySecret} onChange={(event) => setCommunicationForm({ ...communicationForm, apiKeySecret: event.target.value })} /></label>
                  <label className="field-label">Account SID or tenant reference<input className="input" value={communicationForm.accountSid} onChange={(event) => setCommunicationForm({ ...communicationForm, accountSid: event.target.value })} /></label>
                  <label className="field-label">Template namespace<input className="input" value={communicationForm.templateNamespace} onChange={(event) => setCommunicationForm({ ...communicationForm, templateNamespace: event.target.value })} /></label>
                  <label className="switch-card"><input type="checkbox" checked={communicationForm.isEnabled} onChange={(event) => setCommunicationForm({ ...communicationForm, isEnabled: event.target.checked })} /> Enabled</label>
                  <label className="switch-card"><input type="checkbox" checked={communicationForm.simulateWhenDisabled} onChange={(event) => setCommunicationForm({ ...communicationForm, simulateWhenDisabled: event.target.checked })} /> Simulate when disabled</label>
                  <div className="button-row"><button className="btn-primary" type="submit">Save integration</button></div>
                </form>
              </div>
            </details>

            <details className="collapsible" open>
              <summary>Send test communication</summary>
              <div className="collapsible-body">
                <form className="stack" onSubmit={sendTestMessage}>
                  <label className="field-label">Channel<select className="select" value={testMessage.channel} onChange={(event) => setTestMessage({ ...testMessage, channel: Number(event.target.value) })}>
                    <option value={0}>Email</option>
                    <option value={1}>SMS</option>
                    <option value={2}>WhatsApp</option>
                  </select></label>
                  <label className="field-label">Recipient email or phone<input className="input" value={testMessage.recipient} onChange={(event) => setTestMessage({ ...testMessage, recipient: event.target.value })} /></label>
                  <label className="field-label">Subject<input className="input" value={testMessage.subject} onChange={(event) => setTestMessage({ ...testMessage, subject: event.target.value })} /></label>
                  <label className="field-label">Message body<textarea className="textarea" value={testMessage.body} onChange={(event) => setTestMessage({ ...testMessage, body: event.target.value })} /></label>
                  <button className="btn-primary" type="submit">Send / record test</button>
                </form>
              </div>
            </details>
          </div>

          <details className="collapsible" open>
            <summary>Configured providers</summary>
            <div className="collapsible-body">
              <div className="stack">
                {communicationConfigs.map((item) => <div key={item.id} style={{ borderRadius: 18, background: 'var(--panel-soft)', padding: 14 }}>{channelLabel(item.channel)} - {item.providerName} ({item.isEnabled ? 'enabled' : 'disabled'})</div>)}
              </div>
            </div>
          </details>
        </>
      )}

      {activeTab === 'payments' && (
        <div className="settings-workspace">
          <div className="settings-stage">
            <div className="settings-stage-header"><div><p className="eyebrow">Billing integrations</p><h2>Subscription payment providers</h2><span>Configure where subscription payments are deposited and test provider authentication before enabling checkout.</span></div></div>
            <div className="split-editor">
              <div>
                <div className="form-grid spacious-form">
                  <label className="field-label">Provider<select className="select" value={paymentForm.provider} onChange={(event) => {
                    const provider = Number(event.target.value);
                    setPaymentForm({ ...paymentForm, provider, displayName: provider === 0 ? 'M-Pesa' : 'PayPal', currency: provider === 0 ? 'KES' : 'USD', apiBaseUrl: provider === 0 ? 'https://sandbox.safaricom.co.ke' : 'https://api-m.sandbox.paypal.com', promptFieldsJson: provider === 0 ? '[{"key":"phoneNumber","label":"Mobile number","required":true}]' : '[{"key":"email","label":"PayPal email","required":true}]' });
                  }}><option value={0}>M-Pesa</option><option value={1}>PayPal</option></select></label>
                  <label className="field-label">Display name<input className="input" value={paymentForm.displayName} onChange={(event) => setPaymentForm({ ...paymentForm, displayName: event.target.value })} /></label>
                  <label className="field-label">API base URL<input className="input" value={paymentForm.apiBaseUrl} onChange={(event) => setPaymentForm({ ...paymentForm, apiBaseUrl: event.target.value })} /></label>
                  <label className="field-label">Currency<input className="input" value={paymentForm.currency} onChange={(event) => setPaymentForm({ ...paymentForm, currency: event.target.value.toUpperCase() })} /></label>
                  <label className="field-label">Client ID / consumer key<input className="input" value={paymentForm.clientId} onChange={(event) => setPaymentForm({ ...paymentForm, clientId: event.target.value })} /></label>
                  <label className="field-label">Client secret / consumer secret<input className="input" type="password" value={paymentForm.clientSecret} onChange={(event) => setPaymentForm({ ...paymentForm, clientSecret: event.target.value })} /></label>
                  {Number(paymentForm.provider) === 0 && <><label className="field-label">Business shortcode<input className="input" value={paymentForm.businessShortCode} onChange={(event) => setPaymentForm({ ...paymentForm, businessShortCode: event.target.value })} /></label><label className="field-label">Lipa na M-Pesa passkey<input className="input" type="password" value={paymentForm.passKey} onChange={(event) => setPaymentForm({ ...paymentForm, passKey: event.target.value })} /></label></>}
                  <label className="field-label">Deposit account / receiver email<input className="input" value={paymentForm.receiverAccount} onChange={(event) => setPaymentForm({ ...paymentForm, receiverAccount: event.target.value })} /></label>
                  <label className="field-label">Callback URL<input className="input" value={paymentForm.callbackUrl} onChange={(event) => setPaymentForm({ ...paymentForm, callbackUrl: event.target.value })} /></label>
                  <label className="field-label">Callback verification token<input className="input" type="password" value={paymentForm.callbackVerificationToken} onChange={(event) => setPaymentForm({ ...paymentForm, callbackVerificationToken: event.target.value })} /><small>Append this value as the callback URL token query parameter.</small></label>
                  <label className="field-label" style={{ gridColumn: '1 / -1' }}>Checkout fields JSON<textarea className="textarea" value={paymentForm.promptFieldsJson} onChange={(event) => setPaymentForm({ ...paymentForm, promptFieldsJson: event.target.value })} /></label>
                  <label className="switch-card"><input type="checkbox" checked={paymentForm.isSandbox} onChange={(event) => setPaymentForm({ ...paymentForm, isSandbox: event.target.checked })} /> Sandbox mode</label>
                  <label className="switch-card"><input type="checkbox" checked={paymentForm.isEnabled} onChange={(event) => setPaymentForm({ ...paymentForm, isEnabled: event.target.checked })} /> Enabled for checkout</label>
                </div>
                <div className="button-row" style={{ marginTop: 16 }}>
                  <button className="btn-primary" onClick={() => submit(adminApi.savePaymentConfig, paymentForm, 'Payment configuration saved.')}>Save provider</button>
                  <button className="btn-secondary" onClick={() => submit(() => adminApi.testPaymentConfig(paymentForm.provider), null, 'Provider authentication test passed.')}>Test connection</button>
                </div>
              </div>
              <div className="config-list">{paymentConfigs.map((item) => listCard(item.provider, item.displayName, `${item.isEnabled ? 'Enabled' : 'Disabled'} · ${item.isSandbox ? 'Sandbox' : 'Live'} · ${item.currency} · ${item.receiverAccount || 'No receiver set'}`, () => setPaymentForm({ ...paymentForm, ...item, clientSecret: '', passKey: '' })))}</div>
            </div>
          </div>
          <div className="settings-stage">
            <div className="settings-stage-header"><div><p className="eyebrow">Payment operations</p><h2>Recent subscription requests</h2><span>Successful provider callbacks provision subscriptions automatically; pending requests remain available for manual review.</span></div></div>
            <div className="config-list config-list-wide">{paymentTransactions.map((item) => <div key={item.id} className="config-list-card">
              <strong>{item.currency} {Number(item.amount).toFixed(2)} · {item.status}</strong>
              <span>{new Date(item.createdAt).toLocaleString()} · {item.provider ?? 'Admin review'} · Employer {item.employerId}</span>
              {(item.status === 'PendingAdminReview' || item.status === 'AwaitingCustomerAction') && <button className="btn-primary" style={{ marginTop: 10 }} onClick={() => submit(adminApi.activateSubscription, { employerId: item.employerId, planId: item.planId, durationDays: null, paymentTransactionId: item.id, notes: 'Payment/request approved by administrator.' }, 'Subscription request approved and activated.')}>Approve and activate</button>}
            </div>)}</div>
          </div>
        </div>
      )}

          {activeTab === 'payments' && (
            <>
              <div className="settings-stage">
                <div className="settings-stage-header"><div><p className="eyebrow">Pay as you go</p><h2>Usage-based billing rules</h2><span>Configure free allowances and per-action charges for professional job views and employer job postings.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Charged action<select className="select" value={paygoRule.action} onChange={(event) => setPaygoRule({ ...paygoRule, action: Number(event.target.value) })}><option value={0}>Professional job view</option><option value={1}>Employer job posting</option></select></label>
                      <label className="field-label">Free units per period<input className="input" type="number" min="0" value={paygoRule.freeUnitsPerPeriod} onChange={(event) => setPaygoRule({ ...paygoRule, freeUnitsPerPeriod: Number(event.target.value) })} /></label>
                      <label className="field-label">Unit price<input className="input" type="number" min="0" step="0.01" value={paygoRule.unitPrice} onChange={(event) => setPaygoRule({ ...paygoRule, unitPrice: Number(event.target.value) })} /></label>
                      <label className="field-label">Currency<input className="input" value={paygoRule.currency} onChange={(event) => setPaygoRule({ ...paygoRule, currency: event.target.value.toUpperCase() })} /></label>
                      <label className="field-label">Usage period<select className="select" value={paygoRule.periodKey} onChange={(event) => setPaygoRule({ ...paygoRule, periodKey: event.target.value })}><option value="Monthly">Monthly</option><option value="Daily">Daily</option></select></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Client-facing note<textarea className="textarea" value={paygoRule.description} onChange={(event) => setPaygoRule({ ...paygoRule, description: event.target.value })} placeholder="Example: First 20 job views this month are free. Additional views are charged at the configured rate." /></label>
                      <label className="switch-card"><input type="checkbox" checked={paygoRule.isEnabled} onChange={(event) => setPaygoRule({ ...paygoRule, isEnabled: event.target.checked })} /> Enable this usage rule</label>
                      <label className="switch-card"><input type="checkbox" checked={paygoRule.requirePaymentBeforeAction} onChange={(event) => setPaygoRule({ ...paygoRule, requirePaymentBeforeAction: event.target.checked })} /> Require payment before continuing</label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => submit(adminApi.savePayAsYouGoRule, paygoRule, 'Pay-as-you-go rule saved.')}>Save usage rule</button>
                    </div>
                  </div>
                  <div className="config-list">{paygoRules.map((item) => listCard(item.action, item.action === 'ProfessionalJobView' || item.action === 0 ? 'Professional job views' : 'Employer job postings', `${item.isEnabled ? 'Enabled' : 'Disabled'} - ${item.freeUnitsPerPeriod} free/${item.periodKey || 'Monthly'} - ${item.currency} ${Number(item.unitPrice || 0).toFixed(2)}`, () => setPaygoRule({ action: item.action === 'ProfessionalJobView' ? 0 : item.action === 'EmployerJobPosting' ? 1 : Number(item.action), isEnabled: !!item.isEnabled, freeUnitsPerPeriod: item.freeUnitsPerPeriod ?? 0, unitPrice: Number(item.unitPrice || 0), currency: item.currency || 'USD', periodKey: item.periodKey || 'Monthly', requirePaymentBeforeAction: item.requirePaymentBeforeAction !== false, description: item.description || '' })))}</div>
                </div>
              </div>
              <div className="settings-stage">
                <div className="settings-stage-header"><div><p className="eyebrow">Usage ledger</p><h2>Recent pay-as-you-go activity</h2><span>Track free usage, pending payment requests, admin-review requests, and paid usage events.</span></div></div>
                <div className="config-list config-list-wide">{paygoCharges.map((item) => <div key={item.id} className="config-list-card">
                  <strong>{item.currency} {Number(item.amount || 0).toFixed(2)} - {item.status}</strong>
                  <span>{item.action} - {new Date(item.createdAt).toLocaleString()} - {item.employerId ? 'Employer usage' : 'Professional usage'}</span>
                </div>)}</div>
              </div>
            </>
          )}

      {activeTab === 'appearance' && (
        <div className="settings-workbench" style={{ marginTop: 20 }}>
          <aside className="settings-rail">
            <button className="active" type="button">Console theme</button>
            <p>Admins can tune console defaults here. These preferences apply immediately in this browser session and are persisted locally.</p>
          </aside>
          <section className="settings-stage">
            <div className="settings-stage-header">
              <div>
                <p className="eyebrow">Appearance</p>
                <h2>Admin console display</h2>
                <span>Control accent colour, density, and the default sidebar posture.</span>
              </div>
            </div>
            <form className="form-grid spacious-form" onSubmit={saveAppearance}>
              <label className="field-label">Accent colour<input className="input" type="color" value={appearance.accent} onChange={(event) => setAppearance({ ...appearance, accent: event.target.value })} /></label>
              <label className="field-label">Screen density<select className="select" value={appearance.density} onChange={(event) => setAppearance({ ...appearance, density: event.target.value })}><option value="comfortable">Comfortable</option><option value="compact">Compact</option></select></label>
              <label className="switch-card"><input type="checkbox" checked={appearance.sidebarCollapsed} onChange={(event) => setAppearance({ ...appearance, sidebarCollapsed: event.target.checked })} /> Start with sidebar collapsed</label>
              <div className="button-row"><button className="btn-primary" type="submit">Save appearance</button></div>
            </form>
          </section>
        </div>
      )}

      {activeTab === 'content' && (
        <div className="settings-workbench" style={{ marginTop: 20 }}>
          <aside className="settings-rail">
            <div className="config-card" style={{ padding: 16 }}>
              <strong>Landing page</strong>
              <span>Hero slides, feature cards, testimonials, and callouts.</span>
            </div>
            <div className="config-card" style={{ padding: 16 }}>
              <strong>Legal pages</strong>
              <span>{contentPages.length} managed pages with publish controls.</span>
            </div>
            <div className="config-card" style={{ padding: 16 }}>
              <strong>Client theme</strong>
              <span>Light and dark palettes for public and authenticated screens.</span>
            </div>
          </aside>
          <section className="settings-stage">
            <div className="settings-stage-header">
              <div>
                <p className="eyebrow">Public experience</p>
                <h2>Landing, legal pages, and client theme</h2>
                <span>Open focused editors from Settings so public content, legal documents, and the visual system stay in one admin area.</span>
              </div>
            </div>
            <div className="content-editor-grid">
              <Link className="content-editor-card" href="/landing">
                <span className="content-editor-icon">L</span>
                <strong>Landing page editor</strong>
                <small>Manage hero slides, testimonials, content blocks, visibility, and public-marketplace messaging.</small>
              </Link>
              <Link className="content-editor-card" href="/legal">
                <span className="content-editor-icon">P</span>
                <strong>Privacy and terms editor</strong>
                <small>Edit sections, anchors, public links, HTML/CSS fallback, and PDF or Word document publishing.</small>
              </Link>
              <Link className="content-editor-card" href="/theme">
                <span className="content-editor-icon">T</span>
                <strong>Client theme designer</strong>
                <small>Design separate light and dark palettes and preview several public client pages before publishing.</small>
              </Link>
            </div>
          </section>
        </div>
      )}

      {activeTab === 'platform' && (
        <div className="settings-workbench" style={{ marginTop: 20 }}>
          <aside className="settings-rail">
            {platformSections.map((section) => (
              <button key={section.key} className={activePlatformSection === section.key ? 'active' : ''} onClick={() => setActivePlatformSection(section.key)}>
                <span>{section.label}</span>
                <strong>{section.count}</strong>
              </button>
            ))}
          </aside>

          <section className="settings-stage">
            {activePlatformSection === 'categories' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Catalog</p><h2>Professional categories</h2><span>Maintain the cadres used by job posts, profiles, filters, and eligibility rules.</span></div></div>
                <div className="split-editor split-editor-wide">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Category name<input className="input" value={category.name} onChange={(event) => setCategory({ ...category, name: event.target.value })} /></label>
                      <label className="field-label">Slug<input className="input" value={category.slug} onChange={(event) => setCategory({ ...category, slug: event.target.value })} /></label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('category', adminApi.createCategory, adminApi.updateCategory, category, editing.type === 'category' ? 'Category updated.' : 'Category saved.', () => setCategory({ name: '', slug: '', isActive: true }))}>{editing.type === 'category' ? 'Update category' : 'Add category'}</button>
                    </div>
                  </div>
                  <div className="config-list">{configuration?.categories?.map((item) => listCard(item.slug, item.name, `${item.slug}${item.isActive ? '' : ' · Inactive'}`, () => { setEditing({ type: 'category', id: item.id }); setCategory({ name: item.name, slug: item.slug, isActive: item.isActive }); }))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'jobTypes' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Job posting catalog</p><h2>Engagement and period types</h2><span>Control the job type options employers and admins use when creating openings, including shift-driven roles.</span></div></div>
                <div className="split-editor split-editor-wide">
                  <div>
                    <div className="form-grid">
                      <label className="field-label">Name<input className="input" value={jobEngagementType.name} onChange={(event) => setJobEngagementType({ ...jobEngagementType, name: event.target.value })} placeholder="Permanent, Contract, Shift driven" /></label>
                      <label className="field-label">Slug<input className="input" value={jobEngagementType.slug} onChange={(event) => setJobEngagementType({ ...jobEngagementType, slug: event.target.value })} placeholder="contract" /></label>
                      <label className="field-label">Display order<input className="input" type="number" value={jobEngagementType.displayOrder} onChange={(event) => setJobEngagementType({ ...jobEngagementType, displayOrder: Number(event.target.value) })} /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Description<textarea className="textarea" value={jobEngagementType.description} onChange={(event) => setJobEngagementType({ ...jobEngagementType, description: event.target.value })} placeholder="Explain when this posting type should be used." /></label>
                      <label className="switch-card"><input type="checkbox" checked={jobEngagementType.allowsShiftPattern} onChange={(event) => setJobEngagementType({ ...jobEngagementType, allowsShiftPattern: event.target.checked })} /> Capture shift pattern</label>
                      <label className="switch-card"><input type="checkbox" checked={jobEngagementType.isActive} onChange={(event) => setJobEngagementType({ ...jobEngagementType, isActive: event.target.checked })} /> Active</label>
                      <button className="btn-primary" type="button" onClick={() => saveConfig('jobEngagementType', adminApi.createJobEngagementType, adminApi.updateJobEngagementType, jobEngagementType, 'Job posting type saved.', () => setJobEngagementType({ name: '', slug: '', description: '', allowsShiftPattern: false, isActive: true, displayOrder: 0 }))}>{editing.type === 'jobEngagementType' ? 'Update job type' : 'Add job type'}</button>
                    </div>
                  </div>
                  <div className="config-list">{configuration?.jobEngagementTypes?.map((item) => listCard(item.slug, item.name, `${item.description || 'No description'}${item.allowsShiftPattern ? ' · Shift pattern enabled' : ''}${item.isActive ? '' : ' · Inactive'}`, () => { setEditing({ type: 'jobEngagementType', id: item.id }); setJobEngagementType({ name: item.name, slug: item.slug, description: item.description || '', allowsShiftPattern: !!item.allowsShiftPattern, isActive: item.isActive !== false, displayOrder: item.displayOrder || 0 }); }))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'plans' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Commercial rules</p><h2>Subscription plans</h2><span>Configure employer bundles across module access, talent visibility, outreach channels, publishing limits, and pricing.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Plan name<input className="input" value={plan.name} onChange={(event) => setPlan({ ...plan, name: event.target.value })} /></label>
                      <label className="field-label">Slug<input className="input" value={plan.slug} onChange={(event) => setPlan({ ...plan, slug: event.target.value })} /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Bundle description<textarea className="textarea" value={plan.description} onChange={(event) => setPlan({ ...plan, description: event.target.value })} /></label>
                      <label className="field-label">Tier price<input className="input" type="number" min="0" step="0.01" value={plan.priceAmount} onChange={(event) => setPlan({ ...plan, priceAmount: Number(event.target.value) })} /></label>
                      <label className="field-label">Currency<input className="input" value={plan.currency} onChange={(event) => setPlan({ ...plan, currency: event.target.value.toUpperCase() })} /></label>
                      <label className="field-label">Billing interval<select className="select" value={plan.billingInterval} onChange={(event) => setPlan({ ...plan, billingInterval: event.target.value })}><option value="Monthly">Monthly</option><option value="Quarterly">Quarterly</option><option value="Biannual">Biannual</option><option value="Annual">Annual</option><option value="OneTime">One-time</option></select></label>
                      <label className="field-label">Maximum published jobs<input className="input" type="number" value={plan.maxPublishedJobs} onChange={(event) => setPlan({ ...plan, maxPublishedJobs: Number(event.target.value) })} /></label>
                      <label className="field-label">Maximum team users<input className="input" type="number" min="-1" value={plan.maxTeamMembers} onChange={(event) => setPlan({ ...plan, maxTeamMembers: Number(event.target.value) })} /><small>Use -1 for unlimited.</small></label>
                      <label className="field-label">Candidate invites per period<input className="input" type="number" min="-1" value={plan.maxCandidateInvitesPerPeriod} onChange={(event) => setPlan({ ...plan, maxCandidateInvitesPerPeriod: Number(event.target.value) })} /></label>
                      <label className="field-label">Messages per period<input className="input" type="number" min="-1" value={plan.maxMessagesPerPeriod} onChange={(event) => setPlan({ ...plan, maxMessagesPerPeriod: Number(event.target.value) })} /></label>
                      <div className="config-card" style={{ gridColumn: '1 / -1', padding: 16 }}>
                        <strong>Workspace modules</strong>
                        <div className="form-grid" style={{ marginTop: 12 }}>
                          <label className="switch-card"><input type="checkbox" checked={plan.canAccessJobPostingModule} onChange={(event) => setPlan({ ...plan, canAccessJobPostingModule: event.target.checked })} /> Job posting</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canAccessApplicantReviewModule} onChange={(event) => setPlan({ ...plan, canAccessApplicantReviewModule: event.target.checked })} /> Applicant review</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canAccessTalentSearchModule} onChange={(event) => setPlan({ ...plan, canAccessTalentSearchModule: event.target.checked })} /> Talent search</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canAccessReportsModule} onChange={(event) => setPlan({ ...plan, canAccessReportsModule: event.target.checked })} /> Reports</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canAccessCommunicationsModule} onChange={(event) => setPlan({ ...plan, canAccessCommunicationsModule: event.target.checked })} /> Communications</label>
                        </div>
                      </div>
                      <div className="config-card" style={{ gridColumn: '1 / -1', padding: 16 }}>
                        <strong>Professional details visibility</strong>
                        <div className="form-grid" style={{ marginTop: 12 }}>
                          <label className="switch-card"><input type="checkbox" checked={plan.canViewProfessionalProfiles} onChange={(event) => setPlan({ ...plan, canViewProfessionalProfiles: event.target.checked })} /> View professional directory</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canViewProfessionalContactDetails} onChange={(event) => setPlan({ ...plan, canViewProfessionalContactDetails: event.target.checked })} /> View contact details</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canViewProfessionalDocuments} onChange={(event) => setPlan({ ...plan, canViewProfessionalDocuments: event.target.checked })} /> View professional documents</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canViewProfessionalVerificationStatus} onChange={(event) => setPlan({ ...plan, canViewProfessionalVerificationStatus: event.target.checked })} /> View verification status</label>
                        </div>
                      </div>
                      <div className="config-card" style={{ gridColumn: '1 / -1', padding: 16 }}>
                        <strong>Outreach and communications</strong>
                        <div className="form-grid" style={{ marginTop: 12 }}>
                          <label className="switch-card"><input type="checkbox" checked={plan.canInviteCandidates} onChange={(event) => setPlan({ ...plan, canInviteCandidates: event.target.checked })} /> Can invite candidates</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canMessageCandidates} onChange={(event) => setPlan({ ...plan, canMessageCandidates: event.target.checked })} /> Can message candidates</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canUseEmailCommunications} onChange={(event) => setPlan({ ...plan, canUseEmailCommunications: event.target.checked })} /> Email available</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canUseSmsCommunications} onChange={(event) => setPlan({ ...plan, canUseSmsCommunications: event.target.checked })} /> SMS available</label>
                          <label className="switch-card"><input type="checkbox" checked={plan.canUseWhatsAppCommunications} onChange={(event) => setPlan({ ...plan, canUseWhatsAppCommunications: event.target.checked })} /> WhatsApp available</label>
                        </div>
                      </div>
                      <label className="switch-card"><input type="checkbox" checked={plan.isDefault} onChange={(event) => setPlan({ ...plan, isDefault: event.target.checked })} /> Default plan</label>
                      <label className="switch-card"><input type="checkbox" checked={plan.requiresEmployerVerificationToPublishJobs} onChange={(event) => setPlan({ ...plan, requiresEmployerVerificationToPublishJobs: event.target.checked })} /> Verification required to publish</label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('plan', adminApi.createPlan, adminApi.updatePlan, plan, editing.type === 'plan' ? 'Plan updated.' : 'Plan saved.', () => setPlan(emptyPlan))}>{editing.type === 'plan' ? 'Update plan' : 'Add plan'}</button>
                    </div>
                  </div>
                  <div className="config-list">{configuration?.subscriptionPlans?.map((item) => listCard(
                    item.slug,
                    item.name,
                    `${item.currency || 'USD'} ${Number(item.priceAmount || 0).toFixed(2)} / ${item.billingInterval || 'Monthly'} · ${item.maxPublishedJobs} jobs · ${item.canAccessTalentSearchModule ? 'Talent search' : 'No talent search'} · ${item.canInviteCandidates ? 'Invites on' : 'Invites off'}${item.isDefault ? ' · Default' : ''}`,
                    () => {
                      setEditing({ type: 'plan', id: item.id });
                      setPlan({
                        name: item.name,
                        slug: item.slug,
                        description: item.description || '',
                        priceAmount: item.priceAmount ?? 0,
                        currency: item.currency || 'USD',
                        billingInterval: item.billingInterval || 'Monthly',
                        maxPublishedJobs: item.maxPublishedJobs,
                        maxTeamMembers: item.maxTeamMembers ?? 1,
                        maxCandidateInvitesPerPeriod: item.maxCandidateInvitesPerPeriod ?? 0,
                        maxMessagesPerPeriod: item.maxMessagesPerPeriod ?? 0,
                        canAccessJobPostingModule: !!item.canAccessJobPostingModule,
                        canAccessApplicantReviewModule: !!item.canAccessApplicantReviewModule,
                        canAccessTalentSearchModule: !!item.canAccessTalentSearchModule,
                        canAccessReportsModule: !!item.canAccessReportsModule,
                        canAccessCommunicationsModule: !!item.canAccessCommunicationsModule,
                        canViewProfessionalProfiles: !!item.canViewProfessionalProfiles,
                        canViewProfessionalContactDetails: !!item.canViewProfessionalContactDetails,
                        canViewProfessionalDocuments: !!item.canViewProfessionalDocuments,
                        canViewProfessionalVerificationStatus: !!item.canViewProfessionalVerificationStatus,
                        canInviteCandidates: !!item.canInviteCandidates,
                        canMessageCandidates: !!item.canMessageCandidates,
                        canUseEmailCommunications: !!item.canUseEmailCommunications,
                        canUseSmsCommunications: !!item.canUseSmsCommunications,
                        canUseWhatsAppCommunications: !!item.canUseWhatsAppCommunications,
                        requiresEmployerVerificationToPublishJobs: !!item.requiresEmployerVerificationToPublishJobs,
                        isDefault: !!item.isDefault
                      });
                    }
                  ))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'documents' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Document catalog</p><h2>Upload document types</h2><span>Define employer and professional document options shown in onboarding screens.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Document type name<input className="input" value={documentType.name} onChange={(event) => setDocumentType({ ...documentType, name: event.target.value })} /></label>
                      <label className="field-label">Slug<input className="input" value={documentType.slug} onChange={(event) => setDocumentType({ ...documentType, slug: event.target.value })} /></label>
                      <label className="field-label">Target profile<select className="select" value={documentType.targetType} onChange={(event) => setDocumentType({ ...documentType, targetType: Number(event.target.value) })}><option value={0}>Professional</option><option value={1}>Employer</option></select></label>
                      <label className="field-label">Maximum upload size (MB)<input className="input" type="number" min="1" value={documentType.maxFileSizeMb} onChange={(event) => setDocumentType({ ...documentType, maxFileSizeMb: Number(event.target.value) })} /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Accepted file extensions
                        <FormControl fullWidth>
                          <InputLabel>Accepted file extensions</InputLabel>
                          <MuiSelect
                            multiple
                            value={selectedDocumentExtensions}
                            onChange={(event) => {
                              const next = typeof event.target.value === 'string' ? event.target.value.split(',') : event.target.value;
                              setDocumentType({ ...documentType, allowedExtensions: next.join(',') });
                            }}
                            input={<OutlinedInput label="Accepted file extensions" />}
                            renderValue={(selected) => (
                              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
                                {selected.map((value) => <Chip key={value} label={value} size="small" />)}
                              </Box>
                            )}
                          >
                            {commonFileExtensions.map((extension) => <MenuItem key={extension} value={extension}>{extension}</MenuItem>)}
                          </MuiSelect>
                        </FormControl>
                        <small>Select one or more allowed upload formats for this document type.</small>
                      </label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Selected extensions summary
                        <input className="input" value={documentType.allowedExtensions} onChange={(event) => setDocumentType({ ...documentType, allowedExtensions: event.target.value })} placeholder=".pdf,.doc,.docx,.jpg,.jpeg,.png" />
                      </label>
                      <label className="switch-card"><input type="checkbox" checked={documentType.isActive} onChange={(event) => setDocumentType({ ...documentType, isActive: event.target.checked })} /> Active and available for upload</label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Description<textarea className="textarea" value={documentType.description} onChange={(event) => setDocumentType({ ...documentType, description: event.target.value })} /></label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('documentType', adminApi.createDocumentType, adminApi.updateDocumentType, documentType, editing.type === 'documentType' ? 'Document type updated.' : 'Document type saved.', () => setDocumentType({ name: '', slug: '', targetType: 1, description: '', allowedExtensions: '.pdf,.doc,.docx,.jpg,.jpeg,.png', maxFileSizeMb: 10, isActive: true }))}>{editing.type === 'documentType' ? 'Update document type' : 'Add document type'}</button>
                    </div>
                  </div>
                  <div className="config-list">{configuration?.documentTypes?.map((item) => listCard(item.slug, item.name, `${targetTypeLabel(item.targetType)} · ${item.allowedExtensions || 'Any extension'} · ${item.maxFileSizeMb} MB${item.isActive ? '' : ' · Inactive'}`, () => { setEditing({ type: 'documentType', id: item.id }); setDocumentType({ name: item.name, slug: item.slug, targetType: item.targetType === 'Professional' ? 0 : item.targetType === 'Employer' ? 1 : item.targetType, description: item.description || '', allowedExtensions: item.allowedExtensions || '', maxFileSizeMb: item.maxFileSizeMb || 10, isActive: item.isActive }); }))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'rules' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Eligibility</p><h2>Required document rules</h2><span>Map mandatory or optional documents to profile types, categories, and facilities.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Target profile<select className="select" value={rule.targetType} onChange={(event) => setRule({ ...rule, targetType: Number(event.target.value) })}><option value={0}>Professional</option><option value={1}>Employer</option></select></label>
                      <label className="field-label">Applies to category or facility<select className="select" value={rule.appliesToCategoryOrFacilityType} onChange={(event) => setRule({ ...rule, appliesToCategoryOrFacilityType: event.target.value })}>
                        <option value="">All categories/facilities</option>
                        {configuration?.categories?.map((category) => <option key={category.slug} value={category.name}>{category.name}</option>)}
                        <option value="Hospital">Hospital</option><option value="Clinic">Clinic</option><option value="Pharmacy">Pharmacy</option><option value="Laboratory">Laboratory</option>
                      </select></label>
                      <label className="field-label">Document type<select className="select" value={rule.documentType} onChange={(event) => setRule({ ...rule, documentType: event.target.value })}>
                        <option value="">Select document type</option>
                        {configuration?.documentTypes?.filter((doc) => Number(doc.targetType) === Number(rule.targetType) || String(doc.targetType) === (Number(rule.targetType) === 0 ? 'Professional' : 'Employer')).map((doc) => <option key={doc.slug} value={doc.slug}>{doc.name}</option>)}
                      </select></label>
                      <label className="switch-card"><input type="checkbox" checked={rule.isMandatory} onChange={(event) => setRule({ ...rule, isMandatory: event.target.checked })} /> Mandatory</label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('rule', adminApi.createDocumentRule, adminApi.updateDocumentRule, rule, editing.type === 'rule' ? 'Document rule updated.' : 'Document rule saved.', () => setRule({ targetType: 0, appliesToCategoryOrFacilityType: '', documentType: '', isMandatory: true }))}>{editing.type === 'rule' ? 'Update rule' : 'Add rule'}</button>
                    </div>
                  </div>
                  <div className="config-list">{configuration?.requiredDocumentRules?.map((item) => {
                    const targetType = item.targetType === 'Professional' ? 0 : item.targetType === 'Employer' ? 1 : item.targetType;
                    const configuredDocumentType = configuration?.documentTypes?.find((doc) =>
                      (Number(doc.targetType) === Number(targetType) || String(doc.targetType) === (Number(targetType) === 0 ? 'Professional' : 'Employer')) &&
                      (doc.slug === item.documentType || doc.name === item.documentType));
                    return listCard(item.id, configuredDocumentType?.name || item.documentType, `${targetTypeLabel(item.targetType)} · ${item.appliesToCategoryOrFacilityType || 'All'} · ${item.isMandatory ? 'Mandatory' : 'Optional'}`, () => {
                      setEditing({ type: 'rule', id: item.id });
                      setRule({ targetType, appliesToCategoryOrFacilityType: item.appliesToCategoryOrFacilityType || '', documentType: configuredDocumentType?.slug || item.documentType || '', isMandatory: item.isMandatory });
                    });
                  })}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'verification' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Policy</p><h2>Verification policies</h2><span>Decide which stage triggers verification, what action is gated, and which integration route or fallback logic should run.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Policy name<input className="input" value={policy.name} onChange={(event) => setPolicy({ ...policy, name: event.target.value })} /></label>
                      <label className="field-label">Subject type<select className="select" value={policy.subjectType} onChange={(event) => { const subjectType = Number(event.target.value); const nextActions = verificationActionOptions[`${subjectType}:${policy.stage}`] || []; setPolicy({ ...policy, subjectType, actionKey: nextActions[0]?.value || '', fieldName: '', integrationConfigId: '' }); }}><option value={0}>Professional</option><option value={1}>Employer</option></select></label>
                      <label className="field-label">Stage<select className="select" value={policy.stage} onChange={(event) => { const stage = Number(event.target.value); const nextActions = verificationActionOptions[`${policy.subjectType}:${stage}`] || []; setPolicy({ ...policy, stage, actionKey: nextActions[0]?.value || '' }); }}>{verificationStageOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>
                      <label className="field-label">Action<select className="select" value={policy.actionKey} onChange={(event) => setPolicy({ ...policy, actionKey: event.target.value })}>{availablePolicyActions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>
                      <label className="field-label">Policy mode<select className="select" value={policy.policyMode} onChange={(event) => { const policyMode = Number(event.target.value); setPolicy({ ...policy, policyMode, integrationConfigId: '', documentType: policyMode === 2 ? policy.documentType : '', fieldName: policyMode === 3 ? policy.fieldName : '' }); }}>{verificationPolicyModeOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>
                      {Number(policy.policyMode) === 2 && <label className="field-label">Document type<select className="select" value={policy.documentType} onChange={(event) => setPolicy({ ...policy, documentType: event.target.value, integrationConfigId: '' })}><option value="">Any document type</option>{configuration?.documentTypes?.filter((item) => String(item.targetType) === String(policy.subjectType) || Number(item.targetType) === Number(policy.subjectType)).map((item) => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>}
                      {Number(policy.policyMode) === 3 && <label className="field-label">Field name<select className="select" value={policy.fieldName} onChange={(event) => setPolicy({ ...policy, fieldName: event.target.value, integrationConfigId: '' })}><option value="">Select field</option>{integrationFieldOptions.filter((item) => Number(policy.subjectType) === 0 ? item.value === 'licenseNumber' || item.value === 'professionalLicenseNumber' || item.value === 'nationalId' : item.value === 'kraPin' || item.value === 'businessRegistrationNumber' || item.value === 'licenseNumber').map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select></label>}
                      {(Number(policy.policyMode) === 2 || Number(policy.policyMode) === 3) && <label className="field-label">Bound integration<select className="select" value={policy.integrationConfigId} onChange={(event) => setPolicy({ ...policy, integrationConfigId: event.target.value })}><option value="">No bound integration</option>{availablePolicyIntegrations.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
                      <label className="switch-card"><input type="checkbox" checked={policy.requireVerifiedStatusForAction} onChange={(event) => setPolicy({ ...policy, requireVerifiedStatusForAction: event.target.checked })} /> Verified status required</label>
                      <label className="switch-card"><input type="checkbox" checked={policy.requireAllMandatoryDocuments} onChange={(event) => setPolicy({ ...policy, requireAllMandatoryDocuments: event.target.checked })} /> All mandatory documents required</label>
                      <label className="switch-card"><input type="checkbox" checked={policy.blockOnPending} onChange={(event) => setPolicy({ ...policy, blockOnPending: event.target.checked })} /> Block action while pending</label>
                      <label className="switch-card"><input type="checkbox" checked={policy.blockOnFailure} onChange={(event) => setPolicy({ ...policy, blockOnFailure: event.target.checked })} /> Block action on failure</label>
                      <label className="switch-card"><input type="checkbox" checked={policy.bypassWhenIntegrationMissing} onChange={(event) => setPolicy({ ...policy, bypassWhenIntegrationMissing: event.target.checked })} /> Allow when integration missing</label>
                      <label className="switch-card"><input type="checkbox" checked={policy.allowManualOverride} onChange={(event) => setPolicy({ ...policy, allowManualOverride: event.target.checked })} /> Allow manual override</label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Policy notes<textarea className="textarea" value={policy.notes} onChange={(event) => setPolicy({ ...policy, notes: event.target.value })} placeholder="Explain what should happen at this stage and how operations should handle exceptions." /></label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('policy', adminApi.createVerificationPolicy, adminApi.updateVerificationPolicy, { ...policy, integrationConfigId: policy.integrationConfigId || null, documentType: policy.documentType || null, fieldName: policy.fieldName || null, notes: policy.notes || null }, editing.type === 'policy' ? 'Verification policy updated.' : 'Verification policy saved.', () => setPolicy({ name: '', subjectType: 0, stage: 1, actionKey: 'UpdateProfessionalProfile', policyMode: 0, documentType: '', fieldName: '', integrationConfigId: '', requireVerifiedStatusForAction: true, requireAllMandatoryDocuments: true, blockOnPending: true, blockOnFailure: true, bypassWhenIntegrationMissing: true, allowManualOverride: true, notes: '' }))}>{editing.type === 'policy' ? 'Update policy' : 'Add policy'}</button>
                    </div>
                  </div>
                  <div className="config-list config-list-wide">{configuration?.verificationPolicies?.map((item) => listCard(item.id, item.name, `${targetTypeLabel(item.subjectType)} · ${stageLabel(item.stage)} · ${policyModeLabel(item.policyMode)} · ${(verificationActionOptions[`${item.subjectType === 'Professional' ? 0 : item.subjectType === 'Employer' ? 1 : Number(item.subjectType)}:${typeof item.stage === 'number' ? item.stage : verificationStageOptions.find((option) => option.label === item.stage)?.value ?? 0}`] || []).find((option) => option.value === item.actionKey)?.label || item.actionKey}${item.integrationConfigId ? ' · Integration bound' : ''}`, () => { const subjectType = item.subjectType === 'Professional' ? 0 : item.subjectType === 'Employer' ? 1 : Number(item.subjectType); const stage = typeof item.stage === 'number' ? item.stage : verificationStageOptions.find((option) => option.label === item.stage)?.value ?? 1; const policyMode = typeof item.policyMode === 'number' ? item.policyMode : verificationPolicyModeOptions.find((option) => option.label === item.policyMode)?.value ?? 0; setEditing({ type: 'policy', id: item.id }); setPolicy({ name: item.name, subjectType, stage, actionKey: item.actionKey || (verificationActionOptions[`${subjectType}:${stage}`] || [])[0]?.value || '', policyMode, documentType: item.documentType || '', fieldName: item.fieldName || '', integrationConfigId: item.integrationConfigId || '', requireVerifiedStatusForAction: item.requireVerifiedStatusForAction, requireAllMandatoryDocuments: item.requireAllMandatoryDocuments, blockOnPending: item.blockOnPending !== false, blockOnFailure: item.blockOnFailure !== false, bypassWhenIntegrationMissing: item.bypassWhenIntegrationMissing !== false, allowManualOverride: item.allowManualOverride !== false, notes: item.notes || '' }); }))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'declarations' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Client gates</p><h2>Onboarding and job declarations</h2><span>Configure optional checkbox declarations shown during onboarding and job posting. If no active declarations exist for a flow, clients continue normally.</span></div></div>
                <div className="split-editor">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Flow<select className="select" value={declaration.flowKey} onChange={(event) => setDeclaration({ ...declaration, flowKey: event.target.value })}><option value="professional-onboarding">Professional onboarding</option><option value="employer-onboarding">Employer onboarding</option><option value="job-posting">Employer job posting</option></select></label>
                      <label className="field-label">Display order<input className="input" type="number" value={declaration.displayOrder} onChange={(event) => setDeclaration({ ...declaration, displayOrder: Number(event.target.value) })} /></label>
                      <label className="field-label">Declaration title<input className="input" value={declaration.title} onChange={(event) => setDeclaration({ ...declaration, title: event.target.value })} placeholder="I confirm the information provided is accurate" /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Declaration text<textarea className="textarea" value={declaration.body} onChange={(event) => setDeclaration({ ...declaration, body: event.target.value })} placeholder="Write the acknowledgement shown beside the checkbox." /></label>
                      <label className="switch-card"><input type="checkbox" checked={declaration.isRequired} onChange={(event) => setDeclaration({ ...declaration, isRequired: event.target.checked })} /> Required before continuing</label>
                      <label className="switch-card"><input type="checkbox" checked={declaration.isActive} onChange={(event) => setDeclaration({ ...declaration, isActive: event.target.checked })} /> Active</label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('declaration', adminApi.createDeclaration, adminApi.updateDeclaration, declaration, editing.type === 'declaration' ? 'Declaration updated.' : 'Declaration saved.', () => setDeclaration({ flowKey: 'job-posting', title: '', body: '', isRequired: true, isActive: true, displayOrder: 0 }))}>{editing.type === 'declaration' ? 'Update declaration' : 'Add declaration'}</button>
                    </div>
                  </div>
                  <div className="config-list config-list-wide">{declarations.map((item) => listCard(item.id, item.title, `${item.flowKey} - ${item.isRequired ? 'Required' : 'Optional'}${item.isActive ? '' : ' - Inactive'}`, () => { setEditing({ type: 'declaration', id: item.id }); setDeclaration({ flowKey: item.flowKey, title: item.title, body: item.body, isRequired: !!item.isRequired, isActive: !!item.isActive, displayOrder: item.displayOrder || 0 }); }))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'legal' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Legal content</p><h2>Privacy policy and terms</h2><span>Edit public legal pages shown on the client platform. Use plain HTML and optional CSS for controlled custom page content.</span></div></div>
                <div className="split-editor split-editor-wide">
                  <div>
                    <div className="form-grid spacious-form">
                      <label className="field-label">Page<select className="select" value={contentPage.slug} onChange={(event) => {
                        const existing = contentPages.find((page) => page.slug === event.target.value);
                        setContentPage(existing || { slug: event.target.value, title: event.target.value === 'privacy' ? 'Privacy Policy' : 'Terms and Conditions', htmlContent: '', cssContent: '', isPublished: true });
                      }}><option value="privacy">Privacy Policy</option><option value="terms">Terms and Conditions</option></select></label>
                      <label className="field-label">Title<input className="input" value={contentPage.title} onChange={(event) => setContentPage({ ...contentPage, title: event.target.value })} /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>HTML content<textarea className="textarea" style={{ minHeight: 220 }} value={contentPage.htmlContent} onChange={(event) => setContentPage({ ...contentPage, htmlContent: event.target.value })} placeholder="<h2>Your policy section</h2><p>Explain how platform data is handled.</p>" /></label>
                      <label className="field-label" style={{ gridColumn: '1 / -1' }}>Page CSS<textarea className="textarea" value={contentPage.cssContent} onChange={(event) => setContentPage({ ...contentPage, cssContent: event.target.value })} placeholder=".legal-page h2 { color: #8b004a; }" /></label>
                      <label className="switch-card"><input type="checkbox" checked={contentPage.isPublished} onChange={(event) => setContentPage({ ...contentPage, isPublished: event.target.checked })} /> Publish this page</label>
                    </div>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => submit(adminApi.saveContentPage, contentPage, 'Legal page saved.')}>Save legal page</button>
                    </div>
                  </div>
                  <div className="config-list">{contentPages.map((item) => listCard(item.slug, item.title, `${item.slug} - ${item.isPublished ? 'Published' : 'Draft'} - Updated ${new Date(item.updatedAt || item.createdAt).toLocaleDateString()}`, () => setContentPage({ slug: item.slug, title: item.title, htmlContent: item.htmlContent || '', cssContent: item.cssContent || '', isPublished: item.isPublished !== false })))}</div>
                </div>
              </>
            )}

            {activePlatformSection === 'integrations' && (
              <>
                <div className="settings-stage-header"><div><p className="eyebrow">Verification providers</p><h2>Document and field integrations</h2><span>Route documents or captured fields to external verification services, or keep them manual.</span></div></div>
                <div className="split-editor integration-editor">
                  <div>
                    <Stack spacing={2} className="integration-accordion-stack">
                      <Accordion expanded={integrationAccordion === 'target'} onChange={(_, expanded) => setIntegrationAccordion(expanded ? 'target' : false)} disableGutters>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box>
                            <Typography fontWeight={800}>1. Verification target</Typography>
                            <Typography variant="body2" color="text.secondary">Choose what is being verified and how it enters the provider.</Typography>
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box className="form-grid spacious-form">
                            <TextField label="Integration name" value={integration.name} onChange={(event) => setIntegration({ ...integration, name: event.target.value })} fullWidth />
                            <FormControl fullWidth>
                              <InputLabel>Verification subject</InputLabel>
                              <MuiSelect label="Verification subject" value={integration.subject} onChange={(event) => setIntegration({ ...integration, subject: event.target.value, documentType: '', fieldName: '', transportMode: 0 })}>
                                <MenuItem value="Document">Document upload</MenuItem>
                                <MenuItem value="EmployerField">Employer registration field</MenuItem>
                                <MenuItem value="ProfessionalField">Professional registration field</MenuItem>
                              </MuiSelect>
                            </FormControl>
                            {integrationSubjectIsDocument ? (
                              <FormControl fullWidth>
                                <InputLabel>Document type</InputLabel>
                                <MuiSelect label="Document type" value={integration.documentType} onChange={(event) => setIntegration({ ...integration, documentType: event.target.value })}>
                                  <MenuItem value="">Select document type</MenuItem>
                                  {configuration?.documentTypes?.map((doc) => <MenuItem key={doc.slug} value={doc.slug}>{doc.name}</MenuItem>)}
                                </MuiSelect>
                              </FormControl>
                            ) : (
                              <FormControl fullWidth>
                                <InputLabel>Field name</InputLabel>
                                <MuiSelect label="Field name" value={integration.fieldName} onChange={(event) => setIntegration({ ...integration, fieldName: event.target.value })}>
                                  <MenuItem value="">Select field value</MenuItem>
                                  {integrationFieldOptions.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                                </MuiSelect>
                              </FormControl>
                            )}
                            <FormControl fullWidth>
                              <InputLabel>Verification transport</InputLabel>
                              <MuiSelect label="Verification transport" value={integration.transportMode} onChange={(event) => setIntegration({ ...integration, transportMode: Number(event.target.value) })}>
                                {availableTransportOptions.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                              </MuiSelect>
                            </FormControl>
                          </Box>
                        </AccordionDetails>
                      </Accordion>

                      <Accordion expanded={integrationAccordion === 'connection'} onChange={(_, expanded) => setIntegrationAccordion(expanded ? 'connection' : false)} disableGutters>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box>
                            <Typography fontWeight={800}>2. Connection and authentication</Typography>
                            <Typography variant="body2" color="text.secondary">{integrationIsManual ? 'Manual verification skips provider calls.' : 'Configure the endpoint and authentication used for outbound verification calls.'}</Typography>
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          {integrationIsManual ? (
                            <Box className="review-card" sx={{ p: 2.5 }}>
                              <Typography fontWeight={800}>Manual verification route</Typography>
                              <Typography variant="body2" color="text.secondary">No external API request will be made for this integration.</Typography>
                            </Box>
                          ) : (
                            <Box className="form-grid spacious-form">
                              <TextField label="Endpoint URL" value={integration.endpointUrl} onChange={(event) => setIntegration({ ...integration, endpointUrl: event.target.value })} fullWidth />
                              <FormControl fullWidth>
                                <InputLabel>HTTP method</InputLabel>
                                <MuiSelect label="HTTP method" value={integration.httpMethod} onChange={(event) => setIntegration({ ...integration, httpMethod: event.target.value })}>
                                  <MenuItem value="POST">POST</MenuItem>
                                  <MenuItem value="PUT">PUT</MenuItem>
                                  <MenuItem value="PATCH">PATCH</MenuItem>
                                  <MenuItem value="GET">GET</MenuItem>
                                </MuiSelect>
                              </FormControl>
                              <FormControl fullWidth>
                                <InputLabel>Authentication</InputLabel>
                                <MuiSelect label="Authentication" value={integration.authenticationType} onChange={(event) => setIntegration({ ...integration, authenticationType: event.target.value })}>
                                  <MenuItem value="None">None</MenuItem>
                                  <MenuItem value="ApiKey">API key</MenuItem>
                                  <MenuItem value="BearerToken">Bearer token</MenuItem>
                                  <MenuItem value="Basic">Basic auth</MenuItem>
                                  <MenuItem value="CustomHeader">Custom header</MenuItem>
                                </MuiSelect>
                              </FormControl>
                              <TextField label="API key or secret" type="password" value={integration.apiKeySecret} onChange={(event) => setIntegration({ ...integration, apiKeySecret: event.target.value })} fullWidth />
                              <Box sx={{ gridColumn: '1 / -1' }}>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Request headers</Typography>
                                <Stack spacing={1.25}>
                                  {requestHeaders.map((item, index) => (
                                    <Box key={`header-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 1.5 }}>
                                      <TextField label="Header name" value={item.key} onChange={(event) => updateCollection(setRequestHeaders, requestHeaders, index, 'key', event.target.value)} />
                                      <TextField label="Header value" value={item.value} onChange={(event) => updateCollection(setRequestHeaders, requestHeaders, index, 'value', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setRequestHeaders(requestHeaders.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setRequestHeaders([...requestHeaders, blankPair()])}>Add header</button></div>
                                </Stack>
                              </Box>
                              <Box sx={{ gridColumn: '1 / -1' }}>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Query parameters</Typography>
                                <Stack spacing={1.25}>
                                  {queryParameters.map((item, index) => (
                                    <Box key={`query-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 1.5 }}>
                                      <TextField label="Parameter name" value={item.key} onChange={(event) => updateCollection(setQueryParameters, queryParameters, index, 'key', event.target.value)} />
                                      <TextField label="Parameter value" value={item.value} onChange={(event) => updateCollection(setQueryParameters, queryParameters, index, 'value', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setQueryParameters(queryParameters.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setQueryParameters([...queryParameters, blankPair()])}>Add query parameter</button></div>
                                </Stack>
                              </Box>
                            </Box>
                          )}
                        </AccordionDetails>
                      </Accordion>

                      {!integrationIsManual && (
                        <Accordion expanded={integrationAccordion === 'request'} onChange={(_, expanded) => setIntegrationAccordion(expanded ? 'request' : false)} disableGutters>
                          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                            <Box>
                              <Typography fontWeight={800}>3. Request mapping</Typography>
                              <Typography variant="body2" color="text.secondary">Map profile or document values into the outbound request body.</Typography>
                            </Box>
                          </AccordionSummary>
                          <AccordionDetails>
                            <Stack spacing={2}>
                              <TextField label="Request body template" multiline minRows={4} value={integration.requestBodyTemplate} onChange={(event) => setIntegration({ ...integration, requestBodyTemplate: event.target.value })} helperText={integrationSubjectIsDocument ? 'Use placeholders like {{documentType}}, {{fileName}}, {{base64}}.' : 'Use placeholders like {{fieldName}}, {{value}}, {{subjectType}}.'} />
                              <Box>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Field mapping rules</Typography>
                                <Stack spacing={1.25}>
                                  {requestMappings.map((item, index) => (
                                    <Box key={`request-map-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 1.5 }}>
                                      <TextField label="Source value" value={item.source} onChange={(event) => updateCollection(setRequestMappings, requestMappings, index, 'source', event.target.value)} />
                                      <TextField label="Target JSON path" value={item.target} onChange={(event) => updateCollection(setRequestMappings, requestMappings, index, 'target', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setRequestMappings(requestMappings.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setRequestMappings([...requestMappings, blankMapRule()])}>Add mapping</button></div>
                                </Stack>
                              </Box>
                            </Stack>
                          </AccordionDetails>
                        </Accordion>
                      )}

                      {!integrationIsManual && (
                        <Accordion expanded={integrationAccordion === 'response'} onChange={(_, expanded) => setIntegrationAccordion(expanded ? 'response' : false)} disableGutters>
                          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                            <Box>
                              <Typography fontWeight={800}>4. Response and success rules</Typography>
                              <Typography variant="body2" color="text.secondary">Define when a provider result counts as success or failure, then map useful fields back.</Typography>
                            </Box>
                          </AccordionSummary>
                          <AccordionDetails>
                            <Stack spacing={2}>
                              <Box>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Success conditions</Typography>
                                <Stack spacing={1.25}>
                                  {successConditions.map((item, index) => (
                                    <Box key={`success-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 1.5 }}>
                                      <FormControl fullWidth>
                                        <InputLabel>Target</InputLabel>
                                        <MuiSelect label="Target" value={item.target} onChange={(event) => updateCollection(setSuccessConditions, successConditions, index, 'target', event.target.value)}>
                                          {conditionTargets.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                                        </MuiSelect>
                                      </FormControl>
                                      <FormControl fullWidth>
                                        <InputLabel>Operator</InputLabel>
                                        <MuiSelect label="Operator" value={item.operator} onChange={(event) => updateCollection(setSuccessConditions, successConditions, index, 'operator', event.target.value)}>
                                          {conditionOperators.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                                        </MuiSelect>
                                      </FormControl>
                                      <TextField label="Expected value" value={item.expected} onChange={(event) => updateCollection(setSuccessConditions, successConditions, index, 'expected', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setSuccessConditions(successConditions.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setSuccessConditions([...successConditions, blankCondition()])}>Add success rule</button></div>
                                </Stack>
                              </Box>

                              <Box>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Failure conditions</Typography>
                                <Stack spacing={1.25}>
                                  {failureConditions.map((item, index) => (
                                    <Box key={`failure-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 1.5 }}>
                                      <FormControl fullWidth>
                                        <InputLabel>Target</InputLabel>
                                        <MuiSelect label="Target" value={item.target} onChange={(event) => updateCollection(setFailureConditions, failureConditions, index, 'target', event.target.value)}>
                                          {conditionTargets.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                                        </MuiSelect>
                                      </FormControl>
                                      <FormControl fullWidth>
                                        <InputLabel>Operator</InputLabel>
                                        <MuiSelect label="Operator" value={item.operator} onChange={(event) => updateCollection(setFailureConditions, failureConditions, index, 'operator', event.target.value)}>
                                          {conditionOperators.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
                                        </MuiSelect>
                                      </FormControl>
                                      <TextField label="Expected value" value={item.expected} onChange={(event) => updateCollection(setFailureConditions, failureConditions, index, 'expected', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setFailureConditions(failureConditions.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setFailureConditions([...failureConditions, blankCondition()])}>Add failure rule</button></div>
                                </Stack>
                              </Box>

                              <Box>
                                <Typography fontWeight={700} sx={{ mb: 1 }}>Response mappings</Typography>
                                <Stack spacing={1.25}>
                                  {responseMappings.map((item, index) => (
                                    <Box key={`response-map-${index}`} sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 1.5 }}>
                                      <TextField label="Response path" value={item.source} onChange={(event) => updateCollection(setResponseMappings, responseMappings, index, 'source', event.target.value)} />
                                      <TextField label="Store as" value={item.target} onChange={(event) => updateCollection(setResponseMappings, responseMappings, index, 'target', event.target.value)} />
                                      <button className="btn-secondary" type="button" onClick={() => setResponseMappings(responseMappings.filter((_, currentIndex) => currentIndex !== index))}>Remove</button>
                                    </Box>
                                  ))}
                                  <div className="button-row"><button className="btn-secondary" type="button" onClick={() => setResponseMappings([...responseMappings, blankMapRule()])}>Add response map</button></div>
                                </Stack>
                              </Box>
                            </Stack>
                          </AccordionDetails>
                        </Accordion>
                      )}

                      <Accordion expanded={integrationAccordion === 'reliability'} onChange={(_, expanded) => setIntegrationAccordion(expanded ? 'reliability' : false)} disableGutters>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Box>
                            <Typography fontWeight={800}>5. Reliability and overrides</Typography>
                            <Typography variant="body2" color="text.secondary">Set timeouts, retries, and whether admins can bypass the provider when needed.</Typography>
                          </Box>
                        </AccordionSummary>
                        <AccordionDetails>
                          <Box className="form-grid spacious-form">
                            {!integrationIsManual && (
                              <>
                                <TextField label="Timeout seconds" type="number" inputProps={{ min: 1 }} value={integration.timeoutSeconds} onChange={(event) => setIntegration({ ...integration, timeoutSeconds: Number(event.target.value) })} />
                                <TextField label="Retry count" type="number" inputProps={{ min: 0 }} value={integration.retryCount} onChange={(event) => setIntegration({ ...integration, retryCount: Number(event.target.value) })} />
                                <TextField label="Retry delay seconds" type="number" inputProps={{ min: 0 }} value={integration.retryDelaySeconds} onChange={(event) => setIntegration({ ...integration, retryDelaySeconds: Number(event.target.value) })} />
                                <label className="switch-card"><input type="checkbox" checked={integration.retryOnTimeout} onChange={(event) => setIntegration({ ...integration, retryOnTimeout: event.target.checked })} /> Retry on timeout</label>
                                <label className="switch-card"><input type="checkbox" checked={integration.retryOn5xx} onChange={(event) => setIntegration({ ...integration, retryOn5xx: event.target.checked })} /> Retry on 5xx response</label>
                                <label className="switch-card"><input type="checkbox" checked={integration.parseJsonResponse} onChange={(event) => setIntegration({ ...integration, parseJsonResponse: event.target.checked })} /> Parse JSON response</label>
                                <label className="switch-card"><input type="checkbox" checked={integration.storeRawRequestResponse} onChange={(event) => setIntegration({ ...integration, storeRawRequestResponse: event.target.checked })} /> Store raw request/response</label>
                              </>
                            )}
                            <label className="switch-card"><input type="checkbox" checked={integration.isEnabled} onChange={(event) => setIntegration({ ...integration, isEnabled: event.target.checked })} /> Enabled</label>
                            <label className="switch-card"><input type="checkbox" checked={integration.allowManualOverride} onChange={(event) => setIntegration({ ...integration, allowManualOverride: event.target.checked })} /> Allow manual override</label>
                          </Box>
                        </AccordionDetails>
                      </Accordion>
                    </Stack>
                    <div className="button-row" style={{ marginTop: 16 }}>
                      <button className="btn-primary" onClick={() => saveConfig('integration', adminApi.createVerificationIntegration, adminApi.updateVerificationIntegration, integrationPayload, editing.type === 'integration' ? 'Verification integration updated.' : 'Verification integration saved.', resetIntegrationForm)}>{editing.type === 'integration' ? 'Update integration' : 'Save integration'}</button>
                    </div>
                  </div>
                  <div className="config-list config-list-wide">{configuration?.verificationIntegrations?.map((item) => listCard(item.id, item.name, `${subjectLabel(item.subject)} · ${item.documentType || item.fieldName || 'No target selected'} · ${transportLabel(item.transportMode)}${item.isEnabled ? ' · Enabled' : ' · Disabled'}`, () => { setEditing({ type: 'integration', id: item.id }); setIntegration({ name: item.name, subject: item.subject, documentType: item.documentType || '', fieldName: item.fieldName || '', transportMode: typeof item.transportMode === 'number' ? item.transportMode : 0, endpointUrl: item.endpointUrl || '', httpMethod: item.httpMethod || 'POST', apiKeySecret: '', authenticationType: item.authenticationType || 'None', requestHeadersJson: item.requestHeadersJson || '', queryParametersJson: item.queryParametersJson || '', requestBodyTemplate: item.requestBodyTemplate || '', requestFieldMapJson: item.requestFieldMapJson || '', successConditionsJson: item.successConditionsJson || '', failureConditionsJson: item.failureConditionsJson || '', responseMapJson: item.responseMapJson || '', timeoutSeconds: item.timeoutSeconds || 30, retryCount: item.retryCount || 0, retryDelaySeconds: item.retryDelaySeconds || 0, retryOnTimeout: !!item.retryOnTimeout, retryOn5xx: item.retryOn5xx !== false, parseJsonResponse: item.parseJsonResponse !== false, storeRawRequestResponse: item.storeRawRequestResponse !== false, isEnabled: item.isEnabled, allowManualOverride: item.allowManualOverride }); setRequestHeaders(parseEntries(item.requestHeadersJson, [blankPair()])); setQueryParameters(parseEntries(item.queryParametersJson, [blankPair()])); setRequestMappings(parseEntries(item.requestFieldMapJson, [blankMapRule()])); setSuccessConditions(parseEntries(item.successConditionsJson, [blankCondition()])); setFailureConditions(parseEntries(item.failureConditionsJson, [blankCondition()])); setResponseMappings(parseEntries(item.responseMapJson, [blankMapRule()])); setIntegrationAccordion('target'); }))}</div>
                </div>
              </>
            )}
          </section>
        </div>
      )}
      <style jsx global>{`
        .content-editor-grid {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 16px;
        }

        .content-editor-card {
          display: grid;
          gap: 12px;
          min-height: 220px;
          border: 1px solid rgba(148, 163, 184, 0.3);
          border-radius: 28px;
          background: linear-gradient(145deg, rgba(255,255,255,0.96), rgba(248,250,252,0.9));
          padding: 22px;
          color: #07122b;
          text-decoration: none;
          box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
          transition: transform .2s ease, border-color .2s ease;
        }

        .content-editor-card:hover {
          transform: translateY(-3px);
          border-color: var(--accent);
        }

        .content-editor-icon {
          display: grid;
          width: 56px;
          height: 56px;
          place-items: center;
          border-radius: 20px;
          background: color-mix(in srgb, var(--accent) 16%, #fff);
          color: var(--accent);
          font-size: 24px;
          font-weight: 950;
        }

        .content-editor-card strong {
          font-size: 22px;
          font-weight: 950;
          letter-spacing: -0.04em;
        }

        .content-editor-card small {
          color: #64748b;
          font-size: 14px;
          line-height: 1.7;
        }

        @media (max-width: 980px) {
          .content-editor-grid {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </AdminShell>
  );
}
