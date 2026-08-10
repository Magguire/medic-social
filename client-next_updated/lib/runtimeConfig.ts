declare global {
  interface Window {
    __MEDSOCIAL_CONFIG__?: {
      apiBaseUrl?: string;
    };
  }
}

export function getApiBaseUrl() {
  if (typeof window !== 'undefined') {
    const runtimeUrl = window.__MEDSOCIAL_CONFIG__?.apiBaseUrl?.trim();
    if (runtimeUrl) {
      return runtimeUrl.replace(/\/+$/, '');
    }

    return window.location.origin;
  }

  return (process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5241').replace(/\/+$/, '');
}
