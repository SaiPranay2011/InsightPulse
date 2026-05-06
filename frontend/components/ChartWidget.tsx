'use client';

import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';

interface ChartWidgetProps {
  metric: string;
  title: string;
  type: number;
  dimension?: string;
}

export default function ChartWidget({ metric, title, type, dimension }: ChartWidgetProps) {
  const [data, setData] = useState<Array<{ timestamp: string; value: number }>>([]);
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState('7d');
  const [selectedDimension, setSelectedDimension] = useState(dimension || '');
  const [allDimensions, setAllDimensions] = useState<string[]>([]);

  useEffect(() => {
    fetchData();
  }, [metric, timeRange, selectedDimension]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const today = new Date();
      let startDate = new Date();

      if (timeRange === '7d') startDate.setDate(today.getDate() - 7);
      else if (timeRange === '30d') startDate.setDate(today.getDate() - 30);
      else if (timeRange === '1d') startDate = new Date(today);

      const start = startDate.toISOString().split('T')[0];
      const end = today.toISOString().split('T')[0];

      const response = await api.getMetrics(
        metric,
        `${start}T00:00:00Z`,
        `${end}T23:59:59Z`,
        selectedDimension || undefined
      );

      // Extract unique dimensions
      const dims = new Set<string>(
        response.data
          .filter((item: any) => item.dimension)
          .map((item: any) => item.dimension as string)
      );
      setAllDimensions(Array.from(dims));

      const formatted = response.data.map((item: any) => ({
        timestamp: new Date(item.timestamp).toLocaleDateString(),
        value: item.value,
      }));

      setData(formatted);
    } catch (error) {
      console.error('Failed to fetch data');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700 flex items-center justify-center h-96">
        <p className="text-slate-400">Loading chart...</p>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700 flex items-center justify-center h-96">
        <p className="text-slate-400">No data available</p>
      </div>
    );
  }

  return (
    <div className="bg-slate-800 p-6 rounded-lg shadow-lg border border-slate-700">
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-lg font-semibold text-slate-100">{title}</h3>
        
        <div className="flex gap-2 text-xs">
          {['1d', '7d', '30d'].map((range) => (
            <button
              key={range}
              onClick={() => setTimeRange(range)}
              className={`px-3 py-1 rounded transition ${
                timeRange === range
                  ? 'bg-blue-600 text-white'
                  : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
              }`}
            >
              {range === '1d' ? '1D' : range === '7d' ? '7D' : '30D'}
            </button>
          ))}
        </div>
      </div>

      {/* Dimension filter */}
      {allDimensions.length > 0 && (
        <div className="mb-4 flex gap-2 text-xs flex-wrap">
          <span className="text-slate-400">Filter:</span>
          <button
            onClick={() => setSelectedDimension('')}
            className={`px-2 py-1 rounded transition ${
              selectedDimension === ''
                ? 'bg-blue-600 text-white'
                : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
            }`}
          >
            All
          </button>
          {allDimensions.map((dim) => (
            <button
              key={dim}
              onClick={() => setSelectedDimension(dim)}
              className={`px-2 py-1 rounded transition ${
                selectedDimension === dim
                  ? 'bg-blue-600 text-white'
                  : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
              }`}
            >
              {dim}
            </button>
          ))}
        </div>
      )}

      <ResponsiveContainer width="100%" height={300}>
        {type === 1 ? (
          <LineChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
            <XAxis 
              dataKey="timestamp" 
              tick={{ fontSize: 12, fill: '#cbd5e1' }}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis tick={{ fontSize: 12, fill: '#cbd5e1' }} />
            <Tooltip 
              formatter={(value) => value != null ? value.toLocaleString() : ''}
              contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #475569', borderRadius: '8px' }}
              labelStyle={{ color: '#e2e8f0' }}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="value"
              stroke="#3b82f6"
              dot={false}
              isAnimationActive={false}
              strokeWidth={2}
              name={metric}
            />
          </LineChart>
        ) : (
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
            <XAxis 
              dataKey="timestamp"
              tick={{ fontSize: 12, fill: '#cbd5e1' }}
              angle={-45}
              textAnchor="end"
              height={80}
            />
            <YAxis tick={{ fontSize: 12, fill: '#cbd5e1' }} />
            <Tooltip 
              formatter={(value) => value != null ? value.toLocaleString() : ''}
              contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #475569', borderRadius: '8px' }}
              labelStyle={{ color: '#e2e8f0' }}
            />
            <Legend />
            <Bar
              dataKey="value"
              fill="#3b82f6"
              name={metric}
              radius={[8, 8, 0, 0]}
            />
          </BarChart>
        )}
      </ResponsiveContainer>

      <div className="mt-4 pt-4 border-t border-slate-600 text-xs text-slate-400">
        <p>
          Showing {data.length} data points from {timeRange === '1d' ? 'today' : timeRange === '7d' ? 'last 7 days' : 'last 30 days'}
          {selectedDimension && ` for ${selectedDimension}`}
        </p>
      </div>
    </div>
  );
}
