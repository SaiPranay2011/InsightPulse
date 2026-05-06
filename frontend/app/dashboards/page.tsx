'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api } from '@/lib/api';
import { auth } from '@/lib/auth';
import toast from 'react-hot-toast';

interface Dashboard {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export default function DashboardsPage() {
  const router = useRouter();
  const [dashboards, setDashboards] = useState<Dashboard[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [mounted, setMounted] = useState(false);
  const [newDashboard, setNewDashboard] = useState({ name: '', description: '' });

  useEffect(() => {
    setMounted(true);
    const token = auth.getToken();
    
    if (!token) {
      router.push('/login');
      return;
    }

    fetchDashboards();
  }, []);

  const fetchDashboards = async () => {
    try {
      const response = await api.getDashboards();
      setDashboards(response.data);
    } catch (error) {
      toast.error('Failed to load dashboards');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateDashboard = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.createDashboard(newDashboard.name, newDashboard.description);
      toast.success('Dashboard created!');
      setNewDashboard({ name: '', description: '' });
      setShowCreateModal(false);
      fetchDashboards();
    } catch (error) {
      toast.error('Failed to create dashboard');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure?')) return;
    try {
      await api.deleteDashboard(id);
      toast.success('Dashboard deleted');
      fetchDashboards();
    } catch (error) {
      toast.error('Failed to delete dashboard');
    }
  };

  if (!mounted || loading) return <div className="flex justify-center py-12 text-slate-400">Loading...</div>;

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold text-slate-100">Dashboards</h1>
        <button
          onClick={() => setShowCreateModal(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg font-medium transition"
        >
          + New Dashboard
        </button>
      </div>

      {dashboards.length === 0 ? (
        <div className="text-center py-12 border-2 border-dashed border-slate-600 rounded-lg">
          <p className="text-slate-400 mb-4">No dashboards yet</p>
          <button
            onClick={() => setShowCreateModal(true)}
            className="text-blue-400 hover:text-blue-300 font-medium"
          >
            Create your first dashboard
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {dashboards.map((dashboard) => (
            <Link
              key={dashboard.id}
              href={`/dashboards/${dashboard.id}`}
              className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700 hover:border-blue-500 hover:shadow-blue-500/20 transition cursor-pointer"
            >
              <h3 className="text-xl font-semibold text-slate-100 mb-2">
                {dashboard.name}
              </h3>
              <p className="text-slate-400 mb-4">{dashboard.description}</p>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-500">
                  {new Date(dashboard.createdAt).toLocaleDateString()}
                </span>
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    handleDelete(dashboard.id);
                  }}
                  className="text-red-400 hover:text-red-300 text-sm font-medium transition"
                >
                  Delete
                </button>
              </div>
            </Link>
          ))}
        </div>
      )}

      {showCreateModal && (
        <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50">
          <div className="bg-slate-800 p-8 rounded-lg w-full max-w-md border border-slate-700">
            <h2 className="text-2xl font-bold mb-4 text-slate-100">
              Create Dashboard
            </h2>
            <form onSubmit={handleCreateDashboard} className="space-y-4">
              <input
                type="text"
                placeholder="Dashboard name"
                value={newDashboard.name}
                onChange={(e) => setNewDashboard({ ...newDashboard, name: e.target.value })}
                className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                required
              />
              <textarea
                placeholder="Description"
                value={newDashboard.description}
                onChange={(e) => setNewDashboard({ ...newDashboard, description: e.target.value })}
                className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
              />
              <div className="flex gap-4">
                <button
                  type="submit"
                  className="flex-1 bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition"
                >
                  Create
                </button>
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="flex-1 bg-slate-700 hover:bg-slate-600 text-slate-100 font-semibold py-2 rounded-lg transition"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}