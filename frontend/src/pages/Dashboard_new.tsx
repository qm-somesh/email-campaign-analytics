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
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
} from 'recharts';
import { dashboardApi } from '../services/apiService';
import {
  DashboardMetrics,
  RecentCampaign,
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
  const [recentCampaigns, setRecentCampaigns] = useState<RecentCampaign[]>([]);
  const [recentEvents, setRecentEvents] = useState<RecentEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [metricsData, campaignsData, eventsData] = await Promise.all([
          dashboardApi.getMetrics(),
          dashboardApi.getRecentCampaigns(5),
          dashboardApi.getRecentEvents(10),
        ]);

        setMetrics(metricsData);
        setRecentCampaigns(campaignsData);
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

  const campaignStatusData = recentCampaigns.reduce((acc, campaign) => {
    const status = campaign.status;
    acc[status] = (acc[status] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  const pieChartData = Object.entries(campaignStatusData).map(([status, count]) => ({
    name: status,
    value: count,
  }));

  const chartColors = generateChartColors(pieChartData.length);

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Email Campaign Dashboard
      </Typography>

      {/* Key Metrics */}
      <Box display="flex" flexWrap="wrap" gap={3} sx={{ mb: 4 }}>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Total Campaigns"
            value={metrics.totalCampaigns}
            icon={<Campaign />}
            color="#1976d2"
            subtitle={`${metrics.activeCampaigns} active`}
          />
        </Box>
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
      </Box>

      {/* Secondary Metrics */}
      <Box display="flex" flexWrap="wrap" gap={3} sx={{ mb: 4 }}>
        <Box sx={{ flex: '1 1 300px', minWidth: '250px' }}>
          <MetricCard
            title="Recipients"
            value={metrics.totalRecipients}
            icon={<People />}
            color="#0288d1"
            subtitle={`${metrics.activeRecipients} active`}
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
        </Box>

        {/* Campaign Status Distribution */}
        <Box sx={{ flex: '1 1 400px', minWidth: '350px' }}>
          <Paper sx={{ p: 3, height: 400 }}>
            <Typography variant="h6" gutterBottom>
              Campaign Status
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieChartData}
                  cx="50%"
                  cy="50%"
                  outerRadius={80}
                  dataKey="value"
                  label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                >
                  {pieChartData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={chartColors[index]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </Paper>
        </Box>

        {/* Recent Campaigns */}
        <Box sx={{ flex: '1 1 400px', minWidth: '350px' }}>
          <Paper sx={{ p: 3, height: 400, overflow: 'hidden' }}>
            <Typography variant="h6" gutterBottom>
              Recent Campaigns
            </Typography>
            <List sx={{ maxHeight: 320, overflow: 'auto' }}>              {recentCampaigns.map((campaign, index) => (
                <React.Fragment key={campaign.campaignId}>
                  <ListItem>
                    <ListItemIcon>
                      <Campaign color="primary" />
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                          <Typography variant="subtitle1" noWrap>
                            {campaign.name}
                          </Typography>
                          <Chip
                            label={campaign.status}
                            color={getStatusColor(campaign.status)}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Box>
                          <Typography variant="body2" color="textSecondary">
                            {formatNumber(campaign.totalRecipients)} recipients • {formatPercentage(campaign.deliveredCount > 0 ? (campaign.openedCount / campaign.deliveredCount * 100) : 0)} open • {formatPercentage(campaign.deliveredCount > 0 ? (campaign.clickedCount / campaign.deliveredCount * 100) : 0)} click
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Campaign ID: {campaign.campaignId}
                          </Typography>
                        </Box>
                      }
                    />
                  </ListItem>
                  {index < recentCampaigns.length - 1 && <Divider />}
                </React.Fragment>
              ))}
            </List>
          </Paper>
        </Box>

        {/* Recent Events */}
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
