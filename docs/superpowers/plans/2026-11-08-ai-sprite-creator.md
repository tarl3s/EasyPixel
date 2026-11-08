# AI Sprite Creator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** "✨ Create with AI": a description in any language → 4 pixel-art candidates at 16/32/48/64 px → the picked one becomes a new sprite or a new layer, editable by hand.

**Architecture:** Pure, tested pieces in `EasyPixel.Core/Ai/` (pixel-art conversion, prompt building, translation guard, provider clients behind two small interfaces); the Avalonia window and settings in `EasyPixel/Ai/`. Generators return a big picture; `PixelArtConverter` turns it into the sprite, the same for every provider. Tests use fake generators — no network in tests.

**Tech Stack:** C# / .NET 8, Avalonia 11.3, xUnit + Avalonia.Headless; `HttpClient` + `System.Text.Json` only (no new NuGet packages).

**Spec:** `docs/superpowers/specs/2026-11-08-ai-sprite-creator-design.md` (read it first, including *Designer's calls*).

## Global Constraints

- Windows only; Avalonia 11 / .NET 8; no new heavy dependencies; MIT-compatible licences only.
- Every visible text through `L.T` / `L.F`; run `tools/Update-Strings.sh`, then add the entries to all 8 other `src/EasyPixel/Lang/*.json` (the language tests fail otherwise). Edit Lang files with the Edit tool or `[IO.File]::ReadAllText/WriteAllText` (UTF-8, no BOM) — never `Get-Content | Set-Content`.
- API keys: never logged, never in exception messages shown to the person, never sent to any host but their own provider, never committed. EasyPixel ships no key and runs no server.
- Network calls never on the UI thread; every call has a timeout (90 s image, 30 s text) and honours a `CancellationToken`.
- Sizes offered: 16, 32, 48, 64 (square) or the open canvas's size; default 32. Candidates: 4. Default colour count: 12 (range 4–32).
- Version: **v0.3.0** — bumped in `Directory.Build.props` (and `tests/EasyPixel.Tests/Smoke.cs`), but **not released** until the owner approves the contact sheet (Task 7).
- Code, comments, UI text in English; replies to the owner in PT-BR.

## Review Focus

1. A generated picture whose background isn't flat (gradient, scenery, a frame) — expect a usable sprite or the candidate dropped with a note, never a full-canvas rectangle of background. → Task 1 test `Convert_NoisyBackground_StillIsolatesSubject`.
2. A provider returning something that isn't an image (HTML error page, JSON error, empty body, a 1×1 picture) — expect that slot to show a short error and the other slots to keep working. → Task 5 test `FailedCandidate_ShowsError_OthersStillArrive`.
3. Cancel while generating, or closing the window mid-request — no exception, no late result written into the sprite. → Task 5 test `Cancel_StopsAndAddsNothing`.
4. A prompt that is empty, only spaces, or enormous (paste of 10 000 chars) — Generate disabled for empty; long text cut to 1 000 chars before sending. → Task 5 test `EmptyOrHugePrompt`.
5. Translation that drops meaning (a number, a quoted name) or a provider that answers with chatter around the translation — fall back to the original text. → Task 2 tests `Clean_RejectsLostNumber`, `Clean_StripsChatter`.

---

### Task 1: Pixel-art conversion (Core)

**Files:**
- Create: `src/EasyPixel.Core/Ai/PixelArtConverter.cs`
- Test: `tests/EasyPixel.Tests/PixelArtConverterTests.cs`

**Interfaces:**
- Consumes: `Image` (`Model/Cel.cs`, packed RGBA `uint`, `Rgba.FromPacked`/`.Packed`), `Quantize.MedianCut(Image,int)`, `Quantize.ToPalette(Image,Palette,bool dither)`, `Png.Decode(byte[])`.
- Produces:
  - `public sealed record ConvertOptions(int Size, int MaxColors = 12, IReadOnlyList<Rgba>? Palette = null, bool Outline = true);`
  - `public static class PixelArtConverter` with `public static Image? Convert(Image big, ConvertOptions o)` (null when no subject is found) and the steps, each public for tests and tuning: `Image RemoveBackground(Image)`, `PixelRect SubjectBounds(Image)`, `Image Downscale(Image src, PixelRect from, int size)` (bottom-centred, 1-px margin), `Image LimitColors(Image, int max, IReadOnlyList<Rgba>? palette)`, `void Cleanup(Image)`, `void AddOutline(Image)`.

Algorithm choices the tests pin (bodies are yours): background = the dominant colour of the 1-px border, flooded from the border with tolerance 40 (RGB distance), then only the largest 8-connected opaque blob is kept, blobs < 2 % of it dropped; downscale picks, per target cell, the **most frequent** opaque colour of its source cell (transparent if < 40 % opaque) — never an average; colours = median cut to `MaxColors`, no dither, or nearest of `Palette`; cleanup makes alpha 0 or 255 and removes opaque pixels with no opaque 4-neighbour; outline = 1-px outside ring, each pixel the darkest neighbouring colour darkened 50 % (sel-out).

- [ ] **Step 1: Write the failing tests** — `Convert_RedCircleOnWhite_GivesTransparentBgAndOutline` (512² white with a red disc → 32² result: corners transparent, centre red-ish, ≤ 12 colours, every alpha 0 or 255, a dark pixel next to the edge of the disc); `Downscale_KeepsSmallFeature` (a 16×16 black "eye" on a 512² grey body survives at 32² as ≥ 1 dark pixel); `Convert_UsesGivenPalette` (every result colour ∈ palette ∪ outline shades of it); `Convert_EmptyPicture_ReturnsNull`; `Convert_NoisyBackground_StillIsolatesSubject` (white bg with ±20 random noise + a blue square → result corners transparent, blue present); `Convert_SubjectFeetAtBottom` (subject's lowest opaque row = size − 2).
- [ ] **Step 2:** `dotnet test tests/EasyPixel.Tests --filter PixelArtConverter` → FAIL (type missing).
- [ ] **Step 3:** Implement `PixelArtConverter` as specified above.
- [ ] **Step 4:** Same command → PASS; then full `dotnet test` → all green.
- [ ] **Step 5:** Commit `AI: pixel-art conversion of a generated picture`.

### Task 2: Prompt building and translation guard (Core)

**Files:**
- Create: `src/EasyPixel.Core/Ai/SpritePrompt.cs`, `src/EasyPixel.Core/Ai/AiTranslate.cs` (port of `engine/AiTranslate.cs`: `LooksEnglish`, `Clean`, `ToEnglishAsync` — read that file; keep its rules)
- Test: `tests/EasyPixel.Tests/SpritePromptTests.cs`

**Interfaces:**
- Produces:
  - `public enum SpriteView { Auto, Side, Front, TopDown }`
  - `SpritePrompt.ResolveView(string englishRequest, SpriteView chosen) -> SpriteView` (Auto: words for objects/items/buildings — chest, door, potion, sword, coin, house, building, school, tree, sign, icon, item… → Front; otherwise Side).
  - `SpritePrompt.Build(string englishRequest, SpriteView view) -> string` — request first, then the style template: "single game sprite, full body, {side view|front view|top-down view}, centred, plain flat white background, pixel art, bold dark outline, limited palette, simple shading, light from top-left, Terraria and Stardew Valley style, no text, no frame, no shadow on the ground". Request text cut to 1 000 chars.
  - `AiTranslate.LooksEnglish(string) -> bool`, `AiTranslate.Clean(string original, string reply) -> string?`, `AiTranslate.ToEnglishAsync(string request, Func<string, string, CancellationToken, Task<string>> ask, CancellationToken ct) -> Task<string>` (returns the original on any failure).

- [ ] **Step 1: Failing tests** — `ResolveView_DeerMonsterIsSide`, `ResolveView_ChestIsFront`, `ResolveView_ExplicitChoiceWins`, `Build_HasRequestAndStyle` (contains the request, "pixel art", "side view"), `Build_CutsHugeRequest` (10 000-char request → ≤ 1 000 chars of it present); `LooksEnglish_PortugueseDeerIsFalse` (the owner's sentence: "faz um monstro parecido com um cervo, que anda em duas patas, as traseiras; ele tem a boca suja de sangue, olho arregalado e parece estar sempre procurando algo para atacar"); `Clean_RejectsLostNumber` ("um lobo com 3 olhos" + reply without 3 → null); `Clean_StripsChatter` (reply `Here is the translation: "a wolf"` → `a wolf`); `ToEnglish_FailingAskReturnsOriginal`.
- [ ] **Step 2:** run filter `SpritePrompt` → FAIL.
- [ ] **Step 3:** Implement both files.
- [ ] **Step 4:** PASS; full suite green.
- [ ] **Step 5:** Commit `AI: sprite prompt and translation to English`.

### Task 3: Providers (Core)

**Files:**
- Create: `src/EasyPixel.Core/Ai/Providers.cs` (interfaces + registry), one file per provider in `src/EasyPixel.Core/Ai/Providers/`, `docs/ai-providers.md` (what was verified, with dates and links)
- Test: `tests/EasyPixel.Tests/ProviderTests.cs`

**Interfaces:**
- Produces:
  - `public interface IImageGenerator { string Id { get; } string Name { get; } bool NeedsKey { get; } bool Free { get; } int MaxParallel { get; } Task<byte[]> GenerateAsync(string prompt, int seed, string? key, CancellationToken ct); }` — returns PNG/JPEG bytes; throws `AiException(string friendlyMessage)` on any failure (message never contains the key).
  - `public interface ITextAi { string Id { get; } bool NeedsKey { get; } Task<string> AskAsync(string system, string user, string? key, CancellationToken ct); }`
  - `public static class AiProviders { IReadOnlyList<IImageGenerator> Images; IReadOnlyList<ITextAi> Texts; IImageGenerator? Image(string id); ITextAi? Text(string id); }`
  - `public sealed class AiException(string message) : Exception(message)`
  - `ImageDecode.Load(byte[]) -> Image` (PNG via `Png.Decode`; JPEG/WebP through Avalonia is not available in Core — if a provider can only return JPEG, ask it for PNG or decode in the app layer through `Avalonia.Media.Imaging.Bitmap`; record the choice in `docs/ai-providers.md`).

- [ ] **Step 1: Research (no code).** Verify today, from each provider's own docs, and write `docs/ai-providers.md`: (a) at least one **keyless free** image endpoint (candidate: Pollinations image API) and one keyless free text endpoint for translation (candidate: Pollinations text); (b) one **free-key** image provider a person can sign up for (candidates: Cloudflare Workers AI flux/SDXL, Google Gemini image, Hugging Face); (c) one **paid** general model (candidate: OpenAI image) and a **pixel-art specialised** paid one if it exists (candidate: Retro Diffusion); (d) for local: whether a workable Windows + AMD route exists (stable-diffusion.cpp Vulkan, ComfyUI DirectML) — if not, the local option ships as "coming soon". For each: URL, auth, request/response shape, size options, rate limits, terms that allow this use.
- [ ] **Step 2: Failing tests** with a fake `HttpMessageHandler` (inject `HttpClient` via an internal constructor): each provider builds the documented request (URL, headers, body), returns the bytes of a 200 image response, and turns 401/429/500/HTML/empty bodies into `AiException` whose message has no key; `AiProviders.Images` contains ≥ 1 keyless free generator first.
- [ ] **Step 3:** Run filter `ProviderTests` → FAIL.
- [ ] **Step 4:** Implement the providers chosen in Step 1.
- [ ] **Step 5:** PASS; full suite green. Commit `AI: image and text providers`.

### Task 4: AI settings

**Files:**
- Modify: `src/EasyPixel/Settings.cs` (add `public string AiImage { get; set; } = "auto";`, `public string AiText { get; set; } = "auto";`, `public Dictionary<string, string> AiKeys { get; set; } = new();`)
- Create: `src/EasyPixel/Ai/AiSettingsWindow.cs`; Modify: `src/EasyPixel/Dialogs/IDialogs.cs` + `AppDialogs.cs` + `tests/EasyPixel.UiTests/FakeDialogs.cs` (`Task<AiChoice?> AskAiSettings(AiChoice current)`, `public sealed record AiChoice(string Image, string Text, IReadOnlyDictionary<string, string> Keys);`), `MainWindow.axaml` (Edit → "AI settings…"), `MainWindow.View.cs` (handler)
- Test: `tests/EasyPixel.UiTests/AiSettingsTests.cs`

**Interfaces:**
- Consumes: `AiProviders` (Task 3).
- Produces: `AiRouting.ImageGenerators(Settings) -> IReadOnlyList<IImageGenerator>` ("auto" = every free generator that needs no key or has a key, keyless first; a chosen id = that one) and `AiRouting.TextAi(Settings) -> ITextAi?`, in `src/EasyPixel/Ai/AiRouting.cs`.

The window lists generators (name, Free/Paid, "needs a key"), a key box per provider that needs one (masked, with **Test** that makes one cheap call and shows ✓/✗), the sign-up link from `docs/ai-providers.md` for the free-key provider, and the local option labelled "On my computer (slow on most graphics cards)" or "coming soon".

- [ ] **Step 1: Failing tests** — `AiSettings_SavedAndReloaded` (keys round-trip through `Settings.Save/Load`); `Routing_AutoPutsKeylessFirst`; `Routing_SkipsKeyProviderWithoutKey`; `Keys_NeverInStatusOrErrors` (a fake provider throws with the key in its message; the shown text doesn't contain it).
- [ ] **Step 2:** run → FAIL. **Step 3:** implement. **Step 4:** PASS + full suite.
- [ ] **Step 5:** strings + 8 translations; commit `AI: settings and provider choice`.

### Task 5: Create with AI window

**Files:**
- Create: `src/EasyPixel/Ai/CreateWithAiWindow.cs`, `src/EasyPixel/Ai/AiCreateSession.cs` (logic without UI: translate → prompt → up to 4 requests honouring `MaxParallel` → decode → convert; raises `CandidateReady(int slot, Image? sprite, string? error)`), `src/EasyPixel/MainWindow.Ai.cs`
- Modify: `MainWindow.axaml` (File → "✨ Create with AI…", and a ✨ button on the tool strip), `IDialogs.cs`/`AppDialogs.cs`/`FakeDialogs.cs` (`Task<AiPick?> CreateWithAi(AiCreateRequest start)`; `public sealed record AiCreateRequest(int CanvasSize, IReadOnlyList<Rgba>? SpritePalette);` `public sealed record AiPick(Image Sprite, bool AsNewLayer);`)
- Test: `tests/EasyPixel.UiTests/CreateWithAiTests.cs` (fake generator returning a 512² PNG of a coloured shape; fake text AI)

**Interfaces:**
- Consumes: `PixelArtConverter.Convert`, `SpritePrompt`, `AiTranslate.ToEnglishAsync`, `AiRouting`, `ImageDecode.Load`.
- Produces: `MainWindow.CreateWithAiAsync()`; `AiCreateSession(IReadOnlyList<IImageGenerator> gens, ITextAi? text, Func<string, string?> keyFor)` with `Task RunAsync(string request, int size, SpriteView view, IReadOnlyList<Rgba>? palette, CancellationToken ct)`; on a generator's `AiException` it tries the next generator for that slot, and only when all fail reports the error.

Window: description box, Size (16/32/48/64/canvas), View (Auto/Side/Front/Top-down), Palette (Automatic / This sprite's palette / From a file…), Generate, 4 slots (big nearest-neighbour + 1×), "4 more", "Use as new sprite", "Add as a new layer" (enabled when a sprite is open), Cancel. Palette rule from the spec: this sprite's palette, else its used colours, < 3 colours → automatic + note.

- [ ] **Step 1: Failing tests** — `Generate_FillsFourSlots`; `UseAsNewSprite_OpensSizedSprite` (32² document, opaque pixels present); `AddAsLayer_OneUndoStep` (new layer on the current frame; Ctrl+Z removes it); `AddAsLayer_LargerThanCanvas_ScaledDown` (48 on a 16² canvas → fits 16²); `PortugueseRequest_IsTranslatedBeforeGenerating` (fake text AI sees the PT text, fake generator sees the English one); `FailedCandidate_ShowsError_OthersStillArrive`; `Cancel_StopsAndAddsNothing`; `EmptyOrHugePrompt`; `SpritePalette_FewerThan3Colours_FallsBackWithNote`.
- [ ] **Step 2:** run → FAIL. **Step 3:** implement. **Step 4:** PASS + full suite.
- [ ] **Step 5:** strings + translations; commit `AI: Create with AI window`.

### Task 6: Quality contact sheet

**Files:**
- Create: `src/EasyPixel/Ai/AiSheet.cs` (command line `EasyPixel.exe --ai-sheet <out.png> [--provider id]`, handled in `App.axaml.cs` before the main window), `docs/ai-test-prompts.txt` (the 12 prompts from the spec, the deer-monster verbatim in Portuguese, the wolf in Spanish)
- Test: `tests/EasyPixel.UiTests/AiSheetTests.cs` (fake generator)

**Interfaces:**
- Consumes: `AiCreateSession`.
- Produces: a PNG grid — one row per prompt, at 32 and 48 px: each candidate at 1× and 6×, the prompt text under the row — plus a text report of the automatic checks per candidate (transparent around subject, ≤ colour count, binary alpha, outline present, subject fills ≥ 30 % of the canvas height, no orphan pixels) in `<out>.txt`.

- [ ] **Step 1: Failing test** `Sheet_HasARowPerPrompt_AndReport` (fake generator; 12 rows; report lists 12 × 2 × 4 candidates with their check results).
- [ ] **Step 2:** FAIL. **Step 3:** implement. **Step 4:** PASS + full suite.
- [ ] **Step 5: Real run** on the free provider(s): `--ai-sheet <scratchpad>/sheet.png`; look at it; tune `PixelArtConverter` defaults and the style template until it reads as Terraria/Stardew (commit each tuning with before/after notes). Commit `AI: quality contact sheet`.

### Task 7: Finish and hand to the owner

**Files:**
- Modify: `Directory.Build.props` (`<Version>0.3.0</Version>`), `tests/EasyPixel.Tests/Smoke.cs`, `README.md` (and the PT-BR README if present) — a short "Create with AI" section; create `docs/release-notes/v0.3.0.md` (player-facing, plain words).

- [ ] **Step 1:** Full suite green; `/code-review` and `/simplify` on the branch; fix what they find.
- [ ] **Step 2:** Show the owner the contact sheet (path in the scratchpad) and the test build (`dotnet build src/EasyPixel -c Release -o %TEMP%\EasyPixelTry`, start only that copy). **Do not tag, push or release** until the owner approves the sheet and says to publish.
- [ ] **Step 3:** After approval: commit `v0.3.0: Create with AI`, annotated tag `v0.3.0`, push, `tools/Publish-Release.ps1 -NotesFile docs/release-notes/v0.3.0.md`.
