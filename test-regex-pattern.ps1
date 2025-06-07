# Test the regex pattern for numeric threshold detection
$testQuery = "show me campaigns with high click rates more than 500"
$queryLower = $testQuery.ToLowerInvariant()

Write-Host "Testing regex pattern on query: '$queryLower'" -ForegroundColor Yellow

# Test the exact pattern from our code
$pattern1 = "(click|open|deliver|bounce).*?(more than|greater than|above|over).*?(\d+)"
$pattern2 = "(click|open|deliver|bounce).*?(less than|below|under).*?(\d+)"
$pattern3 = "high.*?(click|open|deliver).*?(rate|count).*?(more than|greater than|above|over).*?(\d+)"

$fullPattern = "$pattern1|$pattern2|$pattern3"

Write-Host "`nFull pattern: $fullPattern" -ForegroundColor Cyan

$match = [regex]::Match($queryLower, $fullPattern)

Write-Host "`nMatch success: $($match.Success)" -ForegroundColor $(if($match.Success) {"Green"} else {"Red"})

if ($match.Success) {
    Write-Host "Match value: '$($match.Value)'" -ForegroundColor Green
    
    # Show all groups
    for ($i = 0; $i -lt $match.Groups.Count; $i++) {
        Write-Host "Group $i`: '$($match.Groups[$i].Value)'" -ForegroundColor Blue
    }
    
    # Extract the values like our code does
    $metricType = if ($match.Groups[1].Value) { $match.Groups[1].Value } 
                  elseif ($match.Groups[4].Value) { $match.Groups[4].Value }
                  elseif ($match.Groups[7].Value) { $match.Groups[7].Value }
                  else { "NONE" }
    
    $threshold = if ($match.Groups[3].Value) { $match.Groups[3].Value }
                 elseif ($match.Groups[6].Value) { $match.Groups[6].Value }
                 elseif ($match.Groups[10].Value) { $match.Groups[10].Value }
                 else { "NONE" }
    
    $isGreater = $match.Groups[2].Success -or $match.Groups[9].Success
    
    Write-Host "`nExtracted values:" -ForegroundColor Magenta
    Write-Host "  metricType: '$metricType'" -ForegroundColor White
    Write-Host "  threshold: '$threshold'" -ForegroundColor White
    Write-Host "  isGreater: $isGreater" -ForegroundColor White
} else {
    Write-Host "No match found. Let's test individual patterns:" -ForegroundColor Red
    
    Write-Host "`nTesting pattern 1: $pattern1"
    $match1 = [regex]::Match($queryLower, $pattern1)
    Write-Host "Match 1: $($match1.Success) - '$($match1.Value)'"
    
    Write-Host "`nTesting pattern 2: $pattern2"
    $match2 = [regex]::Match($queryLower, $pattern2)
    Write-Host "Match 2: $($match2.Success) - '$($match2.Value)'"
    
    Write-Host "`nTesting pattern 3: $pattern3"
    $match3 = [regex]::Match($queryLower, $pattern3)
    Write-Host "Match 3: $($match3.Success) - '$($match3.Value)'"
    
    if ($match3.Success) {
        for ($i = 0; $i -lt $match3.Groups.Count; $i++) {
            Write-Host "  Group $i`: '$($match3.Groups[$i].Value)'" -ForegroundColor Blue
        }
    }
}
