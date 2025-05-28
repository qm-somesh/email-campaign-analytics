import React from 'react';
import {
  Container,
  Typography,
  Paper,
  Box,
  Alert,
} from '@mui/material';
import { BarChart } from '@mui/icons-material';

const AnalyticsPage: React.FC = () => {
  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Analytics
      </Typography>
      
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <BarChart sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
        <Typography variant="h5" gutterBottom>
          Advanced Analytics Coming Soon
        </Typography>
        <Typography variant="body1" color="textSecondary" sx={{ mb: 3 }}>
          This page will contain detailed analytics, charts, and insights about your email campaigns.
        </Typography>
        
        <Alert severity="info">
          Features planned for this page:
          <ul style={{ textAlign: 'left', margin: '16px 0' }}>
            <li>Campaign performance trends over time</li>
            <li>Audience engagement analysis</li>
            <li>A/B testing results</li>
            <li>Deliverability insights</li>
            <li>Revenue attribution</li>
            <li>Cohort analysis</li>
          </ul>
        </Alert>
      </Paper>
    </Container>
  );
};

export default AnalyticsPage;
