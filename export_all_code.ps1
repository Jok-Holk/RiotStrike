$root = 'D:\work\RiotStrike'
Get-ChildItem -Path $root -Recurse -Include *.cs |
  Sort-Object FullName |
  ForEach-Object {
    $path = $_.FullName
    "===== $path ====="
    Get-Content $path
  } |
  Set-Content -Path "$root\project_all_code2.txt" -Encoding UTF8
