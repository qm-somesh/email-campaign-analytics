import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Paper,
  Button,
  TextField,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  SelectChangeEvent,
  Alert,
  CircularProgress,
  Chip,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  FilterList as FilterIcon,
  Clear as ClearIcon,
  TrendingUp as TrendingUpIcon,
  Email as EmailIcon,
  PlayArrow as PlayIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import apiService from '../services/apiService';
import {
  EmailTriggerReport,
  EmailTriggerReportFilter,
  EmailTriggerSortField,
  EmailTriggerRequest,
  EmailTriggerNaturalLanguageResponse,
} from '../types';

const EmailTriggerPage: React.FC = () => {
  // State management
  const [reports, setReports] = useState<EmailTriggerReport[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<EmailTriggerReport | null>(null);
  const [strategyNames, setStrategyNames] = useState<string[]>([]);
  
  // Pagination state
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  
  // Filter state
  const [filters, setFilters] = useState<EmailTriggerReportFilter>({
    pageNumber: 1,
    pageSize: 25,
    sortBy: 'strategyName',
    sortDirection: 'asc',
  });
  const [showFilters, setShowFilters] = useState(false);
  
  // Natural language query state
  const [naturalLanguageQuery, setNaturalLanguageQuery] = useState('');
  const [nlQueryLoading, setNlQueryLoading] = useState(false);
  const [nlQueryResponse, setNlQueryResponse] = useState<EmailTriggerNaturalLanguageResponse | null>(null);
  
  // Dialog state for strategy details
  const [selectedStrategy, setSelectedStrategy] = useState<EmailTriggerReport | null>(null);
  const [strategyDialogOpen, setStrategyDialogOpen] = useState(false);

  // Load initial data
  useEffect(() => {
    loadReports();
    loadSummary();
    loadStrategyNames();
  }, []);

  // Load reports with current filters
  useEffect(() => {
    loadReports();
  }, [filters]);

  const loadReports = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiService.emailTriggerApi.getEmailTriggerReportsFiltered(filters);
      setReports(response.items || []);
      setTotalCount(response.totalCount || 0);
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

  const loadStrategyNames = async () => {
    try {
      const names = await apiService.emailTriggerApi.getStrategyNames();
      setStrategyNames(names);
    } catch (err: any) {
      console.error('Failed to load strategy names:', err);
    }
  };

  const handleFilterChange = (field: keyof EmailTriggerReportFilter, value: any) => {
    setFilters(prev => ({
      ...prev,
      [field]: value,
      pageNumber: 1, // Reset to first page when filters change
    }));
    setPage(0); // Reset Material-UI pagination
  };

  const handlePageChange = (event: unknown, newPage: number) => {
    setPage(newPage);
    setFilters(prev => ({
      ...prev,
      pageNumber: newPage + 1,
    }));
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newRowsPerPage = parseInt(event.target.value, 10);
    setRowsPerPage(newRowsPerPage);
    setPage(0);
    setFilters(prev => ({
      ...prev,
      pageSize: newRowsPerPage,
      pageNumber: 1,
    }));
  };

  const handleSortChange = (field: EmailTriggerSortField) => {
    const isCurrentField = filters.sortBy === field;
    const newDirection = isCurrentField && filters.sortDirection === 'asc' ? 'desc' : 'asc';
    
    setFilters(prev => ({
      ...prev,
      sortBy: field,
      sortDirection: newDirection,
    }));
  };

  const clearFilters = () => {
    setFilters({
      pageNumber: 1,
      pageSize: rowsPerPage,
      sortBy: 'strategyName',
      sortDirection: 'asc',
    });
    setPage(0);
  };

  const handleNaturalLanguageQuery = async () => {
    if (!naturalLanguageQuery.trim()) return;
    
    setNlQueryLoading(true);
    setNlQueryResponse(null);
    setError(null);
      try {
      const response = await apiService.emailTriggerApi.processNaturalLanguageQuery(
        naturalLanguageQuery,
        true // Include debug info
      );
      
      // Debug logging
      console.log('Natural Language Query Response:', response);
      console.log('Success:', response.success);
      console.log('Trigger Reports:', response.triggerReports);
      console.log('Trigger Reports Length:', response.triggerReports?.length);      
      setNlQueryResponse(response);
      
      // Debug logging for conditional rendering
      console.log('Rendering conditions after setting response:', {
        success: response.success,
        hasTriggerReports: !!response.triggerReports,
        triggerReportsLength: response.triggerReports?.length,
        conditionResult: response.success && response.triggerReports && response.triggerReports.length > 0
      });
      
      // If the response contains trigger reports, update the main table
      if (response.triggerReports && response.triggerReports.length > 0) {
        setReports(response.triggerReports);
        setTotalCount(response.totalCount || response.triggerReports.length);
      }
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setNlQueryLoading(false);
    }
  };

  const handleStrategyClick = async (strategyName: string) => {
    try {
      const strategyReport = await apiService.emailTriggerApi.getEmailTriggerReportByStrategy(strategyName);
      setSelectedStrategy(strategyReport);
      setStrategyDialogOpen(true);
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    }
  };

  const formatRate = (rate: number): string => {
    return `${rate.toFixed(2)}%`;
  };

  const formatDate = (dateString: string | null | undefined): string => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString();
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Container maxWidth="xl" sx={{ mt: 2, mb: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Typography variant="h4" component="h1" gutterBottom>
            Email Trigger Reports
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            Monitor and analyze email campaign triggers and performance
          </Typography>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}        {/* Summary Cards */}
        {summary && (
          <Box display="flex" flexWrap="wrap" gap={3} sx={{ mb: 3 }}>
            <Box sx={{ flex: '1 1 250px', minWidth: '250px' }}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center">
                    <EmailIcon color="primary" sx={{ mr: 1 }} />
                    <Box>
                      <Typography color="text.secondary" gutterBottom>
                        Total Emails
                      </Typography>
                      <Typography variant="h5">
                        {summary.totalEmails.toLocaleString()}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: '250px' }}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center">
                    <TrendingUpIcon color="success" sx={{ mr: 1 }} />
                    <Box>
                      <Typography color="text.secondary" gutterBottom>
                        Delivery Rate
                      </Typography>
                      <Typography variant="h5">
                        {formatRate(summary.deliveryRate)}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: '250px' }}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center">
                    <TrendingUpIcon color="info" sx={{ mr: 1 }} />
                    <Box>
                      <Typography color="text.secondary" gutterBottom>
                        Open Rate
                      </Typography>
                      <Typography variant="h5">
                        {formatRate(summary.openRate)}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
            <Box sx={{ flex: '1 1 250px', minWidth: '250px' }}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center">
                    <PlayIcon color="secondary" sx={{ mr: 1 }} />
                    <Box>
                      <Typography color="text.secondary" gutterBottom>
                        Click Rate
                      </Typography>
                      <Typography variant="h5">
                        {formatRate(summary.clickRate)}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
          </Box>
        )}

        {/* Natural Language Query */}
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography variant="h6" gutterBottom>
            Natural Language Query
          </Typography>
          <Box display="flex" gap={2} alignItems="center">
            <TextField
              fullWidth
              variant="outlined"
              placeholder="Ask questions like: 'Show me campaigns with high open rates' or 'Find strategies with more than 1000 emails'"
              value={naturalLanguageQuery}
              onChange={(e) => setNaturalLanguageQuery(e.target.value)}
              onKeyPress={(e) => {
                if (e.key === 'Enter') {
                  handleNaturalLanguageQuery();
                }
              }}
            />
            <Button
              variant="contained"
              onClick={handleNaturalLanguageQuery}
              disabled={nlQueryLoading || !naturalLanguageQuery.trim()}
              startIcon={nlQueryLoading ? <CircularProgress size={20} /> : <PlayIcon />}
            >
              Query
            </Button>
          </Box>
            {nlQueryResponse && (
            <Box sx={{ mt: 2 }}>
              <Alert 
                severity={nlQueryResponse.success ? "success" : "error"}
                action={
                  <IconButton size="small" onClick={() => setNlQueryResponse(null)}>
                    <ClearIcon />
                  </IconButton>
                }
              >
                <Typography variant="body2">
                  {nlQueryResponse.explanation || nlQueryResponse.error || 'Query processed successfully'}
                </Typography>
                {nlQueryResponse.intent && (
                  <Typography variant="caption" display="block">
                    Intent: {nlQueryResponse.intent}
                  </Typography>
                )}
                {nlQueryResponse.processingTimeMs && (
                  <Typography variant="caption" display="block">
                    Processed in {nlQueryResponse.processingTimeMs}ms
                  </Typography>
                )}
                
                {/* Show summary information if available */}
                {nlQueryResponse.summary && (
                  <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                    Summary: {nlQueryResponse.summary.totalEmails?.toLocaleString()} emails, {formatRate(nlQueryResponse.summary.deliveryRate)} delivery rate, {formatRate(nlQueryResponse.summary.openRate)} open rate
                  </Typography>
                )}
                
                {/* Show available strategies count if available */}
                {nlQueryResponse.availableStrategies && nlQueryResponse.availableStrategies.length > 0 && (
                  <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                    Found {nlQueryResponse.availableStrategies.length} strategies
                  </Typography>
                )}
                
                {/* Show trigger reports count if available */}
                {nlQueryResponse.triggerReports && nlQueryResponse.triggerReports.length > 0 && (
                  <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                    Found {nlQueryResponse.triggerReports.length} trigger reports (updated table below)
                  </Typography>
                )}              </Alert>
              
              {/* Display trigger reports in a compact data grid */}
              {nlQueryResponse.success && nlQueryResponse.triggerReports && nlQueryResponse.triggerReports.length > 0 && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Query Results ({nlQueryResponse.triggerReports.length} strategies)
                  </Typography>
                  <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 400 }}>
                    <Table stickyHeader size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Strategy Name</TableCell>
                          <TableCell align="right">Total Emails</TableCell>
                          <TableCell align="right">Delivered</TableCell>
                          <TableCell align="right">Delivery Rate</TableCell>
                          <TableCell align="right">Open Rate</TableCell>
                          <TableCell align="right">Click Rate</TableCell>
                          <TableCell align="right">Bounce Rate</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {nlQueryResponse.triggerReports.map((report, index) => (
                          <TableRow 
                            key={`${report.strategyName}-${index}`}
                            hover
                            sx={{ cursor: 'pointer' }}
                            onClick={() => handleStrategyClick(report.strategyName)}
                          >
                            <TableCell>
                              <Typography variant="body2" noWrap>
                                {report.strategyName}
                              </Typography>
                            </TableCell>
                            <TableCell align="right">
                              <Typography variant="body2">
                                {report.totalEmails?.toLocaleString() || 0}
                              </Typography>
                            </TableCell>
                            <TableCell align="right">
                              <Typography variant="body2">
                                {report.deliveredCount?.toLocaleString() || 0}
                              </Typography>
                            </TableCell>
                            <TableCell align="right">
                              <Chip
                                label={formatRate(report.deliveryRate)}
                                size="small"
                                color={report.deliveryRate > 90 ? 'success' : report.deliveryRate > 80 ? 'warning' : 'error'}
                                variant="outlined"
                              />
                            </TableCell>
                            <TableCell align="right">
                              <Chip
                                label={formatRate(report.openRate)}
                                size="small"
                                color={report.openRate > 20 ? 'success' : report.openRate > 10 ? 'warning' : 'default'}
                                variant="outlined"
                              />
                            </TableCell>
                            <TableCell align="right">
                              <Chip
                                label={formatRate(report.clickRate)}
                                size="small"
                                color={report.clickRate > 5 ? 'success' : report.clickRate > 2 ? 'warning' : 'default'}
                                variant="outlined"
                              />
                            </TableCell>
                            <TableCell align="right">
                              <Chip
                                label={formatRate(report.bounceRate)}
                                size="small"
                                color={report.bounceRate < 2 ? 'success' : report.bounceRate < 5 ? 'warning' : 'error'}
                                variant="outlined"
                              />
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Box>
              )}
              
              {/* Display available strategies list */}
              {nlQueryResponse.success && nlQueryResponse.availableStrategies && nlQueryResponse.availableStrategies.length > 0 && !nlQueryResponse.triggerReports && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Available Strategies ({nlQueryResponse.availableStrategies.length})
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, maxHeight: 200, overflow: 'auto' }}>
                    {nlQueryResponse.availableStrategies.map((strategy, index) => (
                      <Chip
                        key={`${strategy}-${index}`}
                        label={strategy}
                        size="small"
                        variant="outlined"
                        onClick={() => handleStrategyClick(strategy)}
                        sx={{ cursor: 'pointer' }}
                      />
                    ))}
                  </Box>
                </Box>
              )}
              
              {/* Display summary as a card if it's the main result */}
              {nlQueryResponse.success && nlQueryResponse.summary && !nlQueryResponse.triggerReports && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Email Trigger Summary
                  </Typography>
                  <Card variant="outlined">
                    <CardContent sx={{ py: 1 }}>
                      <Box display="flex" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={2}>
                        <Box textAlign="center">
                          <Typography variant="h6" color="primary">
                            {nlQueryResponse.summary.totalEmails?.toLocaleString() || 0}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Total Emails
                          </Typography>
                        </Box>
                        <Box textAlign="center">
                          <Typography variant="h6" color="success.main">
                            {formatRate(nlQueryResponse.summary.deliveryRate)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Delivery Rate
                          </Typography>
                        </Box>
                        <Box textAlign="center">
                          <Typography variant="h6" color="info.main">
                            {formatRate(nlQueryResponse.summary.openRate)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Open Rate
                          </Typography>
                        </Box>
                        <Box textAlign="center">
                          <Typography variant="h6" color="secondary.main">
                            {formatRate(nlQueryResponse.summary.clickRate)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Click Rate
                          </Typography>
                        </Box>
                        <Box textAlign="center">
                          <Typography variant="h6" color="error.main">
                            {formatRate(nlQueryResponse.summary.bounceRate)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            Bounce Rate
                          </Typography>
                        </Box>
                      </Box>
                    </CardContent>
                  </Card>
                </Box>
              )}
            </Box>
          )}
        </Paper>

        {/* Filters */}
        <Paper sx={{ p: 2, mb: 3 }}>
          <Box display="flex" justifyContent="between" alignItems="center" mb={2}>
            <Typography variant="h6">Filters</Typography>
            <Box>
              <Button
                startIcon={<FilterIcon />}
                onClick={() => setShowFilters(!showFilters)}
                variant="outlined"
                size="small"
                sx={{ mr: 1 }}
              >
                {showFilters ? 'Hide' : 'Show'} Filters
              </Button>
              <Button
                startIcon={<ClearIcon />}
                onClick={clearFilters}
                variant="outlined"
                size="small"
                sx={{ mr: 1 }}
              >
                Clear
              </Button>
              <Button
                startIcon={<RefreshIcon />}
                onClick={loadReports}
                variant="outlined"
                size="small"
              >
                Refresh
              </Button>
            </Box>
          </Box>          {showFilters && (
            <Box display="flex" flexWrap="wrap" gap={2}>
              <Box sx={{ flex: '1 1 250px', minWidth: '200px' }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Strategy Name</InputLabel>
                  <Select
                    value={filters.strategyName || ''}
                    label="Strategy Name"
                    onChange={(e) => handleFilterChange('strategyName', e.target.value || undefined)}
                  >
                    <MenuItem value="">All Strategies</MenuItem>
                    {strategyNames.map((name) => (
                      <MenuItem key={name} value={name}>
                        {name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Box>
              <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                <DatePicker
                  label="First Email From"
                  value={filters.firstEmailSentFrom ? new Date(filters.firstEmailSentFrom) : null}
                  onChange={(date) => handleFilterChange('firstEmailSentFrom', date?.toISOString())}
                  slotProps={{ textField: { size: 'small', fullWidth: true } }}
                />
              </Box>
              <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                <DatePicker
                  label="First Email To"
                  value={filters.firstEmailSentTo ? new Date(filters.firstEmailSentTo) : null}
                  onChange={(date) => handleFilterChange('firstEmailSentTo', date?.toISOString())}
                  slotProps={{ textField: { size: 'small', fullWidth: true } }}
                />
              </Box>
              <Box sx={{ flex: '1 1 150px', minWidth: '150px' }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Min Total Emails"
                  type="number"
                  value={filters.minTotalEmails || ''}
                  onChange={(e) => handleFilterChange('minTotalEmails', e.target.value ? parseInt(e.target.value) : undefined)}
                />
              </Box>
              <Box sx={{ flex: '1 1 150px', minWidth: '150px' }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Max Total Emails"
                  type="number"
                  value={filters.maxTotalEmails || ''}
                  onChange={(e) => handleFilterChange('maxTotalEmails', e.target.value ? parseInt(e.target.value) : undefined)}
                />
              </Box>
              <Box sx={{ flex: '1 1 150px', minWidth: '150px' }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Min Delivered"
                  type="number"
                  value={filters.minDeliveredCount || ''}
                  onChange={(e) => handleFilterChange('minDeliveredCount', e.target.value ? parseInt(e.target.value) : undefined)}
                />
              </Box>
              <Box sx={{ flex: '1 1 150px', minWidth: '150px' }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Min Opened"
                  type="number"
                  value={filters.minOpenedCount || ''}
                  onChange={(e) => handleFilterChange('minOpenedCount', e.target.value ? parseInt(e.target.value) : undefined)}
                />
              </Box>
              <Box sx={{ flex: '1 1 150px', minWidth: '150px' }}>
                <TextField
                  fullWidth
                  size="small"
                  label="Min Clicked"
                  type="number"
                  value={filters.minClickedCount || ''}
                  onChange={(e) => handleFilterChange('minClickedCount', e.target.value ? parseInt(e.target.value) : undefined)}
                />
              </Box>
            </Box>
          )}
        </Paper>

        {/* Reports Table */}
        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('strategyName')}
                      sx={{ textTransform: 'none' }}
                    >
                      Strategy Name
                      {filters.sortBy === 'strategyName' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('totalEmails')}
                      sx={{ textTransform: 'none' }}
                    >
                      Total Emails
                      {filters.sortBy === 'totalEmails' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('deliveredCount')}
                      sx={{ textTransform: 'none' }}
                    >
                      Delivered
                      {filters.sortBy === 'deliveredCount' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="right">Delivery Rate</TableCell>
                  <TableCell align="right">
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('openedCount')}
                      sx={{ textTransform: 'none' }}
                    >
                      Opened
                      {filters.sortBy === 'openedCount' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="right">Open Rate</TableCell>
                  <TableCell align="right">
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('clickedCount')}
                      sx={{ textTransform: 'none' }}
                    >
                      Clicked
                      {filters.sortBy === 'clickedCount' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="right">Click Rate</TableCell>
                  <TableCell align="right">
                    <Button
                      variant="text"
                      onClick={() => handleSortChange('firstEmailSent')}
                      sx={{ textTransform: 'none' }}
                    >
                      First Email
                      {filters.sortBy === 'firstEmailSent' && (
                        <span>{filters.sortDirection === 'asc' ? ' ↑' : ' ↓'}</span>
                      )}
                    </Button>
                  </TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={10} align="center">
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : reports.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={10} align="center">
                      No email trigger reports found
                    </TableCell>
                  </TableRow>
                ) : (
                  reports.map((report) => (
                    <TableRow key={report.strategyName} hover>
                      <TableCell>
                        <Button
                          variant="text"
                          onClick={() => handleStrategyClick(report.strategyName)}
                          sx={{ textTransform: 'none', textAlign: 'left' }}
                        >
                          {report.strategyName}
                        </Button>
                      </TableCell>
                      <TableCell align="right">{report.totalEmails.toLocaleString()}</TableCell>
                      <TableCell align="right">{report.deliveredCount.toLocaleString()}</TableCell>
                      <TableCell align="right">{formatRate(report.deliveryRate)}</TableCell>
                      <TableCell align="right">{report.openedCount.toLocaleString()}</TableCell>
                      <TableCell align="right">{formatRate(report.openRate)}</TableCell>
                      <TableCell align="right">{report.clickedCount.toLocaleString()}</TableCell>
                      <TableCell align="right">{formatRate(report.clickRate)}</TableCell>
                      <TableCell align="right">{formatDate(report.firstEmailSent)}</TableCell>
                      <TableCell align="center">
                        <Tooltip title="View Details">
                          <IconButton size="small" onClick={() => handleStrategyClick(report.strategyName)}>
                            <InfoIcon />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
          
          <TablePagination
            rowsPerPageOptions={[10, 25, 50, 100]}
            component="div"
            count={totalCount}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handlePageChange}
            onRowsPerPageChange={handleRowsPerPageChange}
          />
        </Paper>

        {/* Strategy Details Dialog */}
        <Dialog
          open={strategyDialogOpen}
          onClose={() => setStrategyDialogOpen(false)}
          maxWidth="md"
          fullWidth
        >
          <DialogTitle>
            Strategy Details: {selectedStrategy?.strategyName}
          </DialogTitle>          <DialogContent>
            {selectedStrategy && (
              <Box display="flex" flexWrap="wrap" gap={2} sx={{ mt: 1 }}>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Total Emails</Typography>
                  <Typography variant="h6">{selectedStrategy.totalEmails.toLocaleString()}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Delivered</Typography>
                  <Typography variant="h6">{selectedStrategy.deliveredCount.toLocaleString()}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Delivery Rate</Typography>
                  <Typography variant="h6">{formatRate(selectedStrategy.deliveryRate)}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Open Rate</Typography>
                  <Typography variant="h6">{formatRate(selectedStrategy.openRate)}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Click Rate</Typography>
                  <Typography variant="h6">{formatRate(selectedStrategy.clickRate)}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Bounce Rate</Typography>
                  <Typography variant="h6">{formatRate(selectedStrategy.bounceRate)}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">First Email Sent</Typography>
                  <Typography variant="h6">{formatDate(selectedStrategy.firstEmailSent)}</Typography>
                </Box>
                <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
                  <Typography variant="body2" color="text.secondary">Last Email Sent</Typography>
                  <Typography variant="h6">{formatDate(selectedStrategy.lastEmailSent)}</Typography>
                </Box>
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setStrategyDialogOpen(false)}>Close</Button>
          </DialogActions>
        </Dialog>
      </Container>
    </LocalizationProvider>
  );
};

export default EmailTriggerPage;
