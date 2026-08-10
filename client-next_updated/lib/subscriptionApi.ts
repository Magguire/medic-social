import apiClient from './apiClient';

export const subscriptionApi = {
  plans: () => apiClient.get<any[]>('/api/subscriptions/plans', { skipAuthRedirect: true }),
  current: (employerId: string) => apiClient.get<any>(`/api/subscriptions/employer/${employerId}`),
  paymentMethods: () => apiClient.get<any[]>('/api/subscriptions/payment-methods'),
  checkout: (payload: { employerId: string; planId: string; provider?: number; payerDetails: Record<string, string> }) =>
    apiClient.post<any>('/api/subscriptions/checkout', payload),
  confirmPayment: (transactionId: string) => apiClient.post<any>(`/api/subscriptions/payments/${transactionId}/confirm`, {}),
  payAsYouGoStatus: (action: number, employerId?: string) => apiClient.get<any>(`/api/subscriptions/paygo/status?action=${action}${employerId ? `&employerId=${employerId}` : ''}`),
  recordPayAsYouGo: (payload: { action: number; employerId?: string; relatedEntityId?: string; payerDetails?: Record<string, string> }) =>
    apiClient.post<any>('/api/subscriptions/paygo/record', payload),
};
