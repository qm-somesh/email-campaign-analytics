#!/usr/bin/env python3
"""
Startup script for EmailCampaignReporting Python Backend
"""

import sys
import os
import uvicorn
import logging

# Add the current directory to Python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def setup_logging(debug_mode=False):
    """Set up logging configuration"""
    log_level = logging.DEBUG if debug_mode else logging.INFO
    log_format = '%(asctime)s - %(name)s - %(levelname)s - %(filename)s:%(lineno)d - %(message)s' if debug_mode else '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    
    logging.basicConfig(
        level=log_level,
        format=log_format
    )
    
    return logging.getLogger(__name__)

def check_debug_mode():
    """Check if debug mode should be enabled"""
    # Check environment variable
    debug_env = os.getenv('DEBUG', '').lower() in ('true', '1', 'yes')
    
    # Check command line argument
    debug_arg = '--debug' in sys.argv
    
    return debug_env or debug_arg

def validate_environment(logger):
    """Validate that the environment is set up correctly"""
    issues = []
    
    # Check if required files exist
    required_files = ['app/main.py', 'app/__init__.py']
    for file_path in required_files:
        if not os.path.exists(file_path):
            issues.append(f"Missing required file: {file_path}")
    
    # Test critical imports
    try:
        from app.main import app
        logger.info("✅ Main application imported successfully")
    except ImportError as e:
        issues.append(f"Cannot import main app: {e}")
        logger.error(f"❌ Import error: {e}")
    except Exception as e:
        issues.append(f"Error importing main app: {e}")
        logger.error(f"❌ Unexpected error: {e}")
    
    return issues

def main():
    """Start the FastAPI application"""
    
    # Check for debug mode
    debug_mode = check_debug_mode()
    
    # Set up logging
    logger = setup_logging(debug_mode)
    
    if debug_mode:
        logger.info("🐛 DEBUG MODE ENABLED")
        logger.info("=" * 50)
    
    logger.info("Starting EmailCampaignReporting Python Backend...")
    
    # Validate environment
    if debug_mode:
        logger.info("🔍 Validating environment...")
        issues = validate_environment(logger)
        if issues:
            logger.warning("⚠️ Environment issues found:")
            for issue in issues:
                logger.warning(f"  - {issue}")
            
            if '--ignore-validation' not in sys.argv:
                logger.error("❌ Stopping due to validation errors. Use --ignore-validation to continue anyway.")
                sys.exit(1)
        else:
            logger.info("✅ Environment validation passed")
    
    # Check if .env file exists
    env_file = os.path.join(os.path.dirname(__file__), '.env')
    if not os.path.exists(env_file):
        logger.warning(f".env file not found at {env_file}")
        logger.info("Creating .env file from template...")
        
        # Create .env from template
        env_example = os.path.join(os.path.dirname(__file__), '.env.example')
        if os.path.exists(env_example):
            with open(env_example, 'r') as src, open(env_file, 'w') as dst:
                dst.write(src.read())
            logger.info(f"Created .env file. Please edit {env_file} with your settings.")
        else:
            logger.warning("No .env.example found. Creating minimal .env file...")
            with open(env_file, 'w') as f:
                f.write("""# Database Configuration
DATABASE_URL=mssql+pyodbc://server/database?driver=ODBC+Driver+17+for+SQL+Server

# Gemini AI Configuration (optional for testing)
GEMINI_API_KEY=

# API Configuration
LOG_LEVEL=INFO
""")
    
    try:
        # Configure server based on debug mode
        server_config = {
            "app": "app.main:app",
            "host": "0.0.0.0",
            "port": 8000,
            "reload": True,
            "log_level": "debug" if debug_mode else "info"
        }
        
        if debug_mode:
            logger.info("🚀 Starting server in DEBUG mode...")
            logger.info(f"   📊 Configuration: {server_config}")
            logger.info("   🌐 Server will be available at:")
            logger.info("      - http://localhost:8000")
            logger.info("      - http://localhost:8000/swagger (API docs)")
            logger.info("      - http://localhost:8000/health (health check)")
        else:
            logger.info("🚀 Starting server...")
        
        # Start the server
        uvicorn.run(**server_config)
        
    except KeyboardInterrupt:
        logger.info("👋 Server stopped by user (Ctrl+C)")
    except Exception as e:
        logger.error(f"❌ Failed to start server: {e}")
        
        if debug_mode:
            logger.error("🔍 Full error details:")
            import traceback
            logger.error(traceback.format_exc())
            
            logger.info("💡 Debugging suggestions:")
            logger.info("  1. Check if port 8000 is already in use")
            logger.info("  2. Verify all dependencies are installed: pip install -r requirements.txt")
            logger.info("  3. Check your .env configuration")
            logger.info("  4. Run debug tools: python debug_tools.py")
        
        sys.exit(1)

if __name__ == "__main__":
    main()
