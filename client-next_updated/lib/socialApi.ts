import * as signalR from '@microsoft/signalr';
import apiClient from './apiClient';
import { getApiBaseUrl } from './runtimeConfig';

export type SocialMediaAsset = {
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  mediaType: string;
};

export type SocialAuthor = {
  userId?: string;
  username: string;
  displayName: string;
  role: string;
  avatarUrl: string;
  isOrganization: boolean;
  guestTag: string;
};

export type SocialChannel = {
  id?: string;
  name: string;
  slug: string;
  description: string;
  isCommunity: boolean;
  isActive: boolean;
  createdByUserId: string;
  adminUserIds: string[];
  joinPolicy: string;
  postingPolicy: string;
  allowedMediaTypes: string[];
  visibleToUserTypes: string[];
  visibleToCategories: string[];
  visibleToLocations: string[];
  createdAt: string;
};

export type SocialPost = {
  id: string;
  channelSlug: string;
  text: string;
  links: string[];
  media: SocialMediaAsset[];
  author: SocialAuthor;
  commentCount: number;
  likeCount: number;
  upvoteCount: number;
  moderationStatus: string;
  createdAt: string;
  updatedAt: string;
};

export type SocialComment = {
  id: string;
  postId: string;
  text: string;
  media: SocialMediaAsset[];
  author: SocialAuthor;
  likeCount: number;
  upvoteCount: number;
  moderationStatus: string;
  createdAt: string;
};

export type SocialProfile = {
  id?: string;
  userId: string;
  username: string;
  displayName: string;
  role: string;
  avatarUrl: string;
  status: string;
  bio: string;
  lastSeenAt?: string;
};

export type SocialConversation = {
  id: string;
  participants: SocialAuthor[];
  status: string;
  lastMessagePreview: string;
  requestedByUserId: string;
  requestedToUserId: string;
  createdAt: string;
  updatedAt: string;
  unreadCount?: number;
};

export type SocialMessage = {
  id: string;
  conversationId: string;
  senderUserId: string;
  sender: SocialAuthor;
  text: string;
  media: SocialMediaAsset[];
  createdAt: string;
  isRead: boolean;
  readAt?: string;
  deliveryStatus?: 'DeliveredOffline' | 'DeliveredOnline' | 'Read' | string;
};

export type SocialDirectoryUser = {
  userId: string;
  displayName: string;
  userType: string;
  email?: string;
  phoneNumber?: string;
  username: string;
  avatarUrl: string;
  status: string;
};

export const socialApi = {
  channels: () => apiClient.get<SocialChannel[]>('/api/social/channels', { skipAuthRedirect: true }),
  createChannel: (payload: {
    name: string;
    description: string;
    joinPolicy: string;
    postingPolicy: string;
    allowedMediaTypes: string[];
    visibleToUserTypes: string[];
    visibleToCategories: string[];
    visibleToLocations: string[];
  }) => apiClient.post<SocialChannel>('/api/social/channels', payload),
  feed: (channelSlug = 'all', pageNumber = 1, pageSize = 25) => apiClient.get<SocialPost[]>(`/api/social/feed?channelSlug=${encodeURIComponent(channelSlug)}&pageNumber=${pageNumber}&pageSize=${pageSize}`, { skipAuthRedirect: true }),
  myProfile: () => apiClient.get<SocialProfile>('/api/social/profile/me'),
  saveProfile: (payload: Partial<SocialProfile>) => apiClient.put<SocialProfile>('/api/social/profile/me', payload),
  createPost: (payload: { channelSlug: string; text: string; links: string[]; media: SocialMediaAsset[] }) => apiClient.post<SocialPost>('/api/social/posts', payload),
  comments: (postId: string) => apiClient.get<SocialComment[]>(`/api/social/posts/${postId}/comments`, { skipAuthRedirect: true }),
  comment: (postId: string, payload: { text: string; media: SocialMediaAsset[] }) => apiClient.post<SocialComment>(`/api/social/posts/${postId}/comments`, payload),
  react: (targetType: 'post' | 'comment', targetId: string, reactionType: 'like' | 'upvote') => apiClient.post<SocialPost | null>(`/api/social/${targetType}/${targetId}/reactions`, { reactionType }),
  report: (payload: { targetType: string; targetId: string; reason: string }) => apiClient.post('/api/social/reports', payload, { skipAuthRedirect: true }),
  searchPeople: (query: string, role = 'Professional') => apiClient.get<SocialDirectoryUser[]>(`/api/social/people/search?q=${encodeURIComponent(query)}&role=${encodeURIComponent(role)}`),
  conversations: () => apiClient.get<SocialConversation[]>('/api/social/conversations'),
  startConversation: (payload: { recipientUserId: string; text: string; media: SocialMediaAsset[] }) => apiClient.post<SocialConversation>('/api/social/conversations', payload),
  acceptConversation: (conversationId: string) => apiClient.post<SocialConversation>(`/api/social/conversations/${conversationId}/accept`),
  rejectConversation: (conversationId: string) => apiClient.post<SocialConversation>(`/api/social/conversations/${conversationId}/reject`),
  messages: (conversationId: string) => apiClient.get<SocialMessage[]>(`/api/social/conversations/${conversationId}/messages`),
  markConversationRead: (conversationId: string) => apiClient.post<SocialConversation>(`/api/social/conversations/${conversationId}/read`),
  sendMessage: (conversationId: string, payload: { text: string; media: SocialMediaAsset[] }) => apiClient.post<SocialMessage>(`/api/social/conversations/${conversationId}/messages`, payload),
  uploadMedia: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
    const response = await fetch(`${getApiBaseUrl()}/api/social/media`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      throw new Error(payload?.errors?.[0] || 'Upload failed');
    }
    return response.json() as Promise<SocialMediaAsset>;
  },
  connectRealtime: () => {
    const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') || '' : '';
    return new signalR.HubConnectionBuilder()
      .withUrl(`${getApiBaseUrl()}/hubs/social`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();
  },
};
