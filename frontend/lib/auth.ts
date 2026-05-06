import { jwtDecode } from 'jwt-decode';

export interface DecodedToken {
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': string;
  tenant_id: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;
}

export const auth = {
  login: async (email: string, password: string) => {
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) throw new Error('Login failed');

    const { token } = await response.json();
    
    // Store in localStorage
    if (typeof window !== 'undefined') {
      localStorage.setItem('authToken', token);
    }
    
    return token;
  },

  logout: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('authToken');
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
      return !!decoded;
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