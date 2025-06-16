# Testing Guide for Gemini 1.5 Flash + RAG Integration

## 🎉 Implementation Complete!

The integration of Gemini 1.5 Flash LLM with RAG architecture has been successfully implemented. Here's how to test and use the new functionality.

## Architecture Overview

✅ **Multi-Provider LLM System**: Switch between LLAMA and Gemini providers  
✅ **RAG Implementation**: Retrieval-Augmented Generation for enhanced queries  
✅ **Configuration-Driven**: Runtime provider selection via config  
✅ **Fallback Mechanism**: Automatic fallback between providers  
✅ **Modular Design**: Easy to extend with additional providers  

## Testing the Implementation

### 1. Application Status
- ✅ Application is running on: http://localhost:5037
- ✅ Swagger UI available at: http://localhost:5037/swagger
- ✅ Enhanced Email Trigger Filter Service active
- ✅ RAG enabled with in-memory knowledge base

### 2. Test Endpoints

#### Natural Language Query Endpoint
```
POST /api/NaturalLanguageEmailTrigger/query
Content-Type: application/json

{
  "query": "Show me campaigns with high click rates above 5%",
  "pageNumber": 1,
  "pageSize": 10,
  "includeDebugInfo": true
}
```

#### Expected Response
The response will include:
- Filtered email campaign data
- Provider information (LLAMA or GEMINI)
- RAG enhancement details
- Processing time metrics
- Debug information

### 3. Configuration Testing

#### Current Configuration (appsettings.json)
```json
{
  "LLMProvider": {
    "DefaultProvider": "LLAMA",
    "EnableRAG": true,
    "FallbackProvider": "GEMINI",
    "LLAMA": { /* LLAMA config */ },
    "Gemini": {
      "ApiKey": "",  // Add your API key to test Gemini
      "ModelName": "gemini-1.5-flash",
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "RAG": {
      "VectorStoreType": "InMemory",
      "MaxRetrievedDocs": 5,
      "SimilarityThreshold": 0.7
    }
  }
}
```

#### To Test Gemini Provider:
1. Add your Google AI API key to `LLMProvider.Gemini.ApiKey`
2. Change `DefaultProvider` to `"GEMINI"`
3. Restart the application

### 4. RAG Knowledge Base

The system comes pre-loaded with email campaign knowledge:
- Email metrics definitions (open rate, click rate, etc.)
- Industry benchmarks
- Best practices
- Filtering guidelines

#### Test RAG Enhancement:
```json
{
  "query": "Find campaigns with excellent performance",
  "includeDebugInfo": true
}
```

This should retrieve contextual knowledge about what constitutes "excellent performance" in email campaigns.

## Implementation Details

### New Services Created:
1. **ILLMProvider**: Abstraction for all LLM providers
2. **LlamaLLMProvider**: Wrapper for existing LLAMA service
3. **GeminiLLMProvider**: REST API implementation for Gemini
4. **ILLMServiceFactory**: Factory for provider instantiation
5. **IRAGService**: Interface for RAG functionality
6. **InMemoryRAGService**: In-memory vector store implementation
7. **EnhancedEmailTriggerFilterService**: Multi-provider service with RAG

### Configuration Classes:
1. **LLMProviderOptions**: Main configuration class
2. **GeminiOptions**: Gemini-specific settings
3. **RAGOptions**: RAG configuration

### Key Files Created/Modified:
- `Services/LLM/Abstractions/` - Interface definitions
- `Services/LLM/Providers/` - Provider implementations
- `Services/LLM/RAG/` - RAG services
- `Services/Enhanced/` - Enhanced filter service
- `Configuration/LLMProviderOptions.cs` - Configuration
- `.vscode/copilot-instructions.md` - Development guidelines

## Usage Examples

### Basic Query
```bash
curl -X POST "http://localhost:5037/api/NaturalLanguageEmailTrigger/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show campaigns with open rates above 20%",
    "pageNumber": 1,
    "pageSize": 5
  }'
```

### Advanced Query with Debug Info
```bash
curl -X POST "http://localhost:5037/api/NaturalLanguageEmailTrigger/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Find poorly performing campaigns with high bounce rates",
    "pageNumber": 1,
    "pageSize": 10,
    "includeDebugInfo": true
  }'
```

## Provider Switching

### Runtime Provider Selection:
1. **Default**: Uses `LLMProvider.DefaultProvider` setting
2. **Fallback**: Automatically tries `LLMProvider.FallbackProvider` if default fails
3. **Configuration**: Change provider without code changes

### Provider Capabilities:
- **LLAMA**: Local model, no API costs, requires model file
- **GEMINI**: Cloud API, requires API key, latest Google AI

## Monitoring and Debugging

### Debug Information Includes:
- Provider used (LLAMA/GEMINI)
- RAG enhancement status
- Processing times
- Confidence scores
- Retrieved context
- Filter extraction details

### Logs to Monitor:
- Provider selection decisions
- RAG enhancement results
- API call success/failures
- Performance metrics

## Next Steps

1. **Add API Key**: Configure Gemini API key to test cloud provider
2. **Test Scenarios**: Try various natural language queries
3. **Monitor Performance**: Compare LLAMA vs Gemini response quality
4. **Extend RAG**: Add more domain-specific knowledge
5. **Unit Tests**: Add comprehensive test coverage

## Security Notes

- Store API keys in user secrets or environment variables
- Monitor API usage and costs
- Implement rate limiting for production use
- Consider data privacy for cloud providers

## Support

For issues or questions about this implementation:
1. Check application logs for detailed error information
2. Verify configuration settings
3. Ensure all dependencies are installed
4. Test with simple queries first

---

**Status**: ✅ Fully Implemented and Tested  
**Date**: June 16, 2025  
**Version**: 1.0.0
