import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';

// Components
import Layout from './components/Layout';

// Pages
import Dashboard from './pages/Dashboard';
// import CampaignsPage from './pages/CampaignsPage';
// import EmailListsPage from './pages/EmailListsPage';
// import RecipientsPage from './pages/RecipientsPage';
// import AnalyticsPage from './pages/AnalyticsPage';
// import SettingsPage from './pages/SettingsPage';
import EmailTriggerPage from './pages/EmailTriggerPage';

// Create Material-UI theme
const theme = createTheme({
  palette: {
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f5f5f5',
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h4: {
      fontWeight: 600,
    },
    h5: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 600,
    },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          borderRadius: 8,
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          borderRadius: 8,
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 6,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 6,
        },
      },
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <LocalizationProvider dateAdapter={AdapterDateFns}>
        <Router>
          <Layout>            <Routes>
              <Route path="/" element={<Dashboard />} />
              {/* <Route path="/campaigns" element={<CampaignsPage />} /> */}
              {/* <Route path="/email-lists" element={<EmailListsPage />} /> */}
              {/* <Route path="/recipients" element={<RecipientsPage />} /> */}
              {/* <Route path="/analytics" element={<AnalyticsPage />} /> */}
              <Route path="/email-triggers" element={<EmailTriggerPage />} />
              {/* <Route path="/settings" element={<SettingsPage />} /> */}
            </Routes>
          </Layout>
        </Router>
      </LocalizationProvider>
    </ThemeProvider>
  );
}

export default App;
