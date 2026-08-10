const DEVICE_SEED_KEY = 'medsocial.browser.deviceSeed';
const DEVICE_ID_KEY = 'medsocial.browser.deviceId';

function getOrCreateSeed() {
  const existing = localStorage.getItem(DEVICE_SEED_KEY);
  if (existing) {
    return existing;
  }

  const seed = globalThis.crypto?.randomUUID?.()
    || `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  localStorage.setItem(DEVICE_SEED_KEY, seed);
  return seed;
}

async function hash(value) {
  if (!globalThis.crypto?.subtle) {
    return btoa(value).replace(/[^a-zA-Z0-9]/g, '').slice(0, 48);
  }

  const bytes = new TextEncoder().encode(value);
  const digest = await globalThis.crypto.subtle.digest('SHA-256', bytes);
  return Array.from(new Uint8Array(digest))
    .map((item) => item.toString(16).padStart(2, '0'))
    .join('');
}

export async function getBrowserDeviceId() {
  if (typeof window === 'undefined') {
    return 'web-server';
  }

  try {
    const stored = localStorage.getItem(DEVICE_ID_KEY);
    if (stored) {
      return stored;
    }

    const signatureSource = [
      getOrCreateSeed(),
      navigator.userAgent || '',
      navigator.platform || '',
      navigator.language || '',
      Intl.DateTimeFormat().resolvedOptions().timeZone || '',
      window.screen?.width || 0,
      window.screen?.height || 0,
      window.screen?.colorDepth || 0,
    ].join('|');

    const deviceId = `web-${(await hash(signatureSource)).slice(0, 48)}`;
    localStorage.setItem(DEVICE_ID_KEY, deviceId);
    return deviceId;
  } catch {
    // Device identification must never prevent authentication.
    const fallback = `web-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 18)}`;
    try {
      localStorage.setItem(DEVICE_ID_KEY, fallback);
    } catch {
      // Storage can be disabled by browser privacy controls.
    }
    return fallback;
  }
}
