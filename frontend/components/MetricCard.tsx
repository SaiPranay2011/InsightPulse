'use client';

import { useState, useEffect } from 'react';
import { api } from '@/lib/api';

interface MetricCardProps {
  metric: string;
  title: string;
  dimension?: string;
}

interface AggregatedData {
  sum: number;
  average: number;
  min: number;
  max: number;
  count: number;
}

export default function MetricCard({ metric, title, dimension }: MetricCardProps) {
  const [data, setData] = useState<AggregatedData | null>(null);
  const [loading, setLoading] = useState(true);
  const [displayType, setDisplayType] = useState<'sum' | 'avg' | 'count' | 'min' | 'max'>('sum');

  useEffect(() => {
    fetchMetric();
    const interval = setInterval(fetchMetric, 5000);
    return () => clearInterval(interval);
  }, [metric, dimension]);

  const fetchMetric = async () => {
    try {
      const today = new Date().toISOString().split('T')[0];
      // Pass dimension so the card respects the widget's dimension filter.
      // Without this, all widgets aggregate across all dimensions regardless
      // of the dimension the widget was configured with.
      const response = await api.getAggregatedMetric(
        metric,
        1,
        `${today}T00:00:00Z`,
        dimension || undefined
      );
      setData(response.data);
    } catch (error) {
      console.error('Failed to fetch metric');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700">
        <p className="text-sm text-slate-400 mb-2">{title}</p>
        <p className="text-2xl font-bold text-slate-400">Loading...</p>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700">
        <p className="text-sm text-slate-400 mb-2">{title}</p>
        <p className="text-2xl font-bold text-slate-600">No data</p>
      </div>
    );
  }

  const displayValue = () => {
    switch (displayType) {
      case 'sum':
        return data.sum.toLocaleString();
      case 'avg':
        return data.average.toFixed(2);
      case 'count':
        return data.count.toString();
      case 'min':
        return data.min.toLocaleString();
      case 'max':
        return data.max.toLocaleString();
      default:
        return data.sum.toLocaleString();
    }
  };

  const displayLabel = () => {
    const labels: Record<string, string> = {
      sum: 'Total',
      avg: 'Average',
      count: 'Count',
      min: 'Min',
      max: 'Max',
    };
    return labels[displayType];
  };

  return (
    <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700 hover:border-blue-500 transition">
      <p className="text-sm text-slate-400 mb-2">{title}</p>
      
      <div className="mb-4">
        <p className="text-3xl font-bold text-blue-400">
          {displayValue()}
        </p>
        <p className="text-xs text-slate-500 mt-1">{displayLabel()}</p>
      </div>

      <div className="flex gap-2 text-xs">
        {(['sum', 'avg', 'count', 'min', 'max'] as const).map((type) => (
          <button
            key={type}
            onClick={() => setDisplayType(type)}
            className={`px-2 py-1 rounded transition ${
              displayType === type
                ? 'bg-blue-600 text-white'
                : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
            }`}
          >
            {type.charAt(0).toUpperCase() + type.slice(1)}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-2 gap-2 mt-4 pt-4 border-t border-slate-600 text-xs">
        <div>
          <p className="text-slate-500">Total</p>
          <p className="font-semibold text-slate-100">{data.sum.toLocaleString()}</p>
        </div>
        <div>
          <p className="text-slate-500">Count</p>
          <p className="font-semibold text-slate-100">{data.count}</p>
        </div>
        <div>
          <p className="text-slate-500">Avg</p>
          <p className="font-semibold text-slate-100">{data.average.toFixed(0)}</p>
        </div>
        <div>
          <p className="text-slate-500">Range</p>
          <p className="font-semibold text-slate-100">{data.min.toLocaleString()}-{data.max.toLocaleString()}</p>
        </div>
      </div>
    </div>
  );
}