# RiotStrike - Code Dump Script
# Usage: cd vao thu muc project, sau do chay .\dump_riotstrike.ps1
# Output: code_dump.txt

$outputFile = "code_dump.txt"
$projectRoot = Get-Location

# Cac thu muc can quet (chi lay trong Assets)
$searchRoot = Join-Path $projectRoot "Assets"

if (-not (Test-Path $searchRoot)) {
    Write-Host "Khong tim thay thu muc Assets. Hay cd vao thu muc Unity project truoc." -ForegroundColor Red
    exit 1
}

# Xoa file cu neu ton tai
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

$files = Get-ChildItem -Path $searchRoot -Recurse -Filter "*.cs" | Sort-Object FullName

$totalFiles = $files.Count
Write-Host "Tim thay $totalFiles file .cs" -ForegroundColor Cyan

$separator = "=" * 80

# Header
Add-Content $outputFile "RIOTSTRIKE - CODE DUMP"
Add-Content $outputFile "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Add-Content $outputFile "Total files: $totalFiles"
Add-Content $outputFile "Project root: $projectRoot"
Add-Content $outputFile $separator
Add-Content $outputFile ""

# Bang muc luc
Add-Content $outputFile "TABLE OF CONTENTS"
Add-Content $outputFile $separator
$index = 1
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($projectRoot.Path.Length + 1)
    Add-Content $outputFile "  [$index] $relativePath"
    $index++
}
Add-Content $outputFile ""
Add-Content $outputFile $separator
Add-Content $outputFile ""

# Noi dung tung file
$index = 1
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($projectRoot.Path.Length + 1)
    
    Write-Host "[$index/$totalFiles] $relativePath" -ForegroundColor Yellow
    
    Add-Content $outputFile "[$index] FILE: $relativePath"
    Add-Content $outputFile $separator
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    Add-Content $outputFile $content
    
    Add-Content $outputFile ""
    Add-Content $outputFile $separator
    Add-Content $outputFile ""
    
    $index++
}

$fileSize = (Get-Item $outputFile).Length / 1KB
Write-Host ""
Write-Host "DONE! Output: $outputFile ($([math]::Round($fileSize, 1)) KB, $totalFiles files)" -ForegroundColor Green
