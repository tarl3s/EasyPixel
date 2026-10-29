# Colour palettes
group: fundamentals
summary: Limited palettes, colour ramps of 4–6 values, hue shifting, saturation, and checking contrast in greyscale.

## Why pixel artists limit their colours
A photo can contain millions of colours. A good pixel art sprite usually has **fewer than 16**, and a whole game often shares one palette of 16 to 32 colours. That isn't nostalgia: a small palette is one of the main reasons pixel art looks clean.

- **Every colour is visible.** With a few colours, each one is a clear decision. Fifteen shades of nearly the same green turn into blur at game size.
- **Everything matches.** When the hero, the trees and the school all use the same palette, they look like they belong to the same world.
- **It's faster.** Picking from eight swatches is quicker than mixing a new colour for every pixel, and you never wonder "which red did I use for the roof?".

Old consoles forced this: the Game Boy had **4** shades, early computers had **8** or **16** colours. Today nothing forces you, but the habit still pays off.

[[example:palette-sizes]] Four shades (Game Boy style), eight colours, and the sixteen of the Sweetie 16 palette that these lessons use.

How many should you use? A good starting point:

- **4–8 colours** for a small sprite (16×16), outline included.
- **16 colours** for a whole small game, or for one detailed character.
- **32 colours** for a bigger game with many materials and places.

In EasyPixel the colours of the document live in the **Palette** panel. Load a ready-made palette with the panel's ⋯ menu → **Load palette…**, add the current colour with ＋, and switch to **Sprite → Colour mode → Indexed (palette)** if you want EasyPixel to keep every pixel on the palette for you. **Sprite → Reduce colours…** brings an image with too many colours back down to a palette.

> Start every new project by choosing (or making) the palette first. Drawing first and "fixing the colours later" almost never works.

## Ramps: from dark to light
Inside a palette, colours are organised in **ramps**. A ramp is the set of shades of one material, ordered from the darkest to the lightest: the skin ramp, the leaf ramp, the brick ramp. You met ramps in the Light and shading lesson; here we look at how to build a good one.

A ramp for a sprite has **4 to 6 values**: a deep shadow, a shadow, the base colour, a light, and maybe a highlight. Fewer than 4 and objects look flat; more than 6 and the steps get too small to see at game size.

Rules for a good ramp:

1. **Each step is clearly lighter than the one before.** Brightness (the "value") must go up step by step, with no two neighbours that look almost the same.
2. **The steps are roughly even.** One huge jump in the middle makes a hard band on round objects.
3. **The ends reach far.** The darkest colour should be really dark and the lightest really light, or the whole sprite looks grey and foggy.

## Hue shifting
The lazy way to make a ramp is to take one colour and add black for the shadows and white for the lights. It works, but the result is dull and muddy, because in real life shadows and light have their own colours.

[[example:ramp-flat]] One hue made darker with black: the shadows look dirty.
[[example:ramp-hue-shift]] Hue shifting: a deep wine purple in the shadow, then red, orange and yellow in the light, about 25 degrees of hue per step.

**Hue shifting** means changing the hue a little at every step: toward **warm** colours (yellow, orange) as the ramp gets lighter, and toward **cool** colours (blue, purple) as it gets darker. Sunlight is warm and the shadows pick up the blue of the sky, so hue-shifted ramps feel like they are lit by a real sun. A green leaf goes from blue-green in the shadow to yellow-green in the light; a red apple goes from purple to red to orange.

> Move the hue about 10 to 30 degrees per step on the colour wheel. Lights lean toward yellow, shadows toward blue or purple. Further than that and the ramp turns into a rainbow.

Ramps can **share colours**. The darkest colour of the skin ramp can be the same purple as the shadow of the cloth, and the lightest green can be the yellow of the sand. Shared colours keep the palette small and tie the picture together.

[[example:ramps-shared]] Three ramps (warm, plants, water) built from 13 colours: the navy and the yellow are used twice.

## Saturation
**Saturation** is how intense a colour is: grey has none, a pure red has a lot. Fully saturated colours everywhere look loud and cheap, and they tire the eye. A few guidelines:

- Keep the **middle** of a ramp the most saturated; the darkest and lightest ends a bit less.
- Use the strongest colours for what matters: the hero, items, danger. Backgrounds and big areas should be calmer.
- Very dark and very light colours look natural with low saturation. A shadow that is both dark and very saturated looks like neon.

! Picking every colour at full saturation because it looks bright and fun in the colour picker. On the canvas it screams, and nothing stands out because everything does.

## Value: the greyscale check
The single most important property of a colour is its **value**: how light or dark it is, ignoring the hue. The eye reads shapes from value first and colour second. Two colours with different hues but the same value — a red and a green of the same brightness — melt together, and the shape disappears.

The test is simple: **imagine the sprite in greyscale** (or squint until the colours fade). If the parts are still easy to tell apart, the values are good. If everything becomes one grey blob, change the values, not the hues.

[[example:value-bad]] Red cap, green spots, brown stem: lots of colour but one value. In greyscale (right) it's a grey blob.
[[example:value-good]] The same mushroom with planned values: dark outline, mid red cap, light spots and stem. It still reads in greyscale.

! Choosing colours by hue only ("red cap, green spots") without checking the values. Colour-blind players and small screens lose the shape completely.

## Making your own palette
1. Pick the main materials of your game: skin, hair, cloth, wood, stone, grass, sky.
2. For each, make a ramp of 4–5 colours with hue shifting and a clear value step between neighbours.
3. Merge colours that are almost the same between ramps. Reuse the darkest and lightest where you can.
4. Draw a test sprite and a test tile with the palette, look at them at 1× and in greyscale, and adjust.
5. Save it with ⋯ → **Save palette…** so every file of the game uses the same colours.

## Try it
[[exercise:palette-ramp]]
Fill the **five boxes** with one colour each to make a ramp from **dark** (left) to **light** (right). Each box must be clearly lighter than the one before, the lightest should lean **warmer** (toward yellow) and the darkest **cooler** (toward blue or purple) — and the ends must be far apart in brightness. Use the Rectangle tool (filled) or the Pencil.
- [ ] I looked at my ramp imagining it in greyscale and every step is visible.
- [ ] No step is much bigger than the others.
- [ ] The colours don't look like a rainbow: the hue moves a little at each step.
