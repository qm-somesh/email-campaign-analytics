"""
Focused Validation Tests for Known Issues and Edge Cases

This script tests specific scenarios that are likely to reveal issues
with the current Gemini API implementation based on the orchestrator prompt analysis.
"""

import requests
import json
import time
from datetime import datetime
from typing import Dict, List, Any

class FocusedGeminiValidator:
    """Focused validator for specific Gemini API scenarios"""
    
    def __init__(self, base_url: str = "http://localhost:8000"):
        self.base_url = base_url
        self.endpoint = f"{base_url}/api/natural-language-sql-query/sql-query"
        self.results = []
    
    def test_query(self, query: str, expected_behavior: str, limit: int = 5) -> Dict:
        """Test a specific query with expected behavior"""
        print(f"\n🔍 Testing: {query}")
        print(f"Expected: {expected_behavior}")
        
        start_time = time.time()
        
        try:
            response = requests.post(
                self.endpoint,
                headers={"Content-Type": "application/json"},
                json={"query": query, "limit": limit},
                timeout=30
            )
            
            execution_time = time.time() - start_time
            
            if response.status_code == 200:
                data = response.json()
                total_count = data.get('total_count', 0)
                items = data.get('items', [])
                
                print(f"✅ SUCCESS: {total_count} results in {execution_time:.2f}s")
                
                if items:
                    # Show first result details
                    first_result = items[0]
                    print(f"   📊 Sample: {first_result.get('strategy_name', 'N/A')}")
                    print(f"   📈 Metrics: CR={first_result.get('click_rate', 0):.1f}%, OR={first_result.get('open_rate', 0):.1f}%, DR={first_result.get('delivery_rate', 0):.1f}%")
                
                return {
                    'query': query,
                    'expected': expected_behavior,
                    'success': True,
                    'total_count': total_count,
                    'execution_time': execution_time,
                    'sample_results': items[:2]
                }
            else:
                print(f"❌ FAILED: HTTP {response.status_code}")
                print(f"   Error: {response.text}")
                return {
                    'query': query,
                    'expected': expected_behavior,
                    'success': False,
                    'error': f"HTTP {response.status_code}: {response.text}",
                    'execution_time': execution_time
                }
                
        except Exception as e:
            execution_time = time.time() - start_time
            print(f"❌ EXCEPTION: {str(e)}")
            return {
                'query': query,
                'expected': expected_behavior,
                'success': False,
                'error': str(e),
                'execution_time': execution_time
            }
    
    def run_focused_tests(self):
        """Run focused tests on known challenging areas"""
        
        print("="*80)
        print("FOCUSED GEMINI API VALIDATION TESTS")
        print("="*80)
        
        # 1. Test percentage-based filtering (recently fixed)
        print("\n" + "="*50)
        print("1. PERCENTAGE-BASED FILTERING TESTS")
        print("="*50)
        
        percentage_tests = [
            ("Which campaigns have click rates less than 5%?", "Should return campaigns with 0-4.99% click rates"),
            ("Show me campaigns with click rates less than 10%", "Should return campaigns with 0-9.99% click rates"),
            ("Which campaigns have open rates less than 50%?", "Should return campaigns with 0-49.99% open rates"),
            ("Find campaigns with bounce rates greater than 3%", "Should return campaigns with >3% bounce rates"),
            ("Show campaigns with delivery rates less than 95%", "Should return campaigns with <95% delivery rates"),
        ]
        
        for query, expected in percentage_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
        
        # 2. Test time-based queries (potential issues with date filtering)
        print("\n" + "="*50)
        print("2. TIME-BASED QUERY TESTS")
        print("="*50)
        
        time_tests = [
            ("Show me recent campaigns", "Should return campaigns with recent activity"),
            ("What campaigns were active last month?", "Should return campaigns from last month"),
            ("Show me campaigns from the past 7 days", "Should return campaigns from last week"),
            ("Give me email metrics for the past 30 days", "Should return 30-day campaign data"),
        ]
        
        for query, expected in time_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
        
        # 3. Test high-performance queries (should exclude poor performers)
        print("\n" + "="*50)
        print("3. HIGH-PERFORMANCE FILTERING TESTS")
        print("="*50)
        
        performance_tests = [
            ("Which campaigns have the highest click rates?", "Should return only campaigns with actual clicks, ordered by click rate DESC"),
            ("Show me the best performing campaigns", "Should exclude campaigns with no deliveries/opens"),
            ("Find campaigns with highest open rates", "Should return only campaigns with actual opens, ordered by open rate DESC"),
            ("Show me most successful campaigns", "Should exclude poor performers"),
        ]
        
        for query, expected in performance_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
        
        # 4. Test problem identification queries
        print("\n" + "="*50)
        print("4. PROBLEM IDENTIFICATION TESTS")
        print("="*50)
        
        problem_tests = [
            ("Show me underperforming campaigns", "Should return campaigns with low metrics"),
            ("Find problematic email campaigns", "Should return campaigns with issues"),
            ("Which campaigns have delivery problems?", "Should return campaigns with low delivery rates"),
            ("Show me campaigns that need improvement", "Should return poor performers"),
        ]
        
        for query, expected in problem_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
        
        # 5. Test volume-based queries
        print("\n" + "="*50)
        print("5. VOLUME-BASED QUERY TESTS")
        print("="*50)
        
        volume_tests = [
            ("Show me high-volume campaigns", "Should return campaigns with many emails sent"),
            ("Which campaigns sent more than 1000 emails?", "Should return campaigns with >1000 total emails"),
            ("Find low-volume campaigns", "Should return campaigns with few emails sent"),
        ]
        
        for query, expected in volume_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
        
        # 6. Test edge cases and specific campaign names
        print("\n" + "="*50)
        print("6. EDGE CASE AND SPECIFIC TESTS")
        print("="*50)
        
        edge_tests = [
            ("Show me Lease Expiration campaign performance", "Should return specific campaign data"),
            ("How did Service Appointment campaigns perform?", "Should return service-related campaigns"),
            ("Compare maintenance vs promotional campaigns", "Should return relevant campaign types"),
            ("Show me all campaigns", "Should return all active campaigns"),
        ]
        
        for query, expected in edge_tests:
            result = self.test_query(query, expected)
            self.results.append(result)
    
    def analyze_results(self):
        """Analyze test results and identify issues"""
        
        print("\n" + "="*80)
        print("FOCUSED TEST RESULTS ANALYSIS")
        print("="*80)
        
        total_tests = len(self.results)
        successful_tests = sum(1 for r in self.results if r['success'])
        failed_tests = total_tests - successful_tests
        
        print(f"\n📊 OVERALL RESULTS:")
        print(f"   Total Tests: {total_tests}")
        print(f"   Successful: {successful_tests} ({successful_tests/total_tests*100:.1f}%)")
        print(f"   Failed: {failed_tests} ({failed_tests/total_tests*100:.1f}%)")
        
        # Analyze zero-result queries
        zero_results = [r for r in self.results if r['success'] and r.get('total_count', 0) == 0]
        if zero_results:
            print(f"\n⚠️  QUERIES RETURNING ZERO RESULTS ({len(zero_results)}):")
            for result in zero_results:
                print(f"   - {result['query']}")
                print(f"     Expected: {result['expected']}")
        
        # Analyze failed queries
        if failed_tests > 0:
            print(f"\n❌ FAILED QUERIES ({failed_tests}):")
            for result in self.results:
                if not result['success']:
                    print(f"   - {result['query']}")
                    print(f"     Error: {result.get('error', 'Unknown error')}")
        
        # Performance analysis
        successful_results = [r for r in self.results if r['success']]
        if successful_results:
            avg_time = sum(r['execution_time'] for r in successful_results) / len(successful_results)
            max_time = max(r['execution_time'] for r in successful_results)
            print(f"\n⏱️  PERFORMANCE METRICS:")
            print(f"   Average execution time: {avg_time:.2f}s")
            print(f"   Maximum execution time: {max_time:.2f}s")
        
        # Save detailed results
        with open('focused_validation_results.json', 'w') as f:
            json.dump(self.results, f, indent=2)
        
        print(f"\n💾 Results saved to: focused_validation_results.json")
        
        # Recommendations
        print(f"\n🔧 RECOMMENDATIONS:")
        
        if zero_results:
            print("   1. Investigate zero-result queries - may indicate prompt issues")
        
        if failed_tests > 0:
            print("   2. Fix failed queries - check API connectivity and error handling")
        
        if any(r.get('total_count', 0) > 0 for r in successful_results):
            print("   3. Validate result accuracy - ensure returned data matches expectations")
        
        print("   4. Consider adding more specific examples to orchestrator prompt")
        print("   5. Test with different percentage thresholds and date ranges")

def main():
    """Main execution"""
    print("Starting Focused Gemini API Validation...")
    
    validator = FocusedGeminiValidator()
    
    # Check API connectivity
    try:
        response = requests.get(f"{validator.base_url}/health", timeout=5)
        if response.status_code != 200:
            print("❌ API not accessible. Please ensure the server is running.")
            return
    except Exception as e:
        print(f"❌ Cannot connect to API: {e}")
        print("Attempting to continue with tests anyway...")
        # Don't return, continue with tests
    
    print("✅ API is accessible")
    
    # Run focused tests
    validator.run_focused_tests()
    
    # Analyze results
    validator.analyze_results()
    
    print("\n🎯 Focused validation complete!")

if __name__ == "__main__":
    main()
