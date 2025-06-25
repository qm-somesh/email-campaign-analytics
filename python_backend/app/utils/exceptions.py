class EmailCampaignReportingException(Exception):
    """Base exception for the application"""
    pass

class ValidationException(EmailCampaignReportingException):
    """Exception for validation errors"""
    pass

class AIServiceException(EmailCampaignReportingException):
    """Exception for AI service errors"""
    pass

class DatabaseException(EmailCampaignReportingException):
    """Exception for database errors"""
    pass

class ConfigurationException(EmailCampaignReportingException):
    """Exception for configuration errors"""
    pass
