# Opens EasyPixel, always at the latest version of the code in this folder.
# If any source file is newer than the last build, it rebuilds first (with a small "Updating..." window), then starts
# bin\app\EasyPixel.exe (a Release build; inside the repo, so it counts as a source build and never self-updates).
# The Start menu shortcut runs this script:
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "<repo>\tools\Start-EasyPixel.ps1"
param([switch]$Force) # -Force: rebuild even if nothing looks changed

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root 'bin\app'
$exe = Join-Path $bin 'EasyPixel.exe'
$dll = Join-Path $bin 'EasyPixel.dll'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

function Test-Stale {
    if ($Force -or -not (Test-Path $dll)) { return $true }
    $built = (Get-Item $dll).LastWriteTime
    $newer = Get-ChildItem (Join-Path $root 'src'), (Join-Path $root 'Directory.Build.props') -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Where-Object { $_.Extension -in '.cs', '.axaml', '.csproj', '.json', '.md', '.props' -and $_.LastWriteTime -gt $built } | Select-Object -First 1
    return $null -ne $newer
}

function Test-Running {
    @(Get-Process EasyPixel -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }).Count -gt 0
}

trap {
    # anything unexpected: say so and leave a log, instead of silently not opening
    "$(Get-Date) $_`n$($_.ScriptStackTrace)" | Out-File (Join-Path $env:TEMP 'easypixel_start.log') -Append
    [System.Windows.Forms.MessageBox]::Show("EasyPixel couldn't start: $_`n`nLog: $env:TEMP\easypixel_start.log", 'EasyPixel', 'OK', 'Error') | Out-Null
    exit 1
}

$stale = Test-Stale
if ($stale -and (Test-Running)) {
    # Windows locks the exe while one is open, so it can't be rebuilt: say so instead of quietly opening the old version
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "There is a newer version of EasyPixel, but it can't update while EasyPixel is open.`n`n" +
        "Save your work, close every EasyPixel window and open it again from this shortcut to update.`n`n" +
        "Open another window of the version that is open now?", 'EasyPixel', 'YesNo', 'Information')
    if ($answer -ne 'Yes') { exit 0 }
}
elseif ($stale) {
    # a small window while it builds, so a slow first build doesn't look like nothing happened
    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'EasyPixel'; $form.Width = 360; $form.Height = 110; $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'FixedDialog'; $form.ControlBox = $false; $form.TopMost = $true
    $form.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 36)
    $label = New-Object System.Windows.Forms.Label
    $label.Text = 'Updating EasyPixel to the latest version...'; $label.AutoSize = $false; $label.Dock = 'Fill'
    $label.TextAlign = 'MiddleCenter'; $label.ForeColor = [System.Drawing.Color]::FromArgb(230, 236, 245)
    $label.Font = New-Object System.Drawing.Font('Segoe UI', 10)
    $form.Controls.Add($label); $form.Show(); [System.Windows.Forms.Application]::DoEvents()

    $log = Join-Path $env:TEMP 'easypixel_build.log'
    $p = Start-Process dotnet -ArgumentList 'build', "`"$(Join-Path $root 'src\EasyPixel\EasyPixel.csproj')`"", '-c', 'Release', '-o', "`"$bin`"", '-nologo', '-v:q' `
        -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput $log -PassThru
    $null = $p.Handle # PowerShell 5.1 only keeps the exit code if the handle was taken while the process ran
    while (-not $p.WaitForExit(100)) { [System.Windows.Forms.Application]::DoEvents() }
    $form.Close()

    if ($p.ExitCode -ne 0) {
        $errors = (Get-Content $log | Where-Object { $_ -match ' error ' } | Select-Object -First 5) -join "`n"
        [System.Windows.Forms.MessageBox]::Show("The update failed, so the previous version will open.`n`n$errors`n`nFull log: $log", 'EasyPixel', 'OK', 'Warning') | Out-Null
    }
}

if (Test-Path $exe) { Start-Process $exe -WorkingDirectory $bin }
else { [System.Windows.Forms.MessageBox]::Show("EasyPixel isn't built yet and the build failed. See $env:TEMP\easypixel_build.log", 'EasyPixel', 'OK', 'Error') | Out-Null }
