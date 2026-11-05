import apiClient from './client';

export interface Page<T> { total: number; page: number; pageSize: number; items: T[] }
export interface DecisionRecord { id: number; applicationId: number; employeeName: string; programName: string; amount: number; reviewerId: number; reviewerName: string; stage: string; decision: string; comments: string; reviewedAt: string; currentStatus: number }
export interface DecisionSnapshot { reference: string; applicationId: number; employeeName: string; employeeId: string; departmentId: number; programName: string; requestedAmount: number; reason: string; isEligible: boolean; ruleEvaluationResult: string; submittedAt: string; reviewerName: string; reviewerRole: string; recommendation: string; comments: string; reviewedAt: string; newStatus: number; documents?: { id: number; fileName: string; fileType: string; fileSize: number; uploadedAt: string }[] }
export interface Receipt { reference: string; record: DecisionRecord; snapshot: DecisionSnapshot | null; historicalSnapshotAvailable: boolean }
export interface Timeline { history: { id: number; status: number; changedBy: number; changedAt: string; notes: string; actorName: string | null }[]; assessments: { id: number; timestamp: string; details: string }[] }
export interface AuditEvent { id: number; userId: number | null; actorName: string; action: string; entityType: string; entityId: string; timestamp: string; details: string }
export const recordsApi = {
  decisions: async (query = '') => (await apiClient.get<Page<DecisionRecord>>(`/records/decisions?${query}`)).data,
  receipt: async (id: number) => (await apiClient.get<Receipt>(`/records/decisions/${id}`)).data,
  timeline: async (id: number) => (await apiClient.get<Timeline>(`/records/applications/${id}`)).data,
  audit: async (query = '') => (await apiClient.get<Page<AuditEvent>>(`/auditlogs?${query}`)).data,
};
export function messageFor(error: unknown, fallback: string) { return (error as { response?: { data?: { message?: string } } })?.response?.data?.message || fallback; }
export async function downloadFile(url: string, filename: string) {
  try {
    const response = await apiClient.get<Blob>(url, { responseType: 'blob' });
    const address = URL.createObjectURL(response.data);
    const link = document.createElement('a'); link.href = address; link.download = filename; document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => URL.revokeObjectURL(address), 1000);
  } catch (error: unknown) {
    const response = (error as { response?: { data?: Blob } }).response;
    if (response?.data instanceof Blob) { try { const body = JSON.parse(await response.data.text()); throw new Error(body.message || 'Download failed.'); } catch (parsed) { if (parsed instanceof Error && !(parsed instanceof SyntaxError)) throw parsed; } }
    throw new Error('Download failed. Check your access and try again.');
  }
}
export const statusLabel = (value: number) => ({ 1: 'Draft', 2: 'Submitted', 3: 'Under HR review', 4: 'Under committee review', 5: 'Committee endorsed — awaiting management', 6: 'Rejected by committee', 7: 'Pending management', 8: 'Approved', 9: 'Rejected', 10: 'Cancelled' }[value] || 'Unknown');
