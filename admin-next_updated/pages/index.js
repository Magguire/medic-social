import { useEffect, useState } from 'react';
import Link from 'next/link';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function DashboardPage() {
  const [dashboard, setDashboard] = useState(null);
  const [audit, setAudit] = useState([]);
  const [user, setUser] = useState(null);
  const [sessionsOpen, setSessionsOpen] = useState(false);
  const [sessionDetail, setSessionDetail] = useState(null);
  const [sessionMessage, setSessionMessage] = useState('');

  useEffect(() => {
    let active = true;
    const load = async () => {
      const [currentUser, currentDashboard, currentAudit] = await Promise.allSettled([
        adminApi.getCurrentUser(),
        adminApi.getDashboard(),
        adminApi.getAudit({ pageNumber: 1, pageSize: 6 }),
      ]);
      if (!active) return;
      if (currentUser.status === 'fulfilled') setUser(currentUser.value);
      if (currentDashboard.status === 'fulfilled') setDashboard(currentDashboard.value);
      if (currentAudit.status === 'fulfilled') setAudit(currentAudit.value.items || []);
    };
    load();
    const interval = window.setInterval(load, 10000);
    const refreshOnFocus = () => load();
    window.addEventListener('focus', refreshOnFocus);
    return () => {
      active = false;
      window.clearInterval(interval);
      window.removeEventListener('focus', refreshOnFocus);
    };
  }, []);

  const stats = dashboard?.stats || {};
  const activeSessionRows = dashboard?.sessionMetrics?.activeSessionsList || [];

  return (
    <AdminShell user={user} title="Admin Dashboard" subtitle="Operations, verification, and audit visibility across the full platform.">
      <div className="stats-grid">
        <Link href="/professionals" className="stat-card navy"><div className="stat-label">Professional accounts</div><div className="stat-value">{stats.totalProfessionals || 0}</div><div className="table-note">{stats.completedProfessionalProfiles || 0} complete · {stats.professionalsWithDocuments || 0} with documents</div></Link>
        <Link href="/jobs" className="stat-card teal"><div className="stat-label">Jobs and applicants</div><div className="stat-value">{stats.activeJobs || 0}</div><div className="table-note">{stats.jobsWithApplicants || 0} jobs with applicants · {stats.totalApplications || 0} applications</div></Link>
        <button type="button" className="stat-card amber" style={{ textAlign: 'left', border: 0 }} onClick={() => setSessionsOpen(true)}><div className="stat-label">Active users</div><div className="stat-value">{stats.activeUsers || 0}</div><div className="table-note">Distinct users active in the last 15 minutes</div></button>
        <Link href="/reports" className="stat-card slate"><div className="stat-label">Active sessions</div><div className="stat-value">{stats.activeSessions || 0}</div><div className="table-note">Click for operational detail</div></Link>
      </div>

      {sessionsOpen && <div className="drawer-backdrop" onClick={() => { setSessionsOpen(false); setSessionDetail(null); }}>
        <aside className="action-drawer" style={{ width: 'min(760px, 94vw)' }} onClick={(event) => event.stopPropagation()}>
          <p className="eyebrow">Live access operations</p>
          <h2 className="panel-heading">Active users and sessions</h2>
          <p className="panel-subtitle">Logical sessions are grouped by user and browser/device. Select one to inspect page visits and API actions.</p>
          {sessionMessage && <div className="toast info" style={{ marginTop: 12 }}>{sessionMessage}</div>}
          {!sessionDetail ? <div className="stack" style={{ marginTop: 18 }}>
            {activeSessionRows.map((session) => <button key={session.sessionId} type="button" className="config-list-card interactive" onClick={async () => setSessionDetail(await adminApi.getSession(session.sessionId))}>
              <strong>{session.fullName || session.email} · {session.role}</strong>
              <span>{session.email} · {session.deviceId || 'Unknown device'} · Last active {new Date(session.lastSeenAt).toLocaleString()}</span>
            </button>)}
            {activeSessionRows.length === 0 && <div className="empty-state">No browser sessions are currently active.</div>}
          </div> : <div style={{ marginTop: 18 }}>
            <button className="btn-secondary" onClick={() => setSessionDetail(null)}>Back to active users</button>
            <div className="panel-card" style={{ marginTop: 14 }}>
              <h3>{sessionDetail.session.fullName || sessionDetail.session.email}</h3>
              <p className="panel-subtitle">{sessionDetail.session.role} · {sessionDetail.session.deviceId} · {sessionDetail.session.ip || 'No IP'}</p>
              <div className="button-row" style={{ marginTop: 14 }}>
                <button className="btn-primary" onClick={async () => { await adminApi.endSession(sessionDetail.session.sessionId); setSessionMessage('The selected browser session has been ended.'); setSessionDetail(null); }}>End this session</button>
                <button className="btn-secondary" onClick={async () => { await adminApi.endUserSessions(sessionDetail.session.userId); setSessionMessage('All sessions for this user have been ended.'); setSessionDetail(null); }}>Kick user from all devices</button>
              </div>
            </div>
            <table className="table-shell" style={{ marginTop: 14 }}><thead><tr><th>Activity</th><th>Target</th><th>When</th></tr></thead><tbody>{(sessionDetail.activities || []).map((item) => <tr key={item.id}><td>{item.action}</td><td>{item.entityId}</td><td>{new Date(item.timestamp).toLocaleString()}</td></tr>)}</tbody></table>
          </div>}
          <button className="btn-secondary" style={{ marginTop: 18 }} onClick={() => { setSessionsOpen(false); setSessionDetail(null); }}>Close</button>
        </aside>
      </div>}

      <div className="stats-grid" style={{ marginTop: 18 }}>
        <Link href="/professionals" className="panel-card"><div className="stat-label">Profile readiness</div><div style={{ fontSize: 28, fontWeight: 850, marginTop: 8 }}>{stats.completedProfessionalProfiles || 0} / {stats.totalProfessionals || 0}</div><div className="table-note">{stats.incompleteProfessionalProfiles || 0} profiles still incomplete</div></Link>
        <Link href="/professionals" className="panel-card"><div className="stat-label">Document readiness</div><div style={{ fontSize: 28, fontWeight: 850, marginTop: 8 }}>{stats.professionalDocumentsUploaded || 0}</div><div className="table-note">{stats.verifiedProfessionalDocuments || 0} verified · {stats.professionalsWithoutDocuments || 0} profiles without uploads</div></Link>
        <Link href="/jobs" className="panel-card"><div className="stat-label">Application workload</div><div style={{ fontSize: 28, fontWeight: 850, marginTop: 8 }}>{stats.pendingApplications || 0}</div><div className="table-note">{stats.shortlistedApplications || 0} shortlisted · {stats.averageApplicantsPerJob || 0} average per job</div></Link>
        <Link href="/jobs" className="panel-card"><div className="stat-label">Job pipeline</div><div style={{ fontSize: 28, fontWeight: 850, marginTop: 8 }}>{stats.totalJobs || 0}</div><div className="table-note">{stats.draftJobs || 0} draft · {stats.closedJobs || 0} closed · {stats.jobsWithoutApplicants || 0} without applicants</div></Link>
      </div>

      <div className="panel-card" style={{ marginTop: 18 }}>
        <h2 className="panel-heading">Live session overview</h2>
        <p className="panel-subtitle">Current non-expired sessions across administration, employer, and professional accounts.</p>
        <div className="stats-grid" style={{ marginTop: 18 }}>
          {(dashboard?.sessionMetrics?.byRole || []).map((item) => (
            <div key={item.role} style={{ borderRadius: 18, background: 'var(--panel-soft)', padding: 18 }}>
              <div className="stat-label">{item.role}</div>
              <div style={{ fontSize: 30, fontWeight: 800, marginTop: 8 }}>{item.sessions}</div>
            </div>
          ))}
        </div>
        <table className="table-shell" style={{ marginTop: 18 }}>
          <thead><tr><th>User</th><th>Role</th><th>Device</th><th>IP</th><th>Signed in</th><th>Expires</th></tr></thead>
          <tbody>
            {activeSessionRows.map((session) => (
              <tr key={session.sessionId}>
                <td><strong>{session.fullName || session.email}</strong><div className="table-note">{session.email}</div></td>
                <td>{session.role}</td>
                <td title={session.userAgent || ''}>{session.deviceId || 'Unknown device'}</td>
                <td>{session.ip || 'Unavailable'}</td>
                <td>{new Date(session.createdAt).toLocaleString()}</td>
                <td>{new Date(session.expiry).toLocaleString()}</td>
              </tr>
            ))}
            {activeSessionRows.length === 0 && <tr><td colSpan="6">No active browser sessions in the last 15 minutes.</td></tr>}
          </tbody>
        </table>
      </div>

      <div className="panel-grid">
        <div className="panel-card">
          <h2 className="panel-heading">Verification Requests</h2>
          <p className="panel-subtitle">Recent requests coming directly from the verification queue.</p>
          <table className="table-shell">
            <thead><tr><th>Subject</th><th>Tenant</th><th>Status</th><th>Created</th></tr></thead>
            <tbody>
              {(dashboard?.verificationRequests || []).map((item) => (
                <tr key={item.id}>
                  <td>{item.subjectType}</td>
                  <td>{item.subjectId}</td>
                  <td><span className={`badge ${String(item.status).toLowerCase()}`}>{item.status}</span></td>
                  <td>{new Date(item.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="panel-card">
          <h2 className="panel-heading">Job Statistics</h2>
          <p className="panel-subtitle">Live output from the consolidated job service.</p>
          <div className="stack" style={{ marginTop: 18 }}>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 18 }}>
              <div style={{ color: 'var(--muted)', fontSize: 13 }}>Published jobs</div>
              <div style={{ fontSize: 36, fontWeight: 800, marginTop: 8 }}>{stats.activeJobs || 0}</div>
            </div>
            <div style={{ borderRadius: 22, background: 'var(--panel-soft)', padding: 18 }}>
              <div style={{ color: 'var(--muted)', fontSize: 13 }}>Verification workload</div>
              <div style={{ fontSize: 36, fontWeight: 800, marginTop: 8 }}>{(dashboard?.verificationRequests || []).length}</div>
            </div>
          </div>
        </div>
      </div>

      <div className="panel-card" style={{ marginTop: 18 }}>
        <h2 className="panel-heading">Recent Activity</h2>
        <p className="panel-subtitle">The platform-wide audit trail for both client and admin actions.</p>
        <table className="table-shell">
          <thead><tr><th>Action</th><th>Entity</th><th>User</th><th>When</th></tr></thead>
          <tbody>
            {audit.map((item) => (
              <tr key={item.id}>
                <td>{item.action}</td>
                <td>{item.entityName || 'n/a'}</td>
                <td>{item.userId || 'system'}</td>
                <td>{new Date(item.timestamp).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminShell>
  );
}
