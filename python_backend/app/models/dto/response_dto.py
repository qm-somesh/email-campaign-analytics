from pydantic import BaseModel, Field, computed_field
from typing import List, Optional, Generic, TypeVar
from datetime import datetime
import math

T = TypeVar('T')

class EmailTriggerReportDto(BaseModel):
    """DTO for email trigger report data"""
    strategy_name: str = Field(..., description="Campaign/Strategy name")
    total_emails: int = Field(..., description="Total number of emails sent")
    delivered_count: int = Field(..., description="Number of delivered emails")
    bounced_count: int = Field(..., description="Number of bounced emails")
    opened_count: int = Field(..., description="Number of opened emails")
    clicked_count: int = Field(..., description="Number of clicked emails")
    complained_count: int = Field(..., description="Number of complained emails")
    unsubscribed_count: int = Field(..., description="Number of unsubscribed emails")
    first_email_sent: Optional[datetime] = Field(None, description="First email sent date")
    last_email_sent: Optional[datetime] = Field(None, description="Last email sent date")
    
    # Calculated percentage properties for better reporting
    @computed_field
    @property
    def delivery_rate(self) -> float:
        """Delivery rate percentage (delivered / total emails * 100)"""
        return round((self.delivered_count / self.total_emails * 100), 2) if self.total_emails > 0 else 0.0
    
    @computed_field
    @property
    def open_rate(self) -> float:
        """Open rate percentage (opened / delivered * 100)"""
        return round((self.opened_count / self.delivered_count * 100), 2) if self.delivered_count > 0 else 0.0
    
    @computed_field
    @property
    def click_rate(self) -> float:
        """Click rate percentage (clicked / delivered * 100)"""
        return round((self.clicked_count / self.delivered_count * 100), 2) if self.delivered_count > 0 else 0.0
    
    @computed_field
    @property
    def bounce_rate(self) -> float:
        """Bounce rate percentage (bounced / total emails * 100)"""
        return round((self.bounced_count / self.total_emails * 100), 2) if self.total_emails > 0 else 0.0
    
    @computed_field
    @property
    def complaint_rate(self) -> float:
        """Complaint rate percentage (complained / delivered * 100)"""
        return round((self.complained_count / self.delivered_count * 100), 2) if self.delivered_count > 0 else 0.0
    
    @computed_field
    @property
    def unsubscribe_rate(self) -> float:
        """Unsubscribe rate percentage (unsubscribed / delivered * 100)"""
        return round((self.unsubscribed_count / self.delivered_count * 100), 2) if self.delivered_count > 0 else 0.0
    
    class Config:
        schema_extra = {
            "example": {
                "strategy_name": "Summer Sale Campaign",
                "total_emails": 1000,
                "delivered_count": 950,
                "bounced_count": 50,
                "opened_count": 300,
                "clicked_count": 75,
                "complained_count": 5,
                "unsubscribed_count": 10,
                "first_email_sent": "2025-06-01T10:00:00Z",
                "last_email_sent": "2025-06-15T15:30:00Z",
                "delivery_rate": 95.0,
                "open_rate": 31.58,
                "click_rate": 7.89,
                "bounce_rate": 5.0,
                "complaint_rate": 0.53,
                "unsubscribe_rate": 1.05
            }
        }

class PaginatedResponse(BaseModel, Generic[T]):
    """Generic paginated response"""
    items: List[T] = Field(..., description="List of items")
    total_count: int = Field(..., description="Total number of items")
    page_number: int = Field(..., description="Current page number")
    page_size: int = Field(..., description="Number of items per page")
    total_pages: int = Field(..., description="Total number of pages")
    
    @classmethod
    def create(cls, items: List[T], total_count: int, page_number: int, page_size: int):
        total_pages = math.ceil(total_count / page_size) if page_size > 0 else 0
        return cls(
            items=items,
            total_count=total_count,
            page_number=page_number,
            page_size=page_size,
            total_pages=total_pages
        )

class DebugContextResponseDto(BaseModel):
    """DTO for debug context response"""
    query: str = Field(..., description="Original query")
    context: List[str] = Field(..., description="Retrieved context items")
    context_count: int = Field(..., description="Number of context items returned")
    
class ErrorResponseDto(BaseModel):
    """DTO for error responses"""
    error: str = Field(..., description="Error message")
    details: Optional[str] = Field(None, description="Additional error details")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Error timestamp")
