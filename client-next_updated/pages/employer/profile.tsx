import { useEffect, useMemo, useState } from 'react';
import PhoneInput from '../../components/PhoneInput';
import Layout from '../../components/Layout';
import { employerApi } from '../../lib/employerApi';
import { verificationApi } from '../../lib/verificationApi';
import { useAuth, useRequireAuth } from '../../lib/useAuth';
import type { EmployerDocument, EmployerProfile, RequiredDocumentRuleSummary } from '../../types';

type ConfiguredDocumentType = {
  name: string;
  slug: string;
  targetType: string;
  description?: string;
  allowedExtensions?: string;
  maxFileSizeMb?: number;
};

const facilityTypes = ['Hospital', 'Clinic', 'Pharmacy', 'Laboratory', 'Home Care', 'Diagnostic Centre', 'Other'];
const defaultTeamPermissions = {
  canManageProfile: false, canManageSettings: false, canCreateJobs: false, canPublishJobs: false,
  canViewApplications: false, canVerifyApplications: false, canInviteProfessionals: false,
  canMessageProfessionals: false, canManageTeam: false,
};
const teamRolePresets: Record<string, Partial<typeof defaultTeamPermissions>> = {
  'Hiring manager': { canCreateJobs: true, canPublishJobs: true, canViewApplications: true, canVerifyApplications: true, canInviteProfessionals: true, canMessageProfessionals: true },
  Recruiter: { canCreateJobs: true, canViewApplications: true, canInviteProfessionals: true, canMessageProfessionals: true },
  'Verification officer': { canViewApplications: true, canVerifyApplications: true },
  'Profile administrator': { canManageProfile: true, canManageSettings: true },
  'Team administrator': { canManageTeam: true, canManageProfile: true },
};
const fieldLabelClass = 'mb-1.5 block text-sm font-semibold text-slate-700';
const parseAllowedExtensions = (value?: string) =>
  (value || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);

export default function EmployerProfilePage() {
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'profile' | 'documents' | 'team'>('profile');
  const [profile, setProfile] = useState<EmployerProfile | null>(null);
  const [team, setTeam] = useState<any[]>([]);
  const [documents, setDocuments] = useState<EmployerDocument[]>([]);
  const [documentTypes, setDocumentTypes] = useState<ConfiguredDocumentType[]>([]);
  const [requiredDocuments, setRequiredDocuments] = useState<RequiredDocumentRuleSummary[]>([]);
  const [form, setForm] = useState({
    name: '',
    facilityType: '',
    contactPhone: '',
    isContactPhonePublic: false,
    address: '',
    businessRegistrationNumber: '',
    kraPin: '',
    licenseNumber: '',
  });
  const [documentType, setDocumentType] = useState('');
  const [documentFile, setDocumentFile] = useState<File | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [contactRegion, setContactRegion] = useState('');
  const [teamForm, setTeamForm] = useState({
    email: '',
    roleName: 'Hiring manager',
    rolePreset: 'Hiring manager',
    customRoleName: '',
    createAccountIfMissing: true,
    firstName: '',
    lastName: '',
    phoneNumber: '',
    temporaryPassword: '',
    canManageProfile: false,
    canManageSettings: false,
    canCreateJobs: true,
    canPublishJobs: false,
    canViewApplications: true,
    canVerifyApplications: false,
    canInviteProfessionals: true,
    canMessageProfessionals: true,
    canManageTeam: false,
    isActive: true,
  });
  const [editingTeamMemberId, setEditingTeamMemberId] = useState<string | null>(null);

  const selectedDocumentType = useMemo(
    () => documentTypes.find((item) => item.slug === documentType || item.name === documentType) || null,
    [documentType, documentTypes],
  );

  const requiredDocumentStatus = useMemo(() => requiredDocuments.map((rule) => ({
    ...rule,
    uploaded: documents.some((document) => document.documentType === rule.documentType),
  })), [requiredDocuments, documents]);

  const load = async () => {
    if (!user) return;
    const availableDocumentTypes = await verificationApi.getDocumentTypes('Employer').catch(() => []);
    setDocumentTypes(availableDocumentTypes);
    setDocumentType((current) => current || availableDocumentTypes[0]?.slug || '');

    try {
      const existing = await employerApi.getCurrent();
      setProfile(existing);
      setForm({
        name: existing.name || '',
        facilityType: existing.facilityType || '',
        contactPhone: existing.contactPhone || '',
        isContactPhonePublic: existing.isContactPhonePublic ?? false,
        address: existing.address || '',
        businessRegistrationNumber: existing.businessRegistrationNumber || '',
        kraPin: existing.kraPin || '',
        licenseNumber: existing.licenseNumber || '',
      });
      setContactRegion('');
      const [existingDocuments, rules] = await Promise.all([
        employerApi.getDocuments(existing.id),
        verificationApi.getRequiredDocuments('Employer', { facilityType: existing.facilityType }).catch(() => []),
      ]);
      setDocuments(existingDocuments);
      setRequiredDocuments(rules);
      setTeam(await employerApi.getTeam(existing.id).catch(() => []));
    } catch {
      setProfile(null);
      setDocuments([]);
      const rules = await verificationApi.getRequiredDocuments('Employer', { facilityType: form.facilityType }).catch(() => []);
      setRequiredDocuments(rules);
    }
  };

  const saveTeamMember = async () => {
    if (!profile) return;
    setBusy(true);
    setMessage(null);
    setError(null);
    try {
      if (editingTeamMemberId) {
        await employerApi.updateTeamMember(profile.id, editingTeamMemberId, teamForm);
      } else {
        await employerApi.addTeamMember(profile.id, { ...teamForm, roleName: teamForm.rolePreset === 'Custom' ? teamForm.customRoleName : teamForm.rolePreset });
      }
      setTeam(await employerApi.getTeam(profile.id));
      setTeamForm((current) => ({ ...current, email: '', firstName: '', lastName: '', phoneNumber: '', temporaryPassword: '' }));
      setEditingTeamMemberId(null);
      setMessage('Team member access saved.');
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to save team member.');
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    if (hydrated && user?.userType === 'Employer') {
      load().catch(() => undefined);
    }
  }, [hydrated, user]);

  const handleFacilityTypeChange = async (facilityType: string) => {
    setForm((current) => ({ ...current, facilityType }));
    const rules = await verificationApi.getRequiredDocuments('Employer', { facilityType }).catch(() => []);
    setRequiredDocuments(rules);
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!user) return;
    setBusy(true);
    setMessage(null);
    setError(null);
    try {
      const saved = profile
        ? await employerApi.update(profile.id, form)
        : await employerApi.register({ ...form, contactEmail: user.email });
      setProfile(saved);
      setMessage('Facility profile saved.');
      setDocuments(await employerApi.getDocuments(saved.id).catch(() => []));
      setRequiredDocuments(await verificationApi.getRequiredDocuments('Employer', { facilityType: saved.facilityType }).catch(() => []));
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to save facility profile.');
    } finally {
      setBusy(false);
    }
  };

  const upload = async () => {
    if (!profile || !documentFile || !documentType) return;
    setBusy(true);
    setMessage(null);
    setError(null);
    try {
      await employerApi.uploadDocument(profile.id, documentType, documentFile);
      setDocuments(await employerApi.getDocuments(profile.id));
      setDocumentFile(null);
      setMessage('Document uploaded for verification.');
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to upload document.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-6">
        <div className="client-stepper">
          {[['profile', 'Facility profile'], ['documents', 'Documents'], ['team', 'Team access']].map(([key, label]) => (
            <button key={key} type="button" className={activeTab === key ? 'active' : ''} onClick={() => setActiveTab(key as typeof activeTab)}>
              {label}
            </button>
          ))}
        </div>

        {activeTab === 'profile' && (
          <form className="surface-card p-6" onSubmit={save}>
            <h1 className="section-title">Facility profile</h1>
            <p className="section-copy mt-2">Keep organization details, tax references, licence information, and verification-ready contacts together.</p>
            {message && <div className="mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{message}</div>}
            {error && <div className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}

            <details className="mt-5 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Organisation details</summary>
              <div className="profile-form-grid mt-4">
                <label><span className={fieldLabelClass}>Facility name</span><input className="input-shell" value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="Facility or organization name" /></label>
                <label><span className={fieldLabelClass}>Facility type</span><select className="input-shell" value={form.facilityType} onChange={(event) => handleFacilityTypeChange(event.target.value)}><option value="">Select facility type</option>{facilityTypes.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
                <label><span className={fieldLabelClass}>Contact email</span><input className="input-shell" value={user?.email || profile?.contactEmail || ''} disabled /></label>
                <div><PhoneInput label="Contact phone" countryValue={contactRegion} phoneValue={form.contactPhone} onCountryChange={setContactRegion} onPhoneChange={(value) => setForm((current) => ({ ...current, contactPhone: value }))} /></div>
                <label className="flex min-h-12 items-center gap-3 rounded-[13px] border border-[var(--client-border)] bg-[var(--client-panel)] px-4 py-3 md:col-span-2">
                  <input type="checkbox" checked={form.isContactPhonePublic} onChange={(event) => setForm((current) => ({ ...current, isContactPhonePublic: event.target.checked }))} />
                  <span>
                    <strong className="block text-sm text-[var(--client-text)]">Display the business phone number publicly</strong>
                    <small className="mt-1 block text-[var(--client-muted)]">Off by default. When disabled, the number remains available only for account administration and authorized workflows.</small>
                  </span>
                </label>
              </div>
            </details>

            <details className="mt-4 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Business identifiers</summary>
              <div className="profile-form-grid mt-4">
                <label><span className={fieldLabelClass}>Business registration number</span><input className="input-shell" value={form.businessRegistrationNumber} onChange={(event) => setForm((current) => ({ ...current, businessRegistrationNumber: event.target.value }))} placeholder="Business registration number" /></label>
                <label><span className={fieldLabelClass}>Tax certificate or tax ID</span><input className="input-shell" value={form.kraPin} onChange={(event) => setForm((current) => ({ ...current, kraPin: event.target.value }))} placeholder="Tax certificate or tax identifier" /></label>
                <label><span className={fieldLabelClass}>Facility licence number</span><input className="input-shell" value={form.licenseNumber} onChange={(event) => setForm((current) => ({ ...current, licenseNumber: event.target.value }))} placeholder="Facility licence number" /></label>
              </div>
            </details>

            <details className="mt-4 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Location and verification readiness</summary>
              <label className="mt-4 block">
                <span className={fieldLabelClass}>Facility address</span>
                <textarea className="text-shell" value={form.address} onChange={(event) => setForm((current) => ({ ...current, address: event.target.value }))} placeholder="Street, building, city, region, and postal details" />
              </label>
              <div className="mt-4 rounded-3xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                <p className="font-semibold text-slate-900">Required profile documents</p>
                <div className="mt-3 grid gap-3 md:grid-cols-2">
                  {requiredDocumentStatus.length === 0 && <p className="text-slate-500">No required facility documents are currently listed for this facility type.</p>}
                  {requiredDocumentStatus.map((rule) => (
                    <div key={rule.id} className="rounded-2xl border border-slate-200 bg-white px-4 py-3">
                      <div className="flex items-center justify-between gap-3">
                        <span className="font-semibold text-slate-900">{rule.documentType}</span>
                        <span className={`pill-chip ${rule.uploaded ? 'bg-emerald-100 text-emerald-700' : ''}`}>{rule.uploaded ? 'Uploaded' : rule.isMandatory ? 'Required' : 'Optional'}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </details>

            <button className="primary-action mt-5" disabled={busy}>{busy ? 'Saving...' : 'Save facility profile'}</button>
          </form>
        )}

        {activeTab === 'documents' && (
          <div className="surface-card p-6">
            <h2 className="section-title">Verification documents</h2>
            <p className="section-copy mt-2">Upload the records required for your facility type so admin verification can proceed smoothly.</p>

            <div className="mt-5 grid gap-4 md:grid-cols-[0.95fr_1fr_auto]">
              <label>
                <span className={fieldLabelClass}>Document type</span>
                <select className="input-shell" value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
                  <option value="">Select document type</option>
                  {documentTypes.map((item) => <option key={item.slug} value={item.slug}>{item.name}</option>)}
                </select>
              </label>
              <label>
                <span className={fieldLabelClass}>Upload file</span>
                <div className="rounded-3xl border border-dashed border-slate-300 bg-slate-50 px-4 py-4 transition hover:border-slate-400 hover:bg-white">
                  <input className="hidden" id="employer-document-upload" type="file" onChange={(event) => setDocumentFile(event.target.files?.[0] || null)} />
                  <label htmlFor="employer-document-upload" className="block cursor-pointer">
                    <span className="block text-sm font-semibold text-slate-900">{documentFile ? documentFile.name : 'Choose or drop a document here'}</span>
                    <span className="mt-1 block text-xs text-slate-500">
                      {documentFile
                        ? `${Math.max(documentFile.size / (1024 * 1024), 0.01).toFixed(2)} MB selected`
                        : 'PDF, Word, spreadsheet, or image files are supported where allowed.'}
                    </span>
                  </label>
                </div>
              </label>
              <div className="flex items-end">
                <button className="primary-action h-12" type="button" disabled={!documentFile || !profile || !documentType || busy} onClick={upload}>
                  {busy ? 'Uploading...' : 'Upload'}
                </button>
              </div>
            </div>

            {selectedDocumentType && (
              <div className="mt-4 rounded-3xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                <div className="flex flex-wrap gap-2">
                  <span className="pill-chip">{selectedDocumentType.name}</span>
                  {parseAllowedExtensions(selectedDocumentType.allowedExtensions).length > 0 && (
                    <span className="pill-chip">Extensions: {parseAllowedExtensions(selectedDocumentType.allowedExtensions).join(', ')}</span>
                  )}
                  {selectedDocumentType.maxFileSizeMb && selectedDocumentType.maxFileSizeMb > 0 && (
                    <span className="pill-chip">Max size: {selectedDocumentType.maxFileSizeMb} MB</span>
                  )}
                </div>
                {selectedDocumentType.description && <p className="mt-3">{selectedDocumentType.description}</p>}
              </div>
            )}

            <div className="mt-4 grid gap-3">
              {documents.map((document) => (
                <div key={document.id} className="rounded-2xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <strong className="text-slate-900">{document.documentType}</strong>
                    <span className="pill-chip">{document.status}</span>
                  </div>
                  <p className="mt-2">{document.fileName}</p>
                </div>
              ))}
            </div>
          </div>
        )}

        {activeTab === 'team' && (
          <div className="surface-card p-6">
            <h2 className="section-title">Employer team access</h2>
            <p className="section-copy mt-2">Link an existing account or create a new recruiter account, assign a role preset, and fine-tune its permissions.</p>
            {message && <div className="mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{message}</div>}
            {error && <div className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}

            <div className="mt-5 rounded-3xl border border-slate-200 bg-white px-4 py-4">
              <div className="profile-form-grid">
                <label><span className={fieldLabelClass}>User email</span><input className="input-shell" value={teamForm.email} onChange={(event) => setTeamForm((current) => ({ ...current, email: event.target.value }))} placeholder="user@example.com" /></label>
                <label><span className={fieldLabelClass}>Role preset</span><select className="input-shell" value={teamForm.rolePreset} onChange={(event) => {
                  const rolePreset = event.target.value;
                  setTeamForm((current) => ({ ...current, rolePreset, roleName: rolePreset === 'Custom' ? current.customRoleName : rolePreset, ...defaultTeamPermissions, ...(teamRolePresets[rolePreset] || {}) }));
                }}>{Object.keys(teamRolePresets).map((role) => <option key={role}>{role}</option>)}<option>Custom</option></select></label>
                {teamForm.rolePreset === 'Custom' && <label><span className={fieldLabelClass}>Custom role name</span><input className="input-shell" value={teamForm.customRoleName} onChange={(event) => setTeamForm((current) => ({ ...current, customRoleName: event.target.value, roleName: event.target.value }))} /></label>}
              </div>
              {!editingTeamMemberId && <div className="mt-4">
                <label className="switch-card"><input type="checkbox" checked={teamForm.createAccountIfMissing} onChange={(event) => setTeamForm((current) => ({ ...current, createAccountIfMissing: event.target.checked }))} /> Create a platform account if this email is new</label>
                {teamForm.createAccountIfMissing && <div className="profile-form-grid mt-4">
                  <label><span className={fieldLabelClass}>First name</span><input className="input-shell" value={teamForm.firstName} onChange={(event) => setTeamForm((current) => ({ ...current, firstName: event.target.value }))} /></label>
                  <label><span className={fieldLabelClass}>Last name</span><input className="input-shell" value={teamForm.lastName} onChange={(event) => setTeamForm((current) => ({ ...current, lastName: event.target.value }))} /></label>
                  <label><span className={fieldLabelClass}>Phone number</span><input className="input-shell" value={teamForm.phoneNumber} onChange={(event) => setTeamForm((current) => ({ ...current, phoneNumber: event.target.value }))} /></label>
                  <label><span className={fieldLabelClass}>Temporary password</span><input className="input-shell" type="password" value={teamForm.temporaryPassword} onChange={(event) => setTeamForm((current) => ({ ...current, temporaryPassword: event.target.value }))} /><small>The user must change this password after signing in.</small></label>
                </div>}
              </div>}
              <div className="mt-4 grid gap-3 md:grid-cols-3">
                {[
                  ['canManageProfile', 'Edit facility profile'],
                  ['canManageSettings', 'Edit settings'],
                  ['canCreateJobs', 'Create jobs'],
                  ['canPublishJobs', 'Publish jobs'],
                  ['canViewApplications', 'View applications'],
                  ['canVerifyApplications', 'Verify application documents'],
                  ['canInviteProfessionals', 'Invite professionals'],
                  ['canMessageProfessionals', 'Message professionals'],
                  ['canManageTeam', 'Manage team'],
                ].map(([key, label]) => (
                  <label key={key} className="switch-card"><input type="checkbox" checked={(teamForm as any)[key]} onChange={(event) => setTeamForm((current) => ({ ...current, [key]: event.target.checked }))} /> {label}</label>
                ))}
              </div>
              <button className="primary-action mt-4" disabled={busy || !profile} onClick={saveTeamMember}>{busy ? 'Saving...' : editingTeamMemberId ? 'Update access' : 'Add access'}</button>
            </div>

            <div className="mt-5 grid gap-3">
              {team.map((member) => (
                <button key={member.id} type="button" className="rounded-2xl border border-slate-200 bg-white px-4 py-4 text-left" onClick={() => {
                  if (member.isOwner) return;
                  setEditingTeamMemberId(member.id);
                  setTeamForm({
                    email: member.email,
                    roleName: member.roleName,
                    rolePreset: teamRolePresets[member.roleName] ? member.roleName : 'Custom',
                    customRoleName: teamRolePresets[member.roleName] ? '' : member.roleName,
                    createAccountIfMissing: false,
                    firstName: member.firstName || '',
                    lastName: member.lastName || '',
                    phoneNumber: '',
                    temporaryPassword: '',
                    canManageProfile: member.canManageProfile,
                    canManageSettings: member.canManageSettings,
                    canCreateJobs: member.canCreateJobs,
                    canPublishJobs: member.canPublishJobs,
                    canViewApplications: member.canViewApplications,
                    canVerifyApplications: member.canVerifyApplications,
                    canInviteProfessionals: member.canInviteProfessionals,
                    canMessageProfessionals: member.canMessageProfessionals,
                    canManageTeam: member.canManageTeam,
                    isActive: member.isActive,
                  });
                }}>
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div><strong>{member.firstName} {member.lastName}</strong><p className="text-sm text-slate-500">{member.email} · {member.roleName}</p></div>
                    <span className="pill-chip">{member.isOwner ? 'Owner' : member.isActive ? 'Active' : 'Disabled'}</span>
                  </div>
                  <div className="mt-3 flex flex-wrap gap-2 text-xs font-bold text-slate-500">
                    {member.canCreateJobs && <span className="pill-chip">Create jobs</span>}
                    {member.canPublishJobs && <span className="pill-chip">Publish jobs</span>}
                    {member.canViewApplications && <span className="pill-chip">Applications</span>}
                    {member.canVerifyApplications && <span className="pill-chip">Verify docs</span>}
                    {member.canInviteProfessionals && <span className="pill-chip">Invites</span>}
                    {member.canManageTeam && <span className="pill-chip">Team admin</span>}
                  </div>
                </button>
              ))}
              {team.length === 0 && <p className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">No team members have been added yet.</p>}
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}
