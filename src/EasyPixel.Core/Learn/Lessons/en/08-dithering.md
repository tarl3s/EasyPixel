# Dithering
group: fundamentals
summary: Checkerboards and ordered patterns to blend colours, make gradients and textures, and when dithering hurts.

## Mixing colours you don't have
With a small palette you will often want a colour in between two of yours: a sky that fades from deep blue to pale blue, a shadow that softens gradually. You could add more colours, but there is an older trick: **dithering**. Place two colours in a fine pattern, and from a distance the eye mixes them into a third.

The simplest pattern is the **checkerboard**: the two colours alternate like the squares of a chess board. At 1× it reads as a 50/50 mix.

[[example:dither-checker]] Navy on the left, blue on the right, and a checkerboard of both in the middle: it reads as a colour between them.

Old games used dithering everywhere because their palettes were tiny. Today it is a style choice and a way to keep palettes small — and, used well, it gives pixel art a lovely hand-made texture.

## Ordered patterns
A checkerboard is a 50% mix. To get 25% or 75%, use sparser patterns: one pixel of the second colour in every 2×2 block, then two, then three. Patterns that grow in a fixed order like this are called **ordered dithering** (or Bayer patterns, after the man who described them). They are regular, so they look tidy and don't flicker in animation.

To blend from one colour to another across an area, go from 0% to 25% to 50% to 75% to 100% of the second colour:

[[example:dither-gradient]] Plum into orange with an ordered pattern: the density of orange grows from left to right.

EasyPixel has a **Dither brush** tool: it paints the current colour only on the pixels of a pattern (a checkerboard or an ordered pattern, chosen in its tool options), always lined up with the canvas grid. Fill an area with one colour, then brush the other over it, and you get a perfect checkerboard without placing every pixel by hand.

> Keep the pattern aligned to the grid of the whole picture, not restarted for every shape. Two checkerboards that don't line up make an ugly seam where they meet.

## Gradients without banding
Without dithering, a gradient made of four colours is four flat **bands** with hard steps between them. Sometimes that is exactly the look you want (it is clean and very "pixel art"). But for skies, glows and soft light, the hard edges look like stripes. A narrow strip of dithering at each boundary softens the steps while keeping the same four colours.

[[example:dither-banding]] Four colours in hard bands: the steps are obvious.
[[example:dither-smooth]] The same four colours with a dithered strip at each boundary: a smooth gradient.

Notice that the second picture uses **no new colours**. That is the magic of dithering: smoothness without growing the palette.

## Dithering as texture
Dithering also makes texture: the rough surface of stone, sand, a fuzzy carpet, the grain of an old photo. A few pixels of a pattern in the shadow area of a rock make it look rough instead of plastic.

Used on a shaded object, the rule is: **dither only where two shades meet**, in a narrow band. Leave the middle of each shade solid.

[[example:dither-ball-good]] Dithering only in the narrow zones where two shades meet: the ball keeps its shape and gains a soft transition.
[[example:dither-ball-noisy]] Dithering scattered everywhere: the ball turns into noise and loses its form.

## When dithering hurts
Dithering is easy to overdo. Watch out for these:

- **Small sprites.** A checkerboard on a 16×16 character looks like dirt. Characters and items usually read better with flat colours and clean shading.
- **Moving things.** A dithered pattern on a sprite that moves or scales flickers and "swims". Dither backgrounds and still objects, not fast-moving characters.
- **High contrast pairs.** A checkerboard of black and white is not a soft grey, it's a vibrating pattern. Dither between **neighbouring** colours of a ramp.
- **Random placement.** Scattered single pixels aren't dithering, they are noise. Real dithering follows a regular pattern.

! Dithering between two colours that are far apart (black and white, red and green). The pattern buzzes instead of blending. Only dither between neighbours on a ramp.

! Covering every area with dithering because it looks "retro". Flat areas are restful for the eye; dithering is a seasoning, not the meal.

## A routine for a dithered gradient
1. Choose 2 to 4 neighbouring colours from a ramp (for a sky: navy, blue, sky blue).
2. Fill the area in flat bands, from dark to light, with even spacing.
3. At each boundary, add a strip of 2 to 4 pixels: first a sparse pattern of the next colour, then a checkerboard, then the next colour with a sparse pattern of the previous one.
4. Keep every pattern aligned to the canvas grid.
5. Look at it at 1×. If you still see the bands, widen the dithered strips; if it looks noisy, narrow them.

## Try it
[[exercise:dither-sky]]
Paint a **sky** that fills the whole canvas, **dark at the top and light at the horizon** (the bottom). Use **2 to 4 colours** and blend between them with **dithering**: checkerboards or ordered patterns where one colour turns into the next. The Dither brush makes it quick.
- [ ] At 1× the sky looks like a smooth gradient, not stripes.
- [ ] I only dithered between neighbouring colours, and my patterns line up.
