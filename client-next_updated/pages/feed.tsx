import AddPhotoAlternateIcon from '@mui/icons-material/AddPhotoAlternate';
import CloseIcon from '@mui/icons-material/Close';
import EditIcon from '@mui/icons-material/Edit';
import GroupsIcon from '@mui/icons-material/Groups';
import LinkIcon from '@mui/icons-material/Link';
import LockIcon from '@mui/icons-material/Lock';
import ModeCommentOutlinedIcon from '@mui/icons-material/ModeCommentOutlined';
import PublicIcon from '@mui/icons-material/Public';
import SendIcon from '@mui/icons-material/Send';
import ThumbUpAltOutlinedIcon from '@mui/icons-material/ThumbUpAltOutlined';
import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { professionalApi } from '../lib/professionalApi';
import { socialApi, SocialAuthor, SocialChannel, SocialComment, SocialConversation, SocialMediaAsset, SocialMessage, SocialPost, SocialProfile } from '../lib/socialApi';
import { useAuth } from '../lib/useAuth';
import type { ProfessionalCategory } from '../types';

const statusOptions = ['Available', 'Busy', 'Open to chat', 'Hiring', 'Offline'];
const mediaOptions = ['text', 'image', 'video', 'file', 'link'];
const userTypeOptions = ['Professional', 'Employer'];

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
}

function authorInitial(post: SocialPost) {
  return (post.author.displayName || post.author.username || '?').slice(0, 1).toUpperCase();
}

function roleLabel(post: SocialPost) {
  if (post.author.isOrganization) return 'Organization';
  if (post.author.role === 'Professional') return 'Professional';
  return post.author.role || 'Member';
}

function toggleValue(values: string[], value: string) {
  return values.includes(value) ? values.filter((item) => item !== value) : [...values, value];
}

function safeArray<T>(value: unknown): T[] {
  if (Array.isArray(value)) return value as T[];
  if (value && typeof value === 'object' && Array.isArray((value as { items?: T[] }).items)) return (value as { items: T[] }).items;
  return [];
}

function MessageTicks({ message, mine }: { message: SocialMessage; mine: boolean }) {
  if (!mine) return null;
  if (message.isRead || message.deliveryStatus === 'Read') {
    return <small className="message-ticks read">✓✓</small>;
  }
  if (message.deliveryStatus === 'DeliveredOnline') {
    return <small className="message-ticks">✓✓</small>;
  }
  return <small className="message-ticks">✓</small>;
}

export default function FeedPage() {
  const router = useRouter();
  const { user, isAuthenticated, hydrated } = useAuth();
  const [channels, setChannels] = useState<SocialChannel[]>([]);
  const [activeChannel, setActiveChannel] = useState('all');
  const [posts, setPosts] = useState<SocialPost[]>([]);
  const [profile, setProfile] = useState<SocialProfile | null>(null);
  const [categories, setCategories] = useState<ProfessionalCategory[]>([]);
  const [profileDraft, setProfileDraft] = useState({ username: '', displayName: '', avatarUrl: '', status: 'Available', bio: '' });
  const [composer, setComposer] = useState({ text: '', links: '', media: [] as SocialMediaAsset[] });
  const [channelDraft, setChannelDraft] = useState({
    name: '',
    description: '',
    joinPolicy: 'Anyone',
    postingPolicy: 'Anyone',
    allowedMediaTypes: ['text', 'image', 'link'],
    visibleToUserTypes: [] as string[],
    visibleToCategories: [] as string[],
    visibleToLocations: '',
  });
  const [comments, setComments] = useState<Record<string, SocialComment[]>>({});
  const [commentDrafts, setCommentDrafts] = useState<Record<string, string>>({});
  const [conversations, setConversations] = useState<SocialConversation[]>([]);
  const [activeConversationId, setActiveConversationId] = useState('');
  const [messages, setMessages] = useState<SocialMessage[]>([]);
  const [messageDraft, setMessageDraft] = useState('');
  const [messageMedia, setMessageMedia] = useState<SocialMediaAsset[]>([]);
  const [toast, setToast] = useState('');
  const [disabledMessage, setDisabledMessage] = useState('');
  const [showChannelForm, setShowChannelForm] = useState(false);
  const [showIdentityEditor, setShowIdentityEditor] = useState(false);
  const [showMessages, setShowMessages] = useState(false);
  const [guestTag, setGuestTag] = useState('guest-reader');

  useEffect(() => {
    if (router.query.messages === 'open') {
      setShowMessages(true);
    }
  }, [router.query.messages]);

  useEffect(() => {
    const existing = window.localStorage.getItem('medsocial.social.guestTag');
    if (existing) {
      setGuestTag(existing);
      return;
    }

    const tag = `guest-${Math.random().toString(36).slice(2, 8)}`;
    window.localStorage.setItem('medsocial.social.guestTag', tag);
    setGuestTag(tag);
  }, []);

  useEffect(() => {
    professionalApi.getCategories().then((items) => setCategories(safeArray(items))).catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    socialApi.channels().then((items) => {
      setDisabledMessage('');
      setChannels(safeArray(items));
    }).catch((error: any) => {
      setChannels([]);
      setDisabledMessage(error.response?.data?.errors?.[0] || error.message || 'Feed is unavailable.');
    });
  }, []);

  useEffect(() => {
    socialApi.feed(activeChannel).then((items) => {
      setDisabledMessage('');
      setPosts(safeArray(items));
    }).catch((error: any) => {
      setPosts([]);
      setDisabledMessage(error.response?.data?.errors?.[0] || error.message || 'Feed is unavailable.');
    });
  }, [activeChannel]);

  useEffect(() => {
    if (!hydrated || !isAuthenticated) return;
    socialApi.myProfile()
      .then((value) => {
        setProfile(value);
        setProfileDraft({ username: value.username, displayName: value.displayName, avatarUrl: value.avatarUrl, status: value.status, bio: value.bio });
        setShowIdentityEditor(false);
      })
      .catch(() => {
        const suggested = user?.email?.split('@')[0]?.replace(/[^a-z0-9._-]/gi, '').toLowerCase() || '';
        setProfileDraft((current) => ({ ...current, username: suggested, displayName: `${user?.firstName || ''} ${user?.lastName || ''}`.trim() }));
      });
    socialApi.conversations().then((items) => setConversations(safeArray(items))).catch(() => setConversations([]));
  }, [hydrated, isAuthenticated, user]);

  useEffect(() => {
    const connection = socialApi.connectRealtime();
    connection.on('postCreated', (post: SocialPost) => setPosts((current) => current.some((item) => item.id === post.id) ? current : [post, ...current]));
    connection.on('postUpdated', (post: SocialPost) => setPosts((current) => current.map((item) => item.id === post.id ? post : item)));
    connection.on('commentCreated', (comment: SocialComment) => setComments((current) => ({ ...current, [comment.postId]: [...(current[comment.postId] || []), comment] })));
    connection.on('channelCreated', (channel: SocialChannel) => setChannels((current) => current.some((item) => item.slug === channel.slug) ? current : [channel, ...current]));
    connection.on('conversationUpdated', (conversation: SocialConversation) => setConversations((current) => [conversation, ...current.filter((item) => item.id !== conversation.id)]));
    connection.on('conversationRead', (conversation: SocialConversation) => setConversations((current) => current.map((item) => item.id === conversation.id ? { ...item, ...conversation } : item)));
    connection.on('messageCreated', (message: SocialMessage) => {
      if (message.conversationId === activeConversationId) {
        setMessages((current) => [...current, message]);
        if (message.senderUserId !== user?.id) {
          socialApi.markConversationRead(message.conversationId).then((conversation) => {
            setConversations((current) => current.map((item) => item.id === conversation.id ? conversation : item));
          }).catch(() => undefined);
        }
      } else if (message.senderUserId !== user?.id) {
        setConversations((current) => current.map((conversation) => conversation.id === message.conversationId
          ? { ...conversation, lastMessagePreview: message.text || 'Media attachment', unreadCount: Number(conversation.unreadCount || 0) + 1, updatedAt: message.createdAt }
          : conversation));
        window.dispatchEvent(new Event('medsocial-notifications-refresh'));
      }
    });
    connection.start().then(async () => {
      await connection.invoke('JoinFeed', activeChannel).catch(() => undefined);
      if (activeConversationId) {
        await connection.invoke('JoinConversation', activeConversationId).catch(() => undefined);
      }
    }).catch(() => undefined);
    return () => {
      connection.stop().catch(() => undefined);
    };
  }, [activeChannel, activeConversationId, user?.id]);

  useEffect(() => {
    if (!activeConversationId) {
      setMessages([]);
      return;
    }
    socialApi.messages(activeConversationId)
      .then((items) => {
        setMessages(safeArray(items));
        return socialApi.markConversationRead(activeConversationId);
      })
      .then((conversation) => setConversations((current) => current.map((item) => item.id === conversation.id ? conversation : item)))
      .catch(() => setMessages([]));
  }, [activeConversationId]);

  const activeConversation = conversations.find((item) => item.id === activeConversationId);
  const selectedChannel = channels.find((item) => item.slug === activeChannel);
  const canPostHere = activeChannel === 'all' || selectedChannel?.postingPolicy !== 'AdminsOnly' || selectedChannel.adminUserIds?.includes(user?.id || '');
  const unreadMessages = conversations.reduce((total, conversation) => total + Number(conversation.unreadCount || 0), 0);

  async function saveProfile() {
    try {
      const saved = await socialApi.saveProfile(profileDraft);
      setProfile(saved);
      setShowIdentityEditor(false);
      setToast('Feed profile saved.');
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to save profile.');
    }
  }

  async function uploadAvatar(files: FileList | null) {
    const file = files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      setToast('Avatar must be an image.');
      return;
    }
    try {
      const uploaded = await socialApi.uploadMedia(file);
      setProfileDraft((current) => ({ ...current, avatarUrl: uploaded.url }));
      setToast('Avatar uploaded. Save identity to apply it.');
    } catch (error: any) {
      setToast(error.message || 'Avatar upload failed.');
    }
  }

  async function createChannel() {
    try {
      const created = await socialApi.createChannel({
        ...channelDraft,
        visibleToLocations: channelDraft.visibleToLocations.split(',').map((item) => item.trim()).filter(Boolean),
      });
      setChannels((current) => [created, ...current.filter((item) => item.slug !== created.slug)]);
      setActiveChannel(created.slug);
      setShowChannelForm(false);
      setChannelDraft({ name: '', description: '', joinPolicy: 'Anyone', postingPolicy: 'Anyone', allowedMediaTypes: ['text', 'image', 'link'], visibleToUserTypes: [], visibleToCategories: [], visibleToLocations: '' });
      setToast('Channel created.');
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to create channel.');
    }
  }

  async function upload(files: FileList | null, target: 'post' | 'message') {
    if (!files?.length) return;
    try {
      const uploaded = await Promise.all(Array.from(files).map((file) => socialApi.uploadMedia(file)));
      if (target === 'post') {
        setComposer((current) => ({ ...current, media: [...current.media, ...uploaded] }));
      } else {
        setMessageMedia((current) => [...current, ...uploaded]);
      }
      setToast(`${uploaded.length} media item${uploaded.length === 1 ? '' : 's'} attached.`);
    } catch (error: any) {
      setToast(error.message || 'Upload failed.');
    }
  }

  async function createPost() {
    if (!isAuthenticated) {
      setToast('Sign in to post to the feed.');
      return;
    }
    if (!composer.text.trim() && composer.media.length === 0 && !composer.links.trim()) return;
    if (!canPostHere) {
      setToast('Only channel admins can post in this channel.');
      return;
    }
    try {
      const created = await socialApi.createPost({
        channelSlug: activeChannel === 'all' ? 'global' : activeChannel,
        text: composer.text,
        links: composer.links.split(/\s+/).filter(Boolean),
        media: composer.media,
      });
      setPosts((current) => [created, ...current]);
      setComposer({ text: '', links: '', media: [] });
      setToast('Posted to Feed.');
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to create post.');
    }
  }

  async function loadComments(postId: string) {
    const items = await socialApi.comments(postId);
    setComments((current) => ({ ...current, [postId]: safeArray(items) }));
  }

  async function addComment(postId: string) {
    const text = commentDrafts[postId] || '';
    if (!text.trim()) return;
    try {
      const created = await socialApi.comment(postId, { text, media: [] });
      setComments((current) => ({ ...current, [postId]: [...(current[postId] || []), created] }));
      setCommentDrafts((current) => ({ ...current, [postId]: '' }));
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to comment.');
    }
  }

  async function react(post: SocialPost, reactionType: 'like' | 'upvote') {
    try {
      const updated = await socialApi.react('post', post.id, reactionType);
      if (updated) setPosts((current) => current.map((item) => item.id === post.id ? updated : item));
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to react.');
    }
  }

  async function startChat(post: SocialPost) {
    if (!post.author.userId) return;
    try {
      const conversation = await socialApi.startConversation({
        recipientUserId: post.author.userId,
        text: `Hi ${post.author.displayName}, I saw your feed post and would like to connect.`,
        media: [],
      });
      setConversations((current) => [conversation, ...current.filter((item) => item.id !== conversation.id)]);
      setActiveConversationId(conversation.id);
      setToast(conversation.status === 'Pending' ? 'Interaction request sent.' : 'Conversation opened.');
      window.dispatchEvent(new Event('medsocial-notifications-refresh'));
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to start chat.');
    }
  }

  async function acceptConversation() {
    if (!activeConversationId) return;
    const updated = await socialApi.acceptConversation(activeConversationId);
    setConversations((current) => current.map((item) => item.id === updated.id ? updated : item));
    setToast('Interaction request accepted.');
    window.dispatchEvent(new Event('medsocial-notifications-refresh'));
  }

  async function rejectConversation() {
    if (!activeConversationId) return;
    try {
      const updated = await socialApi.rejectConversation(activeConversationId);
      setConversations((current) => current.map((item) => item.id === updated.id ? updated : item));
      setToast('Interaction request rejected.');
      window.dispatchEvent(new Event('medsocial-notifications-refresh'));
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to reject interaction.');
    }
  }

  async function sendMessage() {
    if (!activeConversationId || (!messageDraft.trim() && messageMedia.length === 0)) return;
    try {
      const created = await socialApi.sendMessage(activeConversationId, { text: messageDraft, media: messageMedia });
      setMessages((current) => [...current, created]);
      setMessageDraft('');
      setMessageMedia([]);
      window.dispatchEvent(new Event('medsocial-notifications-refresh'));
    } catch (error: any) {
      setToast(error.response?.data?.errors?.[0] || error.message || 'Unable to send message.');
    }
  }

  return (
    <Layout>
      {disabledMessage ? (
        <section className="feed-disabled">
          <h1>Feed is temporarily unavailable</h1>
          <p>{disabledMessage}</p>
        </section>
      ) : (
        <section className="feed-shell">
          <aside className="feed-rail feed-left">
            <div className="feed-card feed-profile-card">
              {isAuthenticated ? (
                <>
                  <div className="feed-profile-top">
                    <div className="feed-avatar large">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profileDraft.displayName || user?.email || 'U').slice(0, 1).toUpperCase()}</div>
                    <div>
                      <strong>{profile?.username ? `@${profile.username}` : 'Create your Feed identity'}</strong>
                      <span>{user?.userType} account</span>
                    </div>
                    {profile && <button type="button" className="feed-icon-button" aria-label="Edit Feed identity" onClick={() => setShowIdentityEditor((current) => !current)}><EditIcon fontSize="small" /></button>}
                  </div>
                  {(!profile || showIdentityEditor) && <div className="feed-identity-editor">
                    <label>Username<input className="feed-input" placeholder="Unique username" value={profileDraft.username} onChange={(event) => setProfileDraft({ ...profileDraft, username: event.target.value })} /></label>
                    <label>Display name<input className="feed-input" placeholder="Display name" value={profileDraft.displayName} onChange={(event) => setProfileDraft({ ...profileDraft, displayName: event.target.value })} /></label>
                    <label className="feed-avatar-upload"><AddPhotoAlternateIcon fontSize="small" /> Upload avatar<input type="file" accept="image/*" hidden onChange={(event) => uploadAvatar(event.target.files)} /></label>
                    <label>Online status<select className="feed-input" value={profileDraft.status} onChange={(event) => setProfileDraft({ ...profileDraft, status: event.target.value })}>{statusOptions.map((item) => <option key={item}>{item}</option>)}</select></label>
                    <label>Short bio<textarea className="feed-textarea compact" placeholder="What should the community know about you?" value={profileDraft.bio} onChange={(event) => setProfileDraft({ ...profileDraft, bio: event.target.value })} /></label>
                    <div className="feed-composer-actions">
                      <button type="button" className="feed-primary" onClick={saveProfile}>Save identity</button>
                      {profile && <button type="button" className="secondary-action" onClick={() => setShowIdentityEditor(false)}>Cancel</button>}
                    </div>
                  </div>}
                </>
              ) : (
                <>
                  <div className="feed-profile-top">
                    <div className="feed-avatar large">G</div>
                    <div>
                      <strong>@{guestTag}</strong>
                      <span>Public browsing mode</span>
                    </div>
                  </div>
                  <p className="feed-muted">Sign in to create posts, channels, comments, reactions, and direct messages.</p>
                  <Link href="/login" className="feed-primary centered">Sign in</Link>
                </>
              )}
            </div>

            <div className="feed-card">
              <div className="feed-section-heading">
                <div><GroupsIcon fontSize="small" /> Channels</div>
                {isAuthenticated && <button type="button" onClick={() => setShowChannelForm((current) => !current)}>{showChannelForm ? 'Close' : 'Create'}</button>}
              </div>
              <button type="button" className={`feed-channel ${activeChannel === 'all' ? 'active' : ''}`} onClick={() => setActiveChannel('all')}>
                <PublicIcon fontSize="small" />
                <span><strong>Global Feed</strong><small>Public platform-wide posts</small></span>
              </button>
              {safeArray<SocialChannel>(channels).map((channel) => (
                <button key={channel.slug} type="button" className={`feed-channel ${activeChannel === channel.slug ? 'active' : ''}`} onClick={() => setActiveChannel(channel.slug)}>
                  {channel.joinPolicy === 'InviteOnly' ? <LockIcon fontSize="small" /> : <GroupsIcon fontSize="small" />}
                  <span><strong>{channel.name}</strong><small>{channel.description || `${channel.joinPolicy} / ${channel.postingPolicy}`}</small></span>
                </button>
              ))}
            </div>
          </aside>

          <main className="feed-main">
            <div className="feed-hero">
              <div>
                <p className="eyebrow">Feed</p>
                <h1>Connect around hiring, practice, learning, and opportunities.</h1>
                <p>Browse publicly. Sign in to post, build channels, react, comment, and message members after accepted interaction requests.</p>
              </div>
              <div className="feed-hero-stats">
                <span><strong>{posts.length}</strong> Visible posts</span>
                <span><strong>{channels.length}</strong> User channels</span>
              </div>
            </div>

            {showChannelForm && isAuthenticated && (
              <div className="feed-card channel-builder">
                <div className="feed-section-heading"><div>Create channel</div><span>Privacy, posting and visibility rules</span></div>
                <div className="feed-grid two">
                  <label>Channel name<input className="feed-input" value={channelDraft.name} onChange={(event) => setChannelDraft({ ...channelDraft, name: event.target.value })} /></label>
                  <label>Description<input className="feed-input" value={channelDraft.description} onChange={(event) => setChannelDraft({ ...channelDraft, description: event.target.value })} /></label>
                  <label>Join policy<select className="feed-input" value={channelDraft.joinPolicy} onChange={(event) => setChannelDraft({ ...channelDraft, joinPolicy: event.target.value })}><option value="Anyone">Anyone can join</option><option value="InviteOnly">Invite only</option></select></label>
                  <label>Posting policy<select className="feed-input" value={channelDraft.postingPolicy} onChange={(event) => setChannelDraft({ ...channelDraft, postingPolicy: event.target.value })}><option value="Anyone">Anyone can post</option><option value="AdminsOnly">Only admins post</option></select></label>
                </div>
                <div className="feed-choice-block">
                  <strong>Media allowed</strong>
                  <div className="feed-chip-row">{mediaOptions.map((item) => <button type="button" key={item} className={channelDraft.allowedMediaTypes.includes(item) ? 'selected' : ''} onClick={() => setChannelDraft({ ...channelDraft, allowedMediaTypes: toggleValue(channelDraft.allowedMediaTypes, item) })}>{item}</button>)}</div>
                </div>
                <div className="feed-choice-block">
                  <strong>Visible to user types</strong>
                  <div className="feed-chip-row">{userTypeOptions.map((item) => <button type="button" key={item} className={channelDraft.visibleToUserTypes.includes(item) ? 'selected' : ''} onClick={() => setChannelDraft({ ...channelDraft, visibleToUserTypes: toggleValue(channelDraft.visibleToUserTypes, item) })}>{item}</button>)}</div>
                </div>
                <div className="feed-choice-block">
                  <strong>Visible to professional categories</strong>
                  <div className="feed-chip-row scrollable">{safeArray<ProfessionalCategory>(categories).map((item) => <button type="button" key={item.id || item.name} className={channelDraft.visibleToCategories.includes(item.name) ? 'selected' : ''} onClick={() => setChannelDraft({ ...channelDraft, visibleToCategories: toggleValue(channelDraft.visibleToCategories, item.name) })}>{item.name}</button>)}</div>
                </div>
                <label>Visible to locations<input className="feed-input" placeholder="Comma-separated regions, cities, or remote tags" value={channelDraft.visibleToLocations} onChange={(event) => setChannelDraft({ ...channelDraft, visibleToLocations: event.target.value })} /></label>
                <button type="button" className="feed-primary" onClick={createChannel}>Create channel</button>
              </div>
            )}

            {isAuthenticated ? (
              <div className="feed-composer">
                <div className="feed-composer-top">
                  <div className="feed-avatar">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profileDraft.displayName || user?.email || 'U').slice(0, 1).toUpperCase()}</div>
                  <textarea className="feed-textarea" placeholder={canPostHere ? 'Share an update, question, hiring note, referral, or resource...' : 'Only channel admins can post here.'} value={composer.text} onChange={(event) => setComposer({ ...composer, text: event.target.value })} disabled={!canPostHere} />
                </div>
                <div className="feed-composer-actions">
                  <label className="feed-tool"><AddPhotoAlternateIcon fontSize="small" /> Media<input type="file" multiple hidden onChange={(event) => upload(event.target.files, 'post')} /></label>
                  <label className="feed-tool grow"><LinkIcon fontSize="small" /><input placeholder="Links separated by spaces" value={composer.links} onChange={(event) => setComposer({ ...composer, links: event.target.value })} /></label>
                  <button type="button" className="feed-primary" onClick={createPost} disabled={!canPostHere}><SendIcon fontSize="small" /> Post</button>
                </div>
                {safeArray<SocialMediaAsset>(composer.media).length > 0 && <div className="feed-media-strip">{safeArray<SocialMediaAsset>(composer.media).map((item) => <span key={item.url}>{item.fileName}</span>)}</div>}
              </div>
            ) : (
              <div className="feed-card feed-signin-callout">
                <strong>Want to join the conversation?</strong>
                <span>Public users can browse. Registered members can post, comment, react, create channels, and message each other.</span>
                <Link href="/register" className="feed-primary centered">Create account</Link>
              </div>
            )}

            <div className="feed-posts">
              {safeArray<SocialPost>(posts).map((post) => (
                <article key={post.id} className="feed-post">
                  <div className="feed-post-head">
                    <div className="feed-avatar">{post.author.avatarUrl ? <img src={post.author.avatarUrl} alt="" /> : authorInitial(post)}</div>
                    <div>
                      <strong>{post.author.displayName || post.author.username}</strong>
                      <span>@{post.author.username} - {roleLabel(post)} - {post.channelSlug === 'global' ? 'Global Feed' : post.channelSlug} - {formatDate(post.createdAt)}</span>
                    </div>
                    {post.author.isOrganization && <span className="feed-badge">Organization</span>}
                  </div>
                  <p className="feed-post-text">{post.text}</p>
                  {safeArray<string>(post.links).length > 0 && <div className="feed-links">{safeArray<string>(post.links).map((item) => <a key={item} href={item} target="_blank" rel="noreferrer">{item}</a>)}</div>}
                  {safeArray<SocialMediaAsset>(post.media).length > 0 && <div className="feed-media-grid">{safeArray<SocialMediaAsset>(post.media).map((item) => item.mediaType === 'image' ? <img key={item.url} src={item.url} alt={item.fileName} /> : <a key={item.url} href={item.url} target="_blank" rel="noreferrer">{item.fileName}</a>)}</div>}
                  <div className="feed-actions">
                    <button type="button" disabled={!isAuthenticated} onClick={() => react(post, 'like')}>Like {post.likeCount}</button>
                    <button type="button" disabled={!isAuthenticated} onClick={() => react(post, 'upvote')}><ThumbUpAltOutlinedIcon fontSize="small" /> Upvote {post.upvoteCount}</button>
                    <button type="button" onClick={() => comments[post.id] ? setComments((current) => ({ ...current, [post.id]: [] })) : loadComments(post.id)}><ModeCommentOutlinedIcon fontSize="small" /> Comments {post.commentCount}</button>
                    {isAuthenticated && post.author.userId && post.author.userId !== user?.id && <button type="button" onClick={() => startChat(post)}>Message</button>}
                    <button type="button" onClick={() => socialApi.report({ targetType: 'post', targetId: post.id, reason: 'Needs moderation review' }).then(() => setToast('Report sent.'))}>Report</button>
                  </div>
                  {comments[post.id]?.length >= 0 && (
                    <div className="feed-comments">
                      {safeArray<SocialComment>(comments[post.id]).map((comment) => <div key={comment.id} className="feed-comment"><strong>@{comment.author.username}</strong><span>{comment.text}</span></div>)}
                      {isAuthenticated && <div className="feed-comment-box"><input placeholder="Write a comment" value={commentDrafts[post.id] || ''} onChange={(event) => setCommentDrafts((current) => ({ ...current, [post.id]: event.target.value }))} /><button type="button" onClick={() => addComment(post.id)}>Reply</button></div>}
                    </div>
                  )}
                </article>
              ))}
              {safeArray<SocialPost>(posts).length === 0 && <div className="feed-empty">No posts yet. Start the conversation when you are signed in.</div>}
            </div>
          </main>

          {showMessages && <aside className="feed-message-dock">
            <div className="feed-card">
              <div className="feed-section-heading"><div><ModeCommentOutlinedIcon fontSize="small" /> Messages</div><button type="button" className="feed-icon-button" aria-label="Close messages" onClick={() => setShowMessages(false)}><CloseIcon fontSize="small" /></button></div>
              {!isAuthenticated && <p className="feed-muted">Sign in to message other members.</p>}
              {isAuthenticated && (
                <>
                  <div className="conversation-list">
                    {safeArray<SocialConversation>(conversations).map((conversation) => {
                      const participants = safeArray<SocialAuthor>(conversation.participants);
                      const other = participants.find((item) => item.userId !== user?.id) || participants[0];
                      return <button key={conversation.id} type="button" className={`conversation-card ${conversation.id === activeConversationId ? 'active' : ''}`} onClick={() => setActiveConversationId(conversation.id)}><strong>{other?.displayName || 'Conversation'}{Number(conversation.unreadCount || 0) > 0 && <em>{conversation.unreadCount}</em>}</strong><span>{conversation.status} - {conversation.lastMessagePreview || 'Interaction request'}</span></button>;
                    })}
                    {safeArray<SocialConversation>(conversations).length === 0 && <p className="feed-muted">No conversations yet.</p>}
                  </div>
                  {activeConversation && (
                    <div className="message-stage">
                      {activeConversation.status === 'Pending' && activeConversation.requestedToUserId === user?.id && (
                        <div className="interaction-request-actions">
                          <p>Accept this interaction to continue the conversation, or reject it to close the request.</p>
                          <button type="button" className="feed-primary" onClick={acceptConversation}>Accept interaction</button>
                          <button type="button" className="feed-danger" onClick={rejectConversation}>Reject request</button>
                        </div>
                      )}
                      {activeConversation.status === 'Pending' && activeConversation.requestedByUserId === user?.id && <p className="feed-muted">Waiting for the recipient to accept this interaction request.</p>}
                      {activeConversation.status === 'Rejected' && <p className="feed-muted">This interaction request was rejected. Messaging is closed for this conversation.</p>}
                      <div className="message-list">{safeArray<SocialMessage>(messages).map((message) => {
                        const mine = message.senderUserId === user?.id;
                        return <div key={message.id} className={`message-bubble ${mine ? 'mine' : ''}`}><span>{message.text}</span>{safeArray<SocialMediaAsset>(message.media).map((item) => <a key={item.url} href={item.url} target="_blank" rel="noreferrer">{item.fileName}</a>)}<MessageTicks message={message} mine={mine} /></div>;
                      })}</div>
                      {activeConversation.status === 'Accepted' && (
                        <>
                          {safeArray<SocialMediaAsset>(messageMedia).length > 0 && <div className="feed-media-strip">{safeArray<SocialMediaAsset>(messageMedia).map((item) => <span key={item.url}>{item.fileName}</span>)}</div>}
                          <div className="message-box">
                            <label className="feed-tool"><AddPhotoAlternateIcon fontSize="small" /><input type="file" multiple hidden onChange={(event) => upload(event.target.files, 'message')} /></label>
                            <input placeholder="Write a message" value={messageDraft} onChange={(event) => setMessageDraft(event.target.value)} />
                            <button type="button" onClick={sendMessage}><SendIcon fontSize="small" /></button>
                          </div>
                        </>
                      )}
                    </div>
                  )}
                </>
              )}
            </div>
          </aside>}
          {!showMessages && <button type="button" className="feed-message-launcher" onClick={() => setShowMessages(true)}><ModeCommentOutlinedIcon /><span>Messages</span>{unreadMessages > 0 && <strong>{unreadMessages}</strong>}</button>}
        </section>
      )}
      {toast && <button type="button" className="toast-message" onClick={() => setToast('')}>{toast}</button>}
    </Layout>
  );
}
