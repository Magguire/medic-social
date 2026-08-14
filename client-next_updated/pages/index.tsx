import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { useAuth } from '../lib/useAuth';
import { contentApi } from '../lib/contentApi';
import { jobApi } from '../lib/jobApi';

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
  featureCards: [],
  employerCalloutTitle: 'Hiring for a medical facility? Find vetted talent with a clearer pipeline.',
  employerCalloutBody: 'Post openings, configure requirements, manage applicants, invite matching professionals, and keep communication inside the same workspace.',
  journeySectionTitle: 'One platform. Two clear paths.',
  journeySectionBody: 'Create a free account to connect with the people and opportunities that move healthcare forward.',
  professionalJourneyTitle: 'For healthcare professionals',
  professionalJourneyBody: 'Build one trusted profile, discover suitable roles, and connect directly with potential employers.',
  employerJourneyTitle: 'For employers',
  employerJourneyBody: 'Grow a searchable talent pool, publish opportunities, and reach healthcare professionals ready for their next role.',
  freeAccessTitle: 'Start free in three simple steps',
  freeAccessBody: 'Choose your account type, create your free account, then complete your profile and start connecting.',
};

export default function HomePage() {
  const router = useRouter();
  const { hydrated, isAuthenticated, bootstrapUser } = useAuth();
  const [stats, setStats] = useState({ liveJobs: 0, employers: 0, professionals: 0 });
  const [activeStory, setActiveStory] = useState(0);
  const [landing, setLanding] = useState(defaultLanding);

  const handleFindRoles = async () => {
    const destination = landing.primaryCallToActionUrl || '/jobs';
    const loginPath = `/login?next=${encodeURIComponent(destination)}`;
    if (!hydrated || !isAuthenticated) {
      await router.push(loginPath);
      return;
    }

    try {
      await bootstrapUser();
      await router.push(destination);
    } catch {
      await router.push(loginPath);
    }
  };
  useEffect(() => {
    jobApi.getMarketplaceMetrics()
      .then((metricResponse) => {
        setStats({
          liveJobs: metricResponse.liveJobs || 0,
          employers: metricResponse.employers || 0,
          professionals: metricResponse.professionals || 0,
        });
      })
      .catch(() => setStats({ liveJobs: 0, employers: 0, professionals: 0 }));
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
  const story = visibleStories[activeStory] || storyCards[0];

  return (
    <Layout>
      <section className="overflow-hidden rounded-[24px] border border-slate-200/80 bg-[var(--client-panel)] shadow-[0_28px_90px_rgba(15,23,42,0.08)] sm:rounded-[36px] xl:rounded-[44px]">
        <div className="grid gap-8 px-5 py-8 sm:px-8 lg:px-10 xl:grid-cols-[minmax(0,0.95fr)_minmax(420px,0.9fr)] xl:py-10">
          <div className="flex flex-col justify-between xl:min-h-[500px]">
            <div>
              <div className="inline-flex items-center gap-3 rounded-full bg-[color-mix(in_srgb,var(--client-primary)_12%,var(--client-panel))] px-4 py-2 text-sm font-black uppercase tracking-[0.14em] text-[var(--client-primary)]">
                <span className="grid h-5 w-5 place-items-center rounded-full bg-[var(--client-primary)]">
                  <span className="h-2.5 w-2.5 rounded-full bg-white" />
                </span>
                {stats.liveJobs} {landing.badgeText}
              </div>

              <h1 className="mt-6 max-w-6xl text-[2.75rem] font-black leading-[0.98] tracking-[-0.06em] text-[var(--client-text)] sm:mt-8 sm:text-[4.6rem] lg:text-[5.5rem] xl:text-[6.9rem]">
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
              <div className="flex flex-wrap gap-3">
                <button type="button" onClick={handleFindRoles} className="rounded-full border border-slate-200 bg-[var(--client-panel)] px-5 py-3 font-black text-[var(--client-secondary)] transition hover:border-[var(--client-primary)]">{landing.primaryCallToActionText || 'Find roles'}</button>
                <Link href="/feed" className="rounded-full border border-slate-200 bg-[var(--client-panel)] px-5 py-3 font-black text-[var(--client-secondary)] transition hover:border-[var(--client-primary)]">Open Feed</Link>
                <Link href={landing.secondaryCallToActionUrl || '/register'} className="rounded-full bg-[var(--client-primary)] px-5 py-3 font-black text-white transition hover:bg-[color-mix(in_srgb,var(--client-primary)_86%,#000)]">{landing.secondaryCallToActionText || 'Join network'}</Link>
              </div>
            </div>
          </div>

          <aside className={`relative flex flex-col justify-center gap-5 ${landing.isHeroMediaVisible ? '' : 'xl:justify-center'}`}>
            <div className="pointer-events-none absolute right-4 top-2 hidden h-48 w-48 rounded-full border-[34px] border-solid xl:block" style={{ borderColor: 'color-mix(in srgb, var(--client-secondary) 10%, transparent)' }} />
            {landing.isHeroMediaVisible && (
            <div className={`relative overflow-hidden rounded-[24px] border border-[var(--client-border)] bg-gradient-to-br ${story.tint} p-3 shadow-[0_24px_70px_rgba(15,23,42,0.12)] sm:rounded-[38px] sm:p-7`}>
              <div className="relative min-h-[390px] rounded-[22px] bg-[var(--client-panel)] p-3 shadow-[inset_0_1px_0_rgba(255,255,255,0.5)] sm:min-h-[430px] sm:rounded-[30px] sm:p-5">
                <div className="h-44 overflow-hidden rounded-[28px] border border-[var(--client-border)] bg-[var(--client-panel-soft)] shadow-inner">
                  {story.imageUrl && <img src={story.imageUrl} alt={story.imageAlt || story.title} className="h-full w-full object-cover" />}
                </div>

                <div className="relative -mt-8 rounded-[28px] border border-[var(--client-border)] bg-[color-mix(in_srgb,var(--client-panel)_94%,transparent)] px-6 pb-6 pt-16 shadow-xl backdrop-blur">
                  <div className="absolute -top-16 left-6 grid h-28 w-28 place-items-center rounded-full bg-[var(--client-primary)] text-5xl font-black text-white shadow-2xl ring-8 ring-[var(--client-panel)]">
                    m
                  </div>
                  <div className="pt-14 sm:pl-32 sm:pt-0">
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

            <div className="grid grid-cols-3 gap-2 sm:gap-3">
              {[['Live roles', stats.liveJobs], ['Employers', stats.employers], ['Professionals', stats.professionals]].map(([label, value]) => (
                <div key={label} className="min-w-0 rounded-[16px] border border-[var(--client-border)] bg-[var(--client-panel)] px-2 py-4 text-center shadow-sm sm:rounded-[24px] sm:p-5">
                  <p className="text-2xl font-black text-[var(--client-secondary)] sm:text-4xl">{value}</p>
                  <p className="mt-2 text-[0.58rem] font-black uppercase tracking-[0.1em] text-[var(--client-primary)] sm:text-xs sm:tracking-[0.2em]">{label}</p>
                </div>
              ))}
            </div>
          </aside>
        </div>
      </section>

      <section className="mx-auto mt-14 max-w-6xl text-center sm:mt-20">
        <p className="text-xs font-black uppercase tracking-[0.22em] text-[var(--client-primary)]">Built for healthcare hiring</p>
        <h2 className="mt-4 text-4xl font-black tracking-[-0.05em] text-[var(--client-secondary)] sm:text-5xl">{landing.journeySectionTitle}</h2>
        <p className="mx-auto mt-4 max-w-2xl text-lg leading-8 text-[var(--client-muted)]">{landing.journeySectionBody}</p>
      </section>

      <section className="mt-8 grid gap-5 lg:grid-cols-2">
        <article className="rounded-[28px] border border-[var(--client-border)] bg-[var(--client-panel)] p-6 shadow-[var(--client-shadow)] sm:p-8">
          <div className="grid h-12 w-12 place-items-center rounded-2xl bg-[color-mix(in_srgb,var(--client-primary)_14%,var(--client-panel))] font-black text-[var(--client-primary)]">P</div>
          <h3 className="mt-6 text-3xl font-black tracking-[-0.04em] text-[var(--client-secondary)]">{landing.professionalJourneyTitle}</h3>
          <p className="mt-3 max-w-xl leading-7 text-[var(--client-muted)]">{landing.professionalJourneyBody}</p>
          <ul className="mt-6 grid gap-3 text-sm font-bold text-[var(--client-text)]">
            <li>✓ Be visible to healthcare employers</li>
            <li>✓ Discover and apply for suitable opportunities</li>
            <li>✓ Keep your profile, experience, and documents together</li>
          </ul>
          <Link href="/register" className="primary-action mt-7">Create your free account</Link>
        </article>

        <article className="rounded-[28px] bg-[var(--client-primary)] p-6 text-white shadow-[0_24px_70px_rgba(94,128,120,0.22)] sm:p-8">
          <div className="grid h-12 w-12 place-items-center rounded-2xl bg-white/15 font-black text-white">E</div>
          <h3 className="mt-6 text-3xl font-black tracking-[-0.04em]">{landing.employerJourneyTitle}</h3>
          <p className="mt-3 max-w-xl leading-7 text-white/80">{landing.employerJourneyBody}</p>
          <ul className="mt-6 grid gap-3 text-sm font-bold text-white">
            <li>✓ Build and search a relevant healthcare talent pool</li>
            <li>✓ Publish roles and reach suitable professionals</li>
            <li>✓ Manage applicants and hiring conversations in one place</li>
          </ul>
          <Link href="/register?type=employer" className="secondary-action mt-7 border-white/30 bg-white text-[var(--client-primary)]">Create your free account</Link>
        </article>
      </section>

      <section className="mt-10 overflow-hidden rounded-[28px] border border-[var(--client-border)] bg-[var(--client-panel)] p-6 sm:p-8">
        <div className="grid gap-8 lg:grid-cols-[0.8fr_1.2fr] lg:items-center">
          <div>
            <p className="text-xs font-black uppercase tracking-[0.2em] text-[var(--client-primary)]">Free access</p>
            <h2 className="mt-3 text-3xl font-black tracking-[-0.04em] text-[var(--client-secondary)]">{landing.freeAccessTitle}</h2>
            <p className="mt-3 leading-7 text-[var(--client-muted)]">{landing.freeAccessBody}</p>
            <p className="mt-5 text-sm font-bold text-[var(--client-text)]">Already have an account? <Link href="/login" className="text-[var(--client-primary)]">Log in here</Link>.</p>
          </div>
          <ol className="grid gap-3 sm:grid-cols-3">
            {[
              ['1', 'Choose your path', 'Professional or employer'],
              ['2', 'Register free', 'Create your account'],
              ['3', 'Start connecting', 'Complete your profile'],
            ].map(([step, title, copy]) => (
              <li key={step} className="rounded-[22px] bg-[var(--client-panel-soft)] p-5">
                <span className="text-sm font-black text-[var(--client-primary)]">{step}</span>
                <strong className="mt-3 block text-[var(--client-text)]">{title}</strong>
                <small className="mt-1 block text-[var(--client-muted)]">{copy}</small>
              </li>
            ))}
          </ol>
        </div>
      </section>

    </Layout>
  );
}
