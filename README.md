# Email Campaign Analytics

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.0-blue.svg)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.0-blue.svg)](https://www.typescriptlang.org/)
[![Material-UI](https://img.shields.io/badge/Material--UI-5.0-blue.svg)](https://mui.com/)

A comprehensive **full-stack email campaign reporting and analytics platform** designed for automotive email marketing campaigns. Built with **React TypeScript frontend** and **.NET 9 Web API backend** with **Google BigQuery** integration for large-scale email campaign data analysis.

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
- BigQuery-powered analytics
- Custom filtering and search
- Export capabilities
- Historical trend analysis

### 🛠 **Technical Features**
- **Backend**: ASP.NET Core Web API (.NET 9)
- **Frontend**: React 18 with TypeScript
- **Database**: Google BigQuery for analytics
- **UI Framework**: Material-UI (MUI) components
- **API Documentation**: Swagger/OpenAPI
- **Development**: Mock service for testing without BigQuery

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
   
   Update `backend/appsettings.Development.json` with your BigQuery settings:
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

## 🗄️ Database Schema

The application works with two main BigQuery tables:

### EmailOutbox Table
Contains primary email campaign data including campaigns, recipients, templates, and sending information.

### EmailStatus Table  
Tracks email delivery events, engagement metrics, and status updates.

**Relationship**: Connected via `EmailOutboxIdentifier` for comprehensive reporting.

## 🔧 Development

### Mock Data Service

The application includes a **MockBigQueryService** that provides realistic test data, allowing you to:
- Develop and test without BigQuery credentials
- Work with realistic campaign, recipient, and engagement data
- Demonstrate the application functionality

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
