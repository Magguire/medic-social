import Link from 'next/link';
import { useRouter } from 'next/router';
import { useState } from 'react';
import { useAuth } from '../lib/useAuth';

function getLandingPath(userType?: string) {
  if (userType === 'SuperAdmin' || userType === 'Admin') {
    return 'http://localhost:3001';
  }
  return '/dashboard';
}

export default function LoginPage() {
  const router = useRouter();
  const { login, isLoading } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    const result = await login(email, password);
    if (!result.success) {
      setError(result.error || 'Login failed.');
      return;
    }

    const next = typeof router.query.next === 'string' ? router.query.next : null;
    const fallback = getLandingPath(result.user?.userType);
    if (fallback.startsWith('http')) {
      window.location.href = fallback;
      return;
    }
    router.push(next || fallback);
  };

  return (
    <div className="mx-auto flex min-h-[calc(100vh-8rem)] max-w-6xl items-center justify-center px-4 py-10">
      <div className="grid w-full overflow-hidden rounded-[32px] border border-[var(--client-border)] bg-[var(--client-panel)] shadow-[0_35px_90px_rgba(15,23,42,0.12)] lg:grid-cols-[0.9fr_1.1fr]">
        <aside className="bg-[linear-gradient(140deg,var(--client-secondary),var(--client-primary))] px-8 py-10 text-white">
          <p className="text-sm font-semibold uppercase tracking-[0.35em] text-white/70">Account Access</p>
          <h1 className="mt-4 text-4xl font-black tracking-tight">Sign in to continue your healthcare hiring workflow.</h1>
          <p className="mt-4 text-white/82">Professionals return to track applications and verification. Employers continue hiring. Admins move into configuration, verification, and audit oversight.</p>
        </aside>

        <main className="px-8 py-10">
          <h2 className="text-3xl font-bold tracking-tight text-[var(--client-text)]">Welcome back</h2>
          <p className="mt-2 text-sm text-[var(--client-muted)]">Use the same platform account across the public client and the admin operations console.</p>
          {error && <div className="mt-5 rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}
          <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
            <div>
              <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Email</label>
              <input className="input-shell" type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="name@example.com" />
            </div>
            <div>
              <label className="mb-2 block text-sm font-semibold text-[var(--client-text)]">Password</label>
              <input className="input-shell" type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Enter your password" />
            </div>
            <button className="primary-action w-full" disabled={isLoading}>{isLoading ? 'Signing in...' : 'Sign in'}</button>
          </form>
          <p className="mt-6 text-sm text-slate-500">No account yet? <Link href="/register" className="font-semibold text-[var(--client-primary)]">Create one now</Link></p>
        </main>
      </div>
    </div>
  );
}
