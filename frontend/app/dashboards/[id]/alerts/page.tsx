'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { api } from '@/lib/api';
import { auth } from '@/lib/auth';
import toast from 'react-hot-toast';

interface Alert {
  id: string;
  name: string;
  metric: string;
  condition: number;
  threshold: number;
  isActive: boolean;
  createdAt: string;
}

export default function AlertsPage() {
  const router = useRouter();
  const params = useParams();
  const dashboardId = params.id as string;

  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newAlert, setNewAlert] = useState({
    name: '',
    metric: '',
    condition: 0,
    threshold: 0,
    emailAddress: '',
  });

  useEffect(() => {
    const token = auth.getToken();
    if (!token) {
      router.push('/login');
      return;
    }

    fetchAlerts();
  }, []);

  const fetchAlerts = async () => {
    try {
      const response = await api.getAlerts(dashboardId);
      setAlerts(response.data);
    } catch (error) {
      toast.error('Failed to load alerts');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateAlert = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.createAlert(
        dashboardId,
        newAlert.name,
        newAlert.metric,
        newAlert.condition,
        newAlert.threshold,
        newAlert.emailAddress
      );
      toast.success('Alert created!');
      setNewAlert({ name: '', metric: '', condition: 0, threshold: 0, emailAddress: '' });
      setShowCreateModal(false);
      fetchAlerts();
    } catch (error) {
      toast.error('Failed to create alert');
    }
  };

  const handleDeleteAlert = async (alertId: string) => {
    if (!confirm('Delete this alert?')) return;
    try {
      await api.deleteAlert(dashboardId, alertId);
      toast.success('Alert deleted');
      fetchAlerts();
    } catch (error) {
      toast.error('Failed to delete alert');
    }
  };

  const getConditionLabel = (condition: number) => {
    const labels = ['Greater Than', 'Less Than', 'Percentage Change'];
    return labels[condition] || 'Unknown';
  };

  if (loading) return <div className="flex justify-center py-12 text-slate-400">Loading...</div>;

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <button
        onClick={() => router.back()}
        className="text-blue-400 hover:text-blue-300 mb-6 font-medium"
      >
        ← Back to Dashboard
      </button>

      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold text-slate-100">Dashboard Alerts</h1>
        <button
          onClick={() => setShowCreateModal(true)}
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded font-medium transition"
        >
          + New Alert
        </button>
      </div>

      {alerts.length === 0 ? (
        <div className="text-center py-12 border-2 border-dashed border-slate-600 rounded-lg">
          <p className="text-slate-400 mb-4">No alerts set up yet</p>
          <button
            onClick={() => setShowCreateModal(true)}
            className="text-blue-400 hover:text-blue-300 font-medium"
          >
            Create your first alert
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {alerts.map((alert) => (
            <div key={alert.id} className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700">
              <div className="flex justify-between items-start mb-4">
                <div className="flex-1">
                  <h3 className="text-lg font-semibold text-slate-100">{alert.name}</h3>
                  <p className="text-slate-400 mt-2">
                    Trigger when <span className="font-medium text-blue-400">{alert.metric}</span> is{' '}
                    <span className="font-medium text-blue-400">{getConditionLabel(alert.condition)}</span>{' '}
                    <span className="font-medium text-blue-400">{alert.threshold}</span>
                  </p>
                  <div className="mt-3 flex gap-4 text-sm text-slate-500">
                    <span>{alert.isActive ? '✓ Active' : '✗ Inactive'}</span>
                    <span>Created {new Date(alert.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>
                <div className="flex flex-col gap-2">
                  <Link
                    href={`/dashboards/${dashboardId}/alerts/${alert.id}`}
                    className="text-blue-400 hover:text-blue-300 font-medium text-sm"
                  >
                    View History
                  </Link>
                  <button
                    onClick={() => handleDeleteAlert(alert.id)}
                    className="text-red-400 hover:text-red-300 font-medium text-sm"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {showCreateModal && (
        <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50">
          <div className="bg-slate-800 p-8 rounded-lg w-full max-w-md border border-slate-700">
            <h2 className="text-2xl font-bold mb-4 text-slate-100">Create Alert</h2>
            <form onSubmit={handleCreateAlert} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Alert Name</label>
                <input
                  type="text"
                  placeholder="e.g., Low Revenue Alert"
                  value={newAlert.name}
                  onChange={(e) => setNewAlert({ ...newAlert, name: e.target.value })}
                  className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Metric</label>
                <input
                  type="text"
                  placeholder="e.g., revenue"
                  value={newAlert.metric}
                  onChange={(e) => setNewAlert({ ...newAlert, metric: e.target.value })}
                  className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Condition</label>
                  <select
                    value={newAlert.condition}
                    onChange={(e) => setNewAlert({ ...newAlert, condition: parseInt(e.target.value) })}
                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100"
                  >
                    <option value={0}>Greater Than</option>
                    <option value={1}>Less Than</option>
                    <option value={2}>Change %</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Threshold</label>
                  <input
                    type="number"
                    placeholder="e.g., 10000"
                    value={newAlert.threshold}
                    onChange={(e) => setNewAlert({ ...newAlert, threshold: parseFloat(e.target.value) })}
                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Email Address</label>
                <input
                  type="email"
                  placeholder="your@email.com"
                  value={newAlert.emailAddress}
                  onChange={(e) => setNewAlert({ ...newAlert, emailAddress: e.target.value })}
                  className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                />
              </div>

              <div className="flex gap-4 pt-4">
                <button
                  type="submit"
                  className="flex-1 bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition"
                >
                  Create Alert
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