import { useEffect, useState } from 'react';
import Link from 'next/link';
import Layout from '../../../components/Layout';
import { employerApi } from '../../../lib/employerApi';
import { jobApi } from '../../../lib/jobApi';
import { useAuth, useRequireAuth } from '../../../lib/useAuth';
import type { EmployerProfile, Job } from '../../../types';

export default function EmployerJobsPage() {
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [profile, setProfile] = useState<EmployerProfile | null>(null);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busyJobId, setBusyJobId] = useState<string | null>(null);
  const [editingJob, setEditingJob] = useState<Job | null>(null);
  const [engagementTypes, setEngagementTypes] = useState<Array<{ name: string; slug: string; allowsShiftPattern: boolean }>>([]);
  const [editForm, setEditForm] = useState({ title: '', department: '', engagementType: 'Permanent', shiftPattern: '', location: '', description: '', salaryMin: '', salaryMax: '', closesAt: '' });
  const [editPosterFiles, setEditPosterFiles] = useState<File[]>([]);

  const load = async () => {
    if (!user) return;
    const employer = await employerApi.getByEmail(user.email);
    setProfile(employer);
    const response = await jobApi.getEmployerJobs(employer.id, employer.tenantId);
    setJobs(response.jobs);
    jobApi.getSearchOptions().then((options) => setEngagementTypes(options.engagementTypes || [])).catch(() => setEngagementTypes([]));
  };

  useEffect(() => {
    if (hydrated && user) load().catch(() => undefined);
  }, [hydrated, user]);

  const publishJob = async (jobId: string) => {
    if (!profile) return;
    setBusyJobId(jobId);
    setError(null);
    try {
      await jobApi.publishJob(jobId, profile.tenantId);
      await load();
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to publish job. Verify facility documents and subscription rules first.');
    } finally {
      setBusyJobId(null);
    }
  };

  const openEdit = (job: Job) => {
    setEditingJob(job);
    setEditPosterFiles([]);
    setEditForm({
      title: job.title || '',
      department: job.department || '',
      engagementType: job.engagementType || 'Permanent',
      shiftPattern: job.shiftPattern || '',
      location: job.location || '',
      description: job.description || '',
      salaryMin: String(job.salaryMin || ''),
      salaryMax: String(job.salaryMax || ''),
      closesAt: job.closesAt ? job.closesAt.slice(0, 10) : '',
    });
  };

  const saveEdit = async () => {
    if (!editingJob || !profile) return;
    setBusyJobId(editingJob.id);
    setError(null);
    try {
      await jobApi.updateJob(editingJob.id, {
        tenantId: profile.tenantId,
        title: editForm.title,
        department: editForm.department,
        engagementType: editForm.engagementType || 'Permanent',
        shiftPattern: editForm.shiftPattern || null,
        location: editForm.location,
        description: editForm.description,
        salaryMin: Number(editForm.salaryMin || 0),
        salaryMax: Number(editForm.salaryMax || 0),
        requiredProfessionalCategory: editingJob.requiredProfessionalCategory || null,
        minimumYearsOfExperience: editingJob.minimumYearsOfExperience,
        requireVerifiedProfessional: editingJob.requireVerifiedProfessional,
        allowInvites: editingJob.allowInvites,
        closesAt: new Date(`${editForm.closesAt}T23:59:59`).toISOString(),
        requiredDocuments: editingJob.requiredDocuments || [],
      });
      if (editPosterFiles.length > 0) {
        await jobApi.uploadPosters(editingJob.id, editPosterFiles);
      }
      setEditingJob(null);
      setEditPosterFiles([]);
      await load();
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to update job.');
    } finally {
      setBusyJobId(null);
    }
  };

  const deletePoster = async (posterId: string) => {
    if (!editingJob) return;
    setBusyJobId(editingJob.id);
    setError(null);
    try {
      await jobApi.deletePoster(editingJob.id, posterId);
      setEditingJob((current) => current ? { ...current, posters: current.posters.filter((poster) => poster.id !== posterId) } : current);
      await load();
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to delete poster.');
    } finally {
      setBusyJobId(null);
    }
  };

  const changeStatus = async (job: Job, status: 'Closed' | 'Cancelled') => {
    if (!profile) return;
    setBusyJobId(job.id);
    setError(null);
    try {
      await jobApi.changeStatus(job.id, profile.tenantId, status);
      await load();
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to update job visibility.');
    } finally {
      setBusyJobId(null);
    }
  };
  const selectedEditEngagementType = engagementTypes.find((item) => item.name === editForm.engagementType);

  return (
    <Layout>
      <div className="surface-card p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="section-title">Created openings</h1>
            <p className="section-copy mt-2">View drafts and published job openings for your facility.</p>
          </div>
          <Link href="/employer/jobs/new" className="primary-action">Create opening</Link>
        </div>
        {error && <div className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}
        <div className="mt-6 grid gap-4">
          {jobs.map((job) => (
            <article key={job.id} className="rounded-2xl border border-slate-200 bg-white px-4 py-4">
              {job.posters?.some((poster) => poster.contentType?.startsWith('image/')) && (
                <div className="mb-4 grid gap-3 md:grid-cols-2">
                  {job.posters.filter((poster) => poster.contentType?.startsWith('image/')).slice(0, 2).map((poster) => (
                    <img key={poster.id} src={poster.publicUrl} alt={`${job.title} poster`} className="h-44 w-full rounded-2xl object-cover" />
                  ))}
                </div>
              )}
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                  <p className="text-lg font-bold text-slate-900">{job.title}</p>
                  <p className="mt-1 text-sm text-slate-500">{job.department} - {job.engagementType || 'Permanent'} - {job.location}</p>
                  {job.posters?.length > 0 && <p className="mt-1 text-xs font-semibold text-slate-500">{job.posters.length} poster{job.posters.length === 1 ? '' : 's'} attached</p>}
                </div>
                <div className="flex flex-wrap gap-2">
                  <span className="pill-chip">{job.status}</span>
                  {job.displayStatus && job.displayStatus !== job.status && (
                    <span className="pill-chip border-amber-200 bg-amber-50 text-amber-800">
                      {job.displayStatus === 'ClosingSoon' ? 'Closing soon' : job.displayStatus}
                    </span>
                  )}
                </div>
              </div>
              <div className="mt-4 flex flex-wrap gap-3">
                <Link href={`/employer/applicants?jobId=${job.id}`} className="secondary-action">View applicants</Link>
                <Link href={`/professionals?jobId=${job.id}`} className="secondary-action">Find matches</Link>
                <button className="secondary-action" type="button" onClick={() => openEdit(job)}>Edit</button>
                {job.status === 'Draft' && <button className="primary-action" disabled={busyJobId === job.id} onClick={() => publishJob(job.id)}>{busyJobId === job.id ? 'Publishing...' : 'Publish'}</button>}
                {job.status === 'Published' && <button className="secondary-action" type="button" disabled={busyJobId === job.id} onClick={() => changeStatus(job, 'Closed')}>Hide / close</button>}
                {job.status !== 'Cancelled' && <button className="secondary-action" type="button" disabled={busyJobId === job.id} onClick={() => changeStatus(job, 'Cancelled')}>Delete / cancel</button>}
              </div>
            </article>
          ))}
          {jobs.length === 0 && <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">No openings created yet.</div>}
        </div>
      </div>
      {editingJob && (
        <div className="modal-backdrop">
          <div className="surface-card max-w-3xl p-6">
            <h2 className="section-title">Edit opening</h2>
            <div className="mt-5 grid gap-4 md:grid-cols-2">
              <label><span className="field-label">Job title</span><input className="input-shell" value={editForm.title} onChange={(event) => setEditForm((current) => ({ ...current, title: event.target.value }))} /></label>
              <label><span className="field-label">Department</span><input className="input-shell" value={editForm.department} onChange={(event) => setEditForm((current) => ({ ...current, department: event.target.value }))} /></label>
              <label><span className="field-label">Job type or period</span><select className="input-shell" value={editForm.engagementType} onChange={(event) => setEditForm((current) => ({ ...current, engagementType: event.target.value, shiftPattern: '' }))}>{(engagementTypes.length ? engagementTypes : [{ name: 'Permanent', slug: 'permanent', allowsShiftPattern: false }]).map((item) => <option key={item.slug} value={item.name}>{item.name}</option>)}</select></label>
              {selectedEditEngagementType?.allowsShiftPattern && <label><span className="field-label">Shift or rota pattern</span><input className="input-shell" value={editForm.shiftPattern} onChange={(event) => setEditForm((current) => ({ ...current, shiftPattern: event.target.value }))} /></label>}
              <label><span className="field-label">Location</span><input className="input-shell" value={editForm.location} onChange={(event) => setEditForm((current) => ({ ...current, location: event.target.value }))} /></label>
              <label><span className="field-label">Closing date</span><input className="input-shell" type="date" value={editForm.closesAt} onChange={(event) => setEditForm((current) => ({ ...current, closesAt: event.target.value }))} /></label>
              <label><span className="field-label">Salary minimum</span><input className="input-shell" type="number" value={editForm.salaryMin} onChange={(event) => setEditForm((current) => ({ ...current, salaryMin: event.target.value }))} /></label>
              <label><span className="field-label">Salary maximum</span><input className="input-shell" type="number" value={editForm.salaryMax} onChange={(event) => setEditForm((current) => ({ ...current, salaryMax: event.target.value }))} /></label>
              <label className="md:col-span-2"><span className="field-label">Role description</span><textarea className="text-shell" value={editForm.description} onChange={(event) => setEditForm((current) => ({ ...current, description: event.target.value }))} /></label>
            </div>
            <div className="mt-5 rounded-3xl border border-dashed border-slate-300 bg-slate-50 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <span className="field-label">Job posters</span>
                  <p className="text-sm text-slate-500">Add new posters or remove old ones from this opening.</p>
                </div>
                <label className="secondary-action cursor-pointer">
                  Add posters
                  <input type="file" accept="image/*,application/pdf" multiple hidden onChange={(event) => setEditPosterFiles((current) => [...current, ...Array.from(event.target.files || [])])} />
                </label>
              </div>
              <div className="mt-4 grid gap-3 md:grid-cols-2">
                {editingJob.posters?.map((poster) => (
                  <div key={poster.id} className="rounded-2xl border border-slate-200 bg-white p-3">
                    {poster.contentType?.startsWith('image/') ? <img src={poster.publicUrl} alt={poster.fileName} className="mb-3 h-28 w-full rounded-xl object-cover" /> : <p className="mb-3 text-sm font-semibold text-slate-700">{poster.fileName}</p>}
                    <button type="button" className="text-sm font-bold text-rose-600" disabled={busyJobId === editingJob.id} onClick={() => deletePoster(poster.id)}>Delete poster</button>
                  </div>
                ))}
                {editPosterFiles.map((file) => <div key={`${file.name}-${file.size}-${file.lastModified}`} className="rounded-2xl bg-white p-3 text-sm font-semibold text-slate-700">{file.name}</div>)}
              </div>
            </div>
            <div className="mt-6 flex flex-wrap gap-3">
              <button className="primary-action" type="button" disabled={busyJobId === editingJob.id} onClick={saveEdit}>Save changes</button>
              <button className="secondary-action" type="button" onClick={() => setEditingJob(null)}>Cancel</button>
            </div>
          </div>
        </div>
      )}
    </Layout>
  );
}
