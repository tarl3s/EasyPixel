# Effects
group: styles
summary: Fire, smoke, explosions and sparkles as short animations: shapes, colour ramps, timing and how effects end.

## Effects are animation first
Effects — fire, smoke, explosions, sparkles, magic, hits — are what make a game feel juicy. A sword hit without a flash feels weak; the same hit with a 3-frame spark feels great. The good news: effects are **short, small and forgiving**. Nobody inspects a single frame of an explosion; what matters is the **shape over time** and the **timing**.

Effects follow a few rules that differ from characters:

- **They change shape a lot** from frame to frame. That's fine — it's what makes them look energetic.
- **They use bright, saturated ramps**: white and yellow at the hottest point, orange and red around it, dark at the edges.
- **They often have no outline**, or a coloured one, so they look like light rather than objects.
- **They have a lifetime**: they start, grow, and end. Even a looping fire has a rhythm.

## Fire
Fire is a looping effect: a flame that never stops. Build it in layers, from the outside in:

1. **Shape**: a teardrop, wide at the bottom and pointed at the top. Make the tip lean and sway a little each frame.
2. **Colours**: red on the outside, orange inside, yellow in the core, and a small white-hot spot near the base where the fire is hottest.
3. **Motion**: in each frame, shift the tip left or right and change its height. Throw an ember (a single orange pixel) above the flame in some frames.
4. **Loop**: 3 to 6 frames at about 80–120 ms each. Fire looks best slightly irregular.

[[example:fx-fire]] A 4-frame fire loop: the tip sways and flickers, an ember jumps off, the core stays bright near the base.

> Flames move **up and inward**: a pixel of fire never travels down. Animate the shapes rising and narrowing, and the eye reads heat.

## Smoke
Smoke is the opposite of fire: **soft, slow and fading**. A puff of smoke:

- **Rises** a little every frame.
- **Grows** as it spreads out.
- **Gets lighter and thinner** until it disappears.

The pixel-art trick for "thinner" is **dithering**: in the late frames, draw only every other pixel of the puff (a checkerboard), then even fewer. The puff breaks apart and fades without needing any transparent colours.

[[example:fx-smoke]] A puff of smoke rising, growing, turning lighter, then thinning out with a checkerboard until it's gone.

Use a grey ramp shifted cool (blue-grey) for most smoke; warmer greys for dust; dark for burning oil.

## Explosions
An explosion is a **one-shot** effect with a clear story in 5 to 8 frames:

1. **Flash**: a small, very bright shape (white and yellow). 1 frame, very short (40–60 ms).
2. **Fireball**: it expands fast — yellow core, orange body, red edge. 1–2 frames.
3. **Peak**: the biggest frame, often turning into a ring as the centre burns out, with debris flying.
4. **Smoke**: the fire turns into dark grey clumps that keep spreading.
5. **Dissipate**: the smoke breaks up (dithering again) and disappears. The last frame is almost empty.

[[example:fx-explosion]] An explosion: flash, fireball, ring with sparks, smoke clumps, then the smoke breaks up and fades.

The timing is what sells it: **fast at the start, slow at the end**. The flash and the fireball are on screen for a blink; the smoke lingers.

! Ending an explosion at its biggest frame, or with a big puff that just vanishes. The effect pops off the screen. Let it break up over the last frames so it ends softly.

! Making the first frame the biggest. An effect that starts at full size has no impact; the growth from small to big is what reads as "boom".

## Sparkles and glints
Sparkles show that something is magic, shiny, or can be picked up. The classic sparkle is a **plus-shaped star** that grows and shrinks:

1. A single bright pixel.
2. A small plus (arms of 1–2 pixels), the centre white.
3. A big plus with short diagonals: the peak.
4. Shrinking back to a small plus, then gone.

[[example:fx-sparkle]] A 4-frame twinkle: a dot, a small plus, a big star with diagonals, and back down.

Place sparkles on items in the world, on a treasure chest, on a magic door. Offset several sparkles in time so they don't all blink together.

## Colours for effects
- Effects can use the **brightest colours of your palette**: white, yellow, aqua. Characters and tiles should leave those for effects, so effects stand out.
- Use **hue-shifted ramps**: fire goes white → yellow → orange → red → dark purple; magic goes white → aqua → blue → purple.
- A frame of pure white over the whole effect for one frame (a **hit flash**) makes impacts feel strong.

## Timing for effects
- Impacts and flashes: **30–60 ms** per frame.
- Explosions: **50–80 ms** at the start, **100–150 ms** for the smoke.
- Fire and loops: **80–120 ms**.
- Sparkles: **60–100 ms**.

Set durations with **Frame → Frame duration…** and watch the result in **Frame → Preview window**: effects are judged in motion, never frame by frame.

## Try it
[[exercise:explosion]]
Animate a **one-shot effect** — an explosion, a puff of smoke or a magic burst — in **at least 5 frames**. It must **start small**, grow to its **biggest frame after the first**, and **fade out**: the last frame at most half the size of the biggest. Use **3 to 6 colours**.
- [ ] The start is quick and the fade is slower.
- [ ] The hottest colours (white, yellow) are in the middle and early frames, the smoke colours at the end.
- [ ] In the Preview window it reads as one "boom", not a blinking shape.
