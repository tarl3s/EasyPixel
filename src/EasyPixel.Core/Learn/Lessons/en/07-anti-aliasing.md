# Anti-aliasing
group: fundamentals
summary: Smoothing the steps of curves with in-between colours by hand, where it helps and where it hurts.

## The staircase problem
A pixel is a square, so every curve and every slanted line in pixel art is really a staircase. In the Lines and curves lesson you learned to make those staircases clean: even runs, no doubled corners. But even a clean curve looks a little jagged when it is big and the contrast is high, like a black line on a white background.

**Anti-aliasing** (AA for short) softens the steps. Where the line moves from one row to the next, you add a pixel of an **in-between colour** — darker than the background, lighter than the line — in the corner of the step. From a normal distance the eye blends it, and the curve looks smoother than the grid really allows.

[[example:aa-none]] A clean curve with no anti-aliasing: correct, but the steps show.
[[example:aa-good]] The same curve with one in-between pixel at each step: the curve reads smoother. The straight sides and the 45° middle (a 1:1 diagonal) are left alone.

Painting programs do this automatically for every brush stroke. Pixel artists do it **by hand**, only where it helps, because automatic anti-aliasing blurs everything and fills the image with colours you didn't choose.

## How to place AA pixels
Look at one step of your curve: the line runs along a row, then jumps diagonally to the next row. The two pixels in the corner of that jump, one on each side of the line, are where AA goes.

1. **Pick the corner pixel that lies closest to the "true" curve** — where the line would pass if it could cut through a pixel. Usually that is the inside of the step, the corner the curve wants to fill.
2. **Choose its colour by how much of the curve passes through it.** If the true curve covers that pixel almost completely, use a colour close to the line; if it just grazes it, use a colour close to the background. Two in-between shades (for example a dark and a light grey between black and white) are plenty.
3. **Longer runs get longer AA.** Where the line runs flat for 4 or 5 pixels before stepping, the AA can be 2 pixels long, getting lighter away from the step.
4. **Leave straight lines and 1:1 diagonals alone.** A perfectly horizontal, vertical or 45° line has no rough steps to smooth.

> Use colours from your palette as AA colours. The middle of a ramp between the line colour and the background often already has the shade you need.

## Where not to use it
Anti-aliasing is a tool for big, high-contrast curves. In many places it hurts:

- **Very small sprites.** On a 16×16 character, an AA pixel is as big as an eye. It blurs details instead of smoothing them. Small sprites look sharpest with no AA at all.
- **Straight parts.** Pixels added beside a straight line just make it look thick and fuzzy.
- **Everywhere at once.** AA on every edge turns crisp pixel art into a blurry image that looks like a scaled-down photo.

[[example:aa-overdone]] Overdone: grey pixels beside every pixel of the line, straight sides included. The shape looks soft and dirty.

! Anti-aliasing every edge "to make it smooth". AA only belongs in the corners where a curve steps; on straight lines and small sprites it only adds blur.

## AA against transparency: the halo
There is a trap with sprites: the background you drew against is not the background the game will show. If you anti-alias the **outer edge** of a sprite against a white page, those light in-between pixels stay in the sprite. Put the sprite in a dark cave in the game and it glows with a light fringe, a **halo**.

[[example:aa-halo]] Left: the coin was anti-aliased against white, so a light fringe rings it on the dark game background. Right: no AA on the outer edge.

So:

- **Don't anti-alias the outer edge** of a sprite that moves over different backgrounds. Keep its outline crisp.
- Anti-alias **inside** the sprite, between its own colours, where you know both sides (a round shield against the arm holding it, a curve inside a face).
- For a tile or a background that is always drawn over the same colour, AA against that colour is fine.

! Anti-aliasing a character's outline on a white canvas, then wondering why it has a pale ring in the game. Check sprites over a dark background before exporting (a temporary dark layer under the drawing works).

## AA, outlines and dithering
Anti-aliasing works hand in hand with the other techniques:

- With a **black outline**, AA pixels between the outline and the fill (inside the shape) soften round heads and bellies without touching the outer edge.
- With **selective outlines** (sel-out), the outline itself already changes colour around the shape, which works like built-in anti-aliasing.
- **Dithering** (next lesson) mixes two colours with a pattern; AA mixes them with a single in-between colour. AA is for edges, dithering is for areas.

## A routine
1. Draw the curve clean first: pixel-perfect, even runs, no doubles. AA can't save a messy line.
2. Decide if the curve needs AA at all: is it big, high-contrast and important? If not, stop.
3. Add one in-between pixel at each step, in the corner nearest the true curve.
4. For long runs, add a second, lighter pixel along the run.
5. Look at it at 1× in **Frame → Preview window**. If the curve looks thicker or blurrier rather than smoother, remove pixels.

## Try it
[[exercise:aa-curve]]
The canvas holds a black shape on white: two **straight sides** and a **curve**. Smooth the curve with anti-aliasing: add **at least 6** in-between pixels (the greys in the palette) in the corners where the curve steps from one row or column to the next. Leave the straight sides alone, and keep every AA pixel touching the line.
- [ ] My AA pixels sit in the corners of the steps, not along the straight sides.
- [ ] Looking at it at 1×, the curve looks smoother, not thicker.
