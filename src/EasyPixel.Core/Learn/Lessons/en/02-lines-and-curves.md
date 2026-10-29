# Lines and curves
group: fundamentals
summary: Pixel-perfect lines, clean slopes (1:1, 2:1, 3:1) and smooth curves without jaggies.

## A line is a staircase
On a grid there are no real diagonal lines. Any line that is not perfectly horizontal or vertical becomes a staircase of short horizontal (or vertical) runs. The trick of clean pixel lines is to control those runs: how long each one is, and how the runs follow each other.

When the runs follow a regular pattern, the eye joins them into a smooth line. When they change length at random, the eye sees every step, and the line looks broken. Those visible, uneven steps are called **jaggies**.

[[example:jaggies-bad]] Jaggies: runs of 1, 4, 1, 2, 5, 1… pixels. The line wobbles.
[[example:jaggies-good]] Runs of 2, 3, 2, 3, 2 that repeat evenly: the same slope, now straight.

Both lines go up the same height across the same width. The only difference is how the pixels are shared out between the runs. That is the whole skill of this lesson.

## The clean slopes
Some slopes are perfectly regular on a grid. Learn them by heart, because you will use them all the time:

- **1:1** — one pixel across, one up. A perfect 45° diagonal, every run is 1 pixel.
- **2:1** — two across, one up. Runs of 2. This is the slope of most isometric art.
- **3:1** — three across, one up. Runs of 3, a gentle slope.
- **1:2** and **1:3** — the same, standing up: runs of 2 or 3 pixels going up, one across.

[[example:slopes]] From left to right: 1:1, 2:1, 3:1, and a steep 1:2 line.

Any other angle needs a mix of run lengths, for example 2, 3, 2, 3 for a slope between 2:1 and 3:1. That is fine as long as the pattern repeats evenly. A pattern like 2, 2, 3, 2, 2, 3 reads as straight; a pattern like 1, 4, 2, 3 never will. When you are free to choose the angle, prefer one of the clean slopes: they always look crisp.

> The **Line tool (L)** draws straight lines for you. Drag from start to end and watch the run lengths: if they come out uneven, move the end point a pixel or two until they are regular.

## Doubles: the extra corner pixel
When you draw a line freehand with the Pencil, you usually get extra pixels at every step, in an L shape: the line goes across, then up, then across. Those corner pixels make the line look thicker and jagged in some places and thin in others. They are called **doubles**.

[[example:doubles-bad]] A freehand diagonal with an L-shaped corner at every step.
[[example:doubles-good]] The same line with the corner pixels removed: one pixel thick everywhere.

Turn on **Pixel perfect** in the Pencil's options bar (at the top). EasyPixel then removes the corner of every L while you draw, so freehand lines come out clean. You can still remove doubles by hand with the Eraser: a pixel is a double when it touches one pixel beside it and one pixel above or below it, and those two also touch each other at the corner.

! Making a line "thicker" by doubling it unevenly. If a line must be thicker, make it two pixels thick everywhere, on purpose. A line that is one pixel thick in some places and two in others looks like a mistake.

## Curves are slopes that change
A curve is a line whose slope keeps changing. In pixels, that means the run lengths change, and the secret of a smooth curve is that they change **gradually and in one direction**. Going around a quarter circle, the runs get shorter: for example 5, 3, 2, 1, 1, 1 and then the same counts going up: 1, 1, 1, 2, 3, 5.

[[example:curve-bad]] Run lengths 3, 1, 4, 1, 2…: the curve has bumps and flat spots.
[[example:curve-good]] Run lengths 5, 3, 2, 1, 1, 1: the curve bends smoothly.

The rule of thumb: never put a long run between two short ones, or a short run between two long ones, unless you want a bump there. If the lengths go 4, 3, 2, 2, 1, 1 the curve is smooth. If they go 4, 2, 3, 1, 2 the curve wobbles even if the overall shape is right.

> To check a curve, count the runs out loud. If the sequence goes up and down like "4, 2, 3, 1", fix the odd one out. Most bumps are one run that is one pixel too long or too short.

! Copying a curve from a smooth drawing pixel by pixel. A perfect circle traced from a vector drawing rarely gives clean runs. Draw the curve, then adjust the runs by hand until the sequence is smooth.

## Lines in real sprites
In a finished sprite, lines are mostly outlines and edges: the back of a character, the brim of a hat, the curve of a shield. The same rules apply. A character's outline with clean runs looks confident and readable. One with jaggies looks hairy, even when the colours and shading are good. Before you add colour to a sprite, go around its outline once and check every staircase and every curve.

Long straight edges are easiest when they use the clean slopes, which is why so many buildings and platforms in pixel games are drawn at 1:1 and 2:1. For organic things like hair, leaves and clouds, use curves whose runs change smoothly, and avoid long perfectly straight lines that make them look stiff.

## Try it
[[exercise:clean-lines]]
On the guide you'll find pairs of red dots. Draw a line between each pair: the first is a 1:1 slope, the second 2:1 and the third 3:1. Then, if you want, follow the dotted arc in the bottom-right corner with a smooth curve. Use **one colour** (two at most), keep every line **one pixel thick**, and leave **no doubles**. The Line tool or the Pencil with Pixel perfect on will help.
- [ ] My 2:1 line steps exactly two pixels across for each pixel up, all the way.
- [ ] My curve's runs get shorter (or longer) smoothly, with no bumps.
