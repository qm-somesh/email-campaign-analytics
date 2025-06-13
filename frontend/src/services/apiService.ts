import axios, { AxiosResponse } from 'axios';
import {
  DashboardMetrics,
  RecentEvent,
  NaturalLanguageQueryRequest,
  NaturalLanguageQueryResponse,
  QueryRequestDto,
  QueryIntent,
  NaturalLanguageStatus,
  EmailTriggerReport,
  EmailTriggerReportFilter,
  EmailTriggerRequest,
  EmailTriggerNaturalLanguageResponse,
  EmailTriggerReportsResponse,
  EmailTriggerSummaryResponse
} from '../types';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5037/api';

// Create axios instance with default configuration
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for logging
apiClient.interceptors.request.use(
  (config) => {
    console.log(`Making ${config.method?.toUpperCase()} request to ${config.url}`);
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Add response interceptor for error handling
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);

// Dashboard API
export const dashboardApi = {
  // Get dashboard metrics
  getMetrics: async (): Promise<DashboardMetrics> => {
    const response = await apiClient.get<DashboardMetrics>('/dashboard/metrics');
    return response.data;
  },

  // Get recent events
  getRecentEvents: async (limit: number = 20): Promise<RecentEvent[]> => {
    const response = await apiClient.get<RecentEvent[]>(`/dashboard/recent-events?limit=${limit}`);
    return response.data;
  },
};

// Email Trigger API methods
const emailTriggerApi = {
  // Get all email trigger reports with pagination
  getEmailTriggerReports: async (
    pageNumber: number = 1,
    pageSize: number = 50
  ): Promise<EmailTriggerReportsResponse> => {
    const response = await apiClient.get<EmailTriggerReportsResponse>(
      `/emailtriggerreport?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
    return response.data;
  },

  // Get email trigger report by strategy name
  getEmailTriggerReportByStrategy: async (
    strategyName: string
  ): Promise<EmailTriggerReport> => {
    const response = await apiClient.get<EmailTriggerReport>(
      `/emailtriggerreport/${encodeURIComponent(strategyName)}`
    );
    return response.data;
  },

  // Get email trigger summary statistics
  getEmailTriggerSummary: async (): Promise<EmailTriggerSummaryResponse> => {
    const response = await apiClient.get<EmailTriggerSummaryResponse>(
      '/emailtriggerreport/summary'
    );
    return response.data;
  },

  // Get available strategy names
  getStrategyNames: async (): Promise<string[]> => {
    const response = await apiClient.get<string[]>(
      '/emailtriggerreport/strategy-names'
    );
    return response.data;
  },

  // Get filtered email trigger reports
  getEmailTriggerReportsFiltered: async (
    filter: EmailTriggerReportFilter
  ): Promise<EmailTriggerReportsResponse> => {
    const params = new URLSearchParams();

    if (filter.strategyName) params.append('strategyName', filter.strategyName);
    if (filter.firstEmailSentFrom) params.append('firstEmailSentFrom', filter.firstEmailSentFrom);
    if (filter.firstEmailSentTo) params.append('firstEmailSentTo', filter.firstEmailSentTo);
    if (filter.minTotalEmails !== undefined) params.append('minTotalEmails', filter.minTotalEmails.toString());
    if (filter.maxTotalEmails !== undefined) params.append('maxTotalEmails', filter.maxTotalEmails.toString());
    if (filter.minDeliveredCount !== undefined) params.append('minDeliveredCount', filter.minDeliveredCount.toString());
    if (filter.minOpenedCount !== undefined) params.append('minOpenedCount', filter.minOpenedCount.toString());
    if (filter.minClickedCount !== undefined) params.append('minClickedCount', filter.minClickedCount.toString());
    if (filter.pageNumber) params.append('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params.append('sortBy', filter.sortBy);
    if (filter.sortDirection) params.append('sortDirection', filter.sortDirection);

    const response = await apiClient.get<EmailTriggerReportsResponse>(
      `/emailtriggerreport/filtered?${params.toString()}`
    );
    return response.data;
  },

  // Trigger email campaign via natural language
  triggerCampaignWithNaturalLanguage: async (
    request: EmailTriggerRequest
  ): Promise<EmailTriggerNaturalLanguageResponse> => {
    const response = await apiClient.post<EmailTriggerNaturalLanguageResponse>(
      '/emailtriggerreport/trigger',
      request
    );
    return response.data;
  },
  // Process natural language query for email triggers
  processNaturalLanguageQuery: async (
    query: string,
    includeDebugInfo: boolean = false
  ): Promise<EmailTriggerNaturalLanguageResponse> => {
    const response = await apiClient.post<EmailTriggerNaturalLanguageResponse>(
      `/NaturalLanguage/triggers/query`,
      { 
        query,
        includeDebugInfo 
      }
    );
    return response.data;
  }
};

// Utility functions
export const formatApiError = (error: any): string => {
  if (error.response?.data?.message) {
    return error.response.data.message;
  }
  if (error.response?.data?.title) {
    return error.response.data.title;
  }
  if (error.message) {
    return error.message;
  }
  return 'An unexpected error occurred';
};

export const isApiError = (error: any): boolean => {
  return error.response && error.response.status >= 400;
};

// Natural Language API
export const naturalLanguageApi = {
  // Process a complete natural language query
  processQuery: async (request: NaturalLanguageQueryRequest): Promise<NaturalLanguageQueryResponse> => {
    const response = await apiClient.post<NaturalLanguageQueryResponse>('/NaturalLanguage/query', request);
    return response.data;
  },

  // Extract intent from a query
  extractIntent: async (query: string): Promise<QueryIntent> => {
    const request: QueryRequestDto = { query };
    const response = await apiClient.post<QueryIntent>('/NaturalLanguage/intent', request);
    return response.data;
  },

  // Generate SQL from intent
  generateSql: async (intent: QueryIntent): Promise<string> => {
    const response = await apiClient.post<string>('/NaturalLanguage/sql', intent);
    return response.data;
  },

  // Get LLM service status
  getStatus: async (): Promise<NaturalLanguageStatus> => {
    const response = await apiClient.get<NaturalLanguageStatus>('/NaturalLanguage/status');
    return response.data;
  },
  // Get example queries
  getExamples: async (): Promise<Record<string, string[]>> => {
    const response = await apiClient.get<Record<string, string[]>>('/NaturalLanguage/examples');
    return response.data;
  }
};

const apiService = {
  dashboardApi,
  naturalLanguageApi,
  emailTriggerApi,
  formatApiError,
  isApiError,
};

export default apiService;
