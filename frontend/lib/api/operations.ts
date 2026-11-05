import api from './client';
export const accountApi = {
  security: async () => (await api.get<{ emailVerified: boolean; mustChangePassword: boolean; hireDate: string; employmentVerifiedAt: string | null; deliveryMode: string }>('/account/security')).data,
  forgot: async (email: string) => (await api.post<{ message: string; deliveryMode: string }>('/account/forgot-password', { email })).data,
  verify: async () => (await api.post<{ message: string; deliveryMode: string }>('/account/verify-email')).data,
  complete: async (token: string, purpose: string, password?: string) => (await api.post<{ message: string }>('/account/complete', { token, purpose, password })).data,
  change: async (currentPassword: string, newPassword: string) => (await api.post<{ message: string }>('/account/change-password', { currentPassword, newPassword })).data,
  invite: async (id: number) => (await api.post<{ message: string; deliveryMode: string }>(`/account/invite/${id}`)).data,
};
export interface Budget { id: number; allocatedAmount: number; proposedAmount: number; proposalStatus: string; requestedBy: number; reason: string; updatedAt: string }
export interface BudgetRow { programId: number; programName: string; year: number; budget: Budget | null; committed: number; remaining: number }
export interface Amendment { entry: { id: number; reviewId: number; requestedBy: number; kind: string; status: string; reason: string; proposedNotes: string; resolution: string; createdAt: string; resolvedAt: string | null; reconsiderationApplicationId: number | null }; applicationId: number; requester: string; resolver: string | null }
export const operationsApi = {
  budgets: async (year: number) => (await api.get<BudgetRow[]>(`/budgets?year=${year}`)).data,
  proposeBudget: async (programId: number, year: number, amount: number, reason: string) => (await api.post('/budgets', { programId, year, amount, reason })).data,
  decideBudget: async (id: number, approve: boolean, reason: string) => (await api.post(`/budgets/${id}/decision`, { approve, reason })).data,
  amendments: async (applicationId?: number) => (await api.get<Amendment[]>(`/amendments${applicationId ? `?applicationId=${applicationId}` : ''}`)).data,
  requestAmendment: async (reviewId: number, kind: string, reason: string, proposedNotes: string) => (await api.post('/amendments', { reviewId, kind, reason, proposedNotes })).data,
  decideAmendment: async (id: number, approve: boolean, reason: string) => (await api.post(`/amendments/${id}/decision`, { approve, reason })).data,
};
