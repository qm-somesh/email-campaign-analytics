import React from 'react';
import {
  Box,
  Container,
  Typography,
} from '@mui/material';
import EmailTriggerComponent from '../components/EmailTriggerComponent';

const Dashboard: React.FC = () => {
  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Email Campaign Dashboard
      </Typography>

      {/* Email Trigger Component */}
      <Box sx={{ mb: 4 }}>
        <EmailTriggerComponent />
      </Box>
    </Container>
  );
};

export default Dashboard;
