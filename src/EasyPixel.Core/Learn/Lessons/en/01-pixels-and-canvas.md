# Pixels and canvas size
group: fundamentals
summary: The grid, choosing 16, 32 or 64 pixels, zoom, and judging your work at 1×.

## Every pixel is a decision
Pixel art is a picture made on a small grid where you place every square by hand. That is the whole difference from painting: in a painting a brush stroke covers thousands of pixels and nobody looks at any one of them. In pixel art a single pixel can be an eye, a highlight on a sword or the corner of a roof. Change it and the whole sprite changes.

That sounds scary, but it is good news. A 16×16 sprite has only 256 squares. You can look at every one of them, ask "does this pixel help?", and fix it. Nothing is hidden. This first lesson is about the grid itself: how big to make your canvas, how to zoom while you work, and how to check the result the way a player will see it.

[[example:potion-16]] A 16×16 potion, zoomed in. Each square is one pixel.

Look at the potion. It uses seven colours: black for the outline, a white and a light grey-blue for the glass, an orange, a red and a dark plum for the liquid, and a yellow for the cork. Every pixel has a job. The white pixels down the left side are the reflection on the glass; the darker pixels on the right are the shadow side of the liquid. You will learn to place those in later lessons. For now, notice how little it takes to say "potion".

## Choosing a canvas size
The canvas size is the first decision, and it decides almost everything else: how much detail you can show, how long the sprite takes to draw, and how big it looks in the game. Common sizes, and what they are good for:

- **8×8** — tiny icons, bullets, coins, letters of a pixel font. Only the essential shape fits.
- **16×16** — the classic size for characters, items and tiles in older-style games. Small enough to finish quickly, big enough for a face with two eyes.
- **32×32** — room for expressions, clothing details, a weapon in the hand, simple shading with three or four tones per colour.
- **64×64** and larger — big bosses, portraits, detailed scenery. Every extra size doubles the work: a 64×64 sprite has 16 times the pixels of a 16×16 one.

[[example:canvas-sizes]] The same slime at 16×16 and at 32×32. The bigger one has room for a highlight and a shadow band; the smaller one says the same thing with fewer pixels.

> Start small. A finished 16×16 sprite teaches you more than an abandoned 64×64 one. When you can make a readable 16×16 character, move up to 32×32.

Pick one size for each kind of thing in your game and keep it. If your hero is 16×32 (16 wide, 32 tall), the other characters should be drawn at about the same scale, and your tiles should match the grid of the world, usually 16×16. A game where every sprite uses its own scale looks messy even if each sprite is beautiful alone.

## Zoom while you draw, judge at 1×
You draw zoomed in, usually at 800% to 1600%, so every pixel is a big square you can click. Use **Ctrl +** and **Ctrl −** to zoom, **Ctrl 0** to fit the sprite on the screen, and hold **Space** and drag to move around. Turn on **View → Pixel grid** when you need to count pixels.

But the player never sees your sprite that big. They see it at its real size, or at 2× or 3× on a modern screen. Some things that look fine zoomed in are invisible or noisy at 1×, and some "mistakes" disappear. So check often: open **Frame → Preview window**, set it to 1× or 2×, and keep it next to the canvas while you work. Press **F** for a full preview.

[[example:potion-1x]] The potion at 1×
[[example:potion-2x]] and at 2×, the way a player might see it.

At 1× the potion is a tiny dot of colour, yet you can still tell what it is because the shape and the main colours are clear. That is the goal for every sprite: readable at its real size. If you can only understand your sprite when it is zoomed in, simplify it.

## One pixel size per picture
Every pixel in a picture must be the same size. It is tempting to draw the big shapes with chunky 2×2 blocks (it is faster) and then add fine details with single pixels. The result is called **mixels** (mixed pixels), and it makes the picture look broken, like two images pasted together.

[[example:mixels-bad]] Mixels: the leaves are drawn with 2×2 blocks, the trunk with single pixels.
[[example:mixels-good]] One pixel size everywhere: the tree reads as one picture.

The same goes for scaling. If you resize a finished sprite to 150% or rotate it by a few degrees, the pixels get uneven sizes. Scale only by whole numbers (2×, 3×, 4×), and do it when you export, not while you draw.

! Drawing at a large size and shrinking the picture down. Shrinking blurs or drops pixels and you lose control over every one of them. Draw at the final size from the start.

## Clean clusters, no noise
Pixels that sit alone, surrounded by a different colour, are hard to read. A few on purpose make sparkles or texture. Too many turn into noise: the eye can't group them into shapes, so the sprite looks dirty. Pixel artists call a lone pixel an "orphan" and usually group colours into small clusters instead.

[[example:orphans-bad]] Random single pixels on a rock: noise.
[[example:orphans-good]] The same colours grouped into clusters: a highlight at the top-left, a shadow at the bottom-right.

! Adding detail by sprinkling single pixels everywhere. More pixels is not more detail. A few clean clusters always read better than many scattered dots.

## Fewer colours, stronger picture
New pixel artists often use too many colours, picking a slightly different shade for every pixel. Each colour you add is one more thing the eye has to sort out. Limits make sprites stronger: with four colours you are forced to decide what really matters. Many classic game sprites used only three colours plus transparency. Start every sprite by choosing a small set of colours from the palette and stick to them; add a colour only when you can say what it is for.

> Tools that help here: the **Pencil (B)** places single pixels, the **Eraser (E)** removes them, the **Eyedropper (I)** or holding **Alt** picks a colour already in the picture, so you reuse colours instead of inventing near-copies.

## Try it
[[exercise:first-sprite]]
Draw a small object on a 16×16 canvas: a potion, a coin, a key, a mushroom, a heart. Use **at most 4 colours** (one can be the outline), keep a one-pixel margin inside the faint guide square so nothing touches the edge, and avoid stray pixels. Check it in the preview window at 1× before you press Check.
- [ ] I looked at it at 1× and can still tell what it is.
- [ ] Every pixel is the same size (no 2×2 blocks pretending to be one pixel).
- [ ] I grouped my colours into clusters instead of scattering single pixels.
