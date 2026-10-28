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
| `src/EasyPixel.Core` | portable model (`Model/Sprite.cs`…), file formats, tools; no UI |
| `src/EasyPixel` | the Avalonia app: `App.axaml(.cs)` (theme + language from settings), `MainWindow.*.cs` (one partial per area: File, Edit, Tools, Keys), `EditorSession.cs` (open sprite, history, frame, layer, selection, floating pixels), `Settings.cs` (`%APPDATA%/EasyPixel/settings.json`), `Loc.cs` (`L.T`) |
| `src/EasyPixel/Tools` | `ToolController` + one class per tool family; `Stroke` turns a gesture into one undo step |
| `src/EasyPixel/Canvas` | `PixelCanvas`: zoom/pan, dirty-rect render into a `WriteableBitmap`, grid, onion skin, marching ants |
| `src/EasyPixel/Dialogs` | `IDialogs` (every question the window asks; tests script it) and the real windows |
| `src/EasyPixel/Theme`, `Lang` | `Dark.axaml`/`Light.axaml` palettes (same keys); `Lang/en.json` written by `tools/Update-Strings.sh` |
| `tests/EasyPixel.Tests` | portable tests of Core |
| `tests/EasyPixel.UiTests` | headless UI tests (`InternalsVisibleTo` from the app) |
