#!/usr/bin/env python3
"""
Simple demonstration script to show how the email campaign service works
This is for learning purposes - shows the flow step by step
"""

import asyncio
import json
from typing import List, Dict

# This simulates the real service flow for learning purposes

class LearningExample:
    """Simple example to understand the service flow"""
    
    def __init__(self):
        self.knowledge_base = [
            "For 'last month' queries: use DateCreated >= DATEADD(month, -1, GETDATE())",
            "Always use LEFT JOIN between EmailTrigger and EmailOutbox",
            "Group by et.Description for campaign-level results",
            "Use CASE WHEN statements for status counting"
        ]
    
    def step1_receive_request(self, user_query: str):
        """Step 1: Receive user's natural language query"""
        print("🗣️  STEP 1: User Request Received")
        print(f"   User asked: '{user_query}'")
        print(f"   Request type: Natural language to SQL")
        print()
        return user_query
    
    def step2_find_context(self, user_query: str) -> List[str]:
        """Step 2: RAG Service finds relevant context"""
        print("📚 STEP 2: RAG Service - Finding Context")
        print(f"   Searching knowledge base for: '{user_query}'")
        
        # Simple keyword matching (real version uses AI embeddings)
        relevant_context = []
        if "last month" in user_query.lower():
            relevant_context.append(self.knowledge_base[0])
        if "campaign" in user_query.lower():
            relevant_context.extend(self.knowledge_base[1:3])
        
        print(f"   Found {len(relevant_context)} relevant pieces of context:")
        for i, context in enumerate(relevant_context, 1):
            print(f"     {i}. {context}")
        print()
        return relevant_context
    
    def step3_generate_sql(self, user_query: str, context: List[str]) -> str:
        """Step 3: Gemini AI generates SQL from query + context"""
        print("🤖 STEP 3: Gemini AI - Generating SQL")
        print(f"   Input: User query + Context")
        print(f"   AI Model: gemini-1.5-flash")
        
        # Simulate AI thinking process
        print(f"   AI is thinking... 🤔")
        
        # This is a simplified example - real version sends to Gemini API
        if "last month" in user_query.lower():
            sql = """SELECT 
    et.Description AS StrategyName,
    COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
    SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount,
    SUM(CASE WHEN st.Status = 'opened' THEN 1 ELSE 0 END) AS OpenedCount
FROM EmailTrigger et
LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
LEFT JOIN WebhookLogs_bak wl ON eo.EmailOutboxId = wl.EmailOutboxId
LEFT JOIN EmailStatus st ON wl.StatusId = st.StatusId
WHERE et.IsActive = 1 
    AND eo.DateCreated >= DATEADD(month, -1, GETDATE())
GROUP BY et.Description
ORDER BY et.Description"""
        else:
            sql = """SELECT 
    et.Description AS StrategyName,
    COUNT(DISTINCT eo.EmailOutboxId) AS TotalEmails,
    SUM(CASE WHEN st.Status = 'delivered' THEN 1 ELSE 0 END) AS DeliveredCount
FROM EmailTrigger et
LEFT JOIN EmailOutbox_bak eo ON eo.CommunicationId = et.CommunicationId
WHERE et.IsActive = 1
GROUP BY et.Description
ORDER BY et.Description"""
        
        print(f"   ✅ SQL Generated!")
        print(f"   Generated SQL:")
        for line in sql.split('\n'):
            print(f"     {line}")
        print()
        return sql
    
    def step4_execute_sql(self, sql: str) -> List[Dict]:
        """Step 4: Execute SQL against database"""
        print("🗄️  STEP 4: Database Execution")
        print(f"   Connecting to: tvm.dev.db.internal.velocityadmin.com\\SQL01")
        print(f"   Database: TV_EmailService")
        print(f"   Executing SQL query...")
        
        # Simulate database results (real version connects to SQL Server)
        mock_results = [
            {
                "StrategyName": "Newsletter Campaign",
                "TotalEmails": 1500,
                "DeliveredCount": 1450,
                "OpenedCount": 725,
                "ClickedCount": 145,
                "BouncedCount": 50
            },
            {
                "StrategyName": "Promotional Campaign",
                "TotalEmails": 2000,
                "DeliveredCount": 1900,
                "OpenedCount": 950,
                "ClickedCount": 190,
                "BouncedCount": 100
            },
            {
                "StrategyName": "Welcome Series",
                "TotalEmails": 800,
                "DeliveredCount": 780,
                "OpenedCount": 468,
                "ClickedCount": 94,
                "BouncedCount": 20
            }
        ]
        
        print(f"   ✅ Query executed successfully!")
        print(f"   Found {len(mock_results)} campaigns")
        print()
        return mock_results
    
    def step5_format_response(self, results: List[Dict], page_number: int, page_size: int) -> Dict:
        """Step 5: Format results for API response"""
        print("📤 STEP 5: Format API Response")
        print(f"   Applying pagination: Page {page_number}, Size {page_size}")
        
        # Apply pagination
        start_idx = (page_number - 1) * page_size
        end_idx = start_idx + page_size
        paginated_results = results[start_idx:end_idx]
        
        total_items = len(results)
        total_pages = (total_items + page_size - 1) // page_size
        
        response = {
            "items": paginated_results,
            "total_items": total_items,
            "page_number": page_number,
            "page_size": page_size,
            "total_pages": total_pages,
            "has_next": page_number < total_pages,
            "has_previous": page_number > 1
        }
        
        print(f"   ✅ Response formatted!")
        print(f"   Returning {len(paginated_results)} items out of {total_items} total")
        print()
        return response
    
    async def full_workflow_demo(self, user_query: str, page_number: int = 1, page_size: int = 10):
        """Demonstrate the complete workflow"""
        print("=" * 80)
        print("🐍 PYTHON EMAIL CAMPAIGN SERVICE - WORKFLOW DEMONSTRATION")
        print("=" * 80)
        print()
        
        # Step 1: Receive request
        query = self.step1_receive_request(user_query)
        
        # Step 2: Get context from RAG
        context = self.step2_find_context(query)
        
        # Step 3: Generate SQL with AI
        sql = self.step3_generate_sql(query, context)
        
        # Step 4: Execute SQL
        raw_results = self.step4_execute_sql(sql)
        
        # Step 5: Format response
        final_response = self.step5_format_response(raw_results, page_number, page_size)
        
        print("🎉 FINAL RESULT:")
        print("=" * 40)
        print(json.dumps(final_response, indent=2))
        print()
        
        return final_response

# Demonstration functions
async def demo_different_queries():
    """Show how different queries are processed"""
    demo = LearningExample()
    
    test_queries = [
        "Show me campaigns from last month",
        "Find all email campaigns",
        "Show me the best performing campaigns"
    ]
    
    for i, query in enumerate(test_queries, 1):
        print(f"\n🔄 DEMO {i}: '{query}'")
        print("-" * 60)
        await demo.full_workflow_demo(query, page_number=1, page_size=5)
        
        if i < len(test_queries):
            input("\nPress Enter to continue to next demo...")

def explain_python_concepts():
    """Explain Python concepts used in this service"""
    print("\n" + "=" * 80)
    print("🐍 PYTHON CONCEPTS EXPLAINED")
    print("=" * 80)
    
    concepts = {
        "Classes": "Blueprints for creating objects. Like EmailCampaignService class.",
        "Async/Await": "Non-blocking code. Can handle multiple requests simultaneously.",
        "Type Hints": "Tell Python what type each variable should be. List[str] means 'list of strings'.",
        "Dependency Injection": "Providing objects from outside instead of creating them inside functions.",
        "Environment Variables": "Configuration stored outside code (.env file) for security.",
        "REST API": "Web service that responds to HTTP requests (GET, POST, etc.)",
        "JSON": "Data format for sending information between systems.",
        "FastAPI": "Python framework for building web APIs quickly.",
        "SQLAlchemy": "Python library for talking to databases.",
        "Pydantic": "Library for data validation and serialization."
    }
    
    for concept, explanation in concepts.items():
        print(f"\n📚 {concept}:")
        print(f"   {explanation}")
    
    print("\n🎯 Why these concepts matter:")
    print("   • Make code easier to read and maintain")
    print("   • Enable testing individual parts")
    print("   • Allow the service to handle many users at once")
    print("   • Keep sensitive data (API keys) secure")

async def main():
    """Main demonstration function"""
    print("🎓 Welcome to the Python Email Campaign Service Learning Demo!")
    print("\nWhat would you like to learn about?")
    print("1. See the complete workflow with examples")
    print("2. Learn about Python concepts used")
    print("3. Both!")
    
    choice = input("\nEnter your choice (1, 2, or 3): ").strip()
    
    if choice in ["1", "3"]:
        await demo_different_queries()
    
    if choice in ["2", "3"]:
        explain_python_concepts()
    
    print("\n🎉 Learning demo complete!")
    print("\nNext steps:")
    print("• Read the PYTHON_BEGINNER_GUIDE.md file")
    print("• Try running the real service: python start.py")
    print("• Experiment with the Swagger UI: http://localhost:8000/swagger")

if __name__ == "__main__":
    # Run the learning demo
    asyncio.run(main())
