import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import Layout from '../../../components/Layout';
import { employerApi } from '../../../lib/employerApi';
import { jobApi } from '../../../lib/jobApi';
import { professionalApi } from '../../../lib/professionalApi';
import { verificationApi } from '../../../lib/verificationApi';
import { declarationApi, type DeclarationConfig } from '../../../lib/declarationApi';
import { useAuth, useRequireAuth } from '../../../lib/useAuth';
import DeclarationChecklist, { requiredDeclarationsAccepted } from '../../../components/DeclarationChecklist';
import type { EmployerProfile, ProfessionalCategory } from '../../../types';

const departmentOptions = [
  'Emergency and urgent care',
  'Outpatient services',
  'Inpatient services',
  'Pharmacy',
  'Nursing',
  'Laboratory',
  'Radiology and imaging',
  'Dental',
  'Physiotherapy and rehabilitation',
  'Theatre and surgical services',
  'Maternity and neonatal care',
  'Community health',
  'Home-based care',
  'Administration',
  'Other',
];

const experienceOptions = [
  { label: 'No minimum experience', value: '0' },
  { label: 'At least 1 year', value: '1' },
  { label: 'At least 2 years', value: '2' },
  { label: 'At least 3 years', value: '3' },
  { label: 'At least 5 years', value: '5' },
  { label: 'At least 10 years', value: '10' },
  { label: 'Custom minimum', value: 'custom' },
];

function toClosingIso(dateValue: string) {
  if (!dateValue) return '';
  return new Date(`${dateValue}T23:59:59`).toISOString();
}

function dedupeFiles(files: File[]) {
  const seen = new Set<string>();
  return files.filter((file) => {
    const key = `${file.name}-${file.size}-${file.lastModified}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

export default function NewEmployerJobPage() {
  const router = useRouter();
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [profile, setProfile] = useState<EmployerProfile | null>(null);
  const [categories, setCategories] = useState<ProfessionalCategory[]>([]);
  const [documentTypes, setDocumentTypes] = useState<Array<{ name: string; slug: string }>>([]);
  const [engagementTypes, setEngagementTypes] = useState<Array<{ name: string; slug: string; allowsShiftPattern: boolean }>>([]);
  const [step, setStep] = useState<'role' | 'requirements' | 'documents' | 'review'>('role');
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [requiredDocuments, setRequiredDocuments] = useState<Array<{ documentType: string; isMandatory: boolean; verificationMode: string; allowAdminOverride: boolean }>>([]);
  const [form, setForm] = useState({ title: '', description: '', department: '', otherDepartment: '', engagementType: 'Permanent', shiftPattern: '', location: '', salaryMin: '50000', salaryMax: '90000', requiredProfessionalCategory: '', minimumYearsOfExperience: '0', customMinimumYearsOfExperience: '', requireVerifiedProfessional: true, allowInvites: true, closesAt: '' });
  const [posterFiles, setPosterFiles] = useState<File[]>([]);
  const [declarations, setDeclarations] = useState<DeclarationConfig[]>([]);
  const [acceptedDeclarationIds, setAcceptedDeclarationIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!hydrated || !user) return;
    employerApi.getByEmail(user.email).then(setProfile).catch(() => setProfile(null));
    professionalApi.getCategories().then(setCategories).catch(() => setCategories([]));
    verificationApi.getDocumentTypes('Professional').then(setDocumentTypes).catch(() => setDocumentTypes([]));
    jobApi.getSearchOptions().then((options) => setEngagementTypes(options.engagementTypes || [])).catch(() => setEngagementTypes([]));
    declarationApi.list('job-posting').then(setDeclarations).catch(() => setDeclarations([]));
  }, [hydrated, user]);

  const createJob = async (event: React.FormEvent) => {
    event.preventDefault();
    if (step !== 'review') {
      moveStep(1);
      return;
    }
    if (!profile) {
      setError('Complete your facility profile before creating an opening.');
      return;
    }
    if (!form.closesAt) {
      setError('Choose an application closing date before creating the opening.');
      setStep('role');
      return;
    }
    if (!requiredDeclarationsAccepted(declarations, acceptedDeclarationIds)) {
      setError('Review and accept the required declarations before creating the opening.');
      setStep('review');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const department = form.department === 'Other' ? form.otherDepartment.trim() : form.department;
      const minimumYearsOfExperience = form.minimumYearsOfExperience === 'custom'
        ? Number(form.customMinimumYearsOfExperience || 0)
        : Number(form.minimumYearsOfExperience || 0);
      const created = await jobApi.createJob({
        employerId: profile.id,
        tenantId: profile.tenantId,
        title: form.title,
        description: form.description,
        department,
        engagementType: form.engagementType || 'Permanent',
        shiftPattern: form.shiftPattern || null,
        location: form.location,
        salaryMin: Number(form.salaryMin),
        salaryMax: Number(form.salaryMax),
        requiredProfessionalCategory: selectedCategories.join(', ') || null,
        minimumYearsOfExperience,
        requireVerifiedProfessional: form.requireVerifiedProfessional,
        allowInvites: form.allowInvites,
        closesAt: toClosingIso(form.closesAt),
        requiredDocuments,
      });
      if (posterFiles.length > 0) {
        await jobApi.uploadPosters(created.id, posterFiles);
      }
      router.push('/employer/jobs');
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to create job opening.');
    } finally {
      setBusy(false);
    }
  };

  const steps: Array<typeof step> = ['role', 'requirements', 'documents', 'review'];
  const moveStep = (direction: number) => setStep(steps[Math.max(0, Math.min(steps.length - 1, steps.indexOf(step) + direction))]);
  const toggleCategory = (name: string) => setSelectedCategories((current) => current.includes(name) ? current.filter((item) => item !== name) : [...current, name]);
  const toggleDocument = (name: string) => setRequiredDocuments((current) => current.some((item) => item.documentType === name)
    ? current.filter((item) => item.documentType !== name)
    : [...current, { documentType: name, isMandatory: true, verificationMode: 'EmployerReview', allowAdminOverride: true }]);
  const updateRequiredDocument = (name: string, patch: Partial<{ isMandatory: boolean; verificationMode: string; allowAdminOverride: boolean }>) =>
    setRequiredDocuments((current) => current.map((item) => item.documentType === name ? { ...item, ...patch } : item));
  const addPosterFiles = (files: FileList | File[]) => {
    const incoming = Array.from(files).filter((file) => file.type.startsWith('image/') || file.type === 'application/pdf');
    setPosterFiles((current) => dedupeFiles([...current, ...incoming]));
  };
  const selectedEngagementType = engagementTypes.find((item) => item.name === form.engagementType);

  return (
    <Layout>
      <form className="surface-card p-6" onSubmit={createJob}>
        <h1 className="section-title">Create job opening</h1>
        <p className="section-copy mt-2">Walk through the role, candidate requirements, documents, and final review.</p>
        {error && <div className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}
        <div className="client-stepper mt-6">{steps.map((item, index) => <button key={item} type="button" className={step === item ? 'active' : ''} onClick={() => setStep(item)}>{index + 1}. {item}</button>)}</div>

        {step === 'role' && <div className="mt-6 grid gap-4 md:grid-cols-2">
          <label><span className="field-label">Job title</span><input className="input-shell" placeholder="Role title" value={form.title} onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))} required /></label>
          <label><span className="field-label">Department</span><select className="input-shell" value={form.department} onChange={(event) => setForm((current) => ({ ...current, department: event.target.value }))} required><option value="">Choose department</option>{departmentOptions.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
          {form.department === 'Other' && <label className="md:col-span-2"><span className="field-label">Specify department</span><input className="input-shell" placeholder="Enter department name" value={form.otherDepartment} onChange={(event) => setForm((current) => ({ ...current, otherDepartment: event.target.value }))} required /></label>}
          <label><span className="field-label">Job type or period</span><select className="input-shell" value={form.engagementType} onChange={(event) => setForm((current) => ({ ...current, engagementType: event.target.value, shiftPattern: '' }))} required>{engagementTypes.length === 0 && <option value="Permanent">Permanent</option>}{engagementTypes.map((item) => <option key={item.slug} value={item.name}>{item.name}</option>)}</select></label>
          {selectedEngagementType?.allowsShiftPattern && <label><span className="field-label">Shift or rota pattern</span><input className="input-shell" placeholder="Night shifts, weekends, short cover, ad hoc locum" value={form.shiftPattern} onChange={(event) => setForm((current) => ({ ...current, shiftPattern: event.target.value }))} /></label>}
          <label><span className="field-label">Location</span><input className="input-shell" placeholder="City, region, remote, or facility area" value={form.location} onChange={(event) => setForm((current) => ({ ...current, location: event.target.value }))} required /></label>
          <label><span className="field-label">Application closing date</span><input className="input-shell" type="date" value={form.closesAt} onChange={(event) => setForm((current) => ({ ...current, closesAt: event.target.value }))} required /></label>
          <label><span className="field-label">Salary minimum</span><input className="input-shell" type="number" placeholder="Minimum salary" value={form.salaryMin} onChange={(event) => setForm((current) => ({ ...current, salaryMin: event.target.value }))} /></label>
          <label><span className="field-label">Salary maximum</span><input className="input-shell" type="number" placeholder="Maximum salary" value={form.salaryMax} onChange={(event) => setForm((current) => ({ ...current, salaryMax: event.target.value }))} /></label>
          <label className="md:col-span-2"><span className="field-label">Role description</span><textarea className="text-shell" placeholder="Describe responsibilities, shift patterns, facility context, and qualifications" value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} required /></label>
        </div>}

        {step === 'requirements' && <div className="mt-6 space-y-5">
          <div className="client-choice-grid">{categories.map((category) => <button type="button" key={category.id} className={`client-choice ${selectedCategories.includes(category.name) ? 'selected' : ''}`} onClick={() => toggleCategory(category.name)}>{category.name}</button>)}</div>
          <div className="grid gap-4 md:grid-cols-3">
            <label><span className="field-label">Minimum experience required</span><select className="input-shell" value={form.minimumYearsOfExperience} onChange={(event) => setForm((current) => ({ ...current, minimumYearsOfExperience: event.target.value }))}>{experienceOptions.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}</select><span className="mt-1 block text-xs text-slate-500">Used as an applicant eligibility and matching floor.</span></label>
            {form.minimumYearsOfExperience === 'custom' && <label><span className="field-label">Custom minimum years</span><input className="input-shell" type="number" min="0" max="60" value={form.customMinimumYearsOfExperience} onChange={(event) => setForm((current) => ({ ...current, customMinimumYearsOfExperience: event.target.value }))} /></label>}
            <label className="client-switch"><input type="checkbox" checked={form.requireVerifiedProfessional} onChange={(event) => setForm((current) => ({ ...current, requireVerifiedProfessional: event.target.checked }))} /> Require verified professionals</label>
            <label className="client-switch"><input type="checkbox" checked={form.allowInvites} onChange={(event) => setForm((current) => ({ ...current, allowInvites: event.target.checked }))} /> Allow employer invites</label>
          </div>
        </div>}

        {step === 'documents' && <div className="mt-6">
          <p className="section-copy">Set applicant document requirements for this opening. When set here, these employer rules override platform professional defaults at apply time.</p>
          <div className="client-choice-grid mt-4">{documentTypes.map((doc) => <button type="button" key={doc.slug} className={`client-choice ${requiredDocuments.some((item) => item.documentType === doc.name) ? 'selected' : ''}`} onClick={() => toggleDocument(doc.name)}>{doc.name}</button>)}</div>
          <div className="mt-5 grid gap-4">
            {requiredDocuments.map((doc) => (
              <div key={doc.documentType} className="rounded-3xl border border-slate-200 bg-white p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="text-lg font-bold text-slate-900">{doc.documentType}</p>
                    <p className="text-sm text-slate-500">Employer-owned requirement for this specific opening.</p>
                  </div>
                  <button className="secondary-action" type="button" onClick={() => toggleDocument(doc.documentType)}>Remove</button>
                </div>
                <div className="mt-4 grid gap-3 md:grid-cols-3">
                  <label className="client-switch"><input type="checkbox" checked={doc.isMandatory} onChange={(event) => updateRequiredDocument(doc.documentType, { isMandatory: event.target.checked })} /> Mandatory for apply</label>
                  <label className="field-label">Verification route<select className="input-shell" value={doc.verificationMode} onChange={(event) => updateRequiredDocument(doc.documentType, { verificationMode: event.target.value })}><option value="EmployerReview">Employer review after upload</option><option value="PlatformVerification">Require platform verification first</option></select></label>
                  <label className="client-switch"><input type="checkbox" checked={doc.allowAdminOverride} onChange={(event) => updateRequiredDocument(doc.documentType, { allowAdminOverride: event.target.checked })} /> Allow admin override</label>
                </div>
              </div>
            ))}
          </div>
          <div
            className="mt-6 rounded-3xl border border-dashed border-slate-300 bg-slate-50 p-5"
            onDragOver={(event) => event.preventDefault()}
            onDrop={(event) => {
              event.preventDefault();
              addPosterFiles(event.dataTransfer.files);
            }}
          >
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <span className="field-label">Job posters</span>
                <span className="mt-1 block text-sm text-slate-500">Drag and drop multiple images or PDFs, or use the add poster button.</span>
              </div>
              <label className="primary-action cursor-pointer">
                Add posters
                <input type="file" accept="image/*,application/pdf" multiple hidden onChange={(event) => { addPosterFiles(event.target.files || []); event.currentTarget.value = ''; }} />
              </label>
            </div>
            {posterFiles.length > 0 && <div className="mt-4 grid gap-3 md:grid-cols-2">
              {posterFiles.map((file) => (
                <div key={`${file.name}-${file.size}-${file.lastModified}`} className="flex items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-white px-3 py-2">
                  <span className="truncate text-sm font-semibold text-slate-700">{file.name}</span>
                  <button type="button" className="text-sm font-bold text-rose-600" onClick={() => setPosterFiles((current) => current.filter((item) => item !== file))}>Remove</button>
                </div>
              ))}
            </div>}
          </div>
        </div>}

        {step === 'review' && <div className="review-card mt-6">
          <h3 className="text-2xl font-black">{form.title || 'Untitled opening'}</h3>
          <p className="mt-1 text-sm font-semibold text-slate-500">{form.engagementType || 'Permanent'}{form.shiftPattern ? ` · ${form.shiftPattern}` : ''}</p>
          <p className="mt-2 text-slate-500">{(form.department === 'Other' ? form.otherDepartment : form.department) || 'Department'} · {form.location || 'Location'}</p>
          <p className="mt-3">Categories: {selectedCategories.length ? selectedCategories.join(', ') : 'Open to multiple categories'}</p>
          <p>Documents: {requiredDocuments.length ? requiredDocuments.map((doc) => `${doc.documentType} (${doc.verificationMode === 'EmployerReview' ? 'employer review' : 'platform verified'})`).join(', ') : 'Platform defaults'}</p>
          <p>Posters: {posterFiles.length ? posterFiles.map((file) => file.name).join(', ') : 'No posters uploaded'}</p>
          <div className="mt-5">
            <DeclarationChecklist declarations={declarations} acceptedIds={acceptedDeclarationIds} onChange={setAcceptedDeclarationIds} />
          </div>
        </div>}

        <div className="mt-6 flex gap-3">
          <button className="secondary-action" type="button" disabled={step === 'role'} onClick={() => moveStep(-1)}>Back</button>
          {step !== 'review' ? <button className="primary-action" type="button" onClick={() => moveStep(1)}>Next</button> : <button className="primary-action" disabled={busy}>{busy ? 'Saving...' : 'Create draft opening'}</button>}
        </div>
      </form>
    </Layout>
  );
}
