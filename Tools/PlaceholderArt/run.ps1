# 임시 그림 생성기 실행
#   전부 다시 그리기:        powershell -ExecutionPolicy Bypass -File Tools\PlaceholderArt\run.ps1
#   이름에 "승주"가 든 것만:  powershell -ExecutionPolicy Bypass -File Tools\PlaceholderArt\run.ps1 승주
# 주의: 같은 이름의 그림을 덮어써요. 진짜 그림을 넣은 뒤에는 실행하지 마세요!
param([string]$Filter = "")
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Add-Type -Path (Join-Path $PSScriptRoot "PlaceholderArt.cs") -ReferencedAssemblies System.Drawing
[PlaceholderArt]::Run($root, $Filter)
