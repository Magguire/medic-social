import Link from 'next/link';
import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { jobApi } from '../lib/jobApi';
import { professionalApi } from '../lib/professionalApi';
import { useAuth, useRequireAuth } from '../lib/useAuth';
import type { JobApplication, ProfessionalProfile } from '../types';

export default function ApplicationsPage() {
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [profile, setProfile] = useState<ProfessionalProfile | null>(null);
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hydrated || !user) return;
    professionalApi.getProfileByUser(user.id)
      .then((result) => {
        setProfile(result);
        return jobApi.getProfessionalApplications(result.id);
      })
      .then(setApplications)
      .catch(() => setError('Complete your professional profile first to start applying and tracking applications.'));
  }, [hydrated, user]);

  return (
    <Layout>
      <section className="surface-card p-6 sm:p-8">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="section-title">My Applications</h1>
            <p className="section-copy">Track every application submitted from your verified professional account.</p>
          </div>
          <Link href="/jobs" className="secondary-action">Browse more jobs</Link>
        </div>
        {error && <div className="mt-5 rounded-2xl bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-700">{error}</div>}
        {!error && applications.length === 0 && <div className="mt-6 rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">No applications yet. Once you apply, this timeline will show live status changes and shortlist updates.</div>}
        <div className="mt-6 space-y-4">
          {applications.map((application) => (
            <article key={application.id} className="rounded-[24px] border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xl font-bold text-slate-900">{application.jobTitle}</p>
                  <p className="mt-1 text-sm text-slate-500">{application.jobDepartment} � {application.jobLocation}</p>
                </div>
                <div className="rounded-full bg-[color-mix(in_srgb,var(--client-primary)_12%,var(--client-panel))] px-4 py-2 text-sm font-semibold text-[var(--client-primary)]">{application.status}</div>
              </div>
              <div className="mt-4 grid gap-3 rounded-2xl bg-slate-50 p-4 text-sm text-slate-600 md:grid-cols-4">
                <div><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Applied</p><p className="mt-2 font-semibold text-slate-900">{new Date(application.appliedAt).toLocaleString()}</p></div>
                <div><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Score</p><p className="mt-2 font-semibold text-slate-900">{application.score ?? 'Pending review'}</p></div>
                <div><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Shortlisted</p><p className="mt-2 font-semibold text-slate-900">{application.isShortlisted ? 'Yes' : 'No'}</p></div>
                <div><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Closes</p><p className="mt-2 font-semibold text-slate-900">{new Date(application.jobClosesAt).toLocaleDateString()}</p></div>
              </div>
              <div className="mt-4 flex gap-3">
                <Link href={`/jobs/${application.jobId}`} className="primary-action">View role</Link>
                {profile && <Link href="/professional/profile" className="secondary-action">Update profile</Link>}
              </div>
            </article>
          ))}
        </div>
      </section>
    </Layout>
  );
}
