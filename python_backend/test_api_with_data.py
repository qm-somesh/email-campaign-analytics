#!/usr/bin/env python3
"""
Test script to validate the email campaign API with test data
"""

import asyncio
import requests
import json
import time
import sys
import os

# Add the current directory to Python path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def test_api_endpoint():
    """Test the API endpoint with various queries"""
    base_url = "http://localhost:8000"
    
    print("🧪 Testing Email Campaign API with Test Data")
    print("=" * 60)
    
    # Test data queries
    test_queries = [
        {
            "name": "Recent Campaigns",
            "query": "Show campaigns from the last 3 months",
            "page_number": 1,
            "page_size": 10
        },
        {
            "name": "Best Performing Campaigns",
            "query": "Show me the best performing campaigns",
            "page_number": 1,
            "page_size": 5
        },
        {
            "name": "Problematic Campaigns",
            "query": "Find problematic campaigns that need attention",
            "page_number": 1,
            "page_size": 10
        },
        {
            "name": "High Open Rate Campaigns",
            "query": "Show campaigns with high open rates",
            "page_number": 1,
            "page_size": 5
        }
    ]
    
    # Test each query
    success_count = 0
    for i, test_case in enumerate(test_queries, 1):
        print(f"\n{i}. Testing: {test_case['name']}")
        print(f"   Query: {test_case['query']}")
        
        try:
            response = requests.post(
                f"{base_url}/api/natural-language-sql-query/sql-query",
                json=test_case,
                headers={"Content-Type": "application/json"},
                timeout=30
            )
            
            print(f"   Status Code: {response.status_code}")
            
            if response.status_code == 200:
                data = response.json()
                print(f"   ✅ Success!")
                print(f"   Response type: {type(data)}")
                if isinstance(data, dict):
                    if "items" in data:
                        print(f"   Items returned: {len(data.get('items', []))}")
                    if "total_count" in data:
                        print(f"   Total count: {data.get('total_count', 0)}")
                    if "generated_sql" in data:
                        sql = data.get('generated_sql', '')
                        print(f"   Generated SQL: {sql[:100]}..." if len(sql) > 100 else f"   Generated SQL: {sql}")
                success_count += 1
            else:
                print(f"   ❌ Failed with status {response.status_code}")
                print(f"   Error: {response.text[:200]}...")
                
        except requests.exceptions.ConnectionError:
            print(f"   ❌ Connection failed - server not running?")
        except requests.exceptions.Timeout:
            print(f"   ❌ Request timeout")
        except Exception as e:
            print(f"   ❌ Error: {e}")
        
        time.sleep(1)  # Small delay between requests
    
    print(f"\n" + "=" * 60)
    print(f"SUMMARY: {success_count}/{len(test_queries)} tests passed")
    
    if success_count == len(test_queries):
        print("🎉 All API tests passed!")
    elif success_count > 0:
        print("⚠️  Some tests passed, check errors above")
    else:
        print("❌ All tests failed - check server and configuration")

def test_health_endpoint():
    """Test the health endpoint first"""
    try:
        response = requests.get("http://localhost:8000/health", timeout=5)
        if response.status_code == 200:
            print("✅ Health check passed")
            return True
        else:
            print(f"❌ Health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Health check error: {e}")
        return False

def test_swagger_endpoint():
    """Test if Swagger is accessible"""
    try:
        response = requests.get("http://localhost:8000/swagger", timeout=5)
        if response.status_code == 200:
            print("✅ Swagger UI accessible")
            return True
        else:
            print(f"❌ Swagger failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Swagger error: {e}")
        return False

def test_cors_with_options():
    """Test CORS preflight request"""
    try:
        response = requests.options(
            "http://localhost:8000/api/natural-language-sql-query/sql-query",
            headers={
                "Origin": "http://localhost:8000",
                "Access-Control-Request-Method": "POST",
                "Access-Control-Request-Headers": "Content-Type"
            },
            timeout=5
        )
        print(f"CORS preflight status: {response.status_code}")
        cors_headers = {k: v for k, v in response.headers.items() if 'cors' in k.lower() or 'access-control' in k.lower()}
        if cors_headers:
            print("CORS headers:")
            for header, value in cors_headers.items():
                print(f"  {header}: {value}")
        return response.status_code in [200, 204]
    except Exception as e:
        print(f"❌ CORS test error: {e}")
        return False

def main():
    print("📡 Testing Server Connectivity and API Endpoints")
    print("=" * 60)
    
    # Test basic connectivity
    print("1. Testing Health Endpoint...")
    health_ok = test_health_endpoint()
    
    print("\n2. Testing Swagger Endpoint...")
    swagger_ok = test_swagger_endpoint()
    
    print("\n3. Testing CORS Configuration...")
    cors_ok = test_cors_with_options()
    
    if not health_ok:
        print("\n❌ Server is not responding. Please start the server first:")
        print("   python start.py")
        return
    
    if health_ok and swagger_ok:
        print("\n4. Testing API Endpoints with Test Data...")
        test_api_endpoint()
    else:
        print("\n⚠️  Basic endpoints not fully working, skipping API tests")
    
    print(f"\n" + "=" * 60)
    print("🔧 Troubleshooting Tips:")
    print("1. Make sure server is running: python start.py")
    print("2. Check CORS settings in app/config/settings.py")
    print("3. Verify virtual environment is activated")
    print("4. Try the curl command from command line instead of Swagger UI")

if __name__ == "__main__":
    main()
