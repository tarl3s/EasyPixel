# Textures and materials
group: fundamentals
summary: Making metal, wood, stone, cloth and grass read as what they are, with texture that isn't noise.

## Materials are about light
Why does metal look like metal and cloth like cloth? Not because of the colour: a grey shirt and a grey sword can use the same greys. The difference is **how the material treats light** and **what marks its surface has**. Shiny materials make sharp, bright highlights and strong contrast; soft materials make gentle gradients; rough materials break the light into many small pieces.

So when you draw a material, ask two questions:

1. **How shiny is it?** That decides the contrast between its light and shadow and the shape of its highlight.
2. **What is its surface pattern?** Grain, cracks, bricks, folds, blades: the marks that tell the eye what it is touching.

[[example:materials]] Metal, wood, stone, cloth and grass, all from the same 16-colour palette.

## Metal
Metal is the shiniest material you will draw often. It reflects its surroundings, so it has **very high contrast**: dark darks next to near-white highlights.

- Use a cool grey or blue-grey ramp with a big jump to the highlight colour.
- Make highlights **sharp and long**: thin streaks along the shape (a diagonal streak on a flat plate, a line along a sword's blade), not soft round spots.
- Put the darkest dark right next to the brightest light. That hard contrast is what reads as "shiny".
- Add details that say "made": rivets (a light pixel with a dark one below-right), seams, scratches.

Gold is metal too: a ramp from brown-orange to yellow to almost white, with the same sharp highlights.

## Wood
Wood is warm, matte and **directional**: its grain runs along the plank.

- Use a warm ramp (reds and oranges, darkened toward purple).
- Draw grain as **long, slightly wavy lines** in a darker shade, following the length of the board. Break the lines often; continuous stripes look like a barcode.
- Planks have a light top edge and a dark gap between them.
- A knot (a small dark oval with grain bending around it) makes wood instantly recognisable. One or two are enough.

## Stone
Stone is matte and **chunky**. Its texture comes from blocks, cracks and chips, not from noise.

- Use a grey ramp shifted cool (blue-grey) in the shadows and slightly warm in the light.
- Divide the surface into **irregular blocks** separated by dark mortar lines. Vary their sizes.
- Give each block a light top-left edge and a darker bottom-right edge, so it looks like it bulges.
- Add a few **cracks** (short dark lines that change direction) and chips. Keep them in clusters.

## Cloth
Cloth is soft: **low contrast, smooth transitions, folds**.

- Use a ramp with small, even steps; avoid near-white highlights unless it is silk or satin.
- Draw folds as a dark crease with a light ridge beside it. Folds start from where the cloth is held or pulled (shoulders, belt, knees) and fan out.
- Seams and hems (a line of dotted stitches) say "fabric".

## Grass and foliage
Grass is a **mass of small repeated shapes**: blades, tufts, leaves.

- Don't draw every blade. Draw small clusters (a "v" or a tuft of 2–3 pixels) in a lighter shade on a mid base, with a darker pixel under each.
- Vary the spacing and avoid straight rows.
- For bushes and trees, think in clumps of leaves, each clump shaded like a small ball, rather than single leaves.

## Texture versus noise
The most common texture mistake is **noise**: single pixels of random colours scattered over the surface "to make it rough". At 1× it looks like dirt or static, and it destroys the shading.

[[example:texture-noise]] Noise: random specks everywhere. It reads as dirty, not as stone.
[[example:texture-clusters]] Texture: the same colours grouped into blocks, mortar lines, edges and a couple of cracks. It reads as a stone wall.

Good texture is made of **clusters**: groups of pixels that form a shape — a crack, a plank line, a fold. As a rule of thumb, most pixels should have at least one neighbour of the same colour. If you find lots of lonely single pixels, merge them into lines and blobs or remove them.

! Sprinkling random single pixels to fake roughness. Make the texture out of clusters and lines that follow the material: grain along the wood, cracks in the stone, folds in the cloth.

## Texture follows the form
Texture is not a sticker on top of a flat shape. It must follow the object's shading and shape:

- On the lit side, texture marks are **lighter and fewer**; in the shadow they are darker and can be denser.
- On a round object, lines bend with the surface: the planks of a barrel curve, the stripes of a shirt wrap around the body.
- Detail fades with distance: a wall far away needs less texture than the wall the player stands next to.

> Draw the shading first with flat shades, then add texture on top. If the texture fights the shading (bright marks in the shadow), the object loses its volume.

! Using the same texture density everywhere. Big flat areas with dense texture are tiring; leave some calm space so the eye can rest, and put the most detail where the player looks.

## Try it
[[exercise:material-block]]
Draw a **16×16 block of one material** — a stone wall, a wooden crate, a metal plate, a brick wall or a patch of cloth. Fill **every pixel**, use **3 to 5 colours**, and give it texture that reads: blocks, grain, rivets, folds. Keep it **clustered**: not flat, but not noise either.
- [ ] Someone looking at it at 1× could tell which material it is.
- [ ] My texture marks are clusters and lines, not single scattered pixels.
- [ ] The light comes from one side and the texture follows it.
