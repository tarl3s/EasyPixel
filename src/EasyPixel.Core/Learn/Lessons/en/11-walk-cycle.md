# Walk cycle
group: fundamentals
summary: The contact and passing poses, 4- and 6-frame walks, the body's bob, arms, and walking in place.

## What a walk cycle is
A **walk cycle** is a short animation that loops: the character takes two steps (left foot, right foot) and ends exactly where it started, ready to begin again. In a game the cycle plays **in place** — the character doesn't move across the canvas — and the game moves the sprite across the level while the cycle plays.

Walking is something everyone sees every day, so a wrong walk is noticed immediately. The good news: a convincing pixel art walk needs only **4 to 8 frames**, and it is built from a few key poses that always work.

## The key poses
Every step of a walk passes through the same poses:

- **Contact**: the legs are furthest apart. The front heel just touches the ground, the back foot is about to push off. This pose defines the stride.
- **Down** (or recoil): the front foot takes the weight, the knee bends, and the whole body is at its **lowest**.
- **Passing**: the back leg swings forward and passes under the body. The supporting leg is straight, so the body is at its **highest**.
- **Up**: the body pushes off toward the next contact.

Then the same poses repeat with the legs swapped. Two steps make one full cycle.

[[example:walk-poses]] Contact, down and passing: the legs apart, the weight landing (body lowest), one leg under the body (body highest). The dotted line marks the head height of the contact pose.

## The 4-frame walk
The simplest walk uses only the contact and passing poses:

1. Contact (right leg in front).
2. Passing (body 1 pixel higher).
3. Contact (left leg in front).
4. Passing (body 1 pixel higher).

[[example:walk-4frame]] A 4-frame walk: contact, passing, contact with the legs swapped, passing. 150 ms per frame.

This is enough for small sprites (16 to 24 pixels tall). Give each frame 120 to 180 ms. Faster than that and the character looks like it is running in place; slower and it looks like it is wading through mud.

## The 6-frame walk
With more pixels to work with, add the **down** pose to each step: contact, down, passing, then the same with the legs swapped. The body sinks on "down" and rises on "passing", which gives the walk weight and bounce.

[[example:walk-6frame]] A 6-frame walk: contact, down (lowest), passing (highest), and the same with the other leg.

An 8-frame walk adds the **up** pose as well. More frames make a smoother walk, but each one has to be drawn for every direction your character faces, so most games stop at 6 or 8.

## The bob
The body goes **up and down** during a walk: highest on passing, lowest on down (or on contact in a 4-frame walk). On a small sprite the difference is just **1 pixel**, and that single pixel is what makes the walk feel alive. Without it, the character glides like it is on wheels.

> Move the whole upper body together — head, torso and arms — when it bobs. If only the head moves, it looks like the character is nodding.

## Legs, arms and the far side
- **Feet stay on the ground line.** In every frame at least one foot touches the ground, and no foot ever goes below it. A sprite whose feet float or sink in some frames will bounce strangely in the game.
- **Arms swing opposite the legs.** When the right leg is in front, the right arm is behind. This counter-swing balances the body. On small sprites, an arm is just a 2–3 pixel stroke that moves forward and back.
- **The far limbs are darker.** The leg and arm on the far side of the body are in shadow. Drawing them a shade darker separates them from the near ones and makes the pose readable.
- **Lean slightly forward** for a determined walk, back for a relaxed or proud one.

! Sliding the character forward across the canvas from frame to frame. The cycle must stay in place: the game moves the sprite. If your walk drifts, it will jump back every time the loop restarts.

! Drawing the legs as two sticks that swing like scissors with the body fixed at one height. Without the bob and without the down pose, the walk has no weight.

## Building it step by step
1. Draw the **contact** pose. Make sure the feet are on the ground line.
2. Copy the frame (**Frame → New frame**) and draw the **passing** pose: one leg under the body, the other lifted a little behind, body 1 pixel higher.
3. Copy the first contact frame and **swap the legs** (and arms). Repeat for the second passing pose.
4. Turn on **View → Onion skin** to compare each frame with its neighbours, and fix jumps.
5. Play it with **Frame → Loop** on and watch it in **Frame → Preview window** at 1×.
6. For a 6-frame walk, insert a **down** frame after each contact.

## Try it
[[exercise:walk-cycle]]
Make a **walk cycle of at least 4 frames** for a small character, walking **in place**. In every frame the lowest foot must stand **exactly on the floor line**, the body must **bob** (the top of the head 1 pixel higher in the passing frames), and the character must not slide sideways from frame to frame. The dotted line is a height reference to see the bob against.
- [ ] The legs swap between the two contact poses, and the arms swing opposite the legs.
- [ ] The far leg (and arm) is a darker shade than the near one.
- [ ] I played it on Loop and there's no jump when it starts over.
