import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useMemo, useState } from 'react';
import { useAdminAuth } from '../lib/useAdminAuth';
import { adminApi, getStoredUser } from '../lib/api';

const navItems = [
  ['/', 'Dashboard', '⌂'],
  ['/jobs', 'Job Posts', '+'],
  ['/social', 'Social', '#'],
  ['/professionals', 'Professionals', '●'],
  ['/employers', 'Employers', '✚'],
  ['/verification', 'Verifications', '✓'],
  ['/settings', 'Settings', '⚙'],
  ['/reports', 'Reports', '▤'],
];

function getCrumbs(pathname, title) {
  if (pathname === '/') {
    return ['Admin', 'Dashboard'];
  }

  const segments = pathname.split('/').filter(Boolean);
  return ['Admin', ...segments.map((segment) => segment.replace(/-/g, ' ').replace(/\b\w/g, (value) => value.toUpperCase())), title].filter((item, index, list) => item && list.indexOf(item) === index);
}

export default function AdminShell({ user, title, subtitle, children }) {
  const router = useRouter();
  const auth = useAdminAuth();
  const effectiveUser = user || auth.user || getStoredUser();
  const [theme, setTheme] = useState('light');
  const [menuOpen, setMenuOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);

  useEffect(() => {
    const storedTheme = localStorage.getItem('medsocial.admin.theme') || 'light';
    const storedCollapsed = localStorage.getItem('medsocial.admin.sidebarCollapsed') === 'true';
    const storedAccent = localStorage.getItem('medsocial.admin.accent') || '#8b004a';
    setTheme(storedTheme);
    setCollapsed(storedCollapsed);
    document.documentElement.dataset.theme = storedTheme;
    document.documentElement.style.setProperty('--accent', storedAccent);
  }, []);

  useEffect(() => {
    if (!effectiveUser) return undefined;

    const idleMs = 20 * 60 * 1000;
    let idleTimer;

    const sendHeartbeat = () => {
      adminApi.heartbeat().catch(() => undefined);
    };

    const expireSession = () => {
      localStorage.setItem('medsocial.admin.idleLogoutAt', new Date().toISOString());
      auth.logout();
    };

    const resetIdleTimer = () => {
      if (idleTimer) {
        window.clearTimeout(idleTimer);
      }
      idleTimer = window.setTimeout(expireSession, idleMs);
    };

    const activityEvents = ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'];
    activityEvents.forEach((eventName) => window.addEventListener(eventName, resetIdleTimer));
    resetIdleTimer();
    sendHeartbeat();
    const heartbeatTimer = window.setInterval(() => {
      if (document.visibilityState === 'visible') {
        sendHeartbeat();
      }
    }, 60 * 1000);

    return () => {
      activityEvents.forEach((eventName) => window.removeEventListener(eventName, resetIdleTimer));
      if (idleTimer) {
        window.clearTimeout(idleTimer);
      }
      window.clearInterval(heartbeatTimer);
    };
  }, [auth, effectiveUser?.email]);

  const toggleTheme = () => {
    const nextTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(nextTheme);
    localStorage.setItem('medsocial.admin.theme', nextTheme);
    document.documentElement.dataset.theme = nextTheme;
    window.dispatchEvent(new Event('medsocial-theme-change'));
  };

  const toggleCollapsed = () => {
    setCollapsed((current) => {
      const next = !current;
      localStorage.setItem('medsocial.admin.sidebarCollapsed', String(next));
      return next;
    });
  };

  const displayEmail = effectiveUser?.email || 'Signed-in administrator';
  const displayRole = effectiveUser?.userType || 'Admin';
  const crumbs = useMemo(() => getCrumbs(router.pathname, title), [router.pathname, title]);

  return (
    <div className={`admin-shell ${collapsed ? 'sidebar-collapsed' : ''}`}>
      <aside className={`admin-sidebar ${menuOpen ? 'open' : ''}`}>
        <div className="admin-brand">
          <div className="admin-brand-mark">+</div>
          <div className="sidebar-label">
            <div style={{ fontSize: 24, fontWeight: 900, letterSpacing: '-0.04em' }}>medicSocial</div>
            <div style={{ fontSize: 12, opacity: 0.72, letterSpacing: '0.24em', textTransform: 'uppercase' }}>Admin console</div>
          </div>
        </div>

        <div className="sidebar-user-card">
          <div className="sidebar-avatar">{displayEmail.slice(0, 1).toUpperCase()}</div>
          <div className="sidebar-label">
            <strong>{displayEmail}</strong>
            <span>{displayRole} access</span>
          </div>
        </div>

        <button className="sidebar-collapse" onClick={toggleCollapsed}>{collapsed ? 'Expand' : 'Collapse'} sidebar</button>

        <nav className="admin-nav">
          {navItems.map(([href, label, icon]) => {
            const active = router.pathname === href;
            return (
              <Link key={href} href={href} title={label} className={active ? 'active' : ''} onClick={() => setMenuOpen(false)}>
                <span className="admin-nav-icon">{icon}</span>
                <span className="nav-label">{label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>

      {menuOpen && <button className="drawer-scrim" aria-label="Close menu" onClick={() => setMenuOpen(false)} />}

      <main className="admin-main">
        <div className="admin-topbar">
          <button className="btn-secondary admin-menu-button" onClick={() => setMenuOpen((current) => !current)}>☰</button>
          <div className="topbar-title">
            <nav className="breadcrumbs" aria-label="Breadcrumb">
              {crumbs.map((crumb, index) => <span key={`${crumb}-${index}`}>{crumb}</span>)}
            </nav>
            <strong>{title}</strong>
          </div>
          <div className="admin-session account-menu-wrap">
            <button className="account-trigger" onClick={() => setAccountOpen((current) => !current)}>
              <span>{displayEmail.slice(0, 1).toUpperCase()}</span>
              <div>
                <strong>{displayEmail}</strong>
                <small>{displayRole} access</small>
              </div>
            </button>
            {accountOpen && (
              <div className="account-menu">
                <button onClick={() => router.push('/settings')}>Edit profile and security</button>
                <button onClick={toggleTheme}>{theme === 'dark' ? 'Use light mode' : 'Use dark mode'}</button>
                <button onClick={toggleCollapsed}>{collapsed ? 'Expand sidebar' : 'Collapse sidebar'}</button>
                <button onClick={auth.logout}>Log out</button>
              </div>
            )}
          </div>
        </div>

        <section className="admin-surface">
          <h1 className="page-title">{title}</h1>
          {subtitle && <p className="page-copy">{subtitle}</p>}
          {children}
        </section>
      </main>
    </div>
  );
}
