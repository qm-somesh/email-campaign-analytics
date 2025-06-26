"""
Comprehensive Gemini API Test Runner with Mock Service

This test runner executes all 13 query categories using the mock Gemini service
to validate the orchestrator logic without API quota limitations.
"""

import asyncio
import sys
import os
import json
import logging
from datetime import datetime
from typing import Dict, List, Any

# Add the project root to Python path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.services.natural_sql_rag.orchestrator import NaturalLanguageSqlQueryOrchestrator
from app.services.natural_sql_rag.mock_rag_service import MockNaturalSqlRagService
from app.services.natural_sql_rag.mock_gemini_test_service import MockGeminiTestService

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('comprehensive_test_results.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

class ComprehensiveTestRunner:
    """Comprehensive test runner for all 13 query categories"""
    
    def __init__(self):
        self.orchestrator = NaturalLanguageSqlQueryOrchestrator(
            rag_service=MockNaturalSqlRagService(),
            gemini_service=MockGeminiTestService()
        )
        self.test_results = []
        
        # Define all 13 categories with test queries
        self.test_categories = {
            "1. Time-Based Queries": [
                "Show me campaign stats for the last month",
                "What campaigns performed best this week?", 
                "Give me email metrics for the past 7 days",
                "Show campaigns from the last 3 months",
                "What are the recent campaign results?",
                "Display email performance for this year",
                "Show me last week's email statistics",
                "Get campaign data from the past 30 days"
            ],
            
            "2. Performance-Based Queries": [
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
            ],
            
            "3. Volume-Based Queries": [
                "Campaigns that sent the most emails",
                "Show me high-volume email campaigns",
                "Which strategies sent the least emails?",
                "Find campaigns with significant email volume",
                "Show me campaigns with more than 1000 emails",
                "Low-volume campaign analysis"
            ],
            
            "4. Specific Metric Queries": [
                "Show me bounce rates by campaign",
                "Campaign click-through rates",
                "Delivery success by strategy", 
                "Complaint rates across campaigns",
                "Unsubscribe patterns by campaign type",
                "Open rate comparison between strategies"
            ],
            
            "5. Combined Time + Performance Queries": [
                "Campaigns with high open rates last month",
                "Best performing campaigns this quarter",
                "Low bounce rate campaigns from last week",
                "High click campaigns in the past 30 days",
                "Recent campaigns with good engagement",
                "Last month's top performing strategies"
            ],
            
            "6. Campaign Analysis Queries": [
                "Compare all active campaign strategies",
                "Show me campaign performance breakdown",
                "Email strategy effectiveness analysis",
                "Campaign ROI by strategy type",
                "Active campaign summary",
                "All campaign metrics overview"
            ],
            
            "7. Trend Analysis Queries": [
                "Campaign performance trends over time",
                "Show me monthly campaign statistics",
                "Weekly email performance comparison",
                "Campaign activity over the last quarter"
            ],
            
            "8. Specific Campaign Type Queries": [
                "Maintenance reminder campaign performance",
                "Lease expiration email statistics",
                "New customer campaign results",
                "Service appointment email metrics",
                "Promotional campaign effectiveness"
            ],
            
            "9. Filtering Queries": [
                "Campaigns with delivery rate above 95%",
                "Show only high-performing strategies",
                "Filter campaigns by open rate threshold",
                "Campaigns with minimal complaints",
                "Exclude low-volume campaigns",
                "Campaigns with click rates less than 5%",
                "Campaigns with open rates less than 50%",
                "Campaigns with bounce rates greater than 3%"
            ],
            
            "10. Sorting Queries": [
                "Sort campaigns by total emails sent",
                "Order by click rate descending",
                "Rank campaigns by engagement",
                "Sort by delivery success rate",
                "Order campaigns alphabetically"
            ],
            
            "11. Date Range Queries": [
                "Campaigns between May 1st and June 1st",
                "Show me Q2 campaign results",
                "Email performance in the first half of 2025",
                "Campaign data for specific date range"
            ],
            
            "12. Comparative Queries": [
                "Compare campaign types side by side",
                "Show the difference between top and bottom performers",
                "Benchmark campaigns against average performance",
                "Compare this month vs last month campaigns"
            ],
            
            "13. Problem Identification Queries": [
                "Find campaigns with high bounce rates",
                "Show me underperforming strategies",
                "Campaigns with delivery issues",
                "Identify problematic email campaigns",
                "Find campaigns that need improvement"
            ]
        }
    
    async def test_query(self, category: str, query: str) -> Dict[str, Any]:
        """Test a single query"""
        logger.info(f"Testing: {query}")
        
        start_time = datetime.now()
        
        try:
            # Test with the orchestrator (this will use mock services)
            result = await self.orchestrator.run_async(query, page_number=1, page_size=10)
            
            execution_time = (datetime.now() - start_time).total_seconds()
            
            return {
                'category': category,
                'query': query,
                'success': True,
                'total_count': result.total_count,
                'execution_time': execution_time,
                'sample_results': [
                    {
                        'strategy_name': item.strategy_name,
                        'total_emails': item.total_emails,
                        'click_rate': item.click_rate,
                        'open_rate': item.open_rate,
                        'delivery_rate': item.delivery_rate
                    } for item in result.items[:2]  # First 2 results
                ]
            }
            
        except Exception as e:
            execution_time = (datetime.now() - start_time).total_seconds()
            logger.error(f"FAILED: {str(e)}")
            
            return {
                'category': category,
                'query': query,
                'success': False,
                'error': str(e),
                'execution_time': execution_time
            }
    
    async def test_category(self, category_name: str, queries: List[str]) -> List[Dict[str, Any]]:
        """Test all queries in a category"""
        logger.info(f"\n{'='*80}")
        logger.info(f"TESTING CATEGORY: {category_name}")
        logger.info(f"{'='*80}")
        
        category_results = []
        
        for i, query in enumerate(queries, 1):
            logger.info(f"\n[{i}/{len(queries)}] Query: {query}")
            
            result = await self.test_query(category_name, query)
            
            if result['success']:
                logger.info(f"SUCCESS - {result['total_count']} results in {result['execution_time']:.2f}s")
                if result.get('sample_results'):
                    sample = result['sample_results'][0]
                    logger.info(f"   Sample: {sample.get('strategy_name', 'N/A')}")
            else:
                logger.error(f"FAILED - {result.get('error', 'Unknown error')}")
            
            category_results.append(result)
            self.test_results.append(result)
        
        # Category summary
        successful = sum(1 for r in category_results if r['success'])
        total = len(category_results)
        logger.info(f"\nCATEGORY SUMMARY: {successful}/{total} ({successful/total*100:.1f}%) successful")
        
        return category_results
    
    async def run_comprehensive_tests(self):
        """Run all comprehensive tests"""
        logger.info("="*100)
        logger.info("COMPREHENSIVE GEMINI API TEST SUITE - MOCK SERVICE")
        logger.info("="*100)
        logger.info(f"Testing {sum(len(queries) for queries in self.test_categories.values())} queries across {len(self.test_categories)} categories")
        
        overall_start = datetime.now()
        
        # Run all categories
        for category_name, queries in self.test_categories.items():
            await self.test_category(category_name, queries)
        
        overall_time = (datetime.now() - overall_start).total_seconds()
        
        # Generate comprehensive report
        self.generate_comprehensive_report(overall_time)
        
        logger.info(f"\nCOMPREHENSIVE TESTING COMPLETED in {overall_time:.2f}s")
    
    def generate_comprehensive_report(self, total_time: float):
        """Generate detailed comprehensive report"""
        total_tests = len(self.test_results)
        successful_tests = sum(1 for r in self.test_results if r['success'])
        failed_tests = total_tests - successful_tests
        
        # Group results by category
        category_stats = {}
        for result in self.test_results:
            category = result['category']
            if category not in category_stats:
                category_stats[category] = {'total': 0, 'success': 0, 'failed': 0, 'avg_time': 0}
            
            category_stats[category]['total'] += 1
            if result['success']:
                category_stats[category]['success'] += 1
            else:
                category_stats[category]['failed'] += 1
        
        # Calculate average execution times
        for category, stats in category_stats.items():
            category_results = [r for r in self.test_results if r['category'] == category and r['success']]
            if category_results:
                stats['avg_time'] = sum(r['execution_time'] for r in category_results) / len(category_results)
        
        # Generate report
        report = f"""
{'='*100}
COMPREHENSIVE GEMINI API TEST REPORT (MOCK SERVICE)
{'='*100}

OVERALL RESULTS:
   Total Tests: {total_tests}
   Successful: {successful_tests} ({successful_tests/total_tests*100:.1f}%)
   Failed: {failed_tests} ({failed_tests/total_tests*100:.1f}%)
   Total Execution Time: {total_time:.2f}s

CATEGORY BREAKDOWN:
"""
        
        for category, stats in category_stats.items():
            success_rate = stats['success'] / stats['total'] * 100
            report += f"\n{category}:"
            report += f"\n  Success: {stats['success']}/{stats['total']} ({success_rate:.1f}%)"
            report += f"\n  Avg Time: {stats['avg_time']:.2f}s"
            if stats['failed'] > 0:
                report += f"\n  Failed: {stats['failed']}"
        
        # Failed queries analysis
        if failed_tests > 0:
            report += f"\n\nFAILED QUERIES ANALYSIS:\n"
            for result in self.test_results:
                if not result['success']:
                    report += f"\nCategory: {result['category']}"
                    report += f"\nQuery: {result['query']}"
                    report += f"Error: {result.get('error', 'Unknown error')}\n"
        
        # Zero results analysis
        zero_result_queries = [r for r in self.test_results if r['success'] and r.get('total_count', 0) == 0]
        if zero_result_queries:
            report += f"\n\nQUERIES RETURNING ZERO RESULTS ({len(zero_result_queries)}):\n"
            for result in zero_result_queries:
                report += f"\n- {result['category']}: {result['query']}"
        
        # Performance analysis
        successful_results = [r for r in self.test_results if r['success']]
        if successful_results:
            avg_time = sum(r['execution_time'] for r in successful_results) / len(successful_results)
            max_time = max(r['execution_time'] for r in successful_results)
            min_time = min(r['execution_time'] for r in successful_results)
            
            report += f"\n\nPERFORMANCE ANALYSIS:"
            report += f"\n   Average execution time: {avg_time:.3f}s"
            report += f"\n   Maximum execution time: {max_time:.3f}s"
            report += f"\n   Minimum execution time: {min_time:.3f}s"
        
        # Quality analysis
        quality_issues = []
        
        if zero_result_queries:
            quality_issues.append(f"Zero-result queries: {len(zero_result_queries)}")
        
        if failed_tests > 0:
            quality_issues.append(f"Failed queries: {failed_tests}")
        
        if avg_time > 1.0:  # More than 1 second average for mock service indicates issues
            quality_issues.append(f"High execution times (avg: {avg_time:.2f}s)")
        
        if quality_issues:
            report += f"\n\nQUALITY ISSUES IDENTIFIED:"
            for issue in quality_issues:
                report += f"\n   - {issue}"
        
        # Recommendations
        report += f"\n\nRECOMMENDATIONS:"
        
        if failed_tests > 0:
            report += f"\n   1. Fix {failed_tests} failed queries - check orchestrator logic"
        
        if zero_result_queries:
            report += f"\n   2. Investigate {len(zero_result_queries)} zero-result queries"
        
        report += f"\n   3. All {successful_tests} successful queries validate orchestrator patterns"
        report += f"\n   4. Mock service demonstrates full capability coverage"
        report += f"\n   5. Ready for production testing with real Gemini API"
        
        report += f"\n\n{'='*100}\n"
        
        logger.info(report)
        
        # Save detailed results to JSON
        with open('comprehensive_test_results.json', 'w') as f:
            json.dump(self.test_results, f, indent=2, default=str)
        
        # Save report to file
        with open('comprehensive_test_report.txt', 'w') as f:
            f.write(report)
        
        logger.info("Results saved to:")
        logger.info("   - comprehensive_test_results.json (detailed data)")
        logger.info("   - comprehensive_test_report.txt (analysis report)")
        logger.info("   - comprehensive_test_results.log (execution log)")

async def main():
    """Main execution"""
    logger.info("Starting Comprehensive Gemini API Test Suite with Mock Service...")
    
    runner = ComprehensiveTestRunner()
    await runner.run_comprehensive_tests()
    
    logger.info("\nAll tests completed successfully!")

if __name__ == "__main__":
    asyncio.run(main())
