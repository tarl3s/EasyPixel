# EasyPixel — design

Date: 2026-10-28. Status: draft for the owner's approval.

## Intent

EasyPixel is an advanced pixel-art / sprite editor, a sibling app of EasyEngine. The owner needed a school building
for their game and is bad at pixel art; EasyEngine's built-in sprite editor is small and fixed-size. EasyPixel exists
so people who aren't artists can still get good sprites: draw them with a full professional toolset, later learn how,
and later still describe them to an AI and refine the result by hand.

It is **standalone in both directions**: EasyPixel works without EasyEngine, and EasyEngine works without EasyPixel.
When both are installed they find each other automatically and exchange sprites with one click.

## Goals (in delivery order)

1. **Hand editor (v1)** — every editing tool found in Aseprite and Piskel, any image size, layers, animation frames.
   Plus the EasyEngine integration and the file formats below.
2. **Learn pixel art (v2)** — a section that teaches pixel art from zero to good. Designed later.
3. **AI (v3)** — designed in detail later; what is decided now:
   - **create** a sprite from a description; **edit** an existing one by text ("make the roof blue", "add a window"),
     changing only what was asked; **continue an animation** from a frame or sequence ("make it walk", "4 more frames of
     the door opening");
   - quality bar: real, clean pixel art — "not a 5-year-old's Paint drawing";
   - everything the AI produces is ordinary pixels/layers/frames, editable by hand afterwards (each AI step is one undo
     step — builder's call, owner may revise);
   - same provider scheme as EasyEngine/EasyVocal: **local** (free, runs on the PC) and **cloud** (free tiers with
     automatic fallback + paid providers with the user's own key), chosen in Settings.
   - Builder's note (my call, not the owner's): image models produce fake, blurry "pixel art"; a post-processing step
     (downscale to the real pixel grid, palette quantization, background removal, outline cleanup) is expected to be
     what makes the quality bar reachable.

## Non-goals (for now)

- macOS / Linux. **Windows only for now**, other platforms later (owner's decision).
- The Learn section and the AI in v1.

## Anti-goals

- AI output that looks like blurry generated art instead of pixel art.
- Locking either app to the other (neither may require the other to be installed).
- A toy editor: if v1 lacks tools people expect from Aseprite/Piskel, it has failed its purpose.

## v1 scope

### Editor
- Canvas of **any size** (not limited like EasyEngine's sprite editor), zoom/pan, pixel grid.
- Feature parity with the tools of Aseprite and Piskel. The builder should compile the concrete list from both apps'
  documentation; it includes at least: pencil, eraser, bucket (contiguous/global), shapes (line, rectangle, ellipse,
  filled/outline), selection tools (rectangle, ellipse, lasso, magic wand) with move/transform, color picker,
  mirror/symmetry drawing, dithering, shading/lighten-darken, outline, flip/rotate/resize, palettes, layers (opacity,
  visibility, blend modes), frames/timeline with per-frame duration, onion skin, animation preview, tags/loops,
  tilemap/tiled mode, undo/redo, keyboard shortcuts.
- Export: PNG, sprite sheet (+ frame data), animated GIF.

### File formats
- Its own project format (layers + frames + palette).
- **Open and save** `.aseprite` and `.piskel`.
- Open other widely used formats — builder's proposal: PSD (layers), animated GIF, PNG sprite sheets (slice into
  frames), `.pyxel` (Pyxel Edit), `.ora` (Krita/GIMP). The owner may add more.

### EasyEngine integration
- In EasyEngine, where images/animations are added to a sprite (the Animations window, next to "🎨 Draw a sprite"):
  button **"Import from EasyPixel"**.
- In EasyPixel: button **"Import sprite from EasyEngine"** (pick a project, object/animation, edit it).
- They detect each other automatically when both are installed; when the other app isn't installed, the button
  explains how to get it (builder's call on exact behavior).
- An animation travels with its frames and speed (EasyEngine animations are a list of frame images + `Fps`).
- **Return trip (owner's decision): EasyPixel only exports.** It never writes into an EasyEngine project; the person
  brings the result back with "Import from EasyPixel" in the engine. EasyPixel reads `.eeproj` only to import.

### Delivery and versioning (owner's decision)
- The hand editor ships **in stages**, not all at once: first the essentials (drawing tools, selection, layers,
  animation/timeline, onion skin, `.aseprite`/`.piskel`, export, EasyEngine integration), then the rest in updates
  (tilemap mode, PSD/`.pyxel`/`.ora` import, remaining tools). Exact cut is the builder's call.
- Same X.Y.Z scheme as EasyEngine: X = big structural change, Y = a new tool/system, Z = fixes and improvements;
  every version an annotated tag + GitHub release.

### Decisions made by the designer (owner may revise)
- `.aseprite` round-trip: lossless for everything EasyPixel supports; if a file holds something it doesn't, it warns
  before saving.
- Colour: RGBA documents plus a true indexed-palette mode, as in Aseprite.
- "Pixelate image" and "reduce to palette" ship in the editor as normal tools; v3's AI cleanup reuses them.


## Constraints / conventions (inherited from the family)

- **Open source and free for everyone** (owner): MIT license, public repo, no paid tiers, accounts or ads. (Paid AI
  providers in v3 are only the user's own optional keys.)

- Same family as EasyEngine/EasyVocal: English UI translated into the same 9 languages, dark/light themes, contrast
  tested, public GitHub repo (MIT) with releases and in-app updater, PT-BR README alongside the English one.
- Never touch users' existing files unexpectedly; EasyEngine's rules (AGENTS.md) apply to any change made in the engine.

## Left to the builder

- **UI framework** — my recommendation: Avalonia (.NET 8), like EasyVocal, because other platforms are planned;
  shipping Windows-only first. (WPF would match EasyEngine but would need a rewrite to go cross-platform.)
- Repo layout (own repo `EasyPixel`, own folder next to `engine` and `EasyVocal`), test strategy, how the apps discover
  each other (e.g. a registry/settings entry each writes on install), exact export options.
