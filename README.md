# Email Campaign Analytics

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.0-blue.svg)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-blue.svg)](https://www.typescriptlang.org/)
[![Material-UI](https://img.shields.io/badge/Material--UI-5.0-blue.svg)](https://mui.com/)

A comprehensive **full-stack email campaign reporting and analytics platform** designed for automotive email marketing campaigns. Built with **React TypeScript frontend** and **.NET 9 Web API backend** with data-driven insights for large-scale email campaign analysis.

## 🚀 Features

### 📊 **Dashboard & Analytics**
- Real-time email campaign performance metrics
- Interactive charts and visualizations
- Campaign engagement tracking (opens, clicks, bounces)
- Recipient management and segmentation

### 📧 **Email Campaign Management**
- Campaign performance monitoring
- Email list management
- Recipient engagement tracking
- Delivery status monitoring

### 🔍 **Advanced Reporting**
- In-memory data analytics
- Custom filtering and search
- Export capabilities
- Historical trend analysis

### 🛠 **Technical Features**
- **Backend**: ASP.NET Core Web API (.NET 9)
- **Frontend**: React 18 with TypeScript
- **Database**: In-memory data service with SQL Server integration capability
- **UI Framework**: Material-UI (MUI) components
- **API Documentation**: Swagger/OpenAPI
- **Development**: Mock services for testing

## 📸 Screenshots

*Coming soon - Application screenshots will be added here*

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   React TypeScript  │    │  .NET 9 Web API  │    │  Google BigQuery │
│   Frontend       │────│     Backend      │────│   Data Store    │
│   (Port 3000)    │    │   (Port 5037)    │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                        │                       │
         │                        │                       │
    Material-UI              Swagger/OpenAPI        EmailOutbox &
    Components               Documentation          EmailStatus Tables
```

## 🚦 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18 or later)
- [Git](https://git-scm.com/)
- Google Cloud account with BigQuery access (optional for development)

### 📥 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/qm-somesh/email-campaign-analytics.git
   cd email-campaign-analytics
   ```

2. **Backend Setup**
   ```bash
   cd backend
   dotnet restore
   dotnet build
   ```

3. **Frontend Setup**
   ```bash
   cd ../frontend
   npm install
   ```

4. **Configuration** (Optional for development)
   
   **BigQuery Configuration** - Update `backend/appsettings.Development.json`:
   ```json
   {
     "BigQuery": {
       "ProjectId": "your-project-id",
       "DatasetId": "your-dataset-id",
       "CredentialsPath": "path/to/service-account-key.json",
       "EmailOutboxTable": "EmailOutbox",
       "EmailStatusTable": "EmailStatus"
     }
   }
   ```

   **SQL Server Configuration** - Add to `backend/appsettings.Development.json`:
   ```json
   {
     "SqlServer": {
       "ConnectionString": "Server=your-server;Database=your-database;Trusted_Connection=true;",
       "EmailTriggerTable": "EmailTrigger",
       "EmailOutboxTable": "EmailOutbox",
       "WebhookLogsTable": "WebhookLogs",
       "EmailStatusTable": "EmailStatus",
       "CommandTimeoutSeconds": 30
     }
   }
   ```

### 🏃‍♂️ Running the Application

#### Option 1: Using VS Code Tasks (Recommended)
If you're using VS Code, you can use the pre-configured tasks:

- **Backend**: `Ctrl+Shift+P` → "Tasks: Run Task" → "run-backend"
- **Frontend**: `Ctrl+Shift+P` → "Tasks: Run Task" → "run-frontend"

#### Option 2: Manual Commands

**Start the Backend API:**
```bash
cd backend
dotnet run
```
*Backend will be available at: http://localhost:5037*
*Swagger documentation: http://localhost:5037/swagger*

**Start the Frontend:**
```bash
cd frontend
npm start
```
*Frontend will be available at: http://localhost:3000*

## 📚 API Documentation

The API documentation is automatically generated and available at:
- **Swagger UI**: http://localhost:5037/swagger
- **OpenAPI Specification**: http://localhost:5037/swagger/v1/swagger.json

### Key API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/campaigns` | GET | List all campaigns with pagination |
| `/api/campaigns/{id}` | GET | Get specific campaign details |
| `/api/campaigns/{id}/stats` | GET | Get campaign statistics |
| `/api/dashboard/metrics` | GET | Get overall dashboard metrics |
| `/api/recipients` | GET | List recipients with filtering |
| `/api/emaillists` | GET | Get email lists |
| `/api/emailtriggerreport` | GET | Get email trigger reports with pagination |
| `/api/emailtriggerreport/strategy/{name}` | GET | Get trigger report by strategy name |
| `/api/emailtriggerreport/summary` | GET | Get trigger report summary statistics |
| `/api/emailtriggerreport/strategies` | GET | Get all available strategy names |

## EmailTriggerReport API Endpoints

| Method | Endpoint | Description | Parameters |
|--------|----------|-------------|------------|
| GET | `/api/EmailTriggerReport` | Get all email trigger reports with pagination | `pageNumber`, `pageSize` |
| GET | `/api/EmailTriggerReport/{strategyName}` | Get specific email trigger report by strategy name | `strategyName` (path parameter) |
| GET | `/api/EmailTriggerReport/summary` | Get summary statistics for all email triggers | None |
| GET | `/api/EmailTriggerReport/strategy-names` | Get list of all available strategy names | None |
| **NEW** | `/api/EmailTriggerReport/filtered` | **Get email trigger reports with flexible filtering and pagination** | See detailed parameters below |

### New Filtered Endpoint Parameters

The `/api/EmailTriggerReport/filtered` endpoint supports comprehensive filtering:

#### Filter Parameters:
- `strategyName` (string, optional): Filter by strategy name (partial match)
- `firstEmailSentFrom` (DateTime, optional): Filter by minimum first email sent date
- `firstEmailSentTo` (DateTime, optional): Filter by maximum first email sent date  
- `lastEmailSentFrom` (DateTime, optional): Filter by minimum last email sent date
- `lastEmailSentTo` (DateTime, optional): Filter by maximum last email sent date
- `minTotalEmails` (int, optional): Filter by minimum total emails count
- `maxTotalEmails` (int, optional): Filter by maximum total emails count
- `minDeliveredCount` (int, optional): Filter by minimum delivered count
- `minOpenedCount` (int, optional): Filter by minimum opened count
- `minClickedCount` (int, optional): Filter by minimum clicked count

#### Pagination Parameters:
- `pageNumber` (int, default: 1): Page number (1-based)
- `pageSize` (int, default: 50): Number of items per page (1-1000)

#### Sorting Parameters:
- `sortBy` (string, default: "StrategyName"): Sort field name
  - Valid values: `StrategyName`, `TotalEmails`, `DeliveredCount`, `BouncedCount`, `OpenedCount`, `ClickedCount`, `FirstEmailSent`, `LastEmailSent`
- `sortDirection` (string, default: "asc"): Sort direction (`asc` or `desc`)

#### Example Requests:

```http
# Filter by strategy name containing "Active"
GET /api/EmailTriggerReport/filtered?strategyName=Active&pageSize=10

# Filter by date range
GET /api/EmailTriggerReport/filtered?firstEmailSentFrom=2024-01-01&firstEmailSentTo=2024-12-31

# Filter by minimum counts with sorting
GET /api/EmailTriggerReport/filtered?minTotalEmails=100&minDeliveredCount=50&sortBy=TotalEmails&sortDirection=desc

# Complex filtering
GET /api/EmailTriggerReport/filtered?strategyName=newsletter&minTotalEmails=50&minOpenedCount=25&sortBy=OpenedCount&sortDirection=desc&pageNumber=1&pageSize=5
```

#### Response Format:

The filtered endpoint returns a paginated response:

```json
{
  "items": [
    {
      "strategyName": "New Active Shopper",
      "totalEmails": 479,
      "deliveredCount": 12,
      "bouncedCount": 0,
      "openedCount": 13,
      "clickedCount": 13,
      "complainedCount": 0,
      "unsubscribedCount": 0,
      "firstEmailSent": "2023-04-26T07:15:55.017",
      "lastEmailSent": "2025-05-01T07:38:23.797",
      "deliveryRate": 2.5052192066805845511482254700,
      "openRate": 108.33333333333333333333333333,
      "clickRate": 108.33333333333333333333333333,
      "bounceRate": 0,
      "complaintRate": 0,
      "unsubscribeRate": 0
    }
  ],
  "totalCount": 4,
  "pageNumber": 1,
  "pageSize": 3,
  "totalPages": 2,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

#### Validation:

The endpoint includes comprehensive validation:
- Page number must be greater than 0
- Page size must be between 1 and 1000
- Date ranges must be valid (from date ≤ to date)
- Numeric ranges must be valid (min ≤ max)
- Invalid parameters return 400 Bad Request with descriptive error messages

## 🗄️ Database Schema

The application works with multiple data sources for comprehensive email campaign analytics:

### Google BigQuery Tables

#### EmailOutbox Table
Contains primary email campaign data including campaigns, recipients, templates, and sending information.

#### EmailStatus Table  
Tracks email delivery events, engagement metrics, and status updates.

**Relationship**: Connected via `EmailOutboxIdentifier` for comprehensive reporting.

### SQL Server Tables

#### EmailTrigger Table
Contains email trigger definitions and descriptions for campaign strategies.

#### WebhookLogs Table
Tracks webhook delivery status and logs for email events.

**Integration**: EmailTriggerService provides specialized reporting for SQL Server-based trigger data with real-time analytics.

## 🔧 Development

### Mock Data Service

The application includes comprehensive mock services that provide realistic test data:

- **MockBigQueryService**: Provides campaign, recipient, and engagement data for BigQuery operations
- **MockEmailTriggerService**: Provides email trigger reports and statistics for SQL Server operations

This allows you to:
- Develop and test without external database credentials
- Work with realistic campaign, recipient, and trigger data
- Demonstrate the full application functionality
- Test all API endpoints with sample data

### Project Structure

```
email-campaign-analytics/
├── backend/                    # .NET 9 Web API
│   ├── Controllers/           # API controllers
│   ├── Models/               # Data models
│   ├── Services/             # Business logic
│   └── Configuration/        # App settings
├── frontend/                  # React TypeScript app
│   ├── src/
│   │   ├── components/       # Reusable components
│   │   ├── pages/           # Page components
│   │   ├── services/        # API services
│   │   ├── types/           # TypeScript definitions
│   │   └── utils/           # Utility functions
│   └── public/              # Static assets
└── README.md
```

### Available Scripts

**Backend:**
- `dotnet build` - Build the API
- `dotnet run` - Run the API
- `dotnet watch run` - Run with hot reload

**Frontend:**
- `npm start` - Start development server
- `npm build` - Build for production
- `npm test` - Run tests

## 🧪 Testing

### Current Testing Strategy
- **Manual Testing**: Browser-based UI testing
- **API Testing**: Swagger UI for endpoint testing
- **Integration Testing**: End-to-end workflow validation

### Recommended Testing (Future)
- **Backend**: Unit tests with xUnit
- **Frontend**: Component tests with React Testing Library
- **E2E**: Cypress or Playwright tests

## 🚀 Deployment

### Development Environment
- ✅ Fully functional with mock data
- ✅ No external dependencies required
- ✅ Complete development workflow available

### Production Considerations
- **Required**: Google BigQuery credentials and configuration
- **Required**: Authentication/authorization implementation
- **Recommended**: Docker containerization
- **Recommended**: CI/CD pipeline setup

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Google BigQuery** for scalable analytics data storage
- **Material-UI** for beautiful React components
- **ASP.NET Core** for robust backend API framework
- **TypeScript** for type-safe development

## 📞 Support

If you have any questions or need help with setup, please:

1. Check the [Issues](https://github.com/qm-somesh/email-campaign-analytics/issues) page
2. Create a new issue with detailed information
3. Review the API documentation at `/swagger`

---

**Built with ❤️ for email marketing analytics**

*Last Updated: May 28, 2025*
