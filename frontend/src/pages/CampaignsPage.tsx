import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  TextField,
  MenuItem,
  TablePagination,
  Tooltip,
  CircularProgress,
  Alert
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  Search as SearchIcon
} from '@mui/icons-material';
import { campaignApi, formatApiError } from '../services/apiService';
import { Campaign, CampaignFilters, PaginationOptions, SortOptions } from '../types';
import { formatNumber, formatDate } from '../utils';

const CampaignsPage: React.FC = () => {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  
  // Pagination state
  const [pagination, setPagination] = useState<PaginationOptions>({
    pageNumber: 1,
    pageSize: 10
  });
    // Filters state
  const [filters, setFilters] = useState<CampaignFilters>({
    search: '',
    status: '',
    type: '',
    fromDate: '',
    toDate: ''
  });
    // Sort state
  const [sort, setSort] = useState<SortOptions>({
    field: 'createdAt',
    direction: 'desc'
  });

  // Load campaigns
  const loadCampaigns = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await campaignApi.getCampaigns(pagination, filters, sort);
      setCampaigns(response.items);
      setTotalCount(response.totalCount);
    } catch (err) {
      setError(formatApiError(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCampaigns();
  }, [pagination, filters, sort]);

  const handlePageChange = (event: unknown, newPage: number) => {
    setPagination({ ...pagination, pageNumber: newPage + 1 });
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setPagination({
      pageNumber: 1,
      pageSize: parseInt(event.target.value, 10)
    });
  };

  const handleFilterChange = (field: keyof CampaignFilters, value: string) => {
    setFilters({ ...filters, [field]: value });
    setPagination({ ...pagination, pageNumber: 1 }); // Reset to first page
  };

  const handleSearch = () => {
    loadCampaigns();
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active':
      case 'sent':
        return 'success';
      case 'draft':
        return 'warning';
      case 'paused':
        return 'info';
      case 'completed':
        return 'primary';
      case 'failed':
        return 'error';
      default:
        return 'default';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const formatNumber = (num: number) => {
    return num.toLocaleString();
  };

  if (loading && campaigns.length === 0) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          Email Campaigns
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            // TODO: Implement create campaign
            console.log('Create campaign clicked');
          }}
        >
          Create Campaign
        </Button>
      </Box>

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box display="flex" flexWrap="wrap" gap={2} alignItems="center">
            <TextField
              label="Search campaigns"
              variant="outlined"
              size="small"
              value={filters.search}
              onChange={(e) => handleFilterChange('search', e.target.value)}
              sx={{ minWidth: 200 }}
              InputProps={{
                endAdornment: (
                  <IconButton onClick={handleSearch} size="small">
                    <SearchIcon />
                  </IconButton>
                )
              }}
            />
            
            <TextField
              select
              label="Status"
              variant="outlined"
              size="small"
              value={filters.status}
              onChange={(e) => handleFilterChange('status', e.target.value)}
              sx={{ minWidth: 120 }}
            >
              <MenuItem value="">All</MenuItem>
              <MenuItem value="active">Active</MenuItem>
              <MenuItem value="draft">Draft</MenuItem>
              <MenuItem value="paused">Paused</MenuItem>
              <MenuItem value="completed">Completed</MenuItem>
              <MenuItem value="failed">Failed</MenuItem>
            </TextField>
            
            <TextField
              select
              label="Type"
              variant="outlined"
              size="small"              value={filters.type}
              onChange={(e) => handleFilterChange('type', e.target.value)}
              sx={{ minWidth: 140 }}
            >
              <MenuItem value="">All Types</MenuItem>
              <MenuItem value="promotional">Promotional</MenuItem>
              <MenuItem value="newsletter">Newsletter</MenuItem>
              <MenuItem value="announcement">Announcement</MenuItem>
              <MenuItem value="transactional">Transactional</MenuItem>
            </TextField>
            
            <TextField
              label="From Date"
              type="date"
              variant="outlined"
              size="small"
              value={filters.fromDate}
              onChange={(e) => handleFilterChange('fromDate', e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            
            <TextField
              label="To Date"
              type="date"
              variant="outlined"
              size="small"
              value={filters.toDate}
              onChange={(e) => handleFilterChange('toDate', e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
          </Box>
        </CardContent>
      </Card>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Campaigns Table */}
      <Card>
        <CardContent sx={{ p: 0 }}>
          <TableContainer component={Paper} elevation={0}>
            <Table>              <TableHead>
                <TableRow>
                  <TableCell>Campaign Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Recipients</TableCell>
                  <TableCell align="right">Delivered</TableCell>
                  <TableCell align="right">Opens</TableCell>
                  <TableCell align="right">Clicks</TableCell>
                  <TableCell>Created Date</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead><TableBody>
                {loading && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <CircularProgress size={24} />
                    </TableCell>
                  </TableRow>
                )}
                {!loading && campaigns && campaigns.length > 0 && campaigns.map((campaign) => (
                  <TableRow key={campaign.campaignId} hover>
                    <TableCell>
                      <Box>
                        <Typography variant="body2" fontWeight="medium">
                          {campaign.name}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {campaign.subject}
                        </Typography>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={campaign.type}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>                    <TableCell>
                      <Chip
                        label={campaign.status}
                        size="small"
                        color={getStatusColor(campaign.status) as any}
                      />
                    </TableCell><TableCell align="right">
                      {formatNumber(campaign.totalRecipients)}
                    </TableCell>
                    <TableCell align="right">
                      {formatNumber(campaign.deliveredCount)}
                    </TableCell>
                    <TableCell align="right">
                      {formatNumber(campaign.openedCount)}
                    </TableCell>
                    <TableCell align="right">
                      {formatNumber(campaign.clickedCount)}
                    </TableCell>
                    <TableCell>
                      {formatDate(campaign.createdAt)}
                    </TableCell>
                    <TableCell align="center">
                      <Box display="flex" gap={1}>
                        <Tooltip title="View Details">
                          <IconButton
                            size="small"
                            onClick={() => {                              // TODO: Navigate to campaign details
                              console.log('View campaign:', campaign.campaignId);
                            }}
                          >
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Edit Campaign">
                          <IconButton
                            size="small"
                            onClick={() => {                              // TODO: Navigate to edit campaign
                              console.log('Edit campaign:', campaign.campaignId);
                            }}
                          >
                            <EditIcon />
                          </IconButton>
                        </Tooltip>
                      </Box>
                    </TableCell>                  </TableRow>
                ))}                {!loading && campaigns && campaigns.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center" sx={{ py: 4 }}>
                      <Typography color="text.secondary">
                        No campaigns found
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
          
          {/* Pagination */}
          <TablePagination
            component="div"
            count={totalCount}
            page={pagination.pageNumber - 1}
            onPageChange={handlePageChange}
            rowsPerPage={pagination.pageSize}
            onRowsPerPageChange={handleRowsPerPageChange}
            rowsPerPageOptions={[5, 10, 25, 50]}
          />
        </CardContent>
      </Card>
    </Box>
  );
};

export default CampaignsPage;