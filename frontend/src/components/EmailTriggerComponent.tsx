import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  TextField,
  Alert,
  CircularProgress,
  Card,
  CardContent,
  Chip,
  IconButton,
  Tooltip,
  Collapse,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  TrendingUp as TrendingUpIcon,
  Email as EmailIcon,
  PlayArrow as PlayIcon,
} from '@mui/icons-material';
import apiService from '../services/apiService';
import {
  EmailTriggerReport,
  EmailTriggerReportFilter,
  EmailTriggerNaturalLanguageResponse,
} from '../types';
import EmailTriggerReportsGrid from './EmailTriggerReportsGrid';
import NaturalLanguageQueryExamples from './QueryExamples';

interface EmailTriggerComponentProps {
  maxItems?: number;
  showFilters?: boolean;
  showNaturalLanguage?: boolean;
  showSummary?: boolean;
  onStrategyClick?: (strategy: EmailTriggerReport) => void;
}

const EmailTriggerComponent: React.FC<EmailTriggerComponentProps> = ({
  maxItems = 10,
  showFilters = false,
  showNaturalLanguage = true,
  showSummary = true,
  onStrategyClick,
}) => {  // State management
  const [reports, setReports] = useState<EmailTriggerReport[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<EmailTriggerReport | null>(null);
  
  // Natural language query state
  const [naturalLanguageQuery, setNaturalLanguageQuery] = useState('');
  const [nlQueryLoading, setNlQueryLoading] = useState(false);
  const [nlQueryResponse, setNlQueryResponse] = useState<EmailTriggerNaturalLanguageResponse | null>(null);
  
  // Filter state
  const [filters] = useState<EmailTriggerReportFilter>({
    pageNumber: 1,
    pageSize: maxItems,
    sortBy: 'totalEmails',
    sortDirection: 'desc',
  });
  // Load initial data
  useEffect(() => {
    loadReports();
    if (showSummary) {
      loadSummary();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadReports = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiService.emailTriggerApi.getEmailTriggerReportsFiltered(filters);
      setReports(response.items || []);
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const loadSummary = async () => {
    try {
      const summaryData = await apiService.emailTriggerApi.getEmailTriggerSummary();
      setSummary(summaryData);
    } catch (err: any) {
      console.error('Failed to load summary:', err);
    }
  };

  const handleNaturalLanguageQuery = async () => {
    if (!naturalLanguageQuery.trim()) return;
    
    setNlQueryLoading(true);
    setNlQueryResponse(null);
    setError(null);
    
    try {
      const response = await apiService.emailTriggerApi.processNaturalLanguageQuery(
        naturalLanguageQuery,
        true
      );
      setNlQueryResponse(response);
      
      // If the response contains trigger reports, update the table
      if (response.triggerReports && response.triggerReports.length > 0) {
        setReports(response.triggerReports.slice(0, maxItems));
      }
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setNlQueryLoading(false);
    }
  };
  const formatRate = (rate: number): string => {
    return `${rate.toFixed(1)}%`;
  };

  const formatNumber = (num: number): string => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    }
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  };

  const getPerformanceColor = (rate: number, type: 'open' | 'click' | 'bounce'): 'success' | 'warning' | 'error' | 'default' => {
    switch (type) {
      case 'open':
        if (rate >= 25) return 'success';
        if (rate >= 15) return 'warning';
        return 'error';
      case 'click':
        if (rate >= 5) return 'success';
        if (rate >= 2) return 'warning';
        return 'error';
      case 'bounce':
        if (rate <= 2) return 'success';
        if (rate <= 5) return 'warning';
        return 'error';
      default:
        return 'default';
    }
  };

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Summary Cards */}      {showSummary && summary && (
        <Box display="flex" flexWrap="wrap" gap={2} sx={{ mb: 3 }}>
          <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
            <Card variant="outlined">
              <CardContent sx={{ pb: 1 }}>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Total Emails
                    </Typography>
                    <Typography variant="h6">
                      {formatNumber(summary.totalEmails)}
                    </Typography>
                  </Box>
                  <EmailIcon color="primary" />
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
            <Card variant="outlined">
              <CardContent sx={{ pb: 1 }}>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Delivery Rate
                    </Typography>
                    <Typography variant="h6">
                      {formatRate(summary.deliveryRate)}
                    </Typography>
                  </Box>
                  <TrendingUpIcon color="success" />
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
            <Card variant="outlined">
              <CardContent sx={{ pb: 1 }}>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Open Rate
                    </Typography>
                    <Typography variant="h6">
                      {formatRate(summary.openRate)}
                    </Typography>
                  </Box>
                  <TrendingUpIcon color="info" />
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
            <Card variant="outlined">
              <CardContent sx={{ pb: 1 }}>
                <Box display="flex" alignItems="center" justifyContent="space-between">
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      Click Rate
                    </Typography>
                    <Typography variant="h6">
                      {formatRate(summary.clickRate)}
                    </Typography>
                  </Box>
                  <PlayIcon color="secondary" />
                </Box>
              </CardContent>
            </Card>
          </Box>
        </Box>
      )}      {/* Natural Language Query */}
      {showNaturalLanguage && (
        <Paper sx={{ p: 2, mb: 2 }}>
          <Typography variant="h6" gutterBottom>
            Ask about Email Triggers
          </Typography>
            {/* Enhanced Query Examples */}
          <Box sx={{ mb: 2 }}>
            <NaturalLanguageQueryExamples 
              onExampleClick={setNaturalLanguageQuery}
              compact={true}
            />
          </Box>

          {/* Query Input */}
          <Box display="flex" gap={1} alignItems="center">
            <TextField
              fullWidth
              size="small"
              variant="outlined"
              placeholder="Ask any question about your email campaigns..."
              value={naturalLanguageQuery}
              onChange={(e) => setNaturalLanguageQuery(e.target.value)}
              onKeyPress={(e) => {
                if (e.key === 'Enter') {
                  handleNaturalLanguageQuery();
                }
              }}
              helperText="Tip: Click any example above or type your own question"
            />
            <Button
              variant="contained"
              size="small"
              onClick={handleNaturalLanguageQuery}
              disabled={nlQueryLoading || !naturalLanguageQuery.trim()}
              startIcon={nlQueryLoading ? <CircularProgress size={16} /> : <PlayIcon />}
            >
              Ask
            </Button>
          </Box>
            {nlQueryResponse && (
            <Box sx={{ mt: 2 }}>
              {/* Query Status Summary */}
              <Alert severity={nlQueryResponse.success ? "success" : "error"} sx={{ mb: 2 }}>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography variant="body2">
                    {nlQueryResponse.explanation || nlQueryResponse.error || 'Query processed successfully'}
                  </Typography>
                  <Box display="flex" gap={1} alignItems="center">
                    {nlQueryResponse.intent && (
                      <Chip 
                        label={`Intent: ${nlQueryResponse.intent}`} 
                        size="small" 
                        variant="outlined"
                      />
                    )}
                    {nlQueryResponse.processingTimeMs && (
                      <Chip 
                        label={`${nlQueryResponse.processingTimeMs}ms`} 
                        size="small" 
                        color="info"
                        variant="outlined"
                      />
                    )}
                  </Box>
                </Box>
              </Alert>

              {/* Detailed Results Grid */}
              {nlQueryResponse.success && nlQueryResponse.triggerReports && nlQueryResponse.triggerReports.length > 0 && (
                <EmailTriggerReportsGrid
                  reports={nlQueryResponse.triggerReports}
                  title="Natural Language Query Results"
                  showPagination={nlQueryResponse.triggerReports.length > 10}
                  showExport={true}
                  onStrategyClick={onStrategyClick}
                  maxHeight={400}
                />
              )}

              {/* Summary Data (if available) */}
              {nlQueryResponse.success && nlQueryResponse.summary && (
                <Paper sx={{ p: 2, mt: 2, bgcolor: 'background.default' }}>
                  <Typography variant="h6" gutterBottom>
                    Summary Metrics
                  </Typography>
                  <Box display="flex" flexWrap="wrap" gap={2}>
                    <Box sx={{ flex: '1 1 200px', minWidth: '150px' }}>
                      <Typography variant="body2" color="text.secondary">Total Emails</Typography>
                      <Typography variant="h6">{formatNumber(nlQueryResponse.summary.totalEmails)}</Typography>
                    </Box>
                    <Box sx={{ flex: '1 1 200px', minWidth: '150px' }}>
                      <Typography variant="body2" color="text.secondary">Delivery Rate</Typography>
                      <Typography variant="h6" color={getPerformanceColor(nlQueryResponse.summary.deliveryRate, 'open')}>
                        {formatRate(nlQueryResponse.summary.deliveryRate)}
                      </Typography>
                    </Box>
                    <Box sx={{ flex: '1 1 200px', minWidth: '150px' }}>
                      <Typography variant="body2" color="text.secondary">Open Rate</Typography>
                      <Typography variant="h6" color={getPerformanceColor(nlQueryResponse.summary.openRate, 'open')}>
                        {formatRate(nlQueryResponse.summary.openRate)}
                      </Typography>
                    </Box>
                    <Box sx={{ flex: '1 1 200px', minWidth: '150px' }}>
                      <Typography variant="body2" color="text.secondary">Click Rate</Typography>
                      <Typography variant="h6" color={getPerformanceColor(nlQueryResponse.summary.clickRate, 'click')}>
                        {formatRate(nlQueryResponse.summary.clickRate)}
                      </Typography>
                    </Box>
                  </Box>
                </Paper>
              )}

              {/* Available Strategies (if query didn't find specific data) */}
              {nlQueryResponse.success && nlQueryResponse.availableStrategies && nlQueryResponse.availableStrategies.length > 0 && (
                <Paper sx={{ p: 2, mt: 2 }}>
                  <Typography variant="h6" gutterBottom>
                    Available Strategies
                  </Typography>
                  <Box display="flex" flexWrap="wrap" gap={1}>
                    {nlQueryResponse.availableStrategies.map((strategy, index) => (
                      <Chip
                        key={index}
                        label={strategy}
                        variant="outlined"
                        size="small"
                        onClick={() => setNaturalLanguageQuery(`Show metrics for "${strategy}"`)}
                        sx={{ cursor: 'pointer' }}
                      />
                    ))}
                  </Box>
                </Paper>
              )}

              {/* Debug Information (if available) */}
              {nlQueryResponse.debugInfo && (
                <Collapse in={true}>
                  <Paper sx={{ p: 2, mt: 2, bgcolor: 'grey.50' }}>
                    <Typography variant="subtitle2" gutterBottom>
                      Debug Information
                    </Typography>
                    <Box display="flex" flexWrap="wrap" gap={1}>
                      <Chip 
                        label={`Method: ${nlQueryResponse.debugInfo.processingMethod}`} 
                        size="small" 
                        variant="outlined"
                      />
                      {nlQueryResponse.debugInfo.serviceMethodCalled && (
                        <Chip 
                          label={`Service: ${nlQueryResponse.debugInfo.serviceMethodCalled}`} 
                          size="small" 
                          variant="outlined"
                        />
                      )}
                    </Box>
                  </Paper>
                </Collapse>
              )}
            </Box>
          )}
        </Paper>
      )}      {/* Reports Grid */}
      {loading ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <CircularProgress size={60} />
          <Typography variant="body1" color="text.secondary" sx={{ mt: 2 }}>
            Loading email trigger reports...
          </Typography>
        </Paper>
      ) : (
        <Box>
          <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
            <Typography variant="h6">Email Trigger Reports</Typography>
            <Tooltip title="Refresh Data">
              <IconButton onClick={loadReports} disabled={loading}>
                <RefreshIcon />
              </IconButton>
            </Tooltip>
          </Box>
          <EmailTriggerReportsGrid
            reports={reports}
            title="Email Trigger Reports"
            showPagination={true}
            showExport={true}
            onStrategyClick={onStrategyClick}
            maxHeight={600}
          />
        </Box>
      )}
    </Box>
  );
};

export default EmailTriggerComponent;
