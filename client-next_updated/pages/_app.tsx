import '../styles/globals.css';
import type { AppProps } from 'next/app';
import Head from 'next/head';
import { useEffect, useMemo, useState } from 'react';
import { CssBaseline, GlobalStyles, ThemeProvider, createTheme } from '@mui/material';
import { Provider } from 'react-redux';
import { store } from '../store';
import { contentApi } from '../lib/contentApi';

const defaultClientTheme = {
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
};

function applyClientTheme(themeConfig: typeof defaultClientTheme, mode: 'light' | 'dark') {
  if (typeof document === 'undefined') {
    return;
  }

  const root = document.documentElement;
  root.dataset.theme = mode;
  root.style.setProperty('--client-primary', themeConfig.primaryColor);
  root.style.setProperty('--client-secondary', themeConfig.secondaryColor);
  root.style.setProperty('--client-accent', themeConfig.accentColor);
  root.style.setProperty('--brand-calm', themeConfig.primaryColor);
  root.style.setProperty('--brand-warm', themeConfig.accentColor);
  root.style.setProperty('--client-bg', mode === 'dark' ? themeConfig.darkBackgroundColor : themeConfig.backgroundColor);
  root.style.setProperty('--client-panel', mode === 'dark' ? themeConfig.darkSurfaceColor : themeConfig.surfaceColor);
  root.style.setProperty('--client-input', mode === 'dark' ? themeConfig.darkSurfaceColor : themeConfig.surfaceColor);
  root.style.setProperty('--client-text', mode === 'dark' ? themeConfig.darkTextColor : themeConfig.textColor);
  root.style.setProperty('--client-muted', mode === 'dark' ? themeConfig.darkMutedTextColor : themeConfig.mutedTextColor);
}

export default function App({ Component, pageProps }: AppProps) {
  const [mode, setMode] = useState<'light' | 'dark'>('light');
  const [clientTheme, setClientTheme] = useState(defaultClientTheme);

  useEffect(() => {
    const applyMode = () => setMode((window.localStorage.getItem('medsocial.client.theme') as 'light' | 'dark') || 'light');
    applyMode();
    window.addEventListener('storage', applyMode);
    window.addEventListener('medsocial-theme-change', applyMode as EventListener);
    return () => {
      window.removeEventListener('storage', applyMode);
      window.removeEventListener('medsocial-theme-change', applyMode as EventListener);
    };
  }, []);

  useEffect(() => {
    let mounted = true;
    contentApi.getClientTheme()
      .then((themeConfig) => {
        if (!mounted) {
          return;
        }

        setClientTheme({ ...defaultClientTheme, ...(themeConfig || {}) });
      })
      .catch(() => undefined);

    return () => {
      mounted = false;
    };
  }, []);

  useEffect(() => {
    applyClientTheme(clientTheme, mode);
  }, [clientTheme, mode]);

  const theme = useMemo(() => createTheme({
    palette: {
      mode,
      primary: { main: clientTheme.primaryColor },
      secondary: { main: clientTheme.accentColor },
      background: mode === 'dark'
        ? { default: clientTheme.darkBackgroundColor, paper: clientTheme.darkSurfaceColor }
        : { default: clientTheme.backgroundColor, paper: clientTheme.surfaceColor },
      text: mode === 'dark'
        ? { primary: clientTheme.darkTextColor, secondary: clientTheme.darkMutedTextColor }
        : { primary: clientTheme.textColor, secondary: clientTheme.mutedTextColor },
    },
    shape: { borderRadius: 20 },
    typography: {
      fontFamily: 'Inter, Segoe UI, Arial, sans-serif',
      h1: { fontWeight: 900 },
      h2: { fontWeight: 800 },
      button: { fontWeight: 800, textTransform: 'none' },
    },
  }), [clientTheme, mode]);

  return (
    <>
      <Head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="description" content="MedicSocial - Healthcare Professionals Platform" />
        <link rel="preconnect" href="https://fonts.gstatic.com" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700&display=swap" rel="stylesheet" />
        <title>MedicSocial</title>
      </Head>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <GlobalStyles styles={{ body: { backgroundColor: theme.palette.background.default } }} />
        <Provider store={store}>
          <Component {...pageProps} />
        </Provider>
      </ThemeProvider>
    </>
  );
}
