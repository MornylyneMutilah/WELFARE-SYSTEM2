import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5249/api';

const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Don't set Content-Type for multipart/form-data - let axios handle it
    if (config.data instanceof FormData) {
      delete config.headers['Content-Type'];
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => {
    if (typeof window !== 'undefined' && ['post', 'put', 'patch', 'delete'].includes(response.config.method || '')) {
      window.dispatchEvent(new Event('welfare:data-changed'));
      try { localStorage.setItem('welfare:last-change', String(Date.now())); } catch { /* Polling still refreshes other views. */ }
    }
    return response;
  },
  (error) => {
    const url = String(error.config?.url || '');
    const isCredentialAuth = url.startsWith('/auth/');
    if (error.response?.status === 401 && !isCredentialAuth && localStorage.getItem('token')) {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      window.location.assign(new URL('/login', window.location.origin).href);
    }
    return Promise.reject(error);
  }
);

export default apiClient;
