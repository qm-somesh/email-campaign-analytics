import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  IconButton,
  Tooltip,
  TextField,
  InputAdornment,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Card,
  CardContent,
  CircularProgress,
  Alert,
  Button,
} from '@mui/material';
import {
  Search,
  Visibility,
  FilterList,
  Download,
  Refresh,
  Campaign as CampaignIcon,
  TrendingUp,
  Email,
  OpenInNew,
  TouchApp,
} from '@mui/icons-material';
// Temporarily comment out date picker imports for compatibility
// import { DatePicker } from '@mui/x-date-pickers/DatePicker';
// import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
// import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { campaignApi } from '../services/apiService';
import {
  Campaign,
  PaginatedResponse,
  CampaignFilters,
  SortOptions,
  PaginationOptions,
} from '../types';
import {
  formatDate,
  formatNumber,
  formatPercentage,
  getStatusColor,
  getCampaignTypeIcon,
} from '../utils';

const CampaignsPage: React.FC = () => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  
  // Pagination
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
    // Filters
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [fromDate, setFromDate] = useState<Date | null>(null);
  const [toDate, setToDate] = useState<Date | null>(null);
    // Sorting
  const [sortField, setSortField] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');

  const fetchCampaigns = async () => {
    try {
      setLoading(true);
      setError(null);

      const pagination: PaginationOptions = {
        pageNumber: page + 1, // API is 1-based
        pageSize: rowsPerPage,
      };      const filters: CampaignFilters = {
        search: search || undefined,
        status: statusFilter || undefined,
        type: typeFilter || undefined,
        fromDate: fromDate?.toISOString() || undefined,
        toDate: toDate?.toISOString() || undefined,
      };

      const sort: SortOptions = {
        field: sortField,
        direction: sortDirection,
      };

      const response: PaginatedResponse<Campaign> = await campaignApi.getCampaigns(
        pagination,
        filters,
        sort
      );

      setCampaigns(response.items);
      setTotalCount(response.totalCount);
    } catch (err: any) {
      console.error('Error fetching campaigns:', err);
      setError('Failed to load campaigns. Please try again.');
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => {
    fetchCampaigns();
  }, [page, rowsPerPage, search, statusFilter, typeFilter, fromDate, toDate, sortField, sortDirection]);

  const handlePageChange = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
    setPage(0);
  };
  const clearFilters = () => {
    setSearch('');
    setStatusFilter('');
    setTypeFilter('');
    setFromDate(null);
    setToDate(null);
    setPage(0);
  };

  const getSortableHeader = (field: string, label: string) => (
    <TableCell
      sx={{ cursor: 'pointer', fontWeight: 'bold' }}
      onClick={() => handleSort(field)}
    >
      <Box display="flex" alignItems="center">
        {label}
        {sortField === field && (
          <Box ml={1}>
            {sortDirection === 'asc' ? '↑' : '↓'}
          </Box>
        )}
      </Box>
    </TableCell>
  );

  if (loading && campaigns.length === 0) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="400px"
      >
        <CircularProgress size={60} />
      </Box>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h4" component="h1">
            Email Campaigns
          </Typography>
          <Box display="flex" gap={2}>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={fetchCampaigns}
              disabled={loading}
            >
              Refresh
            </Button>
            <Button
              variant="outlined"
              startIcon={<Download />}
              disabled={campaigns.length === 0}
            >
              Export
            </Button>
          </Box>
        </Box>        {/* Filters */}
        <Paper sx={{ p: 3, mb: 3 }}>
          <Box 
            display="flex" 
            flexDirection={{ xs: 'column', md: 'row' }} 
            flexWrap="wrap" 
            gap={3} 
            alignItems="center"
          >
            <Box flex={{ xs: '1 1 100%', md: '1 1 30%' }}>
              <TextField
                fullWidth
                label="Search campaigns"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Search />
                    </InputAdornment>
                  ),
                }}
              />
            </Box>
            <Box flex={{ xs: '1 1 100%', md: '1 1 15%' }}>
              <FormControl fullWidth>
                <InputLabel>Status</InputLabel>
                <Select
                  value={statusFilter}
                  label="Status"
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="draft">Draft</MenuItem>
                  <MenuItem value="scheduled">Scheduled</MenuItem>
                  <MenuItem value="sending">Sending</MenuItem>
                  <MenuItem value="sent">Sent</MenuItem>
                  <MenuItem value="completed">Completed</MenuItem>
                  <MenuItem value="paused">Paused</MenuItem>
                  <MenuItem value="failed">Failed</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box flex={{ xs: '1 1 100%', md: '1 1 15%' }}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>                <Select
                  value={typeFilter}
                  label="Type"
                  onChange={(e) => setTypeFilter(e.target.value)}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="promotional">Promotional</MenuItem>
                  <MenuItem value="newsletter">Newsletter</MenuItem>
                  <MenuItem value="announcement">Announcement</MenuItem>
                  <MenuItem value="follow-up">Follow-up</MenuItem>
                  <MenuItem value="welcome">Welcome</MenuItem>
                  <MenuItem value="transactional">Transactional</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Box flex={{ xs: '1 1 100%', md: '1 1 15%' }}>
              <TextField
                label="From Date"
                type="date"
                value={fromDate ? fromDate.toISOString().split('T')[0] : ''}
                onChange={(e) => setFromDate(e.target.value ? new Date(e.target.value) : null)}
                InputLabelProps={{ shrink: true }}
                fullWidth
              />
            </Box>
            <Box flex={{ xs: '1 1 100%', md: '1 1 15%' }}>
              <TextField
                label="To Date"
                type="date"
                value={toDate ? toDate.toISOString().split('T')[0] : ''}
                onChange={(e) => setToDate(e.target.value ? new Date(e.target.value) : null)}
                InputLabelProps={{ shrink: true }}
                fullWidth
              />
            </Box>
            <Box flex={{ xs: '1 1 100%', md: '1 1 10%' }}>
              <Button
                variant="outlined"
                startIcon={<FilterList />}
                onClick={clearFilters}
                fullWidth
              >
                Clear
              </Button>
            </Box>
          </Box>
        </Paper>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}        {/* Summary Cards */}
        <Box 
          display="flex" 
          flexDirection={{ xs: 'column', sm: 'row' }} 
          flexWrap="wrap" 
          gap={3} 
          sx={{ mb: 3 }}
        >
          <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center">
                  <CampaignIcon color="primary" sx={{ mr: 2, fontSize: 32 }} />
                  <Box>
                    <Typography variant="h4">{formatNumber(totalCount)}</Typography>
                    <Typography color="textSecondary">Total Campaigns</Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center">
                  <Email color="success" sx={{ mr: 2, fontSize: 32 }} />
                  <Box>
                    <Typography variant="h4">
                      {formatNumber(campaigns.reduce((sum, c) => sum + c.sentCount, 0))}
                    </Typography>
                    <Typography color="textSecondary">Emails Sent</Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center">
                  <OpenInNew color="info" sx={{ mr: 2, fontSize: 32 }} />
                  <Box>
                    <Typography variant="h4">                      {campaigns.length > 0
                        ? formatPercentage(
                            campaigns.reduce((sum, c) => sum + (c.openedCount / c.deliveredCount * 100 || 0), 0) / campaigns.length
                          )
                        : '0%'}
                    </Typography>
                    <Typography color="textSecondary">Avg Open Rate</Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Box>
          <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
            <Card>
              <CardContent>
                <Box display="flex" alignItems="center">
                  <TouchApp color="warning" sx={{ mr: 2, fontSize: 32 }} />
                  <Box>
                    <Typography variant="h4">                      {campaigns.length > 0
                        ? formatPercentage(
                            campaigns.reduce((sum, c) => sum + (c.clickedCount / c.deliveredCount * 100 || 0), 0) / campaigns.length
                          )
                        : '0%'}
                    </Typography>
                    <Typography color="textSecondary">Avg Click Rate</Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Box>
        </Box>

        {/* Campaigns Table */}
        <Paper sx={{ width: '100%', overflow: 'hidden' }}>
          <TableContainer sx={{ maxHeight: 600 }}>
            <Table stickyHeader>
              <TableHead>
                <TableRow>                  <TableCell sx={{ fontWeight: 'bold' }}>Campaign</TableCell>
                  {getSortableHeader('status', 'Status')}
                  {getSortableHeader('type', 'Type')}
                  {getSortableHeader('sentCount', 'Sent')}
                  {getSortableHeader('openedCount', 'Opens')}
                  {getSortableHeader('clickedCount', 'Clicks')}
                  {getSortableHeader('deliveredCount', 'Delivered')}
                  {getSortableHeader('createdAt', 'Created')}
                  <TableCell sx={{ fontWeight: 'bold' }}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : campaigns.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <Typography variant="body1" color="textSecondary">
                        No campaigns found
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (                  campaigns.map((campaign) => (
                    <TableRow
                      key={campaign.campaignId}
                      hover
                      sx={{ cursor: 'pointer' }}
                    >
                      <TableCell>
                        <Box>
                          <Box display="flex" alignItems="center" mb={0.5}>
                            <Typography variant="body1" sx={{ mr: 1 }}>
                              {getCampaignTypeIcon(campaign.type)}
                            </Typography>
                            <Typography variant="subtitle2" fontWeight="bold">
                              {campaign.name}
                            </Typography>
                          </Box>
                          <Typography variant="body2" color="textSecondary" noWrap>
                            {campaign.subject}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Subject: {campaign.subject}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={campaign.status}
                          color={getStatusColor(campaign.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={campaign.type}
                          variant="outlined"
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{formatNumber(campaign.sentCount)}</TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center">                          <Typography variant="body2">
                            {formatPercentage(campaign.openedCount / campaign.deliveredCount * 100)}
                          </Typography>
                          <Box ml={1} width={40} height={6} bgcolor="grey.200" borderRadius={3}>
                            <Box
                              width={`${Math.min((campaign.openedCount / campaign.deliveredCount * 100), 100)}%`}
                              height="100%"
                              bgcolor="info.main"
                              borderRadius={3}
                            />
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center">                          <Typography variant="body2">
                            {formatPercentage(campaign.clickedCount / campaign.deliveredCount * 100)}
                          </Typography>
                          <Box ml={1} width={40} height={6} bgcolor="grey.200" borderRadius={3}>
                            <Box
                              width={`${Math.min((campaign.clickedCount / campaign.deliveredCount * 100), 100)}%`}
                              height="100%"
                              bgcolor="warning.main"
                              borderRadius={3}
                            />
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Box display="flex" alignItems="center">                          <Typography variant="body2">
                            {formatPercentage(campaign.deliveredCount / campaign.sentCount * 100)}
                          </Typography>
                          <Box ml={1} width={40} height={6} bgcolor="grey.200" borderRadius={3}>                            <Box
                              width={`${Math.min((campaign.deliveredCount / campaign.sentCount * 100), 100)}%`}
                              height="100%"
                              bgcolor="success.main"
                              borderRadius={3}
                            />
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {formatDate(campaign.createdAt)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Tooltip title="View Details">
                          <IconButton
                            size="small"
                            onClick={() => {                              // TODO: Navigate to campaign detail page
                              console.log('View campaign:', campaign.campaignId);
                            }}
                          >
                            <Visibility />
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
      </Container>
  );
};

export default CampaignsPage;
