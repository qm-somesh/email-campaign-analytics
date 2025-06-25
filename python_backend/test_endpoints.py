#!/usr/bin/env python3
"""
Quick test script to check if API endpoints are accessible
"""

import requests
import time
import sys

def test_endpoint(url, endpoint_name, expected_content_type="application/json"):
    """Test a single endpoint"""
    try:
        print(f"Testing {endpoint_name}...")
        response = requests.get(url, timeout=10)
        
        print(f"  Status Code: {response.status_code}")
        print(f"  Content-Type: {response.headers.get('Content-Type', 'Not specified')}")
        
        if response.status_code == 200:
            print(f"  ✅ {endpoint_name} is working!")
            if "application/json" in response.headers.get('Content-Type', ''):
                try:
                    json_data = response.json()
                    print(f"  Response: {json_data}")
                except:
                    print(f"  Response length: {len(response.text)} characters")
            else:
                print(f"  Response length: {len(response.text)} characters")
            return True
        else:
            print(f"  ❌ {endpoint_name} failed with status {response.status_code}")
            print(f"  Error: {response.text[:200]}...")
            return False
            
    except requests.exceptions.ConnectionError:
        print(f"  ❌ {endpoint_name} - Connection failed (server not running?)")
        return False
    except requests.exceptions.Timeout:
        print(f"  ❌ {endpoint_name} - Request timeout")
        return False
    except Exception as e:
        print(f"  ❌ {endpoint_name} - Error: {e}")
        return False

def main():
    base_url = "http://localhost:8000"
    
    print("=" * 60)
    print("EmailCampaignReporting Python Backend - Endpoint Test")
    print("=" * 60)
    print(f"Testing server at: {base_url}")
    print()
    
    # Test endpoints
    endpoints = [
        (f"{base_url}/", "Root endpoint"),
        (f"{base_url}/health", "Health check"),
        (f"{base_url}/swagger", "Swagger UI Documentation"),
        (f"{base_url}/docs", "FastAPI Auto Documentation"),
        (f"{base_url}/redoc", "ReDoc Documentation"),
        (f"{base_url}/openapi.json", "OpenAPI Schema")
    ]
    
    results = []
    for url, name in endpoints:
        result = test_endpoint(url, name)
        results.append((name, result))
        print()
        time.sleep(1)  # Small delay between requests
    
    # Summary
    print("=" * 60)
    print("SUMMARY:")
    print("=" * 60)
    
    working = 0
    for name, result in results:
        status = "✅ Working" if result else "❌ Failed"
        print(f"{name:<30} {status}")
        if result:
            working += 1
    
    print(f"\n{working}/{len(results)} endpoints are working")
    
    if working == 0:
        print("\n🚨 Server appears to be down or not accessible")
        print("Troubleshooting:")
        print("1. Make sure the server is running: python start.py")
        print("2. Check if port 8000 is blocked by firewall")
        print("3. Verify virtual environment is activated")
    elif working < len(results):
        print(f"\n⚠️  Some endpoints are not working")
        print("This could indicate:")
        print("1. Server is starting up (try again in a few seconds)")
        print("2. Some routes are not properly configured")
        print("3. Missing dependencies for documentation")
    else:
        print(f"\n🎉 All endpoints are working correctly!")
        print("You can now access:")
        print("• API Documentation: http://localhost:8000/swagger")
        print("• Alternative Docs: http://localhost:8000/docs")
        print("• ReDoc: http://localhost:8000/redoc")

if __name__ == "__main__":
    main()
