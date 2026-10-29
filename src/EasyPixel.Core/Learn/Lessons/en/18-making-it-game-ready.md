# Making it game-ready
group: styles
summary: Sprite sheets, tags for each animation, aligned frames, and sending your art to EasyEngine with Import from EasyPixel.

## From drawing to game
A sprite that looks great in the editor still has to get into the game in a shape the game understands. A game engine needs to know: **which pictures** make up each animation, **in what order**, **how fast**, and **where the character stands** in each frame. This lesson covers the habits that make that step painless, and the direct route from EasyPixel to EasyEngine.

## One file per character, one tag per animation
Keep all the animations of one character in **one document**: idle, walk, jump, attack, hurt, one after another on the timeline. Then name each animation with a **tag**: a named range of frames.

To make a tag, pick its frames on the timeline (click the first, Shift+click the last) and use **Frame → New tag from the picked frames…**. Give it the name the game will use: **idle**, **walk**, **jump**, **attack**. Short, lowercase, no spaces is the safest habit, because these names end up in code and in the game's settings.

[[example:sheet-tags]] One document holding two animations: frames 1–2 tagged "idle" (green), frames 3–6 tagged "walk" (blue).

A tag also has a **direction** (right-click the tag on the timeline): forward, reverse or ping-pong (forward then back, without repeating the ends — perfect for a breathing idle). Every frame should belong to a tag; leftover frames are usually forgotten tests.

> Keep the same canvas size for every animation of a character. If the attack needs a sword sticking out, make the whole canvas wide enough for it, and keep the character in the same place in every frame.

## Aligned frames
In the game, the engine places every frame of the sprite at the **same position**. If your character's feet are on row 20 in one frame and row 21 in the next, the character will jitter up and down while it plays, as if the floor were shaking.

[[example:frames-aligned-good]] Aligned: the feet stay on the same row in every frame, so the walk stays put.
[[example:frames-aligned-bad]] Not aligned: each frame is shifted a pixel, so the walker shakes and slides.

Rules for alignment:

- Choose a **floor row** for the whole character and keep the lowest foot on it in every frame, for every animation (except jumps, where leaving the floor is the point).
- Keep the body's centre over the same column. A walk cycle walks **in place**; the game moves the sprite.
- When you copy a frame to start the next one (**Frame → New frame**), you keep the position for free. Use the onion skin to check.

! Drawing each animation on a different canvas size, or with the character in a different place. In the game, switching from idle to walk makes the character jump sideways. One canvas size, one floor row, one centre for all animations.

## Durations and speed
Each frame has its own duration (**Frame → Frame duration…**). Many engines, and EasyEngine's simple animations, play an animation at **one speed** (frames per second). When you export, EasyPixel turns the average duration of the tagged frames into a speed: 100 ms per frame becomes 10 fps. If your animation relies on holds (a long first frame), check how it looks in the game; you can repeat a frame to make a hold that survives a fixed speed.

## Sprite sheets
Many engines want a **sprite sheet**: all frames laid out in one image, in a row or a grid, plus a description of where each frame is. **File → Export…** → **Sprite sheet (PNG + JSON)** writes both: the PNG with the frames, and a JSON file with each frame's position, duration and your tags.

[[example:sheet-layout]] A horizontal sprite sheet: four walk frames side by side, each in a cell the size of the canvas.

The export options:

- **Layout**: Horizontal (one row), Vertical (one column) or Grid (rows of a fixed number of **Columns**).
- **Padding**: empty pixels between frames. Some engines blur or "bleed" neighbouring frames into each other when they scale the sheet; 1–2 pixels of padding prevents it.

Other export choices in **File → Export…**: a single PNG of the current frame (for a still object like a tile or the school building), or an **animated GIF** to share your animation.

## Exporting to EasyEngine
If you make your game with **EasyEngine**, there is a shortcut: no files to pick, no sheets to cut.

1. In EasyPixel, finish the sprite and its tags, and save it (**File → Save**).
2. Use **File → Export to EasyEngine**. EasyPixel writes **one export per tag** (or one for the whole timeline if there are no tags): the frames as pictures and their speed, in EasyPixel's export folder. It tells you what it exported.
3. In EasyEngine, select the object, open its **Animations** window and click **Import from EasyPixel**.
4. Pick the export from the list (it shows the name, the number of frames, the speed and the time).
5. Choose where the frames go: **Add to** the animation you are editing, or **New animation** with the tag's name.

Your **idle** and **walk** tags become EasyEngine animations with the right frames and speed. To change the art later, edit the same document in EasyPixel, export again and import again.

EasyPixel can also go the other way: **File → Import sprite from EasyEngine…** opens a sprite from an EasyEngine project so you can improve it. EasyPixel never changes the EasyEngine project itself; it only exports, and EasyEngine imports.

## A checklist before exporting
- Every frame belongs to a tag, and each tag has a clear name.
- The feet are on the same floor row in every frame; loops loop without a jump.
- The canvas size is the same for all animations of the character.
- Hidden or helper layers (sketches, guides) are hidden or deleted: exports flatten the **visible** layers.
- You looked at every animation at 1× in **Frame → Preview window**.

## Try it
[[exercise:game-ready]]
The document holds a slime's **idle** (frames 1–2) and a hero's **walk** (frames 3–6). Get it ready for the game: make **two tags** — one for the idle frames and one for the walk frames — with clear names, so **every frame belongs to a tag**. Pick the frames on the timeline, then use **Frame → New tag from the picked frames…**. Keep the feet on the floor row.
- [ ] My tags are named the way the game will call them (like idle and walk).
- [ ] I tried File → Export to EasyEngine (or File → Export… → Sprite sheet) and looked at what it wrote.
