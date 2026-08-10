import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import Layout from '../../components/Layout';
import { jobApi } from '../../lib/jobApi';
import { professionalApi } from '../../lib/professionalApi';
import { subscriptionApi } from '../../lib/subscriptionApi';
import { useAuth } from '../../lib/useAuth';
import type { Job, ProfessionalProfile } from '../../types';

export default function JobDetailsPage() {
  const router = useRouter();
  const { id } = router.query;
  const { hydrated, isAuthenticated, user } = useAuth();
  const [job, setJob] = useState<Job | null>(null);
  const [profile, setProfile] = useState<ProfessionalProfile | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [applying, setApplying] = useState(false);
  const [isWatching, setIsWatching] = useState(false);
  const [paygoNotice, setPaygoNotice] = useState<string | null>(null);

  useEffect(() => {
    if (!hydrated || !id) return;
    jobApi.getJob(String(id)).then(setJob).catch(() => setError('Job not found.'));
  }, [hydrated, id]);

  useEffect(() => {
    if (!hydrated || !isAuthenticated || !user) return;
    professionalApi.getProfileByUser(user.id).then(setProfile).catch(() => setProfile(null));
    if (id && user.userType === 'Professional') {
      jobApi.getWatchStatus(String(id)).then((result) => setIsWatching(result.isWatching)).catch(() => setIsWatching(false));
      subscriptionApi.recordPayAsYouGo({ action: 0, relatedEntityId: String(id) })
        .then((result) => {
          if (result?.isChargeRequired) {
            setPaygoNotice(`This view was recorded as a paid job view: ${result.currency} ${result.amount}.`);
          }
        })
        .catch((requestError) => {
          if (requestError?.response?.status === 402) {
            const data = requestError.response.data;
            setPaygoNotice(data?.message || 'Payment is required to continue viewing more job details.');
          }
        });
    }
  }, [hydrated, id, isAuthenticated, user]);

  const toggleWatch = async () => {
    if (!job) return;
    if (!isAuthenticated || !user) {
      router.push(`/login?next=${encodeURIComponent(`/jobs/${job.id}`)}`);
      return;
    }
    if (user.userType !== 'Professional') {
      setError('Only professional accounts can watch jobs.');
      return;
    }

    try {
      const result = isWatching ? await jobApi.unwatchJob(job.id) : await jobApi.watchJob(job.id);
      setIsWatching(result.isWatching);
      setMessage(result.isWatching ? 'This job is now on your watch list.' : 'This job was removed from your watch list.');
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to update your watch list.');
    }
  };

  const handleApply = async () => {
    if (!job) return;
    if (!isAuthenticated || !user) {
      router.push(`/login?next=${encodeURIComponent(`/jobs/${job.id}`)}`);
      return;
    }
    if (user.userType !== 'Professional') {
      setError('Only professional accounts can apply for jobs.');
      return;
    }
    if (!profile) {
      router.push('/professional/profile');
      return;
    }

    try {
      setApplying(true);
      setError(null);
      await jobApi.applyForJob(job.id, profile.id, profile.tenantId);
      setMessage('Application submitted. Your timeline and audit activity have been updated.');
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to submit this application.');
    } finally {
      setApplying(false);
    }
  };

  if (!job) {
    return <Layout><div className="surface-card p-10 text-center text-slate-500">Loading role details...</div></Layout>;
  }

  return (
    <Layout>
      <section className="grid gap-6 lg:grid-cols-[1.45fr_0.8fr]">
        <article className="surface-card p-8">
          {job.posters?.length > 0 && (
            <div className="mb-6 grid gap-3 md:grid-cols-2">
              {job.posters.map((poster) => poster.contentType?.startsWith('image/') ? (
                <img key={poster.id} src={poster.publicUrl} alt={`${job.title} poster`} className="h-64 w-full rounded-3xl object-cover" />
              ) : (
                <a key={poster.id} href={poster.publicUrl} target="_blank" rel="noreferrer" className="rounded-3xl border border-slate-200 bg-slate-50 p-5 font-semibold text-slate-700">{poster.fileName}</a>
              ))}
            </div>
          )}
          <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-200 pb-6">
            <div>
              <span className="pill-chip">{job.status}</span>
              {job.displayStatus && <span className="pill-chip ml-2">{job.displayStatus === 'ClosingSoon' ? 'Closing soon' : job.displayStatus}</span>}
              <h1 className="mt-4 text-4xl font-black tracking-tight text-slate-900">{job.title}</h1>
              <p className="mt-2 text-lg text-slate-500">{job.department} · {job.location}</p>
            </div>
            <div className="rounded-[24px] bg-slate-50 px-5 py-4 text-right text-sm text-slate-500">
              <p>Salary range</p>
              <p className="mt-1 text-xl font-bold text-slate-900">KES {job.salaryMin.toLocaleString()} - {job.salaryMax.toLocaleString()}</p>
            </div>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-3">
            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Professional category</p><p className="mt-2 text-sm font-semibold text-slate-900">{job.requiredProfessionalCategory || 'Multiple categories supported'}</p></div>
            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Job type</p><p className="mt-2 text-sm font-semibold text-slate-900">{job.engagementType || 'Permanent'}{job.shiftPattern ? ` - ${job.shiftPattern}` : ''}</p></div>
            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Experience floor</p><p className="mt-2 text-sm font-semibold text-slate-900">{job.minimumYearsOfExperience ?? 0} years</p></div>
            <div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Closing date</p><p className="mt-2 text-sm font-semibold text-slate-900">{new Date(job.closesAt).toLocaleDateString()}</p></div>
          </div>

          <div className="mt-8">
            <h2 className="section-title">Role overview</h2>
            <p className="mt-4 whitespace-pre-wrap text-base leading-8 text-slate-600">{job.description}</p>
          </div>
        </article>

        <aside className="space-y-6">
          <div className="surface-card p-6">
            <h2 className="text-xl font-semibold text-slate-900">Application path</h2>
            <div className="mt-4 space-y-3 text-sm text-slate-600">
              <div className="rounded-2xl bg-slate-50 p-4">1. Browse publicly and review the job requirements.</div>
              <div className="rounded-2xl bg-slate-50 p-4">2. Sign in as a professional and complete your biodata, education, qualifications, and document uploads.</div>
              <div className="rounded-2xl bg-slate-50 p-4">3. Apply once your profile and required documents are ready.</div>
            </div>
            {message && <p className="mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{message}</p>}
            {paygoNotice && <p className="mt-4 rounded-2xl bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-800">{paygoNotice}</p>}
            {error && <p className="mt-4 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</p>}
            <button className="primary-action mt-5 w-full" disabled={applying} onClick={handleApply}>{applying ? 'Submitting...' : isAuthenticated ? 'Apply for this job' : 'Login to apply'}</button>
            <button className="secondary-action mt-3 w-full" type="button" onClick={toggleWatch}>{isWatching ? 'Remove from watch list' : 'Watch this job'}</button>
            {!isAuthenticated && <Link href="/register" className="secondary-action mt-3 w-full">Create account first</Link>}
            {isAuthenticated && user?.userType === 'Professional' && !profile && <Link href="/professional/profile" className="secondary-action mt-3 w-full">Complete profile</Link>}
          </div>
        </aside>
      </section>
    </Layout>
  );
}
