import Link from 'next/link';
import { useRouter } from 'next/router';
import type { PropsWithChildren } from 'react';
import { useEffect, useState } from 'react';
import { useAuth } from '../lib/useAuth';
import { employerApi } from '../lib/employerApi';
import { subscriptionApi } from '../lib/subscriptionApi';
import apiClient from '../lib/apiClient';
import { notificationApi, type InAppNotification } from '../lib/notificationApi';
import { getBrowserDeviceId } from '../lib/deviceIdentity';
import { contentApi } from '../lib/contentApi';

const navigation = [
  { href: '/jobs', label: 'Job Listings' },
  { href: '/feed', label: 'Feed' },
  { href: '/applications', label: 'My Applications' },
  { href: '/dashboard', label: 'Dashboard' },
  { href: '/professional/profile', label: 'Profile' },
];

const publicNavigation = [
  { href: '/jobs', label: 'Find Roles' },
  { href: '/register?type=employer', label: 'For Employers' },
  { href: '/feed', label: 'Feed' },
  { href: '/terms', label: 'Resources' },
];

const employerWorkspace = [
  { href: '/dashboard', label: 'Overview', icon: '⌂' },
  { href: '/feed', label: 'Feed', icon: '#' },
  { href: '/employer/profile', label: 'Facility Profile', icon: '✚' },
  { href: '/employer/jobs/new', label: 'Create Opening', icon: '+', entitlement: 'canAccessJobPostingModule' },
  { href: '/employer/jobs', label: 'Created Openings', icon: '▤' },
  { href: '/employer/applicants', label: 'Applicants', icon: '◎' },
  { href: '/professionals', label: 'Talent Search', icon: '⌕' },
  { href: '/settings', label: 'Settings', icon: '⚙' },
];

const professionalWorkspace = [
  { href: '/dashboard', label: 'Overview', icon: '⌂' },
  { href: '/feed', label: 'Feed', icon: '#' },
  { href: '/professional/profile', label: 'Complete Profile', icon: '●' },
  { href: '/applications', label: 'Applications', icon: '▤' },
  { href: '/jobs', label: 'Browse Jobs', icon: '⌕' },
  { href: '/settings', label: 'Settings', icon: '⚙' },
];

export default function Layout({ children }: PropsWithChildren) {
  const router = useRouter();
  const { user, isAuthenticated, logout } = useAuth();
  const [theme, setTheme] = useState('light');
  const [menuOpen, setMenuOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [notifications, setNotifications] = useState<InAppNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [subscriptionPlan, setSubscriptionPlan] = useState<any>(null);
  const [brand, setBrand] = useState({ name: 'medicSocial', tagline: 'Healthcare hiring' });

  useEffect(() => {
    const savedTheme = window.localStorage.getItem('medsocial.client.theme') || 'light';
    setTheme(savedTheme);
    document.documentElement.dataset.theme = savedTheme;
  }, []);

  useEffect(() => {
    contentApi.getLandingPage()
      .then((content) => setBrand({
        name: content?.brandName || 'medicSocial',
        tagline: content?.brandTagline || 'Healthcare hiring',
      }))
      .catch(() => setBrand({ name: 'medicSocial', tagline: 'Healthcare hiring' }));
  }, []);

  useEffect(() => {
    if (user?.userType !== 'Employer') return;
    employerApi.getCurrent()
      .then((employer) => subscriptionApi.current(employer.id))
      .then((value) => setSubscriptionPlan(value.plan))
      .catch(() => setSubscriptionPlan(null));
  }, [user]);

  useEffect(() => {
    if (!isAuthenticated) return;
    let cancelled = false;
    const loadNotifications = () => {
      notificationApi.list(false)
        .then((result) => {
          if (cancelled) return;
          setNotifications(Array.isArray(result.items) ? result.items : []);
          setUnreadCount(result.unreadCount || 0);
        })
        .catch(() => undefined);
    };
    loadNotifications();
    window.addEventListener('medsocial-notifications-refresh', loadNotifications);
    const timer = window.setInterval(loadNotifications, 30000);
    return () => {
      cancelled = true;
      window.removeEventListener('medsocial-notifications-refresh', loadNotifications);
      window.clearInterval(timer);
    };
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) return;
    const recordPage = async (path: string) => {
      apiClient.post('/api/audit/page-view', {
        path,
        title: document.title,
        referrer: document.referrer || null,
        deviceId: await getBrowserDeviceId(),
      }).catch(() => undefined);
    };
    recordPage(router.asPath);
    router.events.on('routeChangeComplete', recordPage);
    return () => router.events.off('routeChangeComplete', recordPage);
  }, [isAuthenticated, router.asPath, router.events]);

  useEffect(() => {
    if (!isAuthenticated) return;

    const idleMs = 20 * 60 * 1000;
    let idleTimer: number | undefined;

    const sendHeartbeat = async () => {
      await apiClient.post('/api/audit/heartbeat', {
        deviceId: await getBrowserDeviceId(),
      }).catch(() => undefined);
    };

    const expireSession = () => {
      window.localStorage.setItem('medsocial.client.idleLogoutAt', new Date().toISOString());
      logout();
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
  }, [isAuthenticated, logout]);

  useEffect(() => {
    if (user?.mustChangePassword && router.pathname !== '/settings') {
      router.replace('/settings?tab=security');
    }
  }, [router, user?.mustChangePassword]);

  useEffect(() => {
    const closeMenus = () => {
      setMenuOpen(false);
      setAccountOpen(false);
      setNotificationsOpen(false);
    };
    router.events.on('routeChangeComplete', closeMenus);
    return () => router.events.off('routeChangeComplete', closeMenus);
  }, [router.events]);

  useEffect(() => {
    if (!menuOpen) return undefined;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false);
    };
    document.addEventListener('keydown', closeOnEscape);
    document.body.classList.add('drawer-open');
    return () => {
      document.removeEventListener('keydown', closeOnEscape);
      document.body.classList.remove('drawer-open');
    };
  }, [menuOpen]);

  const toggleTheme = () => {
    const nextTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(nextTheme);
    document.documentElement.dataset.theme = nextTheme;
    window.localStorage.setItem('medsocial.client.theme', nextTheme);
    window.dispatchEvent(new Event('medsocial-theme-change'));
  };

  const isActive = (href: string) => router.pathname === href || router.pathname.startsWith(`${href}/`);
  const workspaceItems = user?.userType === 'Employer' ? employerWorkspace : professionalWorkspace;
  const showWorkspace = isAuthenticated && user && !router.pathname.startsWith('/login') && !router.pathname.startsWith('/register');
  const profileHref = user?.userType === 'Employer' ? '/employer/profile' : '/professional/profile';
  const displayName = user ? `${user.firstName} ${user.lastName}`.trim() || user.email : '';
  const openNotification = async (notification: InAppNotification) => {
    await notificationApi.markRead(notification.id).catch(() => undefined);
    setUnreadCount((current) => Math.max(0, current - (notification.readAt ? 0 : 1)));
    setNotifications((current) => current.map((item) => item.id === notification.id ? { ...item, readAt: new Date().toISOString() } : item));
    if (notification.actionUrl) {
      router.push(notification.actionUrl);
      setNotificationsOpen(false);
    }
  };

  return (
    <div className="min-h-screen">
      <header className="client-header sticky top-0 z-30 border-b backdrop-blur">
        <div className="mx-auto flex max-w-[92rem] items-center justify-between gap-4 px-4 py-3 sm:px-5 lg:px-6">
          <div className="client-header-leading flex items-center gap-8">
            <button
              className="hamburger-button"
              type="button"
              aria-label={menuOpen ? 'Close navigation menu' : 'Open navigation menu'}
              aria-expanded={menuOpen}
              aria-controls={showWorkspace ? 'workspace-navigation' : 'public-navigation-drawer'}
              onClick={() => setMenuOpen((current) => !current)}
            >
              <span aria-hidden="true">{menuOpen ? '×' : '☰'}</span>
            </button>
            <Link href="/" className="brand-lockup">
              <div className="brand-mark">+</div>
              <div>
                <p className="brand-title">{brand.name}</p>
                <p className="brand-subtitle">{brand.tagline}</p>
              </div>
            </Link>
            <nav className="hidden items-center gap-2 lg:flex">
              {(isAuthenticated ? navigation : publicNavigation).map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                    isActive(item.href)
                      ? 'bg-emerald-50 text-emerald-700'
                      : 'text-slate-500 hover:bg-slate-100 hover:text-slate-900'
                  }`}
                >
                  {item.label}
                </Link>
              ))}
              {user?.userType === 'Employer' && (
                <Link href="/professionals" className={`rounded-full px-4 py-2 text-sm font-semibold transition ${isActive('/professionals') ? 'bg-emerald-50 text-emerald-700' : 'text-slate-500 hover:bg-slate-100 hover:text-slate-900'}`}>
                  Talent Search
                </Link>
              )}
            </nav>
          </div>

          <div className="client-header-actions flex items-center gap-3">
            {isAuthenticated && user ? (
              <div className="account-menu-wrap">
                <button className="notification-trigger" type="button" onClick={() => setNotificationsOpen((current) => !current)}>
                  <span className="sr-only">Notifications</span>
                  <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none">
                    <path d="M15 17H9m10-2.2c-1.2-1.1-1.7-2.4-1.7-4.3V9a5.3 5.3 0 0 0-10.6 0v1.5c0 1.9-.5 3.2-1.7 4.3-.6.5-.2 1.5.6 1.5h12.8c.8 0 1.2-1 .6-1.5Z" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                    <path d="M10 20a2.2 2.2 0 0 0 4 0" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
                  </svg>
                  {unreadCount > 0 && <strong>{unreadCount}</strong>}
                </button>
                {notificationsOpen && (
                  <div className="notification-menu">
                    <div className="notification-menu-head">
                      <strong>Notifications</strong>
                      <button type="button" onClick={async () => { await notificationApi.markAllRead(); setUnreadCount(0); setNotifications((current) => current.map((item) => ({ ...item, readAt: item.readAt || new Date().toISOString() }))); }}>Mark all read</button>
                    </div>
                    {notifications.map((notification) => (
                      <button key={notification.id} type="button" className={`notification-item ${notification.readAt ? '' : 'unread'}`} onClick={() => openNotification(notification)}>
                        <strong>{notification.title}</strong>
                        <span>{notification.message}</span>
                        <small>{new Date(notification.createdAt).toLocaleString()}</small>
                      </button>
                    ))}
                    {notifications.length === 0 && <p className="notification-empty">No notifications yet.</p>}
                  </div>
                )}
                <button className="account-trigger" onClick={() => setAccountOpen((current) => !current)}>
                  <span>{displayName.slice(0, 1).toUpperCase()}</span>
                  <div>
                    <strong>{displayName}</strong>
                    <small>{user.userType} account</small>
                  </div>
                </button>
                {accountOpen && (
                  <div className="account-menu">
                    <button onClick={() => router.push(profileHref)}>Edit profile</button>
                    <button onClick={toggleTheme}>{theme === 'dark' ? 'Use light mode' : 'Use dark mode'}</button>
                    <button onClick={logout}>Log out</button>
                  </div>
                )}
              </div>
            ) : (
              <>
                <button className="secondary-action" type="button" onClick={toggleTheme}>{theme === 'dark' ? 'Light mode' : 'Dark mode'}</button>
                <Link href="/login" className="secondary-action">Login</Link>
                <Link href="/register" className="primary-action public-join-action">Join Network</Link>
              </>
            )}
          </div>
        </div>
      </header>

      {!showWorkspace && (
        <aside id="public-navigation-drawer" className={`public-navigation-drawer ${menuOpen ? 'open' : ''}`} aria-hidden={!menuOpen}>
          <div className="drawer-heading">
            <strong>Navigation</strong>
            <button type="button" aria-label="Close navigation menu" onClick={() => setMenuOpen(false)}>×</button>
          </div>
          <nav>
            {publicNavigation.map((item) => (
              <Link key={item.href} href={item.href} className={isActive(item.href) ? 'active' : ''}>{item.label}</Link>
            ))}
          </nav>
          {!isAuthenticated && (
            <div className="drawer-account-actions">
              <Link href="/login" className="secondary-action">Login</Link>
              <Link href="/register" className="primary-action">Join Network</Link>
              <button className="secondary-action" type="button" onClick={toggleTheme}>{theme === 'dark' ? 'Use light mode' : 'Use dark mode'}</button>
            </div>
          )}
        </aside>
      )}
      {!showWorkspace && menuOpen && <button className="drawer-scrim public-drawer-scrim" aria-label="Close menu" onClick={() => setMenuOpen(false)} />}

      {showWorkspace ? (
        <div className="workspace-shell mx-auto grid max-w-[92rem] gap-5 px-4 py-5 sm:px-5 lg:grid-cols-[248px_1fr] lg:px-6">
          <aside id="workspace-navigation" className={`workspace-sidebar ${menuOpen ? 'open' : ''}`}>
            <div className="sidebar-user-card">
              <div className="sidebar-avatar">{displayName.slice(0, 1).toUpperCase()}</div>
              <div>
                <strong>{displayName}</strong>
                <span>{user.userType} workspace</span>
              </div>
            </div>
            <nav className="mt-4 grid gap-2">
              {workspaceItems.map((item) => (
                <Link
                  key={item.href}
                  href={(item as any).entitlement && subscriptionPlan && !subscriptionPlan[(item as any).entitlement] ? '/settings' : item.href}
                  title={(item as any).entitlement && subscriptionPlan && !subscriptionPlan[(item as any).entitlement] ? 'This module is not included in your current subscription.' : ''}
                  onClick={() => setMenuOpen(false)}
                  className={`workspace-link ${isActive(item.href) ? 'active' : ''} ${(item as any).entitlement && subscriptionPlan && !subscriptionPlan[(item as any).entitlement] ? 'opacity-50' : ''}`}
                >
                  <span className="workspace-icon">{item.icon}</span>{item.label}{(item as any).entitlement && subscriptionPlan && !subscriptionPlan[(item as any).entitlement] ? ' · Upgrade' : ''}
                </Link>
              ))}
            </nav>
          </aside>
          {menuOpen && <button className="drawer-scrim" aria-label="Close menu" onClick={() => setMenuOpen(false)} />}
          <main>{children}</main>
        </div>
      ) : (
        <main className="mx-auto max-w-[92rem] px-4 py-5 sm:px-5 lg:px-6">{children}</main>
      )}
      <footer className="mx-auto flex max-w-[92rem] flex-wrap items-center justify-between gap-3 px-4 pb-6 pt-2 text-sm text-slate-500 sm:px-5 lg:px-6">
        <span>{brand.name} {brand.tagline}</span>
        <nav className="flex flex-wrap gap-3">
          <Link href="/privacy" className="font-semibold hover:text-slate-900">Privacy Policy</Link>
          <Link href="/terms" className="font-semibold hover:text-slate-900">Terms and Conditions</Link>
          <Link href="/feed" className="font-semibold hover:text-slate-900">Feed</Link>
        </nav>
      </footer>
    </div>
  );
}
