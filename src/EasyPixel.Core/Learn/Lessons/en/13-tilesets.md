# Tilesets
group: styles
summary: Grid sizes, seamless tiles, edge and corner tiles, variations, and testing in the tiled preview.

## What a tileset is
Most 2D game worlds are built from **tiles**: small square pictures placed on a grid, like floor tiles in a bathroom. One grass tile, repeated a hundred times, becomes a meadow. A few dozen tiles (grass, dirt, the edges where they meet, walls, water) can build a whole level. A set of tiles that fit together is a **tileset**.

Tiles save a lot of work, but they come with two challenges: the tiles must join **without visible seams**, and the repetition must **not be obvious**. This lesson covers both, plus the edge and corner pieces every platform and path needs.

## Choosing the tile size
Pick one tile size for the whole game and make your characters fit it:

- **16×16** — the most common size. Classic platformers and top-down RPGs use it; a character is usually one tile wide and one or two tiles tall.
- **32×32** — more room for detail and texture, common in modern pixel games. Characters are about one tile wide.
- **8×8** — very retro, used for tiny games and for detailed tilesets assembled from smaller pieces.

Your tile size and your character size go together: the hero should fit through a one-tile corridor and land on a one-tile platform. In EasyPixel, **View → Grid settings** sets the tile grid to your tile size, and **View → Tile grid** shows it over the canvas.

## Seamless tiles
A tile is **seamless** when copies of it placed side by side join without a visible line. What matters is the edges: whatever touches the right edge must continue naturally on the left edge, and whatever touches the bottom must continue at the top, because that is what will sit next to it.

[[example:tile-seam-bad]] Not seamless: light on the left, dark on the right, a yellow patch cut off by the edge, a dark line at the bottom. Every tile border shows.
[[example:tile-seam-good]] Seamless: the same tile repeated, and you can't see where one ends and the next begins.

How to make one:

1. Fill the whole tile with the base colour. Ground tiles have **no transparent pixels**; a hole shows the background through the floor.
2. Add texture (tufts of grass, pebbles, cracks) mostly in the **middle** of the tile first.
3. When a detail crosses an edge, continue it on the **opposite** edge: a tuft that sticks out of the right side reappears on the left at the same height.
4. Keep the overall brightness even across the tile. A tile that is lighter on one side than the other shows a stripe pattern when repeated, even if the pixels at the edges match.
5. Turn on **View → Tiled preview**. EasyPixel draws copies of the canvas all around it, and you can keep drawing while you watch the seams.

[[example:tile-single]] One 16×16 grass tile: a base colour and small tufts placed so they wrap around the edges.

> Work with the Tiled preview on from the very first pixel. Fixing a seam afterwards is much harder than never making one.

! Shading a ground tile as if it were an object, with a light corner and a dark corner. A repeated tile has no single light source of its own; shade the small details inside it, and keep the tile's overall brightness flat.

## Hiding the repetition
Even a perfect seamless tile can look like wallpaper when it repeats across a big area: the eye spots the pattern, especially if one detail stands out.

[[example:tile-repeat-bad]] One big shiny rock in the tile: repeated, it forms an obvious grid of rocks.

Ways to avoid that:

- Keep the texture **small and low in contrast**. Many small details blend; one big detail repeats.
- Avoid details that line up in straight rows or columns.
- Make **variations**: two or three versions of the same tile with the details moved around, and one with a rare feature (a flower, a crack, a bone). Mix them when you build the level, using the rare one sparingly.

## Edges and corners
Ground isn't infinite: a platform has a top edge with grass, sides and corners. You need a tile for each case. The basic set is a **3×3 block**: four corners, four edges and a middle tile. With these nine pieces you can build a rectangle of any size.

[[example:tileset-edges]] The nine pieces of a 3×3 set (spread apart) and a platform built from them.

Rules for edge tiles:

- The middle tile is your seamless tile. Every edge tile must be seamless with it along the side they share.
- Edge tiles repeat **along their edge**: the top-edge tile must be seamless with itself left to right.
- Corners round off or bevel the shape, so platforms don't look like cut-out rectangles.
- Outline the outer border of the platform (top and sides) if your characters have outlines, so the walkable surface stands out from the background.

Later you can add **inner corners** (for L-shaped ground), slopes at the clean 1:1 and 2:1 angles from the Lines lesson, and one-tile-wide pieces. Many games use 16 or 47 tiles for one ground type so every neighbour combination has a piece.

! Drawing a whole level as one big picture and cutting it into tiles afterwards. The tiles won't fit any other layout. Design the tiles first, then build levels with them.

## Testing a tileset
- Use **View → Tiled preview** for a single tile.
- For a set, make a test canvas a few tiles wide (for example 96×64 for 16 px tiles), turn on the **Tile grid**, and assemble a small platform or room from copies of your tiles. Look at it at 1× in the preview window.
- Put a character on it. A tileset that looks good alone can still be too busy or too bright for the characters to read against it. The ground should be a bit darker and less saturated than the characters.

## Try it
[[exercise:seamless-tile]]
Make a **16×16 ground tile** (grass, dirt, stone or sand) that repeats without seams. Fill **every pixel**, use **3 to 6 colours**, and make sure details that cross an edge continue on the opposite edge. Turn on **View → Tiled preview** while you draw.
- [ ] I looked at it with View → Tiled preview and can't find the tile borders.
- [ ] No single detail stands out when the tile repeats.
