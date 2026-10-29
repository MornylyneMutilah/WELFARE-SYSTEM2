import apiClient from './client';

export interface EligibilityRuleResponse {
  id: number;
  welfareProgramId: number;
  ruleName: string;
  ruleType: number;
  ruleValue: string;
  description: string;
  isActive: boolean;
}

export interface CreateEligibilityRuleRequest {
  welfareProgramId: number;
  ruleName: string;
  ruleType: number;
  ruleValue: string;
  description: string;
}

export interface UpdateEligibilityRuleRequest {
  ruleName: string;
  ruleType: number;
  ruleValue: string;
  description: string;
  isActive: boolean;
}

export const eligibilityRulesApi = {
  getRuleById: async (id: number): Promise<EligibilityRuleResponse> => {
    const response = await apiClient.get<EligibilityRuleResponse>(`/eligibility-rules/${id}`);
    return response.data;
  },

  getRulesByProgramId: async (programId: number): Promise<EligibilityRuleResponse[]> => {
    const response = await apiClient.get<EligibilityRuleResponse[]>(`/eligibility-rules/program/${programId}`);
    return response.data;
  },

  createRule: async (data: CreateEligibilityRuleRequest): Promise<EligibilityRuleResponse> => {
    const response = await apiClient.post<EligibilityRuleResponse>('/eligibility-rules', data);
    return response.data;
  },

  updateRule: async (id: number, data: UpdateEligibilityRuleRequest): Promise<EligibilityRuleResponse> => {
    const response = await apiClient.put<EligibilityRuleResponse>(`/eligibility-rules/${id}`, data);
    return response.data;
  },

  deactivateRule: async (id: number): Promise<void> => {
    await apiClient.delete(`/eligibility-rules/${id}`);
  },
};
