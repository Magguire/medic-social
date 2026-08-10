async function fetchWithAuth(input, init = {}) {
  const base = typeof input === 'string' ? input : input.url;
  const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
  init.headers = init.headers || {};
  if (token) init.headers['Authorization'] = `Bearer ${token}`;

  let res = await fetch(input, init);
  if (res.status === 401) {
    // try refresh
    const refresh = localStorage.getItem('refreshToken');
    if (!refresh) return res;
    const r = await fetch('/api/auth/refresh', { method: 'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify({ refreshToken: refresh }) });
    if (r.ok) {
      const j = await r.json();
      // j must contain accessToken and refreshToken depending on API shape
      if (j.accessToken) localStorage.setItem('accessToken', j.accessToken);
      if (j.refreshToken) localStorage.setItem('refreshToken', j.refreshToken);
      // retry original
      const newToken = localStorage.getItem('accessToken');
      init.headers['Authorization'] = `Bearer ${newToken}`;
      res = await fetch(input, init);
    }
  }
  return res;
}

export default fetchWithAuth;
