import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { contentApi } from '../lib/contentApi';
import { jobApi } from '../lib/jobApi';
import type { Job } from '../types';

const storyCards = [
  {
    isVisible: true,
    partner: 'Verified employer',
    label: 'Verified employer',
    quote: 'We can review applications, documents, and candidate conversations in one calmer workspace.',
    testimonial: 'We can review applications, documents, and candidate conversations in one calmer workspace.',
    title: 'Verified hiring partners',
    author: 'medicSocial partner',
    imageUrl: 'https://images.unsplash.com/photo-1550831107-1553da8c8464?auto=format&fit=crop&w=1400&q=80',
    imageAlt: 'Healthcare professional with stethoscope in a clinical setting',
    tint: 'from-[#eef8f4] via-[#f9faf8] to-[#e8f2ee]',
  },
  {
    isVisible: true,
    partner: 'Professional network',
    label: 'Professional network',
    quote: 'I can browse first, watch interesting roles, then apply once my profile and documents are ready.',
    testimonial: 'I can browse first, watch interesting roles, then apply once my profile and documents are ready.',
    title: 'Application-ready professionals',
    author: 'Verified professional',
    imageUrl: 'https://images.unsplash.com/photo-1582750433449-648ed127bb54?auto=format&fit=crop&w=1400&q=80',
    imageAlt: 'Medical professional reviewing patient or career information',
    tint: 'from-[#f8f1e7] via-[#fffdf8] to-[#eaf5f1]',
  },
  {
    isVisible: true,
    partner: 'Community feed',
    label: 'Community feed',
    quote: 'The Feed keeps hiring conversations, role signals, and healthcare career discussions visible.',
    testimonial: 'The Feed keeps hiring conversations, role signals, and healthcare career discussions visible.',
    title: 'A marketplace with conversation',
    author: 'Healthcare community',
    imageUrl: 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=1400&q=80',
    imageAlt: 'Healthcare team collaborating around a tablet',
    tint: 'from-[#eef5f2] via-[#f8fbfa] to-[#f2eadf]',
  },
];

const platformFeatures = [
  { icon: 'V', title: 'Verified profiles', body: 'Profiles, education, experience, documents, and verification status stay connected to applications.', isVisible: true, displayOrder: 0 },
  { icon: 'E', title: 'Employer workspaces', body: 'Facilities manage job posts, team access, applicants, candidate invites, and communication.', isVisible: true, displayOrder: 1 },
  { icon: 'F', title: 'Feed and messages', body: 'Registered users can post, join channels, request chats, and receive in-app notifications.', isVisible: true, displayOrder: 2 },
  { icon: 'A', title: 'Admin-configured rules', body: 'Subscriptions, pay-as-you-go, declarations, legal pages, document rules, and policies remain configurable.', isVisible: true, displayOrder: 3 },
];

const defaultLanding = {
  isHeroMediaVisible: true,
  badgeText: 'Open roles available now',
  headline: 'The specialized home for healthcare careers.',
  highlightText: 'healthcare',
  subheading: 'Connecting medical professionals, healthcare facilities, and hiring teams through verified profiles, configurable workflows, and a marketplace built for care work.',
  primaryCallToActionText: 'Find roles',
  primaryCallToActionUrl: '/jobs',
  secondaryCallToActionText: 'Join network',
  secondaryCallToActionUrl: '/register',
  heroSlides: storyCards,
  featureCards: platformFeatures,
  employerCalloutTitle: 'Hiring for a medical facility? Find vetted talent with a clearer pipeline.',
  employerCalloutBody: 'Post openings, configure requirements, manage applicants, invite matching professionals, and keep communication inside the same workspace.',
};

function formatMoney(value: number) {
  if (!value) return 'Salary not listed';
  return value.toLocaleString();
}

export default function HomePage() {
  const router = useRouter();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [stats, setStats] = useState({ liveJobs: 0, employers: 0, professionals: 0 });
  const [searchTerm, setSearchTerm] = useState('');
  const [location, setLocation] = useState('');
  const [activeStory, setActiveStory] = useState(0);
  const [landing, setLanding] = useState(defaultLanding);

  useEffect(() => {
    Promise.all([
      jobApi.listJobs(undefined, 1, 6).catch(() => ({ jobs: [], totalCount: 0 })),
      jobApi.getMarketplaceMetrics().catch(() => ({ liveJobs: 0, employers: 0, professionals: 0 })),
    ]).then(([jobResponse, metricResponse]) => {
      const publicJobs = jobResponse.jobs || [];
      setJobs(publicJobs);
      setStats({
        liveJobs: metricResponse.liveJobs || jobResponse.totalCount || publicJobs.length || 0,
        employers: metricResponse.employers || 0,
        professionals: metricResponse.professionals || 0,
      });
    });
  }, []);

  useEffect(() => {
    contentApi.getLandingPage()
      .then((config) => setLanding({ ...defaultLanding, ...config }))
      .catch(() => setLanding(defaultLanding));
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => {
      const visibleStories = (landing.heroSlides || []).filter((item: any) => item.isVisible !== false);
      setActiveStory((current) => (current + 1) % Math.max(visibleStories.length, 1));
    }, 6500);

    return () => window.clearInterval(timer);
  }, [landing.heroSlides]);

  const visibleStories = (landing.heroSlides || []).filter((item: any) => item.isVisible !== false).sort((a: any, b: any) => (a.displayOrder || 0) - (b.displayOrder || 0));
  const visibleFeatures = (landing.featureCards || []).filter((item: any) => item.isVisible !== false).sort((a: any, b: any) => (a.displayOrder || 0) - (b.displayOrder || 0));
  const story = visibleStories[activeStory] || storyCards[0];
  const featuredJobs = jobs.slice(0, 4);
  const secondaryJobs = jobs.slice(4, 6);

  const handleSearch = () => {
    const params = new URLSearchParams();
    if (searchTerm.trim()) params.set('q', searchTerm.trim());
    if (location.trim()) params.set('location', location.trim());
    router.push(`/jobs${params.toString() ? `?${params.toString()}` : ''}`);
  };

  return (
    <Layout>
      <section className="overflow-hidden rounded-[44px] border border-slate-200/80 bg-[var(--client-panel)] shadow-[0_28px_90px_rgba(15,23,42,0.08)]">
        <div className="grid gap-8 px-5 py-8 sm:px-8 lg:px-10 xl:grid-cols-[minmax(0,0.95fr)_minmax(420px,0.9fr)] xl:py-10">
          <div className="flex min-h-[500px] flex-col justify-between">
            <div>
              <div className="inline-flex items-center gap-3 rounded-full bg-[color-mix(in_srgb,var(--client-primary)_12%,var(--client-panel))] px-4 py-2 text-sm font-black uppercase tracking-[0.14em] text-[var(--client-primary)]">
                <span className="grid h-5 w-5 place-items-center rounded-full bg-[var(--client-primary)]">
                  <span className="h-2.5 w-2.5 rounded-full bg-white" />
                </span>
                {stats.liveJobs || featuredJobs.length} {landing.badgeText}
              </div>

              <h1 className="mt-8 max-w-6xl text-[4.25rem] font-black leading-[0.95] tracking-[-0.075em] text-[var(--client-text)] sm:text-[5.9rem] xl:text-[6.9rem]">
                {landing.headline.includes(landing.highlightText) && landing.highlightText ? (
                  <>
                    {landing.headline.split(landing.highlightText)[0]}<span className="text-[var(--client-primary)]">{landing.highlightText}</span>{landing.headline.split(landing.highlightText).slice(1).join(landing.highlightText)}
                  </>
                ) : landing.headline}
              </h1>

              <p className="mt-8 max-w-3xl text-xl leading-9 text-slate-700 sm:text-2xl">
                {landing.subheading}
              </p>
            </div>

            <div className="mt-10 space-y-5">
              <div className="rounded-[28px] border border-slate-200 bg-white p-3 shadow-[0_26px_70px_rgba(15,23,42,0.12)]">
                <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(220px,0.7fr)_auto]">
                  <label className="sr-only" htmlFor="landing-search">Job title or keyword</label>
                  <input
                    id="landing-search"
                    className="h-16 rounded-[22px] border border-slate-100 bg-[var(--client-panel)] px-6 text-lg text-[var(--client-text)] outline-none transition focus:border-[var(--client-primary)] focus:ring-4 focus:ring-[color-mix(in_srgb,var(--client-primary)_14%,transparent)]"
                    placeholder="Job title, keyword, skill, or facility"
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    onKeyDown={(event) => event.key === 'Enter' && handleSearch()}
                  />
                  <label className="sr-only" htmlFor="landing-location">City, region, or remote preference</label>
                  <input
                    id="landing-location"
                    className="h-16 rounded-[22px] border border-slate-100 bg-[var(--client-panel)] px-6 text-lg text-[var(--client-text)] outline-none transition focus:border-[var(--client-primary)] focus:ring-4 focus:ring-[color-mix(in_srgb,var(--client-primary)_14%,transparent)]"
                    placeholder="City or remote"
                    value={location}
                    onChange={(event) => setLocation(event.target.value)}
                    onKeyDown={(event) => event.key === 'Enter' && handleSearch()}
                  />
                  <button className="rounded-[22px] bg-[var(--client-primary)] px-9 text-lg font-black text-white shadow-[0_18px_35px_rgba(94,128,120,0.24)] transition hover:bg-[color-mix(in_srgb,var(--client-primary)_86%,#000)]" onClick={handleSearch}>
                    Search Jobs
                  </button>
                </div>
              </div>

              <div className="flex flex-wrap gap-3">
                <Link href={landing.primaryCallToActionUrl || '/jobs'} className="rounded-full border border-slate-200 bg-[var(--client-panel)] px-5 py-3 font-black text-[var(--client-secondary)] transition hover:border-[var(--client-primary)]">{landing.primaryCallToActionText || 'Find roles'}</Link>
                <Link href="/feed" className="rounded-full border border-slate-200 bg-[var(--client-panel)] px-5 py-3 font-black text-[var(--client-secondary)] transition hover:border-[var(--client-primary)]">Open Feed</Link>
                <Link href={landing.secondaryCallToActionUrl || '/register'} className="rounded-full bg-[var(--client-primary)] px-5 py-3 font-black text-white transition hover:bg-[color-mix(in_srgb,var(--client-primary)_86%,#000)]">{landing.secondaryCallToActionText || 'Join network'}</Link>
              </div>
            </div>
          </div>

          <aside className={`relative flex flex-col justify-center gap-5 ${landing.isHeroMediaVisible ? '' : 'xl:justify-center'}`}>
            <div className="pointer-events-none absolute right-4 top-2 hidden h-48 w-48 rounded-full border-[34px] border-solid xl:block" style={{ borderColor: 'color-mix(in srgb, var(--client-secondary) 10%, transparent)' }} />
            {landing.isHeroMediaVisible && (
            <div className={`relative overflow-hidden rounded-[38px] border border-[var(--client-border)] bg-gradient-to-br ${story.tint} p-7 shadow-[0_24px_70px_rgba(15,23,42,0.12)]`}>
              <div className="relative min-h-[430px] rounded-[30px] bg-[var(--client-panel)] p-5 shadow-[inset_0_1px_0_rgba(255,255,255,0.5)]">
                <div className="h-44 overflow-hidden rounded-[28px] border border-[var(--client-border)] bg-[var(--client-panel-soft)] shadow-inner">
                  {story.imageUrl && <img src={story.imageUrl} alt={story.imageAlt || story.title} className="h-full w-full object-cover" />}
                </div>

                <div className="relative -mt-8 rounded-[28px] border border-[var(--client-border)] bg-[color-mix(in_srgb,var(--client-panel)_94%,transparent)] px-6 pb-6 pt-16 shadow-xl backdrop-blur">
                  <div className="absolute -top-16 left-6 grid h-28 w-28 place-items-center rounded-full bg-[var(--client-primary)] text-5xl font-black text-white shadow-2xl ring-8 ring-[var(--client-panel)]">
                    m
                  </div>
                  <div className="pl-32">
                    <p className="text-xs font-black uppercase tracking-[0.22em] text-[var(--client-primary)]">{story.label || story.partner}</p>
                    <h2 className="mt-2 text-2xl font-black leading-tight tracking-[-0.04em] text-[var(--client-text)]">{story.title}</h2>
                  </div>

                  <div className="mt-6 rounded-[22px] border border-[var(--client-border)] bg-[var(--client-panel-soft)] p-5">
                    <p className="text-base italic leading-7 text-[var(--client-text)]">"{story.testimonial || story.quote}"</p>
                    <div className="mt-5 flex flex-wrap items-center justify-between gap-3">
                      <span className="text-xs font-black uppercase tracking-[0.18em] text-[var(--client-muted)]">{story.author || 'medicSocial partner'}</span>
                      <div className="flex gap-2">
                        {visibleStories.map((item: any, index: number) => (
                          <button
                            key={item.title}
                            type="button"
                            onClick={() => setActiveStory(index)}
                            className={`h-2.5 rounded-full transition-all ${activeStory === index ? 'w-9 bg-[var(--client-primary)]' : 'w-2.5 bg-slate-300'}`}
                            aria-label={`Show story ${index + 1}`}
                          />
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            )}

            <div className="grid grid-cols-3 gap-3">
              {[['Live roles', stats.liveJobs], ['Employers', stats.employers], ['Professionals', stats.professionals]].map(([label, value]) => (
                <div key={label} className="rounded-[24px] border border-[var(--client-border)] bg-[var(--client-panel)] p-5 text-center shadow-sm">
                  <p className="text-4xl font-black text-[var(--client-secondary)]">{value}</p>
                  <p className="mt-2 text-xs font-black uppercase tracking-[0.2em] text-[var(--client-primary)]">{label}</p>
                </div>
              ))}
            </div>
          </aside>
        </div>
      </section>

      <section className="mt-16">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2 className="text-5xl font-black tracking-[-0.055em] text-[var(--client-secondary)]">Featured Opportunities</h2>
            <p className="mt-3 text-xl text-slate-600">Verified roles from employers hiring through the platform.</p>
          </div>
          <Link href="/jobs" className="text-lg font-black text-[var(--client-primary)]">View all {stats.liveJobs || jobs.length}+ jobs</Link>
        </div>

        <div className="grid gap-7 lg:grid-cols-2">
          {featuredJobs.map((job) => {
            const poster = job.posters?.find((item) => item.contentType?.startsWith('image/'));
            const closingSoon = job.displayStatus === 'ClosingSoon';
            return (
              <article key={job.id} className="group overflow-hidden rounded-[28px] border border-slate-200 bg-white p-8 shadow-sm transition hover:-translate-y-1 hover:shadow-[0_24px_70px_rgba(15,23,42,0.12)]">
                <div className="flex items-start justify-between gap-4">
                  <div className="grid h-16 w-16 place-items-center overflow-hidden rounded-[22px] bg-slate-50">
                    {poster?.publicUrl ? (
                      <img src={poster.publicUrl} alt={`${job.title} poster`} className="h-full w-full object-cover" />
                    ) : (
                      <span className="h-8 w-8 rounded-lg bg-slate-200" />
                    )}
                  </div>
                  <span className={`rounded-lg px-3 py-2 text-sm font-black uppercase ${closingSoon ? 'bg-amber-50 text-amber-700' : 'bg-emerald-50 text-emerald-700'}`}>
                    {closingSoon ? 'Closing soon' : job.status || 'Open'}
                  </span>
                </div>

                <div className="mt-10">
                  <h3 className="text-3xl font-black tracking-[-0.04em] text-[var(--client-secondary)]">{job.title}</h3>
                  <p className="mt-3 text-xl text-slate-600">{job.department || 'Healthcare'} - {job.location || 'Location flexible'}</p>
                </div>

                <div className="mt-7 flex flex-wrap gap-3">
                  <span className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700">
                    {formatMoney(job.salaryMin)}{job.salaryMax ? ` - ${formatMoney(job.salaryMax)}` : ''}
                  </span>
                  {job.minimumYearsOfExperience != null && (
                    <span className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700">
                      {job.minimumYearsOfExperience}+ years
                    </span>
                  )}
                  {job.requiredProfessionalCategory && (
                    <span className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-700">
                      {job.requiredProfessionalCategory}
                    </span>
                  )}
                </div>

                <Link href={`/jobs/${job.id}`} className="mt-9 flex h-16 items-center justify-center rounded-[18px] border border-slate-200 text-lg font-black text-[var(--client-secondary)] transition group-hover:border-[var(--client-primary)] group-hover:text-[var(--client-primary)]">
                  View role
                </Link>
              </article>
            );
          })}
        </div>
      </section>

      <section className="mt-16 grid gap-7 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="rounded-[44px] bg-[var(--client-primary)] p-8 text-white shadow-[0_24px_70px_rgba(94,128,120,0.22)] sm:p-12">
          <h2 className="max-w-2xl text-5xl font-black leading-[1.04] tracking-[-0.06em]">
            Hiring for a medical facility? Find vetted talent with a clearer pipeline.
          </h2>
          <p className="mt-7 max-w-xl text-xl leading-9 text-white/86">
            Post openings, configure requirements, manage applicants, invite matching professionals, and keep communication inside the same workspace.
          </p>
          <div className="mt-9 flex flex-wrap gap-4">
            <Link href="/register" className="rounded-[20px] bg-white px-8 py-5 text-lg font-black text-[var(--client-primary)]">Post an opening</Link>
            <Link href="/feed" className="rounded-[20px] border border-white/30 px-8 py-5 text-lg font-black text-white">Explore Feed</Link>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {visibleFeatures.map((feature: any) => (
            <div key={feature.title} className="rounded-[28px] border border-[var(--client-border)] bg-[var(--client-panel)] p-7 shadow-sm">
              <div className="mb-7 grid h-12 w-12 place-items-center rounded-[18px] bg-[color-mix(in_srgb,var(--client-primary)_14%,var(--client-panel))] text-lg font-black text-[var(--client-primary)]">{feature.icon || feature.title?.slice(0, 1) || '+'}</div>
              <h3 className="text-2xl font-black tracking-[-0.04em] text-[var(--client-secondary)]">{feature.title}</h3>
              <p className="mt-3 leading-7 text-[var(--client-muted)]">{feature.body}</p>
            </div>
          ))}
        </div>
      </section>

      {secondaryJobs.length > 0 && (
        <section className="mt-16 rounded-[36px] border border-slate-200 bg-white p-8">
          <div className="flex flex-wrap items-end justify-between gap-4">
            <div>
              <h2 className="text-4xl font-black tracking-[-0.05em] text-[var(--client-secondary)]">Recently added</h2>
              <p className="mt-2 text-slate-600">A quick look at fresh marketplace activity.</p>
            </div>
            <Link href="/jobs" className="rounded-full bg-[var(--client-primary)] px-5 py-3 font-black text-white">Browse all roles</Link>
          </div>
          <div className="mt-6 grid gap-3">
            {secondaryJobs.map((job) => (
              <Link key={job.id} href={`/jobs/${job.id}`} className="flex flex-wrap items-center justify-between gap-4 rounded-[22px] border border-slate-200 px-5 py-5 transition hover:border-[var(--client-primary)]">
                <div>
                  <p className="text-lg font-black text-[var(--client-secondary)]">{job.title}</p>
                  <p className="text-slate-600">{job.department || 'Healthcare'} - {job.location || 'Location flexible'}</p>
                </div>
                <span className="font-black text-[var(--client-primary)]">View details</span>
              </Link>
            ))}
          </div>
        </section>
      )}
    </Layout>
  );
}
