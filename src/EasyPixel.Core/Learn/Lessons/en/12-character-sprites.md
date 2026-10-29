# Character sprites
group: styles
summary: Proportions (chibi vs realistic), 16 and 32 px heroes, facing directions, and readable characters.

## The character comes first
The player looks at the hero more than at anything else in the game. A good character sprite must be **readable at a glance** — who it is, where it faces, what it is doing — at the small size the game shows it. Detail comes second.

Before you draw, answer three questions:

1. **How big will it be on screen?** Pick the sprite size from your tile size: a character usually fits one tile wide and one or two tiles tall (16×16, 16×24, 16×32 or 32×32).
2. **What view is the game?** Side view (platformer) or top-down (RPG)? It decides which directions you need.
3. **What makes it recognisable?** A hat, a hair colour, a big sword, a scarf. Pick one or two features and make them bold.

## Proportions: chibi and realistic
A character's proportions are often measured in **heads**: how many times the head fits in the full height.

- **Chibi** (or "super-deformed"): about **2 heads** tall. A huge head, a small body, short legs. This is the most common style for small sprites, because the face and expression stay readable even at 16 pixels.
- **Realistic**: about **5 to 7 heads** tall. Long legs, a small head. It needs more pixels: at least 32, often 48 or 64, or the face becomes a single pixel.
- In between, **3 to 4 heads** is a popular compromise for 32-pixel characters.

[[example:proportions]] Left: a chibi hero, two heads tall. Right: a taller hero of about five heads. Both are readable, but the chibi's face survives at a smaller size.

Whatever you choose, use **the same proportions for every character** in the game. A realistic villain next to a chibi hero looks like they come from different games.

> Make the head big and the eyes simple: two dark pixels (or 1×2 on a bigger sprite) are enough. Players read the face first, so give it room.

## 16 pixels or 32 pixels?
- **16 px**: every pixel is a decision. The eyes are 1 pixel each, the hands are 1 pixel, the feet 2 pixels. Use flat colours, 4–8 of them, and rely on silhouette and colour blocks. Animation is quick to make.
- **32 px**: room for a face with expression, clothing details, a hand holding a sword, shading with 3–4 tones per material. Animation takes much longer: each frame is four times as many pixels.

For a first game, a 16×16 or 16×24 chibi is the wise choice: you will draw it in many poses and directions, and a small sprite keeps that manageable.

## Facing directions
- A **side-view** game (platformer) needs the character facing **right**. Facing left is usually the same sprite flipped (the game does it, or **Edit → Flip horizontally**). Make sure nothing breaks when flipped, like text on a shirt or a sword that must stay in one hand.
- A **top-down** game needs at least **front (down), back (up) and side**; the other side is the side view flipped. Some games add the four diagonals for eight directions.

[[example:facing-dirs]] The same chibi facing front, right and back. The hair, the shirt and the proportions stay identical between views.

Keep everything consistent between directions: the same height, the same colours, the feet on the same row. When the game switches from the front sprite to the side sprite, the character must not jump or change size.

## Readability tricks
- **Silhouette test.** Fill the sprite with black. Can you still tell what it is and which way it faces? If not, push the shape: bigger hair, a stance, an arm away from the body.
- **Contrast with the background.** A dark outline (or a selective one) separates the character from any background. Most games outline their characters even when the tiles have no outlines.
- **One accent colour.** A red scarf or a yellow hat draws the eye and makes the hero findable in a busy scene.
- **Big, simple hands and feet.** At small sizes, a hand is a 1–2 pixel blob. That's fine.

[[example:hero-silhouette]] The side-view hero and its silhouette: the hair, the nose and the feet say which way it faces, even in black.

! Drawing a beautiful character on a big canvas (100 px tall), then shrinking it to 16 px for the game. Details turn to mush and the colours multiply. Draw at the final size from the start.

! Drawing the character a little differently in each direction (one pixel taller from the front, the eyes higher on the side). It reads as several characters; keep a reference sheet and compare.

## A routine for a new character
1. Make a canvas the size of the sprite, plus a little room. Draw a **floor line** where the feet stand.
2. Block in the silhouette with one colour: head, body, legs. Check the proportions.
3. Fill flat colours for skin, hair, clothes. Keep the palette small.
4. Add the face: eyes first, a mouth only if it fits.
5. Outline (**Sprite → Outline** gives you a first pass), then fix the outline by hand.
6. Shade with one light direction, 1–2 shades per material.
7. Look at it at 1×, next to a tile from your game.

## Try it
[[exercise:hero-sprite]]
Draw a **hero** for your game standing on the floor line: about **two heads tall** (the dotted line marks about where a chibi's chin goes, half the height), with an **outline**, **3 to 8 colours**, and the feet resting **exactly on the floor line**. Keep everything inside the canvas with a one-pixel margin.
- [ ] My hero passes the silhouette test: in black, I can still tell what it is.
- [ ] The face is readable at 1×.
- [ ] It has one feature that makes it recognisable (a hat, a scarf, a hair colour…).
