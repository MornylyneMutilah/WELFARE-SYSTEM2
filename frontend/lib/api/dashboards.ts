import apiClient from './client';

export interface PendingApplication {
  id: number; employeeName: string; programName: string; requestedAmount: number; submittedAt: string; anomalyFlags: string;
}
export interface EmployeeDashboardStats {
  totalApplications: number; pendingApplications: number; approvedApplications: number; rejectedApplications: number;
  totalApprovedAmount: number; unreadNotifications: number;
}
export interface HRDashboardStats {
  totalApplications: number; pendingReview: number; approvedByHR: number; rejectedByHR: number;
  flaggedApplications: number; unreadNotifications: number; pendingApplications: PendingApplication[];
}
export interface CommitteeDashboardStats {
  totalApplications: number; pendingReview: number; approvedByCommittee: number; rejectedByCommittee: number;
  unreadNotifications: number; pendingApplications: PendingApplication[];
}
export interface ManagementDashboardStats {
  totalApplications: number; pendingApproval: number; approvedByManagement: number; rejectedByManagement: number;
  totalExpenditure: number; averageProcessingDays: number; unreadNotifications: number; pendingApplications: PendingApplication[];
}
export interface AdminDashboardStats {
  totalUsers: number; totalApplications: number; totalPrograms: number; activePrograms: number;
  totalExpenditure: number; pendingApplications: number; flaggedApplications: number;
  departmentStats: { departmentId: number; departmentName: string; applicationCount: number; totalAmount: number }[];
}
export const dashboardsApi = {
  employee: async () => (await apiClient.get<EmployeeDashboardStats>('/dashboards/employee')).data,
  hr: async () => (await apiClient.get<HRDashboardStats>('/dashboards/hr')).data,
  committee: async () => (await apiClient.get<CommitteeDashboardStats>('/dashboards/committee')).data,
  management: async () => (await apiClient.get<ManagementDashboardStats>('/dashboards/management')).data,
  admin: async () => (await apiClient.get<AdminDashboardStats>('/dashboards/admin')).data,
};
