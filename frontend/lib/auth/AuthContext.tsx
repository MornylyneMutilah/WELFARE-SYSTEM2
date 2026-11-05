'use client';

import React, { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';
import { authApi, type UserDto } from '../api/auth';
import { tokenUtils } from './tokenUtils';
import { usersApi } from '../api/users';

interface AuthContextType {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    employeeId: string;
    departmentId: number;
    hireDate?: string | null;
    requestedRole?: string | null;
  }) => Promise<void>;
  completeExternalLogin: (code: string) => Promise<void>;
  logout: () => void;
  refreshUserData: () => Promise<void>;
  consumeLoginWelcome: (userId: number) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  // Presentation-only signal: set on successful sign-in, never during session hydration or refresh.
  const pendingWelcome = useRef<number | null>(null);
  const consumeLoginWelcome = useCallback((userId: number) => {
    if (pendingWelcome.current !== userId) return false;
    pendingWelcome.current = null;
    return true;
  }, []);

  useEffect(() => {
    // Hydrate browser storage after mount, preserving the server's initial render.
    let active = true;
    queueMicrotask(async () => {
      if (!active) return;
      try {
        const storedToken = tokenUtils.getToken();
        const storedUser = localStorage.getItem('user');
        if (storedToken && storedUser) {
          const verified = await usersApi.getCurrentUser();
          if (!active) return;
          const verifiedUser: UserDto = {
            id: verified.id, email: verified.email, firstName: verified.firstName, lastName: verified.lastName,
            employeeId: verified.employeeId, role: verified.roleName, department: verified.departmentName,
            mustChangePassword: verified.mustChangePassword,
          };
          setToken(storedToken);
          setUser(verifiedUser);
          localStorage.setItem('user', JSON.stringify(verifiedUser));
        }
      } catch {
        tokenUtils.clearAll();
        localStorage.removeItem('user');
      } finally {
        if (active) setIsLoading(false);
      }
    });
    return () => { active = false; };
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await authApi.login({ email, password });
      pendingWelcome.current = response.user.id;
      
      tokenUtils.setToken(response.token);
      tokenUtils.setRefreshToken(response.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.user));
      
      setToken(response.token);
      setUser(response.user);
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  };

  const completeExternalLogin = useCallback(async (code: string) => {
    const response = await authApi.exchangeExternal(code);
    pendingWelcome.current = response.user.id;
    tokenUtils.setToken(response.token);
    tokenUtils.setRefreshToken(response.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.user));
    setToken(response.token);
    setUser(response.user);
  }, []);

  const register = async (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    employeeId: string;
    departmentId: number;
    hireDate?: string | null;
    requestedRole?: string | null;
  }) => {
    try {
      // Just register without setting token/user - user should login manually
      await authApi.register(data);
      // Don't auto-login, let user login manually for security
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  };

  const logout = () => {
    pendingWelcome.current = null;
    tokenUtils.clearAll();
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  const refreshUserData = async () => {
    try {
      const refreshToken = tokenUtils.getRefreshToken();
      if (!refreshToken) {
        logout();
        return;
      }

      const response = await authApi.refreshToken({ refreshToken });
      
      tokenUtils.setToken(response.token);
      tokenUtils.setRefreshToken(response.refreshToken);
      localStorage.setItem('user', JSON.stringify(response.user));
      
      setToken(response.token);
      setUser(response.user);
    } catch (error) {
      console.error('Token refresh failed:', error);
      logout();
    }
  };

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!user,
    isLoading,
    login,
    register,
    completeExternalLogin,
    logout,
    refreshUserData,
    consumeLoginWelcome,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
