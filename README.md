<h1 align="center">EasyPixel</h1>

<p align="center">
  <b>A free pixel-art editor for people who aren't artists (yet).</b><br>
  Layers, animation, any canvas size, <code>.aseprite</code> files — and it plays well with EasyEngine.
</p>

<p align="center">
  <a href="https://github.com/tarl3s/EasyPixel/releases/latest/download/EasyPixel-win-x64.zip">
    <img src="https://img.shields.io/badge/Download-Windows%2010%2F11-18B98A?style=for-the-badge&logo=windows&logoColor=white" alt="Download for Windows">
  </a>
  <br>
  <sub><a href="https://github.com/tarl3s/EasyPixel/releases/latest">Latest release &amp; what's new</a> · <a href="README.pt-BR.md">Leia em português</a></sub>
</p>

---

## What is EasyPixel

EasyPixel is a pixel-art and sprite editor with the tools you expect from Aseprite and Piskel, made to be easy to pick up.
It is **free, open source (MIT) and has no accounts, ads or paid tiers**. It runs on **Windows** for now; nothing in the
code stops a macOS / Linux version later.

It is a sibling of [EasyEngine](https://github.com/tarl3s/EasyEngine) (a 2D game engine for people who don't like
programming) and EasyVocal. Each works on its own; together they exchange sprites with one click.

## Get started

1. **[Download EasyPixel](https://github.com/tarl3s/EasyPixel/releases/latest/download/EasyPixel-win-x64.zip)** (Windows 10/11, 64-bit).
2. Unzip it anywhere and open **`EasyPixel.exe`**. Nothing else to install.
   - Windows may say *"Windows protected your PC"* because the app isn't signed yet: click **More info → Run anyway**.
3. **File → New**, pick a size and start drawing. **Help → Keyboard shortcuts** lists every key (the Aseprite ones work).

When a new version is out, EasyPixel asks if you want it and installs it when you close the program. Your sprites are
never touched. You can turn the check off in **Edit → Settings**.

## What it can do

- **Drawing tools** — pencil, eraser, bucket (contiguous or global), line, rectangle, ellipse (filled or outline),
  shading, lighten / darken, dither brush, spray, eyedropper, mirror drawing (horizontal and vertical).
- **Selection** — rectangle, ellipse, lasso, magic wand; move, flip, rotate, scale, cut, copy, paste, invert, select by colour.
- **Layers** — groups, opacity, blend modes, show / hide, lock, reorder.
- **Animation** — frames with their own duration, a timeline, tags (named loops), loop / ping-pong / reverse playback,
  onion skin, a floating preview and a full-screen preview.
- **Any canvas size** — from 1x1 up to thousands of pixels, with zoom, pan, a pixel grid, a tile grid and a tiled preview.
- **Colour** — RGBA or indexed (palette) mode like Aseprite, a palette panel, load / save palettes (`.gpl`, `.hex`, `.pal`),
  reduce colours, outline, pixelate.
- **Sprite tools** — resize, canvas size, rotate, flip.
- **Undo / redo** for everything, including layer and frame changes.
- **Files**
  - Open and save **`.aseprite` / `.ase`** (lossless for everything EasyPixel models; it warns before saving if the file
    holds something it doesn't), **`.piskel`** and EasyPixel's own **`.epx`**.
  - Open PNG and animated GIF; **File → Import sprite sheet…** cuts a PNG sheet into frames.
  - Export **PNG**, animated **GIF** and **sprite sheet** (PNG + JSON, horizontal / vertical / grid).
- **Learn pixel art** — **Help → Learn pixel art** opens 18 lessons, from your first sprite to game-ready sheets:
  pixels and canvas size, lines and curves, shapes, colour palettes, light and shading, outlines, anti-aliasing,
  dithering, textures, animation, walk cycles, characters, tilesets, backgrounds, buildings, items and icons, effects,
  and getting sprites into a game. Each lesson has pictures drawn by EasyPixel itself and a **Try it** exercise that
  opens a practice canvas; **Check** tells you what's right and what to fix, and a passed lesson gets a ★. Save a
  practice and open it later: the lesson bar comes back. Lessons are in English and Português (Brasil); the other
  languages show the English text.
- **9 languages** — English, German, Spanish, French, Japanese, Portuguese (Brazil and Portugal), Chinese (simplified and traditional).
- **Dark and light themes.**

## EasyEngine integration

If you have both installed, they find each other:

- In EasyPixel: **File → Import sprite from EasyEngine…** lets you pick a project, an object and its animation, and edit it.
- In EasyPixel: **File → Export to EasyEngine** saves the sprite with its frames and speed where EasyEngine can pick it up.
- In EasyEngine's **Animations** window: **Import from EasyPixel** brings the result back.

EasyPixel never writes into an EasyEngine project — it only reads one to import; the result comes back from
EasyEngine's side.

## Known limits

- Every cel is stored at the full canvas size for now, so very large canvases with many frames use a lot of memory
  (a 4096x4096 sprite needs about 64 MB per frame and layer).

## Roadmap

1. Tilemap mode; import of PSD, `.pyxel` and `.ora` files.
2. **AI generation** — describe a sprite, or ask for changes and more frames; local (free, on your PC) and cloud
   providers, with clean real-pixel results you can keep editing by hand.

## Build from source

Needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build                          # 0 errors expected
dotnet test                           # portable tests + headless UI tests
dotnet run --project src/EasyPixel    # run the editor
powershell -ExecutionPolicy Bypass -File tools/Publish-Release.ps1 -DryRun   # self-contained build + zip, no upload
```

A build run from the source never checks for updates — the code is the update.

## License

[MIT](LICENSE) — free for everyone.
