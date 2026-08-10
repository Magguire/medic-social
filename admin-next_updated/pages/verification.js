import { useEffect, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function VerificationPage() {
  const [user, setUser] = useState(null);
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [reason, setReason] = useState('');
  const [bypassIntegration, setBypassIntegration] = useState(true);
  const [error, setError] = useState('');

  const load = async () => {
    const [currentUser, requests] = await Promise.all([
      adminApi.getCurrentUser(),
      adminApi.getVerificationRequests('Pending'),
    ]);
    setUser(currentUser);
    setItems(requests);
    setSelected(requests[0] || null);
  };

  useEffect(() => {
    load().catch((requestError) => setError(requestError.message));
  }, []);

  const review = async (action) => {
    if (!selected || !user) return;
    try {
      if (action === 'approve') {
        await adminApi.approveVerification(selected.id, user.id, bypassIntegration);
      } else {
        await adminApi.rejectVerification(selected.id, user.id, reason, bypassIntegration);
      }
      setReason('');
      await load();
      setError('');
    } catch (requestError) {
      setError(requestError.message);
    }
  };

  return (
    <AdminShell user={user} title="Verification Requests" subtitle="Approve, reject, and bypass integration checks when no external provider is configured.">
      {error && <div style={{ marginTop: 18, borderRadius: 16, background: '#ffe5e9', color: '#c0354a', padding: 14, fontWeight: 700 }}>{error}</div>}
      <div className="panel-grid">
        <div className="panel-card">
          <h2 className="panel-heading">Pending Queue</h2>
          <div className="stack" style={{ marginTop: 18 }}>
            {items.map((item) => (
              <button key={item.id} onClick={() => setSelected(item)} className="btn-secondary" style={{ justifyContent: 'space-between', display: 'flex', width: '100%', textAlign: 'left' }}>
                <span>
                  <strong>{item.subjectType}</strong>
                  <div style={{ color: 'var(--muted)', fontSize: 13, marginTop: 4 }}>{item.notes || 'Verification request'}</div>
                </span>
                <span className={`badge ${String(item.status).toLowerCase()}`}>{item.status}</span>
              </button>
            ))}
          </div>
        </div>

        <div className="panel-card">
          <h2 className="panel-heading">Review Workspace</h2>
          {selected ? (
            <>
              <div className="stack" style={{ marginTop: 18 }}>
                <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Subject type</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{selected.subjectType}</div></div>
                <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Review mode</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{bypassIntegration ? 'Manual override allowed' : 'External integration required'}</div></div>
                <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 16 }}><strong>Notes</strong><div style={{ color: 'var(--muted)', marginTop: 6 }}>{selected.notes || 'No notes supplied.'}</div></div>
              </div>
              <label style={{ display: 'flex', alignItems: 'center', gap: 10, marginTop: 18, color: 'var(--muted)' }}>
                <input type="checkbox" checked={bypassIntegration} onChange={(event) => setBypassIntegration(event.target.checked)} />
                Bypass verification integration if provider configuration is missing
              </label>
              <textarea className="textarea" style={{ marginTop: 18 }} value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Optional rejection reason or review note" />
              <div className="button-row" style={{ marginTop: 18 }}>
                <button className="btn-primary" onClick={() => review('approve')}>Approve</button>
                <button className="btn-danger" onClick={() => review('reject')}>Reject</button>
              </div>
            </>
          ) : (
            <p className="panel-subtitle" style={{ marginTop: 16 }}>No pending requests.</p>
          )}
        </div>
      </div>
    </AdminShell>
  );
}
