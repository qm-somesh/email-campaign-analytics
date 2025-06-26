"""
Comprehensive Gemini API Unit Test Suite for Email Campaign Analytics

This test suite evaluates the Gemini API's ability to accurately interpret,
process, and respond to various user queries related to email campaign analytics.
"""

import asyncio
import json
import logging
from datetime import datetime
from typing import Dict, List, Any, Optional
import requests
from dataclasses import dataclass

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('gemini_test_results.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

@dataclass
class TestResult:
    """Test result data structure"""
    category: str
    query: str
    success: bool
    total_results: int
    execution_time: float
    error_message: Optional[str] = None
    sample_results: Optional[List[Dict]] = None
    generated_sql: Optional[str] = None

class GeminiApiTester:
    """Comprehensive tester for Gemini API email campaign analytics"""
    
    def __init__(self, base_url: str = "http://localhost:8000"):
        self.base_url = base_url
        self.endpoint = f"{base_url}/api/natural-language-sql-query/sql-query"
        self.test_results: List[TestResult] = []
        
    def execute_query(self, query: str, limit: int = 10) -> TestResult:
        """Execute a single query and return test result"""
        start_time = datetime.now()
        
        try:
            response = requests.post(
                self.endpoint,
                headers={"Content-Type": "application/json"},
                json={"query": query, "limit": limit},
                timeout=30
            )
            
            execution_time = (datetime.now() - start_time).total_seconds()
            
            if response.status_code == 200:
                data = response.json()
                sample_results = data.get('items', [])[:3]  # First 3 results for validation
                
                return TestResult(
                    category="",  # Will be set by caller
                    query=query,
                    success=True,
                    total_results=data.get('total_count', 0),
                    execution_time=execution_time,
                    sample_results=sample_results
                )
            else:
                return TestResult(
                    category="",
                    query=query,
                    success=False,
                    total_results=0,
                    execution_time=execution_time,
                    error_message=f"HTTP {response.status_code}: {response.text}"
                )
                
        except Exception as e:
            execution_time = (datetime.now() - start_time).total_seconds()
            return TestResult(
                category="",
                query=query,
                success=False,
                total_results=0,
                execution_time=execution_time,
                error_message=str(e)
            )
    
    def test_category(self, category_name: str, queries: List[str]) -> List[TestResult]:
        """Test all queries in a category"""
        logger.info(f"\n{'='*60}")
        logger.info(f"Testing Category: {category_name}")
        logger.info(f"{'='*60}")
        
        category_results = []
        
        for i, query in enumerate(queries, 1):
            logger.info(f"\n[{i}/{len(queries)}] Testing: {query}")
            
            result = self.execute_query(query)
            result.category = category_name
            
            if result.success:
                logger.info(f"✅ SUCCESS - {result.total_results} results in {result.execution_time:.2f}s")
                if result.sample_results:
                    logger.info(f"Sample result: {result.sample_results[0].get('strategy_name', 'N/A')}")
            else:
                logger.error(f"❌ FAILED - {result.error_message}")
            
            category_results.append(result)
            self.test_results.append(result)
        
        return category_results
    
    def run_comprehensive_tests(self):
        """Run all test categories"""
        
        # 1. Time-Based Queries
        time_queries = [
            "Show me campaign stats for the last month",
            "What campaigns performed best this week?",
            "Give me email metrics for the past 7 days",
            "Show campaigns from the last 3 months",
            "What are the recent campaign results?",
            "Display email performance for this year",
            "Show me last week's email statistics",
            "Get campaign data from the past 30 days"
        ]
        self.test_category("Time-Based Queries", time_queries)
        
        # 2. Performance-Based Queries
        performance_queries = [
            "Campaigns with high open rates",
            "Show me the best performing campaigns",
            "Which campaigns have the highest click rates?",
            "Find campaigns with low bounce rates",
            "Top 10 campaigns by open rate",
            "Campaigns with the best delivery rates",
            "Show me campaigns with high engagement",
            "Which strategies have the lowest unsubscribe rates?",
            "Find the most successful email campaigns",
            "Campaigns with poor performance metrics"
        ]
        self.test_category("Performance-Based Queries", performance_queries)
        
        # 3. Volume-Based Queries
        volume_queries = [
            "Campaigns that sent the most emails",
            "Show me high-volume email campaigns",
            "Which strategies sent the least emails?",
            "Find campaigns with significant email volume",
            "Show me campaigns with more than 1000 emails",
            "Low-volume campaign analysis"
        ]
        self.test_category("Volume-Based Queries", volume_queries)
        
        # 4. Specific Metric Queries
        metric_queries = [
            "Show me bounce rates by campaign",
            "Campaign click-through rates",
            "Delivery success by strategy",
            "Complaint rates across campaigns",
            "Unsubscribe patterns by campaign type",
            "Open rate comparison between strategies"
        ]
        self.test_category("Specific Metric Queries", metric_queries)
        
        # 5. Combined Time + Performance Queries
        combined_queries = [
            "Campaigns with high open rates last month",
            "Best performing campaigns this quarter",
            "Low bounce rate campaigns from last week",
            "High click campaigns in the past 30 days",
            "Recent campaigns with good engagement",
            "Last month's top performing strategies"
        ]
        self.test_category("Combined Time + Performance Queries", combined_queries)
        
        # 6. Campaign Analysis Queries
        analysis_queries = [
            "Compare all active campaign strategies",
            "Show me campaign performance breakdown",
            "Email strategy effectiveness analysis",
            "Campaign ROI by strategy type",
            "Active campaign summary",
            "All campaign metrics overview"
        ]
        self.test_category("Campaign Analysis Queries", analysis_queries)
        
        # 7. Specific Campaign Type Queries
        campaign_type_queries = [
            "Maintenance reminder campaign performance",
            "Lease expiration email statistics",
            "New customer campaign results",
            "Service appointment email metrics",
            "Promotional campaign effectiveness"
        ]
        self.test_category("Specific Campaign Type Queries", campaign_type_queries)
        
        # 8. Filtering Queries
        filtering_queries = [
            "Campaigns with delivery rate above 95%",
            "Show only high-performing strategies",
            "Filter campaigns by open rate threshold",
            "Campaigns with minimal complaints",
            "Exclude low-volume campaigns",
            "Campaigns with click rates less than 5%",
            "Campaigns with open rates less than 50%",
            "Campaigns with bounce rates greater than 3%"
        ]
        self.test_category("Filtering Queries", filtering_queries)
        
        # 9. Sorting Queries
        sorting_queries = [
            "Sort campaigns by total emails sent",
            "Order by click rate descending",
            "Rank campaigns by engagement",
            "Sort by delivery success rate",
            "Order campaigns alphabetically"
        ]
        self.test_category("Sorting Queries", sorting_queries)
        
        # 10. Problem Identification Queries
        problem_queries = [
            "Find campaigns with high bounce rates",
            "Show me underperforming strategies",
            "Campaigns with delivery issues",
            "Identify problematic email campaigns",
            "Find campaigns that need improvement"
        ]
        self.test_category("Problem Identification Queries", problem_queries)
    
    def generate_comprehensive_report(self):
        """Generate detailed test report"""
        total_tests = len(self.test_results)
        successful_tests = sum(1 for r in self.test_results if r.success)
        failed_tests = total_tests - successful_tests
        
        # Group results by category
        category_stats = {}
        for result in self.test_results:
            if result.category not in category_stats:
                category_stats[result.category] = {'total': 0, 'success': 0, 'failed': 0}
            
            category_stats[result.category]['total'] += 1
            if result.success:
                category_stats[result.category]['success'] += 1
            else:
                category_stats[result.category]['failed'] += 1
        
        # Generate report
        report = f"""
{'='*80}
COMPREHENSIVE GEMINI API TEST REPORT
{'='*80}

OVERALL RESULTS:
- Total Tests: {total_tests}
- Successful: {successful_tests} ({successful_tests/total_tests*100:.1f}%)
- Failed: {failed_tests} ({failed_tests/total_tests*100:.1f}%)

CATEGORY BREAKDOWN:
"""
        
        for category, stats in category_stats.items():
            success_rate = stats['success'] / stats['total'] * 100
            report += f"\n{category}:"
            report += f"\n  ✅ Success: {stats['success']}/{stats['total']} ({success_rate:.1f}%)"
            if stats['failed'] > 0:
                report += f"\n  ❌ Failed: {stats['failed']}"
        
        # Failed queries analysis
        if failed_tests > 0:
            report += f"\n\nFAILED QUERIES ANALYSIS:\n"
            for result in self.test_results:
                if not result.success:
                    report += f"\nCategory: {result.category}"
                    report += f"\nQuery: {result.query}"
                    report += f"Error: {result.error_message}\n"
        
        # Performance analysis
        avg_time = sum(r.execution_time for r in self.test_results if r.success) / successful_tests if successful_tests > 0 else 0
        report += f"\n\nPERFORMANCE ANALYSIS:"
        report += f"\n- Average execution time: {avg_time:.2f}s"
        
        # Zero results analysis
        zero_result_queries = [r for r in self.test_results if r.success and r.total_results == 0]
        if zero_result_queries:
            report += f"\n\nQUERIES RETURNING ZERO RESULTS ({len(zero_result_queries)}):"
            for result in zero_result_queries:
                report += f"\n- {result.category}: {result.query}"
        
        report += f"\n\n{'='*80}\n"
        
        logger.info(report)
        
        # Save detailed results to JSON
        with open('gemini_test_results.json', 'w') as f:
            json.dump([{
                'category': r.category,
                'query': r.query,
                'success': r.success,
                'total_results': r.total_results,
                'execution_time': r.execution_time,
                'error_message': r.error_message,
                'sample_results': r.sample_results
            } for r in self.test_results], f, indent=2)
        
        return report

def main():
    """Main test execution"""
    logger.info("Starting Comprehensive Gemini API Test Suite")
    
    tester = GeminiApiTester()
    
    # Check if API is accessible
    try:
        response = requests.get(f"{tester.base_url}/docs")
        if response.status_code != 200:
            logger.error("API not accessible. Please ensure the server is running.")
            return
    except Exception as e:
        logger.error(f"Cannot connect to API: {e}")
        return
    
    # Run comprehensive tests
    tester.run_comprehensive_tests()
    
    # Generate and display report
    report = tester.generate_comprehensive_report()
    
    logger.info("Test suite completed. Results saved to:")
    logger.info("- gemini_test_results.log (detailed log)")
    logger.info("- gemini_test_results.json (structured data)")

if __name__ == "__main__":
    main()
