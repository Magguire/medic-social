import apiClient from './apiClient';
import type { AuditItem } from '../types';

export const auditApi = {
  mine: (limit = 20) => apiClient.get<AuditItem[]>(`/api/audit/mine?limit=${limit}`),
};
