from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import logging
import uvicorn
from .config.settings import settings
from .controllers.natural_language_sql_controller import router as nl_sql_router
from .utils.exceptions import EmailCampaignReportingException

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format=settings.log_format
)

logger = logging.getLogger(__name__)

# Create FastAPI application
app = FastAPI(
    title=settings.api_title,
    version=settings.api_version,
    description=settings.api_description,
    docs_url="/swagger",
    redoc_url="/redoc"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"] if settings.cors_origins == ["*"] else settings.cors_origins,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allow_headers=["*"],
)

# Global exception handler
@app.exception_handler(EmailCampaignReportingException)
async def custom_exception_handler(request: Request, exc: EmailCampaignReportingException):
    return JSONResponse(
        status_code=400,
        content={"error": str(exc), "type": exc.__class__.__name__}
    )

# Include routers
app.include_router(nl_sql_router)

# Health check endpoint
@app.get("/health")
async def health_check():
    return {"status": "healthy", "service": "EmailCampaignReporting Python Backend"}

# Root endpoint
@app.get("/")
async def root():
    return {
        "service": "EmailCampaignReporting Python Backend",
        "version": settings.api_version,
        "docs": "/swagger"
    }

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level=settings.log_level.lower()
    )
