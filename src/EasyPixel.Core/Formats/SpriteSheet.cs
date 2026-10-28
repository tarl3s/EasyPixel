using System.Text.Json;
using EasyPixel.Core.Model;
using EasyPixel.Core.Render;

namespace EasyPixel.Core.Formats;

public enum SheetKind { Horizontal, Vertical, Grid }

/// <summary>How frames are arranged on a sheet. <see cref="Grid"/> fills rows left to right with <c>Columns</c> cells per row.</summary>
public readonly record struct SheetLayout(SheetKind Kind, int Columns = 0)
{
    public static SheetLayout Horizontal => new(SheetKind.Horizontal);
    public static SheetLayout Vertical => new(SheetKind.Vertical);
    public static SheetLayout Grid(int columns) => new(SheetKind.Grid, Math.Max(1, columns));
}

/// <summary>Frames to and from one picture. Padding is the gap in pixels between neighbouring cells (none around the outside).</summary>
public static class SpriteSheet
{
    /// <summary>The flattened frames on one RGBA sheet, plus a JSON description (frames with x, y, w, h, durationMs; tags; sheet size).</summary>
    public static (Image sheet, string json) Export(Sprite sp, SheetLayout layout, int padding)
    {
        padding = Math.Max(0, padding);
        int n = sp.Frames.Count;
        int cols = layout.Kind switch { SheetKind.Horizontal => n, SheetKind.Vertical => 1, _ => Math.Clamp(layout.Columns, 1, n) };
        int rows = (n + cols - 1) / cols;
        int cw = sp.Width, ch = sp.Height;
        var sheet = new Image(cols * cw + (cols - 1) * padding, rows * ch + (rows - 1) * padding);

        var flat = new Image(cw, ch);
        var all = new PixelRect(0, 0, cw, ch);
        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteStartArray("frames");
            for (int f = 0; f < n; f++)
            {
                int x = f % cols * (cw + padding), y = f / cols * (ch + padding);
                Compositor.Flatten(sp, f, flat, all);
                for (int j = 0; j < ch; j++)
                    Array.Copy(flat.Pixels, j * cw, sheet.Pixels, (y + j) * sheet.Width + x, cw);
                w.WriteStartObject();
                w.WriteNumber("x", x); w.WriteNumber("y", y); w.WriteNumber("w", cw); w.WriteNumber("h", ch);
                w.WriteNumber("durationMs", sp.Frames[f].DurationMs);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartArray("tags");
            foreach (var t in sp.Tags)
            {
                w.WriteStartObject();
                w.WriteString("name", t.Name);
                w.WriteNumber("from", t.From); w.WriteNumber("to", t.To);
                w.WriteString("direction", t.Dir.ToString().ToLowerInvariant());
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartObject("size");
            w.WriteNumber("w", sheet.Width); w.WriteNumber("h", sheet.Height);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        return (sheet, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
    }

    /// <summary>Cuts an RGBA picture into cells (left to right, top to bottom) as the frames of a one-layer sprite. Fully transparent cells at the end are dropped (at least one frame stays).</summary>
    public static Sprite Slice(Image img, int cellW, int cellH, int padding)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellW);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellH);
        padding = Math.Max(0, padding);
        int cols = (img.Width + padding) / (cellW + padding), rows = (img.Height + padding) / (cellH + padding);
        if (cols < 1 || rows < 1) throw new ArgumentException("The cell is larger than the picture.");

        var cells = new List<Image>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var cell = new Image(cellW, cellH);
                int x0 = c * (cellW + padding), y0 = r * (cellH + padding);
                for (int j = 0; j < cellH; j++)
                    Array.Copy(img.Pixels, (y0 + j) * img.Width + x0, cell.Pixels, j * cellW, cellW);
                cells.Add(cell);
            }
        int keep = cells.Count;
        while (keep > 1 && cells[keep - 1].Pixels.All(p => (p >> 24) == 0)) keep--;

        var sp = new Sprite(cellW, cellH);
        for (int f = 1; f < keep; f++) sp.Frames.Add(new Frame());
        for (int f = 0; f < keep; f++) sp.Cels[(sp.Layers[0], f)] = new Cel(cells[f]);
        return sp;
    }
}
