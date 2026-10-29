# Shapes and silhouettes
group: fundamentals
summary: Readable shapes first: the silhouette test, shape language and strong poses.

## Shape before detail
At game size, a sprite is seen for a fraction of a second, often while it moves. The player doesn't study its buttons or the pattern on its cape. They recognise it from its **shape**: the outline of the whole figure against the background. If the shape is clear, the sprite works even with simple colours. If the shape is a blob, no amount of detail will save it.

That is why experienced pixel artists start with the big shape and only add detail at the end. Block out the whole figure in one or two flat colours first. Move parts around while they are cheap to change. Only when the shape reads well do you add colours, shading and outlines.

## The silhouette test
Fill your sprite with a single dark colour, so only the outer shape is left: its silhouette. Now ask: can I still tell what it is? Is it a knight, a wizard, a slime, a chest? Can I tell which way it faces and what it is holding?

[[example:silhouette-good]] A readable knight: plume, sword held out, shield to the side, legs apart. Even in black you know what it is.
[[example:silhouette-bad]] The same knight pressed together: arms against the body, sword hidden, legs closed. The silhouette is a capsule.

Both knights have the same parts and the same colours. The difference is only where the parts are. In the readable one, every important part **sticks out** of the body and has a gap of background around it: the sword, the shield, the plume, the space between the legs. In the other, everything overlaps the body, so the silhouette loses it.

> A quick silhouette test without changing your work: zoom far out (Ctrl −) or squint at the screen until the colours blur together. What you can still make out is the silhouette. For a strict test, duplicate the layer (Layer → Duplicate layer), turn each of its colours black with the Bucket (G) with **Contiguous** off, and hide the copy again when you are done.

## Make the important parts stick out
A few habits make almost any sprite read better:

- **Separate the limbs.** Leave at least one pixel of background between the arms and the body and between the legs. At 16 pixels, that gap is often the only thing that shows there are two legs.
- **Hold items away from the body.** A sword drawn in front of the chest disappears. The same sword held out to the side, at an angle, says "warrior" instantly.
- **Exaggerate the key feature.** A wizard's hat, a rabbit's ears, a knight's plume: make the one feature that identifies the character bigger than it would be in real life.
- **Use a clear pose.** Characters standing perfectly straight and symmetrical look stiff. A slight lean, one foot forward, a weapon raised: small asymmetries make a pose readable and alive.

! Drawing a character perfectly front-facing and symmetrical with the arms down. It is the easiest pose to draw and the hardest to read. Turn the character a little to the side (a three-quarter view) or give it an action pose.

## Shape language
Simple shapes carry feelings, and players read them without thinking. Use that on purpose when you design characters, enemies and items:

- **Round shapes** (circles, soft curves) feel friendly, soft, harmless, cute. Slimes, helpers, pickups.
- **Square shapes** (blocks, flat tops, right angles) feel stable, strong, slow, reliable. Golems, guards, walls, chests.
- **Triangles** (points, spikes, sharp angles) feel dangerous, fast, aggressive. Villains, spikes, enemies to avoid.

[[example:shape-language]] Round, square, triangle: friendly, sturdy, dangerous.

Mix them to tell a story. A round character with a triangular weapon is a friendly hero who can fight. A square body with sharp triangular shoulders is a heavy, dangerous boss. When you design an enemy, ask what the player should feel, and choose the main shape before you choose any colour.

## Size and proportion
Proportions also change how a sprite reads. Small game characters are usually drawn with a big head (a third to a half of the body height, sometimes more), because the face carries the expression and a tiny head at 16 pixels can't show eyes. That style is called **chibi**. Taller, more realistic proportions (a head about a sixth or seventh of the height) need a bigger canvas, at least 32 pixels tall, to stay readable.

Whatever you choose, keep it the same across your game: if the hero has a big head, the villagers should too.

> Readability also depends on what is around the sprite. A dark character on a dark background has no silhouette at all. Keep your characters a bit brighter or more saturated than the backgrounds they walk on, or give them an outline (see the Outlines lesson).

## From silhouette to finished sprite
A good way to work, for any sprite:

1. Block out the silhouette in one flat colour. Try two or three versions side by side and keep the clearest.
2. Split it into the main parts with flat colours (skin, clothes, hair, weapon), still without shading.
3. Check it at 1× in the preview window. Fix anything that merges.
4. Only now add shading, outlines and small details.

! Starting with the eyes or the face and building the body around them. You will polish details that later need to move. Shape first, details last.

## Try it
[[exercise:silhouette]]
Draw a character, creature or object as a **silhouette in one colour** on the 32×32 canvas, standing on the faint ground line. Make it **big and bold** (it should fill a good part of the canvas, without becoming a blob), in **one or two pieces**, and leave a margin around it. Give it at least one feature that sticks out.
- [ ] Someone else could guess what it is from the shape alone.
- [ ] Arms, legs or held items are separated from the body by gaps.
- [ ] It has one exaggerated feature that identifies it (a hat, ears, a weapon, a tail…).
