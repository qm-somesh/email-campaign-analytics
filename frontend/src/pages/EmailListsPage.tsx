import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Typography,
  Paper,
  Card,
  CardContent,
  CircularProgress,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  Divider,
} from '@mui/material';
import { Email, People, TrendingUp } from '@mui/icons-material';
import { emailListApi } from '../services/apiService';
import { EmailList } from '../types';
import { formatNumber, formatDate } from '../utils';

const EmailListsPage: React.FC = () => {
  const [emailLists, setEmailLists] = useState<EmailList[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchEmailLists = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await emailListApi.getEmailLists();
        setEmailLists(data);
      } catch (err: any) {
        console.error('Error fetching email lists:', err);
        setError('Failed to load email lists. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchEmailLists();
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
  const totalSubscribers = emailLists.reduce((sum, list) => sum + list.totalRecipients, 0);
  const totalActive = emailLists.reduce((sum, list) => sum + list.activeRecipients, 0);

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        Email Lists
      </Typography>      {/* Summary Cards */}
      <Box 
        display="flex" 
        flexDirection={{ xs: 'column', sm: 'row' }} 
        gap={3} 
        sx={{ mb: 4 }}
      >
        <Box flex={1}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <Email color="primary" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{emailLists.length}</Typography>
                  <Typography color="textSecondary">Total Lists</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
        <Box flex={1}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <People color="success" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(totalSubscribers)}</Typography>
                  <Typography color="textSecondary">Total Subscribers</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
        <Box flex={1}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center">
                <TrendingUp color="info" sx={{ mr: 2, fontSize: 32 }} />
                <Box>
                  <Typography variant="h4">{formatNumber(totalActive)}</Typography>
                  <Typography color="textSecondary">Active Subscribers</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Email Lists */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          All Email Lists
        </Typography>
        <List>
          {emailLists.map((list, index) => (
            <React.Fragment key={list.listId}>
              <ListItem>
                <ListItemIcon>
                  <Email color="primary" />
                </ListItemIcon>
                <ListItemText
                  primary={
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Typography variant="h6">{list.name}</Typography>
                      <Box display="flex" gap={1}>
                        <Chip
                          label={`${formatNumber(list.totalRecipients)} subscribers`}
                          color="primary"
                          size="small"
                        />
                        <Chip
                          label={`${formatNumber(list.activeRecipients)} active`}
                          color="success"
                          size="small"
                        />
                      </Box>
                    </Box>
                  }
                  secondary={
                    <Box>
                      {list.description && (
                        <Typography variant="body2" color="textSecondary" sx={{ mb: 1 }}>
                          {list.description}
                        </Typography>
                      )}
                      <Typography variant="caption" color="textSecondary">
                        Created: {formatDate(list.createdAt)} • Last Updated: {formatDate(list.updatedAt)}
                      </Typography>                      {list.tags && list.tags.length > 0 && (
                        <Box mt={1} display="flex" gap={0.5} flexWrap="wrap">
                          {list.tags.map((tag: string) => (
                            <Chip
                              key={tag}
                              label={tag}
                              size="small"
                              variant="outlined"
                            />
                          ))}
                        </Box>
                      )}
                    </Box>
                  }
                />
              </ListItem>
              {index < emailLists.length - 1 && <Divider />}
            </React.Fragment>
          ))}
        </List>
      </Paper>
    </Container>
  );
};

export default EmailListsPage;
