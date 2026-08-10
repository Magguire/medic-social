import '../styles/globals.css';
import Head from 'next/head';
import { useRouter } from 'next/router';
import { Component, useEffect, useMemo, useState } from 'react';
import { CssBaseline, GlobalStyles, ThemeProvider, createTheme } from '@mui/material';
import { useAdminAuth } from '../lib/useAdminAuth';

const PUBLIC_ROUTES = new Set(['/login']);
const ADMIN_ROLES = new Set(['SuperAdmin', 'TenantAdmin', 'Auditor']);

class AdminErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error) {
    return { error };
  }

  componentDidUpdate(previousProps) {
    if (previousProps.routeKey !== this.props.routeKey && this.state.error) {
      this.setState({ error: null });
    }
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <div className="admin-boot">
        <div className="access-card">
          <h1>Admin page could not load</h1>
          <p>{this.state.error?.message || 'An unexpected client-side error occurred.'}</p>
          <button className="btn-primary" onClick={() => window.location.reload()}>Reload page</button>
        </div>
      </div>
    );
  }
}

function AuthGate({ children }) {
  const router = useRouter();
  const { hydrated, isAuthenticated, user, logout } = useAdminAuth();
  const isPublicRoute = PUBLIC_ROUTES.has(router.pathname);

  if (isPublicRoute) {
    return children;
  }

  if (!hydrated) {
    return <div className="admin-boot">Checking admin session...</div>;
  }

  if (user && !ADMIN_ROLES.has(user.userType)) {
    return (
      <div className="admin-boot">
        <div className="access-card">
          <h1>Admin access only</h1>
          <p>This console is limited to SuperAdmin, TenantAdmin, and Auditor accounts. Employer and professional accounts should use the client workspace.</p>
          <button className="btn-primary" onClick={logout}>Return to login</button>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <div className="admin-boot">Redirecting to login...</div>;
  }

  return children;
}

export default function App({ Component, pageProps }) {
  const router = useRouter();
  const [mode, setMode] = useState('light');

  useEffect(() => {
    const applyMode = () => setMode(localStorage.getItem('medsocial.admin.theme') || 'light');
    applyMode();
    window.addEventListener('storage', applyMode);
    window.addEventListener('medsocial-theme-change', applyMode);
    return () => {
      window.removeEventListener('storage', applyMode);
      window.removeEventListener('medsocial-theme-change', applyMode);
    };
  }, []);

  const theme = useMemo(() => createTheme({
    palette: {
      mode,
      primary: { main: '#8b004a' },
      secondary: { main: '#0ea5a4' },
      background: mode === 'dark'
        ? { default: '#0f1116', paper: '#161b22' }
        : { default: '#f8f3eb', paper: '#fffdf9' },
    },
    shape: { borderRadius: 18 },
    typography: {
      fontFamily: 'Inter, Segoe UI, Arial, sans-serif',
      h1: { fontWeight: 900 },
      h2: { fontWeight: 800 },
      button: { fontWeight: 800, textTransform: 'none' },
    },
    components: {
      MuiAccordion: {
        styleOverrides: {
          root: {
            borderRadius: 22,
            overflow: 'hidden',
            border: '1px solid rgba(148,163,184,0.22)',
            boxShadow: 'none',
            '&:before': { display: 'none' },
          },
        },
      },
    },
  }), [mode]);

  return (
    <>
      <Head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700;800&display=swap" rel="stylesheet" />
      </Head>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <GlobalStyles styles={{ body: { backgroundColor: theme.palette.background.default } }} />
        <AdminErrorBoundary routeKey={router.asPath}>
          <AuthGate>
            <Component {...pageProps} />
          </AuthGate>
        </AdminErrorBoundary>
      </ThemeProvider>
    </>
  );
}
