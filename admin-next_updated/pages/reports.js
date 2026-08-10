import { useEffect, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function ReportsPage() {
  const [user, setUser] = useState(null);
  const [audit, setAudit] = useState([]);
  const [messages, setMessages] = useState([]);
  const [auditTotal, setAuditTotal] = useState(0);
  const [messageTotal, setMessageTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [messagePage, setMessagePage] = useState(1);
  const [activeTab, setActiveTab] = useState('audit');
  const [showArchived, setShowArchived] = useState(false);
  const [selectedAudit, setSelectedAudit] = useState([]);
  const [toast, setToast] = useState('');
  const pageSize = 20;
  const messagePageSize = 10;

  useEffect(() => {
    Promise.all([
      adminApi.getCurrentUser(),
      adminApi.getAudit({ pageNumber: page, pageSize, archived: showArchived }),
      adminApi.getCommunicationMessages({ pageNumber: messagePage, pageSize: messagePageSize }),
    ]).then(([currentUser, auditResponse, messageResponse]) => {
      setUser(currentUser);
      setAudit(auditResponse.items || []);
      setAuditTotal(auditResponse.totalCount || 0);
      setMessages(messageResponse.items || []);
      setMessageTotal(messageResponse.totalCount || 0);
    });
  }, [page, messagePage, showArchived]);

  const archiveSelected = async () => {
    if (!selectedAudit.length) return;
    await adminApi.archiveAudit(selectedAudit);
    setToast(`${selectedAudit.length} audit record(s) archived.`);
    setSelectedAudit([]);
    const response = await adminApi.getAudit({ pageNumber: page, pageSize, archived: showArchived });
    setAudit(response.items || []);
    setAuditTotal(response.totalCount || 0);
  };

  const rangeText = (currentPage, size, total, label) => {
    if (!total) return `No ${label} found`;
    const start = ((currentPage - 1) * size) + 1;
    const end = Math.min(currentPage * size, total);
    return `Showing ${start}-${end} of ${total} ${label}`;
  };

  return (
    <AdminShell user={user} title="Reports and Audit Trail" subtitle="Every client and admin action captured through the shared audit pipeline.">
      <div className="tabs" style={{ marginTop: 20 }}>
        <button className={`tab-button ${activeTab === 'audit' ? 'active' : ''}`} onClick={() => setActiveTab('audit')}>Audit events</button>
        <button className={`tab-button ${activeTab === 'communications' ? 'active' : ''}`} onClick={() => setActiveTab('communications')}>Communication logs</button>
      </div>
      {toast && <div className="toast-stack"><button className="toast success" onClick={() => setToast('')}>{toast}</button></div>}

      {activeTab === 'audit' && (
        <details className="collapsible" open>
          <summary>Audit events</summary>
          <div className="collapsible-body">
            <p className="pagination-meta">{rangeText(page, pageSize, auditTotal, 'audit records')}</p>
            <div className="button-row" style={{ marginBottom: 14 }}>
              <label className="switch-card"><input type="checkbox" checked={showArchived} onChange={(event) => { setShowArchived(event.target.checked); setPage(1); setSelectedAudit([]); }} /> Show archived audit records</label>
              {!showArchived && <button className="btn-primary" disabled={!selectedAudit.length} onClick={archiveSelected}>Archive selected ({selectedAudit.length})</button>}
            </div>
            <table className="table-shell">
              <thead><tr><th>{!showArchived && <input type="checkbox" checked={audit.length > 0 && selectedAudit.length === audit.length} onChange={(event) => setSelectedAudit(event.target.checked ? audit.map((item) => item.id) : [])} />}</th><th>Action</th><th>Entity</th><th>Actor</th><th>IP</th><th>Timestamp</th></tr></thead>
              <tbody>
                {audit.map((item) => (
                  <tr key={item.id}>
                    <td>{!showArchived && <input type="checkbox" checked={selectedAudit.includes(item.id)} onChange={() => setSelectedAudit((current) => current.includes(item.id) ? current.filter((id) => id !== item.id) : [...current, item.id])} />}</td>
                    <td>{item.action}</td>
                    <td>{item.entityName || 'n/a'}</td>
                    <td>{item.userId ? 'Authenticated user' : 'system'}</td>
                    <td>{item.ipAddress || 'n/a'}</td>
                    <td>{new Date(item.timestamp).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="button-row" style={{ marginTop: 16, alignItems: 'center' }}>
              <button className="btn-secondary" disabled={page === 1} onClick={() => setPage((current) => Math.max(1, current - 1))}>Previous</button>
              <strong>Page {page} of {Math.max(1, Math.ceil(auditTotal / pageSize))}</strong>
              <button className="btn-secondary" disabled={page >= Math.ceil(auditTotal / pageSize || 1)} onClick={() => setPage((current) => current + 1)}>Next</button>
            </div>
          </div>
        </details>
      )}

      {activeTab === 'communications' && (
        <details className="collapsible" open>
          <summary>Communication log</summary>
          <div className="collapsible-body">
            <p className="pagination-meta">{rangeText(messagePage, messagePageSize, messageTotal, 'communication records')}</p>
            <table className="table-shell">
              <thead><tr><th>Channel</th><th>Recipient</th><th>Subject</th><th>Status</th><th>Timestamp</th></tr></thead>
              <tbody>
                {messages.map((item) => (
                  <tr key={item.id}>
                    <td>{item.channel}</td>
                    <td>{item.recipient}</td>
                    <td>{item.subject || 'n/a'}</td>
                    <td><span className={`badge ${String(item.status).toLowerCase()}`}>{item.status}</span></td>
                    <td>{new Date(item.createdAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="button-row" style={{ marginTop: 16, alignItems: 'center' }}>
              <button className="btn-secondary" disabled={messagePage === 1} onClick={() => setMessagePage((current) => Math.max(1, current - 1))}>Previous</button>
              <strong>Page {messagePage} of {Math.max(1, Math.ceil(messageTotal / messagePageSize))}</strong>
              <button className="btn-secondary" disabled={messagePage >= Math.ceil(messageTotal / messagePageSize || 1)} onClick={() => setMessagePage((current) => current + 1)}>Next</button>
            </div>
          </div>
        </details>
      )}
    </AdminShell>
  );
}
