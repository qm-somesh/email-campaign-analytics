// Dashboard Types
export interface DashboardMetrics {
  totalCampaigns: number;
  activeCampaigns: number;
  totalEmailsSent: number;
  overallOpenRate: number;
  overallClickRate: number;
  overallBounceRate: number;
  overallDeliveryRate: number;
  totalRecipients: number;
  activeRecipients: number;
  recentEventsCount: number;
}

export interface RecentEvent {
  id: string;
  eventType: string;
  recipient: string;
  campaignName: string;
  timestamp: string;
  status: string;
}

// API Response Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Filter and Sort Types
export interface CampaignFilters {
  status?: string;
  type?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
}

export interface RecipientFilters {
  listId?: string;
  status?: string;
  search?: string;
}

export interface SortOptions {
  field: string;
  direction: 'asc' | 'desc';
}

export interface PaginationOptions {
  pageNumber: number;
  pageSize: number;
}

// Natural Language Query Types
export interface QueryFilter {
  field: string;
  operator: string;
  value: string;
}

export interface QueryIntent {
  queryType: string;
  action: string;
  entities: Record<string, any>;
  filters: QueryFilter[];
  groupBy: string[];
  orderBy: string[];
  limit: number;
}

export interface NaturalLanguageQueryRequest {
  query: string;
  context?: string;
  includeDebugInfo?: boolean;
}

export interface QueryRequestDto {
  query: string;
}

export interface NaturalLanguageQueryResponse {
  originalQuery: string;
  intent: string;
  generatedSql: string;
  parameters: Record<string, any>;
  results: any[];
  success: boolean;
  error?: string;
  debugInfo?: {
    llmResponse: string;
    tokensUsed: number;
    llmProcessingTimeMs: number;
    sqlExecutionTimeMs: number;
    confidenceScore: number;
    warnings: string[];
  };
  processingTimeMs: number;
}

export interface NaturalLanguageStatus {
  isAvailable: boolean;
  modelType: string;
  modelPath?: string;
  lastHealthCheck: string;
  capabilities: string[];
}

export interface ExampleQuery {
  category: string;
  examples: string[];
}

// Email Trigger Types
export interface EmailTriggerReport {
  strategyName: string;
  totalEmails: number;
  deliveredCount: number;
  bouncedCount: number;
  openedCount: number;
  clickedCount: number;
  complainedCount: number;
  unsubscribedCount: number;
  firstEmailSent?: string;
  lastEmailSent?: string;
  // Calculated properties
  deliveryRate: number;
  openRate: number;
  clickRate: number;
  bounceRate: number;
  complaintRate: number;
  unsubscribeRate: number;
}

export interface EmailTriggerReportFilter {
  strategyName?: string;
  firstEmailSentFrom?: string;
  firstEmailSentTo?: string;
  minTotalEmails?: number;
  maxTotalEmails?: number;
  minDeliveredCount?: number;
  minOpenedCount?: number;
  minClickedCount?: number;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface EmailTriggerResponse {
  success: boolean;
  campaignId?: string;
  campaignTriggerId?: string;
  recipientsCount: number;
  recipientCount: number;
  strategyName?: string;
  generatedSql?: string;
  message?: string;
  estimatedDeliveryTime?: string;
  errorMessage?: string;
  error?: string;
  triggeredAt: string;
  metadata?: Record<string, any>;
}

export interface EmailTriggerRequest {
  command: string;
  context?: string;
  parameters?: Record<string, any>;
}

export interface EmailTriggerNaturalLanguageResponse {
  originalQuery: string;
  success: boolean;
  intent?: string;
  generatedSql?: string;
  explanation?: string;
  triggerReports?: EmailTriggerReport[];
  summary?: EmailTriggerReport;
  availableStrategies?: string[];
  totalCount?: number;
  parameters?: Record<string, any>;
  processingTimeMs: number;
  error?: string;
  debugInfo?: EmailTriggerQueryDebugInfo;
}

export interface EmailTriggerQueryDebugInfo {
  processingMethod: string;
  serviceMethodCalled?: string;
  extractedFilters?: Record<string, any>;
  warnings: string[];
  additionalInfo?: Record<string, any>;
}

// Email Trigger API Response Types
export interface EmailTriggerReportsResponse extends PaginatedResponse<EmailTriggerReport> {}

export interface EmailTriggerSummaryResponse extends EmailTriggerReport {}

export interface EmailTriggerStrategyNamesResponse {
  strategyNames: string[];
}

// Email Trigger Sort Options
export type EmailTriggerSortField = 
  | 'strategyName'
  | 'totalEmails'
  | 'deliveredCount'
  | 'bouncedCount'
  | 'openedCount'
  | 'clickedCount'
  | 'firstEmailSent'
  | 'lastEmailSent';

export interface EmailTriggerSortOptions {
  field: EmailTriggerSortField;
  direction: 'asc' | 'desc';
}
