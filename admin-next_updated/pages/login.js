import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { adminApi, getStoredUser } from '../lib/api';

export default function LoginPage() {
  const router = useRouter();
  const [form, setForm] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const token = localStorage.getItem('accessToken');
    const user = getStoredUser();
    if (token && user) {
      const next = typeof router.query.next === 'string' ? router.query.next : '/';
      router.replace(next);
    }
  }, [router.asPath, router.query.next]);

  const submit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError('');

    try {
      await adminApi.login(form);
      const next = typeof router.query.next === 'string' ? router.query.next : '/';
      router.push(next);
    } catch (requestError) {
      setError(requestError.message || 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-shell">
      <div className="login-hero">
        <p className="login-kicker">MedicSocial admin</p>
        <h1>Secure access for verification, policy, and audit work.</h1>
        <p>
          Sign in with an admin-capable account to review verification requests, manage platform rules,
          and inspect the audit trail without manual token entry.
        </p>
      </div>

      <form className="login-card" onSubmit={submit}>
        <div className="login-brand">
          <div className="admin-brand-mark">+</div>
          <div>
            <div className="login-brand-title">medicSocial</div>
            <div className="login-brand-copy">Operations console</div>
          </div>
        </div>

        <label className="login-field">
          <span>Email</span>
          <input
            className="input"
            type="email"
            autoComplete="email"
            value={form.email}
            onChange={(event) => setForm({ ...form, email: event.target.value })}
            placeholder="name@example.com"
            required
          />
        </label>

        <label className="login-field">
          <span>Password</span>
          <div className="password-field">
            <input
              className="input password-input"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
              placeholder="Enter your password"
              required
            />
            <button
              className="password-toggle"
              type="button"
              onClick={() => setShowPassword((current) => !current)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
              aria-pressed={showPassword}
            >
              {showPassword ? (
                <svg viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M3 4.5 19.5 21" />
                  <path d="M10.6 10.7a2 2 0 0 0 2.8 2.8" />
                  <path d="M9.4 5.5A10.8 10.8 0 0 1 12 5.2c5.2 0 9.1 3.2 10 6.8a10.7 10.7 0 0 1-4.1 5.5" />
                  <path d="M6.2 7.1A10.9 10.9 0 0 0 2 12c.7 2.7 3.2 5.2 6.4 6.3" />
                </svg>
              ) : (
                <svg viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M2 12c.9-3.6 4.8-6.8 10-6.8s9.1 3.2 10 6.8c-.9 3.6-4.8 6.8-10 6.8S2.9 15.6 2 12Z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              )}
            </button>
          </div>
        </label>

        {error && <div className="login-error">{error}</div>}

        <button className="btn-primary login-submit" disabled={loading} type="submit">
          {loading ? 'Signing in...' : 'Sign in'}
        </button>

        <p className="login-note">Need to rotate your password after sign-in? Open <strong>Settings</strong> in the admin console.</p>
      </form>
    </div>
  );
}
