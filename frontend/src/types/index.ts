// Campaign Types
export interface Campaign {
  campaignId: string;
  name: string;
  type: string;
  status: string;
  createdAt: string;
  launchedAt?: string;
  completedAt?: string;
  subject: string;
  fromEmail: string;
  fromName: string;
  totalRecipients: number;
  sentCount: number;
  deliveredCount: number;
  openedCount: number;
  clickedCount: number;
  bouncedCount: number;
  unsubscribedCount: number;
  complaintsCount: number;
  tags?: string[];
  notes?: string;
}

export interface CampaignStats {
  totalEmails: number;
  delivered: number;
  opened: number;
  clicked: number;
  bounced: number;
  complained: number;
  unsubscribed: number;
  failed: number;
  openRate: number;
  clickRate: number;
  bounceRate: number;
  deliveryRate: number;
}

// Email List Types
export interface EmailList {
  listId: string;
  name: string;
  description: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  totalRecipients: number;
  activeRecipients: number;
  bouncedRecipients: number;
  unsubscribedRecipients: number;
  tags?: string[];
  notes?: string;
}

// Recipient Types
export interface Recipient {
  recipientId: string;
  emailAddress: string;
  firstName?: string;
  lastName?: string;
  status: string;
  createdAt: string;
  lastEngagementAt?: string;
  location?: string;
  deviceType?: string;
  preferredLanguage?: string;
  subscribedAt?: string;
  unsubscribedAt?: string;
  unsubscribeReason?: string;
  totalOpens: number;
  totalClicks: number;
  totalBounces: number;
  totalEmailsReceived: number;
  customFields?: string;
  tags?: string[];
  notes?: string;
  emailListId?: string;
  emailListName?: string;
  subscriptionDate?: string;
  lastEmailDate?: string;
}

// Email Event Types
export interface EmailEvent {
  eventId: string;
  campaignId: string;
  recipientId: string;
  eventType: string;
  timestamp: string;
  emailAddress?: string;
  subject?: string;
  reason?: string;
  userAgent?: string;
  ipAddress?: string;
  location?: string;
  deviceType?: string;
  clickUrl?: string;
  additionalData?: string;
}

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
  recentCampaignsCount: number;
  recentEventsCount: number;
}

export interface RecentCampaign {
  campaignId: string;
  name: string;
  type: string;
  status: string;
  createdAt: string;
  subject: string;
  totalRecipients: number;
  sentCount: number;
  deliveredCount: number;
  openedCount: number;
  clickedCount: number;
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
