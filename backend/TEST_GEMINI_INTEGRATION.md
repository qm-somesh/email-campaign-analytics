# Test Gemini + RAG Integration

## Test the Natural Language Query Endpoint

### 1. Basic Test Query
```bash
curl -X POST "http://localhost:5038/api/NaturalLanguageEmailTrigger/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show me campaigns with high click rates above 5%",
    "pageNumber": 1,
    "pageSize": 10,
    "includeDebugInfo": true
  }'
```

### 2. RAG-Enhanced Query
```bash
curl -X POST "http://localhost:5038/api/NaturalLanguageEmailTrigger/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Find excellent performing campaigns",
    "pageNumber": 1,
    "pageSize": 5,
    "includeDebugInfo": true
  }'
```

### 3. Complex Performance Query
```bash
curl -X POST "http://localhost:5038/api/NaturalLanguageEmailTrigger/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show campaigns with poor bounce rates and high delivery rates from last month",
    "pageNumber": 1,
    "pageSize": 10,
    "includeDebugInfo": true
  }'
```

## Expected Debug Information

The response should include:
- **Provider**: "GEMINI" (as primary)
- **RAG Enhancement**: Details about context retrieval
- **Processing Time**: Response time metrics
- **Confidence Score**: AI confidence in the extraction
- **Filter Parameters**: Extracted filter criteria

## Swagger UI
Access the interactive API documentation at:
http://localhost:5038/swagger

## Test Results Checklist

✅ Application started successfully on port 5038  
☐ Gemini provider is being used as primary  
☐ RAG enhancement is working  
☐ Query extraction is successful  
☐ Fallback to LLAMA works if Gemini fails  
☐ Debug information shows provider details  

## Configuration Summary

- **Primary Provider**: Gemini 1.5 Flash
- **Fallback Provider**: TinyLlama (local)
- **RAG**: Enabled with in-memory knowledge base
- **API Endpoint**: http://localhost:5038
- **Model**: gemini-1.5-flash
- **API Key**: Configured ✅
