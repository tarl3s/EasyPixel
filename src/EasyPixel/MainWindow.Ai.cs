using Avalonia.Interactivity;
using EasyPixel.Ai;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;

namespace EasyPixel;

// File → ✨ Create with AI (also the ✨ button on the tool strip): the chosen candidate becomes a new sprite or a new layer.
public partial class MainWindow
{
    private void CreateWithAi_Click(object? sender, RoutedEventArgs e) => _ = CreateWithAiAsync();

    /// <summary>
    /// Opens the Create with AI window. With no usable picture service (the normal first run: they all need a free key)
    /// it says so and offers AI settings. <paramref name="skipSetupCheck"/> is for tests whose dialogs answer directly.
    /// </summary>
    internal async Task CreateWithAiAsync(bool skipSetupCheck = false)
    {
        if (!skipSetupCheck && AiRouting.UsableGenerators(App.Settings).Count == 0)
        {
            if (!await Dialogs.Confirm(L.T("To create sprites with AI, choose a picture service and add its free key in AI settings."),
                    L.T("Open AI settings"), L.T("Not now")))
                return;
            await AiSettingsAsync();
            if (AiRouting.UsableGenerators(App.Settings).Count == 0) return;
        }
        var doc = Session.Doc;
        if (await Dialogs.CreateWithAi(new AiCreateRequest(Math.Min(doc.Width, doc.Height), SpriteColors(doc))) is not { } pick) return;
        if (pick.AsNewLayer) AddAiLayer(pick.Sprite);
        else await OpenAiSpriteAsync(pick.Sprite);
    }

    /// <summary>The sprite's palette (opaque colours), else the colours its pictures use (at most 64).</summary>
    private static IReadOnlyList<Rgba> SpriteColors(Sprite doc)
    {
        var colors = doc.Palette.Colors.Where(c => c.A == 255).Distinct().ToList();
        if (colors.Count > 0) return colors;
        var seen = new HashSet<uint>();
        foreach (var cel in doc.Cels.Values)
            foreach (var p in cel.Image.Pixels)
            {
                var c = Rgba.FromPacked(doc.ToPacked(p));
                if (c.A == 255 && seen.Add(c.Packed)) { colors.Add(c); if (colors.Count >= 64) return colors; }
            }
        return colors;
    }

    private async Task OpenAiSpriteAsync(Image sprite)
    {
        if (!await ConfirmDiscardAsync()) return;
        var doc = NewDocument(sprite.Width, sprite.Height, ColorMode.Rgba);
        doc.Cels[(doc.Layers[0], 0)] = new Cel(sprite.Clone());
        SetSession(new EditorSession(doc));
        ShowStatus(L.F("New {0} x {1} sprite", sprite.Width, sprite.Height));
    }

    /// <summary>A new layer above the current one holding the sprite, centred (scaled down to fit a smaller canvas). One undo step.</summary>
    internal void AddAiLayer(Image sprite)
    {
        var doc = Session.Doc;
        var img = FitInto(sprite, doc.Width, doc.Height);
        if (doc.Mode == ColorMode.Indexed)
            for (int i = 0; i < img.Pixels.Length; i++)
            {
                var c = Rgba.FromPacked(img.Pixels[i]);
                int near = c.A == 0 ? -1 : doc.Palette.Nearest(c, doc.Palette.TransparentIndex);
                img.Pixels[i] = near >= 0 ? (uint)near : doc.TransparentPixel; // the palette is not changed
            }
        var layer = new Layer { Name = FreeName("Layer") };
        int frame = Session.Frame;
        var cel = new Cel(img, (doc.Width - img.Width) / 2, (doc.Height - img.Height) / 2);
        ChangeLayers(L.N("Create with AI"), () =>
        {
            LayerTree.Insert(doc, layer, Session.Layer);
            doc.Cels[(layer, frame)] = cel;
        });
        Session.Layer = layer;
        ShowStatus(doc.Mode == ColorMode.Indexed
            ? L.T("The AI sprite was added as a new layer. Its colours were matched to the palette.")
            : L.T("The AI sprite was added as a new layer."));
    }

    /// <summary>The picture itself when it fits, else a nearest-neighbour copy scaled down to fit.</summary>
    private static Image FitInto(Image src, int width, int height)
    {
        if (src.Width <= width && src.Height <= height) return src.Clone();
        double k = Math.Min((double)width / src.Width, (double)height / src.Height);
        int w = Math.Max(1, (int)(src.Width * k)), h = Math.Max(1, (int)(src.Height * k));
        var res = new Image(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                res.Pixels[y * w + x] = src.Get(x * src.Width / w, y * src.Height / h);
        return res;
    }
}
