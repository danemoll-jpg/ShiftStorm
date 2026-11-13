# ShiftStorm: run the Unity Editor in batch mode, no window, Below Normal priority, half the cores for jobs (testing rules 2 and 4).
# Usage: powershell -File Tools/Unity-Batch.ps1 -Log local/logs/setup.log [-Method ShiftStorm.EditorTools.ProjectSetup.Run] [-Extra '-buildVersion','0.1.0']
param([string]$Log = 'local/logs/unity.log', [string]$Method = '', [string[]]$Extra = @(), [int]$TimeoutMinutes = 30)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
$logPath = Join-Path $root $Log; New-Item -ItemType Directory -Force (Split-Path $logPath) | Out-Null
$uargs = @('-batchmode', '-quit', '-nographics', '-projectPath', $root, '-logFile', $logPath, '-job-worker-count', '4')
if ($Method) { $uargs += @('-executeMethod', $Method) }
$uargs += $Extra
$p = Start-Process $unity -ArgumentList $uargs -PassThru -WindowStyle Hidden
$p.PriorityClass = 'BelowNormal'
if (-not $p.WaitForExit($TimeoutMinutes * 60000)) { $p.Kill(); throw "Unity timed out after $TimeoutMinutes min" }
$errors = Select-String -Path $logPath -Pattern 'error CS|\[Build\]|\[Setup\]|Exception' | Select-Object -First 25
$errors | ForEach-Object { $_.Line }
"exit $($p.ExitCode)"
