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

export function buildApiUrl(path) {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${getApiBaseUrl()}${path.startsWith('/') ? path : `/${path}`}`;
}

export function getClientBaseUrl() {
  if (typeof window !== 'undefined') {
    const runtimeUrl = window.__MEDSOCIAL_CONFIG__?.clientBaseUrl?.trim();
    if (runtimeUrl) {
      return runtimeUrl.replace(/\/+$/, '');
    }

    const currentUrl = new URL(window.location.href);
    if (currentUrl.port === '3001') {
      currentUrl.port = '3000';
      return currentUrl.origin;
    }

    if (currentUrl.port === '3002') {
      currentUrl.port = '3000';
      return currentUrl.origin;
    }

    return currentUrl.origin;
  }

  return (process.env.NEXT_PUBLIC_CLIENT_BASE_URL || 'http://localhost:3000').replace(/\/+$/, '');
}

export function buildClientUrl(path) {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${getClientBaseUrl()}${path.startsWith('/') ? path : `/${path}`}`;
}
