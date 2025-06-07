# Debug the regex pattern issue
$query = "show me campaigns with high click rates more than 500"

# Test the exact pattern from the code
$pattern1 = "(click|open|deliver|bounce).*?(more than|greater than|above|over).*?(\d+)"
$pattern2 = "(click|open|deliver|bounce).*?(less than|below|under).*?(\d+)" 
$pattern3 = "high.*?(click|open|deliver).*?(rate|count).*?(more than|greater than|above|over).*?(\d+)"
$fullPattern = "$pattern1|$pattern2|$pattern3"

Write-Host "Testing query: $query"
Write-Host "Full pattern: $fullPattern"
Write-Host ""

$match = [regex]::Match($query, $fullPattern)
Write-Host "Match success: $($match.Success)"

if ($match.Success) {
    Write-Host "Groups found:"
    for ($i = 0; $i -lt $match.Groups.Count; $i++) {
        Write-Host "  Group $i`: '$($match.Groups[$i].Value)'"
    }
    
    # Test the extraction logic
    $metricType = if (![string]::IsNullOrEmpty($match.Groups[1].Value)) { $match.Groups[1].Value }
                  elseif (![string]::IsNullOrEmpty($match.Groups[4].Value)) { $match.Groups[4].Value }
                  else { $match.Groups[7].Value }
                  
    $thresholdStr = if (![string]::IsNullOrEmpty($match.Groups[3].Value)) { $match.Groups[3].Value }
                    elseif (![string]::IsNullOrEmpty($match.Groups[6].Value)) { $match.Groups[6].Value }
                    else { $match.Groups[10].Value }
    
    Write-Host ""
    Write-Host "Extracted:"
    Write-Host "  Metric Type: '$metricType'"
    Write-Host "  Threshold: '$thresholdStr'"
} else {
    Write-Host "No match found!"
}
