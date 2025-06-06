import React, { useState } from 'react';
import {
  Box,
  Typography,
  Chip,
  Card,
  CardContent,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Tooltip,
  IconButton,
  Collapse,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Lightbulb as LightbulbIcon,
  Visibility as VisibilityIcon,
  VisibilityOff as VisibilityOffIcon,
  Help as HelpIcon,
} from '@mui/icons-material';

interface QueryExample {
  query: string;
  description: string;
  category: string;
  difficulty: 'basic' | 'intermediate' | 'advanced';
}

interface QueryCategory {
  name: string;
  icon: string;
  description: string;
  examples: QueryExample[];
}

interface NaturalLanguageQueryExamplesProps {
  onExampleClick: (query: string) => void;
  compact?: boolean;
}

const NaturalLanguageQueryExamples: React.FC<NaturalLanguageQueryExamplesProps> = ({
  onExampleClick,
  compact = false,
}) => {
  const [expandedCategories, setExpandedCategories] = useState<string[]>(['performance']);
  const [showAllExamples, setShowAllExamples] = useState(false);

  const queryCategories: QueryCategory[] = [
    {
      name: 'Performance',
      icon: '🎯',
      description: 'Email campaign performance and engagement metrics',
      examples: [
        {
          query: 'Show me campaigns with high click rates',
          description: 'Find campaigns performing above average',
          category: 'performance',
          difficulty: 'basic'
        },
        {
          query: 'What campaigns had more than 1000 opens last week?',
          description: 'Filter by specific engagement metrics',
          category: 'performance',
          difficulty: 'basic'
        },
        {
          query: 'Find campaigns with click rate above 5% in the last month',
          description: 'Performance threshold filtering',
          category: 'performance',
          difficulty: 'intermediate'
        },
        {
          query: 'Show me underperforming campaigns from Q4',
          description: 'Identify campaigns needing attention',
          category: 'performance',
          difficulty: 'intermediate'
        }
      ]
    },
    {
      name: 'Discovery',
      icon: '🔍',
      description: 'Find and explore email campaigns and lists',
      examples: [
        {
          query: 'Show me all promotional campaigns',
          description: 'Filter by campaign type',
          category: 'discovery',
          difficulty: 'basic'
        },
        {
          query: 'Find campaigns sent to VIP customers',
          description: 'Search by recipient segments',
          category: 'discovery',
          difficulty: 'basic'
        },
        {
          query: 'What campaigns were sent from the sales team?',
          description: 'Filter by sender or department',
          category: 'discovery',
          difficulty: 'intermediate'
        },
        {
          query: 'Show me campaigns with Black Friday in the subject',
          description: 'Search by subject line content',
          category: 'discovery',
          difficulty: 'intermediate'
        }
      ]
    },
    {
      name: 'Volume',
      icon: '📊',
      description: 'Email volume and delivery statistics',
      examples: [
        {
          query: 'How many emails were sent yesterday?',
          description: 'Daily volume tracking',
          category: 'volume',
          difficulty: 'basic'
        },
        {
          query: 'Show me campaigns with more than 10000 recipients',
          description: 'Large campaign identification',
          category: 'volume',
          difficulty: 'basic'
        },
        {
          query: 'What is our weekly email volume trend?',
          description: 'Volume trend analysis',
          category: 'volume',
          difficulty: 'intermediate'
        },
        {
          query: 'Find campaigns that exceeded our daily send limit',
          description: 'Compliance and limit monitoring',
          category: 'volume',
          difficulty: 'advanced'
        }
      ]
    },
    {
      name: 'Time Analysis',
      icon: '📅',
      description: 'Time-based queries and scheduling',
      examples: [
        {
          query: 'Show me campaigns from last month',
          description: 'Time period filtering',
          category: 'time',
          difficulty: 'basic'
        },
        {
          query: 'What campaigns are scheduled for tomorrow?',
          description: 'Upcoming campaign preview',
          category: 'time',
          difficulty: 'basic'
        },
        {
          query: 'Compare this week performance to last week',
          description: 'Week-over-week comparison',
          category: 'time',
          difficulty: 'intermediate'
        },
        {
          query: 'Show me campaigns sent on weekends in December',
          description: 'Specific time pattern analysis',
          category: 'time',
          difficulty: 'advanced'
        }
      ]
    },
    {
      name: 'Troubleshooting',
      icon: '🔧',
      description: 'Issues, failures, and problem diagnosis',
      examples: [
        {
          query: 'Show me failed email deliveries',
          description: 'Identify delivery issues',
          category: 'troubleshooting',
          difficulty: 'basic'
        },
        {
          query: 'What campaigns had high bounce rates?',
          description: 'Quality issues identification',
          category: 'troubleshooting',
          difficulty: 'basic'
        },
        {
          query: 'Find campaigns with spam complaints',
          description: 'Reputation management',
          category: 'troubleshooting',
          difficulty: 'intermediate'
        },
        {
          query: 'Show me emails that failed due to invalid addresses',
          description: 'Specific failure analysis',
          category: 'troubleshooting',
          difficulty: 'intermediate'
        }
      ]
    },
    {
      name: 'Comparative Analysis',
      icon: '⚖️',
      description: 'Compare campaigns, time periods, and segments',
      examples: [
        {
          query: 'Compare newsletter vs promotional campaign performance',
          description: 'Campaign type comparison',
          category: 'comparative',
          difficulty: 'intermediate'
        },
        {
          query: 'How do mobile vs desktop open rates compare?',
          description: 'Device performance comparison',
          category: 'comparative',
          difficulty: 'intermediate'
        },
        {
          query: 'Compare Q1 and Q2 campaign effectiveness',
          description: 'Quarterly performance analysis',
          category: 'comparative',
          difficulty: 'advanced'
        },
        {
          query: 'Show me best performing subject lines by campaign type',
          description: 'Cross-segment optimization insights',
          category: 'comparative',
          difficulty: 'advanced'
        }
      ]
    }
  ];

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'basic':
        return '#4caf50';
      case 'intermediate':
        return '#ff9800';
      case 'advanced':
        return '#f44336';
      default:
        return '#9e9e9e';
    }
  };

  const handleCategoryToggle = (category: string) => {
    setExpandedCategories(prev =>
      prev.includes(category)
        ? prev.filter(c => c !== category)
        : [...prev, category]
    );
  };

  if (compact) {
    // Show only a few key examples as clickable chips
    const featuredExamples = [
      'Show me campaigns with high click rates',
      'What campaigns had more than 1000 opens?',
      'Find failed email deliveries',
      'Compare this month to last month'
    ];

    return (
      <Box sx={{ mb: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <Typography variant="caption" color="text.secondary">
            Try these examples:
          </Typography>
          <Tooltip title="Click to see all examples">
            <IconButton 
              onClick={() => setShowAllExamples(!showAllExamples)}
              size="small"
            >
              {showAllExamples ? <VisibilityOffIcon /> : <VisibilityIcon />}
            </IconButton>
          </Tooltip>
        </Box>
        
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1 }}>
          {featuredExamples.map((example, index) => (
            <Chip
              key={index}
              label={example}
              size="small"
              variant="outlined"
              onClick={() => onExampleClick(example)}
              sx={{
                cursor: 'pointer',
                '&:hover': {
                  backgroundColor: 'primary.light',
                  color: 'white',
                },
              }}
            />
          ))}
        </Box>

        <Collapse in={showAllExamples}>
          <Card sx={{ mt: 2 }}>
            <CardContent>
              {queryCategories.map((category) => (
                <Accordion
                  key={category.name.toLowerCase()}
                  expanded={expandedCategories.includes(category.name.toLowerCase())}
                  onChange={() => handleCategoryToggle(category.name.toLowerCase())}
                >
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography sx={{ fontSize: 16 }}>{category.icon}</Typography>
                      <Typography variant="subtitle2">{category.name}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        ({category.examples.length} examples)
                      </Typography>
                    </Box>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Typography variant="caption" color="text.secondary" sx={{ mb: 2, display: 'block' }}>
                      {category.description}
                    </Typography>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      {category.examples.map((example, index) => (
                        <Box
                          key={index}
                          onClick={() => onExampleClick(example.query)}
                          sx={{
                            p: 1,
                            border: '1px solid',
                            borderColor: 'divider',
                            borderRadius: 1,
                            cursor: 'pointer',
                            '&:hover': {
                              backgroundColor: 'action.hover',
                              borderColor: 'primary.main',
                            },
                          }}
                        >
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <Typography variant="body2" sx={{ fontWeight: 500 }}>
                              {example.query}
                            </Typography>
                            <Chip
                              label={example.difficulty}
                              size="small"
                              sx={{
                                backgroundColor: getDifficultyColor(example.difficulty),
                                color: 'white',
                                fontSize: '0.6rem',
                                height: 16,
                              }}
                            />
                          </Box>
                          <Typography variant="caption" color="text.secondary">
                            {example.description}
                          </Typography>
                        </Box>
                      ))}
                    </Box>
                  </AccordionDetails>
                </Accordion>
              ))}
            </CardContent>
          </Card>
        </Collapse>
      </Box>
    );
  }

  // Full view mode - show all examples in an organized way
  return (
    <Box>
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <LightbulbIcon color="primary" />
            <Typography variant="h6">Natural Language Query Examples</Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Click any example below to try it out. These queries demonstrate the types of questions you can ask about your email campaigns.
          </Typography>
        </CardContent>
      </Card>

      {queryCategories.map((category) => (
        <Accordion
          key={category.name.toLowerCase()}
          expanded={expandedCategories.includes(category.name.toLowerCase())}
          onChange={() => handleCategoryToggle(category.name.toLowerCase())}
          sx={{ mb: 1 }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <Typography sx={{ fontSize: 20 }}>{category.icon}</Typography>
              <Box>
                <Typography variant="h6">{category.name}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {category.description}
                </Typography>
              </Box>
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))' }}>
              {category.examples.map((example, index) => (
                <Card
                  key={index}
                  onClick={() => onExampleClick(example.query)}
                  sx={{
                    cursor: 'pointer',
                    transition: 'all 0.2s',
                    '&:hover': {
                      transform: 'translateY(-2px)',
                      boxShadow: 3,
                      borderColor: 'primary.main',
                    },
                  }}
                  variant="outlined"
                >
                  <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                      <Typography variant="body1" sx={{ fontWeight: 500, flex: 1 }}>
                        "{example.query}"
                      </Typography>
                      <Chip
                        label={example.difficulty}
                        size="small"
                        sx={{
                          backgroundColor: getDifficultyColor(example.difficulty),
                          color: 'white',
                          fontSize: '0.7rem',
                          ml: 1,
                        }}
                      />
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      {example.description}
                    </Typography>
                  </CardContent>
                </Card>
              ))}
            </Box>
          </AccordionDetails>
        </Accordion>
      ))}

      <Card sx={{ mt: 3, backgroundColor: 'info.light' }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <HelpIcon color="info" />
            <Typography variant="subtitle1" color="info.dark">
              Pro Tips
            </Typography>
          </Box>
          <Typography variant="body2" color="info.dark">
            • Be specific: "campaigns from last week" works better than "recent campaigns"
            <br />
            • Use numbers: "more than 1000 emails" or "above 5% click rate"
            <br />
            • Ask for comparisons: "compare this month to last month"
            <br />
            • Use natural language: "show me", "find", "what campaigns", "how many"
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
};

export default NaturalLanguageQueryExamples;
