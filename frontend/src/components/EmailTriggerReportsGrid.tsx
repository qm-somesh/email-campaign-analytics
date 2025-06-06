import React, { useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  Chip,
  Avatar,
  Tooltip,
  Button,
  IconButton,
  Menu,
  MenuItem,
  LinearProgress,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Email as EmailIcon,
  Mouse as ClickIcon,
  Visibility as OpenIcon,
  Block as BounceIcon,
  GetApp as ExportIcon,
  MoreVert as MoreIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { EmailTriggerReport } from '../types';

interface EmailTriggerReportsGridProps {
  reports: EmailTriggerReport[];
  title?: string;
  showPagination?: boolean;
  showExport?: boolean;
  onStrategyClick?: (strategy: EmailTriggerReport) => void;
  maxHeight?: number;
}

type SortField = keyof EmailTriggerReport;
type SortDirection = 'asc' | 'desc';

const EmailTriggerReportsGrid: React.FC<EmailTriggerReportsGridProps> = ({
  reports,
  title = "Email Trigger Performance Reports",
  showPagination = true,
  showExport = true,
  onStrategyClick,
  maxHeight = 600,
}) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [sortField, setSortField] = useState<SortField>('totalEmails');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedStrategy, setSelectedStrategy] = useState<EmailTriggerReport | null>(null);

  // Sorting logic
  const sortedReports = React.useMemo(() => {
    return [...reports].sort((a, b) => {
      const aVal = a[sortField];
      const bVal = b[sortField];
      
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortDirection === 'asc' ? aVal - bVal : bVal - aVal;
      }
      
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortDirection === 'asc' 
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      }
      
      return 0;
    });
  }, [reports, sortField, sortDirection]);

  // Pagination
  const paginatedReports = showPagination 
    ? sortedReports.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
    : sortedReports;

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  const handlePageChange = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleRowsPerPageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, strategy: EmailTriggerReport) => {
    setAnchorEl(event.currentTarget);
    setSelectedStrategy(strategy);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedStrategy(null);
  };

  const formatNumber = (num: number): string => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    }
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toLocaleString();
  };

  const formatRate = (rate: number): string => {
    return `${rate.toFixed(1)}%`;
  };

  const formatDate = (dateString: string | null | undefined): string => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getPerformanceColor = (rate: number, type: 'open' | 'click' | 'bounce' | 'delivery'): 'success' | 'warning' | 'error' | 'info' => {
    switch (type) {
      case 'delivery':
        if (rate >= 95) return 'success';
        if (rate >= 90) return 'warning';
        return 'error';
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
        return 'info';
    }
  };

  const getStrategyInitials = (strategyName: string): string => {
    return strategyName
      .split(' ')
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const getPerformanceIcon = (rate: number, type: 'open' | 'click' | 'bounce' | 'delivery') => {
    const color = getPerformanceColor(rate, type);
    const isGood = color === 'success';
    
    return isGood ? (
      <TrendingUp fontSize="small" color={color} />
    ) : (
      <TrendingDown fontSize="small" color={color} />
    );
  };

  const exportToCsv = () => {
    const headers = [
      'Strategy Name',
      'Total Emails',
      'Delivered',
      'Opened',
      'Clicked',
      'Bounced',
      'Complained',
      'Unsubscribed',
      'Delivery Rate (%)',
      'Open Rate (%)',
      'Click Rate (%)',
      'Bounce Rate (%)',
      'First Email Sent',
      'Last Email Sent'
    ];

    const csvData = [
      headers.join(','),
      ...sortedReports.map(report => [
        `"${report.strategyName}"`,
        report.totalEmails,
        report.deliveredCount,
        report.openedCount,
        report.clickedCount,
        report.bouncedCount,
        report.complainedCount,
        report.unsubscribedCount,
        report.deliveryRate.toFixed(2),
        report.openRate.toFixed(2),
        report.clickRate.toFixed(2),
        report.bounceRate.toFixed(2),
        `"${formatDate(report.firstEmailSent)}"`,
        `"${formatDate(report.lastEmailSent)}"`
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvData], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `email-trigger-reports-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  if (!reports || reports.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <EmailIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
        <Typography variant="h6" color="text.secondary">
          No email trigger reports found
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Try adjusting your query or filters
        </Typography>
      </Paper>
    );
  }

  return (
    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
      {/* Header */}
      <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box display="flex" alignItems="center" gap={1}>
            <EmailIcon color="primary" />
            <Typography variant="h6" component="h3">
              {title}
            </Typography>
            <Chip 
              label={`${reports.length} ${reports.length === 1 ? 'Strategy' : 'Strategies'}`}
              size="small"
              variant="outlined"
            />
          </Box>
          {showExport && (
            <Button
              size="small"
              startIcon={<ExportIcon />}
              onClick={exportToCsv}
              variant="outlined"
            >
              Export CSV
            </Button>
          )}
        </Box>
      </Box>

      {/* Table */}
      <TableContainer sx={{ maxHeight }}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 'bold', minWidth: 200 }}>
                <TableSortLabel
                  active={sortField === 'strategyName'}
                  direction={sortField === 'strategyName' ? sortDirection : 'asc'}
                  onClick={() => handleSort('strategyName')}
                >
                  Strategy
                </TableSortLabel>
              </TableCell>
              
              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'totalEmails'}
                  direction={sortField === 'totalEmails' ? sortDirection : 'asc'}
                  onClick={() => handleSort('totalEmails')}
                >
                  <Box display="flex" alignItems="center" gap={0.5}>
                    <EmailIcon fontSize="small" />
                    Total
                  </Box>
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'deliveredCount'}
                  direction={sortField === 'deliveredCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('deliveredCount')}
                >
                  Delivered
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'openedCount'}
                  direction={sortField === 'openedCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('openedCount')}
                >
                  <Box display="flex" alignItems="center" gap={0.5}>
                    <OpenIcon fontSize="small" />
                    Opens
                  </Box>
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'clickedCount'}
                  direction={sortField === 'clickedCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('clickedCount')}
                >
                  <Box display="flex" alignItems="center" gap={0.5}>
                    <ClickIcon fontSize="small" />
                    Clicks
                  </Box>
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'bouncedCount'}
                  direction={sortField === 'bouncedCount' ? sortDirection : 'asc'}
                  onClick={() => handleSort('bouncedCount')}
                >
                  <Box display="flex" alignItems="center" gap={0.5}>
                    <BounceIcon fontSize="small" />
                    Bounces
                  </Box>
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold', minWidth: 150 }}>
                Performance Rates
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                <TableSortLabel
                  active={sortField === 'firstEmailSent'}
                  direction={sortField === 'firstEmailSent' ? sortDirection : 'asc'}
                  onClick={() => handleSort('firstEmailSent')}
                >
                  Date Range
                </TableSortLabel>
              </TableCell>

              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                Actions
              </TableCell>
            </TableRow>
          </TableHead>
          
          <TableBody>
            {paginatedReports.map((report, index) => (
              <TableRow
                key={`${report.strategyName}-${index}`}
                hover
                sx={{ cursor: 'pointer' }}
                onClick={() => onStrategyClick?.(report)}
              >
                {/* Strategy Name */}
                <TableCell>
                  <Box display="flex" alignItems="center" gap={2}>
                    <Avatar
                      sx={{
                        width: 40,
                        height: 40,
                        bgcolor: 'primary.main',
                        fontSize: '0.875rem'
                      }}
                    >
                      {getStrategyInitials(report.strategyName)}
                    </Avatar>
                    <Box>
                      <Typography variant="subtitle2" fontWeight="bold">
                        {report.strategyName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Strategy ID: {report.strategyName.toLowerCase().replace(/\s+/g, '-')}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>

                {/* Total Emails */}
                <TableCell align="center">
                  <Typography variant="h6" fontWeight="bold">
                    {formatNumber(report.totalEmails)}
                  </Typography>
                </TableCell>

                {/* Delivered Count */}
                <TableCell align="center">
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {formatNumber(report.deliveredCount)}
                    </Typography>
                    <Box display="flex" alignItems="center" justifyContent="center" gap={0.5}>
                      {getPerformanceIcon(report.deliveryRate, 'delivery')}
                      <Typography variant="caption" color="text.secondary">
                        {formatRate(report.deliveryRate)}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>

                {/* Opens Count */}
                <TableCell align="center">
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {formatNumber(report.openedCount)}
                    </Typography>
                    <Box display="flex" alignItems="center" justifyContent="center" gap={0.5}>
                      {getPerformanceIcon(report.openRate, 'open')}
                      <Typography variant="caption" color="text.secondary">
                        {formatRate(report.openRate)}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>

                {/* Clicks Count */}
                <TableCell align="center">
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {formatNumber(report.clickedCount)}
                    </Typography>
                    <Box display="flex" alignItems="center" justifyContent="center" gap={0.5}>
                      {getPerformanceIcon(report.clickRate, 'click')}
                      <Typography variant="caption" color="text.secondary">
                        {formatRate(report.clickRate)}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>

                {/* Bounces Count */}
                <TableCell align="center">
                  <Box>
                    <Typography variant="body2" fontWeight="medium">
                      {formatNumber(report.bouncedCount)}
                    </Typography>
                    <Box display="flex" alignItems="center" justifyContent="center" gap={0.5}>
                      {getPerformanceIcon(report.bounceRate, 'bounce')}
                      <Typography variant="caption" color="text.secondary">
                        {formatRate(report.bounceRate)}
                      </Typography>
                    </Box>
                  </Box>
                </TableCell>

                {/* Performance Indicators */}
                <TableCell align="center">
                  <Box display="flex" flexDirection="column" gap={0.5} alignItems="center">
                    <Chip
                      label={`Delivery: ${formatRate(report.deliveryRate)}`}
                      size="small"
                      color={getPerformanceColor(report.deliveryRate, 'delivery')}
                      variant="outlined"
                    />
                    <Chip
                      label={`Open: ${formatRate(report.openRate)}`}
                      size="small"
                      color={getPerformanceColor(report.openRate, 'open')}
                      variant="outlined"
                    />
                    <Chip
                      label={`Click: ${formatRate(report.clickRate)}`}
                      size="small"
                      color={getPerformanceColor(report.clickRate, 'click')}
                      variant="outlined"
                    />
                  </Box>
                </TableCell>

                {/* Date Range */}
                <TableCell align="center">
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      <strong>First:</strong> {formatDate(report.firstEmailSent)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      <strong>Last:</strong> {formatDate(report.lastEmailSent)}
                    </Typography>
                  </Box>
                </TableCell>

                {/* Actions */}
                <TableCell align="center">
                  <IconButton
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleMenuOpen(e, report);
                    }}
                  >
                    <MoreIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      {showPagination && (
        <TablePagination
          rowsPerPageOptions={[5, 10, 25, 50, 100]}
          component="div"
          count={reports.length}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handlePageChange}
          onRowsPerPageChange={handleRowsPerPageChange}
          labelRowsPerPage="Strategies per page:"
        />
      )}

      {/* Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem
          onClick={() => {
            if (selectedStrategy && onStrategyClick) {
              onStrategyClick(selectedStrategy);
            }
            handleMenuClose();
          }}
        >
          <InfoIcon sx={{ mr: 1 }} fontSize="small" />
          View Details
        </MenuItem>
        <MenuItem
          onClick={() => {
            if (selectedStrategy) {
              // Copy strategy name to clipboard
              navigator.clipboard.writeText(selectedStrategy.strategyName);
            }
            handleMenuClose();
          }}
        >
          Copy Strategy Name
        </MenuItem>
        <MenuItem
          onClick={() => {
            if (selectedStrategy) {
              // Create single-strategy CSV
              const headers = ['Strategy Name', 'Total Emails', 'Delivered', 'Opened', 'Clicked', 'Delivery Rate %', 'Open Rate %', 'Click Rate %'];
              const data = [
                headers.join(','),
                [
                  `"${selectedStrategy.strategyName}"`,
                  selectedStrategy.totalEmails,
                  selectedStrategy.deliveredCount,
                  selectedStrategy.openedCount,
                  selectedStrategy.clickedCount,
                  selectedStrategy.deliveryRate.toFixed(2),
                  selectedStrategy.openRate.toFixed(2),
                  selectedStrategy.clickRate.toFixed(2)
                ].join(',')
              ].join('\n');

              const blob = new Blob([data], { type: 'text/csv;charset=utf-8;' });
              const link = document.createElement('a');
              const url = URL.createObjectURL(blob);
              link.setAttribute('href', url);
              link.setAttribute('download', `${selectedStrategy.strategyName.toLowerCase().replace(/\s+/g, '-')}-report.csv`);
              link.style.visibility = 'hidden';
              document.body.appendChild(link);
              link.click();
              document.body.removeChild(link);
            }
            handleMenuClose();
          }}
        >
          Export Strategy Data
        </MenuItem>
      </Menu>
    </Paper>
  );
};

export default EmailTriggerReportsGrid;
