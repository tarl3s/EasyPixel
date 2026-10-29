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
- Versioning X.Y.Z; `<Version>` lives in `Directory.Build.props` (0.1.0 = stage 1, 0.2.0 = Learn pixel art).
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
| `src/EasyPixel.Core/Learn` | Learn pixel art (see *Lessons*): `Lessons/<lang>/NN-slug.md` (embedded), `LessonParser`, `LessonLibrary` (language → English fallback), `LessonArt` + `LessonArt.More.cs` (illustrations; lessons 04 and 07–18 in the second file), `ArtCanvas` + `Pal` (drawing helpers, the 16 lesson colours), `Exercises` + `Exercise` (practice documents), `Checks.cs` + `Checks.More.cs` (`Rules`, the check rule library) |
| `src/EasyPixel/Learn`, `MainWindow.Learn.cs` | `LearnWindow` (Help → Learn pixel art: lesson list + page), `ArtView` (an illustration at integer zoom, animated ones play), `LessonStrip` (the bar above the canvas during an exercise: goal, self-checks, Check, Back to lesson); progress in `Settings.LessonsRead` / `LessonsPassed` |

## Lessons

Help → **Learn pixel art** opens the Learn window. Each lesson is a page of text with illustrations drawn by EasyPixel
itself and a **Try it** exercise that opens a practice document in the editor; **Check** judges it with simple rules plus
self-check items the person ticks. Design: `docs/superpowers/specs/2026-10-29-learn-pixel-art-design.md`.

### Writing a lesson

One file per lesson and language: `src/EasyPixel.Core/Learn/Lessons/<lang>/NN-slug.md` (e.g. `en/07-anti-aliasing.md`).
The file name (without `.md`) is the lesson id, the same in every language; `NN` sets the order and the number shown.
English is the master list: every lesson needs `en/`; another language (`pt-BR/`, `de/`… the folder name is the `L`
language code) shows its own file when there is one and otherwise English with the note "This lesson isn't translated
yet". New files are embedded automatically (`EasyPixel.Core.csproj` globs `Learn/Lessons/**/*.md`). UTF-8, no BOM.

```text
# Anti-aliasing                         ← title (the first "# " line)
group: fundamentals                     ← or "styles"; metadata lines go right after the title
summary: One line for the lesson list.

## A section heading                    ← "##" (or "###")
A paragraph. Lines join until a blank line. **Bold** works inline.

[[example:jaggies-bad]] Caption under the picture
[[example:jaggies-good|Another caption]]   ← consecutive examples sit side by side
> A tip (consecutive ">" lines join).
! A common mistake (consecutive "!" lines join).
- a bullet item ("-", "*" or "+"; an indented next line continues it)
1. a numbered item (a list is numbered when its first item is)
<!-- a comment, ignored (may span lines) -->

## Try it
[[exercise:clean-lines]]               ← names an Exercises entry; everything until the next heading is the goal
Goal text, as paragraphs or bullets.
- [ ] A self-check item the person ticks before Check passes.
```

The parser never throws: an unknown marker (`[[foo:x]]`), an empty one or an unknown `group` is skipped and listed in
`Lesson.Warnings`. `LearnContentTests` fails when a shipped lesson file (any language) has a warning, a missing title or
summary, no illustration, no Try it, an `[[example:id]]` that `LessonArt` doesn't have or an `[[exercise:id]]` that
`Exercises` doesn't have; a translation must keep the English file's group, exercise and number of self-checks. All 18
lessons of the curriculum are written, and every English lesson needs 600+ words, a tip, a common mistake, 3+
illustrations and self-checks (`EveryLesson_IsSubstantial`); `LearnUiTests.EveryLesson_Opens_AndItsExerciseChecks`
opens each page and starts and checks each exercise. Teach like a tutor: concrete numbers, why, common mistakes, then practice. Refer to real menu paths
(View → Tiled preview, Sprite → Outline, Frame → Preview window, the Pencil's Pixel perfect option).

### Illustrations (`LessonArt`)

Add an entry in `LessonArt.Build()` (or `More()` in `LessonArt.More.cs`): `Add("id", zoom, width, height, c => …)`
draws on an `ArtCanvas` (RGBA): `Px`, `Line` (pixel-perfect by default), `Rect`/`Ellipse` (filled or not), `Arc`
(a pixel-perfect elliptic arc), `Rows(x, y, "..kk..", …)` (one character per
pixel: the letters of `Pal.Letters` — k black, e ebony, n navy, d slate, s silver, w white, p plum, r red, o orange,
y yellow, l lime, g green, t teal, b blue, c sky, a aqua; `.` or space = untouched; an optional legend overrides
letters), `Sphere` (a ball shaded from a light direction with a dark→light ramp), `Outline`, `SelOut` (outline pixels
pick a colour from what they touch), `Shade` (recolour by a function), `Copy`, and `NewFrame(copy, durationMs)` for an
animation (several frames play in the lesson). Use the `Pal` colours, keep good/bad pairs the same size and drawn by the
same code with one difference, and make the zoom so the picture is about 100–300 px wide. Ids in use:
`potion-16/-1x/-2x`, `canvas-sizes`, `mixels-bad/-good`, `orphans-bad/-good`, `jaggies-bad/-good`, `slopes`,
`doubles-bad/-good`, `curve-bad/-good`, `silhouette-good/-bad`, `shape-language`, `pillow-shading`, `light-source`,
`cast-shadow`, `ramp-flat`, `ramp-hue-shift`, `cube-faces`, `outline-none/-black/-colored/-selout`, `inner-lines`,
`dither-checker`, `dither-gradient`, `idle-bob`, `walk-4frame`, `tile-seam-bad/-good`, `tile-repeat-bad`, `tile-single`,
`tileset-edges`, `parallax-layers`, and in `LessonArt.More.cs`: `palette-sizes`, `ramps-shared`, `value-bad/-good`,
`aa-none/-good/-overdone/-halo`, `dither-banding/-smooth`, `dither-ball-good/-noisy`, `materials`, `texture-noise`,
`texture-clusters`, `bounce-ball`, `bounce-spacing`, `squash-stretch`, `onion-skin`, `walk-6frame`, `walk-poses`,
`proportions`, `facing-dirs`, `hero-silhouette`, `atmosphere-bad/-good`, `hills-strip`, `hills-strip-bad`,
`school-side`, `school-34`, `school-top`, `windows-good/-bad`, `school-props`, `icon-set`, `icon-set-1x`,
`icon-detail-bad/-good`, `fx-fire`, `fx-smoke`, `fx-explosion`, `fx-sparkle`, `sheet-layout`, `sheet-tags`,
`frames-aligned-good/-bad`. Reusable pieces: `Slime`, `Walker`/`WalkerPose` (a 14×20 walker at an offset, any leg
pose), `AntiAlias` (coverage-based AA on a circle arc). To look at them as PNGs: set `EASYPIXEL_ART_DUMP=<folder>` and run `dotnet test --filter EveryIllustration_Builds`.
`LearnContentTests.Illustrations_AgreeWithTheRules` checks that good/bad pairs pass/fail the matching rule; add a line
there for a new pair.

### Exercises and check rules

Add an `Exercise` to `Exercises.All`: `Id`, `Width`/`Height`, `Frames` (start frame count), `Guide` (reference marks on
the locked guide layer, drawn at `GuideOpacity`), `Start` (what the drawing layer starts with, e.g. a shape to
outline), `Palette` (the colours offered) and `Rules`. `CreateDocument(guideName, workName)` takes the layer names (the
app passes `L.T("Guide")` / `L.T("Drawing")`) and marks the guide with `Layer.Extra["easypixel:guide"]`
(`Exercise.GuideKey`) and the exercise id `Layer.Extra["easypixel:exercise"]` (`Exercise.ExerciseKey`). .epx keeps
both, and .aseprite keeps every `easypixel:*` layer key as the layer's user-data text (when it has none of its own), so
a saved practice opened again (`SetSession` → `ResumePractice`, via `Exercise.Of(sprite)`) gets its lesson bar back.
`Exercise.Check(sprite)` flattens every layer except the guide (found by the mark, so renaming is harmless; a document
with no marked layer falls back to a layer named "Guide") — one image per frame — and runs each rule (when every
drawing layer is hidden it says only that); check messages say "the drawing layer", never a layer name; a `CheckResult` passes when every `CheckLine` does. The goal
text and the self-check items live in the lesson file, so they are translated with it. The rule library (`Rules.*`):

| Rule | Passes when |
|---|---|
| `NotEmpty(min)` | every frame has at least `min` opaque pixels |
| `MaxColors(n)` / `MinColors(n)` | distinct opaque colours (all frames) ≤ n / ≥ n |
| `KeepsMargin(m)` | nothing within `m` px of the canvas border |
| `FillRatio(min, max)` | opaque share of the canvas is within bounds (silhouette readable, not a blob) |
| `MaxParts(n)` | at most `n` 8-connected pieces (no stray pixels) |
| `NoClumps()` | no 2×2 opaque block (lines one pixel thick) |
| `PixelPerfect(allowed)` | no L-shaped double corners in lines (square corners of straight runs are fine) |
| `HasOutline(color?, share)` | ≥ `share` of the edge pixels (touching transparency) are `color`, or darker than the inside pixel next to them (black, coloured and sel-out all pass) |
| `LightFrom(dir)` | inside pixels on the side facing `dir` are brighter on average than the far side (pillow shading and reversed light fail with their own message) |
| `FullyOpaque()` | no transparent pixel |
| `Seamless(h, v)` | the wrap-around step (last column → first, last row → first) is no harsher than the tile's own texture |
| `HasFrames(n, movement)` | ≥ n frames, each different from the one before |
| `RampOrdered(points, minStep)` | the colours at the points get brighter by ≥ `minStep` (0–255) each |
| `HueShift(dark, light, deg)` | the light swatch's hue is nearer yellow than the dark one's by ≥ `deg` degrees |
| `UsesDither(min)` | at least `min` 2×2 checkerboard cells of two colours |
| `Contrast(min, max)` | the luma between the darkest and lightest colour is within bounds (a value check; `max` keeps far layers hazy) |
| `AntiAliased(line, bg, min)` | ≥ `min` in-between pixels (darker than `bg`, lighter than `line`), each touching the line and within 2 px of a step of the curve (≤ 15 % on straight runs) |
| `GetsLighter(toward)` | four bands across the picture each no darker than the one before, and the last clearly lighter (gradients) |
| `Texture(min, max)` | the share of neighbouring pixel pairs with different colours is within bounds (not flat, not noise) |
| `Bounces(floorY, h)` | the lowest pixel touches row `floorY` in some frame, rises `h` px in another, never goes below |
| `OnFloor(floorY)` | in every frame the lowest pixel is on row `floorY` |
| `InPlace(drift)` | the centre of mass moves sideways at most `drift` px across frames |
| `Bobs(min)` | the top of the drawing changes height by ≥ `min` px between frames |
| `Repeats(n)` | ≥ `n` identical single-colour details (same shape, 2×2 or bigger, smaller than 1/12 of the canvas) |
| `UsesTheSpace(share)` | the drawing spans ≥ `share` of the canvas width or height |
| `GrowsThenFades()` | ≥ 3 frames, the biggest after the first, the last ≤ half the biggest |
| `HasTags(n)` | ≥ `n` tags, all named, no two with one name, every frame inside one |
| `FilledBottom()` | every pixel of the bottom row is opaque |

A new rule derives from `CheckRule` (in `Checks.cs` or `Checks.More.cs`) and writes every message through `Say(true|false, "English with
{0}", args…)` — `tools/Update-Strings.sh` and `LangTests` collect those literals, so after adding one run the script and
translate the new keys in every `Lang/<code>.json`. Point at a place when a rule fails ("e.g. at {2}, {3}"). Each rule
needs a passing and a failing fixture in `LearnRuleTests`, and each exercise a passing answer in
`LearnContentTests.EveryExercise_HasAPassingAnswer` (it runs for every entry of `Exercises.All` and fails for one with no
answer; a fresh document must fail its check). Multi-frame answers draw with `NewFrame`; `game-ready` adds tags.

## Releasing

Every version is an annotated `vX.Y.Z` tag plus a GitHub release (the in-app updater and the README's download button read
`releases/latest`, asset `EasyPixel-win-x64.zip` — never rename it). Bump `<Version>` in `Directory.Build.props`, write
`docs/release-notes/vX.Y.Z.md`, commit `vX.Y.Z: …`, `git tag -a vX.Y.Z`, push with tags, then
`powershell -ExecutionPolicy Bypass -File tools/Publish-Release.ps1 -NotesFile docs/release-notes/vX.Y.Z.md`.
Only with the owner's word; try the packaging first with `-DryRun -OutDir <scratch>`.

## Last known green (v0.2.0)

`dotnet test`: EasyPixel.Tests 344/344, EasyPixel.UiTests 106/106 (includes the contrast audit, the 9-language check,
the end-to-end draw-animate-export test and every lesson and exercise of Learn pixel art). Last packaging check (v0.1.0): `Publish-Release.ps1 -DryRun` builds a 37.7 MB zip with `EasyPixel.exe` +
`easypixel-files.txt`, and the published exe starts.
