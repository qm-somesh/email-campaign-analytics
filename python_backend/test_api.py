#!/usr/bin/env python3
"""
Test script for EmailCampaignReporting Python Backend API
"""

import requests
import json
import time
import sys

def test_api():
    """Test the API endpoints"""
    
    base_url = "http://localhost:8000"
    
    print("🧪 Testing EmailCampaignReporting Python Backend API")
    print("=" * 60)
    
    # Test 1: Health Check
    print("\n1. Testing Health Check...")
    try:
        response = requests.get(f"{base_url}/health", timeout=5)
        if response.status_code == 200:
            print("✅ Health check passed")
            print(f"   Response: {response.json()}")
        else:
            print(f"❌ Health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Health check failed: {e}")
        return False
    
    # Test 2: API Documentation
    print("\n2. Testing API Documentation...")
    try:
        response = requests.get(f"{base_url}/swagger", timeout=5)
        if response.status_code == 200:
            print("✅ API documentation accessible")
        else:
            print(f"⚠️  API documentation returned: {response.status_code}")
    except Exception as e:
        print(f"⚠️  API documentation error: {e}")
    
    # Test 3: Debug Context Endpoint
    print("\n3. Testing Debug Context Endpoint...")
    try:
        test_payload = {
            "query": "show me problematic campaigns",
            "max_items": 5
        }
        
        response = requests.post(
            f"{base_url}/api/natural-language-sql-query/debug-context",
            json=test_payload,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if response.status_code == 200:
            print("✅ Debug context endpoint working")
            result = response.json()
            print(f"   Query: {result.get('query', 'N/A')}")
            print(f"   Context items: {result.get('context_count', 0)}")
            if result.get('context'):
                print("   Sample context:")
                for i, item in enumerate(result['context'][:2]):
                    print(f"     {i+1}. {item[:80]}...")
        else:
            print(f"❌ Debug context failed: {response.status_code}")
            print(f"   Response: {response.text}")
            return False
    except Exception as e:
        print(f"❌ Debug context failed: {e}")
        return False
    
    # Test 4: SQL Query Endpoint (will likely fail without database, but should return proper error)
    print("\n4. Testing SQL Query Endpoint...")
    try:
        test_payload = {
            "query": "show me all campaigns",
            "page_number": 1,
            "page_size": 10
        }
        
        response = requests.post(
            f"{base_url}/api/natural-language-sql-query/sql-query",
            json=test_payload,
            headers={"Content-Type": "application/json"},
            timeout=15
        )
        
        if response.status_code == 200:
            print("✅ SQL query endpoint working")
            result = response.json()
            print(f"   Total items: {result.get('total_count', 'N/A')}")
        elif response.status_code in [500, 503]:
            print("⚠️  SQL query endpoint reachable but failed (expected without database)")
            print(f"   Status: {response.status_code}")
            error_info = response.json() if response.headers.get('content-type', '').startswith('application/json') else response.text
            print(f"   Error: {error_info}")
        else:
            print(f"❌ SQL query unexpected status: {response.status_code}")
            return False
    except Exception as e:
        print(f"⚠️  SQL query failed (expected without database): {e}")
    
    print("\n" + "=" * 60)
    print("🎉 API accessibility test completed!")
    print("\n📋 Next steps:")
    print("   1. API is accessible at: http://localhost:8000")
    print("   2. Documentation at: http://localhost:8000/swagger")
    print("   3. Configure database connection in .env file")
    print("   4. Add Gemini API key to .env file for full functionality")
    
    return True

def wait_for_server(max_wait=30):
    """Wait for the server to start"""
    print(f"⏳ Waiting for server to start (max {max_wait}s)...")
    
    for i in range(max_wait):
        try:
            response = requests.get("http://localhost:8000/health", timeout=2)
            if response.status_code == 200:
                print("✅ Server is ready!")
                return True
        except:
            pass
        
        print(f"   Waiting... ({i+1}/{max_wait})")
        time.sleep(1)
    
    print("❌ Server didn't start in time")
    return False

if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--wait":
        if wait_for_server():
            test_api()
    else:
        test_api()
