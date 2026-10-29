# Outlines
group: fundamentals
summary: Black, coloured and selective outlines (sel-out), inner lines, and when to leave the outline out.

## Why sprites have outlines
An outline is a line of dark pixels around the edge of a sprite. It separates the sprite from whatever is behind it, so the shape reads on any background: light sky, dark cave, busy grass. That is why most game sprites, especially characters and items the player must notice, have one.

[[example:outline-none]] No outline: the apple's edge depends on the background behind it.
[[example:outline-black]] A black outline: the shape reads on any background.

An outline is also part of the style. A thick dark outline looks bold and cartoony; a soft coloured one looks gentle and painterly; no outline at all looks soft and realistic. Choose one style for a whole game and use it for every sprite of the same kind.

## The rules of a clean outline
- **One pixel thick.** At sprite sizes, a two-pixel outline eats half the drawing. Keep it one pixel everywhere, except where you deliberately thicken it for weight (see below).
- **Clean lines.** The outline is a line, so everything from the Lines and curves lesson applies: clean slopes, smooth curves, **no doubles**. Most hairy-looking sprites have doubles in their outline.
- **Closed.** Go all the way around. A gap lets the background "leak" into the sprite.
- **Outside or on the edge.** Usually you draw the outline on the pixels just outside the filled shape. EasyPixel can do this for you: **Sprite → Outline** adds a one-pixel outline around everything on the current layer in the primary colour.

> Draw the outline after you block in the shape and before you shade. Shading next to an outline is easier to judge, and fixing the outline later means re-shading the edge.

! Outlining every small detail inside the sprite with the same black line. The sprite turns into a colouring-book page. Keep the black for the outer edge; inside, use softer lines (see Inner lines below).

## Coloured outlines
Pure black is strong, but it can look harsh, especially on soft or bright sprites. A **coloured outline** uses a very dark version of a colour instead of black: a deep purple, a dark brown, a dark blue. It still separates the sprite from the background, but it blends better with the colours inside.

[[example:outline-colored]] A dark plum outline instead of black: softer, still readable.

A good choice is the darkest colour of your palette that is not black, often a dark purple or dark blue, because it matches the cool shadows of hue-shifted shading.

## Selective outlines (sel-out)
The most refined style is the **selective outline**, or sel-out. Instead of one colour all around, each part of the outline is a **darker shade of the colour it touches**. Next to the red skin of the apple the outline is dark red; next to the green leaf it is dark green. And it follows the light: on the lit side the outline is lighter (sometimes it almost disappears), on the shadow side it is the darkest.

[[example:outline-selout]] Sel-out: each outline pixel is a darker shade of its neighbour, darkest on the shadow side.

Sel-out rules of thumb:

- Take the outline colour from the ramp of the material it touches, one or two steps darker than the darkest shade next to it.
- On the side facing the light, use a lighter shade of the ramp, or the base colour, so that edge glows.
- On the shadow side, use the darkest colour of that ramp, or your outline colour, so the sprite still separates from dark backgrounds.
- Where two materials meet the background close together, pick one; don't put two outline colours side by side.

> Start with a black or coloured outline, shade the sprite, then recolour the outline piece by piece. The Eyedropper (I or Alt) picks the colour next to the outline so you can choose the next darker one in your palette.

! Making a sel-out so light that the outline vanishes on the shadow side too. The shadow side must stay dark, or the sprite melts into dark backgrounds.

## Inner lines
Inside a sprite you still need lines to separate parts: an arm in front of the body, a belt, the edge of a hood. If you draw them in black like the outer outline, the sprite looks busy and flat. Use **inner lines** instead: a darker shade of the colour around them, one or two steps down the ramp, not black.

[[example:inner-lines]] The outer outline is black; where the arm crosses the body, a dark green inner line keeps them apart without cutting the shape in two.

Use as few inner lines as you can. Often a change of shade (light arm against shaded body) separates parts without any line at all.

## Thickness and weight
Some artists thicken the outline on the shadow side and the bottom of a sprite, two pixels instead of one in a few places. It suggests weight and shadow, and makes the sprite feel planted on the ground. Do it on purpose and the same way for every sprite, or not at all.

## When to leave the outline out
Not everything needs an outline. Backgrounds and far scenery usually have none: outlines pull things forward, so outlined backgrounds fight with the characters for attention. Large tiles, effects like fire and smoke, and soft things like clouds usually look better without. A common recipe: outlined characters, items and interactive objects; no outlines on the background. Players then see at a glance what they can touch.

## Try it
[[exercise:outline-apple]]
The canvas holds an apple with no outline. Give it a **closed outline**, one pixel thick, all the way around the apple, the stem and the leaf. Use black, a dark colour, or try a sel-out with darker shades of red and green. The Pencil with Pixel perfect on, or **Sprite → Outline**, will get you started.
- [ ] My outline is one pixel thick and has no doubles.
- [ ] (Bonus) I tried a sel-out: darker shades of the fill instead of one colour, darkest on the shadow side.
