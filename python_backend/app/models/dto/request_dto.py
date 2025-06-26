from pydantic import BaseModel, Field, validator
from typing import Optional

class NaturalLanguageSqlQueryRequestDto(BaseModel):
    """DTO for natural language SQL query requests"""
    query: str = Field(..., min_length=1, max_length=1000, description="Natural language query")
    page_number: int = Field(1, ge=1, description="Page number for pagination")
    page_size: int = Field(50, ge=1, le=1000, description="Number of items per page")
    
    @validator('query')
    def validate_query(cls, v):
        if not v or not v.strip():
            raise ValueError('Query cannot be empty or whitespace only')
        return v.strip()
    
    class Config:
        schema_extra = {
            "example": {
                "query": "Show me campaigns with high open rates from last month",
                "page_number": 1,
                "page_size": 50
            }
        }

class DebugContextRequestDto(BaseModel):
    """DTO for debug context requests"""
    query: str = Field(..., min_length=1, max_length=1000, description="Query to get context for")
    max_items: int = Field(10, ge=1, le=50, description="Maximum context items to return")
