# ShiftStorm: run a check in the built exe the quiet way (testing rules 1, 2, 4): -batchmode (no window, never takes focus), -mute,
# Below Normal priority, its own log. Usage: powershell -File Tools/Run-Check.ps1 -Check all -Out local/round1/check1
param([string]$Check = 'all', [string]$Out = 'local/round1/check', [int]$TimeoutMinutes = 20, [string[]]$Extra = @())
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$Extra = @($Extra | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })   # 'powershell -File' passes an array as one comma-joined string
$out = Join-Path $root $Out; New-Item -ItemType Directory -Force $out | Out-Null
$exe = Join-Path $root 'Builds/ShiftStorm/ShiftStorm.exe'
$a = @('-batchmode', '-mute', '-check', $Check, '-checkOut', $out, '-logFile', (Join-Path $out 'Player.log')) + $Extra
$p = Start-Process $exe -ArgumentList $a -PassThru -WindowStyle Hidden
$p.PriorityClass = 'BelowNormal'
if (-not $p.WaitForExit($TimeoutMinutes * 60000)) { $p.Kill(); "TIMED OUT after $TimeoutMinutes min" }
"exit $($p.ExitCode)"
if (Test-Path (Join-Path $out 'check_report.txt')) { Get-Content (Join-Path $out 'check_report.txt') }
