# Publishes the version in Directory.Build.props as a GitHub release: a self-contained single-file win-x64 build (no .NET
# needed) zipped as EasyPixel-win-x64.zip, with easypixel-files.txt listing every file (the in-app updater removes
# exactly those when the next version comes). The asset name is fixed: the README's download button and the updater use it.
#   powershell -ExecutionPolicy Bypass -File tools\Publish-Release.ps1 -NotesFile docs\release-notes\v0.1.0.md
#   powershell -ExecutionPolicy Bypass -File tools\Publish-Release.ps1 -DryRun [-OutDir <folder>]   (build + zip, no gh)
# The tag vX.Y.Z must already exist and be pushed before a real publish.
param(
    [string]$NotesFile,
    [switch]$DryRun,
    [string]$OutDir
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$gh = 'C:\Program Files\GitHub CLI\gh.exe'
if (-not $OutDir) { $OutDir = Join-Path $env:TEMP 'EasyPixel_publish' }
$OutDir = [IO.Path]::GetFullPath($OutDir)

$props = [IO.File]::ReadAllText((Join-Path $root 'Directory.Build.props'))
if ($props -notmatch '<Version>(\d+\.\d+\.\d+)</Version>') { throw 'No <Version> in Directory.Build.props' }
$version = $Matches[1]
$tag = "v$version"

if (-not $DryRun) {
    # Check everything the upload needs before spending a minute on the build.
    if (-not $NotesFile) { throw 'Pass -NotesFile <notes.md> to publish (or -DryRun).' }
    if (-not (Test-Path -LiteralPath $NotesFile -PathType Leaf)) { throw "Notes file not found: $NotesFile" }
    git -C $root rev-parse --verify --quiet "refs/tags/$tag" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The tag $tag doesn't exist here: create and push it first (git tag -a $tag -m ...)." }
}

$out = Join-Path $OutDir 'EasyPixel'
$zip = Join-Path $OutDir 'EasyPixel-win-x64.zip'
if (Test-Path -LiteralPath $OutDir) {
    # Only ever wipe a folder this script made: a wrong -OutDir (a home or desktop folder) must not be deleted.
    $foreign = @(Get-ChildItem -LiteralPath $OutDir -Force | Where-Object { $_.Name -notin @('EasyPixel', 'EasyPixel-win-x64.zip') })
    if ($foreign.Count -gt 0) { throw "$OutDir holds other files ($($foreign[0].Name)...): pass an empty folder or a previous -OutDir of this script." }
    Remove-Item -LiteralPath $OutDir -Recurse -Force
}
New-Item -ItemType Directory -Force $OutDir | Out-Null

# the native libraries (Skia, HarfBuzz, ANGLE) go inside EasyPixel.exe, so the zip is one file plus the manifest
dotnet publish (Join-Path $root 'src\EasyPixel\EasyPixel.csproj') -c Release -r win-x64 --self-contained true --nologo -v q `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -p:Version=$version -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$manifest = 'easypixel-files.txt'
$files = @(Get-ChildItem $out -Recurse -File | ForEach-Object { $_.FullName.Substring($out.Length + 1) })
[IO.File]::WriteAllLines((Join-Path $out $manifest), [string[]]($files + $manifest), (New-Object Text.UTF8Encoding $false))
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -CompressionLevel Optimal
"built $zip ($([Math]::Round((Get-Item $zip).Length / 1MB, 1)) MB, $($files.Count + 1) files)"

if ($DryRun) { 'Done (-DryRun: nothing was published).'; return }

& $gh release create $tag $zip --repo tarl3s/EasyPixel --title "EasyPixel $tag" --notes-file $NotesFile --verify-tag
if ($LASTEXITCODE -ne 0) { throw 'gh release create failed' }
