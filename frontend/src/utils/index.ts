// Date formatting utilities
export const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

export const formatDateTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

export const formatDateTimeShort = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const diffInHours = (now.getTime() - date.getTime()) / (1000 * 60 * 60);

  if (diffInHours < 1) {
    const minutes = Math.floor(diffInHours * 60);
    return `${minutes}m ago`;
  } else if (diffInHours < 24) {
    return `${Math.floor(diffInHours)}h ago`;
  } else if (diffInHours < 48) {
    return 'Yesterday';
  } else {
    return formatDate(dateString);
  }
};

// Number formatting utilities
export const formatNumber = (num: number | undefined | null): string => {
  if (num === undefined || num === null || isNaN(num)) {
    return '0';
  }
  return num.toLocaleString();
};

export const formatPercentage = (num: number | undefined | null, decimals: number = 1): string => {
  if (num === undefined || num === null || isNaN(num)) {
    return '0.0%';
  }
  return `${num.toFixed(decimals)}%`;
};

export const formatRate = (numerator: number | undefined | null, denominator: number | undefined | null, decimals: number = 1): string => {
  if (!numerator || !denominator || denominator === 0) return '0.0%';
  const rate = (numerator / denominator) * 100;
  return formatPercentage(rate, decimals);
};

// Email and campaign utilities
export const getStatusColor = (status: string): 'success' | 'error' | 'warning' | 'info' | 'default' => {
  switch (status.toLowerCase()) {
    case 'delivered':
    case 'sent':
    case 'active':
    case 'completed':
      return 'success';
    case 'failed':
    case 'bounced':
    case 'complained':
    case 'unsubscribed':
    case 'inactive':
      return 'error';
    case 'pending':
    case 'scheduled':
    case 'processing':
      return 'warning';
    case 'draft':
    case 'paused':
      return 'info';
    default:
      return 'default';
  }
};

export const getEventTypeColor = (eventType: string): 'success' | 'error' | 'warning' | 'info' | 'default' => {
  switch (eventType.toLowerCase()) {
    case 'delivered':
    case 'opened':
    case 'clicked':
      return 'success';
    case 'bounced':
    case 'failed':
    case 'complained':
    case 'unsubscribed':
      return 'error';
    case 'pending':
    case 'queued':
      return 'warning';
    case 'sent':
      return 'info';
    default:
      return 'default';
  }
};

export const getCampaignTypeIcon = (type: string): string => {
  switch (type.toLowerCase()) {
    case 'promotional':
      return '🎯';
    case 'newsletter':
      return '📰';
    case 'announcement':
      return '📢';
    case 'follow-up':
      return '🔄';
    case 'welcome':
      return '👋';
    case 'transactional':
      return '💼';
    default:
      return '📧';
  }
};

// Data processing utilities
export const calculateEngagementScore = (
  opens: number,
  clicks: number,
  totalSent: number
): number => {
  if (totalSent === 0) return 0;
  const openRate = (opens / totalSent) * 100;
  const clickRate = (clicks / totalSent) * 100;
  // Weighted engagement score: open rate (30%) + click rate (70%)
  return (openRate * 0.3) + (clickRate * 0.7);
};

export const getEngagementLevel = (score: number): {
  level: string;
  color: 'success' | 'error' | 'warning' | 'info';
} => {
  if (score >= 15) return { level: 'Excellent', color: 'success' };
  if (score >= 10) return { level: 'Good', color: 'info' };
  if (score >= 5) return { level: 'Average', color: 'warning' };
  return { level: 'Poor', color: 'error' };
};

// Chart data utilities
export const generateChartColors = (count: number): string[] => {
  const colors = [
    '#1976d2', // Primary blue
    '#388e3c', // Success green
    '#f57c00', // Warning orange
    '#d32f2f', // Error red
    '#7b1fa2', // Purple
    '#0288d1', // Light blue
    '#689f38', // Light green
    '#fbc02d', // Yellow
    '#e64a19', // Deep orange
    '#5d4037', // Brown
  ];
  
  const result = [];
  for (let i = 0; i < count; i++) {
    result.push(colors[i % colors.length]);
  }
  return result;
};

// Validation utilities
export const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

// Search and filter utilities
export const searchFilter = <T extends Record<string, any>>(
  items: T[],
  searchTerm: string,
  searchFields: (keyof T)[]
): T[] => {
  if (!searchTerm.trim()) return items;
  
  const lowerSearchTerm = searchTerm.toLowerCase();
  return items.filter(item =>
    searchFields.some(field => {
      const value = item[field];
      if (typeof value === 'string') {
        return value.toLowerCase().includes(lowerSearchTerm);
      }
      if (typeof value === 'number') {
        return value.toString().includes(lowerSearchTerm);
      }
      return false;
    })
  );
};

// Local storage utilities
export const saveToLocalStorage = (key: string, data: any): void => {
  try {
    localStorage.setItem(key, JSON.stringify(data));
  } catch (error) {
    console.error('Error saving to localStorage:', error);
  }
};

export const loadFromLocalStorage = <T>(key: string, defaultValue: T): T => {
  try {
    const item = localStorage.getItem(key);
    return item ? JSON.parse(item) : defaultValue;
  } catch (error) {
    console.error('Error loading from localStorage:', error);
    return defaultValue;
  }
};

export const removeFromLocalStorage = (key: string): void => {
  try {
    localStorage.removeItem(key);
  } catch (error) {
    console.error('Error removing from localStorage:', error);
  }
};

// Export all utilities
export default {
  formatDate,
  formatDateTime,
  formatDateTimeShort,
  formatNumber,
  formatPercentage,
  formatRate,
  getStatusColor,
  getEventTypeColor,
  getCampaignTypeIcon,
  calculateEngagementScore,
  getEngagementLevel,
  generateChartColors,
  isValidEmail,
  searchFilter,
  saveToLocalStorage,
  loadFromLocalStorage,
  removeFromLocalStorage,
};
