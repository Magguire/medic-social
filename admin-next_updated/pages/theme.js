import { useEffect, useState } from 'react';
import Link from 'next/link';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

const defaultTheme = {
  primaryColor: '#607f75',
  secondaryColor: '#111827',
  accentColor: '#b66a3c',
  backgroundColor: '#fbf7ef',
  surfaceColor: '#ffffff',
  textColor: '#111827',
  mutedTextColor: '#667085',
  darkBackgroundColor: '#111820',
  darkSurfaceColor: '#1d2a31',
  darkTextColor: '#f7f2ea',
  darkMutedTextColor: '#c1c8c4',
  isPublished: true,
};

const fields = [
  ['primaryColor', 'Primary action color', 'brand'],
  ['secondaryColor', 'Secondary/navigation color', 'brand'],
  ['accentColor', 'Accent and alert color', 'brand'],
  ['backgroundColor', 'Light background', 'light'],
  ['surfaceColor', 'Light cards and panels', 'light'],
  ['textColor', 'Light text', 'light'],
  ['mutedTextColor', 'Light muted text', 'light'],
  ['darkBackgroundColor', 'Dark background', 'dark'],
  ['darkSurfaceColor', 'Dark cards and panels', 'dark'],
  ['darkTextColor', 'Dark text', 'dark'],
  ['darkMutedTextColor', 'Dark muted text', 'dark'],
];

const previewPages = [
  { key: 'landing', label: 'Landing' },
  { key: 'jobs', label: 'Jobs' },
  { key: 'register', label: 'Register' },
  { key: 'feed', label: 'Feed' },
];

function PalettePreview({ theme, dark = false, page = 'landing', onPageChange }) {
  const style = {
    '--preview-primary': theme.primaryColor,
    '--preview-secondary': theme.secondaryColor,
    '--preview-accent': theme.accentColor,
    '--preview-bg': dark ? theme.darkBackgroundColor : theme.backgroundColor,
    '--preview-surface': dark ? theme.darkSurfaceColor : theme.surfaceColor,
    '--preview-text': dark ? theme.darkTextColor : theme.textColor,
    '--preview-muted': dark ? theme.darkMutedTextColor : theme.mutedTextColor,
  };

  const pageCopy = {
    landing: ['The specialized home for healthcare careers.', 'Search, public stats, story cards, and employer callouts.'],
    jobs: ['Open healthcare opportunities.', 'Filter roles by category, location, department, and readiness.'],
    register: ['Create the account that matches your role.', 'Professional and employer registration must feel trustworthy.'],
    feed: ['Connect around hiring and practice.', 'Posts, comments, channels, and messages need strong contrast.'],
  }[page] || ['Client preview', 'Palette-controlled screens.'];

  return (
    <div className="palette-preview" style={style}>
      <nav className="preview-page-tabs">
        {previewPages.map((item) => (
          <button key={item.key} type="button" className={page === item.key ? 'active' : ''} onClick={() => onPageChange(item.key)}>{item.label}</button>
        ))}
      </nav>
      <header>
        <div className="preview-logo">m</div>
        <div>
          <strong>medicSocial</strong>
          <span>{dark ? 'Dark palette' : 'Light palette'} - {previewPages.find((item) => item.key === page)?.label}</span>
        </div>
        <div className="preview-nav-swatch">
          <span>Secondary/nav</span>
          <strong>{theme.secondaryColor}</strong>
        </div>
      </header>
      <h3>{pageCopy[0]}</h3>
      <p>{pageCopy[1]}</p>
      <div className="preview-search">
        <span>{page === 'register' ? 'name@example.com' : page === 'feed' ? 'Share an update with the network' : 'Job title, keyword, skill, or facility'}</span>
        <button>{page === 'register' ? 'Join' : page === 'feed' ? 'Post' : 'Search Jobs'}</button>
      </div>
      <div className="preview-cards">
        <article><strong>{page === 'jobs' ? '9' : '12'}</strong><span>{page === 'feed' ? 'Posts' : 'Live roles'}</span></article>
        <article><strong>{page === 'register' ? '2' : '8'}</strong><span>{page === 'register' ? 'Account types' : 'Employers'}</span></article>
        <article><strong>{page === 'feed' ? '4' : '42'}</strong><span>{page === 'feed' ? 'Channels' : 'Professionals'}</span></article>
      </div>
    </div>
  );
}

export default function ClientThemePage() {
  const [user, setUser] = useState(null);
  const [form, setForm] = useState(defaultTheme);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [previewPage, setPreviewPage] = useState('landing');

  useEffect(() => {
    Promise.all([adminApi.getCurrentUser(), adminApi.getClientTheme()])
      .then(([currentUser, theme]) => {
        setUser(currentUser);
        setForm({ ...defaultTheme, ...(theme || {}) });
      })
      .catch((requestError) => setError(requestError.message || 'Unable to load client theme.'));
  }, []);

  const update = (key, value) => setForm((current) => ({ ...current, [key]: value }));

  const save = async (event) => {
    event.preventDefault();
    setError('');
    setMessage('');
    try {
      const saved = await adminApi.saveClientTheme(form);
      setForm({ ...defaultTheme, ...saved });
      setMessage('Client theme saved.');
      setTimeout(() => setMessage(''), 3000);
    } catch (requestError) {
      setError(requestError.message || 'Unable to save client theme.');
      setTimeout(() => setError(''), 4500);
    }
  };

  return (
    <AdminShell user={user} title="Client Theme" subtitle="Design the colors used by the client landing page, registration, dashboards, profile screens, and light/dark modes.">
      {(message || error) && <div className="toast-stack"><div className={`toast ${error ? 'error' : 'success'}`}>{error || message}</div></div>}
      <Link href="/settings?tab=content" className="btn-secondary back-link">Back to content settings</Link>

      <form className="theme-shell" onSubmit={save}>
        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Palette designer</p>
              <h2>Client brand tokens</h2>
              <p>These values are loaded by the client UI at runtime. Unpublished palettes keep the current public theme active.</p>
            </div>
            <label className="switch-card compact"><input type="checkbox" checked={form.isPublished} onChange={(event) => update('isPublished', event.target.checked)} /> Published</label>
          </div>

          {[
            ['brand', 'Brand colors', 'Shared across light and dark mode'],
            ['light', 'Light mode palette', 'Background, cards, and text for public daylight screens'],
            ['dark', 'Dark mode palette', 'Background, cards, and text for low-light screens'],
          ].map(([group, title, helper]) => (
            <section key={group} className="palette-section">
              <div>
                <h3>{title}</h3>
                <p>{helper}</p>
              </div>
              <div className="palette-grid">
                {fields.filter(([, , fieldGroup]) => fieldGroup === group).map(([key, label]) => (
                  <label key={key} className="color-field">
                    <span>{label}</span>
                    <div>
                      <input type="color" value={form[key]} onChange={(event) => update(key, event.target.value)} />
                      <input className="input" value={form[key]} onChange={(event) => update(key, event.target.value)} />
                    </div>
                  </label>
                ))}
              </div>
            </section>
          ))}

          <div className="button-row sticky-actions">
            <button type="button" className="btn-secondary" onClick={() => setForm(defaultTheme)}>Reset to default</button>
            <button type="submit" className="btn-primary">Save client theme</button>
          </div>
        </section>

        <section className="preview-stack">
          <PalettePreview theme={form} page={previewPage} onPageChange={setPreviewPage} />
          <PalettePreview theme={form} dark page={previewPage} onPageChange={setPreviewPage} />
        </section>
      </form>

      <style jsx global>{`
        .theme-shell {
          display: grid;
          grid-template-columns: minmax(340px, 0.85fr) minmax(0, 1.15fr);
          gap: 22px;
          margin-top: 22px;
          align-items: start;
        }

        .admin-card {
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 28px;
          background: rgba(255, 255, 255, 0.92);
          padding: 24px;
          box-shadow: 0 20px 55px rgba(15, 23, 42, 0.08);
        }

        .section-heading {
          display: flex;
          align-items: flex-start;
          justify-content: space-between;
          gap: 20px;
          border-bottom: 1px solid rgba(148, 163, 184, 0.2);
          margin-bottom: 20px;
          padding-bottom: 18px;
        }

        .section-heading h2 {
          margin: 4px 0;
          color: #07122b;
          font-size: 28px;
          font-weight: 900;
          letter-spacing: -0.04em;
        }

        .section-heading p {
          margin: 0;
          color: #64748b;
        }

        .eyebrow {
          color: #5e8078 !important;
          font-size: 12px;
          font-weight: 900;
          letter-spacing: 0.2em;
          text-transform: uppercase;
        }

        .palette-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 14px;
        }

        .palette-section {
          display: grid;
          gap: 14px;
          border: 1px solid rgba(148, 163, 184, 0.26);
          border-radius: 24px;
          background: linear-gradient(145deg, rgba(255,255,255,0.98), rgba(248,250,252,0.92));
          padding: 18px;
          margin-top: 16px;
        }

        .palette-section h3 {
          margin: 0;
          color: #07122b;
          font-size: 20px;
          font-weight: 950;
          letter-spacing: -0.04em;
        }

        .palette-section p {
          margin: 4px 0 0;
          color: #64748b;
          font-size: 13px;
          font-weight: 700;
        }

        .color-field {
          display: grid;
          gap: 8px;
          color: #334155;
          font-weight: 900;
        }

        .color-field div {
          display: grid;
          grid-template-columns: 56px minmax(0, 1fr);
          gap: 10px;
          align-items: center;
        }

        .color-field input[type='color'] {
          width: 56px;
          height: 48px;
          border: 1px solid rgba(148, 163, 184, 0.34);
          border-radius: 16px;
          background: transparent;
          padding: 4px;
        }

        .preview-stack {
          display: grid;
          gap: 18px;
        }

        .palette-preview {
          position: relative;
          overflow: hidden;
          border: 1px solid rgba(148, 163, 184, 0.24);
          border-radius: 34px;
          background:
            radial-gradient(circle at 12% 14%, color-mix(in srgb, var(--preview-accent) 18%, transparent), transparent 28%),
            radial-gradient(circle at 86% 10%, color-mix(in srgb, var(--preview-primary) 18%, transparent), transparent 28%),
            var(--preview-bg);
          color: var(--preview-text);
          padding: clamp(22px, 4vw, 40px);
          box-shadow: 0 24px 70px rgba(15, 23, 42, 0.11);
        }

        .preview-page-tabs {
          position: relative;
          z-index: 2;
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-bottom: 16px;
        }

        .preview-page-tabs button {
          border: 1px solid color-mix(in srgb, var(--preview-muted) 22%, transparent);
          border-radius: 999px;
          background: color-mix(in srgb, var(--preview-surface) 76%, transparent);
          color: var(--preview-muted);
          padding: 8px 12px;
          font-size: 12px;
          font-weight: 900;
          cursor: pointer;
        }

        .preview-page-tabs button.active {
          background: var(--preview-primary);
          color: #fff;
          border-color: var(--preview-primary);
        }

        .palette-preview::after {
          content: "";
          position: absolute;
          right: -80px;
          top: 34px;
          width: 260px;
          height: 260px;
          border-radius: 50%;
          border: 44px solid color-mix(in srgb, var(--preview-primary) 12%, transparent);
          pointer-events: none;
        }

        .palette-preview header {
          position: relative;
          z-index: 1;
          display: flex;
          align-items: center;
          gap: 14px;
          border: 1px solid color-mix(in srgb, var(--preview-muted) 18%, transparent);
          border-radius: 24px;
          background: linear-gradient(135deg, color-mix(in srgb, var(--preview-secondary) 88%, transparent), color-mix(in srgb, var(--preview-surface) 92%, transparent));
          padding: 12px;
          backdrop-filter: blur(12px);
        }

        .preview-logo {
          display: grid;
          width: 52px;
          height: 52px;
          place-items: center;
          border-radius: 18px;
          background: var(--preview-primary);
          color: #fff;
          font-size: 24px;
          font-weight: 950;
        }

        .palette-preview header > div:not(.preview-logo):not(.preview-nav-swatch) strong {
          color: #fff;
        }

        .palette-preview header > div:not(.preview-logo):not(.preview-nav-swatch) span {
          color: color-mix(in srgb, #fff 74%, var(--preview-muted));
        }

        .preview-nav-swatch {
          margin-left: auto;
          border: 1px solid color-mix(in srgb, var(--preview-primary) 36%, transparent);
          border-radius: 18px;
          background: color-mix(in srgb, var(--preview-secondary) 90%, transparent);
          color: #fff;
          padding: 10px 12px;
          text-align: right;
          box-shadow: inset 0 0 0 1px color-mix(in srgb, #fff 12%, transparent);
        }

        .preview-nav-swatch span,
        .preview-nav-swatch strong {
          color: #fff;
        }

        .preview-nav-swatch span {
          font-size: 10px;
          font-weight: 900;
          letter-spacing: 0.14em;
          text-transform: uppercase;
          opacity: 0.76;
        }

        .palette-preview strong,
        .palette-preview span {
          display: block;
        }

        .palette-preview span,
        .palette-preview p {
          color: var(--preview-muted);
        }

        .palette-preview h3 {
          position: relative;
          z-index: 1;
          max-width: 620px;
          margin: 34px 0 14px;
          font-size: clamp(2.3rem, 5vw, 4.8rem);
          line-height: 0.95;
          letter-spacing: -0.07em;
        }

        .palette-preview p {
          position: relative;
          z-index: 1;
          max-width: 620px;
          line-height: 1.7;
        }

        .preview-search {
          position: relative;
          z-index: 1;
          display: grid;
          grid-template-columns: minmax(0, 1fr) auto;
          gap: 12px;
          margin-top: 26px;
          border-radius: 26px;
          background: var(--preview-surface);
          padding: 12px;
          box-shadow: 0 18px 45px rgba(15, 23, 42, 0.12);
        }

        .preview-search span {
          display: flex;
          align-items: center;
          min-height: 54px;
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 18px;
          padding: 0 16px;
          color: var(--preview-muted);
        }

        .preview-search button {
          border: 0;
          border-radius: 18px;
          background: var(--preview-primary);
          color: #fff;
          padding: 0 24px;
          font-weight: 900;
        }

        .preview-cards {
          position: relative;
          z-index: 1;
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 12px;
          margin-top: 18px;
        }

        .preview-cards article {
          border: 1px solid color-mix(in srgb, var(--preview-muted) 18%, transparent);
          border-radius: 22px;
          background: color-mix(in srgb, var(--preview-surface) 88%, transparent);
          padding: 18px;
          text-align: center;
          box-shadow: 0 14px 30px rgba(15, 23, 42, 0.08);
        }

        .preview-cards article strong {
          color: var(--preview-primary);
          font-size: 32px;
        }

        .sticky-actions {
          margin-top: 18px;
          justify-content: flex-end;
        }

        @media (max-width: 1080px) {
          .theme-shell,
          .section-heading {
            grid-template-columns: 1fr;
          }

          .theme-shell,
          .section-heading {
            display: grid;
          }
        }

        @media (max-width: 700px) {
          .palette-grid,
          .preview-search,
          .preview-cards {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </AdminShell>
  );
}
