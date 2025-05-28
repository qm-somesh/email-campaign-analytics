# Email Campaign Reporting Application - Development Instructions

## Project Overview
This is an Email Campaign Reporting Application with a React.js frontend and .NET 9 backend microservice that connects to Google BigQuery for automotive email campaign analytics and reporting.

## Current Status
- **Backend**: ✅ Completed and functional with MockBigQueryService for testing
- **Frontend**: 🚧 Basic React TypeScript structure created, needs development
- **BigQuery Integration**: ✅ Service layer implemented for EmailOutbox and EmailStatus tables
- **API Documentation**: ✅ Swagger/OpenAPI configured and working
- **Testing**: ✅ Mock service allows testing without BigQuery credentials

## Architecture
- **Backend**: ASP.NET Core Web API (.NET 9) with BigQuery integration
- **Frontend**: React TypeScript application with Material-UI components (to be implemented)
- **Database**: Google BigQuery for analytics and reporting data
- **API Communication**: RESTful API with JSON responses
- **Development**: MockBigQueryService for local development and testing

## Backend Structure

### Models
- `Campaign`: Email campaign data model
- `EmailList`: Email list management model  
- `Recipient`: Email recipient model
- `EmailEvent`: Email event tracking model (opens, clicks, bounces, etc.)

### Controllers
- `CampaignsController`: Campaign management endpoints
- `DashboardController`: Dashboard metrics and analytics
- `EmailListsController`: Email list management
- `RecipientsController`: Recipient management

### Services
- `IBigQueryService`: Interface for BigQuery operations
- `BigQueryService`: Implementation of BigQuery data access layer for EmailOutbox and EmailStatus tables
- `MockBigQueryService`: Mock implementation for development and testing without BigQuery credentials

### Configuration
- `BigQueryOptions`: BigQuery connection and table configuration
- CORS enabled for frontend communication
- OpenAPI/Swagger documentation

## API Endpoints

### Campaigns
- `GET /api/campaigns` - Get all campaigns with pagination
- `GET /api/campaigns/{id}` - Get specific campaign
- `GET /api/campaigns/{id}/stats` - Get campaign statistics
- `GET /api/campaigns/{id}/events` - Get campaign email events

### Dashboard  
- `GET /api/dashboard/metrics` - Get overall dashboard metrics
- `GET /api/dashboard/recent-campaigns` - Get recent campaigns
- `GET /api/dashboard/recent-events` - Get recent email events

### Email Lists
- `GET /api/emaillists` - Get all email lists
- `GET /api/emaillists/{id}` - Get specific email list

### Recipients
- `GET /api/recipients` - Get recipients with optional list filtering
- `GET /api/recipients/{id}` - Get specific recipient

## Database Schema

### 1. EmailOutbox Table

| Column Name              | Data Type           | Description                                           |
|------------------------- |-------------------- |------------------------------------------------------|
| EmailOutboxId            | STRING(300)         | Unique ID for the outbox record                      |
| EmailOutboxIdentifier    | STRING(200)         | Identifier for correlation (with EmailStatus)         |
| EmailCustomers_SID       | INT64               | Customer ID                                          |
| AccountId                | INT64               | Account ID                                           |
| EmailTo                  | STRING(500)         | Recipient email                                      |
| FirstName                | STRING(50)          | Customer first name                                  |
| LastName                 | STRING(50)          | Customer last name                                   |
| Pin                      | STRING(50)          | Customer PIN                                         |
| StrategyId               | INT64               | Campaign/strategy ID                                 |
| StrategySid              | INT64               | Alternate campaign ID                                |
| StrategyName             | STRING(100)         | Campaign/campaign name                               |
| EmailTemplateId          | INT64               | Email template ID                                    |
| TemplateName             | STRING(100)         | Email template name                                  |
| Subject                  | STRING(500)         | Email subject                                        |
| EmailFrom                | STRING(200)         | Sender                                               |
| EmailCc                  | STRING(500)         | CC recipients                                        |
| EmailBcc                 | STRING(500)         | BCC recipients                                       |
| StatusId                 | INT64               | Status ID (sent, delivered, failed, etc.)            |
| Status                   | STRING(50)          | Status description                                   |
| Message                  | STRING              | Status message/log                                   |
| MailgunMessageId         | STRING(200)         | Mailgun message ID                                   |
| DateCreated              | TIMESTAMP           | Record creation date                                 |
| ScheduledDate            | TIMESTAMP           | Scheduled send date                                  |
| EmailServiceType         | STRING(50)          | Email service type                                   |
| CommunicationId          | INT64               | Communication ID                                     |
| ActionId                 | INT64               | Action ID                                            |
| EventType                | STRING(200)         | Email event type                                     |
| EventId                  | INT64               | Event ID                                             |
| EventName                | STRING(100)         | Event name                                           |
| EmailType                | STRING(50)          | Email type                                           |
| WorkflowId               | STRING(50)          | Workflow ID                                          |
| WorkflowDefinitionId     | STRING(100)         | Workflow definition ID                               |
| VehicleStatus            | STRING(50)          | Vehicle status (if applicable)                       |
| BatchGlobalVariables     | STRING              | Global variables for batch                           |
| BatchUserVariables       | STRING              | User variables for batch                             |
| HtmlContent              | STRING              | Email HTML content                                   |
| EmailBatchIdentifier     | STRING(100)         | Batch identifier                                     |
| BatchDate                | TIMESTAMP           | Batch date                                           |
| IsSampleEmail            | BOOL                | Whether this is a sample/test email                  |
| IsDnc                    | BOOL                | Do not contact flag                                  |
| EmailDomain              | STRING(100)         | Email domain                                         |
| ClientName               | STRING(100)         | Client name                                          |
| CommunicationLinks       | STRING              | Communication links (if any)                         |
| CKSConsumerId            | INT64               | Consumer ID                                          |
| CKSConsumerCountry       | STRING              | Consumer country                                     |
| EmailProvider            | STRING(100)         | Email provider (e.g., Mailgun)                       |

---

### 2. EmailStatus Table

| Column Name              | Data Type           | Description                                           |
|------------------------- |-------------------- |------------------------------------------------------|
| Id                       | STRING(200)         | Unique status record ID                               |
| EmailOutboxIdentifier    | STRING(200)         | Links to EmailOutbox                                  |
| Recipient                | STRING(200)         | Recipient email                                       |
| StatusId                 | INT64               | Email status ID                                       |
| Status                   | STRING(50)          | Delivered, Failed, Complained, Unsubscribed, etc.     |
| MailgunMessageId         | STRING              | Mailgun message ID                                    |
| MailgunEventId           | STRING              | Mailgun event ID                                      |
| Severity                 | STRING(50)          | Severity of event                                     |
| Reason                   | STRING(50)          | Reason for failure, complaint, etc.                   |
| LogLevel                 | STRING(1000)        | Logging level                                         |
| DeliveryStatusCode       | STRING(50)          | Delivery status code                                  |
| Tags                     | STRING(200)         | Tags                                                  |
| UserVariables            | STRING              | User variables                                        |
| EmailCustomers_SID       | INT64               | Customer ID                                           |
| FirstName                | STRING(100)         | Customer first name                                   |
| LastName                 | STRING(100)         | Customer last name                                    |
| Pin                      | STRING(50)          | Customer PIN                                          |
| StrategyId               | INT64               | Campaign/strategy ID                                  |
| StrategyName             | STRING(100)         | Campaign name                                         |
| EmailTemplateId          | INT64               | Email template ID                                     |
| TemplateName             | STRING(100)         | Email template name                                   |
| WorkflowDefinitionId     | STRING(100)         | Workflow definition ID                                |
| EmailServiceType         | STRING(50)          | Email service type                                    |
| HtmlContent              | STRING              | Email content                                         |
| RawMailgunRequestObject  | STRING              | Raw Mailgun API request                               |
| MailgunMessage           | STRING              | Raw Mailgun message                                   |
| MigrationJobId           | STRING              | Migration job ID                                      |
| IsDnc                    | BOOL                | Do not contact flag                                   |
| MailgunDateCreated       | TIMESTAMP           | Mailgun event timestamp                               |
| DateCreated_UTC          | TIMESTAMP           | UTC creation date                                     |
| IsBotEvent               | BOOL                | Is Bot event                                          |
| LinkText                 | STRING              | Link text (if event is link click)                    |
| LinkCategory             | STRING              | Link category                                         |
| LinkUrl                  | STRING              | Link URL                                              |
| IP                       | STRING(100)         | IP address of event                                   |
| DomainName               | STRING(500)         | Domain name                                           |
| EmailProvider            | STRING(100)         | Email provider                                        |

---

## Key Relationships

- **EmailOutbox.EmailOutboxIdentifier** ↔ **EmailStatus.EmailOutboxIdentifier** (Primary join for reporting)
- **Status/StatusId** fields indicate delivery status for both tables.

## Development Setup

### Prerequisites
- .NET 9 SDK
- Node.js and npm
- Google Cloud credentials for BigQuery access

### Backend Configuration
Update `appsettings.Development.json` with your BigQuery settings:
```json
{
  "BigQuery": {
    "ProjectId": "teamvelocity-dev-330212",
    "DatasetId": "PrecisionEmail", 
    "CredentialsPath": "path/to/service-account-key.json",
    "EmailOutboxTable": "EmailOutbox",
    "EmailStatusTable": "EmailStatus"
  }
}
```

### Development with Mock Service
The application automatically uses `MockBigQueryService` when:
- BigQuery credentials are not available
- The credentials path contains placeholder text
- For local development and testing without BigQuery access

### Running the Application
Use VS Code tasks:
- `build-backend`: Build the .NET API
- `run-backend`: Run the backend API
- `watch-backend`: Run backend with hot reload
- `run-frontend`: Start React development server
- `install-frontend`: Install frontend dependencies
- `build-frontend`: Build the frontend for production

### Accessing the Application
- Backend API: http://localhost:5037
- Swagger UI: http://localhost:5037/swagger
- Frontend (when implemented): http://localhost:3000

## Code Patterns

### Error Handling
- All controllers use try-catch blocks with proper logging
- Returns appropriate HTTP status codes
- Structured error messages for client consumption

### Data Access
- Repository pattern implemented through `IBigQueryService`
- Parameterized queries to prevent SQL injection
- Proper async/await usage throughout

### API Design
- RESTful conventions
- Consistent response formats
- Pagination support for list endpoints
- Optional filtering parameters

## Key Features Implemented

### Backend Features ✅
- BigQuery integration for analytics queries
- Campaign performance metrics calculation
- Email event tracking and reporting
- Pagination and filtering support
- CORS configuration for frontend integration
- Comprehensive error handling and logging
- OpenAPI documentation
- MockBigQueryService for development testing

### Data Models ✅
- Comprehensive campaign tracking
- Email engagement metrics
- Recipient management
- Event-driven email tracking
- Flexible tagging and custom fields

### Mock Data Available
- Sample campaign data with various types (promotional, newsletter, announcement)
- Email list management with subscriber counts
- Recipient data with engagement history
- Email event tracking (opens, clicks, bounces)
- Dashboard metrics and performance data

## Next Steps for Frontend Development
1. Install additional dependencies (Material-UI, charts library, axios)
2. Create dashboard components with data visualization
3. Implement campaign detail views
4. Add email list management interface
5. Create recipient management features
6. Set up API integration with error handling
7. Implement responsive design with modern UI

## Testing the Backend
With the MockBigQueryService, you can test all API endpoints:
- Visit http://localhost:5037/swagger for interactive API documentation
- Test campaign endpoints: GET /api/campaigns
- Test dashboard metrics: GET /api/dashboard/metrics
- Test email lists: GET /api/emaillists
- Test recipients: GET /api/recipients

## Performance Considerations
- BigQuery queries optimized for large datasets
- Pagination implemented to handle large result sets
- Async operations throughout for scalability
- Proper connection management for BigQuery client

## Security Notes
- BigQuery credentials should be stored securely
- API endpoints should include authentication in production
- CORS configured for development (restrict in production)
- Input validation implemented for all endpoints
