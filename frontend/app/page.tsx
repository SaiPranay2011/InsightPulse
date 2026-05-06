'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { auth } from '@/lib/auth';
import Link from 'next/link';

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    if (auth.isAuthenticated()) {
      router.push('/dashboards');
    }
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      <div className="max-w-6xl mx-auto px-6 py-20">
        <div className="text-center mb-8">
          <h1 className="text-6xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent mb-4">
            Welcome to InsightPulse
          </h1>
          <p className="text-xl text-slate-400 mb-6">
            Real-time business intelligence dashboards
          </p>
          <div className="flex gap-4 justify-center">
            <Link
              href="/login"
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-lg font-semibold transition"
            >
              Login
            </Link>
            <Link
              href="/register"
              className="bg-purple-600 hover:bg-purple-700 text-white px-8 py-3 rounded-lg font-semibold transition"
            >
              Register
            </Link>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-8 md:grid-cols-3 max-w-6xl mx-auto mt-20">
          <div className="bg-slate-800 p-8 rounded-lg shadow-lg border border-slate-700 hover:border-blue-500 transition">
            <h3 className="text-2xl font-bold text-blue-400 mb-4">📊 Real-time Dashboards</h3>
            <p className="text-slate-400">Create custom dashboards with live-updating metrics and charts</p>
          </div>
          <div className="bg-slate-800 p-8 rounded-lg shadow-lg border border-slate-700 hover:border-blue-500 transition">
            <h3 className="text-2xl font-bold text-blue-400 mb-4">🔔 Smart Alerts</h3>
            <p className="text-slate-400">Get notified instantly when metrics cross your thresholds</p>
          </div>
          <div className="bg-slate-800 p-8 rounded-lg shadow-lg border border-slate-700 hover:border-blue-500 transition">
            <h3 className="text-2xl font-bold text-blue-400 mb-4">🔐 Enterprise Ready</h3>
            <p className="text-slate-400">Multi-tenant architecture with role-based access control</p>
          </div>
        </div>
      </div>
    </div>
  );
}