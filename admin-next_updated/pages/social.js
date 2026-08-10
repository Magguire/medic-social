import { useEffect, useMemo, useState } from 'react';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';
import { useAdminAuth } from '../lib/useAdminAuth';

const defaultFeature = {
  isEnabled: true,
  disabledMessage: 'The community forum is temporarily unavailable.',
};

const mediaOptions = ['text', 'image', 'video', 'file', 'link'];
const userTypeOptions = ['Professional', 'Employer', 'Recruiter', 'Admin'];
const tabs = [
  ['overview', 'Overview'],
  ['posts', 'Posts'],
  ['channels', 'Channels'],
  ['reports', 'Reports'],
  ['profiles', 'Profiles'],
  ['messages', 'Messages'],
];

function safeArray(value) {
  if (value && !Array.isArray(value) && Array.isArray(value.items)) {
    return value.items;
  }
  return Array.isArray(value) ? value : [];
}

function safeObject(value) {
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {};
}

function safeCallback(callback) {
  return typeof callback === 'function' ? callback : () => undefined;
}

function participantLabel(conversation) {
  return safeArray(conversation?.participants)
    .map((item) => safeObject(item).displayName || safeObject(item).username)
    .filter(Boolean)
    .join(' / ') || 'Unknown participants';
}

function formatDate(value) {
  if (!value) return 'Not recorded';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not recorded' : date.toLocaleString();
}

function valueList(values) {
  const items = safeArray(values).filter(Boolean);
  return items.length ? items.join(', ') : 'All';
}

function toggleArray(values, value) {
  const current = safeArray(values);
  return current.includes(value) ? current.filter((item) => item !== value) : [...current, value];
}

function emptyChannelDraft() {
  return {
    id: '',
    name: '',
    description: '',
    isActive: true,
    joinPolicy: 'Anyone',
    postingPolicy: 'Anyone',
    allowedMediaTypes: ['text', 'image', 'link'],
    visibleToUserTypes: [],
    visibleToCategories: [],
    visibleToLocations: '',
  };
}

export default function SocialModerationPage() {
  const { user } = useAdminAuth();
  const [activeTab, setActiveTab] = useState('overview');
  const [overview, setOverview] = useState(null);
  const [reports, setReports] = useState([]);
  const [posts, setPosts] = useState([]);
  const [channels, setChannels] = useState([]);
  const [profiles, setProfiles] = useState([]);
  const [conversations, setConversations] = useState([]);
  const [conversationMessages, setConversationMessages] = useState([]);
  const [selectedConversation, setSelectedConversation] = useState(null);
  const [selectedPostDetail, setSelectedPostDetail] = useState(null);
  const [selectedProfileDetail, setSelectedProfileDetail] = useState(null);
  const [feature, setFeature] = useState(defaultFeature);
  const [filters, setFilters] = useState({
    reportStatus: '',
    postStatus: '',
    channelSlug: '',
    postQuery: '',
    profileRole: '',
    profileQuery: '',
    conversationStatus: '',
  });
  const [messageSearch, setMessageSearch] = useState({ q: '', role: 'Professional' });
  const [messageResults, setMessageResults] = useState([]);
  const [messageDraft, setMessageDraft] = useState('Hello, this is the MedicSocial administration team. We would like to start a conversation.');
  const [channelDraft, setChannelDraft] = useState(emptyChannelDraft());
  const [editingChannel, setEditingChannel] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState('');
  const [error, setError] = useState('');

  async function loadSocial() {
    setLoading(true);
    setError('');
    try {
      const [featureResult, overviewResult, reportsResult, postsResult, channelsResult, profilesResult, conversationsResult] = await Promise.allSettled([
        adminApi.getFeature('social'),
        adminApi.getSocialOverview(),
        adminApi.getSocialReports(filters.reportStatus),
        adminApi.getAdminSocialPosts({ channelSlug: filters.channelSlug, status: filters.postStatus, q: filters.postQuery, pageSize: 30 }),
        adminApi.getAdminSocialChannels(true),
        adminApi.getAdminSocialProfiles({ q: filters.profileQuery, role: filters.profileRole, pageSize: 30 }),
        adminApi.getAdminSocialConversations({ status: filters.conversationStatus, pageSize: 30 }),
      ]);

      if (featureResult.status === 'fulfilled') {
        setFeature({
          isEnabled: Boolean(featureResult.value?.isEnabled),
          disabledMessage: featureResult.value?.disabledMessage || defaultFeature.disabledMessage,
        });
      }
      if (overviewResult.status === 'fulfilled') setOverview(overviewResult.value || null);
      if (reportsResult.status === 'fulfilled') setReports(safeArray(reportsResult.value));
      if (postsResult.status === 'fulfilled') setPosts(safeArray(postsResult.value));
      if (channelsResult.status === 'fulfilled') setChannels(safeArray(channelsResult.value));
      if (profilesResult.status === 'fulfilled') setProfiles(safeArray(profilesResult.value));
      if (conversationsResult.status === 'fulfilled') setConversations(safeArray(conversationsResult.value));

      const failed = [featureResult, overviewResult, reportsResult, postsResult, channelsResult, profilesResult, conversationsResult].find((result) => result.status === 'rejected');
      if (failed) setError(failed.reason?.message || 'Some social data could not be loaded.');
    } catch (err) {
      setError(err?.message || 'Social console could not be loaded.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadSocial();
  }, [filters.reportStatus, filters.postStatus, filters.channelSlug, filters.profileRole, filters.conversationStatus]);

  const stats = useMemo(() => ([
    ['Profiles', 'profiles', overview?.profiles || 0, `${overview?.onlineProfiles || 0} online recently`],
    ['Channels', 'channels', overview?.channels || 0, `${overview?.activeChannels || 0} active`],
    ['Posts', 'posts', overview?.posts || 0, `${overview?.hiddenPosts || 0} hidden`],
    ['Reports', 'reports', overview?.openReports || 0, 'open moderation items'],
    ['Comments', 'posts', overview?.comments || 0, `${overview?.hiddenComments || 0} hidden`],
    ['Messages', 'messages', overview?.conversations || 0, `${overview?.pendingConversations || 0} pending requests`],
  ]), [overview]);

  async function saveFeature() {
    setBusy(true);
    setError('');
    try {
      const updated = await adminApi.updateFeature('social', feature);
      setFeature({ isEnabled: Boolean(updated?.isEnabled), disabledMessage: updated?.disabledMessage || defaultFeature.disabledMessage });
      setToast(updated?.isEnabled ? 'Feed and messaging are enabled.' : 'Feed and messaging are disabled.');
      await loadSocial();
    } catch (err) {
      setError(err?.message || 'Unable to save social availability.');
    } finally {
      setBusy(false);
    }
  }

  async function moderate(report, nextStatus) {
    setBusy(true);
    setError('');
    try {
      const payload = { status: nextStatus, reason: nextStatus === 'Hidden' ? report.reason : 'Reviewed by admin' };
      if (report.targetType === 'post') {
        await adminApi.moderateSocialPost(report.targetId, payload);
      }
      if (report.targetType === 'comment') {
        await adminApi.moderateSocialComment(report.targetId, payload);
      }
      if (report.id) {
        await adminApi.updateSocialReport(report.id, { status: nextStatus === 'Hidden' ? 'Actioned' : 'Reviewed' });
      }
      setToast(`Marked ${report.targetType || 'content'} as ${nextStatus.toLowerCase()}.`);
      await loadSocial();
    } catch (err) {
      setError(err?.message || 'Unable to moderate this report.');
    } finally {
      setBusy(false);
    }
  }

  async function moderatePost(post, nextStatus) {
    setBusy(true);
    setError('');
    try {
      await adminApi.moderateSocialPost(post.id, {
        status: nextStatus,
        reason: nextStatus === 'Hidden' ? 'Hidden by admin moderation.' : 'Restored by admin moderation.',
      });
      setToast(nextStatus === 'Hidden' ? 'Post hidden.' : 'Post restored.');
      await loadSocial();
    } catch (err) {
      setError(err?.message || 'Unable to update post moderation.');
    } finally {
      setBusy(false);
    }
  }

  async function openPostDetails(post) {
    const postId = safeObject(post).id;
    if (!postId) return;
    setBusy(true);
    setError('');
    try {
      setSelectedPostDetail(await adminApi.getAdminSocialPostDetails(postId));
    } catch (err) {
      setError(err?.message || 'Unable to load post details.');
    } finally {
      setBusy(false);
    }
  }

  async function openProfileDetails(profileOrAuthor) {
    const source = safeObject(profileOrAuthor);
    const lookup = source.userId || source.id || source.username;
    if (!lookup) return;
    setBusy(true);
    setError('');
    try {
      setSelectedProfileDetail(await adminApi.getAdminSocialProfileDetails(lookup));
    } catch (err) {
      setError(err?.message || 'Unable to load profile details.');
    } finally {
      setBusy(false);
    }
  }

  function editChannel(channel) {
    setChannelDraft({
      id: channel.id || channel.slug,
      name: channel.name || '',
      description: channel.description || '',
      isActive: Boolean(channel.isActive),
      joinPolicy: channel.joinPolicy || 'Anyone',
      postingPolicy: channel.postingPolicy || 'Anyone',
      allowedMediaTypes: safeArray(channel.allowedMediaTypes).length ? channel.allowedMediaTypes : ['text'],
      visibleToUserTypes: safeArray(channel.visibleToUserTypes),
      visibleToCategories: safeArray(channel.visibleToCategories),
      visibleToLocations: safeArray(channel.visibleToLocations).join(', '),
    });
    setEditingChannel(true);
    setActiveTab('channels');
  }

  async function saveChannel() {
    if (!channelDraft.id) {
      setError('Choose a channel to edit first.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await adminApi.updateAdminSocialChannel(channelDraft.id, {
        ...channelDraft,
        visibleToLocations: channelDraft.visibleToLocations.split(',').map((item) => item.trim()).filter(Boolean),
      });
      setToast('Channel updated.');
      setEditingChannel(false);
      setChannelDraft(emptyChannelDraft());
      await loadSocial();
    } catch (err) {
      setError(err?.message || 'Unable to save channel.');
    } finally {
      setBusy(false);
    }
  }

  async function searchMessageRecipient() {
    setBusy(true);
    setError('');
    setToast('');
    try {
      const results = await adminApi.searchSocialPeople(messageSearch);
      setMessageResults(safeArray(results));
      if (!safeArray(results).length) {
        setToast('No matching account was found.');
      }
    } catch (err) {
      setMessageResults([]);
      setError(err?.message || 'Unable to search message recipients.');
    } finally {
      setBusy(false);
    }
  }

  async function startAdminConversation(person) {
    setBusy(true);
    setError('');
    try {
      const conversation = await adminApi.startSocialConversation({ recipientUserId: person.userId, text: messageDraft, media: [] });
      setToast(`Conversation request sent to ${person.displayName || person.username}.`);
      setSelectedConversation(conversation);
      setMessageResults([]);
      await loadConversationMessages(conversation);
      await loadSocial();
    } catch (err) {
      setError(err?.message || 'Unable to start conversation.');
    } finally {
      setBusy(false);
    }
  }

  async function loadConversationMessages(conversation) {
    if (!conversation?.id) return;
    setSelectedConversation(conversation);
    setError('');
    try {
      const messages = await adminApi.getAdminSocialConversationMessages(conversation.id);
      setConversationMessages(safeArray(messages));
    } catch (err) {
      setConversationMessages([]);
      setError(err?.message || 'Unable to load conversation messages.');
    }
  }

  return (
    <AdminShell user={user} title="Feed Administration" subtitle="Control public Feed availability, channels, posts, social profiles, reports, and direct-message safety.">
      <div className="toast-stack" aria-live="polite">
        {toast && <button className="toast success" onClick={() => setToast('')}>{toast}</button>}
        {error && <button className="toast error" onClick={() => setError('')}>{error}</button>}
      </div>

      <div className="social-admin-hero">
        <div>
          <p className="eyebrow">Social operations</p>
          <h2>Keep the Feed healthy without leaving admin.</h2>
          <p>Disable the module during incidents, review reports, manage channel visibility, and inspect platform social activity from one place.</p>
        </div>
        <div className={`social-status-card ${feature.isEnabled ? 'enabled' : 'disabled'}`}>
          <span>{feature.isEnabled ? 'Enabled' : 'Disabled'}</span>
          <strong>{feature.isEnabled ? 'Feed live' : 'Feed paused'}</strong>
          <small>{feature.disabledMessage || defaultFeature.disabledMessage}</small>
        </div>
      </div>

      <div className="social-tabs" role="tablist" aria-label="Social administration sections">
        {tabs.map(([key, label]) => (
          <button key={key} type="button" className={activeTab === key ? 'active' : ''} onClick={() => setActiveTab(key)}>{label}</button>
        ))}
      </div>

      {loading ? <div className="empty-state">Loading social administration data...</div> : (
        <>
          {activeTab === 'overview' && (
            <div className="social-admin-grid">
              <section className="panel-card social-wide">
                <div className="card-topline">
                  <strong>Global availability</strong>
                  <span>{feature.isEnabled ? 'Live' : 'Paused'}</span>
                </div>
                <p className="panel-subtitle">This switch blocks Feed pages, posting, comments, reactions, uploads, realtime presence, and direct messages when disabled.</p>
                <div className="form-grid compact-grid">
                  <label className="switch-card"><input type="checkbox" checked={feature.isEnabled} onChange={(event) => setFeature({ ...feature, isEnabled: event.target.checked })} /> Social module enabled</label>
                  <label className="field-label">Disabled message<input className="input" value={feature.disabledMessage} onChange={(event) => setFeature({ ...feature, disabledMessage: event.target.value })} /></label>
                </div>
                <div className="button-row" style={{ marginTop: 14 }}>
                  <button className="btn-primary" disabled={busy} onClick={saveFeature}>Save availability</button>
                  <button className="btn-secondary" disabled={busy} onClick={loadSocial}>Refresh</button>
                </div>
              </section>

              <section className="social-metric-grid social-wide">
                {stats.map(([label, targetTab, value, hint]) => (
                  <button key={label} className="social-metric" type="button" onClick={() => setActiveTab(targetTab)}>
                    <span>{label}</span>
                    <strong>{value}</strong>
                    <small>{hint}</small>
                  </button>
                ))}
              </section>

              <section className="panel-card">
                <h2 className="panel-heading">Recent posts</h2>
                <p className="panel-subtitle">Latest content across public and community channels.</p>
                <div className="stack">
                  {safeArray(posts).slice(0, 5).map((post) => <PostCard key={post.id} post={post} onModerate={moderatePost} onOpenDetails={openPostDetails} onOpenProfile={openProfileDetails} />)}
                  {safeArray(posts).length === 0 && <div className="empty-state">No posts have been created yet.</div>}
                </div>
              </section>

              <section className="panel-card">
                <h2 className="panel-heading">Open reports</h2>
                <p className="panel-subtitle">Reports awaiting moderation.</p>
                <div className="stack">
                  {safeArray(reports).filter((report) => report.status === 'Open').slice(0, 5).map((report) => <ReportCard key={report.id || report.targetId} report={report} onModerate={moderate} />)}
                  {safeArray(reports).filter((report) => report.status === 'Open').length === 0 && <div className="empty-state">No open reports.</div>}
                </div>
              </section>
            </div>
          )}

          {activeTab === 'posts' && (
            <section className="panel-card">
              <div className="admin-toolbar social-toolbar">
                <input className="input" placeholder="Search post text, author, or username" value={filters.postQuery} onChange={(event) => setFilters({ ...filters, postQuery: event.target.value })} onKeyDown={(event) => { if (event.key === 'Enter') loadSocial(); }} />
                <select className="select" value={filters.channelSlug} onChange={(event) => setFilters({ ...filters, channelSlug: event.target.value })}>
                  <option value="">All channels</option>
                  <option value="global">Global Feed</option>
                  {safeArray(channels).map((channel) => <option key={channel.slug} value={channel.slug}>{channel.name}</option>)}
                </select>
                <select className="select" value={filters.postStatus} onChange={(event) => setFilters({ ...filters, postStatus: event.target.value })}>
                  <option value="">All moderation states</option>
                  <option value="Visible">Visible</option>
                  <option value="Hidden">Hidden</option>
                </select>
                <button className="btn-secondary" onClick={loadSocial}>Search</button>
              </div>
              <div className="social-post-list">
                {safeArray(posts).map((post) => <PostCard key={post.id} post={post} onModerate={moderatePost} onOpenDetails={openPostDetails} onOpenProfile={openProfileDetails} />)}
                {safeArray(posts).length === 0 && <div className="empty-state">No posts match this filter.</div>}
              </div>
            </section>
          )}

          {activeTab === 'channels' && (
            <section className="social-admin-grid">
              <div className="panel-card">
                <h2 className="panel-heading">Channels and communities</h2>
                <p className="panel-subtitle">Click a channel to edit access, posting policy, media rules, and visibility.</p>
                <div className="stack">
                  {safeArray(channels).map((channel) => (
                    <button key={channel.id || channel.slug} type="button" className="config-list-card interactive social-channel-card" onClick={() => editChannel(channel)}>
                      <strong>{channel.name}</strong>
                      <span>{channel.description || 'No description yet.'}</span>
                      <small>{channel.isActive ? 'Active' : 'Inactive'} / {channel.joinPolicy} / {channel.postingPolicy}</small>
                    </button>
                  ))}
                  {safeArray(channels).length === 0 && <div className="empty-state">No custom channels have been created yet.</div>}
                </div>
              </div>

              <div className="panel-card">
                <h2 className="panel-heading">{editingChannel ? 'Edit channel' : 'Choose a channel'}</h2>
                <p className="panel-subtitle">Admins can deactivate a channel without deleting its history.</p>
                {editingChannel ? (
                  <div className="stack">
                    <label className="field-label">Channel name<input className="input" value={channelDraft.name} onChange={(event) => setChannelDraft({ ...channelDraft, name: event.target.value })} /></label>
                    <label className="field-label">Description<textarea className="textarea" value={channelDraft.description} onChange={(event) => setChannelDraft({ ...channelDraft, description: event.target.value })} /></label>
                    <div className="form-grid compact-grid">
                      <label className="field-label">Join policy<select className="select" value={channelDraft.joinPolicy} onChange={(event) => setChannelDraft({ ...channelDraft, joinPolicy: event.target.value })}><option>Anyone</option><option>InviteOnly</option></select></label>
                      <label className="field-label">Posting policy<select className="select" value={channelDraft.postingPolicy} onChange={(event) => setChannelDraft({ ...channelDraft, postingPolicy: event.target.value })}><option>Anyone</option><option>AdminsOnly</option></select></label>
                    </div>
                    <label className="switch-card"><input type="checkbox" checked={channelDraft.isActive} onChange={(event) => setChannelDraft({ ...channelDraft, isActive: event.target.checked })} /> Channel active</label>
                    <div>
                      <strong>Allowed media</strong>
                      <div className="choice-grid tight">
                        {mediaOptions.map((item) => <button key={item} type="button" className={channelDraft.allowedMediaTypes.includes(item) ? 'choice-card selected' : 'choice-card'} onClick={() => setChannelDraft({ ...channelDraft, allowedMediaTypes: toggleArray(channelDraft.allowedMediaTypes, item) })}>{item}</button>)}
                      </div>
                    </div>
                    <div>
                      <strong>Visible user types</strong>
                      <div className="choice-grid tight">
                        {userTypeOptions.map((item) => <button key={item} type="button" className={channelDraft.visibleToUserTypes.includes(item) ? 'choice-card selected' : 'choice-card'} onClick={() => setChannelDraft({ ...channelDraft, visibleToUserTypes: toggleArray(channelDraft.visibleToUserTypes, item) })}>{item}</button>)}
                      </div>
                    </div>
                    <label className="field-label">Visible locations<input className="input" placeholder="Comma-separated locations or leave blank for all" value={channelDraft.visibleToLocations} onChange={(event) => setChannelDraft({ ...channelDraft, visibleToLocations: event.target.value })} /></label>
                    <div className="button-row">
                      <button className="btn-primary" disabled={busy} onClick={saveChannel}>Save channel</button>
                      <button className="btn-secondary" disabled={busy} onClick={() => { setEditingChannel(false); setChannelDraft(emptyChannelDraft()); }}>Cancel</button>
                    </div>
                  </div>
                ) : <div className="empty-state">Select any channel card to edit its configuration.</div>}
              </div>
            </section>
          )}

          {activeTab === 'reports' && (
            <section className="panel-card">
              <div className="admin-toolbar social-toolbar">
                <select className="select" value={filters.reportStatus} onChange={(event) => setFilters({ ...filters, reportStatus: event.target.value })}>
                  <option value="">All reports</option>
                  <option value="Open">Open</option>
                  <option value="Reviewed">Reviewed</option>
                  <option value="Actioned">Actioned</option>
                </select>
                <button className="btn-secondary" onClick={loadSocial}>Refresh</button>
              </div>
              <div className="config-list" style={{ marginTop: 20 }}>
                {safeArray(reports).map((report) => <ReportCard key={report.id || report.targetId} report={report} onModerate={moderate} />)}
                {safeArray(reports).length === 0 && <div className="empty-state">No social reports match this filter.</div>}
              </div>
            </section>
          )}

          {activeTab === 'profiles' && (
            <section className="panel-card">
              <div className="admin-toolbar social-toolbar">
                <input className="input" placeholder="Search username, display name, or bio" value={filters.profileQuery} onChange={(event) => setFilters({ ...filters, profileQuery: event.target.value })} onKeyDown={(event) => { if (event.key === 'Enter') loadSocial(); }} />
                <select className="select" value={filters.profileRole} onChange={(event) => setFilters({ ...filters, profileRole: event.target.value })}>
                  <option value="">All roles</option>
                  {userTypeOptions.map((item) => <option key={item}>{item}</option>)}
                </select>
                <button className="btn-secondary" onClick={loadSocial}>Search</button>
              </div>
              <div className="social-profile-grid">
                {safeArray(profiles).map((profile) => <ProfileCard key={profile.id || profile.userId} profile={profile} onOpenDetails={openProfileDetails} />)}
                {safeArray(profiles).length === 0 && <div className="empty-state">No social profiles match this filter.</div>}
              </div>
            </section>
          )}

          {activeTab === 'messages' && (
            <section className="social-admin-grid">
              <div className="panel-card">
                <h2 className="panel-heading">Start admin conversation</h2>
                <p className="panel-subtitle">Search by exact email or phone. Admins can contact employers, recruiters, and professionals.</p>
                <div className="stack">
                  <div className="form-grid compact-grid">
                    <label className="field-label">Email or phone<input className="input" value={messageSearch.q} onChange={(event) => setMessageSearch({ ...messageSearch, q: event.target.value })} onKeyDown={(event) => { if (event.key === 'Enter') searchMessageRecipient(); }} /></label>
                    <label className="field-label">Account type<select className="select" value={messageSearch.role} onChange={(event) => setMessageSearch({ ...messageSearch, role: event.target.value })}><option>Professional</option><option>Employer</option><option>Recruiter</option></select></label>
                  </div>
                  <label className="field-label">Introductory message<textarea className="textarea" value={messageDraft} onChange={(event) => setMessageDraft(event.target.value)} /></label>
                  <button className="btn-primary" disabled={busy} onClick={searchMessageRecipient}>Find recipient</button>
                  <div className="stack">
                    {safeArray(messageResults).map((person) => (
                      <button key={person.userId} type="button" className="config-list-card interactive social-channel-card" onClick={() => startAdminConversation(person)}>
                        <strong>{person.displayName || person.username}</strong>
                        <span>@{person.username} / {person.userType} / {person.email || person.phoneNumber || 'verified contact'}</span>
                        <small>{person.status || 'Offline'}</small>
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              <div className="panel-card social-wide">
                <div className="admin-toolbar social-toolbar">
                  <select className="select" value={filters.conversationStatus} onChange={(event) => setFilters({ ...filters, conversationStatus: event.target.value })}>
                    <option value="">All conversation states</option>
                    <option value="Pending">Pending</option>
                    <option value="Accepted">Accepted</option>
                  </select>
                  <button className="btn-secondary" onClick={loadSocial}>Refresh</button>
                </div>
                <div className="conversation-admin-layout">
                  <div className="conversation-list">
                    {safeArray(conversations).map((conversation) => (
                      <button key={conversation.id} type="button" className={selectedConversation?.id === conversation.id ? 'conversation-row active' : 'conversation-row'} onClick={() => loadConversationMessages(conversation)}>
                        <strong>{participantLabel(conversation)}</strong>
                        <span>{conversation.lastMessagePreview || 'No message preview'}</span>
                        <small>{conversation.status || 'Unknown'} / {formatDate(conversation.updatedAt)}</small>
                      </button>
                    ))}
                    {safeArray(conversations).length === 0 && <div className="empty-state">No conversations match this filter.</div>}
                  </div>
                  <div className="conversation-detail">
                    {selectedConversation ? (
                      <>
                        <div className="card-topline">
                          <strong>{participantLabel(selectedConversation)}</strong>
                          <span>{selectedConversation.status}</span>
                        </div>
                        <div className="message-transcript">
                          {safeArray(conversationMessages).map((message) => (
                            <div key={message.id} className="admin-message-bubble">
                              <strong>{message.sender?.displayName || message.sender?.username || 'Unknown sender'}</strong>
                              <p>{message.text || 'Media attachment'}</p>
                              {safeArray(message.media).length > 0 && <small>{safeArray(message.media).length} attachment(s)</small>}
                              <small>{message.isRead ? 'Read' : message.deliveryStatus === 'DeliveredOnline' ? 'Delivered, recipient online' : 'Delivered'} / {formatDate(message.createdAt)}</small>
                            </div>
                          ))}
                          {safeArray(conversationMessages).length === 0 && <div className="empty-state">No messages have been sent yet.</div>}
                        </div>
                      </>
                    ) : (
                      <div className="empty-state">Select a conversation to inspect the message history.</div>
                    )}
                  </div>
                </div>
              </div>
            </section>
          )}
        </>
      )}
      {selectedPostDetail && (
        <PostDetailModal
          detail={selectedPostDetail}
          onClose={() => setSelectedPostDetail(null)}
          onModeratePost={moderatePost}
          onOpenProfile={openProfileDetails}
        />
      )}
      {selectedProfileDetail && (
        <ProfileDetailModal
          detail={selectedProfileDetail}
          onClose={() => setSelectedProfileDetail(null)}
          onOpenPost={openPostDetails}
        />
      )}
    </AdminShell>
  );
}

function PostCard({ post, onModerate, onOpenDetails, onOpenProfile }) {
  const postValue = safeObject(post);
  const author = safeObject(postValue.author);
  const moderatePost = safeCallback(onModerate);
  const openDetails = safeCallback(onOpenDetails);
  const openProfile = safeCallback(onOpenProfile);
  return (
    <article className="social-post-card">
      <div className="social-post-head">
        <div className="sidebar-avatar">{(author.displayName || author.username || '?').slice(0, 1).toUpperCase()}</div>
        <button type="button" className="link-button text-left" onClick={() => openProfile(author)}>
          <strong>{author.displayName || author.username || 'Unknown author'}</strong>
          <span>@{author.username || 'unknown'} / {author.role || 'Member'} / {postValue.channelSlug || 'global'}</span>
        </button>
        <span className={`badge ${postValue.isHidden ? 'rejected' : 'approved'}`}>{postValue.isHidden ? 'Hidden' : postValue.moderationStatus || 'Visible'}</span>
      </div>
      <p>{postValue.text || 'Media-only post'}</p>
      {safeArray(postValue.links).length > 0 && <small>Links: {safeArray(postValue.links).join(', ')}</small>}
      {safeArray(postValue.media).length > 0 && <small>Media: {safeArray(postValue.media).length} attachment(s)</small>}
      <div className="social-post-meta">
        <span>{postValue.likeCount || 0} likes</span>
        <span>{postValue.upvoteCount || 0} upvotes</span>
        <span>{postValue.commentCount || 0} comments</span>
        <span>{formatDate(postValue.createdAt)}</span>
      </div>
      <div className="button-row">
        <button className="btn-secondary" onClick={() => openDetails(postValue)}>View details</button>
        {postValue.isHidden ? (
          <button className="btn-secondary" onClick={() => moderatePost(postValue, 'Visible')}>Restore</button>
        ) : (
          <button className="btn-danger" onClick={() => moderatePost(postValue, 'Hidden')}>Hide post</button>
        )}
      </div>
    </article>
  );
}

function ReportCard({ report, onModerate }) {
  const reportValue = safeObject(report);
  const moderateReport = safeCallback(onModerate);
  return (
    <div className="config-card">
      <div className="card-topline">
        <strong>{reportValue.targetType || 'content'} report</strong>
        <span>{reportValue.status || 'Open'}</span>
      </div>
      <p>{reportValue.reason || 'No reason supplied.'}</p>
      <small>Target: {reportValue.targetId || 'Unknown'} / Reporter: {reportValue.reporterUserId || 'Guest or unavailable'} / {formatDate(reportValue.createdAt)}</small>
      <div className="button-row" style={{ marginTop: 12 }}>
        <button className="btn-danger" onClick={() => moderateReport(reportValue, 'Hidden')}>Hide content</button>
        <button className="btn-secondary" onClick={() => moderateReport(reportValue, 'Visible')}>Keep visible</button>
      </div>
    </div>
  );
}

function ProfileCard({ profile, onOpenDetails }) {
  const profileValue = safeObject(profile);
  const openDetails = safeCallback(onOpenDetails);
  return (
    <article className="social-profile-card interactive" role="button" tabIndex={0} onClick={() => openDetails(profileValue)} onKeyDown={(event) => { if (event.key === 'Enter') openDetails(profileValue); }}>
      <div className="social-post-head">
        <div className="sidebar-avatar">{profileValue.avatarUrl ? <img src={profileValue.avatarUrl} alt="" /> : (profileValue.displayName || profileValue.username || '?').slice(0, 1).toUpperCase()}</div>
        <div>
          <strong>{profileValue.displayName || profileValue.username || 'Unnamed profile'}</strong>
          <span>@{profileValue.username || 'unknown'} / {profileValue.role || 'Member'}</span>
        </div>
      </div>
      <p>{profileValue.bio || 'No profile bio yet.'}</p>
      <small>Status: {profileValue.status || 'Unknown'} / Last seen: {formatDate(profileValue.lastSeenAt)}</small>
    </article>
  );
}

function DetailOverlay({ title, subtitle, onClose, children }) {
  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <section className="modal-panel social-detail-modal" role="dialog" aria-modal="true" aria-label={title} onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <p className="eyebrow">Admin drill-down</p>
            <h2>{title}</h2>
            {subtitle && <p className="panel-subtitle">{subtitle}</p>}
          </div>
          <button className="btn-secondary" type="button" onClick={onClose}>Close</button>
        </div>
        {children}
      </section>
    </div>
  );
}

function MetadataGrid({ metadata }) {
  const value = safeObject(metadata);
  const rows = [
    ['Device', value.deviceId || 'Not captured'],
    ['IP address', value.ipAddress || 'Not captured'],
    ['Client source', value.source || 'web'],
    ['User agent', value.userAgent || 'Not captured'],
  ];
  return (
    <div className="detail-grid">
      {rows.map(([label, item]) => (
        <div key={label} className="detail-cell">
          <span>{label}</span>
          <strong>{item}</strong>
        </div>
      ))}
    </div>
  );
}

function PostDetailModal({ detail, onClose, onModeratePost, onOpenProfile }) {
  const post = safeObject(detail?.post);
  const author = safeObject(post.author);
  const comments = safeArray(detail?.comments);
  const reactions = safeArray(detail?.reactions);
  const reports = safeArray(detail?.reports);
  const moderatePost = safeCallback(onModeratePost);
  const openProfile = safeCallback(onOpenProfile);

  return (
    <DetailOverlay title="Post details" subtitle={`${author.displayName || author.username || 'Unknown author'} / ${post.channelSlug || 'global'} / ${formatDate(post.createdAt)}`} onClose={onClose}>
      <div className="social-detail-section">
        <div className="card-topline">
          <button type="button" className="link-button" onClick={() => openProfile(author)}><strong>{author.displayName || author.username || 'Unknown author'}</strong></button>
          <span className={`badge ${post.isHidden ? 'rejected' : 'approved'}`}>{post.isHidden ? 'Hidden' : post.moderationStatus || 'Visible'}</span>
        </div>
        <p className="detail-text">{post.text || 'Media-only post'}</p>
        <div className="social-post-meta">
          <span>{post.likeCount || 0} likes</span>
          <span>{post.upvoteCount || 0} upvotes</span>
          <span>{post.commentCount || 0} comments</span>
          <span>Updated {formatDate(post.updatedAt)}</span>
        </div>
        {safeArray(post.links).length > 0 && <p><strong>Links:</strong> {safeArray(post.links).join(', ')}</p>}
        {safeArray(post.media).length > 0 && (
          <div className="detail-list">
            <h3>Media attachments</h3>
            {safeArray(post.media).map((media, index) => <p key={`${media.url}-${index}`}>{media.fileName || media.url} / {media.contentType || media.mediaType} / {media.sizeBytes || 0} bytes</p>)}
          </div>
        )}
        <h3>Captured device and request context</h3>
        <MetadataGrid metadata={post.requestMetadata} />
        <div className="button-row">
          {post.isHidden ? <button className="btn-secondary" onClick={() => moderatePost(post, 'Visible')}>Restore post</button> : <button className="btn-danger" onClick={() => moderatePost(post, 'Hidden')}>Hide post</button>}
        </div>
      </div>

      <div className="social-detail-columns">
        <div className="detail-list">
          <h3>Comments</h3>
          {comments.map((comment) => {
            const commentAuthor = safeObject(comment.author);
            return (
              <article key={comment.id} className="config-card">
                <div className="card-topline">
                  <button type="button" className="link-button" onClick={() => openProfile(commentAuthor)}><strong>{commentAuthor.displayName || commentAuthor.username || 'Unknown commenter'}</strong></button>
                  <span className={`badge ${comment.isHidden ? 'rejected' : 'approved'}`}>{comment.isHidden ? 'Hidden' : comment.moderationStatus || 'Visible'}</span>
                </div>
                <p>{comment.text || 'Media-only comment'}</p>
                <small>{comment.likeCount || 0} likes / {comment.upvoteCount || 0} upvotes / {formatDate(comment.createdAt)}</small>
                <MetadataGrid metadata={comment.requestMetadata} />
              </article>
            );
          })}
          {comments.length === 0 && <div className="empty-state">No comments on this post.</div>}
        </div>
        <div className="detail-list">
          <h3>Reactions and reports</h3>
          {reactions.map((reaction) => {
            const user = safeObject(reaction.user);
            return (
              <article key={reaction.id} className="config-card">
                <strong>{reaction.reactionType || 'reaction'} on {reaction.targetType}</strong>
                <p>{user.displayName || user.username || 'Unknown user'} / {formatDate(reaction.createdAt)}</p>
                <MetadataGrid metadata={reaction.requestMetadata} />
              </article>
            );
          })}
          {reactions.length === 0 && <div className="empty-state">No reactions captured.</div>}
          {reports.map((report) => <p key={report.id} className="badge rejected">{report.reason || 'Reported'} / {report.status || 'Open'} / {formatDate(report.createdAt)}</p>)}
        </div>
      </div>
    </DetailOverlay>
  );
}

function ProfileDetailModal({ detail, onClose, onOpenPost }) {
  const profile = safeObject(detail?.profile);
  const posts = safeArray(detail?.posts);
  const comments = safeArray(detail?.comments);
  const conversations = safeArray(detail?.conversations);
  const openPost = safeCallback(onOpenPost);

  return (
    <DetailOverlay title={profile.displayName || profile.username || 'Social profile'} subtitle={`@${profile.username || 'unknown'} / ${profile.role || 'Member'} / ${profile.status || 'Unknown'}`} onClose={onClose}>
      <div className="social-detail-section">
        <div className="social-post-head">
          <div className="sidebar-avatar">{profile.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profile.displayName || profile.username || '?').slice(0, 1).toUpperCase()}</div>
          <div>
            <strong>{profile.displayName || profile.username || 'Unnamed profile'}</strong>
            <span>Last seen {formatDate(profile.lastSeenAt)}</span>
          </div>
        </div>
        <p>{profile.bio || 'No profile bio yet.'}</p>
        <div className="detail-grid">
          <div className="detail-cell"><span>Posts</span><strong>{posts.length}</strong></div>
          <div className="detail-cell"><span>Comments</span><strong>{comments.length}</strong></div>
          <div className="detail-cell"><span>Conversations</span><strong>{conversations.length}</strong></div>
        </div>
      </div>
      <div className="social-detail-columns">
        <div className="detail-list">
          <h3>Recent posts</h3>
          {posts.map((post) => (
            <button key={post.id} type="button" className="config-list-card interactive" onClick={() => openPost(post)}>
              <strong>{post.text || 'Media-only post'}</strong>
              <span>{post.channelSlug || 'global'} / {formatDate(post.createdAt)}</span>
            </button>
          ))}
          {posts.length === 0 && <div className="empty-state">No posts by this profile.</div>}
        </div>
        <div className="detail-list">
          <h3>Recent comments and conversations</h3>
          {comments.map((comment) => <p key={comment.id} className="config-card">{comment.text || 'Media-only comment'} / {formatDate(comment.createdAt)}</p>)}
          {conversations.map((conversation) => <p key={conversation.id} className="config-card">{participantLabel(conversation)} / {conversation.status} / {formatDate(conversation.updatedAt)}</p>)}
          {comments.length === 0 && conversations.length === 0 && <div className="empty-state">No comments or conversations found.</div>}
        </div>
      </div>
    </DetailOverlay>
  );
}
