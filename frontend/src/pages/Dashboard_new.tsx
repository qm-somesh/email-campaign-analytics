import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Paper,
  Typography,
  Card,
  CardContent,
  CircularProgress,
  Alert,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
} from '@mui/material';
import {
  TrendingUp,
  Email,
  OpenInNew,
  TouchApp,
  Campaign,
  People,
  Schedule,
  CheckCircle,
  Error,
  Warning,
} from '@mui/icons-material';
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { dashboardApi } from '../services/apiService';
import {
  DashboardMetrics,
  RecentEvent,
} from '../types';
import {
  formatNumber,
  formatPercentage,
  formatDateTimeShort,
  getStatusColor,
  getEventTypeColor,
  generateChartColors,
} from '../utils';

interface MetricCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color: string;
  subtitle?: string;
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  icon,
  color,
  subtitle,
  trend,
}) => (
  <Card sx={{ height: '100%' }}>
    <CardContent>
      <Box display="flex" alignItems="center" justifyContent="space-between">
        <Box>
          <Typography color="textSecondary" gutterBottom variant="h6">
            {title}
          </Typography>
          <Typography variant="h4" component="h2" sx={{ color, fontWeight: 'bold' }}>
            {typeof value === 'number' ? formatNumber(value) : value}
          </Typography>
          {subtitle && (
            <Typography color="textSecondary" variant="body2">
              {subtitle}
            </Typography>
          )}
          {trend && (
            <Box display="flex" alignItems="center" mt={1}>
              <TrendingUp
                sx={{
                  color: trend.isPositive ? 'success.main' : 'error.main',
                  mr: 0.5,
                  fontSize: 16,
                }}
              />
              <Typography
                variant="body2"
                sx={{
                  color: trend.isPositive ? 'success.main' : 'error.main',
                }}
              >
                {trend.isPositive ? '+' : ''}{trend.value}%
              </Typography>
            </Box>
          )}
        </Box>
        <Box
          sx={{
            p: 2,
            borderRadius: '50%',
            backgroundColor: `${color}20`,
            color,
          }}
        >
          {icon}
        </Box>
      </Box>
    </CardContent>
  </Card>
);

const Dashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [recentEvents, setRecentEvents] = useState<RecentEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [metricsData, eventsData] = await Promise.all([
          dashboardApi.getMetrics(),
          dashboardApi.getRecentEvents(10),
        ]);

        setMetrics(metricsData);
        setRecentEvents(eventsData);
      } catch (err: any) {
        console.error('Error fetching dashboard data:', err);
        setError('Failed to load dashboard data. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (loading) {
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

  if (error) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      </Container>
    );
  }

  if (!metrics) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="warning">No dashboard data available.</Alert>
      </Container>
    );
  }
  // Prepare chart data
  const performanceData = [
    { name: 'Delivery Rate', value: metrics.overallDeliveryRate, color: '#4caf50' },
    { name: 'Open Rate', value: metrics.overallOpenRate, color: '#2196f3' },
    { name: 'Click Rate', value: metrics.overallClickRate, color: '#ff9800' },
    { name: 'Bounce Rate', value: metrics.overallBounceRate, color: '#f44336' },
  ];

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Email Campaign Dashboard
      </Typography>      {/* Key Metrics */}
      <Box display="flex" flexWrap="wrap" gap={3} sx={{ mb: 4 }}>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Emails Sent"
            value={metrics.totalEmailsSent}
            icon={<Email />}
            color="#388e3c"
            subtitle="Total delivered"
          />
        </Box>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Open Rate"
            value={formatPercentage(metrics.overallOpenRate)}
            icon={<OpenInNew />}
            color="#f57c00"
            subtitle="Overall average"
          />
        </Box>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Click Rate"
            value={formatPercentage(metrics.overallClickRate)}
            icon={<TouchApp />}
            color="#7b1fa2"
            subtitle="Overall average"
          />
        </Box>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Delivery Rate"
            value={formatPercentage(metrics.overallDeliveryRate)}
            icon={<CheckCircle />}
            color="#4caf50"
            subtitle="Success rate"
          />
        </Box>
      </Box>

      {/* Secondary Metrics */}
      <Box display="flex" flexWrap="wrap" gap={3} sx={{ mb: 4 }}>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Bounce Rate"
            value={formatPercentage(metrics.overallBounceRate)}
            icon={<Error />}
            color="#f44336"
            subtitle="Failed deliveries"
          />
        </Box>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Recent Events"
            value={metrics.recentEventsCount}
            icon={<Schedule />}
            color="#795548"
            subtitle="Last 24 hours"
          />
        </Box>
      </Box>

      {/* Charts and Lists */}
      <Box display="flex" flexWrap="wrap" gap={3}>
        {/* Performance Chart */}
        <Box sx={{ flex: '2 1 600px', minWidth: '500px' }}>
          <Paper sx={{ p: 3, height: 400 }}>
            <Typography variant="h6" gutterBottom>
              Performance Overview
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={performanceData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip formatter={(value) => `${Number(value).toFixed(1)}%`} />
                <Bar dataKey="value" fill="#8884d8">
                  {performanceData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Box>        {/* Recent Events */}
        <Box sx={{ flex: '1 1 400px', minWidth: '350px' }}>
          <Paper sx={{ p: 3, height: 400, overflow: 'hidden' }}>
            <Typography variant="h6" gutterBottom>
              Recent Events
            </Typography>
            <List sx={{ maxHeight: 320, overflow: 'auto' }}>
              {recentEvents.map((event, index) => (
                <React.Fragment key={event.id}>
                  <ListItem>
                    <ListItemIcon>
                      {event.eventType === 'delivered' && <CheckCircle color="success" />}
                      {event.eventType === 'opened' && <OpenInNew color="info" />}
                      {event.eventType === 'clicked' && <TouchApp color="primary" />}
                      {event.eventType === 'bounced' && <Error color="error" />}
                      {event.eventType === 'failed' && <Error color="error" />}
                      {!['delivered', 'opened', 'clicked', 'bounced', 'failed'].includes(event.eventType) && (
                        <Warning color="warning" />
                      )}
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                          <Typography variant="subtitle2" noWrap>
                            {event.eventType.charAt(0).toUpperCase() + event.eventType.slice(1)}
                          </Typography>
                          <Chip
                            label={event.status}
                            color={getEventTypeColor(event.eventType)}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="textSecondary" noWrap>
                            {event.recipient} • {event.campaignName}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {formatDateTimeShort(event.timestamp)}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < recentEvents.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </Paper>
        </Box>
      </Box>
    </Container>
  );
};

export default Dashboard;
