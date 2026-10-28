# EasyPixel Stage 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship EasyPixel v0.1.0: a Windows pixel-art editor with the essential Aseprite/Piskel toolset at any image size,
layers + animation, `.aseprite`/`.piskel` open/save, PNG/sheet/GIF export, and the EasyEngine round trip by export.

**Architecture:** A UI-free core library (`EasyPixel.Core`: document model, raster ops, undo, file formats) tested with
xUnit, and an Avalonia app (`EasyPixel`) on top of it. The EasyEngine bridge is a small folder+JSON contract both apps
read; EasyEngine gets one button in `AnimationWindow`, no new dependency.

**Tech Stack:** .NET 8, Avalonia 11.3.22 (same as EasyVocal), xUnit 2.5.3, Avalonia.Headless.XUnit for UI tests.
No other NuGet packages: zlib via `System.IO.Compression.ZLibStream`, PNG via Avalonia's `Bitmap` in the app and a
small own PNG codec in Core (tests must not need a UI).

**Spec:** `docs/superpowers/specs/2026-10-28-easypixel-design.md`

## Global Constraints

- Windows only for now; code must not block a later macOS/Linux port (no WPF, no WinForms; Windows-only calls behind one interface).
- Open source, free for everyone: MIT license, public repo `github.com/tarl3s/EasyPixel`, no paid tiers, accounts or ads.
- Repo folder: `C:\Users\tarle\OneDrive\Área de Trabalho\Jogos\EasyPixel` (next to `engine` and `EasyVocal`).
- Versioning X.Y.Z like EasyEngine; this stage ships `0.1.0`; `<Version>` lives in `Directory.Build.props`; each version an annotated tag + GitHub release.
- UI 100% English, every visible text through `L.T(...)`, translated into the same 9 languages as EasyEngine (`Lang/*.json`).
- Dark and light themes from palette resources; text contrast ≥ 4.5:1.
- Any canvas size (tested up to 4096×4096); no fixed-size assumption anywhere.
- EasyPixel never writes into an EasyEngine project; it only reads `.eeproj` to import, and exports.
- `.aseprite` round trip lossless for everything EasyPixel models; unknown content → warning before saving.
- Colour modes: RGBA and indexed (palette) as in Aseprite.
- UTF-8 without BOM; never rewrite sources via PowerShell `Get-Content | Set-Content`.
- Owner is answered in PT-BR; code, commits, docs in English.

## Review Focus

1. Huge canvases (4096×4096, 100 frames): flood fill, undo and rendering must stay responsive and not blow memory — undo stores only the changed rectangle (test in Task 3).
2. Malformed or newer-version `.aseprite`/`.piskel`/GIF files: open must fail with a readable message, never crash or hang (truncated-file tests in Tasks 8–9).
3. Indexed-mode documents: drawing a colour not in the palette, deleting a palette entry in use, transparent index — must not corrupt pixels (test in Task 2).
4. EasyEngine not installed / project moved / `.eeproj` from an older engine version: "Import sprite from EasyEngine" explains, never throws (test in Task 11).
5. Unsaved work: closing the window or opening another file with changes asks first (test in Task 10).

---

## File Structure

```
EasyPixel/
  EasyPixel.sln, Directory.Build.props, LICENSE, README.md, README.pt-BR.md, AGENTS.md, CLAUDE.md, .gitignore
  src/EasyPixel.Core/           (net8.0 class library, no UI)
    Model/  Rgba.cs, Palette.cs, Cel.cs, Layer.cs, Frame.cs, Tag.cs, Sprite.cs
    Raster/ Shapes.cs, Fill.cs, Brush.cs, Symmetry.cs, Dither.cs, Shading.cs, Outline.cs, Transform.cs, Pixelate.cs, Quantize.cs
    Select/ Selection.cs, Selectors.cs
    Render/ Compositor.cs, BlendModes.cs, Onion.cs
    History/ History.cs, PixelEdit.cs
    Formats/ Png.cs, EpxFormat.cs, AsepriteFormat.cs, PiskelFormat.cs, GifFormat.cs, SpriteSheet.cs, FormatRegistry.cs
    Bridge/ EasyEngineBridge.cs, ExportHandoff.cs, AppDiscovery.cs
  src/EasyPixel/                (Avalonia app, WinExe)
    App.axaml(.cs), MainWindow.axaml(.cs), Canvas/PixelCanvas.cs, Panels/{Tools,Palette,Layers,Timeline,Preview}Panel.cs,
    Tools/ToolController.cs, Loc.cs, Lang/*.json, Theme/*.axaml, Settings.cs, Update/Updater.cs, Assets/EasyPixel.ico
  tests/EasyPixel.Tests/        (xUnit, Core)
  tests/EasyPixel.UiTests/      (Avalonia.Headless.XUnit)
  tools/Publish-Release.ps1, tools/Make-Icon.ps1
engine/ (EasyEngine repo)
  EasyPixelBridge.cs (new), AnimationWindow.xaml(.cs) (button), FullTest.EasyPixel.cs (new), Lang/*.json
```

---

### Task 1: Repository scaffold

**Files:** everything at the repo root above, `src/EasyPixel.Core/EasyPixel.Core.csproj`, `src/EasyPixel/EasyPixel.csproj`, `tests/*/…csproj`.

**Interfaces:** Produces the solution every later task builds in.

- [ ] **Step 1:** `git init` the folder; `Directory.Build.props` with `<Version>0.1.0</Version>`, `Nullable enable`, `LangVersion 12`, `ImplicitUsings enable` (copy EasyVocal's). MIT `LICENSE` (holder: tarl3s). `.gitignore` for bin/obj/publish.
- [ ] **Step 2:** Projects: Core `net8.0`; app `WinExe net8.0` with Avalonia/Avalonia.Desktop/Avalonia.Themes.Fluent 11.3.22, `InternalsVisibleTo EasyPixel.UiTests`; tests per the Tech Stack line.
- [ ] **Step 3:** Write test `Smoke.CoreAssemblyLoads` asserting `typeof(EasyPixel.Core.Model.Sprite).Assembly.GetName().Version == new Version(0,1,0,0)`; run `dotnet test` → FAIL (no Sprite).
- [ ] **Step 4:** Add an empty `Sprite` class; `dotnet build` 0 errors, `dotnet test` PASS; app opens an empty window.
- [ ] **Step 5:** `AGENTS.md` (project in one paragraph, build/test commands, the Global Constraints above, file map) and `CLAUDE.md` (`@AGENTS.md` + PT-BR answer rule, as in the engine). README.md/README.pt-BR.md one-paragraph stubs.
- [ ] **Step 6:** Commit `chore: scaffold EasyPixel solution`. Create the public GitHub repo only at the end of the session (owner's rule).

### Task 2: Document model (RGBA + indexed)

**Files:** `Core/Model/*`, `tests/EasyPixel.Tests/ModelTests.cs`

**Interfaces — Produces:**
- `readonly record struct Rgba(byte R, byte G, byte B, byte A)` with `uint Packed`, `static Rgba Transparent`.
- `enum ColorMode { Rgba, Indexed }`
- `sealed class Palette { List<Rgba> Colors; int TransparentIndex = -1; int IndexOf(Rgba c); int Add(Rgba c); }`
- `sealed class Cel { int X, Y; Image Image; float Opacity = 1; }` and `sealed class Image { int Width, Height; uint[] Pixels; uint Get(int x,int y); void Set(int x,int y,uint v); Image Clone(); }` — in Indexed mode `Pixels` hold palette indices.
- `enum BlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten, ColorDodge, ColorBurn, HardLight, SoftLight, Difference, Exclusion, Hue, Saturation, Color, Luminosity, Addition, Subtract, Divide }` (Aseprite's list, same order as its file ids).
- `sealed class Layer { string Name; bool Visible = true, Locked; byte Opacity = 255; BlendMode Blend; Layer? Parent; bool IsGroup; }`
- `sealed class Frame { int DurationMs = 100; }`, `sealed class Tag { string Name; int From, To; LoopDir Dir; Rgba Color; }`, `enum LoopDir { Forward, Reverse, PingPong }`
- `sealed class Sprite { int Width, Height; ColorMode Mode; Palette Palette; List<Layer> Layers; List<Frame> Frames; List<Tag> Tags; Dictionary<(Layer,int),Cel> Cels; Dictionary<string,byte[]> UnknownChunks; Cel GetOrCreateCel(Layer l,int frame); void Resize(int w,int h, Anchor a); void ConvertMode(ColorMode to); }`

- [ ] **Step 1: Tests** — `NewSprite_AnySize` (8000×1 and 1×8000 allowed; 0 or negative throws `ArgumentOutOfRangeException`); `Indexed_DrawColorNotInPalette_AddsEntry` (palette grows, pixel = new index); `Indexed_RemovePaletteEntryInUse_RemapsToNearest`; `ConvertMode_RgbaToIndexedToRgba_PreservesColors` for a ≤256-colour image; `GetOrCreateCel_ReturnsSameCel`.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run → PASS. **Step 5:** Commit `feat(core): document model`.

### Task 3: Undo/redo

**Files:** `Core/History/*`, `tests/…/HistoryTests.cs`

**Interfaces — Produces:** `interface IEdit { string Name; void Undo(Sprite s); void Redo(Sprite s); long Bytes; }`; `PixelEdit.Capture(Cel cel, PixelRect dirty)` → stores before/after of the dirty rectangle only; `sealed class History { void Push(IEdit e); bool Undo(); bool Redo(); bool IsDirty; void MarkSaved(); long MaxBytes = 512 MB; }` (drops oldest edits past the cap); structural edits (add/remove layer/frame, resize) are `IEdit`s too.

- [ ] Tests: `Undo_RestoresPixels`, `Redo_AfterUndo`, `NewEditClearsRedo`, `IsDirty_FalseAfterMarkSaved_TrueAfterEdit`, `BigCanvas_OnePixelEdit_StoresSmallDiff` (4096×4096, set one pixel → `Bytes < 1024`), `Cap_DropsOldest`. Fail → implement → pass → commit `feat(core): undo history`.

### Task 4: Drawing operations

**Files:** `Core/Raster/{Shapes,Fill,Brush,Symmetry,Dither,Shading,Outline,Transform,Pixelate,Quantize}.cs`, `tests/…/RasterTests.cs`

**Interfaces — Produces** (all write through a `PixelSink` that records the dirty rect, so Task 3 can capture): `Shapes.Line(PixelSink s,int x0,int y0,int x1,int y1,Brush b, bool pixelPerfect)`, `Shapes.Rect(…, bool filled)`, `Shapes.Ellipse(…, bool filled)`; `Brush { int Size; BrushShape Shape /*Square, Circle*/; }`; `Fill.Flood(PixelSink s,int x,int y,uint color,bool contiguous,int tolerance, Selection? clip)` (scanline, no recursion); `Symmetry { bool H, V; float AxisX, AxisY; IEnumerable<(int,int)> Mirror(int x,int y); }`; `Dither.Pattern(DitherKind kind /*Checker, Bayer2, Bayer4, Bayer8*/, int x,int y) → bool`; `Shading.Apply(… , Palette ramp, bool lighten)` (Aseprite shading: step along the palette ramp) and `Shading.LightenDarken(…, int amount)`; `Outline.Apply(Image img, uint color, bool outside, bool diagonal)`; `Transform.FlipH/FlipV/Rotate90/Rotate180/ScaleNearest(Image, int w,int h)`/`RotateFree(Image, double deg)` (RotSprite-style nearest); `Pixelate.Apply(Image img, int cell)`; `Quantize.ToPalette(Image img, Palette p, bool dither)` and `Quantize.MedianCut(Image img, int maxColors) → Palette`.

- [ ] Tests with exact pixels: `Line_PixelPerfect_NoDoubleCorners` (0,0→5,2 has no L-shaped triples); `Flood_Contiguous_StopsAtBorder`; `Flood_Global_ReplacesAll`; `Flood_4096Square_UnderOneSecond`; `Flood_RespectsSelectionClip`; `Ellipse_Filled_Symmetric`; `Symmetry_HV_FourPoints`; `Bayer4_Pattern_Matches` (known 4×4 matrix); `Outline_Outside_OnlyTransparentNeighbours`; `Rotate90_FourTimes_Identity`; `Pixelate_Cell4_BlockUniform`; `MedianCut_MaxColorsRespected`. Fail → implement → pass → commit `feat(core): raster ops`.

### Task 5: Selection

**Files:** `Core/Select/*`, `tests/…/SelectionTests.cs`

**Interfaces — Produces:** `sealed class Selection { int Width, Height; bool Contains(int x,int y); PixelRect Bounds; static Selection All/None; Selection Union/Subtract/Intersect(Selection o); Selection Invert(); }`; `Selectors.Rect/Ellipse/Lasso(IReadOnlyList<(int,int)> poly)/MagicWand(Image img,int x,int y,int tolerance,bool contiguous)`; `Floating { Image Pixels; int X, Y; static Floating Lift(Cel c, Selection s); void Drop(Cel c); }` (move/transform/copy/paste go through it; clipboard = PNG on the OS clipboard in the app).

- [ ] Tests: `Lasso_Concave_ContainsInterior`; `MagicWand_Tolerance0_ExactColor`; `Subtract_RemovesArea`; `LiftMoveDrop_MovesPixels_LeavesTransparent`; `Floating_FlipH`. Fail → implement → pass → commit `feat(core): selection`.

### Task 6: Compositing and onion skin

**Files:** `Core/Render/*`, `tests/…/RenderTests.cs`

**Interfaces — Produces:** `Compositor.Flatten(Sprite s, int frame, Image target, PixelRect area, bool includeHidden=false)`; `BlendModes.Blend(BlendMode m, uint back, uint front, byte opacity) → uint` (formulas = Aseprite's `doc/blend_funcs.cpp` semantics); `Onion.Render(Sprite s,int frame, OnionSettings o, Image target)` with `OnionSettings { int Before=1, After=1; byte Opacity=68; bool Tint; }`.

- [ ] Tests: `Normal_HalfOpacity_OverWhite` exact value; one known pixel per blend mode (red over grey) matching Aseprite's results; `HiddenLayer_NotDrawn`; `GroupOpacity_AppliesToChildren`; `Flatten_DirtyRect_Only`. Fail → implement → pass → commit `feat(core): compositor`.

### Task 7: Native format `.epx` and PNG codec

**Files:** `Core/Formats/{Png,EpxFormat,FormatRegistry}.cs`, `tests/…/FormatTests.Epx.cs`

**Interfaces — Produces:** `Png.Encode(Image img) → byte[]`, `Png.Decode(byte[]) → Image` (8-bit RGBA/RGB/indexed/grey, all filters; Adam7 decode); `interface ISpriteFormat { string Name; string[] Extensions; bool CanRead, CanWrite; Sprite Read(Stream s, List<string> warnings); void Write(Sprite sp, Stream s, List<string> warnings); }`; `FormatRegistry.ForPath(string path) → ISpriteFormat?`. `.epx` = zip: `sprite.json` (all model fields, `"format":1`) + `cels/<layer>_<frame>.png`.

- [ ] Tests: `Png_RoundTrip_Random64`; `Epx_RoundTrip_AllModelFields` (layers, groups, blend, tags, durations, indexed palette); `Epx_NewerFormatVersion_ReadableError`; `Epx_Truncated_ThrowsFormatException_NotCrash`. Fail → implement → pass → commit `feat(core): epx format + png`.

### Task 8: `.aseprite` read/write

**Files:** `Core/Formats/AsepriteFormat.cs`, `tests/…/FormatTests.Aseprite.cs`, `tests/…/Samples/*.aseprite` (hand-built in the test via the writer + a few small files created in Aseprite-compatible LibreSprite or taken from MIT-licensed repos, with their licence noted)

Implements the public spec (`aseprite/docs/ase-file-specs.md`): header, frames, chunks 0x2004 layer, 0x2005 cel (raw, linked, compressed image; compressed tilemap → kept as unknown), 0x2007 color profile, 0x2018 tags, 0x2019 palette, 0x0004/0x0011 old palettes, 0x2020 user data, 0x2022 slices, 0x2023 tileset. What EasyPixel doesn't model (slices, user data, tilesets, tilemap cels) goes into `Sprite.UnknownChunks` and is written back unchanged; if a later edit can't keep them consistent (e.g. frame deleted), add a warning.

- [ ] Tests: `RoundTrip_Bytes_ModelledFieldsEqual`; `LinkedCel_PreservedAsLink`; `Indexed_TransparentIndex_Kept`; `UnknownChunks_WrittenBack`; `Truncated_ReadableError`; `GreyscaleMode_ReadsAsRgba_WithWarning`. Fail → implement → pass → commit `feat(core): aseprite format`.

### Task 9: `.piskel`, GIF, PNG sheets

**Files:** `Core/Formats/{PiskelFormat,GifFormat,SpriteSheet}.cs`, `tests/…/FormatTests.Other.cs`

`.piskel`: JSON `modelVersion 2`, layers as base64 PNG strips (`chunks` with `layout`), `fps`. GIF: read (LZW, disposal methods, local palettes, transparency) and write (per-frame palette via `Quantize.MedianCut`, frame delay from `DurationMs`, loop forever). Sheets: `SpriteSheet.Export(Sprite, SheetLayout {Horizontal, Vertical, Grid(cols)}, int padding) → (Image sheet, string json)` (json = frames with x,y,w,h,durationMs + tags); `SpriteSheet.Slice(Image img, int cellW, int cellH, int padding) → Sprite`; plain PNG opens as a 1-frame sprite.

- [ ] Tests: `Piskel_RoundTrip`; `Piskel_ModelVersion1_Readable`; `Gif_WriteRead_FramesAndDelays`; `Gif_Disposal_RestoreBackground`; `Gif_Truncated_ReadableError`; `Sheet_ExportSlice_RoundTrip`. Fail → implement → pass → commit `feat(core): piskel, gif, sheets`.

### Task 10: The editor UI

**Files:** app project files listed in File Structure; `tests/EasyPixel.UiTests/*`

**Interfaces — Consumes:** everything from Tasks 2–9. **Produces:** `ToolController` (current tool, colours, brush, symmetry, dither, pixel-perfect), `EditorSession { Sprite Doc; History History; string? Path; int Frame; Layer Layer; Selection Sel; }`.

Layout (Aseprite-like): top menu (File/Edit/Sprite/Layer/Frame/Select/View/Help), left tool strip, centre `PixelCanvas` (zoom 1–6400% with wheel/Ctrl+wheel, space-drag pan, pixel grid toggle, tiled-preview toggle, renders only the dirty rect into a `WriteableBitmap`), right panels (colour picker + palette with load/save `.gpl`/`.hex`/`.pal`, layers with visibility/lock/opacity/blend/groups), bottom timeline (frames, durations, tags, onion-skin toggle, play/loop) and a floating preview. Tools: pencil, eraser, bucket, line, rectangle, ellipse, rect/ellipse/lasso/magic-wand selection, move, eyedropper, shading/lighten-darken, dither brush, spray, mirror toggles. Sprite menu: canvas size, resize, rotate, flip, colour mode, outline, pixelate, reduce to palette. Shortcuts follow Aseprite's defaults (B pencil, E eraser, G bucket, L line, U rect/ellipse, M marquee, Q lasso, W wand, V move, I eyedropper, Alt eyedropper, X swap colours, Ctrl+Z/Y, Alt+N new frame, Shift+N new layer, Tab hide panels, Enter play). Dark/light theme, `L.T` everywhere, 9 `Lang/*.json`.

- [ ] Tests (headless): `NewSprite_DrawPencil_PixelSet`; `UndoShortcut_RevertsStroke`; `CloseWithChanges_AsksToSave` (Review Focus 5); `OpenAseprite_ThenSave_KeepsFile`; `Zoom_6400_NoException`; `AllTexts_GoThroughLoc` (no untranslated literal in the visual tree); `Contrast_AllTexts_AtLeast4_5` both themes; `Canvas4096_Stroke_Under16ms` per segment.
- [ ] Manual check by the agent: run the app, draw, animate, export a GIF; save a snapshot PNG of the main window per theme to `%TEMP%\easypixel_snap_*.png` (never capture the owner's screen).
- [ ] Commit `feat(app): editor UI` (split into smaller commits per panel as work goes).

### Task 11: EasyEngine bridge (EasyPixel side)

**Files:** `Core/Bridge/*`, `tests/…/BridgeTests.cs`, app menu items.

**Contract (both apps rely on it — keep exact):**
- Discovery: each app writes on start `HKCU\Software\EasyEngine\InstallPath` / `HKCU\Software\EasyPixel\InstallPath` (exe path). Behind `AppDiscovery.FindEasyEngine() → string?` / `FindEasyPixel()`; fallback: settings files `%APPDATA%\EasyEngine\settings.json` exists. Windows registry access in one class.
- Handoff folder: `%LOCALAPPDATA%\EasyPixel\Exports\<name>-<yyyyMMdd-HHmmss>\` containing `frame_000.png…` and `easypixel-export.json`: `{"format":1,"name":"walk","fps":10,"frames":["frame_000.png",…],"width":32,"height":32,"source":"<optional original path>"}`. `fps` = round(1000 / average frame duration). One folder per animation tag (or the whole timeline when no tags).
- `ExportHandoff.Write(Sprite s, string? tag) → string folder`; `ExportHandoff.ListRecent(int max=20) → IReadOnlyList<HandoffInfo>` (newest first; `HandoffInfo { string Folder, Name; int Fps, FrameCount; DateTime When; }`) — this file is shared source: the engine copies the reader part (Task 12).
- Import from EasyEngine: `EasyEngineBridge.ListProjects()` (the engine's Recent list in `%APPDATA%\EasyEngine\settings.json`), `ListSprites(string eeproj) → (object, animation, frames[], fps)` reading only the fields needed, tolerantly (missing/extra keys fine); `ToSprite(...) → Sprite` (one tag per animation).
- Menu: File → "Import sprite from EasyEngine…", File → "Export to EasyEngine" (writes handoff, toast "Now click Import from EasyPixel in EasyEngine"). When EasyEngine isn't found, the import item explains and links `github.com/tarl3s/EasyEngine`.

- [ ] Tests: `Handoff_WriteThenList_RoundTrip`; `Handoff_FpsFromDurations`; `ListSprites_OldProjectMissingKeys_Works`; `ListSprites_MissingAssetFile_SkipsWithWarning`; `EngineNotInstalled_ReturnsNull_NoThrow` (Review Focus 4). Fail → implement → pass → commit `feat: EasyEngine bridge`.

### Task 12: "Import from EasyPixel" in EasyEngine

**Repo:** `engine` (follow its AGENTS.md: no NuGet, `L.T`, contrast, fulltest).
**Files:** Create `EasyPixelBridge.cs` (handoff reader + discovery, same contract as Task 11, plus writing `HKCU\Software\EasyEngine\InstallPath` at startup); modify `AnimationWindow.xaml` (button "🖌 Import from EasyPixel" under "🎨 Draw a sprite") and `AnimationWindow.xaml.cs` (handler next to `DrawSprite_Click`, line ~259); create `FullTest.EasyPixel.cs`; `Lang/*.json`.

Handler: list recent handoffs (newest first) in a picker showing name, frames, fps, time; copy the PNGs into the project's assets folder (unique names, same copy routine used when adding images), then — like `DrawSprite_Click` — add the frames to the selected animation, or create a new animation named after the handoff with its `Fps`. Through the existing undo/backup paths. No handoffs → message "Export from EasyPixel first (File → Export to EasyEngine)"; EasyPixel not found → message with the download link and an "Open folder…" option to pick a handoff folder by hand.

- [ ] Tests in `--fulltest`: `EasyPixelImport_CreatesAnimationWithFps`, `EasyPixelImport_AddsToExisting`, `EasyPixelImport_BrokenJson_Message`. Run `--strings`, add the new strings to every `Lang/<code>.json`; run `--fulltest` and `--selftest` (both themes): all green. Commit in the engine repo (version bump Z → `0.15.3`, per its rules; release at session end when the owner says so).

### Task 13: Release plumbing and v0.1.0

**Files:** `src/EasyPixel/Update/Updater.cs` (port EasyVocal's: checks `releases/latest`, asks, swaps files after exit using a manifest), `tools/Publish-Release.ps1` (self-contained win-x64 single file → `EasyPixel-win-x64.zip` + `easypixel-files.txt` manifest → `gh release create`), `tools/Make-Icon.ps1` + `Assets/EasyPixel.ico`, full README.md/README.pt-BR.md (what it is, download button to `releases/latest/download/EasyPixel-win-x64.zip`, features, EasyEngine integration, MIT).

- [ ] Run all suites (`dotnet test` both projects) → green; manual run; commit `v0.1.0: first release` and annotated tag `v0.1.0`.
- [ ] Creating the GitHub repo, pushing, and publishing the release happen only when the owner says the session is over.
