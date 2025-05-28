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
  Avatar,
} from '@mui/material';
import { Search, Person, Email as EmailIcon, TrendingUp } from '@mui/icons-material';
import { recipientApi, emailListApi } from '../services/apiService';
import {
  Recipient,
  EmailList,
  PaginatedResponse,
  RecipientFilters,
  SortOptions,
  PaginationOptions,
} from '../types';
import {
  formatDate,
  formatNumber,
  getStatusColor,
} from '../utils';

const RecipientsPage: React.FC = () => {
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [emailLists, setEmailLists] = useState<EmailList[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  
  // Pagination
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
    // Filters
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [listFilter, setListFilter] = useState<string | ''>('');
  
  // Sorting
  const [sortField, setSortField] = useState('lastEmailDate');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [listsData] = await Promise.all([
        emailListApi.getEmailLists(),
      ]);
      
      setEmailLists(listsData);

      const pagination: PaginationOptions = {
        pageNumber: page + 1, // API is 1-based
        pageSize: rowsPerPage,
      };

      const filters: RecipientFilters = {
        search: search || undefined,
        status: statusFilter || undefined,
        listId: listFilter || undefined,
      };

      const sort: SortOptions = {
        field: sortField,
        direction: sortDirection,
      };

      const recipientsData: PaginatedResponse<Recipient> = await recipientApi.getRecipients(
        pagination,
        filters,
        sort
      );

      setRecipients(recipientsData.items);
      setTotalCount(recipientsData.totalCount);
    } catch (err: any) {
      console.error('Error fetching recipients:', err);
      setError('Failed to load recipients. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [page, rowsPerPage, search, statusFilter, listFilter, sortField, sortDirection]);

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
  if (loading && recipients.length === 0) {
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

  // Calculate metrics with null safety
  const activeRecipients = recipients ? recipients.filter(r => r.status === 'active').length : 0;
  const totalEmailsReceived = recipients ? recipients.reduce((sum, r) => sum + (r.totalEmailsReceived || 0), 0) : 0;
  const totalOpens = recipients ? recipients.reduce((sum, r) => sum + (r.totalOpens || 0), 0) : 0;

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Recipients
      </Typography>      {/* Summary Cards */}
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
                <Person color="primary" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(totalCount)}</Typography>
                  <Typography color="textSecondary">Total Recipients</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
        <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <TrendingUp color="success" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(activeRecipients)}</Typography>
                  <Typography color="textSecondary">Active Recipients</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
        <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <EmailIcon color="info" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(totalEmailsReceived)}</Typography>
                  <Typography color="textSecondary">Emails Received</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
        <Box flex={{ xs: '1 1 100%', sm: '1 1 45%', md: '1 1 22%' }}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <TrendingUp color="warning" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(totalOpens)}</Typography>
                  <Typography color="textSecondary">Total Opens</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>      {/* Filters */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box 
          display="flex" 
          flexDirection={{ xs: 'column', md: 'row' }} 
          gap={3} 
          alignItems="center"
        >
          <Box flex={{ xs: '1 1 100%', md: '2 1 40%' }}>
            <TextField
              fullWidth
              label="Search recipients"
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
          <Box flex={{ xs: '1 1 100%', md: '1 1 30%' }}>
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                value={statusFilter}
                label="Status"
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="active">Active</MenuItem>
                <MenuItem value="inactive">Inactive</MenuItem>
                <MenuItem value="unsubscribed">Unsubscribed</MenuItem>
                <MenuItem value="bounced">Bounced</MenuItem>
              </Select>
            </FormControl>
          </Box>
          <Box flex={{ xs: '1 1 100%', md: '1 1 30%' }}>
            <FormControl fullWidth>
              <InputLabel>Email List</InputLabel>
              <Select
                value={listFilter}
                label="Email List"
                onChange={(e) => setListFilter(e.target.value as string | '')}
              >
                <MenuItem value="">All Lists</MenuItem>
                {emailLists.map((list) => (                  <MenuItem key={list.listId} value={list.listId}>
                    {list.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Recipients Table */}
      <Paper sx={{ width: '100%', overflow: 'hidden' }}>
        <TableContainer sx={{ maxHeight: 600 }}>
          <Table stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 'bold' }}>Recipient</TableCell>
                {getSortableHeader('status', 'Status')}
                <TableCell sx={{ fontWeight: 'bold' }}>Email List</TableCell>
                {getSortableHeader('totalEmailsReceived', 'Emails Received')}
                {getSortableHeader('totalOpens', 'Opens')}
                {getSortableHeader('totalClicks', 'Clicks')}
                {getSortableHeader('subscriptionDate', 'Subscribed')}
                {getSortableHeader('lastEmailDate', 'Last Email')}
              </TableRow>
            </TableHead>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">
                    <CircularProgress />
                  </TableCell>
                </TableRow>
              ) : !recipients || recipients.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">
                    <Typography variant="body1" color="textSecondary">
                      No recipients found
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (                recipients.map((recipient) => (
                  <TableRow
                    key={recipient.recipientId}
                    hover
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell>
                      <Box display="flex" alignItems="center">                        <Avatar sx={{ mr: 2, bgcolor: 'primary.main' }}>
                          {(recipient.firstName?.charAt(0) || 'U')}{(recipient.lastName?.charAt(0) || 'U')}
                        </Avatar>
                        <Box>                          <Typography variant="subtitle2" fontWeight="bold">
                            {recipient.firstName || 'Unknown'} {recipient.lastName || 'User'}
                          </Typography>
                          <Typography variant="body2" color="textSecondary">
                            {recipient.emailAddress}
                          </Typography>                          {recipient.tags && recipient.tags.length > 0 && (
                            <Box mt={0.5} display="flex" gap={0.5} flexWrap="wrap">
                              {recipient.tags.slice(0, 2).map((tag: string) => (
                                <Chip
                                  key={tag}
                                  label={tag}
                                  size="small"
                                  variant="outlined"
                                />
                              ))}                              {recipient.tags && recipient.tags.length > 2 && (
                                <Chip
                                  label={`+${recipient.tags.length - 2}`}
                                  size="small"
                                  variant="outlined"
                                />
                              )}
                            </Box>
                          )}
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={recipient.status}
                        color={getStatusColor(recipient.status)}
                        size="small"
                      />
                    </TableCell>                    <TableCell>
                      <Typography variant="body2">
                        {recipient.emailListName || 'N/A'}
                      </Typography>
                    </TableCell>
                    <TableCell>{formatNumber(recipient.totalEmailsReceived || 0)}</TableCell>
                    <TableCell>{formatNumber(recipient.totalOpens)}</TableCell>
                    <TableCell>{formatNumber(recipient.totalClicks)}</TableCell>                    <TableCell>
                      <Typography variant="body2">
                        {recipient.subscriptionDate ? formatDate(recipient.subscriptionDate) : 'N/A'}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {recipient.lastEmailDate ? formatDate(recipient.lastEmailDate) : 'N/A'}
                      </Typography>
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

export default RecipientsPage;
