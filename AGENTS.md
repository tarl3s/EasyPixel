# AGENTS.md — EasyPixel

Context for AI coding assistants. Read this before changing anything. Design spec:
`docs/superpowers/specs/2026-10-28-easypixel-design.md`.

## The project in one paragraph

EasyPixel is a free, open-source (MIT) pixel-art editor for Windows, a sibling of EasyEngine and EasyVocal. It is a
C# / .NET 8 / Avalonia 11 app. It edits sprites with layers, frames and tags, supports RGBA and indexed (palette)
colour modes like Aseprite, round-trips `.aseprite` files losslessly for everything it models, and can import
EasyEngine `.eeproj` projects (read-only) and export. Windows first, but nothing may block a later macOS/Linux port.

- Repo: `github.com/tarl3s/EasyPixel` (public, MIT). Never push or create remotes without the owner's word.
- Owner speaks Brazilian Portuguese; code, comments, UI text, commits and this file are English.

## Build and test

```bash
dotnet build                      # 0 errors expected
dotnet test                       # EasyPixel.Tests (portable) + EasyPixel.UiTests (headless Avalonia)
dotnet run --project src/EasyPixel
```

## Global constraints

- Windows only for now; no WPF/WinForms; Windows-only calls go behind one interface.
- MIT, free for everyone: no paid tiers, accounts or ads.
- Versioning X.Y.Z; `<Version>` lives in `Directory.Build.props` (stage 1 ships 0.1.0).
- .NET 8 (SDK 8.0.425), Avalonia 11.3.22, xUnit 2.5.3, Avalonia.Headless.XUnit 11.3.22; no other NuGet packages.
- UI 100% English, every visible text through `L.T(...)`, translated into 9 languages: en, de, es, fr, ja, pt-BR,
  pt-PT, zh-Hans, zh-Hant (`Lang/*.json`).
- Dark and light themes from palette resources; text contrast >= 4.5:1.
- Any canvas size (tested up to 4096x4096); no fixed-size assumptions.
- EasyPixel never writes into an EasyEngine project; it only reads `.eeproj` to import, and exports.
- `.aseprite` round trip lossless for everything modelled; unknown content -> warning before saving.
- Colour modes: RGBA and indexed (palette).
- UTF-8 without BOM. Never rewrite sources with PowerShell `Get-Content | Set-Content`; use edit tools, `sed`/`perl`.
- Commit messages end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Each test project writes temp files only under its own temp dir and cleans up.

## File map

| Path | What |
|---|---|
| `Directory.Build.props` | shared version and compiler settings |
| `src/EasyPixel.Core` | portable model (`Model/Sprite.cs`, `Model/LayerTree.cs` keeps groups whole), file formats (`Formats/PaletteFormats.cs`: .gpl/.hex/.pal), raster tools (`Raster/SpriteTransforms.cs`: whole-sprite rotate/flip/scale/reduce), undo steps (`History/`: pixel, cel, layer structure/props, frame, tags, palette, whole-sprite); no UI |
| `src/EasyPixel` | the Avalonia app: `App.axaml(.cs)` (theme + language from settings), `MainWindow.*.cs` (one partial per menu or area: File, Edit, Sprite, Layer, Frame, Select, View (+ Settings, Help), Tools, Keys = Aseprite shortcuts), `EditorSession.cs` (open sprite, history, frame, layer, selection, floating pixels), `Settings.cs` (`%APPDATA%/EasyPixel/settings.json`), `Loc.cs` (`L.T`) |
| `src/EasyPixel/Tools` | `ToolController` + one class per tool family; `Stroke` turns a gesture into one undo step |
| `src/EasyPixel/Canvas` | `PixelCanvas`: zoom/pan, dirty-rect render into a `WriteableBitmap`, grid, onion skin, marching ants |
| `src/EasyPixel/Panels` | one file per panel: `ColorPanel` + `ColorPicker` (HSV), `PalettePanel`, `LayersPanel`, `TimelinePanel` (frames, tags, playback buttons), `Playback` (which frame comes next), `PreviewWindow` (floating 1x-8x and full preview) |
| `src/EasyPixel/Dialogs` | `IDialogs` (every question the window asks; tests script it) and the real windows |
| `src/EasyPixel/Theme`, `Lang` | `Dark.axaml`/`Light.axaml` palettes (same keys); `Lang/en.json` written by `tools/Update-Strings.sh`, the other 8 languages translate its keys (run the script, then add the new keys to every `Lang/<code>.json`; `LangTests` fails under 97% or a lost `{0}`); `L.Keep(control)` marks the person's own content (layer/tag names) as not translated |
| `src/EasyPixel.Core/Formats`, `Render`, `Raster`, `Select` | `ISpriteFormat` + `FormatRegistry` (`AsepriteFormat` lossless with warnings, `PiskelFormat`, `EpxFormat` = EasyPixel's own zip, `PngFormat`/`Png` decoder, `GifFormat`, `SpriteSheet` import/export), `Compositor` (+ blend modes, onion), brush/fill/shapes/dither/shading/symmetry/quantize, selection + floating pixels |
| `src/EasyPixel.Core/Bridge`, `src/EasyPixel/MainWindow.Bridge.cs`, `WindowsInstallRegistry.cs` | the EasyEngine link: `AppDiscovery` finds the other app (registry `HKCU\Software\EasyPixel\InstallPath`, behind an interface), `EasyEngineBridge` reads `.eeproj` animations (read-only) and `ExportHandoff` writes what EasyEngine's "Import from EasyPixel" picks up |
| `src/EasyPixel.Core/Update/ReleaseInfo.cs`, `src/EasyPixel/Update/Updater.cs`, `MainWindow.Update.cs` | updates: `UpdateLogic` (pure: parse GitHub's latest release, version compare, source-build detection = an `EasyPixel.sln` above the exe, manifest-based file removal) and `GitHubUpdater : IUpdateService` (download to `%TEMP%\EasyPixel_update`, hidden PowerShell swap after exit, only files listed in the old `easypixel-files.txt`). `MainWindow.CheckForUpdatesAsync` runs at startup unless `Settings.CheckForUpdates` is off; no network = silent |
| `src/EasyPixel/Assets` | `EasyPixel.ico` (application + window icon), drawn by `tools/Make-Icon.ps1` |
| `tools/` | `Update-Strings.sh` (rewrites `Lang/en.json`), `Make-Icon.ps1`, `Publish-Release.ps1` (self-contained single-file win-x64 → `EasyPixel-win-x64.zip` + `easypixel-files.txt` → `gh release create`; `-DryRun` builds and zips without `gh`) |
| `docs/release-notes/vX.Y.Z.md` | player-facing release notes, passed to `Publish-Release.ps1 -NotesFile` |
| `tests/EasyPixel.Tests` | portable tests of Core |
| `tests/EasyPixel.UiTests` | headless UI tests (`InternalsVisibleTo` from the app) |

## Releasing

Every version is an annotated `vX.Y.Z` tag plus a GitHub release (the in-app updater and the README's download button read
`releases/latest`, asset `EasyPixel-win-x64.zip` — never rename it). Bump `<Version>` in `Directory.Build.props`, write
`docs/release-notes/vX.Y.Z.md`, commit `vX.Y.Z: …`, `git tag -a vX.Y.Z`, push with tags, then
`powershell -ExecutionPolicy Bypass -File tools/Publish-Release.ps1 -NotesFile docs/release-notes/vX.Y.Z.md`.
Only with the owner's word; try the packaging first with `-DryRun -OutDir <scratch>`.

## Last known green (v0.1.0)

`dotnet test`: EasyPixel.Tests 241/241, EasyPixel.UiTests 92/92 (includes the contrast audit, the 9-language check and
the end-to-end draw-animate-export test). `Publish-Release.ps1 -DryRun` builds a 37.7 MB zip with `EasyPixel.exe` +
`easypixel-files.txt`, and the published exe starts.
