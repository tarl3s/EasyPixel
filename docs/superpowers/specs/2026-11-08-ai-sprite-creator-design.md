# AI sprite creator — design (EasyPixel v0.3.0)

Date: 2026-11-08. Owner: tarl3s (speaks Brazilian Portuguese; UI and code are English, translated through `L.T`).

## Intent

The owner makes games in EasyEngine and is not good at drawing. They want to type a plain description of a sprite —
in any language — and get **good pixel art**, not scribbles, that they then finish by hand in EasyPixel's editor.

Their real example (Portuguese, paraphrased): *"make a monster that looks like a deer, walks on two legs (the hind
ones), has a blood-stained mouth, wide-open eyes, and always seems to be looking for something to attack."*

"Good" means **Terraria / Stardew Valley level**: small, readable sprites (enemies around 32–48 px) where you can tell
what each thing is at a glance by its silhouette, with a dark outline, few colours per object, simple shading from one
light direction, no blur, noise or excess detail. Undertale's look is explicitly *not* the target.

This is the first stage of EasyPixel's AI (roadmap: editor → lessons → AI). Later stages — edit a sprite by text,
continue an animation — are **not** part of this design.

## Goals

1. **Create from scratch**: a prompt in any language → 4 candidate sprites at the chosen size → the person picks one
   (or asks for 4 more) → it lands in the editor, fully editable.
2. **Quality comes from our pipeline, not only from the model.** Whatever generator is used, the result goes through
   EasyPixel's own pixel-art conversion so it reads as hand-made pixel art (see *Pixel-art conversion*).
3. **Works for everyone, free**: a free cloud option needing no key, plus paid options with the person's own key, plus a
   local option for people with a strong GPU.
4. **Any language**: the request is translated to English before generation (the approach that worked in EasyEngine:
   `engine/AiTranslate.cs` — detect if already English, translate with a short call, refuse a translation that lost a
   number or a quoted text and fall back to the original).

## Non-goals (this version)

- Editing an existing sprite by text; continuing / generating animation frames; tilesets or backgrounds made by AI;
  AI inside the lessons. (Later versions.)
- Training or shipping our own model.

## Anti-goals (would make it a failure even if it "works")

- Output that looks like a shrunken photo or a blurry painting: anti-aliased edges, hundreds of colours, noise, stray
  pixels, a background baked into the sprite.
- A subject that isn't readable at its size (the deer-monster's eyes and bloody mouth lost in mush).
- Anything that blocks the editor while waiting, or a crash / lost work when a service fails or is offline.
- Keys leaking: API keys are never logged, never sent anywhere but their own provider, never committed.

## The person's flow (agreed with the owner)

1. **"✨ Create with AI"** (menu entry and a toolbar button). A window asks for:
   - the description (free text, any language);
   - the size: 16, 32, 48 or 64 px (square), or "the open canvas's size" — default 32;
   - optionally a palette: none (automatic), the open sprite's palette, or a palette file (reusing palette loading,
     which already reads .gpl/.hex/.pal/.ase/.aco/.act/.txt and pictures).
2. **Generate** shows progress (cancellable; the editor stays usable) and then **4 candidates** side by side, already
   converted to pixel art, shown big (nearest-neighbour zoom) and at 1×.
3. The person picks one, or presses **"4 more"** (same description) — they can also tweak the text and regenerate.
4. **Use it**: as a **new sprite**, or as a **new layer in the open sprite** (centred). Transparent background, outline,
   limited colours. From there it is ordinary editing; one undo step when added to an existing sprite.
5. **Settings → AI**: choose the generator — "Automatic (free)", a paid provider with the person's own key, or "On my
   computer (slow)". Keys stay on this computer (EasyPixel settings file in `%APPDATA%`/`%LOCALAPPDATA%`, same place as
   other settings).

## How it is made (decided)

- **Generators are image models, not text models drawing pixels.** A text LLM placing pixels gives scribbles; image
  models give good shapes, and our conversion makes them pixel art.
- **Cloud is the main path** (owner's GPU is an RX 550 4 GB — local generation would be minutes per image; most users
  are similar). Local is an option, labelled slow.
- **Prompting**: the English request is wrapped with a style template, e.g. "single game sprite, full body, side view,
  centred, plain flat background, pixel art, bold dark outline, limited palette, simple shading, light from top-left,
  Terraria / Stardew Valley style, no text, no frame". The exact template is tuned by measurement (see *Quality bar*).
- **Translation** uses a text model (the same cloud/local choice as the person's AI settings where possible; a free
  option must exist). If translation fails, the original text is used.

## Pixel-art conversion (the core; runs locally, all providers)

From a large generated image (typically 512–1024 px) to a clean N×N sprite:

1. **Remove the background**: the prompt asks for a flat plain background; detect it from the border colour(s) and
   flood it to transparent (tolerance), keeping the largest subject blob; drop small detached specks.
2. **Crop and fit**: crop to the subject's bounds, scale to fit the target size keeping proportions, bottom-centred
   (feet on the ground), with a 1-px margin for the outline.
3. **Downscale** with a pixel-art aware method (e.g. per-cell dominant / median colour rather than averaging, which
   blurs), so small features like an eye survive.
4. **Limit colours**: quantise to a small palette (default around 8–16 colours, chosen by the image; or map to the
   chosen palette if the person picked one).
5. **Clean up**: remove orphan pixels and 1-px noise, make alpha binary (no semi-transparent pixels).
6. **Outline**: add a dark outline around the silhouette (a darkened shade of the neighbouring colour, sel-out style,
   or black — builder's call after measuring which reads better).

Each step lives in `EasyPixel.Core` as pure functions over `Image`, unit-tested, so it can be tuned and reused later
(edit-by-text and animation will need it too).

## Providers (left to the builder, with constraints)

The builder researches what is available **at build time** (free tiers and model ids change; verify with the
providers' current docs) and picks:

- **Free, no key** (the "Automatic" default): at least one; if it is rate-limited or down, fall back to the next free
  one, then tell the person plainly.
- **Paid / BYO key**: at least one general image model and, if available, a pixel-art specialised one. Keys entered in
  Settings → AI, checked with a cheap call ("Test").
- **Local**: an option that runs on the person's machine; on weak GPUs it says it will be slow before starting.
- Network calls never run on the UI thread; every call has a timeout and cancel; errors are shown in plain words.

## Quality bar (how "good, not scribbles" is checked)

- A **fixed test set** of ~12 prompts covering the owner's needs (the deer-monster verbatim in Portuguese, a knight,
  a slime, a bat, a tree, a chest, a potion, a door, a school building front, a wolf in Spanish, an item icon, a
  skeleton), each at 32 px and 48 px.
- An **eval mode** (headless or a dev menu) that runs the set on a provider and saves a contact sheet PNG (each
  candidate big and at 1×), so the owner and the builder can judge by eye.
- **Automatic checks** on every candidate: transparent background around the subject, ≤ chosen colour count, no
  semi-transparent pixels, an outline present, the subject fills a reasonable part of the canvas, no orphan pixels.
- The pipeline is not done until the owner looks at the contact sheet and agrees it is Terraria/Stardew-like.

## Constraints

- Avalonia 11 / .NET 8, Windows only; no new heavy dependencies without reason (HTTP + JSON from the BCL is enough for
  cloud calls). MIT-compatible licences only.
- Every visible text goes through `L.T` and is added to all 9 `Lang/*.json` (the language tests enforce it).
- Tests: Core tests for the conversion steps and the translation guard; UI tests with a fake generator (no network in
  tests) for the window flow: generate → 4 candidates → new sprite / new layer → undo.
- Version: new system → **v0.3.0**.

## Designer's calls (settled after the builder check; the owner can override)

- **View**: a "View" choice in the window — Automatic (default), Side, Front, Top-down. Automatic picks from the
  request (creatures and characters → side; objects, items, buildings, doors, chests → front). The style template
  takes the view as a parameter.
- **Free default without a shipped key**: EasyPixel never ships a key and runs no server. "Automatic (free)" uses
  keyless free services; if none answers, it says so and offers a free-key provider the person signs up for
  themselves (with a short how-to and a link), stored like any other key.
- **Text AI for translation**: EasyPixel has no AI code yet. Port what is needed from EasyEngine's `CloudClient`,
  `AiSettings`, `OllamaClient` and `AiTranslate` into EasyPixel (copy, adapted to Avalonia and EasyPixel's settings);
  the two repos don't share code.
- **Four candidates**: requests go out in parallel where the provider allows, one at a time otherwise; each candidate
  appears in its slot as soon as it arrives. "4 more" works the same.
- **"The open sprite's palette"**: its palette if it has colours (transparent entry excluded); otherwise the colours
  its pixels use; fewer than 3 colours → fall back to automatic with a short note.
- **New layer in an animated sprite**: the picture goes on the current frame of the new layer (one undo step). If the
  sprite is smaller than the chosen size, the picture is scaled down to fit (nearest neighbour), never cropped.
- **Quality gate**: v0.3.0 is released only after the owner has looked at the contact sheet and agreed. The eval
  uses the free providers by default; a paid provider only when the owner says so (it costs their money).

## Left to the builder

Exact providers and models; the style template wording; downscale / quantise / outline algorithms and their defaults
(tuned on the contact sheet); window layout details; how the local option is installed/started (or whether it ships
as "coming soon" if no workable local route exists on Windows/AMD — say so to the owner).
