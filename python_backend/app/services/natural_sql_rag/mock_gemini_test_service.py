"""
Mock Gemini Service for Comprehensive Testing Without API Quota Limits

This service provides pre-defined SQL responses for different query types,
enabling comprehensive testing of the orchestrator and controller logic
without hitting Gemini API quotas.
"""

import re
import logging
from typing import Dict, List, Optional
from .interfaces import IGeminiSqlGeneratorService

logger = logging.getLogger(__name__)

class MockGeminiTestService(IGeminiSqlGeneratorService):
    """Mock Gemini service with comprehensive SQL responses for testing"""
    
    def __init__(self):
        # Base SQL without the GROUP BY - will be added by each method
        self.base_select = """SELECT et.Description AS StrategyName, COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails, SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount, SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS BouncedCount, SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount, SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS ClickedCount, SUM(CASE WHEN st.Status = 'complained' THEN 1 ELSE 0 END) AS ComplainedCount, SUM(CASE WHEN st.Status = 'unsubscribed' THEN 1 ELSE 0 END) AS UnsubscribedCount, MIN(eo.DateCreated) AS FirstEmailSent, MAX(eo.DateCreated) AS LastEmailSent FROM EmailTrigger et LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId LEFT JOIN WebhookLogs_bak es ON eo.EmailOutboxId = es.EmailOutboxId LEFT JOIN EmailStatus st ON es.StatusId = st.StatusId WHERE et.IsActive = 1"""
        self.base_group_by = """GROUP BY et.Description"""
        
        # Pre-defined responses for different query types
        self.query_patterns = {
            # Time-based queries
            r'recent|latest': self._get_recent_sql,
            r'last month|past month': self._get_last_month_sql,
            r'last week|past week': self._get_last_week_sql,
            r'last (\d+) days|past (\d+) days': self._get_last_n_days_sql,
            r'this year': self._get_this_year_sql,
            
            # Performance-based queries (best/highest)
            r'highest click rates|best click rates': self._get_highest_click_rates_sql,
            r'highest open rates|best open rates': self._get_highest_open_rates_sql,
            r'best performing|top performing|most successful': self._get_best_performing_sql,
            r'low bounce rates|best delivery': self._get_low_bounce_sql,
            
            # Performance-based queries (worst/lowest)
            r'lowest click rates|worst click rates': self._get_lowest_click_rates_sql,
            r'lowest open rates|worst open rates|least.*open rate': self._get_lowest_open_rates_sql,
            r'worst performing|bottom performing|least successful': self._get_worst_performing_sql,
            
            # Filtering queries
            r'click rates less than (\d+)%?': self._get_click_rates_less_than_sql,
            r'open rates less than (\d+)%?': self._get_open_rates_less_than_sql,
            r'bounce rates greater than (\d+)%?': self._get_bounce_rates_greater_than_sql,
            r'delivery rates? less than (\d+)%?': self._get_delivery_rates_less_than_sql,
            
            # Volume-based queries
            r'high-volume|high volume|sent the most': self._get_high_volume_sql,
            r'low-volume|low volume|sent the least': self._get_low_volume_sql,
            r'more than (\d+) emails': self._get_more_than_emails_sql,
            
            # Problem identification
            r'underperforming|poor performance|need improvement': self._get_underperforming_sql,
            r'problematic|delivery problems|issues': self._get_problematic_sql,
            
            # Specific campaign types
            r'maintenance|service appointment|lease expiration': self._get_specific_campaign_sql,
            
            # All campaigns
            r'all campaigns|show.*campaigns|campaign.*overview': self._get_all_campaigns_sql,
        }
    
    async def generate_sql_async(self, prompt: str) -> str:
        """Generate SQL based on prompt pattern matching"""
        user_query = self._extract_user_query(prompt)
        logger.info(f"Mock Gemini processing query: {user_query}")
        
        # Try to match query patterns
        for pattern, handler in self.query_patterns.items():
            match = re.search(pattern, user_query.lower())
            if match:
                logger.info(f"Matched pattern: {pattern}")
                return handler(user_query, match)
        
        # Default to all campaigns if no pattern matches
        logger.info("No specific pattern matched, returning all campaigns SQL")
        return self._get_all_campaigns_sql(user_query)
    
    def _extract_user_query(self, prompt: str) -> str:
        """Extract user query from the full prompt"""
        # Look for "USER REQUEST:" section
        if "USER REQUEST:" in prompt:
            return prompt.split("USER REQUEST:")[-1].strip()
        return prompt
    
    def _get_recent_sql(self, query: str, match=None) -> str:
        """Recent campaigns - last 30 days"""
        return f"{self.base_select} AND eo.DateCreated >= DATEADD(day, -30, GETDATE()) {self.base_group_by} ORDER BY LastEmailSent DESC"
    
    def _get_last_month_sql(self, query: str, match=None) -> str:
        """Last month campaigns"""
        return f"{self.base_select} AND eo.DateCreated >= DATEADD(month, -1, GETDATE()) {self.base_group_by} ORDER BY LastEmailSent DESC"
    
    def _get_last_week_sql(self, query: str, match=None) -> str:
        """Last week campaigns"""
        return f"{self.base_select} AND eo.DateCreated >= DATEADD(week, -1, GETDATE()) {self.base_group_by} ORDER BY LastEmailSent DESC"
    
    def _get_last_n_days_sql(self, query: str, match) -> str:
        """Last N days campaigns"""
        days = match.group(1) or match.group(2) or "30"
        return f"{self.base_select} AND eo.DateCreated >= DATEADD(day, -{days}, GETDATE()) {self.base_group_by} ORDER BY LastEmailSent DESC"
    
    def _get_this_year_sql(self, query: str, match=None) -> str:
        """This year campaigns"""
        return f"{self.base_select} AND eo.DateCreated >= DATEADD(year, 0, GETDATE()) {self.base_group_by} ORDER BY LastEmailSent DESC"
    
    def _get_highest_click_rates_sql(self, query: str, match=None) -> str:
        """Highest click rates"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC"
    
    def _get_highest_open_rates_sql(self, query: str, match=None) -> str:
        """Highest open rates"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC"
    
    def _get_best_performing_sql(self, query: str, match=None) -> str:
        """Best performing campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 AND SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) DESC"
    
    def _get_low_bounce_sql(self, query: str, match=None) -> str:
        """Low bounce rates"""
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 10 ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) ASC"
    
    def _get_lowest_click_rates_sql(self, query: str, match=None) -> str:
        """Lowest click rates"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC"
    
    def _get_lowest_open_rates_sql(self, query: str, match=None) -> str:
        """Lowest open rates"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC"
    
    def _get_worst_performing_sql(self, query: str, match=None) -> str:
        """Worst performing campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) > 0 ORDER BY (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) ASC"
    
    def _get_click_rates_less_than_sql(self, query: str, match) -> str:
        """Click rates less than X%"""
        percentage = int(match.group(1))
        threshold = percentage / 100
        return f"{self.base_select} {self.base_group_by} HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) < {threshold} ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'clicked' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC"
    
    def _get_open_rates_less_than_sql(self, query: str, match) -> str:
        """Open rates less than X%"""
        percentage = int(match.group(1))
        threshold = percentage / 100
        return f"{self.base_select} {self.base_group_by} HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) < {threshold} ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC"
    
    def _get_bounce_rates_greater_than_sql(self, query: str, match) -> str:
        """Bounce rates greater than X%"""
        percentage = int(match.group(1))
        threshold = percentage / 100
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) > 0 AND (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > {threshold} ORDER BY (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) DESC"
    
    def _get_delivery_rates_less_than_sql(self, query: str, match) -> str:
        """Delivery rates less than X%"""
        percentage = int(match.group(1))
        threshold = percentage / 100
        return f"{self.base_select} {self.base_group_by} HAVING COALESCE((CAST(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)), 0) < {threshold} ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)), 0) ASC"
    
    def _get_high_volume_sql(self, query: str, match=None) -> str:
        """High volume campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) >= 1000 ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC"
    
    def _get_low_volume_sql(self, query: str, match=None) -> str:
        """Low volume campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) < 100 ORDER BY COUNT(DISTINCT eo.EmailOutboxId) ASC"
    
    def _get_more_than_emails_sql(self, query: str, match) -> str:
        """More than X emails"""
        count = int(match.group(1))
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) > {count} ORDER BY COUNT(DISTINCT eo.EmailOutboxId) DESC"
    
    def _get_underperforming_sql(self, query: str, match=None) -> str:
        """Underperforming campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) < 0.6 ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC"
    
    def _get_problematic_sql(self, query: str, match=None) -> str:
        """Problematic campaigns"""
        return f"{self.base_select} {self.base_group_by} HAVING COUNT(DISTINCT eo.EmailOutboxId) = 0 OR (CAST(SUM(CASE WHEN st.Status IN ('bounced', 'failed') THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(COUNT(DISTINCT eo.EmailOutboxId), 0)) > 0.05 OR (CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)) < 0.5 ORDER BY COALESCE((CAST(SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS FLOAT) / NULLIF(SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END), 0)), 0) ASC"
    
    def _get_specific_campaign_sql(self, query: str, match=None) -> str:
        """Specific campaign types"""
        # Extract campaign type from query
        campaign_filters = {
            'maintenance': "AND et.Description LIKE '%Maintenance%'",
            'service appointment': "AND et.Description LIKE '%Service Appointment%'",
            'lease expiration': "AND et.Description LIKE '%Lease Expiration%'",
            'promotional': "AND (et.Description LIKE '%Bonus%' OR et.Description LIKE '%Coupon%')",
        }
        
        query_lower = query.lower()
        additional_filter = ""
        
        for keyword, filter_clause in campaign_filters.items():
            if keyword in query_lower:
                additional_filter = filter_clause
                break
        
        return f"{self.base_select} {additional_filter} {self.base_group_by} ORDER BY et.Description"
    
    def _get_all_campaigns_sql(self, query: str, match=None) -> str:
        """All campaigns - default query"""
        return f"{self.base_select} {self.base_group_by} ORDER BY et.Description"
