import { jwtDecode } from 'jwt-decode';
import { api } from './api';

export interface DecodedToken {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': string;
  tenant_id: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;
}

export const auth = {
  // Use the shared api client (which has the correct base URL baked in at build time)
  // rather than calling process.env.NEXT_PUBLIC_API_URL directly, which resolves to
  // "undefined" at runtime if the env var wasn't set during `next build`.
  login: async (email: string, password: string) => {
    const response = await api.login(email, password);
    const { token } = response.data;

    if (typeof window !== 'undefined') {
      localStorage.setItem('authToken', token);
    }

    return token;
  },

  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
    }
  },

  getToken: () => {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('authToken');
  },

  isAuthenticated: () => {
    if (typeof window === 'undefined') return false;
    const token = localStorage.getItem('authToken');
    if (!token) return false;

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      // Check token hasn't expired
      const exp = (decoded as any).exp;
      if (exp && Date.now() / 1000 > exp) {
        localStorage.removeItem('authToken');
        return false;
      }
      return true;
    } catch {
      return false;
    }
  },

  getCurrentUser: () => {
    if (typeof window === 'undefined') return null;
    const token = localStorage.getItem('authToken');
    if (!token) return null;

    try {
      return jwtDecode<DecodedToken>(token);
    } catch {
      return null;
    }
  },
};