import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import Layout from '../components/Layout';
import { employerApi } from '../lib/employerApi';
import { jobApi } from '../lib/jobApi';
import { professionalApi } from '../lib/professionalApi';
import { useAuth, useRequireAuth } from '../lib/useAuth';
import type { EmployerDocument, EmployerProfile, Job, JobApplication, ProfessionalDocument, ProfessionalProfile } from '../types';

export default function DashboardPage() {
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [professionalProfile, setProfessionalProfile] = useState<ProfessionalProfile | null>(null);
  const [professionalDocuments, setProfessionalDocuments] = useState<ProfessionalDocument[]>([]);
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [employerProfile, setEmployerProfile] = useState<EmployerProfile | null>(null);
  const [employerDocuments, setEmployerDocuments] = useState<EmployerDocument[]>([]);
  const [employerJobs, setEmployerJobs] = useState<Job[]>([]);

  useEffect(() => {
    if (!hydrated || !user) return;

    if (user.userType === 'Professional') {
      professionalApi.getProfileByUser(user.id)
        .then(async (profile) => {
          setProfessionalProfile(profile);
          const [docs, apps] = await Promise.all([
            professionalApi.getDocuments(profile.id),
            jobApi.getProfessionalApplications(profile.id),
          ]);
          setProfessionalDocuments(docs);
          setApplications(apps);
        })
        .catch(() => setProfessionalProfile(null));
    }

    if (user.userType === 'Employer') {
      employerApi.getByEmail(user.email)
        .then(async (profile) => {
          setEmployerProfile(profile);
          const [docs, jobs] = await Promise.all([
            employerApi.getDocuments(profile.id),
            jobApi.getEmployerJobs(profile.id, profile.tenantId),
          ]);
          setEmployerDocuments(docs);
          setEmployerJobs(jobs.jobs);
        })
        .catch(() => setEmployerProfile(null));
    }
  }, [hydrated, user]);

  const completion = useMemo(() => {
    if (!user) return 0;
    if (user.userType === 'Employer') {
      let score = employerProfile ? 35 : 10;
      if (employerProfile?.businessRegistrationNumber || employerProfile?.kraPin || employerProfile?.licenseNumber) score += 25;
      if (employerDocuments.length > 0) score += 25;
      if (employerProfile?.verificationStatus === 'Verified') score += 15;
      return Math.min(score, 100);
    }

    let score = professionalProfile ? 40 : 15;
    if (professionalProfile?.professionalCategory) score += 20;
    if (professionalDocuments.length > 0) score += 25;
    if (professionalProfile?.verificationStatus === 'Verified') score += 15;
    return Math.min(score, 100);
  }, [employerDocuments.length, employerProfile, professionalDocuments.length, professionalProfile, user]);

  if (!user) {
    return <Layout><div className="surface-card p-10 text-center text-slate-500">Loading dashboard...</div></Layout>;
  }

  if (user.userType === 'Employer') {
    const publishedJobs = employerJobs.filter((job) => job.status === 'Published').length;
    const draftJobs = employerJobs.filter((job) => job.status === 'Draft').length;

    return (
      <Layout>
        <section>
          <div className="surface-card overflow-hidden">
            <div className="bg-[linear-gradient(120deg,var(--client-secondary),var(--client-primary))] px-6 py-7 text-white">
              <p className="text-sm font-semibold uppercase tracking-[0.32em] text-white/70">Employer overview</p>
              <h1 className="mt-3 text-3xl font-black tracking-tight">Your hiring workspace at a glance.</h1>
              <p className="mt-3 max-w-3xl text-white/82">Use the sidebar for facility profile, new openings, applicants, and settings. This dashboard stays focused on readiness.</p>
            </div>
            <div className="space-y-6 px-6 py-6">
              <div>
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h2 className="section-title">Onboarding completion</h2>
                    <p className="section-copy">Facility profile, documents, and verification readiness.</p>
                  </div>
                  <strong className="text-3xl text-slate-900">{completion}%</strong>
                </div>
                <div className="progress-track mt-4"><div className="progress-fill" style={{ width: `${completion}%` }} /></div>
              </div>
              <div className="grid gap-4 md:grid-cols-3">
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Published jobs</p><p className="mt-2 text-4xl font-black text-slate-900">{publishedJobs}</p></div>
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Draft jobs</p><p className="mt-2 text-4xl font-black text-slate-900">{draftJobs}</p></div>
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Verification</p><p className="mt-2 text-2xl font-bold text-slate-900">{employerProfile?.verificationStatus || 'Not started'}</p></div>
              </div>
              <div className="flex flex-wrap gap-3">
                <Link href="/employer/profile" className="primary-action">Manage facility profile</Link>
                <Link href="/feed" className="secondary-action">Open Feed</Link>
                <Link href="/employer/jobs/new" className="secondary-action">Create job opening</Link>
                <Link href="/employer/applicants" className="secondary-action">Review applicants</Link>
              </div>
            </div>
          </div>
        </section>
      </Layout>
    );
  }

  if (user.userType === 'Professional') {
    return (
      <Layout>
        <section>
          <div className="surface-card overflow-hidden">
            <div className="bg-[linear-gradient(120deg,var(--client-secondary),var(--client-primary))] px-6 py-7 text-white">
              <p className="text-sm font-semibold uppercase tracking-[0.32em] text-white/70">Professional overview</p>
              <h1 className="mt-3 text-3xl font-black tracking-tight">Stay application-ready.</h1>
              <p className="mt-3 max-w-3xl text-white/82">Complete your profile, track verification, and keep applications moving from focused workspace pages.</p>
            </div>
            <div className="space-y-6 px-6 py-6">
              <div>
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h2 className="section-title">Profile completion</h2>
                    <p className="section-copy">Profile category, documents, and verification readiness.</p>
                  </div>
                  <strong className="text-3xl text-slate-900">{completion}%</strong>
                </div>
                <div className="progress-track mt-4"><div className="progress-fill" style={{ width: `${completion}%` }} /></div>
              </div>
              <div className="grid gap-4 md:grid-cols-3">
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Applications</p><p className="mt-2 text-4xl font-black text-slate-900">{applications.length}</p></div>
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Documents</p><p className="mt-2 text-4xl font-black text-slate-900">{professionalDocuments.length}</p></div>
                <div className="rounded-2xl bg-slate-50 p-5"><p className="text-sm text-slate-500">Verification</p><p className="mt-2 text-2xl font-bold text-slate-900">{professionalProfile?.verificationStatus || 'Not started'}</p></div>
              </div>
              <div className="flex flex-wrap gap-3">
                <Link href="/professional/profile" className="primary-action">Complete profile</Link>
                <Link href="/feed" className="secondary-action">Open Feed</Link>
                <Link href="/jobs" className="secondary-action">Browse jobs</Link>
                <Link href="/applications" className="secondary-action">View applications</Link>
              </div>
            </div>
          </div>
        </section>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="surface-card p-8 text-center">
        <h1 className="section-title">Admin accounts continue in the operations console</h1>
        <p className="mt-3 text-sm text-slate-500">Use the dedicated admin UI for verification queues, configuration, subscriptions, and full audit oversight.</p>
        <a href="http://localhost:3001" className="primary-action mt-5 inline-flex">Open admin console</a>
      </div>
    </Layout>
  );
}
