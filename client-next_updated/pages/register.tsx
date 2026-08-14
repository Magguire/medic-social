import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import { useAuth } from '../lib/useAuth';
import { contentApi } from '../lib/contentApi';

type LegalSlug = 'privacy' | 'terms';

const defaultLegal = {
  privacy: { title: 'Privacy Policy', htmlContent: '<p>Privacy policy content is loading.</p>', cssContent: '' },
  terms: { title: 'Terms and Conditions', htmlContent: '<p>Terms and conditions content is loading.</p>', cssContent: '' },
};

export default function RegisterPage() {
  const router = useRouter();
  const { register, isLoading } = useAuth();
  const [accountType, setAccountType] = useState<'Professional' | 'Employer'>('Professional');
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', organizationName: '', businessPhoneNumber: '', password: '', confirmPassword: '' });
  const [agreements, setAgreements] = useState({ privacy: false, terms: false });
  const [legalPages, setLegalPages] = useState(defaultLegal);
  const [activeLegal, setActiveLegal] = useState<LegalSlug | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (router.query.type === 'employer') {
      setAccountType('Employer');
    }
  }, [router.query.type]);

  useEffect(() => {
    let mounted = true;
    Promise.all([
      contentApi.getPage('privacy').catch(() => defaultLegal.privacy),
      contentApi.getPage('terms').catch(() => defaultLegal.terms),
    ]).then(([privacy, terms]) => {
      if (!mounted) return;
      setLegalPages({
        privacy: { ...defaultLegal.privacy, ...(privacy || {}) },
        terms: { ...defaultLegal.terms, ...(terms || {}) },
      });
    });

    return () => {
      mounted = false;
    };
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (!agreements.privacy || !agreements.terms) {
      setError('You must accept the Privacy Policy and Terms and Conditions before creating an account.');
      return;
    }
    if (accountType === 'Employer' && (!form.organizationName.trim() || !form.businessPhoneNumber.trim())) {
      setError('Organization name and business phone number are required for employer accounts.');
      return;
    }
    const result = await register(form.email, form.password, form.firstName, form.lastName, accountType, form.organizationName, form.businessPhoneNumber, agreements.terms, agreements.privacy);
    if (!result.success) {
      setError(result.error || 'Registration failed.');
      return;
    }
    router.push('/dashboard');
  };

  return (
    <div className="mx-auto flex min-h-[calc(100vh-8rem)] max-w-6xl items-center justify-center py-4 sm:px-4 sm:py-10">
      <div className="grid w-full overflow-hidden rounded-[22px] border border-[var(--client-border)] bg-[var(--client-panel)] shadow-[0_35px_90px_rgba(15,23,42,0.12)] sm:rounded-[32px] lg:grid-cols-[0.95fr_1.05fr]">
        <aside className="bg-[linear-gradient(140deg,var(--client-secondary),var(--client-primary))] px-5 py-7 text-white sm:px-8 sm:py-10">
          <p className="text-sm font-semibold uppercase tracking-[0.35em] text-white/70">Registration</p>
          <h1 className="mt-4 text-4xl font-black tracking-tight">Create the account that matches your role in the platform.</h1>
          <div className="mt-6 space-y-4 text-sm text-white/82">
            <div className="rounded-2xl bg-white/10 p-4">Professionals can browse publicly, then register to complete biodata, qualifications, and verification for applications.</div>
            <div className="rounded-2xl bg-white/10 p-4">Employers create their account first, then onboard the facility, upload business records, and start posting jobs from the dashboard.</div>
          </div>
        </aside>

        <main className="px-5 py-7 sm:px-8 sm:py-10">
          <div className="flex gap-3 rounded-2xl bg-slate-100 p-2">
            {(['Professional', 'Employer'] as const).map((option) => (
              <button key={option} className={`flex-1 rounded-2xl px-4 py-3 text-sm font-semibold transition ${accountType === option ? 'bg-[var(--client-panel)] text-[var(--client-primary)] shadow-sm' : 'text-slate-500'}`} onClick={() => setAccountType(option)} type="button">
                {option}
              </button>
            ))}
          </div>
          {error && <div className="mt-5 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}
          <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">First name</label>
                <input className="input-shell" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} />
              </div>
              <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Last name</label>
                <input className="input-shell" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} />
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Email</label>
              <input className="input-shell" type="email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} required />
            </div>
            {accountType === 'Employer' && (
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Organization name</label>
                  <input className="input-shell" value={form.organizationName} onChange={(event) => setForm((current) => ({ ...current, organizationName: event.target.value }))} placeholder="Facility or organization name" required />
                </div>
                <div>
                  <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Business phone number</label>
                  <input className="input-shell" type="tel" value={form.businessPhoneNumber} onChange={(event) => setForm((current) => ({ ...current, businessPhoneNumber: event.target.value }))} placeholder="Include country or area code" required />
                </div>
              </div>
            )}
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Password</label>
                <input className="input-shell" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} />
              </div>
              <div>
                <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Confirm password</label>
                <input className="input-shell" type="password" value={form.confirmPassword} onChange={(event) => setForm((current) => ({ ...current, confirmPassword: event.target.value }))} />
              </div>
            </div>
            <div className="grid gap-3 rounded-[22px] border border-[var(--client-border)] bg-[color-mix(in_srgb,var(--client-primary)_7%,var(--client-panel))] p-4">
              <p className="text-sm font-black uppercase tracking-[0.16em] text-[var(--client-primary)]">Required agreements</p>
              <LegalAcceptanceCard
                checked={agreements.privacy}
                label="I accept the Privacy Policy"
                onChange={(value) => setAgreements((current) => ({ ...current, privacy: value }))}
                onPreview={() => setActiveLegal('privacy')}
                href="/privacy"
              />
              <LegalAcceptanceCard
                checked={agreements.terms}
                label="I accept the Terms and Conditions"
                onChange={(value) => setAgreements((current) => ({ ...current, terms: value }))}
                onPreview={() => setActiveLegal('terms')}
                href="/terms"
              />
            </div>
            <button className="primary-action w-full" disabled={isLoading}>{isLoading ? 'Creating account...' : `Create ${accountType.toLowerCase()} account`}</button>
          </form>
          <p className="mt-6 text-sm text-slate-500">Already registered? <Link href="/login" className="font-semibold text-[var(--client-primary)]">Sign in</Link></p>
        </main>
      </div>

      {activeLegal && (
        <div className="modal-backdrop">
          <section className="surface-card max-w-4xl p-5">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="text-xs font-black uppercase tracking-[0.18em] text-[var(--client-primary)]">Review before continuing</p>
                <h2 className="text-3xl font-black tracking-[-0.04em] text-[var(--client-text)]">{legalPages[activeLegal].title}</h2>
              </div>
              <button className="secondary-action" type="button" onClick={() => setActiveLegal(null)}>Close</button>
            </div>
            <div className="max-h-[58vh] overflow-auto rounded-[24px] border border-[var(--client-border)] bg-[var(--client-panel)] p-4">
              <style>{legalPages[activeLegal].cssContent}</style>
              <div dangerouslySetInnerHTML={{ __html: legalPages[activeLegal].htmlContent }} />
            </div>
            <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
              <button
                className="secondary-action"
                type="button"
                onClick={() => {
                  setAgreements((current) => ({ ...current, [activeLegal]: false }));
                  setActiveLegal(null);
                  setError(`${legalPages[activeLegal].title} was rejected. Registration cannot continue until it is accepted.`);
                }}
              >
                Reject
              </button>
              <button
                className="primary-action"
                type="button"
                onClick={() => {
                  setAgreements((current) => ({ ...current, [activeLegal]: true }));
                  setActiveLegal(null);
                  setError(null);
                }}
              >
                Accept and continue
              </button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}

function LegalAcceptanceCard({
  checked,
  label,
  onChange,
  onPreview,
  href,
}: {
  checked: boolean;
  label: string;
  onChange: (value: boolean) => void;
  onPreview: () => void;
  href: string;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[var(--client-border)] bg-[var(--client-panel)] p-3">
      <label className="flex min-w-0 flex-1 items-center gap-3 text-sm font-bold text-[var(--client-text)]">
        <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />
        <span>{label}</span>
      </label>
      <div className="flex items-center gap-2">
        <button className="secondary-action !px-3 !py-2 text-xs" type="button" onClick={onPreview}>Preview</button>
        <Link className="text-xs font-black text-[var(--client-primary)]" href={href} target="_blank">Open</Link>
      </div>
    </div>
  );
}
