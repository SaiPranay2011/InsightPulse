'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { auth } from '@/lib/auth';
import toast from 'react-hot-toast';

interface AlertHistoryItem {
  id: string;
  metricValue: number;
  message: string;
  wasTriggered: boolean;
  timestamp: string;
}

export default function AlertHistoryPage() {
  const params = useParams();
  const router = useRouter();
  const dashboardId = params.id as string;
  const alertId = params.alertId as string;

  const [history, setHistory] = useState<AlertHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = auth.getToken();
    if (!token) {
      router.push('/login');
      return;
    }

    fetchHistory();
  }, []);

  const fetchHistory = async () => {
    try {
      const response = await api.getAlertHistory(dashboardId, alertId);
      setHistory(response.data);
    } catch (error) {
      toast.error('Failed to load alert history');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="flex justify-center py-12 text-slate-400">Loading...</div>;

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <button
        onClick={() => router.back()}
        className="text-blue-400 hover:text-blue-300 mb-6 font-medium"
      >
        ← Back
      </button>

      <h1 className="text-3xl font-bold text-slate-100 mb-6">Alert History</h1>

      {history.length === 0 ? (
        <div className="text-center py-12 border-2 border-dashed border-slate-600 rounded-lg">
          <p className="text-slate-400">No history yet</p>
        </div>
      ) : (
        <div className="space-y-4">
          {history.map((item) => (
            <div
              key={item.id}
              className={`p-4 rounded-lg border ${
                item.wasTriggered
                  ? 'bg-red-900 bg-opacity-20 border-red-700'
                  : 'bg-slate-800 border-slate-700'
              }`}
            >
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <div className="flex gap-2 items-center mb-2">
                    {item.wasTriggered ? (
                      <span className="text-red-400 font-semibold">🔴 TRIGGERED</span>
                    ) : (
                      <span className="text-green-400 font-semibold">✓ OK</span>
                    )}
                  </div>
                  <p className="text-slate-300">{item.message || `Value: ${item.metricValue}`}</p>
                </div>
                <span className="text-sm text-slate-500">
                  {new Date(item.timestamp).toLocaleString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}