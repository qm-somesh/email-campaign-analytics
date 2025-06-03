import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  TextField,
  Button,
  Typography,
  Alert,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  Divider,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Help as HelpIcon,
  Code as CodeIcon,
  Insights as InsightsIcon,
  Send as SendIcon,
  AutoAwesome as AutoAwesomeIcon,
} from '@mui/icons-material';
import {
  NaturalLanguageQueryRequest,
  NaturalLanguageQueryResponse,
  NaturalLanguageStatus,
  ExampleQuery,
} from '../types';
import apiService from '../services/apiService';

interface NaturalLanguageQueryProps {
  onResultsChange?: (results: any[]) => void;
}

const NaturalLanguageQuery: React.FC<NaturalLanguageQueryProps> = ({
  onResultsChange,
}) => {
  const [query, setQuery] = useState('');
  const [response, setResponse] = useState<NaturalLanguageQueryResponse | null>(null);
  const [status, setStatus] = useState<NaturalLanguageStatus | null>(null);
  const [examples, setExamples] = useState<ExampleQuery[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showExamples, setShowExamples] = useState(false);
  const [includeDebug, setIncludeDebug] = useState(false);

  // Load initial data
  useEffect(() => {
    loadStatus();
    loadExamples();
  }, []);

  const loadStatus = async () => {
    try {
      const statusData = await apiService.naturalLanguageApi.getStatus();
      setStatus(statusData);
    } catch (err) {
      console.error('Failed to load LLM status:', err);
    }
  };
  const loadExamples = async () => {
    try {
      const exampleData = await apiService.naturalLanguageApi.getExamples();
      
      // Transform flat object structure to array structure
      const transformedExamples = Object.entries(exampleData).map(([category, examples]) => ({
        category: category.charAt(0).toUpperCase() + category.slice(1),
        examples: Array.isArray(examples) ? examples : []
      }));
      
      setExamples(transformedExamples);
    } catch (err) {
      console.error('Failed to load examples:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;

    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const request: NaturalLanguageQueryRequest = {
        query: query.trim(),
        includeDebugInfo: includeDebug,
      };

      const result = await apiService.naturalLanguageApi.processQuery(request);
      setResponse(result);

      if (result.success && onResultsChange) {
        onResultsChange(result.results);
      }
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const handleExampleClick = (exampleQuery: string) => {
    setQuery(exampleQuery);
    setShowExamples(false);
  };

  const renderResults = () => {
    if (!response?.results || response.results.length === 0) {
      return (
        <Alert severity="info">
          No results found for your query.
        </Alert>
      );
    }

    // Get the keys from the first result to create table headers
    const firstResult = response.results[0];
    const keys = Object.keys(firstResult);

    return (
      <TableContainer component={Paper} sx={{ mt: 2 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              {keys.map((key) => (
                <TableCell key={key}>
                  <Typography variant="subtitle2" fontWeight="bold">
                    {key.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
                  </Typography>
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {response.results.map((result, index) => (
              <TableRow key={index}>
                {keys.map((key) => (
                  <TableCell key={key}>
                    {typeof result[key] === 'number' 
                      ? result[key].toLocaleString() 
                      : String(result[key] || '')}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  };

  return (
    <Box>
      <Card>
        <CardContent>
          {/* Header */}
          <Box display="flex" alignItems="center" gap={1} mb={2}>
            <AutoAwesomeIcon color="primary" />
            <Typography variant="h6">
              Natural Language Query
            </Typography>
            <Tooltip title="Ask questions in plain English">
              <IconButton size="small" onClick={() => setShowExamples(true)}>
                <HelpIcon />
              </IconButton>
            </Tooltip>
            {status && (
              <Chip
                label={status.isAvailable ? 'AI Ready' : 'Mock Mode'}
                color={status.isAvailable ? 'success' : 'warning'}
                size="small"
                variant="outlined"
              />
            )}
          </Box>

          {/* Query Form */}
          <Box component="form" onSubmit={handleSubmit}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              placeholder="Ask me anything... e.g., 'Show me campaigns from last month with open rates above 25%'"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              disabled={loading}
              sx={{ mb: 2 }}
            />
            
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Box display="flex" gap={1}>
                <Button
                  type="submit"
                  variant="contained"
                  disabled={loading || !query.trim()}
                  startIcon={loading ? <CircularProgress size={20} /> : <SendIcon />}
                >
                  {loading ? 'Processing...' : 'Ask AI'}
                </Button>
                
                <Button
                  variant="outlined"
                  onClick={() => setShowExamples(true)}
                  startIcon={<HelpIcon />}
                >
                  Examples
                </Button>
              </Box>

              <Box display="flex" alignItems="center" gap={1}>
                <Typography variant="caption" color="text.secondary">
                  Debug Info
                </Typography>
                <input
                  type="checkbox"
                  checked={includeDebug}
                  onChange={(e) => setIncludeDebug(e.target.checked)}
                />
              </Box>
            </Box>
          </Box>

          {/* Error Display */}
          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}

          {/* Response Display */}
          {response && (
            <Box mt={3}>
              <Typography variant="h6" gutterBottom>
                Results
              </Typography>
              
              {/* Success/Error Status */}
              <Alert 
                severity={response.success ? 'success' : 'error'} 
                sx={{ mb: 2 }}
              >
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Typography>
                    {response.success 
                      ? `Found ${response.results?.length || 0} results` 
                      : response.error}
                  </Typography>
                  <Chip 
                    label={`${response.processingTimeMs}ms`} 
                    size="small" 
                    variant="outlined"
                  />
                </Box>
              </Alert>

              {/* Query Intent */}
              {response.intent && (
                <Box mb={2}>
                  <Chip 
                    label={`Intent: ${response.intent}`} 
                    color="primary" 
                    variant="outlined"
                    icon={<InsightsIcon />}
                  />
                </Box>
              )}

              {/* Results Table */}
              {response.success && renderResults()}

              {/* Debug Information */}
              {response.debugInfo && includeDebug && (
                <Accordion sx={{ mt: 2 }}>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Box display="flex" alignItems="center" gap={1}>
                      <CodeIcon />
                      <Typography>Debug Information</Typography>
                    </Box>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Box>
                      <Typography variant="subtitle2" gutterBottom>
                        Generated SQL:
                      </Typography>
                      <Paper sx={{ p: 2, bgcolor: 'grey.100', mb: 2 }}>
                        <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
                          {response.generatedSql}
                        </Typography>
                      </Paper>
                      
                      <Typography variant="subtitle2" gutterBottom>
                        Performance Metrics:
                      </Typography>
                      <Box display="flex" gap={2} flexWrap="wrap">
                        <Chip label={`LLM: ${response.debugInfo.llmProcessingTimeMs}ms`} size="small" />
                        <Chip label={`SQL: ${response.debugInfo.sqlExecutionTimeMs}ms`} size="small" />
                        <Chip label={`Tokens: ${response.debugInfo.tokensUsed}`} size="small" />
                        <Chip label={`Confidence: ${(response.debugInfo.confidenceScore * 100).toFixed(1)}%`} size="small" />
                      </Box>
                      
                      {response.debugInfo.warnings.length > 0 && (
                        <Alert severity="warning" sx={{ mt: 2 }}>
                          <Typography variant="subtitle2">Warnings:</Typography>
                          <ul>
                            {response.debugInfo.warnings.map((warning, index) => (
                              <li key={index}>{warning}</li>
                            ))}
                          </ul>
                        </Alert>
                      )}
                    </Box>
                  </AccordionDetails>
                </Accordion>
              )}
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Examples Dialog */}
      <Dialog 
        open={showExamples} 
        onClose={() => setShowExamples(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          <Box display="flex" alignItems="center" gap={1}>
            <AutoAwesomeIcon />
            Example Queries
          </Box>
        </DialogTitle>
        <DialogContent>
          {examples.map((category, index) => (
            <Box key={index} mb={3}>
              <Typography variant="h6" gutterBottom color="primary">
                {category.category}
              </Typography>
              <List dense>
                {category.examples.map((example, exampleIndex) => (                  <ListItem
                    key={exampleIndex}
                    onClick={() => handleExampleClick(example)}
                    sx={{ 
                      border: '1px solid', 
                      borderColor: 'grey.300',
                      borderRadius: 1,
                      mb: 1,
                      cursor: 'pointer',
                      '&:hover': { bgcolor: 'primary.50' }
                    }}
                  >
                    <ListItemText 
                      primary={example}
                      primaryTypographyProps={{ variant: 'body2' }}
                    />
                  </ListItem>
                ))}
              </List>
              {index < examples.length - 1 && <Divider sx={{ mt: 2 }} />}
            </Box>
          ))}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowExamples(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default NaturalLanguageQuery;
