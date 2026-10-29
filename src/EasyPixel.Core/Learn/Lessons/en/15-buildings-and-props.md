# Buildings and props
group: styles
summary: Drawing a building (a school) in side view, 3/4 top-down view and straight from above, with windows, doors, repetition and props.

## One building, three views
Before you draw a building, you must know **from where the player looks at it**. The same school is drawn very differently depending on the game's camera, and mixing views in one game is one of the quickest ways to make it look wrong. In this lesson we follow one example all the way: **a school**.

There are three common views:

- **Side view** — platformers and side-scrollers. You see the front of the building flat, like a drawing of a facade. No roof from above, no ground plan.
- **3/4 view** (often just called "top-down" in RPGs) — the camera looks down at an angle. You see the **roof from above** and the **front wall below it**, squashed shorter. This is the classic view of town RPGs.
- **Straight top-down** — the camera looks straight down, like a map. You see **only the roof**; the walls are hidden. Strategy games and some shooters use it.

## Side view: the facade
In side view the building is a flat front made of rectangles. That makes it the easiest view, and the best place to learn how buildings are put together.

[[example:school-side]] The school in side view: brick walls, a roof edge, a gable with a clock, two rows of identical windows, a sign over a double door, steps, bushes.

Start from big shapes and go down to details:

1. **Mass**: one rectangle for the main block on the ground line. Add a second block or a tower only if it helps the silhouette (the gable with the clock says "school" or "town hall" at once).
2. **Roof**: a flat roof with an edge, or a sloped roof as a triangle or trapezoid. The roof's edge sticks out a little past the walls and casts a thin shadow line under it.
3. **Openings**: windows and the door. Place them on a regular grid — same height for every window in a row, same spacing between them.
4. **Material**: bricks, planks or plaster. Keep the texture subtle on big walls: a few mortar lines, not every brick.
5. **Details last**: a sign, a clock, a flag, steps, a lamp, a bush by the door.

## Windows and repetition
Buildings are full of repetition: rows of windows, lines of bricks, roof tiles. Repetition is good — it looks built and orderly — but only if the repeated parts are **exactly the same**.

The trick is simple: **draw one window, then copy it**. Select it, **Edit → Copy**, **Edit → Paste**, and move the copy into place. Every window will match perfectly, and if you change the design you can copy it again.

[[example:windows-good]] One window design copied four times: the facade looks tidy and built.
[[example:windows-bad]] Four windows drawn one by one: slightly different sizes and heights. The building looks crooked and careless.

> Line windows up on a grid: same top row, same spacing. Use **View → Tile grid** with a grid size equal to your window spacing to place them.

What makes a window read as a window, even at 6×8 pixels:

- A **frame** lighter or darker than the wall.
- **Glass** in a cool colour (sky blue by day, yellow light at night).
- A **reflection**: one or two light pixels in a corner, always the same corner on every window.
- A **cross bar** or a sill (a light line under the window) if there is room.

Doors are wider and reach the ground. A double door with two small knobs, steps in front and a sign or awning above says "public building" — exactly what a school needs.

## 3/4 view: roof and wall
In the 3/4 view the camera is tilted, so you see two faces of the building: the **roof**, seen from above, and the **front wall**, seen from the front but **shorter** than in side view (the angle squashes it).

[[example:school-34]] The school in 3/4 view: the roof with its rows of tiles takes the top part, the front wall with the same windows and door is shorter below it, and the shadow falls on the ground.

Rules for the 3/4 view:

- The roof usually takes **half or more** of the building's height on screen.
- The front wall is shorter: windows become a bit squatter, but keep the **same design** as in your other art.
- Only the **front** wall shows. Side walls are hidden, or show as a thin strip if the building is drawn slightly turned (most RPGs don't).
- The building's **footprint** (where it touches the ground) is the bottom of the front wall. Characters walk in front of it and behind it, so the game needs to know where that line is.
- A **shadow** on the ground, on the side away from the light, grounds the building.

! Drawing the roof from above but the walls in perspective with vanishing points. RPG buildings use a simple, flat 3/4 view: verticals stay vertical and horizontals stay horizontal. Real perspective makes tiles and sprites impossible to line up.

## Straight from above
In a straight top-down view you only see the roof. To make it read as a school and not just a rectangle:

[[example:school-top]] The school from straight above: the roof with a ridge, skylights, an air-conditioning box, an awning over the door on the front edge, and a shadow that shows the building is tall.

- Give the roof **structure**: a ridge line, panels or tiles, chimneys, skylights, machines.
- Show the **entrance** at the edge: an awning, a path leading to it, a sign on the ground.
- Use the **shadow** to show height: a taller building casts a wider shadow.

## Props
**Props** are the objects that make a place believable: desks, chairs, a chalkboard, lockers, a bin, benches, a bike rack. They are drawn with the same rules as buildings, in the **same view** as everything else.

[[example:school-props]] School props in side view: a desk and a chair, a chalkboard, a locker, a bin — same light, same outline, same palette.

- Draw props at the **same scale** as the characters: a door must be taller than the hero, a desk reaches about the hero's waist.
- Use the **same light direction and outline style** as the rest of the game.
- Make a few versions of common props (a clean and a messy desk) so rooms don't look copy-pasted.

! Drawing the building at a scale that doesn't fit the characters: doors smaller than the hero, or windows bigger than the whole player. Put a character sprite next to the building while you draw it.

## A routine for your school
1. Decide the view of your game. Stick to it for every building and prop.
2. Put a character sprite on the canvas for scale. Draw the ground line.
3. Block the mass and the roof in flat colours.
4. Draw **one** window and **one** door; copy the window to every place on a grid.
5. Add the wall texture, the sign, the clock and the shadow.
6. Look at it at 1× next to your tiles and characters.

## Try it
[[exercise:school-front]]
Draw a **school** (or any building you need) in **side view**, standing on the ground line. Draw **one window, then copy it** so at least **three windows match exactly**. Use **4 to 12 colours**, keep the building in one piece (a separate sign or door is fine, up to three pieces in all), and make it rest **right on the ground line**.
- [ ] My windows are lined up on a grid, with the same spacing.
- [ ] The door is taller than a character would be, and reaches the ground.
- [ ] Something tells you it's a school (a sign, a clock, a bell, a flag).
