# Learn pixel art — design (EasyPixel v0.2.0)

Date: 2026-10-29. Owner request (PT-BR, paraphrased): "do the lessons too, complete — lighting, colour palettes,
outline, etc. — and also sprite styles like tileset, sprites, background." Owner was away; the designer made the calls
below (marked) so work could continue.

## Intent

Someone who is bad at pixel art (the owner, drawing a school for their game) opens **Learn pixel art** and goes from
zero to making good sprites, tiles and backgrounds — learning by reading short illustrated explanations and practising
inside the real editor.

## What a lesson is

- A short page: sections of plain text, each with **illustrated examples drawn by EasyPixel itself** (good vs bad,
  before/after), zoomable, so no external art is needed and the art is MIT like the rest.
- A **"Try it" exercise** that opens a prepared practice document in the editor (a guide layer with instructions and a
  locked reference, a work layer to draw on). The person draws, then presses **Check**.
- **Check** gives friendly feedback from simple automatic rules where a rule can judge the result (e.g. "every edge
  pixel has an outline", "you used at most 8 colours", "the tile's left and right edges match"). Where no rule can judge
  art, Check compares against the lesson's goals as a checklist the person ticks themselves (designer call).
- Progress (lesson done / exercise passed) is saved in settings; the Learn window shows it.

## Curriculum (designer call; owner named lighting, palettes, outline, tileset, sprites, background)

Fundamentals
1. Pixels and canvas size — the grid, choosing 16/32/64 px, zoom, reading at 1×.
2. Lines and curves — pixel-perfect lines, clean slopes (1:1, 2:1, 3:1), avoiding jaggies.
3. Shapes and silhouettes — readable shapes, silhouette test.
4. Colour palettes — limited palettes, colour ramps, hue shifting, contrast and value.
5. Light and shading — light source, form shadows, cast shadows, highlights, avoiding pillow shading.
6. Outlines — black, coloured and selective outlines (sel-out), inner lines.
7. Anti-aliasing — manual AA on curves, when not to.
8. Dithering — patterns, gradients, textures, when it hurts.
9. Textures and materials — metal, wood, stone, cloth, grass.
10. Animation basics — frames, timing, onion skin, squash and stretch, idle bob.
11. Walk cycle — 4- and 6-frame walks, contact/passing poses.

Styles (what a game needs)
12. Character sprites — proportions (chibi vs realistic), 16/32 px characters, facing directions.
13. Tilesets — grid size, seamless tiles, edge/corner tiles, testing in tiled preview.
14. Backgrounds and parallax — layered depth, atmospheric perspective, lower contrast far away.
15. Buildings and props — perspective for top-down vs side view (the owner's school!), windows, doors, repetition.
16. Items and icons — readability at small sizes, consistent lighting across a set.
17. Effects — fire, smoke, explosions, sparkles as short animations.
18. Making it game-ready — sprite sheets, tags, exporting to EasyEngine.

## Languages

English and Brazilian Portuguese lesson text ship complete (designer call: the owner reads PT-BR; lessons are long and
translating ~10k words into 7 more languages is a separate job). Other languages show the English lesson text with
the UI around it translated, and a one-line note. Lesson content lives in `Lessons/<lang>/` (not `Lang/*.json`).

## Non-goals

AI in lessons (v3); video; online content; grading art quality beyond simple rules.

## Versioning

New system → **v0.2.0** (Y bump).
