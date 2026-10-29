# Animation basics
group: fundamentals
summary: Frames and timing, holds, onion skin, squash and stretch, and a first idle bob.

## Frames and timing
An animation is a sequence of still pictures, **frames**, shown one after another fast enough that the eye sees movement. Each frame in EasyPixel has its own **duration** in milliseconds: how long it stays on screen before the next one.

Pixel art animations use **few frames**: an idle loop has 2 to 4, a walk 4 to 8, an attack 3 to 6. That is less than in a cartoon, and it is part of the style: every frame counts, and the timing does a lot of the work.

Common timings:

- **100 ms** per frame (10 frames per second) is a good default for walks and loops.
- **60–80 ms** for fast actions: a swing, a dash, an impact.
- **150–400 ms** for slow, calm motion: breathing, a flag in a light breeze, a blinking light.

You don't have to use one duration for all frames. Changing durations inside one animation is how you give it rhythm. Set a frame's time with **Frame → Frame duration…**, play it with **Frame → Play / stop**, and watch it at real size in **Frame → Preview window**.

## Holds and spacing
Two ideas from classic animation make even three frames feel alive:

- **Holds**: a frame that stays on screen longer than the others. A sword swing feels powerful when the wind-up is held (250 ms), the swing is fast (60 ms per frame) and the end pose is held again. Without holds, everything moves at the same speed and nothing has weight.
- **Spacing**: how far the object moves between two frames. Small steps look slow, big steps look fast. Real objects **ease in and ease out**: they start slowly, speed up and slow down again before they stop.

A bouncing ball shows both. At the top of the bounce the ball is slow, so the frames are close together. Near the floor it is fast, so they are far apart.

[[example:bounce-spacing]] One bounce with a frame every few pixels across: the drawings bunch up at the top (slow) and spread out near the floor (fast).

> Act it out. Bounce a real ball, swing your arm, nod your head, and watch where the motion is slow and where it is fast. Then put the holds and the big steps in the same places.

## Squash and stretch
Soft or springy things change shape when they move. A ball **squashes** (gets wider and flatter) when it hits the floor, and **stretches** (gets taller and thinner) when it falls or flies fast. It looks exaggerated in a single frame, but in motion it makes the ball feel heavy and elastic instead of a stiff cut-out.

[[example:squash-stretch]] At rest, squashed on landing (wider, flatter), stretched when falling fast (taller, thinner).

The rule: **keep the volume about the same**. If the ball gets 4 pixels wider, it gets about 4 pixels shorter. A ball that grows in every direction looks like it is inflating, not squashing.

Squash and stretch isn't just for balls: a character squashes a little when landing from a jump, a slime squashes on every hop, a button squashes when pressed.

[[example:bounce-ball]] A bouncing ball with squash on the floor, stretch when it moves fast, and a short hold at the top.

## Onion skin
When you draw a frame, you need to see the frames around it, or the motion jumps. **Onion skin** shows the previous and next frames faintly behind the one you are drawing, like tracing paper. Turn it on with **View → Onion skin**, and choose how many frames to show and how strong they are in **Frame → Onion skin settings…**.

[[example:onion-skin]] Onion skin: the previous frame in red, the next one in blue, the frame being drawn on top.

Work like this:

1. Draw the **key poses** first: the extremes of the motion (highest point, lowest point, the start and end of a swing).
2. Turn on the onion skin and draw the **in-between** frames, placing each between its neighbours.
3. Play it often. Fix the frame that jumps, not the whole animation.

New frames are added with **Frame → New frame** (a copy of the current one, ready to adjust) or **Frame → New empty frame**.

## Your first animation: an idle bob
Characters in games are almost never perfectly still. Even when the player does nothing, the hero breathes, sways or bobs. This **idle** animation keeps the game alive, and it is the easiest animation to make.

[[example:idle-bob]] A two-frame idle: in the second frame the slime is one pixel lower and a bit wider. Each frame stays 300 ms.

An idle bob in 2 to 4 frames:

1. Frame 1: the character standing normally.
2. Frame 2: the body (head and chest) 1 pixel lower. The feet stay exactly where they were!
3. Optionally frames 3 and 4: back up, and a tiny extra movement (hair, cape, a blink).
4. Give each frame 200 to 400 ms: idle is calm.

! Moving the whole character, feet included, in an idle animation. In the game it looks like the character is floating or sliding. The feet stay planted; only the body moves.

! Giving every frame the same duration in an action. A punch where the wind-up, the hit and the recovery all last 100 ms has no impact. Hold the poses that matter and rush the ones in between.

## Loops
Most game animations **loop**: idle, walk, run, a torch flame. A good loop has no visible jump between the last frame and the first. Check it with **Frame → Loop** on: play it for a while and watch the moment it wraps. If you see a hitch, the last frame should lead naturally into the first, like any other frame into the next.

## Try it
[[exercise:bouncing-ball]]
Animate a **ball bouncing** on the floor line. Make **at least 5 frames**: in one the ball touches the floor (its lowest pixel right on top of the line), in another it is **at least 10 pixels higher**, and it never sinks into the floor. Add frames with **Frame → New frame**, turn on **View → Onion skin**, and play it with **Frame → Play / stop**.
- [ ] The ball squashes when it touches the floor (and stretches when it moves fast).
- [ ] The frames are closer together at the top of the bounce than near the floor.
- [ ] I played it in the Preview window and it loops without a jump.
