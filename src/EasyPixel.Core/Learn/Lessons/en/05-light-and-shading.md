# Light and shading
group: fundamentals
summary: One light source, form shadows and cast shadows, highlights, colour ramps with hue shifting, and avoiding pillow shading.

## Shading means choosing a light
Shading is what makes a flat shape look round, solid and heavy. It is not "darker at the edges": it is the result of **one decision**, where the light comes from. Every pixel of shading follows from that decision. The parts of the object that face the light are lit; the parts that turn away from it fall into shadow.

So before you place a single shade, decide where your light is. In most games the light comes from the **top-left** (or straight from the top), and every sprite in the game uses the same direction. Write it down, draw a little sun in the corner of your canvas if it helps, and never change it within one picture.

[[example:light-source]] Light from the top-left: lit on the upper left, shadow on the lower right, a small highlight where the light hits most directly.

## The parts of light and shadow
Look at any round object under one lamp and you will find the same parts. Pixel art uses them all, simplified into a few colours:

- **Light** — the side facing the light, in the object's main lit colour.
- **Highlight** — a small spot (one to four pixels on a small sprite) where the surface faces the light most directly. On shiny things it is almost white; on matte things it is just the lightest tone. Keep it small: a big highlight makes everything look wet.
- **Form shadow** (or core shadow) — the side turned away from the light. It follows the shape of the object, so on a ball it is a crescent on the far side.
- **Reflected light** — a slightly lighter band at the very edge of the shadow side, light bounced back from the ground. Optional, but it makes things feel round. Keep it darker than any lit part.
- **Cast shadow** — the shadow the object throws on the ground or on other objects, on the side away from the light. It is usually the darkest area and grounds the object, so it doesn't float.

[[example:cast-shadow]] The cast shadow on the ground, away from the light, makes the ball sit on the floor.

On a box the parts are even simpler: each flat face gets one tone. The face pointing at the light is the lightest, the face turned away is the darkest, and the one in between gets a middle tone.

[[example:cube-faces]] Light from above and the left: top face lightest, left face middle, right face darkest.

## Pillow shading
The most common shading mistake has a name: **pillow shading**. It happens when you shade "from the outline inwards": dark at all the edges, lighter rings towards the middle, as if the light came from the viewer's eyes. The object looks puffy and flat, like a pillow, and there is no light direction at all.

[[example:pillow-shading]] Pillow shading: dark rings all around, light in the middle. Where is the light? Nowhere.
[[example:light-source]] The same colours with one light from the top-left.

! Pillow shading: darkening every edge equally. Pick a light direction and let the side facing it stay light, even right up to the outline.

The fix is always the same: decide where the light is, then make the edge that faces it **light**. The shadow side gets the dark tones, and the lit side doesn't. If your shading looks the same on all sides, it is pillow shading.

## Colour ramps
A **ramp** is the set of shades you use for one material, ordered from darkest to lightest. For a small sprite, 3 to 5 shades per material are plenty: a shadow, a base, a light and maybe a deep shadow and a highlight. More shades than that rarely show at game size and make the sprite look blurry.

Each step in a ramp should be a clear, visible step in brightness (in "value"). If two neighbouring shades look almost the same, merge them; if a jump is huge, add a middle shade.

The obvious way to make darker shades, mixing the base colour with black, gives muddy, dull colours:

[[example:ramp-flat]] A ramp made by adding black: the shadows look dirty and grey.

## Hue shifting
Real light is rarely pure white, and real shadows are not black. Sunlight is warm (a little yellow), and shadows pick up the cool blue of the sky. Painters use this, and pixel artists rely on it: **shift the hue** as you go along the ramp. Move the lighter shades toward **warm** colours (yellow, orange) and the darker shades toward **cool** colours (blue, purple). Saturation is usually highest in the middle of the ramp and a bit lower at both ends.

[[example:ramp-hue-shift]] The same idea with hue shifting: deep wine purple, then red, orange, and a warm yellow for the light, each step about 25 degrees round the colour wheel. Rich and alive.

For example, a red apple's ramp might go from a dark purple in the deepest shadow, through crimson and red, to orange in the light and a pale yellow highlight. A green leaf: dark blue-green shadows, green, then yellow-green light. A grey stone: blue-grey shadows and warm, slightly yellow lights.

> Shift the hue by about 10 to 30 degrees per step on the colour wheel, and lower the brightness clearly at the same time. If the ramp looks rainbow-like, you shifted too far.

! Using pure black for shadows and pure white for highlights. Black kills the colour of the material, and white makes it look like plastic. Use a dark, cool colour for the darkest shadow and a pale, warm colour for the highlight.

## A shading routine
1. Fill the shape with the base colour (the middle of the ramp).
2. Decide the light direction.
3. Paint the form shadow with the next darker shade on the side away from the light, following the shape.
4. Add the lighter shade on the side facing the light.
5. Add a small highlight and, if you like, a deeper shadow where the shape turns furthest away (and under things).
6. Add the cast shadow on the ground.
7. Look at it at 1×. If a shade isn't visible there, remove it.

Use the colours of your palette instead of picking new ones for each pixel: a sprite with a planned ramp of four colours looks cleaner than one with fifteen random shades.

## Try it
[[exercise:shade-ball]]
The canvas holds a flat orange ball, and the sun mark in the corner says the light comes from the **top-left**. Shade the ball: use **3 to 6 colours** in total, make the top-left side lit and the bottom-right side fall into shadow, and add a small highlight. The palette has a hue-shifted ramp ready for you (purple, red, orange, yellow).
- [ ] The darkest shadow is on the bottom-right, not all around the edge.
- [ ] I added a small highlight on the lit side.
- [ ] My shades get warmer toward the light and cooler toward the shadow.
