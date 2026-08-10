import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { authApi } from '../lib/authApi';
import { useAuth, useRequireAuth } from '../lib/useAuth';
import { employerApi } from '../lib/employerApi';
import { subscriptionApi } from '../lib/subscriptionApi';
import { useRouter } from 'next/router';

const defaultPreferences = {
  landingPage: '/dashboard',
  jobAlerts: true,
  inviteAlerts: true,
  verificationReminders: true,
  profileDiscoverable: true,
  showPhoneAfterShortlist: true,
  showEmailAfterApplication: true,
  saveJobSearches: true,
  compactDashboard: false,
};

export default function SettingsPage() {
  const router = useRouter();
  useRequireAuth();
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<'account' | 'billing' | 'preferences' | 'privacy' | 'security'>('account');
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [preferences, setPreferences] = useState(defaultPreferences);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [employer, setEmployer] = useState<any>(null);
  const [plans, setPlans] = useState<any[]>([]);
  const [subscription, setSubscription] = useState<any>(null);
  const [paymentMethods, setPaymentMethods] = useState<any[]>([]);
  const [checkout, setCheckout] = useState({ planId: '', provider: '', phoneNumber: '', email: '' });

  useEffect(() => {
    if (typeof window === 'undefined') return;
    const saved = window.localStorage.getItem('medsocial.client.preferences');
    if (saved) {
      try {
        setPreferences({ ...defaultPreferences, ...JSON.parse(saved) });
      } catch {
        setPreferences(defaultPreferences);
      }
    }
  }, []);

  useEffect(() => {
    if (router.query.tab === 'security' || user?.mustChangePassword) setActiveTab('security');
  }, [router.query.tab, user?.mustChangePassword]);

  useEffect(() => {
    if (user?.userType !== 'Employer') return;
    employerApi.getCurrent().then(async (profile) => {
      setEmployer(profile);
      const [availablePlans, currentSubscription, methods] = await Promise.all([
        subscriptionApi.plans(),
        subscriptionApi.current(profile.id),
        subscriptionApi.paymentMethods(),
      ]);
      setPlans(availablePlans);
      setSubscription(currentSubscription);
      setPaymentMethods(methods);
    }).catch(() => undefined);
  }, [user]);

  useEffect(() => {
    if (!router.isReady || typeof router.query.transactionId !== 'string' || user?.userType !== 'Employer') return;
    setBusy(true);
    subscriptionApi.confirmPayment(router.query.transactionId)
      .then((response) => {
        setMessage(response.message || 'Payment confirmed and subscription activated.');
        setActiveTab('billing');
        return employer ? subscriptionApi.current(employer.id) : null;
      })
      .then((value) => { if (value) setSubscription(value); })
      .catch((requestError: any) => setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to confirm payment.'))
      .finally(() => setBusy(false));
  }, [employer, router.isReady, router.query.transactionId, user]);

  const requestUpgrade = async () => {
    if (!employer || !checkout.planId) return;
    setBusy(true); setError(null); setMessage(null);
    try {
      const response = await subscriptionApi.checkout({
        employerId: employer.id,
        planId: checkout.planId,
        provider: checkout.provider === '' ? undefined : Number(checkout.provider),
        payerDetails: { phoneNumber: checkout.phoneNumber, email: checkout.email || user?.email || '' },
      });
      setMessage(response.message || 'Subscription request received.');
      if (response.redirectUrl) window.location.assign(response.redirectUrl);
      setSubscription(await subscriptionApi.current(employer.id));
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to initiate subscription payment.');
    } finally { setBusy(false); }
  };

  const persistPreferences = (nextPreferences: typeof defaultPreferences) => {
    setPreferences(nextPreferences);
    window.localStorage.setItem('medsocial.client.preferences', JSON.stringify(nextPreferences));
    setMessage('Client preferences saved on this device.');
    setTimeout(() => setMessage(null), 2600);
  };

  const updatePassword = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    setError(null);
    try {
      await authApi.changePassword(form.currentPassword, form.newPassword, form.confirmNewPassword);
      setMessage('Password updated successfully.');
      setForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to update password.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Layout>
      <div className="space-y-6">
        <div className="client-stepper">
          {[
            ['account', 'Account'],
            ...(user?.userType === 'Employer' ? [['billing', 'Subscription']] : []),
            ['preferences', 'Preferences'],
            ['privacy', 'Privacy'],
            ['security', 'Security'],
          ].map(([key, label]) => (
            <button key={key} type="button" className={activeTab === key ? 'active' : ''} onClick={() => setActiveTab(key as typeof activeTab)}>
              {label}
            </button>
          ))}
        </div>

        {message && <div className="rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{message}</div>}
        {user?.mustChangePassword && <div className="rounded-2xl bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-800">Your account was created with a temporary password. Set a new password before continuing.</div>}
        {error && <div className="rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}

        {activeTab === 'account' && (
          <div className="surface-card p-6">
            <h1 className="section-title">Account overview</h1>
            <p className="section-copy mt-2">Quick visibility into who you are signed in as and how the platform currently sees your account.</p>
            <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <div className="rounded-2xl bg-slate-50 px-4 py-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-500">Email</p><p className="mt-2 font-semibold text-slate-900">{user?.email}</p></div>
              <div className="rounded-2xl bg-slate-50 px-4 py-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-500">Account type</p><p className="mt-2 font-semibold text-slate-900">{user?.userType}</p></div>
              <div className="rounded-2xl bg-slate-50 px-4 py-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-500">Verification</p><p className="mt-2 font-semibold text-slate-900">{user?.verificationStatus || 'Pending'}</p></div>
              <div className="rounded-2xl bg-slate-50 px-4 py-4"><p className="text-xs uppercase tracking-[0.2em] text-slate-500">Subscription</p><p className="mt-2 font-semibold text-slate-900">{user?.subscriptionTier || 'Free'}</p></div>
            </div>

            <details className="mt-6 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Session controls</summary>
              <div className="mt-4 flex flex-wrap gap-3">
                <button type="button" className="primary-action" onClick={() => logout()}>Logout this device</button>
                <button type="button" className="secondary-action" onClick={() => { window.localStorage.removeItem('medsocial.client.preferences'); setPreferences(defaultPreferences); setMessage('Saved client preferences cleared.'); }}>Reset local preferences</button>
              </div>
            </details>
          </div>
        )}

        {activeTab === 'preferences' && (
          <div className="surface-card p-6">
            <h2 className="section-title">Client preferences</h2>
            <p className="section-copy mt-2">Choose how the client workspace behaves for you on this device.</p>
            <div className="mt-6 grid gap-4 md:grid-cols-2">
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Default landing page</span>
                <select className="input-shell" value={preferences.landingPage} onChange={(event) => persistPreferences({ ...preferences, landingPage: event.target.value })}>
                  <option value="/dashboard">Dashboard</option>
                  <option value="/jobs">Jobs</option>
                  <option value="/applications">Applications</option>
                  <option value="/professional/profile">Profile</option>
                </select>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.compactDashboard} onChange={(event) => persistPreferences({ ...preferences, compactDashboard: event.target.checked })} />
                <span>Use compact dashboard cards</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.jobAlerts} onChange={(event) => persistPreferences({ ...preferences, jobAlerts: event.target.checked })} />
                <span>Notify me about matching jobs</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.inviteAlerts} onChange={(event) => persistPreferences({ ...preferences, inviteAlerts: event.target.checked })} />
                <span>Notify me when employers send invites</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.verificationReminders} onChange={(event) => persistPreferences({ ...preferences, verificationReminders: event.target.checked })} />
                <span>Remind me about pending verification tasks</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.saveJobSearches} onChange={(event) => persistPreferences({ ...preferences, saveJobSearches: event.target.checked })} />
                <span>Remember recent job searches on this device</span>
              </label>
            </div>
          </div>
        )}

        {activeTab === 'billing' && user?.userType === 'Employer' && (
          <div className="space-y-6">
            <div className="surface-card p-6">
              <h2 className="section-title">Subscription and usage</h2>
              <p className="section-copy mt-2">Your access is evaluated against the active billing period and its configured entitlements.</p>
              <div className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                <div className="rounded-2xl bg-slate-50 p-4"><span className="text-xs font-bold uppercase tracking-wider text-slate-500">Current plan</span><strong className="mt-2 block text-xl">{subscription?.plan?.name || user.subscriptionTier}</strong></div>
                <div className="rounded-2xl bg-slate-50 p-4"><span className="text-xs font-bold uppercase tracking-wider text-slate-500">Status</span><strong className="mt-2 block text-xl">{subscription?.subscription?.status || (subscription?.isLegacyFallback ? 'Legacy access' : 'Pending')}</strong></div>
                <div className="rounded-2xl bg-slate-50 p-4"><span className="text-xs font-bold uppercase tracking-wider text-slate-500">Published jobs</span><strong className="mt-2 block text-xl">{subscription?.usages?.find((item: any) => item.metricKey === 'jobs-published')?.quantity || 0} / {subscription?.plan?.maxPublishedJobs ?? 0}</strong></div>
                <div className="rounded-2xl bg-slate-50 p-4"><span className="text-xs font-bold uppercase tracking-wider text-slate-500">Team members</span><strong className="mt-2 block text-xl">Up to {subscription?.plan?.maxTeamMembers ?? 1}</strong></div>
              </div>
            </div>

            <div className="surface-card p-6">
              <h2 className="section-title">Choose a plan</h2>
              <div className="mt-5 grid gap-4 lg:grid-cols-3">
                {plans.map((plan) => <button key={plan.id} type="button" onClick={() => setCheckout((current) => ({ ...current, planId: plan.id }))} className={`rounded-3xl border p-5 text-left ${checkout.planId === plan.id ? 'border-[var(--client-primary)] bg-[color-mix(in_srgb,var(--client-primary)_12%,var(--client-panel))]' : 'border-slate-200 bg-white'}`}>
                  <strong className="text-xl">{plan.name}</strong><span className="mt-2 block text-2xl font-black">{plan.currency} {Number(plan.priceAmount).toFixed(2)}</span><span className="text-sm text-slate-500">per {plan.billingInterval}</span>
                  <p className="mt-3 text-sm text-slate-600">{plan.description}</p>
                  <div className="mt-4 text-sm text-slate-500">{plan.maxPublishedJobs} published jobs · {plan.maxTeamMembers} team users · {plan.maxCandidateInvitesPerPeriod} invites</div>
                </button>)}
              </div>

              {checkout.planId && <div className="mt-6 rounded-3xl border border-slate-200 p-5">
                <div className="grid gap-4 md:grid-cols-2">
                  <label><span className="mb-1.5 block text-sm font-semibold">Payment method</span><select className="input-shell" value={checkout.provider} onChange={(event) => setCheckout((current) => ({ ...current, provider: event.target.value }))}><option value="">Administrator review if no method is configured</option>{paymentMethods.map((method) => <option key={method.provider} value={method.provider}>{method.displayName}</option>)}</select></label>
                  {checkout.provider === '0' && <label><span className="mb-1.5 block text-sm font-semibold">M-Pesa phone number</span><input className="input-shell" value={checkout.phoneNumber} onChange={(event) => setCheckout((current) => ({ ...current, phoneNumber: event.target.value }))} /></label>}
                  {checkout.provider === '1' && <label><span className="mb-1.5 block text-sm font-semibold">PayPal email</span><input className="input-shell" type="email" value={checkout.email} onChange={(event) => setCheckout((current) => ({ ...current, email: event.target.value }))} /></label>}
                </div>
                <button type="button" className="primary-action mt-4" disabled={busy} onClick={requestUpgrade}>{busy ? 'Starting payment...' : 'Continue with upgrade'}</button>
              </div>}
            </div>

            <div className="surface-card p-6"><h2 className="section-title">Payment and request history</h2><div className="mt-4 space-y-3">{(subscription?.payments || []).map((payment: any) => <div key={payment.id} className="rounded-2xl bg-slate-50 p-4"><div className="flex justify-between gap-3"><strong>{payment.currency} {Number(payment.amount).toFixed(2)}</strong><span className="pill-chip">{payment.status}</span></div><p className="mt-1 text-sm text-slate-500">{new Date(payment.createdAt).toLocaleString()}</p></div>)}</div></div>
          </div>
        )}

        {activeTab === 'privacy' && (
          <div className="surface-card p-6">
            <h2 className="section-title">Visibility and privacy</h2>
            <p className="section-copy mt-2">Control how much of your profile should be exposed during hiring workflows on this device until server-side syncing is added.</p>
            <div className="mt-6 grid gap-4 md:grid-cols-2">
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.profileDiscoverable} onChange={(event) => persistPreferences({ ...preferences, profileDiscoverable: event.target.checked })} />
                <span>Allow employers to discover my profile in search</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.showPhoneAfterShortlist} onChange={(event) => persistPreferences({ ...preferences, showPhoneAfterShortlist: event.target.checked })} />
                <span>Show my phone number after shortlist</span>
              </label>
              <label className="client-switch flex items-center gap-3 px-4 py-4">
                <input type="checkbox" checked={preferences.showEmailAfterApplication} onChange={(event) => persistPreferences({ ...preferences, showEmailAfterApplication: event.target.checked })} />
                <span>Show my email once I submit an application</span>
              </label>
            </div>
          </div>
        )}

        {activeTab === 'security' && (
          <div className="surface-card p-6">
            <h2 className="section-title">Security</h2>
            <p className="section-copy mt-2">Update your password and keep your account protected.</p>
            <form className="mt-6 grid max-w-2xl gap-4" onSubmit={updatePassword}>
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Current password</span>
                <input className="input-shell" type="password" value={form.currentPassword} onChange={(event) => setForm((current) => ({ ...current, currentPassword: event.target.value }))} />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">New password</span>
                <input className="input-shell" type="password" value={form.newPassword} onChange={(event) => setForm((current) => ({ ...current, newPassword: event.target.value }))} />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Confirm new password</span>
                <input className="input-shell" type="password" value={form.confirmNewPassword} onChange={(event) => setForm((current) => ({ ...current, confirmNewPassword: event.target.value }))} />
              </label>
              <button className="primary-action" type="submit" disabled={busy}>{busy ? 'Updating...' : 'Update password'}</button>
            </form>
          </div>
        )}
      </div>
    </Layout>
  );
}
