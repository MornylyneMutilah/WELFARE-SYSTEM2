import apiClient from './client';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  departmentId: number;
  hireDate?: string | null;
  requestedRole?: string | null;
}

export interface UserDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  role: string;
  department: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: UserDto;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ExternalProviderStatus {
  provider: string;
  configured: boolean;
}

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/login', data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/register', data);
    return response.data;
  },

  getExternalProviders: async (): Promise<ExternalProviderStatus[]> => {
    const response = await apiClient.get<ExternalProviderStatus[]>('/auth/external/providers');
    return response.data;
  },

  externalStartUrl: (provider: string): string => {
    const base = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5249/api';
    return `${base}/auth/external/${provider}/start`;
  },

  exchangeExternal: async (code: string): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/external/exchange', { code });
    return response.data;
  },

  refreshToken: async (data: RefreshTokenRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/auth/refresh', data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/auth/logout');
  },
};
