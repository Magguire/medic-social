import { useEffect, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

export default function EmployersPage() {
  const [user, setUser] = useState(null);
  const [data, setData] = useState({ items: [] });
  const [configuration, setConfiguration] = useState(null);
  const [editing, setEditing] = useState(null);
  const [resetting, setResetting] = useState(null);
  const [actionItem, setActionItem] = useState(null);
  const [search, setSearch] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [toast, setToast] = useState(null);
  const [subscriptionUpgrade, setSubscriptionUpgrade] = useState(null);

  const showToast = (type, text) => {
    setToast({ type, text });
    setTimeout(() => setToast(null), 3200);
  };

  const load = async () => {
    const [currentUser, employers, config] = await Promise.all([adminApi.getCurrentUser(), adminApi.getEmployers(), adminApi.getConfiguration()]);
    setUser(currentUser);
    setData(employers);
    setConfiguration(config);
  };

  useEffect(() => {
    load().catch(() => undefined);
  }, []);

  const saveEmployer = async (event) => {
    event.preventDefault();
    try {
      await adminApi.updateEmployer(editing.id, editing);
      setEditing(null);
      showToast('success', 'Employer profile updated.');
      await load();
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to update employer.');
    }
  };

  const resetPassword = async (event) => {
    event.preventDefault();
    try {
      await adminApi.adminResetPassword({ userId: resetting.userId, email: resetting.contactEmail, newPassword });
      setResetting(null);
      setNewPassword('');
      showToast('success', 'Employer password reset.');
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to reset employer password.');
    }
  };

  const updateStatus = async (item, verificationStatus) => {
    if (!item.id) {
      showToast('info', 'This account must complete its employer profile before profile verification can begin.');
      return;
    }
    try {
      await adminApi.updateEmployer(item.id, { ...item, verificationStatus });
      setActionItem(null);
      showToast('info', verificationStatus === 'Verified' ? 'Employer marked verified.' : 'Employer verification initiated.');
      await load();
    } catch (requestError) {
      showToast('error', requestError.message || 'Unable to update employer verification.');
    }
  };

  const filteredItems = data.items.filter((item) => {
    const value = `${item.name || ''} ${item.facilityType || ''} ${item.contactEmail || ''} ${item.subscriptionTier || ''}`.toLowerCase();
    return value.includes(search.toLowerCase());
  });

  return (
    <AdminShell user={user} title="Employers" subtitle="Facilities, subscription tiers, and verification progress across the platform.">
      {toast && <div className="toast-stack"><div className={`toast ${toast.type}`}>{toast.text}</div></div>}

      {actionItem && (
        <div className="drawer-backdrop" onClick={() => setActionItem(null)}>
          <aside className="action-drawer" onClick={(event) => event.stopPropagation()}>
            <p className="eyebrow">Employer actions</p>
            <h2 className="panel-heading">{actionItem.name}</h2>
            <p className="panel-subtitle">{actionItem.contactEmail || 'No contact email'}</p>
            <div className="stack" style={{ marginTop: 18 }}>
              <button className="btn-secondary" disabled={!actionItem.id} onClick={() => { setEditing(actionItem); setActionItem(null); }}>Edit profile</button>
              <button className="btn-secondary" onClick={() => { setResetting(actionItem); setActionItem(null); }}>Reset password</button>
              <button className="btn-secondary" disabled={!actionItem.id} onClick={() => updateStatus(actionItem, 'Pending')}>Initiate verification</button>
              <button className="btn-primary" disabled={!actionItem.id} onClick={() => updateStatus(actionItem, 'Verified')}>Mark verified</button>
              <button className="btn-primary" disabled={!actionItem.id} onClick={() => { setSubscriptionUpgrade({ employerId: actionItem.id, planId: '', durationDays: 30, notes: '' }); setActionItem(null); }}>Manage subscription</button>
              <button className="btn-secondary" onClick={() => setActionItem(null)}>Close</button>
            </div>
          </aside>
        </div>
      )}

      {editing && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-card">
            <h2 className="panel-heading">Edit employer</h2>
            <form className="form-grid" style={{ marginTop: 16 }} onSubmit={saveEmployer}>
              <input className="input" placeholder="Facility name" value={editing.name || ''} onChange={(event) => setEditing({ ...editing, name: event.target.value })} />
              <input className="input" placeholder="Facility type" value={editing.facilityType || ''} onChange={(event) => setEditing({ ...editing, facilityType: event.target.value })} />
              <input className="input" placeholder="Contact email" value={editing.contactEmail || ''} onChange={(event) => setEditing({ ...editing, contactEmail: event.target.value })} />
              <input className="input" placeholder="Contact phone" value={editing.contactPhone || ''} onChange={(event) => setEditing({ ...editing, contactPhone: event.target.value })} />
              <select className="select" value={editing.subscriptionTier || ''} onChange={(event) => setEditing({ ...editing, subscriptionTier: event.target.value })}>
                <option value="">Select subscription</option>
                {configuration?.subscriptionPlans?.map((plan) => <option key={plan.slug} value={plan.slug}>{plan.name}</option>)}
              </select>
              <select className="select" value={editing.verificationStatus || 'Pending'} onChange={(event) => setEditing({ ...editing, verificationStatus: event.target.value })}>
                <option value="Pending">Pending</option>
                <option value="Verified">Verified</option>
                <option value="Rejected">Rejected</option>
              </select>
              <input className="input" placeholder="Business registration" value={editing.businessRegistrationNumber || ''} onChange={(event) => setEditing({ ...editing, businessRegistrationNumber: event.target.value })} />
              <input className="input" placeholder="KRA PIN" value={editing.kraPin || ''} onChange={(event) => setEditing({ ...editing, kraPin: event.target.value })} />
              <input className="input" placeholder="Licence number" value={editing.licenseNumber || ''} onChange={(event) => setEditing({ ...editing, licenseNumber: event.target.value })} />
              <textarea className="textarea" placeholder="Address" value={editing.address || ''} onChange={(event) => setEditing({ ...editing, address: event.target.value })} />
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
            <h2 className="panel-heading">Reset employer password</h2>
            <p className="panel-subtitle">{resetting.name} - {resetting.contactEmail}</p>
            <form className="stack" style={{ marginTop: 16 }} onSubmit={resetPassword}>
              <input className="input" type="password" required placeholder="New password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
              <div className="button-row">
                <button className="btn-primary" type="submit">Reset password</button>
                <button className="btn-secondary" type="button" onClick={() => setResetting(null)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {subscriptionUpgrade && (
        <div className="modal-backdrop" role="dialog" aria-modal="true">
          <div className="modal-card">
            <h2 className="panel-heading">Manually provision subscription</h2>
            <p className="panel-subtitle">This action immediately replaces the employer's active subscription and is fully audited.</p>
            <div className="form-grid" style={{ marginTop: 16 }}>
              <label className="field-label">Subscription plan<select className="select" value={subscriptionUpgrade.planId} onChange={(event) => setSubscriptionUpgrade({ ...subscriptionUpgrade, planId: event.target.value })}><option value="">Select plan</option>{configuration?.subscriptionPlans?.map((plan) => <option key={plan.id} value={plan.id}>{plan.name} · {plan.currency} {plan.priceAmount}</option>)}</select></label>
              <label className="field-label">Duration in days<input className="input" type="number" min="1" value={subscriptionUpgrade.durationDays} onChange={(event) => setSubscriptionUpgrade({ ...subscriptionUpgrade, durationDays: Number(event.target.value) })} /></label>
              <label className="field-label" style={{ gridColumn: '1 / -1' }}>Approval notes<textarea className="textarea" value={subscriptionUpgrade.notes} onChange={(event) => setSubscriptionUpgrade({ ...subscriptionUpgrade, notes: event.target.value })} /></label>
            </div>
            <div className="button-row" style={{ marginTop: 16 }}>
              <button className="btn-primary" disabled={!subscriptionUpgrade.planId} onClick={async () => { try { await adminApi.activateSubscription(subscriptionUpgrade); setSubscriptionUpgrade(null); showToast('success', 'Employer subscription activated.'); await load(); } catch (requestError) { showToast('error', requestError.message || 'Unable to activate subscription.'); } }}>Activate subscription</button>
              <button className="btn-secondary" onClick={() => setSubscriptionUpgrade(null)}>Cancel</button>
            </div>
          </div>
        </div>
      )}

      <div className="panel-card" style={{ marginTop: 22 }}>
        <div className="context-search">
          <input className="input" placeholder="Search employers by facility, type, subscription, or email" value={search} onChange={(event) => setSearch(event.target.value)} />
          <span>{filteredItems.length} of {data.items.length} employers</span>
        </div>
        <table className="table-shell">
          <thead><tr><th>Account</th><th>Profile</th><th>Facility type</th><th>Subscription</th><th>Last login</th><th>Verification</th><th>Actions</th></tr></thead>
          <tbody>
            {filteredItems.map((item) => (
              <tr key={item.userId}>
                <td><strong>{item.name || 'Employer'}</strong><div className="table-note">{item.contactEmail}</div></td>
                <td><span className={`badge ${item.hasCompletedProfile ? 'verified' : 'pending'}`}>{item.hasCompletedProfile ? 'Complete' : 'Not completed'}</span></td>
                <td>{item.facilityType || 'Not set'}</td>
                <td>{item.subscriptionTier}</td>
                <td>{item.lastLoginAt ? new Date(item.lastLoginAt).toLocaleString() : 'Never'}</td>
                <td><span className={`badge ${String(item.verificationStatus).toLowerCase()}`}>{item.verificationStatus}</span></td>
                <td>
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
