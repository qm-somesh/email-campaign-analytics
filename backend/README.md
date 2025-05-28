# Email Campaign Reporting API

A .NET 8 Web API for automotive email campaign analytics and reporting, integrated with Google BigQuery.

## Features

- Campaign management and analytics
- Email list management
- Recipient tracking
- Real-time dashboard metrics
- Email event tracking (opens, clicks, bounces, etc.)
- BigQuery integration for scalable data analytics

## API Endpoints

### Dashboard
- `GET /api/dashboard/metrics` - Get overall dashboard metrics
- `GET /api/dashboard/recent-campaigns` - Get recent campaigns
- `GET /api/dashboard/recent-events` - Get recent email events

### Campaigns
- `GET /api/campaigns` - Get all campaigns (paginated)
- `GET /api/campaigns/{id}` - Get campaign by ID
- `GET /api/campaigns/{id}/stats` - Get campaign statistics
- `GET /api/campaigns/{id}/events` - Get campaign email events

### Email Lists
- `GET /api/emaillists` - Get all email lists (paginated)
- `GET /api/emaillists/{id}` - Get email list by ID
- `GET /api/emaillists/{id}/recipients` - Get recipients for a list

### Recipients
- `GET /api/recipients` - Get all recipients (paginated)
- `GET /api/recipients/{id}` - Get recipient by ID

## Configuration

### BigQuery Setup

1. Create a Google Cloud Project
2. Enable the BigQuery API
3. Create a service account with BigQuery permissions
4. Download the service account JSON key
5. Update `appsettings.json`:

```json
{
  "BigQuery": {
    "ProjectId": "your-gcp-project-id",
    "DatasetId": "email_campaign_analytics",
    "CredentialsPath": "path/to/service-account-key.json",
    "CampaignsTable": "campaigns",
    "EmailListsTable": "email_lists",
    "RecipientsTable": "recipients",
    "EmailEventsTable": "email_events",
    "CampaignRecipientsTable": "campaign_recipients",
    "ListRecipientsTable": "list_recipients"
  }
}
```

### Running the Application

```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:7189` or `http://localhost:5189`.

## Database Schema

The application expects the following BigQuery tables:

### campaigns
- campaign_id (STRING)
- name (STRING)
- type (STRING)
- status (STRING)
- created_at (TIMESTAMP)
- launched_at (TIMESTAMP)
- completed_at (TIMESTAMP)
- subject (STRING)
- from_email (STRING)
- from_name (STRING)
- total_recipients (INTEGER)
- sent_count (INTEGER)
- delivered_count (INTEGER)
- opened_count (INTEGER)
- clicked_count (INTEGER)
- bounced_count (INTEGER)
- unsubscribed_count (INTEGER)
- complaints_count (INTEGER)
- tags (STRING)
- notes (STRING)

### email_lists
- list_id (STRING)
- name (STRING)
- description (STRING)
- status (STRING)
- created_at (TIMESTAMP)
- updated_at (TIMESTAMP)
- total_recipients (INTEGER)
- active_recipients (INTEGER)
- bounced_recipients (INTEGER)
- unsubscribed_recipients (INTEGER)
- tags (STRING)
- notes (STRING)

### recipients
- recipient_id (STRING)
- email_address (STRING)
- first_name (STRING)
- last_name (STRING)
- status (STRING)
- created_at (TIMESTAMP)
- last_engagement_at (TIMESTAMP)
- location (STRING)
- device_type (STRING)
- preferred_language (STRING)
- subscribed_at (TIMESTAMP)
- unsubscribed_at (TIMESTAMP)
- unsubscribe_reason (STRING)
- total_opens (INTEGER)
- total_clicks (INTEGER)
- total_bounces (INTEGER)
- custom_fields (STRING)
- tags (STRING)
- notes (STRING)

### email_events
- event_id (STRING)
- campaign_id (STRING)
- recipient_id (STRING)
- event_type (STRING)
- timestamp (TIMESTAMP)
- email_address (STRING)
- subject (STRING)
- reason (STRING)
- user_agent (STRING)
- ip_address (STRING)
- location (STRING)
- device_type (STRING)
- click_url (STRING)
- additional_data (STRING)

## CORS Configuration

The API is configured to allow requests from the React frontend running on `http://localhost:3000`.

## Logging

The application uses the built-in .NET logging framework. Logs are written to the console in development mode.

## Error Handling

All controllers include proper error handling with:
- Structured logging
- HTTP status codes
- User-friendly error messages
- Exception details in development mode
