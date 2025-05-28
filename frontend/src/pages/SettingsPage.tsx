import React from 'react';
import {
  Container,
  Typography,
  Paper,
  Box,
  Alert,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Settings as SettingsIcon,
  Api,
  Security,
  Notifications,
  Storage,
  Email,
} from '@mui/icons-material';

const SettingsPage: React.FC = () => {
  const settingsItems = [
    {
      icon: <Api />,
      title: 'API Configuration',
      description: 'Configure BigQuery connections and API endpoints',
    },
    {
      icon: <Email />,
      title: 'Email Service Settings',
      description: 'Mailgun configuration and email service parameters',
    },
    {
      icon: <Security />,
      title: 'Security & Authentication',
      description: 'User permissions and authentication settings',
    },
    {
      icon: <Notifications />,
      title: 'Notifications',
      description: 'Alert preferences and notification settings',
    },
    {
      icon: <Storage />,
      title: 'Data Management',
      description: 'Data retention, backup, and export settings',
    },
  ];

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Settings
      </Typography>
      
      <Paper sx={{ p: 4 }}>
        <Box display="flex" alignItems="center" mb={3}>
          <SettingsIcon sx={{ fontSize: 40, color: 'primary.main', mr: 2 }} />
          <Box>
            <Typography variant="h5" gutterBottom>
              Application Settings
            </Typography>
            <Typography variant="body1" color="textSecondary">
              Configure your email campaign reporting application preferences and integrations.
            </Typography>
          </Box>
        </Box>
        
        <Alert severity="info" sx={{ mb: 3 }}>
          Settings functionality is under development. Current configuration is managed through 
          environment variables and application configuration files.
        </Alert>
        
        <Typography variant="h6" gutterBottom sx={{ mt: 3 }}>
          Available Settings Categories
        </Typography>
        
        <List>
          {settingsItems.map((item, index) => (
            <ListItem
              key={index}
              sx={{
                border: '1px solid',
                borderColor: 'grey.300',
                borderRadius: 1,
                mb: 1,
                '&:hover': {
                  backgroundColor: 'grey.50',
                },
              }}
            >
              <ListItemIcon sx={{ color: 'primary.main' }}>
                {item.icon}
              </ListItemIcon>
              <ListItemText
                primary={
                  <Typography variant="subtitle1" fontWeight="bold">
                    {item.title}
                  </Typography>
                }
                secondary={item.description}
              />
            </ListItem>
          ))}
        </List>
        
        <Alert severity="warning" sx={{ mt: 3 }}>
          Current Configuration:
          <ul style={{ margin: '8px 0' }}>
            <li>API URL: {process.env.REACT_APP_API_URL || 'http://localhost:5037/api'}</li>
            <li>Environment: {process.env.NODE_ENV}</li>
            <li>Build Version: {process.env.REACT_APP_VERSION || 'Development'}</li>
          </ul>
        </Alert>
      </Paper>
    </Container>
  );
};

export default SettingsPage;
