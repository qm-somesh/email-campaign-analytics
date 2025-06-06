import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Paper,
  Button,
  TextField,
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stepper,
  Step,
  StepLabel,
  StepContent,
  Chip,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  PlayArrow as PlayIcon,
  Send as SendIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
  ExpandMore as ExpandMoreIcon,
  Code as CodeIcon,
  People as PeopleIcon,
  Schedule as ScheduleIcon,
} from '@mui/icons-material';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import apiService from '../services/apiService';
import {
  EmailTriggerRequest,
  EmailTriggerResponse,
  EmailTriggerNaturalLanguageResponse,
} from '../types';

interface EmailTriggerCampaignProps {
  onTriggerSuccess?: (response: EmailTriggerResponse) => void;
  onClose?: () => void;
}

const EmailTriggerCampaign: React.FC<EmailTriggerCampaignProps> = ({
  onTriggerSuccess,
  onClose,
}) => {
  // State management
  const [activeStep, setActiveStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  
  // Trigger request state
  const [naturalLanguageCommand, setNaturalLanguageCommand] = useState('');
  const [triggerResponse, setTriggerResponse] = useState<EmailTriggerResponse | null>(null);
  const [nlResponse, setNlResponse] = useState<EmailTriggerNaturalLanguageResponse | null>(null);
  
  // Advanced options state
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [context, setContext] = useState('');
  const [parameters, setParameters] = useState<Record<string, any>>({});
  const [scheduledTime, setScheduledTime] = useState<Date | null>(null);
  
  // Available strategies
  const [strategyNames, setStrategyNames] = useState<string[]>([]);
  const [selectedStrategy, setSelectedStrategy] = useState('');

  // Load strategy names on mount
  useEffect(() => {
    loadStrategyNames();
  }, []);

  const loadStrategyNames = async () => {
    try {
      const names = await apiService.emailTriggerApi.getStrategyNames();
      setStrategyNames(names);
    } catch (err: any) {
      console.error('Failed to load strategy names:', err);
    }
  };

  const handleNext = () => {
    setActiveStep((prevStep) => prevStep + 1);
  };

  const handleBack = () => {
    setActiveStep((prevStep) => prevStep - 1);
  };

  const handleReset = () => {
    setActiveStep(0);
    setNaturalLanguageCommand('');
    setTriggerResponse(null);
    setNlResponse(null);
    setError(null);
    setSuccess(false);
    setContext('');
    setParameters({});
    setScheduledTime(null);
    setSelectedStrategy('');
  };

  const validateStep = (step: number): boolean => {
    switch (step) {
      case 0:
        return naturalLanguageCommand.trim().length > 0 || selectedStrategy.length > 0;
      case 1:
        return true; // Review step is always valid
      default:
        return true;
    }
  };

  const processNaturalLanguageCommand = async () => {
    if (!naturalLanguageCommand.trim()) {
      setError('Please enter a command or select a strategy.');
      return;
    }

    setLoading(true);
    setError(null);
    
    try {
      const response = await apiService.emailTriggerApi.processNaturalLanguageQuery(
        naturalLanguageCommand,
        true
      );
      setNlResponse(response);
      
      if (response.success) {
        handleNext();
      } else {
        setError(response.error || 'Failed to process command');
      }
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const triggerCampaign = async () => {
    setLoading(true);
    setError(null);
    
    try {
      const request: EmailTriggerRequest = {
        command: naturalLanguageCommand || `Trigger ${selectedStrategy} campaign`,
        context,
        parameters: {
          ...parameters,
          ...(scheduledTime && { scheduledTime: scheduledTime.toISOString() }),
          ...(selectedStrategy && { strategyName: selectedStrategy }),
        },
      };

      const response = await apiService.emailTriggerApi.triggerCampaignWithNaturalLanguage(request);
      
      if (response.success) {        setSuccess(true);
        setNlResponse(response);
        handleNext();
        if (onTriggerSuccess && response.triggerReports?.[0]) {
          // Convert to EmailTriggerResponse format
          const triggerResp: EmailTriggerResponse = {
            success: true,
            strategyName: response.triggerReports[0].strategyName,
            recipientsCount: response.triggerReports[0].totalEmails,
            recipientCount: response.triggerReports[0].totalEmails,
            message: response.explanation || 'Campaign triggered successfully',
            triggeredAt: new Date().toISOString(),
            metadata: response.parameters,
          };
          onTriggerSuccess(triggerResp);
        }
      } else {
        setError(response.error || 'Failed to trigger campaign');
      }
    } catch (err: any) {
      setError(apiService.formatApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const addParameter = (key: string, value: any) => {
    setParameters(prev => ({
      ...prev,
      [key]: value,
    }));
  };

  const removeParameter = (key: string) => {
    setParameters(prev => {
      const newParams = { ...prev };
      delete newParams[key];
      return newParams;
    });
  };

  const steps = [
    {
      label: 'Define Campaign Trigger',
      description: 'Describe what you want to trigger or select a strategy',
    },
    {
      label: 'Review & Configure',
      description: 'Review the trigger details and add any additional options',
    },
    {
      label: 'Execute',
      description: 'Trigger the email campaign',
    },
    {
      label: 'Results',
      description: 'View the trigger results',
    },
  ];

  const getStepContent = (step: number) => {
    switch (step) {
      case 0:
        return (
          <Box>
            <Typography variant="h6" gutterBottom>
              How would you like to trigger the campaign?
            </Typography>
            
            <Box mb={3}>
              <Typography variant="subtitle2" gutterBottom>
                Option 1: Natural Language Command
              </Typography>
              <TextField
                fullWidth
                multiline
                rows={3}
                variant="outlined"
                placeholder="Describe what you want to trigger, e.g., 'Send welcome email to new customers' or 'Trigger promotional campaign for high-value customers'"
                value={naturalLanguageCommand}
                onChange={(e) => setNaturalLanguageCommand(e.target.value)}
                helperText="Use natural language to describe your email campaign trigger"
              />
            </Box>

            <Typography variant="body2" color="text.secondary" align="center" mb={2}>
              OR
            </Typography>

            <Box mb={3}>
              <Typography variant="subtitle2" gutterBottom>
                Option 2: Select Existing Strategy
              </Typography>
              <FormControl fullWidth>
                <InputLabel>Strategy</InputLabel>
                <Select
                  value={selectedStrategy}
                  label="Strategy"
                  onChange={(e) => setSelectedStrategy(e.target.value)}
                >
                  <MenuItem value="">Select a strategy...</MenuItem>
                  {strategyNames.map((name) => (
                    <MenuItem key={name} value={name}>
                      {name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>            <Accordion expanded={showAdvanced} onChange={() => setShowAdvanced(!showAdvanced)}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography>Advanced Options</Typography>
              </AccordionSummary>
              <AccordionDetails>
                <Box display="flex" flexDirection="column" gap={2}>
                  <Box>
                    <TextField
                      fullWidth
                      label="Context"
                      multiline
                      rows={2}
                      value={context}
                      onChange={(e) => setContext(e.target.value)}
                      helperText="Additional context for the trigger"
                    />
                  </Box>
                  <Box>
                    <LocalizationProvider dateAdapter={AdapterDateFns}>
                      <DateTimePicker
                        label="Schedule for later"
                        value={scheduledTime}
                        onChange={(date) => setScheduledTime(date)}
                        slotProps={{ textField: { fullWidth: true } }}
                      />
                    </LocalizationProvider>
                  </Box>
                </Box>
              </AccordionDetails>
            </Accordion>

            <Box mt={3} display="flex" justifyContent="space-between">
              <Button onClick={onClose} disabled={loading}>
                Cancel
              </Button>
              <Button
                variant="contained"
                onClick={processNaturalLanguageCommand}
                disabled={!validateStep(0) || loading}
                startIcon={loading ? <CircularProgress size={20} /> : <PlayIcon />}
              >
                {loading ? 'Processing...' : 'Process Command'}
              </Button>
            </Box>
          </Box>
        );

      case 1:
        return (
          <Box>
            <Typography variant="h6" gutterBottom>
              Review Trigger Details
            </Typography>
            
            {nlResponse && (
              <Card sx={{ mb: 2 }}>
                <CardContent>                  <Typography variant="subtitle2" gutterBottom>
                    Processed Command
                  </Typography>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    {nlResponse.explanation || nlResponse.error}
                  </Typography>
                  {nlResponse.intent && (
                    <Typography variant="body2" color="primary" gutterBottom>
                      Intent: {nlResponse.intent}
                    </Typography>
                  )}
                  
                  {nlResponse.parameters && Object.keys(nlResponse.parameters).length > 0 && (
                    <Box mt={2}>
                      <Typography variant="subtitle2" gutterBottom>
                        Extracted Parameters
                      </Typography>
                      <Box display="flex" gap={1} flexWrap="wrap">
                        {Object.entries(nlResponse.parameters).map(([key, value]) => (
                          <Chip
                            key={key}
                            label={`${key}: ${value}`}
                            size="small"
                            variant="outlined"
                          />
                        ))}
                      </Box>
                    </Box>
                  )}
                </CardContent>
              </Card>
            )}

            <Card sx={{ mb: 2 }}>
              <CardContent>                <Typography variant="subtitle2" gutterBottom>
                  Trigger Configuration
                </Typography>
                <Box display="flex" flexDirection="column" gap={1}>
                  <Typography variant="body2">
                    <strong>Command:</strong> {naturalLanguageCommand || `Trigger ${selectedStrategy} campaign`}
                  </Typography>
                  {context && (
                    <Typography variant="body2">
                      <strong>Context:</strong> {context}
                    </Typography>
                  )}
                  {scheduledTime && (
                    <Typography variant="body2">
                      <strong>Scheduled:</strong> {scheduledTime.toLocaleString()}
                    </Typography>
                  )}
                  {Object.keys(parameters).length > 0 && (
                    <Box>
                      <Typography variant="body2">
                        <strong>Parameters:</strong>
                      </Typography>
                      <Box ml={2}>
                        {Object.entries(parameters).map(([key, value]) => (
                          <Typography key={key} variant="body2" color="text.secondary">
                            • {key}: {JSON.stringify(value)}
                          </Typography>
                        ))}
                      </Box>
                    </Box>
                  )}
                </Box>
              </CardContent>
            </Card>

            <Box display="flex" justifyContent="space-between">
              <Button onClick={handleBack} disabled={loading}>
                Back
              </Button>
              <Button
                variant="contained"
                onClick={triggerCampaign}
                disabled={loading}
                startIcon={loading ? <CircularProgress size={20} /> : <SendIcon />}
                color="primary"
              >
                {loading ? 'Triggering...' : 'Trigger Campaign'}
              </Button>
            </Box>
          </Box>
        );

      case 2:
        return (
          <Box textAlign="center">
            <CircularProgress size={60} sx={{ mb: 2 }} />
            <Typography variant="h6" gutterBottom>
              Triggering Email Campaign...
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Please wait while we process your request
            </Typography>
          </Box>
        );

      case 3:
        return (
          <Box>
            <Box textAlign="center" mb={3}>
              {success ? (
                <CheckIcon color="success" sx={{ fontSize: 60, mb: 1 }} />
              ) : (
                <ErrorIcon color="error" sx={{ fontSize: 60, mb: 1 }} />
              )}
              <Typography variant="h6" gutterBottom>
                {success ? 'Campaign Triggered Successfully!' : 'Campaign Trigger Failed'}
              </Typography>
            </Box>

            {nlResponse && (
              <Card>
                <CardContent>                  <Typography variant="subtitle2" gutterBottom>
                    Result Details
                  </Typography>
                  <Typography variant="body2" gutterBottom>
                    {nlResponse.explanation || nlResponse.error}
                  </Typography>
                  {nlResponse.intent && (
                    <Typography variant="body2" color="primary" gutterBottom>
                      Intent: {nlResponse.intent}
                    </Typography>
                  )}
                  
                  {nlResponse.triggerReports && nlResponse.triggerReports.length > 0 && (
                    <Box mt={2}>
                      <Typography variant="subtitle2" gutterBottom>
                        Campaign Statistics
                      </Typography>
                      {nlResponse.triggerReports.map((report, index) => (
                        <Box key={index} mb={1}>                          <Box display="flex" flexWrap="wrap" gap={2}>
                            <Box sx={{ flex: '1 1 200px' }}>
                              <Typography variant="body2">
                                <PeopleIcon fontSize="small" sx={{ mr: 1, verticalAlign: 'middle' }} />
                                {report.totalEmails.toLocaleString()} recipients
                              </Typography>
                            </Box>
                            <Box sx={{ flex: '1 1 200px' }}>
                              <Typography variant="body2">
                                <ScheduleIcon fontSize="small" sx={{ mr: 1, verticalAlign: 'middle' }} />
                                {scheduledTime ? 'Scheduled' : 'Immediate'}
                              </Typography>
                            </Box>
                          </Box>
                        </Box>
                      ))}
                    </Box>
                  )}

                  {nlResponse.processingTimeMs && (
                    <Typography variant="caption" color="text.secondary" display="block" mt={2}>
                      Processing time: {nlResponse.processingTimeMs}ms
                    </Typography>
                  )}
                </CardContent>
              </Card>
            )}

            <Box mt={3} display="flex" justifyContent="space-between">
              <Button onClick={handleReset} variant="outlined">
                Trigger Another Campaign
              </Button>
              <Button onClick={onClose} variant="contained">
                Close
              </Button>
            </Box>
          </Box>
        );

      default:
        return 'Unknown step';
    }
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h5" gutterBottom>
          Trigger Email Campaign
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Stepper activeStep={activeStep} orientation="vertical">
          {steps.map((step, index) => (
            <Step key={step.label}>
              <StepLabel>
                <Typography variant="subtitle1">{step.label}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {step.description}
                </Typography>
              </StepLabel>
              <StepContent>
                {getStepContent(index)}
              </StepContent>
            </Step>
          ))}
        </Stepper>
      </Paper>
    </LocalizationProvider>
  );
};

export default EmailTriggerCampaign;
