#!/usr/bin/env python3
"""
Debug startup script for EmailCampaignReporting Python Backend
This version runs WITHOUT reload for proper breakpoint debugging
"""

import sys
import os
import uvicorn
import logging

# Add the current directory to Python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def setup_debug_logging():
    """Set up enhanced debug logging"""
    logging.basicConfig(
        level=logging.DEBUG,
        format='%(asctime)s - %(name)s - %(levelname)s - %(filename)s:%(lineno)d - %(message)s'
    )
    
    # Enable debug logging for specific modules
    logging.getLogger("app").setLevel(logging.DEBUG)
    logging.getLogger("uvicorn").setLevel(logging.DEBUG)
    
    return logging.getLogger(__name__)

def main():
    """Main entry point for debug server"""
    logger = setup_debug_logging()
    
    print("🐛 Starting FastAPI in DEBUG MODE (No Reload)")
    print("=" * 60)
    print("✅ Breakpoints will work in VS Code")
    print("⚠️  Manual restart required for code changes")
    print("🌐 Server will be available at: http://localhost:8000")
    print("📚 Swagger UI at: http://localhost:8000/docs")
    print("=" * 60)
    
    # Check if we can import the app
    try:
        from app.main import app
        logger.info("✅ FastAPI app imported successfully")
    except Exception as e:
        logger.error(f"❌ Failed to import FastAPI app: {e}")
        return
    
    # Start server WITHOUT reload for debugging
    try:
        uvicorn.run(
            "app.main:app",
            host="0.0.0.0",
            port=8000,
            reload=False,  # 🔑 This is the key - no reload for debugging
            log_level="debug",
            debug=True,
            access_log=True
        )
    except KeyboardInterrupt:
        logger.info("🛑 Server stopped by user")
    except Exception as e:
        logger.error(f"❌ Server error: {e}")

if __name__ == "__main__":
    main()
