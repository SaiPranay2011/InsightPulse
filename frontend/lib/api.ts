import axios, { AxiosInstance } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

class ApiClient {
  private instance: AxiosInstance;

  constructor() {
    this.instance = axios.create({
      baseURL: API_BASE_URL,
      timeout: 30000,
    });

    // Add token to requests
    this.instance.interceptors.request.use((config) => {
      const token = typeof window !== 'undefined' ? localStorage.getItem('authToken') : null;
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Handle errors
    this.instance.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Auth endpoints
  register(email: string, password: string, firstName: string, lastName: string) {
    return this.instance.post('/auth/register', { email, password, firstName, lastName });
  }

  login(email: string, password: string) {
    return this.instance.post('/auth/login', { email, password });
  }

  // Metric endpoints
  ingestMetric(metricName: string, value: number, dimension?: string) {
    return this.instance.post('/metrics/ingest', { metricName, value, dimension });
  }

  getMetrics(metricName: string, startDate: string, endDate: string, dimension?: string) {
    return this.instance.get(`/metrics/${metricName}`, {
      params: { startDate, endDate, dimension },
    });
  }

  getAggregatedMetric(metricName: string, level: number = 1, periodStart?: string) {
    return this.instance.get(`/metrics/aggregated/${metricName}`, {
      params: { level, periodStart },
    });
  }

  // Dashboard endpoints
  getDashboards() {
    return this.instance.get('/dashboards');
  }

  getDashboard(id: string) {
    return this.instance.get(`/dashboards/${id}`);
  }

  createDashboard(name: string, description: string) {
    return this.instance.post('/dashboards', {
      name,
      description,
      refreshIntervalSeconds: 300,
      isPublic: false,
    });
  }

  addWidget(dashboardId: string, title: string, type: number, metric: string, dimension?: string) {
    return this.instance.post(`/dashboards/${dashboardId}/widgets`, {
      title,
      type,
      metric,
      dimension,
      positionX: 0,
      positionY: 0,
      width: 6,
      height: 3,
    });
  }

  deleteWidget(dashboardId: string, widgetId: string) {
    return this.instance.delete(`/dashboards/${dashboardId}/widgets/${widgetId}`);
  }

  deleteDashboard(id: string) {
    return this.instance.delete(`/dashboards/${id}`);
  }

  // Alert endpoints
  createAlert(dashboardId: string, name: string, metric: string, condition: number, threshold: number, email?: string) {
    return this.instance.post(`/dashboards/${dashboardId}/alerts`, {
      name,
      metric,
      condition,
      threshold,
      emailAddress: email,
    });
  }

  getAlerts(dashboardId: string) {
    return this.instance.get(`/dashboards/${dashboardId}/alerts`);
  }

  deleteAlert(dashboardId: string, alertId: string) {
    return this.instance.delete(`/dashboards/${dashboardId}/alerts/${alertId}`);
  }

  getAlertHistory(dashboardId: string, alertId: string) {
    return this.instance.get(`/dashboards/${dashboardId}/alerts/${alertId}/history`);
  }
}

export const api = new ApiClient();