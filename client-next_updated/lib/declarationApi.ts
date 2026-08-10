import apiClient from './apiClient';

export type DeclarationConfig = {
  id: string;
  flowKey: string;
  title: string;
  body: string;
  isRequired: boolean;
  displayOrder: number;
};

export const declarationApi = {
  list: (flowKey: string) => apiClient.get<DeclarationConfig[]>(`/api/declarations/${encodeURIComponent(flowKey)}`),
};
