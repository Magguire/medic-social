import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import Layout from '../../components/Layout';
import { employerApi } from '../../lib/employerApi';
import { jobApi } from '../../lib/jobApi';
import { useAuth, useRequireAuth } from '../../lib/useAuth';
import type { EmployerApplicant, EmployerProfile, Job } from '../../types';

export default function EmployerApplicantsPage() {
  const router = useRouter();
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [profile, setProfile] = useState<EmployerProfile | null>(null);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [selectedJobId, setSelectedJobId] = useState('');
  const [applications, setApplications] = useState<EmployerApplicant[]>([]);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!hydrated || !user) return;
    employerApi.getByEmail(user.email)
      .then(async (employer) => {
        setProfile(employer);
        const response = await jobApi.getEmployerJobs(employer.id, employer.tenantId);
        setJobs(response.jobs);
        const initialJobId = typeof router.query.jobId === 'string' ? router.query.jobId : response.jobs[0]?.id || '';
        setSelectedJobId(initialJobId);
      })
      .catch(() => undefined);
  }, [hydrated, user, router.query.jobId]);

  useEffect(() => {
    if (!profile || !selectedJobId) {
      setApplications([]);
      return;
    }
    jobApi.getApplicationsByJob(selectedJobId, profile.tenantId).then(setApplications).catch(() => setApplications([]));
  }, [profile, selectedJobId]);

  const reviewDocument = async (applicationId: string, documentId: string, isApproved: boolean) => {
    if (!profile) return;
    const notes = typeof window !== 'undefined'
      ? window.prompt(isApproved ? 'Approval notes (optional)' : 'Reason for rejection', '')
      : '';

    await jobApi.reviewApplicationDocument(applicationId, documentId, {
      tenantId: profile.tenantId,
      isApproved,
      notes: notes || undefined,
    });

    setMessage(isApproved ? 'Document approved for this applicant.' : 'Document rejected for this applicant.');
    const refreshed = await jobApi.getApplicationsByJob(selectedJobId, profile.tenantId);
    setApplications(refreshed);
  };

  return (
    <Layout>
      <div className="surface-card p-6">
        <h1 className="section-title">Applicants</h1>
        <p className="section-copy mt-2">Review applicants and their requirement fit for each created opening.</p>
        {message && <div className="mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{message}</div>}
        <div className="mt-5 max-w-xl">
          <select className="input-shell" value={selectedJobId} onChange={(event) => setSelectedJobId(event.target.value)}>
            <option value="">Select a job opening</option>
            {jobs.map((job) => <option key={job.id} value={job.id}>{job.title} - {job.status}</option>)}
          </select>
        </div>
        <div className="mt-6 grid gap-4">
          {applications.map((application) => (
            <article key={application.id} className="rounded-2xl border border-slate-200 bg-white px-4 py-4">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                  <p className="text-lg font-bold text-slate-900">{application.professionalCategory || 'Professional applicant'}</p>
                  <p className="mt-1 text-sm text-slate-500">{application.specialty || 'Specialty not set'} - {application.yearsOfExperience || 0} years</p>
                </div>
                <span className="pill-chip">{application.status}</span>
              </div>
              <div className="mt-4 grid gap-3 md:grid-cols-3">
                <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-400">Verification</p><p className="mt-2 font-semibold text-slate-900">{application.verificationStatus}</p></div>
                <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-400">Match score</p><p className="mt-2 font-semibold text-slate-900">{application.score ?? 'n/a'}</p></div>
                <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-400">Applied</p><p className="mt-2 font-semibold text-slate-900">{new Date(application.appliedAt).toLocaleDateString()}</p></div>
              </div>
              <div className="mt-4 rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Required documents</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {application.requiredDocuments.map((item) => <span key={item.id} className="pill-chip">{item.documentType} · {item.verificationMode === 'EmployerReview' ? 'Employer review' : 'Platform verification'}</span>)}
                  {application.requiredDocuments.length === 0 && <span className="text-sm text-slate-500">Platform defaults only</span>}
                </div>
                {application.missingRequiredDocuments.length > 0 && <p className="mt-3 text-sm font-semibold text-rose-600">Missing: {application.missingRequiredDocuments.join(', ')}</p>}
              </div>
              <div className="mt-4 grid gap-3">
                {application.documents.map((document) => (
                  <div key={document.id} className="rounded-2xl border border-slate-200 bg-white px-4 py-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div>
                        <p className="font-semibold text-slate-900">{document.documentType}</p>
                        <p className="text-sm text-slate-500">{document.fileName}</p>
                      </div>
                      <span className="pill-chip">{document.status}</span>
                    </div>
                    <p className="mt-2 text-sm text-slate-500">{document.verificationMode === 'EmployerReview' ? 'Employer can approve or reject this document for the job.' : 'This document follows platform verification before the application gate.'}</p>
                    {document.verificationNotes && <p className="mt-2 text-sm text-slate-600">Notes: {document.verificationNotes}</p>}
                    {document.isRequired && document.verificationMode === 'EmployerReview' && (
                      <div className="mt-3 flex gap-3">
                        <button className="secondary-action" type="button" onClick={() => reviewDocument(application.id, document.id, false)}>Reject</button>
                        <button className="primary-action" type="button" onClick={() => reviewDocument(application.id, document.id, true)}>Approve</button>
                      </div>
                    )}
                  </div>
                ))}
                {application.documents.length === 0 && <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-5 text-sm text-slate-500">No uploaded documents available for this applicant yet.</div>}
              </div>
            </article>
          ))}
          {applications.length === 0 && <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">No applicants for this opening yet.</div>}
        </div>
      </div>
    </Layout>
  );
}
