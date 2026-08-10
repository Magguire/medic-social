import { useEffect, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function JobsPage() {
  const [user, setUser] = useState(null);
  const [jobs, setJobs] = useState([]);
  const [configuration, setConfiguration] = useState(null);
  const [employers, setEmployers] = useState([]);
  const [options, setOptions] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [selectedJob, setSelectedJob] = useState(null);
  const [moderationReason, setModerationReason] = useState('');
  const [step, setStep] = useState('role');
  const [selectedCategories, setSelectedCategories] = useState([]);
  const [requirementScore, setRequirementScore] = useState({ category: 30, experience: 25, verification: 25, documents: 20 });
  const [requiredDocuments, setRequiredDocuments] = useState([]);
  const [filters, setFilters] = useState({ q: '', category: '', department: '', engagementType: '', location: '', requireVerifiedProfessional: '', moderationState: 'active' });
  const [jobForm, setJobForm] = useState({
    employerId: '',
    title: '',
    description: '',
    department: '',
    engagementType: 'Permanent',
    shiftPattern: '',
    location: '',
    salaryMin: 50000,
    salaryMax: 90000,
    requiredProfessionalCategory: '',
    minimumYearsOfExperience: 0,
    requireVerifiedProfessional: true,
    allowInvites: true,
    publishNow: true,
    closesAt: '',
  });

  const load = async (nextFilters = filters) => {
    const [currentUser, jobResponse, config, employerResponse, searchOptions] = await Promise.all([
      adminApi.getCurrentUser(),
      adminApi.getAdminJobs(nextFilters),
      adminApi.getConfiguration(),
      adminApi.getEmployers(),
      adminApi.getJobSearchOptions(),
    ]);
      setUser(currentUser);
      setJobs(jobResponse.jobs || []);
      setConfiguration(config);
      setEmployers(employerResponse.items || []);
      setOptions(searchOptions);
  };

  useEffect(() => {
    load().catch(() => undefined);
  }, []);

  const applyFilters = async (event) => {
    event.preventDefault();
    await load(filters);
  };

  const clearFilters = async () => {
    const emptyFilters = { q: '', category: '', department: '', engagementType: '', location: '', requireVerifiedProfessional: '', moderationState: 'active' };
    setFilters(emptyFilters);
    await load(emptyFilters);
  };

  const toggleCategory = (name) => {
    setSelectedCategories((current) => current.includes(name) ? current.filter((item) => item !== name) : [...current, name]);
  };

  const toggleDocument = (name) => {
    setRequiredDocuments((current) => current.some((item) => item.documentType === name)
      ? current.filter((item) => item.documentType !== name)
      : [...current, { documentType: name, isMandatory: true, verificationMode: 'EmployerReview', allowAdminOverride: true }]);
  };

  const updateRequiredDocument = (name, patch) => {
    setRequiredDocuments((current) => current.map((item) => item.documentType === name ? { ...item, ...patch } : item));
  };

  const createJob = async (event) => {
    event.preventDefault();
    await adminApi.adminCreateJob({
      ...jobForm,
      salaryMin: Number(jobForm.salaryMin),
      salaryMax: Number(jobForm.salaryMax),
      minimumYearsOfExperience: Number(jobForm.minimumYearsOfExperience),
      requiredProfessionalCategory: selectedCategories.join(', ') || null,
      requiredDocuments,
    });
    setMessage('Job posting created successfully.');
    setJobForm((current) => ({ ...current, title: '', description: '', department: '', engagementType: 'Permanent', shiftPattern: '', location: '', closesAt: '' }));
    await load();
  };

  const openJob = async (id) => {
    setError('');
    try {
      setSelectedJob(await adminApi.getAdminJob(id));
    } catch (requestError) {
      setError(requestError.message || 'Unable to load job details.');
    }
  };

  const changeStatus = async (id, status) => {
    setError('');
    try {
      await adminApi.changeAdminJobStatus(id, { status, reason: moderationReason });
      setMessage(`Job ${status.toLowerCase()} successfully.`);
      setModerationReason('');
      setSelectedJob(null);
      await load();
    } catch (requestError) {
      setError(requestError.message || 'Unable to update job status.');
    }
  };

  const restoreJob = async (id) => {
    setError('');
    try {
      await adminApi.restoreAdminJob(id);
      setMessage('Job restored successfully.');
      setSelectedJob(null);
      await load({ ...filters, moderationState: filters.moderationState });
    } catch (requestError) {
      setError(requestError.message || 'Unable to restore job.');
    }
  };

  return (
    <AdminShell user={user} title="Job Posts" subtitle="Published roles alongside the configuration rules that shape who can post and apply.">
      {message && <div style={{ marginTop: 18, borderRadius: 16, background: '#e8f8ef', color: '#117549', padding: 14, fontWeight: 700 }}>{message}</div>}
      {error && <div style={{ marginTop: 18, borderRadius: 16, background: '#fff0f3', color: '#b0003a', padding: 14, fontWeight: 700 }}>{error}</div>}
      <details className="collapsible" open>
        <summary>Search filters</summary>
        <div className="collapsible-body">
          <form className="form-grid" onSubmit={applyFilters}>
            <input className="input" placeholder="Search title, location, department" value={filters.q} onChange={(event) => setFilters({ ...filters, q: event.target.value })} />
            <select className="select" value={filters.category} onChange={(event) => setFilters({ ...filters, category: event.target.value })}>
              <option value="">All professional categories</option>
              {configuration?.categories?.map((category) => <option key={category.id || category.slug} value={category.name}>{category.name}</option>)}
            </select>
            <select className="select" value={filters.department} onChange={(event) => setFilters({ ...filters, department: event.target.value })}>
              <option value="">All departments</option>
              {options?.departments?.map((department) => <option key={department} value={department}>{department}</option>)}
            </select>
            <select className="select" value={filters.engagementType} onChange={(event) => setFilters({ ...filters, engagementType: event.target.value })}>
              <option value="">All job types</option>
              {options?.engagementTypes?.map((item) => <option key={item.slug} value={item.name}>{item.name}</option>)}
            </select>
            <select className="select" value={filters.location} onChange={(event) => setFilters({ ...filters, location: event.target.value })}>
              <option value="">All locations</option>
              {options?.locations?.map((location) => <option key={location} value={location}>{location}</option>)}
            </select>
            <select className="select" value={filters.requireVerifiedProfessional} onChange={(event) => setFilters({ ...filters, requireVerifiedProfessional: event.target.value })}>
              <option value="">Any verification rule</option>
              <option value="true">Verified professionals required</option>
              <option value="false">Verification optional</option>
            </select>
            <select className="select" value={filters.moderationState} onChange={(event) => setFilters({ ...filters, moderationState: event.target.value })}>
              <option value="active">Active and ordinary jobs</option>
              <option value="flagged">Flagged jobs</option>
              <option value="removed">Removed jobs</option>
              <option value="all">All jobs</option>
            </select>
            <div className="button-row">
              <button className="btn-primary" type="submit">Apply filters</button>
              <button className="btn-secondary" type="button" onClick={clearFilters}>Clear</button>
            </div>
          </form>
        </div>
      </details>

      <div className="panel-card job-configurator" style={{ marginTop: 22 }}>
        <div className="settings-stage-header">
          <div>
            <p className="eyebrow">Configurator</p>
            <h2>Super admin job posting</h2>
            <span>Design a role, define matching requirements, attach document expectations, and weight the applicant score.</span>
          </div>
        </div>
        <div className="stepper">
          {['role', 'requirements', 'documents', 'scoring', 'review'].map((key, index) => <button key={key} className={step === key ? 'active' : ''} onClick={() => setStep(key)}>{index + 1}. {key}</button>)}
        </div>
        <form style={{ marginTop: 18 }} onSubmit={createJob}>
          {step === 'role' && <div className="form-grid">
            <select className="select" value={jobForm.employerId} onChange={(event) => setJobForm({ ...jobForm, employerId: event.target.value })} required><option value="">Select employer</option>{employers.map((employer) => <option key={employer.id} value={employer.id}>{employer.name} ({employer.facilityType})</option>)}</select>
            <input className="input" placeholder="Job title" value={jobForm.title} onChange={(event) => setJobForm({ ...jobForm, title: event.target.value })} required />
            <input className="input" placeholder="Department" value={jobForm.department} onChange={(event) => setJobForm({ ...jobForm, department: event.target.value })} required />
            <select className="select" value={jobForm.engagementType} onChange={(event) => setJobForm({ ...jobForm, engagementType: event.target.value, shiftPattern: '' })} required>
              {(options?.engagementTypes?.length ? options.engagementTypes : [{ name: 'Permanent', slug: 'permanent' }]).map((item) => <option key={item.slug} value={item.name}>{item.name}</option>)}
            </select>
            {options?.engagementTypes?.find((item) => item.name === jobForm.engagementType)?.allowsShiftPattern && <input className="input" placeholder="Shift or rota pattern" value={jobForm.shiftPattern} onChange={(event) => setJobForm({ ...jobForm, shiftPattern: event.target.value })} />}
            <input className="input" placeholder="Location" value={jobForm.location} onChange={(event) => setJobForm({ ...jobForm, location: event.target.value })} required />
            <input className="input" type="number" placeholder="Salary min" value={jobForm.salaryMin} onChange={(event) => setJobForm({ ...jobForm, salaryMin: event.target.value })} />
            <input className="input" type="number" placeholder="Salary max" value={jobForm.salaryMax} onChange={(event) => setJobForm({ ...jobForm, salaryMax: event.target.value })} />
            <input className="input" type="date" value={jobForm.closesAt} onChange={(event) => setJobForm({ ...jobForm, closesAt: event.target.value })} required />
            <textarea className="textarea" style={{ gridColumn: '1 / -1' }} placeholder="Describe responsibilities, shifts, facility context, and outcomes" value={jobForm.description} onChange={(event) => setJobForm({ ...jobForm, description: event.target.value })} required />
          </div>}

          {step === 'requirements' && <div className="stack">
            <h3 className="panel-heading">Candidate requirements</h3>
            <div className="choice-grid">{configuration?.categories?.map((category) => <button type="button" key={category.id} className={`choice-card ${selectedCategories.includes(category.name) ? 'selected' : ''}`} onClick={() => toggleCategory(category.name)}>{category.name}</button>)}</div>
            <div className="form-grid">
              <input className="input" type="number" placeholder="Minimum years" value={jobForm.minimumYearsOfExperience} onChange={(event) => setJobForm({ ...jobForm, minimumYearsOfExperience: event.target.value })} />
              <label className="switch-card"><input type="checkbox" checked={jobForm.requireVerifiedProfessional} onChange={(event) => setJobForm({ ...jobForm, requireVerifiedProfessional: event.target.checked })} /> Require verified professional</label>
              <label className="switch-card"><input type="checkbox" checked={jobForm.allowInvites} onChange={(event) => setJobForm({ ...jobForm, allowInvites: event.target.checked })} /> Allow invites</label>
              <label className="switch-card"><input type="checkbox" checked={jobForm.publishNow} onChange={(event) => setJobForm({ ...jobForm, publishNow: event.target.checked })} /> Publish immediately</label>
            </div>
          </div>}

          {step === 'documents' && <div className="stack">
            <h3 className="panel-heading">Required documents</h3>
            <p className="panel-subtitle">Select applicant documents for this posting. These job-level rules override platform professional defaults when the job is evaluated.</p>
            <div className="choice-grid">{configuration?.documentTypes?.filter((doc) => String(doc.targetType) === 'Professional' || Number(doc.targetType) === 0).map((doc) => <button type="button" key={doc.slug} className={`choice-card ${requiredDocuments.some((item) => item.documentType === doc.name) ? 'selected' : ''}`} onClick={() => toggleDocument(doc.name)}>{doc.name}</button>)}</div>
            <div className="stack">
              {requiredDocuments.map((doc) => (
                <div key={doc.documentType} className="review-card">
                  <h3>{doc.documentType}</h3>
                  <div className="form-grid" style={{ marginTop: 12 }}>
                    <label className="switch-card"><input type="checkbox" checked={doc.isMandatory} onChange={(event) => updateRequiredDocument(doc.documentType, { isMandatory: event.target.checked })} /> Mandatory for apply</label>
                    <label className="field-label">Verification route<select className="select" value={doc.verificationMode} onChange={(event) => updateRequiredDocument(doc.documentType, { verificationMode: event.target.value })}><option value="EmployerReview">Employer review after upload</option><option value="PlatformVerification">Require platform verification</option></select></label>
                    <label className="switch-card"><input type="checkbox" checked={doc.allowAdminOverride} onChange={(event) => updateRequiredDocument(doc.documentType, { allowAdminOverride: event.target.checked })} /> Allow admin override</label>
                  </div>
                </div>
              ))}
            </div>
          </div>}

          {step === 'scoring' && <div className="form-grid">
            {Object.entries(requirementScore).map(([key, value]) => <label key={key} className="field-label">{key} score weight<input className="input" type="number" value={value} onChange={(event) => setRequirementScore({ ...requirementScore, [key]: Number(event.target.value) })} /></label>)}
          </div>}

          {step === 'review' && <div className="review-card">
            <h3>{jobForm.title || 'Untitled role'}</h3>
            <p>{jobForm.engagementType || 'Permanent'}{jobForm.shiftPattern ? ` · ${jobForm.shiftPattern}` : ''}</p>
            <p>{jobForm.department || 'Department'} · {jobForm.location || 'Location'}</p>
            <p>Categories: {selectedCategories.length ? selectedCategories.join(', ') : 'Open'}</p>
            <p>Documents: {requiredDocuments.length ? requiredDocuments.map((doc) => `${doc.documentType} (${doc.verificationMode === 'EmployerReview' ? 'employer review' : 'platform verified'})`).join(', ') : 'Platform defaults'}</p>
            <p>Score weights: {Object.entries(requirementScore).map(([key, value]) => `${key} ${value}`).join(' / ')}</p>
          </div>}

          <div className="button-row" style={{ marginTop: 18 }}>
            <button className="btn-secondary" type="button" disabled={step === 'role'} onClick={() => setStep(['role', 'requirements', 'documents', 'scoring', 'review'][Math.max(0, ['role', 'requirements', 'documents', 'scoring', 'review'].indexOf(step) - 1)])}>Back</button>
            {step !== 'review' ? <button className="btn-primary" type="button" onClick={() => setStep(['role', 'requirements', 'documents', 'scoring', 'review'][Math.min(4, ['role', 'requirements', 'documents', 'scoring', 'review'].indexOf(step) + 1)])}>Next</button> : <button className="btn-primary" type="submit">Create job</button>}
          </div>
        </form>
      </div>

      <div className="panel-grid">
        <div className="panel-card">
          <h2 className="panel-heading">Job moderation</h2>
          <p className="pagination-meta">Showing {jobs.length} jobs. Use flagged/removed filters for recoverable moderation views.</p>
          <table className="table-shell">
            <thead><tr><th>Title</th><th>Employer</th><th>Department</th><th>Type</th><th>Location</th><th>Status</th><th>Applications</th><th>Actions</th></tr></thead>
            <tbody>
              {jobs.map((row) => {
                const job = row.job || row;
                return (
                <tr key={job.id}>
                  <td>{job.title}</td>
                  <td>{row.employerName || 'Unknown employer'}</td>
                  <td>{job.department}</td>
                  <td>{job.engagementType || 'Permanent'}</td>
                  <td>{job.location}</td>
                  <td><span className={`badge ${String(job.status).toLowerCase()}`}>{job.status}</span></td>
                  <td>{row.applicationsCount || 0}</td>
                  <td>
                    <div className="button-row">
                      <button className="btn-secondary" type="button" onClick={() => openJob(job.id)}>View</button>
                      {job.status === 'Flagged' || job.status === 'Removed'
                        ? <button className="btn-primary" type="button" onClick={() => restoreJob(job.id)}>Restore</button>
                        : <button className="btn-secondary" type="button" onClick={() => changeStatus(job.id, 'Closed')}>Close</button>}
                    </div>
                  </td>
                </tr>
              );})}
            </tbody>
          </table>
        </div>

        <div className="panel-card">
          <h2 className="panel-heading">Configuration Snapshot</h2>
          <div className="stack" style={{ marginTop: 16 }}>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Categories</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{configuration?.categories?.length || 0}</div></div>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Locations</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{options?.metrics?.locationCount || 0}</div></div>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Subscription plans</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{configuration?.subscriptionPlans?.length || 0}</div></div>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Verification policies</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{configuration?.verificationPolicies?.length || 0}</div></div>
          </div>
        </div>
      </div>

      {selectedJob && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-card">
            <div className="settings-stage-header">
              <div>
                <p className="eyebrow">Job details</p>
                <h2>{selectedJob.job.title}</h2>
                <span>{selectedJob.employer?.name || 'Unknown employer'} - {selectedJob.job.department} - {selectedJob.job.engagementType || 'Permanent'} - {selectedJob.job.location}</span>
              </div>
              <button className="btn-secondary" type="button" onClick={() => setSelectedJob(null)}>Close</button>
            </div>
            <div className="panel-grid" style={{ marginTop: 18 }}>
              <div className="panel-card">
                <h3 className="panel-heading">Role summary</h3>
                <p>{selectedJob.job.description}</p>
                <div className="stack" style={{ marginTop: 14 }}>
                  <span>Status: <strong>{selectedJob.job.status}</strong></span>
                  <span>Display status: <strong>{selectedJob.job.displayStatus}</strong></span>
                  <span>Applications: <strong>{selectedJob.applicationCount}</strong></span>
                  <span>Watchers: <strong>{selectedJob.watchCount}</strong></span>
                  {selectedJob.job.moderationReason && <span>Moderation reason: <strong>{selectedJob.job.moderationReason}</strong></span>}
                </div>
              </div>
              <div className="panel-card">
                <h3 className="panel-heading">Admin actions</h3>
                <textarea className="textarea" placeholder="Reason shown to employer when flagging or removing" value={moderationReason} onChange={(event) => setModerationReason(event.target.value)} />
                <div className="button-row" style={{ marginTop: 14 }}>
                  <button className="btn-secondary" type="button" onClick={() => changeStatus(selectedJob.job.id, 'Draft')}>Set draft</button>
                  <button className="btn-secondary" type="button" onClick={() => changeStatus(selectedJob.job.id, 'Published')}>Publish</button>
                  <button className="btn-secondary" type="button" onClick={() => changeStatus(selectedJob.job.id, 'Closed')}>Close</button>
                  <button className="btn-secondary" type="button" onClick={() => changeStatus(selectedJob.job.id, 'Flagged')}>Flag</button>
                  <button className="btn-primary" type="button" onClick={() => changeStatus(selectedJob.job.id, 'Removed')}>Remove</button>
                  {(selectedJob.job.status === 'Flagged' || selectedJob.job.status === 'Removed') && <button className="btn-secondary" type="button" onClick={() => restoreJob(selectedJob.job.id)}>Restore</button>}
                </div>
              </div>
            </div>
            <div className="panel-card" style={{ marginTop: 18 }}>
              <h3 className="panel-heading">Posters and requirements</h3>
              <div className="choice-grid">
                {(selectedJob.job.posters || []).map((poster) => <a key={poster.id} className="choice-card selected" href={poster.publicUrl} target="_blank" rel="noreferrer">{poster.fileName}</a>)}
                {(selectedJob.job.requiredDocuments || []).map((doc) => <span key={doc.id} className="choice-card">{doc.documentType} {doc.isMandatory ? '(mandatory)' : '(optional)'}</span>)}
              </div>
            </div>
          </div>
        </div>
      )}
    </AdminShell>
  );
}
