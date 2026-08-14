import { useEffect, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function ProfessionalsPage() {
  const [user, setUser] = useState(null);
  const [items, setItems] = useState([]);
  const [configuration, setConfiguration] = useState(null);
  const [editing, setEditing] = useState(null);
  const [resetting, setResetting] = useState(null);
  const [actionItem, setActionItem] = useState(null);
  const [search, setSearch] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [toast, setToast] = useState(null);

  const showToast = (type, text) => {
    setToast({ type, text });
    setTimeout(() => setToast(null), 3200);
  };

  const load = async () => {
    const [currentUser, professionals, config] = await Promise.all([adminApi.getCurrentUser(), adminApi.getProfessionals(), adminApi.getConfiguration()]);
    setUser(currentUser);
    setItems(professionals);
    setConfiguration(config);
  };

  useEffect(() => {
    load().catch(() => undefined);
  }, []);

  const saveProfessional = async (event) => {
    event.preventDefault();
    try {
      await adminApi.updateProfessional(editing.id, {
        yearsOfExperience: Number(editing.yearsOfExperience || 0),
        professionalCategory: editing.professionalCategory,
      });
      setEditing(null);
      showToast('success', 'Professional profile updated.');
      await load();
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to update professional.');
    }
  };

  const resetPassword = async (event) => {
    event.preventDefault();
    try {
      await adminApi.adminResetPassword({ userId: resetting.userId, email: resetting.email, newPassword });
      setResetting(null);
      setNewPassword('');
      showToast('success', 'Professional password reset.');
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to reset professional password.');
    }
  };

  const setVerification = async (item, status) => {
    if (!item.id) {
      showToast('info', 'This account must create a professional profile before profile verification can begin.');
      return;
    }
    try {
      await adminApi.setProfessionalVerification(item.id, { status, notes: status === 'Verified' ? 'Verified by super admin.' : 'Verification review initiated by admin.' });
      setActionItem(null);
      showToast('info', `Professional status set to ${status}.`);
      await load();
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to update professional verification.');
    }
  };

  const filteredItems = items.filter((item) => {
    const value = `${item.fullName || ''} ${item.email || ''} ${item.professionalCategory || ''} ${item.specialty || ''}`.toLowerCase();
    return value.includes(search.toLowerCase());
  });

  return (
    <AdminShell user={user} title="Professionals" subtitle="Platform-wide view of categories, experience, and verification readiness.">
      {toast && <div className="toast-stack"><div className={`toast ${toast.type}`}>{toast.text}</div></div>}

      {actionItem && (
        <div className="drawer-backdrop" onClick={() => setActionItem(null)}>
          <aside className="action-drawer" onClick={(event) => event.stopPropagation()}>
            <p className="eyebrow">Professional actions</p>
            <h2 className="panel-heading">{actionItem.fullName || actionItem.email || 'Professional'}</h2>
            <p className="panel-subtitle">{actionItem.professionalCategory || 'No category set'}</p>
            <div className="stack" style={{ marginTop: 18 }}>
              <button className="btn-secondary" disabled={!actionItem.id} onClick={() => { setEditing(actionItem); setActionItem(null); }}>Edit profile</button>
              <button className="btn-secondary" onClick={() => { setResetting(actionItem); setActionItem(null); }}>Reset password</button>
              <button className="btn-secondary" disabled={!actionItem.id} onClick={() => setVerification(actionItem, 'Pending')}>Initiate verification</button>
              <button className="btn-primary" disabled={!actionItem.id} onClick={() => setVerification(actionItem, 'Verified')}>Mark verified</button>
              <button className="btn-secondary" onClick={() => setActionItem(null)}>Close</button>
            </div>
          </aside>
        </div>
      )}

      {editing && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-card">
            <h2 className="panel-heading">Edit professional</h2>
            <form className="form-grid" style={{ marginTop: 16 }} onSubmit={saveProfessional}>
              <select className="select" value={editing.professionalCategory || ''} onChange={(event) => setEditing({ ...editing, professionalCategory: event.target.value })}>
                <option value="">Select category</option>
                {configuration?.categories?.map((category) => <option key={category.slug} value={category.name}>{category.name}</option>)}
              </select>
              <input className="input" placeholder="Years of experience" type="number" value={editing.yearsOfExperience || 0} onChange={(event) => setEditing({ ...editing, yearsOfExperience: event.target.value })} />
              <div className="button-row">
                <button className="btn-primary" type="submit">Save changes</button>
                <button className="btn-secondary" type="button" onClick={() => setEditing(null)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {resetting && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-card">
            <h2 className="panel-heading">Reset professional password</h2>
            <p className="panel-subtitle">{resetting.fullName || 'Professional'} - {resetting.email || 'No email on profile'}</p>
            <form className="stack" style={{ marginTop: 16 }} onSubmit={resetPassword}>
              <input className="input" type="password" required placeholder="New password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
              <div className="button-row">
                <button className="btn-primary" type="submit" disabled={!resetting.email && !resetting.userId}>Reset password</button>
                <button className="btn-secondary" type="button" onClick={() => setResetting(null)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="panel-card" style={{ marginTop: 22 }}>
        <div className="context-search">
          <input className="input" placeholder="Search professionals by name, email, category, or specialty" value={search} onChange={(event) => setSearch(event.target.value)} />
          <select className="select" value="" onChange={(event) => setSearch(event.target.value)}>
            <option value="">Category filter</option>
            {configuration?.categories?.map((category) => <option key={category.slug} value={category.name}>{category.name}</option>)}
          </select>
          <span>{filteredItems.length} of {items.length} professionals</span>
        </div>
        <table className="table-shell record-table">
          <thead><tr><th>Account</th><th>Profile</th><th>Category</th><th>Experience</th><th>Last login</th><th>Verification</th><th>Actions</th></tr></thead>
          <tbody>
            {filteredItems.map((item) => (
              <tr key={item.userId}>
                <td data-label="Account"><strong>{item.fullName || 'Professional'}</strong><div className="table-note">{item.email}</div></td>
                <td data-label="Profile"><span className={`badge ${item.hasCompletedProfile ? 'verified' : 'pending'}`}>{item.hasCompletedProfile ? 'Complete' : 'Not completed'}</span></td>
                <td data-label="Category">{item.professionalCategory || 'Not set'}</td>
                <td data-label="Experience">{item.yearsOfExperience} years</td>
                <td data-label="Last login">{item.lastLoginAt ? new Date(item.lastLoginAt).toLocaleString() : 'Never'}</td>
                <td data-label="Verification"><span className={`badge ${String(item.verificationStatus).toLowerCase()}`}>{item.verificationStatus}</span></td>
                <td data-label="Actions">
                  <button className="btn-secondary" onClick={() => setActionItem(item)}>Actions</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AdminShell>
  );
}
