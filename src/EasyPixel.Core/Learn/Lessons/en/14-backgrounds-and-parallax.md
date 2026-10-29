# Backgrounds and parallax
group: styles
summary: Layered depth, atmospheric perspective, lower contrast far away, parallax layers that repeat, and keeping the player readable.

## What a background is for
The background sets the mood and the place — a sunny town, a dark forest, the schoolyard at dusk — but it has one rule above all: **it must not compete with the game**. The player, the enemies, the platforms and the items have to read instantly on top of it. A beautiful background that hides the hero is a bad background.

So backgrounds are usually:

- **Lower in contrast** than the foreground.
- **Less saturated**, with fewer details.
- **Darker or lighter overall** than the characters, so the characters pop out.

## Layers of depth
Side-view games build backgrounds from **layers** at different distances: the sky, far mountains, closer hills, trees, and finally the ground the player walks on. When the camera moves, each layer scrolls at a different speed: far layers slowly, near layers fast. That difference, called **parallax**, creates a strong feeling of depth with flat pictures.

[[example:parallax-layers]] Parallax: the sky and the far mountains stay still, the hills move a little, the near ground and the tree move the most.

Typical speeds, as a fraction of the camera's movement:

- Sky: **0** (doesn't move) or very slow.
- Far mountains: **0.1–0.25**.
- Hills and distant buildings: **0.3–0.5**.
- Near trees and fences: **0.6–0.8**.
- The playing ground: **1** (moves with the camera).

In EasyPixel, draw each layer on its own **layer** (**Layer → New layer**) so you can see them together, then export them as separate images for the game.

## Atmospheric perspective
The air between you and a distant mountain isn't empty: haze and light scatter in it. The further away something is, the more it fades toward the colour of the sky. Painters call this **atmospheric perspective**, and it is the most powerful depth tool you have:

- **Far** layers: light, close to the sky colour, **low contrast**, low saturation, few details, no outlines.
- **Middle** layers: medium values, a bit more colour and detail.
- **Near** layers: the darkest darks, the most saturation, the most detail.

[[example:atmosphere-bad]] Every range in the same strong green with black outlines: it's flat, like cardboard cut-outs at one distance.
[[example:atmosphere-good]] The far range pale and hazy, the middle one blue-grey, the near one dark and green: instant depth with the same shapes.

> To make a far layer, take the colours of the near one and mix them halfway toward the sky. Two or three colours are enough; keep them close in value.

! Outlining background layers the same way as characters. Outlines pull things forward; on far layers they flatten the depth and make the background fight the player. Leave the outlines for the things the player interacts with.

## Layers that repeat sideways
A level is usually much wider than one background picture, so each layer **repeats horizontally**: when the camera passes its right edge, its left edge comes next. Like a tile, the layer must be **seamless left to right**: the hill that leaves the right edge must continue at the left edge at the same height.

[[example:hills-strip]] A hills layer that wraps: the right edge meets the left at the same height, so it can repeat forever.
[[example:hills-strip-bad]] Not seamless: the hills are high on the right and low on the left, so a cliff appears every time it repeats.

Check it with **View → Tiled preview**, which draws copies of the canvas around it. A few more rules for parallax layers:

- The bottom of each layer (except the sky) must be **filled all the way across**. A hole lets the layer behind show through.
- Make the layer's width a nice multiple of the screen (or of 16 or 32 px). Shapes that repeat at the same rate as the width are easy to wrap: a hill every 32 pixels on a 64-pixel layer.
- Don't put a single unique landmark in a short repeating layer: a castle that passes every two seconds gives the repetition away. Put landmarks on wide, slow layers, or as separate objects.

## Keeping the player readable
Put your character on top of the background and look at it at 1× before calling the background done.

- If the character disappears into the background, **lower the background's contrast** or shift its values (darker sky behind a light hero, or the other way round).
- Avoid busy textures right behind the playing area. Detail belongs at the top of the screen and far away.
- Keep the background's colours a little cooler and greyer than the characters'. Warm, saturated colours come forward; cool, grey colours stay back.

! Making the background the most detailed picture of the game. The player stops seeing the hero, the enemies and the edges of the platforms. Simplify until the foreground reads at a glance.

## A routine for a parallax background
1. Paint the sky first: a gradient (dithered if your palette is small) or a flat colour.
2. Draw the farthest layer: a simple silhouette, pale and close to the sky colour, seamless left to right.
3. Add each nearer layer darker, more saturated and more detailed than the one behind.
4. Test each layer with View → Tiled preview.
5. Put a character sprite on top and squint: it must stand out.

## Try it
[[exercise:parallax-hills]]
Paint a **far hills layer** for a parallax background: hills that **repeat seamlessly left to right**, reach the **bottom edge all the way across**, and stay hazy — **at most 3 colours**, close in brightness. Leave the sky above transparent: another layer will hold it. Turn on **View → Tiled preview** to check the wrap.
- [ ] In the Tiled preview I can't see where the layer starts over.
- [ ] My hills are pale and low in contrast, like they're far away.
- [ ] No single landmark stands out and gives away the repetition.
