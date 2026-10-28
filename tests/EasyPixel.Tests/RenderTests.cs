using EasyPixel.Core.Model;
using EasyPixel.Core.Render;
using Xunit;

namespace EasyPixel.Tests;

public class RenderTests
{
    private static uint P(int r, int g, int b, int a = 255) => new Rgba((byte)r, (byte)g, (byte)b, (byte)a).Packed;

    private static readonly uint Grey = P(128, 128, 128);
    private static readonly uint Red = P(255, 0, 0);

    [Fact]
    public void Normal_HalfOpacity_OverWhite()
    {
        // Sa = MUL(255,128) = 128; Ra = 128 + 255 - MUL(255,128) = 255;
        // R = 255 + (0 - 255) * 128 / 255 = 255 - 128 = 127 for every channel.
        Assert.Equal(P(127, 127, 127), BlendModes.Blend(BlendMode.Normal, P(255, 255, 255), P(0, 0, 0), 128));
    }

    [Fact]
    public void Blend_TransparentBackdrop_GivesSourceWithScaledAlpha()
    {
        Assert.Equal(P(255, 0, 0, 128), BlendModes.Blend(BlendMode.Multiply, 0, Red, 128));
        Assert.Equal(P(1, 2, 3), BlendModes.Blend(BlendMode.Normal, P(1, 2, 3), 0, 255));
    }

    // Red (255,0,0) over grey (128,128,128), both opaque, opacity 255: the result is the blend function itself.
    // MUL(a,b) = a*b/255 rounded; DIV(a,b) = (a*255 + b/2)/b.
    [Theory]
    [InlineData(BlendMode.Normal, 255, 0, 0)]
    // Multiply: R MUL(128,255)=128, G MUL(128,0)=0.
    [InlineData(BlendMode.Multiply, 128, 0, 0)]
    // Screen: R 128+255-MUL(128,255)=255, G 128+0-0=128.
    [InlineData(BlendMode.Screen, 255, 128, 128)]
    // Overlay = HardLight with arguments swapped: backdrop 128 is "s" -> s>=128: s=2*128-255=1;
    // R: screen(255,1)=255+1-MUL(255,1)=255; G: screen(0,1)=1.
    [InlineData(BlendMode.Overlay, 255, 1, 1)]
    // Darken = min, Lighten = max.
    [InlineData(BlendMode.Darken, 128, 0, 0)]
    [InlineData(BlendMode.Lighten, 255, 128, 128)]
    // Dodge: R s'=255-255=0, b>=s' -> 255. G s'=255, b<s' -> DIV(128,255)=(32640+127)/255=128.
    [InlineData(BlendMode.ColorDodge, 255, 128, 128)]
    // Burn: R b'=127, s=255: 255-DIV(127,255)=255-127=128. G s=0: b'>=s -> 0.
    [InlineData(BlendMode.ColorBurn, 128, 0, 0)]
    // HardLight: R s=255 -> s'=255, screen(128,255)=255. G s=0 -> MUL(128,0)=0.
    [InlineData(BlendMode.HardLight, 255, 0, 0)]
    // SoftLight (W3C): b=0.50196. R s=1: d=sqrt(b)=0.70849, r=d -> 180.67+.5 -> 181. G s=0: r=b*b=0.25197 -> 64.
    [InlineData(BlendMode.SoftLight, 181, 64, 64)]
    // Difference |128-255|=127, |128-0|=128.
    [InlineData(BlendMode.Difference, 127, 128, 128)]
    // Exclusion: R 128+255-2*MUL(128,255)=383-256=127; G 128+0-0=128.
    [InlineData(BlendMode.Exclusion, 127, 128, 128)]
    // Hue: src red, SetSat(.., Sat(grey)=0) -> (0,0,0); SetLum(.., Lum(grey)=128) -> grey.
    [InlineData(BlendMode.Hue, 128, 128, 128)]
    // Saturation: backdrop grey has max==min -> SetSat gives 0s; SetLum to 128 -> grey.
    [InlineData(BlendMode.Saturation, 128, 128, 128)]
    // Color: Lum(red)=255*30/100=76, d=128-76=52 -> (307,52,52); clip: l=128, x=307:
    // R=128+179*127/179=255; G=128+(-76*127)/179=128-53=75 (truncating); B=75.
    [InlineData(BlendMode.Color, 255, 75, 75)]
    // Luminosity: backdrop grey, Lum(red)=76, d=76-128=-52 -> (76,76,76).
    [InlineData(BlendMode.Luminosity, 76, 76, 76)]
    // Addition: min(255,128+255)=255, 128+0=128.
    [InlineData(BlendMode.Addition, 255, 128, 128)]
    // Subtract: max(0,128-255)=0, 128-0=128.
    [InlineData(BlendMode.Subtract, 0, 128, 128)]
    // Divide: R b<s -> DIV(128,255)=128. G s=0, b>=s -> 255.
    [InlineData(BlendMode.Divide, 128, 255, 255)]
    public void RedOverGrey_EachMode(BlendMode mode, int r, int g, int b)
    {
        Assert.Equal(P(r, g, b), BlendModes.Blend(mode, Grey, Red, 255));
    }

    [Fact]
    public void EveryBlendMode_IsCoveredByTheTable()
    {
        var covered = typeof(RenderTests).GetMethod(nameof(RedOverGrey_EachMode))!
            .GetCustomAttributes(typeof(InlineDataAttribute), false).Cast<InlineDataAttribute>()
            .Select(a => (BlendMode)a.GetData(null!).First()[0]).ToHashSet();
        Assert.Equal(Enum.GetValues<BlendMode>().ToHashSet(), covered);
    }

    private static Sprite Paint(Sprite s, Layer l, int frame, int x, int y, uint v)
    {
        s.GetOrCreateCel(l, frame).Image.Set(x, y, v);
        return s;
    }

    [Fact]
    public void HiddenLayer_NotDrawn()
    {
        var s = new Sprite(2, 2);
        var l2 = new Layer { Name = "b", Visible = false };
        s.Layers.Add(l2);
        Paint(s, s.Layers[0], 0, 0, 0, Red);
        Paint(s, l2, 0, 1, 1, Red);
        var img = new Image(2, 2);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 2, 2));
        Assert.Equal(Red, img.Get(0, 0));
        Assert.Equal(0u, img.Get(1, 1));
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 2, 2), includeHidden: true);
        Assert.Equal(Red, img.Get(1, 1));
    }

    [Fact]
    public void LayerOrder_LastIsOnTop_AndOpacitiesMultiply()
    {
        var s = new Sprite(1, 1);
        var top = new Layer { Opacity = 128 };
        s.Layers.Add(top);
        Paint(s, s.Layers[0], 0, 0, 0, P(255, 255, 255));
        Paint(s, top, 0, 0, 0, P(0, 0, 0));
        s.Cels[(top, 0)].Opacity = 1f;
        var img = new Image(1, 1);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 1, 1));
        Assert.Equal(P(127, 127, 127), img.Get(0, 0));
        s.Cels[(top, 0)].Opacity = 0.5f; // 128 * 0.5 = 64 -> 255 + (0-255)*64/255 = 191
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 1, 1));
        Assert.Equal(P(191, 191, 191), img.Get(0, 0));
    }

    [Fact]
    public void GroupOpacity_AppliesToChildren()
    {
        var s = new Sprite(2, 1);
        var bg = s.Layers[0];
        var group = new Layer { IsGroup = true, Opacity = 128 };
        var a = new Layer { Parent = group };
        var b = new Layer { Parent = group };
        s.Layers.Add(group); s.Layers.Add(a); s.Layers.Add(b);
        Paint(s, bg, 0, 0, 0, P(255, 255, 255)); Paint(s, bg, 0, 1, 0, P(255, 255, 255));
        // Both children paint pixel 0 black; opacity is applied once to the group, not per child.
        Paint(s, a, 0, 0, 0, P(0, 0, 0)); Paint(s, b, 0, 0, 0, P(0, 0, 0));
        var img = new Image(2, 1);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 2, 1));
        Assert.Equal(P(127, 127, 127), img.Get(0, 0));
        Assert.Equal(P(255, 255, 255), img.Get(1, 0));

        group.Visible = false; // hidden group hides its children
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 2, 1));
        Assert.Equal(P(255, 255, 255), img.Get(0, 0));
    }

    [Fact]
    public void GroupBlend_UsesGroupMode()
    {
        var s = new Sprite(1, 1);
        var group = new Layer { IsGroup = true, Blend = BlendMode.Multiply };
        var a = new Layer { Parent = group };
        s.Layers.Add(group); s.Layers.Add(a);
        Paint(s, s.Layers[0], 0, 0, 0, Grey);
        Paint(s, a, 0, 0, 0, Red);
        var img = new Image(1, 1);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 1, 1));
        Assert.Equal(P(128, 0, 0), img.Get(0, 0));
    }

    [Fact]
    public void Flatten_DirtyRect_Only()
    {
        var s = new Sprite(4, 4);
        Paint(s, s.Layers[0], 0, 1, 1, Red);
        Paint(s, s.Layers[0], 0, 3, 3, Red);
        var img = new Image(4, 4, 0xFF123456);
        Compositor.Flatten(s, 0, img, new PixelRect(1, 1, 2, 2));
        Assert.Equal(Red, img.Get(1, 1));
        Assert.Equal(0u, img.Get(2, 2));          // cleared inside the area
        Assert.Equal(0xFF123456u, img.Get(3, 3)); // untouched outside
        Assert.Equal(0xFF123456u, img.Get(0, 0));
        Compositor.Flatten(s, 0, img, new PixelRect(-5, -5, 100, 100)); // clipped
        Assert.Equal(Red, img.Get(3, 3));
    }

    [Fact]
    public void Indexed_UsesPaletteAndTransparentIndex()
    {
        var s = new Sprite(2, 1, ColorMode.Indexed);
        int i = s.Palette.Add(new Rgba(10, 20, 30, 255));
        s.GetOrCreateCel(s.Layers[0], 0).Image.Set(0, 0, (uint)i);
        var img = new Image(2, 1);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 2, 1));
        Assert.Equal(P(10, 20, 30), img.Get(0, 0));
        Assert.Equal(0u, img.Get(1, 0));
    }

    [Fact]
    public void CelOffset_IsHonouredAndClipped()
    {
        var s = new Sprite(3, 3);
        var cel = new Cel(new Image(2, 2, Red), -1, 2);
        s.Cels[(s.Layers[0], 0)] = cel;
        var img = new Image(3, 3);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 3, 3));
        Assert.Equal(Red, img.Get(0, 2));
        Assert.Equal(0u, img.Get(1, 2));
        Assert.Equal(0u, img.Get(0, 1));
    }

    [Fact]
    public void Onion_PreviousAndNext_NoWrap_FadeAndTint()
    {
        var s = new Sprite(3, 1);
        s.Frames.Add(new Frame()); s.Frames.Add(new Frame());
        var l = s.Layers[0];
        Paint(s, l, 0, 0, 0, P(255, 255, 255));
        Paint(s, l, 2, 2, 0, P(255, 255, 255));
        var img = new Image(3, 1);
        var o = new OnionSettings { Before = 1, After = 1, Opacity = 100 };
        Onion.Render(s, 1, o, img);
        Assert.Equal(P(255, 255, 255, 100), img.Get(0, 0));
        Assert.Equal(P(255, 255, 255, 100), img.Get(2, 0));
        Assert.Equal(0u, img.Get(1, 0));

        o.Tint = true; // previous: halfway to red, next: halfway to blue (255+0)/2 = 127
        Onion.Render(s, 1, o, img);
        Assert.Equal(P(255, 127, 127, 100), img.Get(0, 0));
        Assert.Equal(P(127, 127, 255, 100), img.Get(2, 0));

        Onion.Render(s, 0, o, img); // frame 0: nothing before (no wrap), frame 1 is empty
        Assert.Equal(0u, img.Get(0, 0));
        Assert.Equal(0u, img.Get(2, 0));
        Onion.Render(s, 2, new OnionSettings { Before = 2, After = 2, Opacity = 100 }, img);
        Assert.Equal(P(255, 255, 255, 50), img.Get(0, 0)); // two frames away: fades to 100 * 1/2
    }

    [Fact]
    public void Group_PartialArea_ReusingScratch_StaysCorrect()
    {
        var s = new Sprite(4, 1);
        var group = new Layer { IsGroup = true, Opacity = 128 };
        var a = new Layer { Parent = group };
        s.Layers.Add(group); s.Layers.Add(a);
        for (int x = 0; x < 4; x++) { Paint(s, s.Layers[0], 0, x, 0, P(255, 255, 255)); Paint(s, a, 0, x, 0, P(0, 0, 0)); }
        var scratch = new CompositeScratch();
        var img = new Image(4, 1, 0xFF123456);
        Compositor.Flatten(s, 0, img, new PixelRect(1, 0, 2, 1), scratch: scratch);
        Assert.Equal(0xFF123456u, img.Get(0, 0));
        Assert.Equal(P(127, 127, 127), img.Get(1, 0));
        Assert.Equal(P(127, 127, 127), img.Get(2, 0));
        Assert.Equal(0xFF123456u, img.Get(3, 0));
        // Second call over a different area reuses the scratch (stale pixels at x=1,2 must not leak).
        s.Cels[(a, 0)].Image.Set(1, 0, 0);
        s.Cels[(a, 0)].Image.Set(2, 0, 0);
        Compositor.Flatten(s, 0, img, new PixelRect(2, 0, 2, 1), scratch: scratch);
        Assert.Equal(P(127, 127, 127), img.Get(1, 0)); // outside the second area: untouched
        Assert.Equal(P(255, 255, 255), img.Get(2, 0)); // child now empty: group adds nothing
        Assert.Equal(P(127, 127, 127), img.Get(3, 0));
    }

    [Fact]
    public void Onion_ZeroBeforeAndAfter_DrawsNothing_AndAreaIsRespected()
    {
        var s = new Sprite(3, 1);
        s.Frames.Add(new Frame());
        for (int x = 0; x < 3; x++) Paint(s, s.Layers[0], 1, x, 0, P(255, 255, 255));
        var img = new Image(3, 1, 0xFF123456);
        Onion.Render(s, 0, new OnionSettings { Before = 0, After = 0 }, img);
        Assert.All(img.Pixels, p => Assert.Equal(0u, p));
        img.Pixels.AsSpan().Fill(0xFF123456);
        Onion.Render(s, 0, new OnionSettings { Before = 0, After = 1, Opacity = 100 }, img, new PixelRect(1, 0, 1, 1));
        Assert.Equal(0xFF123456u, img.Get(0, 0));
        Assert.Equal(P(255, 255, 255, 100), img.Get(1, 0));
        Assert.Equal(0xFF123456u, img.Get(2, 0));
    }

    [Fact]
    public void Indexed_PaletteEntryWithAlpha_IsBlended()
    {
        var s = new Sprite(1, 1, ColorMode.Indexed);
        int bg = s.Palette.Add(new Rgba(255, 255, 255, 255));
        int half = s.Palette.Add(new Rgba(0, 0, 0, 128));
        var top = new Layer();
        s.Layers.Add(top);
        s.GetOrCreateCel(s.Layers[0], 0).Image.Set(0, 0, (uint)bg);
        s.GetOrCreateCel(top, 0).Image.Set(0, 0, (uint)half);
        var img = new Image(1, 1);
        Compositor.Flatten(s, 0, img, new PixelRect(0, 0, 1, 1));
        Assert.Equal(P(127, 127, 127), img.Get(0, 0)); // same as 50% black over white
    }
}
