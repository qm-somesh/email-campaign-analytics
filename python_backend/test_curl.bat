@echo off
echo Testing Email Campaign API with curl
echo =====================================

echo.
echo 1. Testing Health Endpoint...
curl -X GET "http://localhost:8000/health" -H "accept: application/json"

echo.
echo.
echo 2. Testing API Endpoint with sample query...
curl -X POST "http://localhost:8000/api/natural-language-sql-query/sql-query" ^
     -H "accept: application/json" ^
     -H "Content-Type: application/json" ^
     -d "{\"query\": \"Show campaigns from the last 3 months\", \"page_number\": 1, \"page_size\": 10}"

echo.
echo.
echo 3. Testing with problematic campaigns query...
curl -X POST "http://localhost:8000/api/natural-language-sql-query/sql-query" ^
     -H "accept: application/json" ^
     -H "Content-Type: application/json" ^
     -d "{\"query\": \"Find problematic campaigns\", \"page_number\": 1, \"page_size\": 5}"

echo.
echo.
echo Testing complete!
pause
