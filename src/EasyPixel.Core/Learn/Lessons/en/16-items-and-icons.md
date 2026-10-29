# Items and icons
group: styles
summary: Items and icons that read at small sizes, bold shapes, consistent light and outline across a whole set.

## Small pictures with a big job
Items and icons are the smallest art in a game and some of the most important: a potion in the inventory, a coin on the ground, a key in the HUD, a sword in the shop. The player has to recognise them **instantly**, at **16×16 or smaller**, often for a fraction of a second while something else is happening.

That makes icons a lesson in **readability**. A beautiful icon that the player can't identify at 1× has failed.

[[example:icon-set]] A set of five 16×16 icons: sword, potion, key, coin, shield. Same outline, same light from the top-left, same palette.
[[example:icon-set-1x]] The same set at its real size. This is how the player sees it: each one still reads.

## Shape first
The silhouette carries most of the meaning, even more than for characters:

- **Use the whole slot.** An icon should fill most of its square: a sword goes corner to corner, a potion fills the height. Tiny objects in the middle of a big empty square are hard to see.
- **Exaggerate the telling part.** A key is recognised by its round bow and its teeth, so make them big. A potion by its round flask and narrow neck. A sword by the crossguard.
- **Use iconic shapes, not realistic ones.** The "classic" heart, the round coin, the heater shield — players know these shapes from many games. Fighting the convention makes your icon harder to read.
- **Angle long objects diagonally.** A sword, a staff or a wand fits a square slot best from corner to corner, usually with the handle at the bottom-left.

## Fewer details, bolder pixels
At 16×16 there is no room for small details. A detail of one pixel is either noise or invisible.

[[example:icon-detail-bad]] Too much detail: a one-pixel blade, many colours, a gem and specks. At 1× it reads as a scratch.
[[example:icon-detail-good]] The same sword with a bold two-pixel blade, five colours and an outline: clear at any size.

Rules of thumb:

- **3 to 6 colours** per icon, plus the outline. Each colour is one part (blade, guard, grip) or one step of light.
- **Make thin parts at least 2 pixels wide** where you can. A 1-pixel blade gets lost.
- **Use strong value contrast**: a light highlight next to a dark shadow. A coin needs a bright yellow and a dark orange, not two similar yellows.
- **One highlight.** A small bright pixel or streak on the top-left says "shiny" — perfect for potions, coins and gems.

! Drawing an icon by shrinking a big painting of the object. The details turn to mush, the colours multiply and the shape gets thin. Draw icons at their real size, pixel by pixel.

## Outline and background
Icons appear over many backgrounds: inventory slots, the ground, a shop window, the dark of a cave. An **outline** keeps them readable on all of them.

- A dark outline all around is the safest choice (a coloured or selective outline works too, if it stays darker than every background it will sit on).
- Leave **a 1-pixel margin** inside the slot so the outline isn't cut off and icons don't touch each other in a grid.
- Don't anti-alias the outer edge: the background behind the icon changes (see the halo in the Anti-aliasing lesson).

## Consistency across a set
Icons are always seen **together**: a row of inventory slots, a shop list. A set looks professional when every icon follows the same rules:

- **The same light direction** — usually from the top-left — so highlights are on the same side everywhere.
- **The same outline** colour and thickness.
- **The same palette** and the same number of shades per material: all metals use the same steel ramp, all gold the same gold ramp.
- **The same size in the slot.** Big objects (a shield) and small objects (a ring) both fill most of the slot; icons are not drawn to scale with each other.
- **The same style of detail**: if one icon has a highlight streak, they all do.

> Draw the whole set on one canvas, side by side (for example 5 slots of 16×16 on an 88×16 canvas). You see at once if one icon is too dark, too small or lit from the wrong side. Then cut them apart when exporting.

! Making each icon on a different day with different colours. The set looks like it came from five games. Keep a palette file and one "reference icon" to compare every new one against.

## Items in the world
Items lying on the ground in the game (a coin to collect, a heart, a key) use the same rules, plus two more:

- **They must stand out from the tiles.** Make items brighter and more saturated than the ground, and outline them.
- **A little motion helps**: a 2–4 frame bob, a glint that travels across the coin, a slow pulse. Moving things catch the eye (see the Effects lesson for sparkles).

## A routine for an icon
1. Make a 16×16 canvas (or your game's icon size). Remember the 1-pixel margin.
2. Block the silhouette with one colour, filling most of the slot.
3. Add flat colours for each part (blade, guard, grip).
4. Add one shadow shade on the bottom-right and one highlight on the top-left.
5. Outline it.
6. Check at 1×: **Frame → Preview window**, or zoom to 100%. If you can't tell what it is, simplify.

## Try it
[[exercise:item-icon]]
Draw a **16×16 item icon** — a potion, a key, a coin, a heart, a book, anything your game needs. Make it **fill the slot** (at least 70% of the width or height), keep a **1-pixel margin**, give it an **outline**, use **3 to 8 colours** with **strong contrast** between the darkest and the lightest, and draw it in **one piece**.
- [ ] I looked at it at 1× and can tell what it is immediately.
- [ ] The light comes from the top-left, like the rest of my icons.
- [ ] There are no single-pixel details that turn into noise.
