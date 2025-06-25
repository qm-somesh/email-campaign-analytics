from pydantic_settings import BaseSettings
from typing import List

class Settings(BaseSettings):    # Database Configuration
    database_url: str = "mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server"
    database_pool_size: int = 10
    database_max_overflow: int = 20
    
    # Gemini AI Configuration
    gemini_api_key: str = ""
    gemini_model_name: str = "gemini-1.5-flash"
    gemini_base_url: str = "https://generativelanguage.googleapis.com/v1"
    gemini_temperature: float = 0.1
    gemini_max_tokens: int = 512
    
    # Embedding Configuration
    embedding_model_name: str = "all-MiniLM-L6-v2"
    embedding_dimension: int = 384
    max_context_items: int = 10
      # API Configuration
    api_title: str = "Email Campaign Reporting API"
    api_version: str = "1.0.0"
    api_description: str = "Python backend service for email campaign analytics"
    cors_origins: List[str] = ["*"]  # Allow all origins for development
    
    # Pagination
    default_page_size: int = 50
    max_page_size: int = 1000
    
    # Logging
    log_level: str = "INFO"
    log_format: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    class Config:
        env_file = ".env"

settings = Settings()
