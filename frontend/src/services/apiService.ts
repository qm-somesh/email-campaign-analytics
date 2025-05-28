import axios, { AxiosResponse } from 'axios';
import {
  Campaign,
  CampaignStats,
  EmailList,
  Recipient,
  EmailEvent,
  DashboardMetrics,
  RecentCampaign,
  RecentEvent,
  PaginatedResponse,
  CampaignFilters,
  RecipientFilters,
  SortOptions,
  PaginationOptions
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

// Campaign API
export const campaignApi = {
  // Get all campaigns with pagination and filters
  getCampaigns: async (
    pagination: PaginationOptions,
    filters?: CampaignFilters,
    sort?: SortOptions
  ): Promise<PaginatedResponse<Campaign>> => {
    const params = new URLSearchParams();
    params.append('pageNumber', pagination.pageNumber.toString());
    params.append('pageSize', pagination.pageSize.toString());
      if (filters?.status) params.append('status', filters.status);
    if (filters?.type) params.append('type', filters.type);
    if (filters?.fromDate) params.append('fromDate', filters.fromDate);
    if (filters?.toDate) params.append('toDate', filters.toDate);
    if (filters?.search) params.append('search', filters.search);
    
    if (sort?.field) params.append('sortBy', sort.field);
    if (sort?.direction) params.append('sortDirection', sort.direction);

    const response = await apiClient.get<PaginatedResponse<Campaign>>(`/campaigns?${params}`);
    return response.data;
  },
  // Get specific campaign
  getCampaign: async (id: string): Promise<Campaign> => {
    const response = await apiClient.get<Campaign>(`/campaigns/${id}`);
    return response.data;
  },

  // Get campaign statistics
  getCampaignStats: async (id: string): Promise<CampaignStats> => {
    const response = await apiClient.get<CampaignStats>(`/campaigns/${id}/stats`);
    return response.data;
  },

  // Get campaign email events
  getCampaignEvents: async (
    id: string,
    pagination: PaginationOptions,
    eventType?: string
  ): Promise<PaginatedResponse<EmailEvent>> => {
    const params = new URLSearchParams();
    params.append('pageNumber', pagination.pageNumber.toString());
    params.append('pageSize', pagination.pageSize.toString());
    if (eventType) params.append('eventType', eventType);

    const response = await apiClient.get<PaginatedResponse<EmailEvent>>(`/campaigns/${id}/events?${params}`);
    return response.data;
  },
};

// Dashboard API
export const dashboardApi = {
  // Get dashboard metrics
  getMetrics: async (): Promise<DashboardMetrics> => {
    const response = await apiClient.get<DashboardMetrics>('/dashboard/metrics');
    return response.data;
  },

  // Get recent campaigns
  getRecentCampaigns: async (limit: number = 10): Promise<RecentCampaign[]> => {
    const response = await apiClient.get<RecentCampaign[]>(`/dashboard/recent-campaigns?limit=${limit}`);
    return response.data;
  },

  // Get recent events
  getRecentEvents: async (limit: number = 20): Promise<RecentEvent[]> => {
    const response = await apiClient.get<RecentEvent[]>(`/dashboard/recent-events?limit=${limit}`);
    return response.data;
  },
};

// Email Lists API
export const emailListApi = {
  // Get all email lists
  getEmailLists: async (): Promise<EmailList[]> => {
    const response = await apiClient.get<EmailList[]>('/emaillists');
    return response.data;
  },
  // Get specific email list
  getEmailList: async (id: string): Promise<EmailList> => {
    const response = await apiClient.get<EmailList>(`/emaillists/${id}`);
    return response.data;
  },
};

// Recipients API
export const recipientApi = {
  // Get recipients with pagination and filters
  getRecipients: async (
    pagination: PaginationOptions,
    filters?: RecipientFilters,
    sort?: SortOptions
  ): Promise<PaginatedResponse<Recipient>> => {
    const params = new URLSearchParams();
    params.append('pageNumber', pagination.pageNumber.toString());
    params.append('pageSize', pagination.pageSize.toString());
    
    if (filters?.listId) params.append('listId', filters.listId.toString());
    if (filters?.status) params.append('status', filters.status);
    if (filters?.search) params.append('search', filters.search);
    
    if (sort?.field) params.append('sortBy', sort.field);
    if (sort?.direction) params.append('sortDirection', sort.direction);

    const response = await apiClient.get<PaginatedResponse<Recipient>>(`/recipients?${params}`);
    return response.data;
  },
  // Get specific recipient
  getRecipient: async (id: string): Promise<Recipient> => {
    const response = await apiClient.get<Recipient>(`/recipients/${id}`);
    return response.data;
  },
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

export default {
  campaignApi,
  dashboardApi,
  emailListApi,
  recipientApi,
  formatApiError,
  isApiError,
};
