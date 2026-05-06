'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { api } from '@/lib/api';
import { auth } from '@/lib/auth';
import MetricCard from '@/components/MetricCard';
import ChartWidget from '@/components/ChartWidget';
import toast from 'react-hot-toast';

interface Widget {
  id: string;
  title: string;
  type: number;
  metric: string;
  dimension?: string;
  positionX: number;
  positionY: number;
  width: number;
  height: number;
}

interface Dashboard {
  id: string;
  name: string;
  description: string;
  widgets: Widget[];
}

export default function DashboardDetailPage() {
  const router = useRouter();
  const params = useParams();
  const dashboardId = params.id as string;

  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [mounted, setMounted] = useState(false);
  const [showAddWidget, setShowAddWidget] = useState(false);
  const [newWidget, setNewWidget] = useState({
    title: '',
    type: 0,
    metric: '',
    dimension: 'global',
    positionX: 0,
    positionY: 0,
    width: 6,
    height: 3,
  });

  useEffect(() => {
    setMounted(true);
    const token = auth.getToken();
    
    if (!token) {
      router.push('/login');
      return;
    }

    fetchDashboard();
  }, []);

  const fetchDashboard = async () => {
    try {
      const response = await api.getDashboard(dashboardId);
      setDashboard(response.data);
    } catch (error) {
      toast.error('Failed to load dashboard');
      router.push('/dashboards');
    } finally {
      setLoading(false);
    }
  };

  const handleAddWidget = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      // Call custom API to add widget with full config
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/dashboards/${dashboardId}/widgets`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${auth.getToken()}`,
        },
        body: JSON.stringify({
          title: newWidget.title,
          type: newWidget.type,
          metric: newWidget.metric,
          dimension: newWidget.dimension === 'global' ? null : newWidget.dimension,
          positionX: newWidget.positionX,
          positionY: newWidget.positionY,
          width: newWidget.width,
          height: newWidget.height,
        }),
      });

      if (!response.ok) throw new Error('Failed to add widget');

      toast.success('Widget added!');
      setNewWidget({
        title: '',
        type: 0,
        metric: '',
        dimension: 'global',
        positionX: 0,
        positionY: 0,
        width: 6,
        height: 3,
      });
      setShowAddWidget(false);
      fetchDashboard();
    } catch (error) {
      toast.error('Failed to add widget');
    }
  };

  const handleRemoveWidget = async (widgetId: string) => {
    try {
      await api.deleteWidget(dashboardId, widgetId);
      toast.success('Widget removed');
      fetchDashboard();
    } catch (error) {
      toast.error('Failed to remove widget');
    }
  };

  if (!mounted || loading) return <div className="flex justify-center py-12 text-slate-400">Loading...</div>;
  if (!dashboard) return <div className="text-center py-12 text-slate-400">Dashboard not found</div>;

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex justify-between items-start mb-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-100">
            {dashboard.name}
          </h1>
          <p className="text-slate-400">{dashboard.description}</p>
        </div>
        <div className="flex gap-2">
          <Link
            href={`/dashboards/${dashboardId}/alerts`}
            className="bg-purple-600 hover:bg-purple-700 text-white px-4 py-2 rounded-lg font-medium transition"
          >
            🔔 Alerts
          </Link>
          <button
            onClick={() => setShowAddWidget(true)}
            className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg font-medium transition"
          >
            + Add Widget
          </button>
        </div>
      </div>

      {dashboard.widgets.length === 0 ? (
        <div className="text-center py-12 border-2 border-dashed border-slate-600 rounded-lg">
          <p className="text-slate-400 mb-4">No widgets yet</p>
          <button
            onClick={() => setShowAddWidget(true)}
            className="text-blue-400 hover:text-blue-300 font-medium"
          >
            Add your first widget
          </button>
        </div>
      ) : (
        <div className="grid gap-6" style={{
          gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))'
        }}>
          {dashboard.widgets.map((widget) => (
            <div key={widget.id} className="relative group">
              {widget.type === 0 ? (
                <MetricCard 
                  metric={widget.metric} 
                  title={widget.title}
                  dimension={widget.dimension}
                />
              ) : (
                <ChartWidget
                  metric={widget.metric}
                  title={widget.title}
                  type={widget.type}
                  dimension={widget.dimension}
                />
              )}
              
              <button
                onClick={() => handleRemoveWidget(widget.id)}
                className="absolute top-2 right-2 bg-red-600 hover:bg-red-700 text-white px-2 py-1 rounded text-sm opacity-0 group-hover:opacity-100 transition"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      )}

      {showAddWidget && (
        <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50 overflow-y-auto">
          <div className="bg-slate-800 p-8 rounded-lg w-full max-w-2xl border border-slate-700 my-8">
            <h2 className="text-2xl font-bold mb-6 text-slate-100">
              Add Widget
            </h2>

            <form onSubmit={handleAddWidget} className="space-y-6">
              {/* Basic Info */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-slate-300 border-b border-slate-600 pb-2">
                  Basic Information
                </h3>

                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-2">
                    Widget Title *
                  </label>
                  <input
                    type="text"
                    placeholder="e.g., Daily Revenue, User Signups"
                    value={newWidget.title}
                    onChange={(e) => setNewWidget({ ...newWidget, title: e.target.value })}
                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                    required
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Widget Type *
                    </label>
                    <select
                      value={newWidget.type}
                      onChange={(e) => setNewWidget({ ...newWidget, type: parseInt(e.target.value) })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100"
                      required
                    >
                      <option value={0}>📊 Metric Card (Single Value)</option>
                      <option value={1}>📈 Line Chart</option>
                      <option value={2}>📊 Bar Chart</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Metric Name *
                    </label>
                    <input
                      type="text"
                      placeholder="e.g., revenue, users, orders"
                      value={newWidget.metric}
                      onChange={(e) => setNewWidget({ ...newWidget, metric: e.target.value })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                      required
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-2">
                    Dimension (optional)
                  </label>
                  <input
                    type="text"
                    placeholder="e.g., global, us, eu, product_a"
                    value={newWidget.dimension}
                    onChange={(e) => setNewWidget({ ...newWidget, dimension: e.target.value })}
                    className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 placeholder-slate-400"
                  />
                  <p className="text-xs text-slate-500 mt-1">Filter data by dimension (leave empty for all data)</p>
                </div>
              </div>

              {/* Position & Size */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-slate-300 border-b border-slate-600 pb-2">
                  Position & Size
                </h3>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Position X: {newWidget.positionX}
                    </label>
                    <input
                      type="range"
                      min="0"
                      max="12"
                      step="1"
                      value={newWidget.positionX}
                      onChange={(e) => setNewWidget({ ...newWidget, positionX: parseInt(e.target.value) })}
                      className="w-full"
                    />
                    <input
                      type="number"
                      min="0"
                      max="12"
                      value={newWidget.positionX}
                      onChange={(e) => setNewWidget({ ...newWidget, positionX: parseInt(e.target.value) })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 mt-2"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Position Y: {newWidget.positionY}
                    </label>
                    <input
                      type="range"
                      min="0"
                      max="12"
                      step="1"
                      value={newWidget.positionY}
                      onChange={(e) => setNewWidget({ ...newWidget, positionY: parseInt(e.target.value) })}
                      className="w-full"
                    />
                    <input
                      type="number"
                      min="0"
                      max="12"
                      value={newWidget.positionY}
                      onChange={(e) => setNewWidget({ ...newWidget, positionY: parseInt(e.target.value) })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 mt-2"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Width: {newWidget.width} cols
                    </label>
                    <input
                      type="range"
                      min="3"
                      max="12"
                      step="1"
                      value={newWidget.width}
                      onChange={(e) => setNewWidget({ ...newWidget, width: parseInt(e.target.value) })}
                      className="w-full"
                    />
                    <input
                      type="number"
                      min="3"
                      max="12"
                      value={newWidget.width}
                      onChange={(e) => setNewWidget({ ...newWidget, width: parseInt(e.target.value) })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 mt-2"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-slate-300 mb-2">
                      Height: {newWidget.height} rows
                    </label>
                    <input
                      type="range"
                      min="2"
                      max="6"
                      step="1"
                      value={newWidget.height}
                      onChange={(e) => setNewWidget({ ...newWidget, height: parseInt(e.target.value) })}
                      className="w-full"
                    />
                    <input
                      type="number"
                      min="2"
                      max="6"
                      value={newWidget.height}
                      onChange={(e) => setNewWidget({ ...newWidget, height: parseInt(e.target.value) })}
                      className="w-full px-4 py-2 bg-slate-700 border border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-slate-100 mt-2"
                    />
                  </div>
                </div>

                <div className="bg-slate-700 p-4 rounded-lg">
                  <p className="text-xs text-slate-400 mb-2">📐 Preview:</p>
                  <div className="text-sm text-slate-300 space-y-1">
                    <p>• Position: ({newWidget.positionX}, {newWidget.positionY})</p>
                    <p>• Size: {newWidget.width} columns × {newWidget.height} rows</p>
                    <p>• Grid: 12 columns available</p>
                  </div>
                </div>
              </div>

              {/* Buttons */}
              <div className="flex gap-4 pt-4 border-t border-slate-600">
                <button
                  type="submit"
                  className="flex-1 bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition"
                >
                  ✓ Add Widget
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowAddWidget(false);
                    setNewWidget({
                      title: '',
                      type: 0,
                      metric: '',
                      dimension: 'global',
                      positionX: 0,
                      positionY: 0,
                      width: 6,
                      height: 3,
                    });
                  }}
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