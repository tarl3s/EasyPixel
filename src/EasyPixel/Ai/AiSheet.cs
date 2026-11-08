using System.Text;
using EasyPixel.Core.Ai;
using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Ai;

/// <summary>
/// Dev tool: <c>EasyPixel.exe --ai-sheet out.png [--provider id]</c> runs a fixed set of prompts at 32 and 48 px and saves a
/// contact sheet (one row per prompt, each candidate at 1x and 6x, the prompt under the row) plus <c>out.png.txt</c>
/// with the automatic checks of every candidate, so the quality can be judged by eye.
/// </summary>
public static class AiSheet
{
    public static readonly string[] Prompts =
    [
        "faz um monstro parecido com um cervo, que anda em duas patas, as traseiras; ele tem a boca suja de sangue, olho arregalado e parece estar sempre procurando algo para atacar",
        "a knight in armor with a sword and shield",
        "a green slime",
        "a bat with spread wings",
        "a tree",
        "a treasure chest",
        "a red potion bottle",
        "a wooden door",
        "the front of a school building",
        "un lobo gris que aúlla a la luna",
        "a sword item icon",
        "a skeleton warrior",
    ];

    public static readonly int[] Sizes = [32, 48];
    private const int Zoom = 6, Gap = 4, LabelH = 14;

    public sealed record Check(string Name, bool Ok);

    /// <summary>The automatic checks of one finished sprite.</summary>
    public static IReadOnlyList<Check> Checks(Image img, int maxColors = 16)
    {
        int w = img.Width, h = img.Height;
        bool Opaque(int x, int y) => Rgba.FromPacked(img.Get(x, y)).A > 0;
        bool borderClear = true, binary = true;
        var colors = new HashSet<uint>();
        int minY = h, maxY = -1, orphans = 0, edge = 0, dark = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = Rgba.FromPacked(img.Get(x, y));
                if (p.A != 0 && p.A != 255) binary = false;
                if (p.A == 0) continue;
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1) borderClear = false;
                colors.Add(p.Packed);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                bool lonely = true, touchesEmpty = false;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (Opaque(x + dx, y + dy)) lonely = false;
                        if (dx * dy == 0 && !Opaque(x + dx, y + dy)) touchesEmpty = true;
                    }
                if (lonely) orphans++;
                if (touchesEmpty) { edge++; if (0.299 * p.R + 0.587 * p.G + 0.114 * p.B < 90) dark++; }
            }
        bool any = maxY >= 0;
        return
        [
            new("transparent around subject", any && borderClear),
            new($"colours <= {maxColors} ({colors.Count})", any && colors.Count <= maxColors),
            new("binary alpha", binary),
            new("outline present", any && edge > 0 && dark >= edge * 0.7),
            new("fills >= 30% of height", any && (maxY - minY + 1) >= h * 0.3),
            new("no orphan pixels", any && orphans == 0),
        ];
    }

    private static int CellW(int s) => AiCreateSession.Slots * (s + Zoom * s + 3 * Gap) + Gap;

    /// <summary>Runs the whole set. Returns the exit code: 0 done, 2 nothing to generate with.</summary>
    public static async Task<int> RunAsync(string outPng, IReadOnlyList<IImageGenerator> gens, ITextAi? text,
        Func<string, string?> keyFor, string[]? prompts = null, CancellationToken ct = default, TextWriter? log = null)
    {
        prompts ??= Prompts;
        if (gens.Count == 0)
        {
            (log ?? Console.Error).WriteLine("No picture generator with a key is set up. Add a key in Settings (or pass --provider id).");
            return 2;
        }
        int rowW = Sizes.Sum(CellW), rowH = Zoom * Sizes.Max() + LabelH + 2 * Gap;
        var sheet = new Image(rowW, rowH * prompts.Length, new Rgba(48, 48, 56, 255).Packed);
        var report = new StringBuilder();
        for (int r = 0; r < prompts.Length; r++)
        {
            int x0 = 0, y0 = r * rowH;
            foreach (var size in Sizes)
            {
                var session = new AiCreateSession(gens, text, keyFor);
                var done = new Image?[AiCreateSession.Slots];
                var errs = new string?[AiCreateSession.Slots];
                session.CandidateReady += (i, img, err) => { done[i] = img; errs[i] = err; };
                try { await session.RunAsync(prompts[r], size, SpriteView.Auto, null, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException) { for (int i = 0; i < done.Length; i++) errs[i] ??= ex is AiException ae ? AiErrors.Describe(ae) : ex.Message; }
                for (int i = 0; i < AiCreateSession.Slots; i++)
                {
                    var tag = $"P{r + 1:00} {size}px #{i + 1}";
                    if (done[i] is not { } img) { report.AppendLine($"{tag}: ERROR {errs[i]}"); continue; }
                    var checks = Checks(img);
                    report.AppendLine($"{tag}: {(checks.All(c => c.Ok) ? "PASS" : "FAIL")} | " +
                        string.Join("; ", checks.Select(c => (c.Ok ? "ok " : "NO ") + c.Name)));
                    int cx = x0 + Gap + i * (size + Zoom * size + 3 * Gap);
                    Blit(sheet, img, cx, y0 + Gap, 1);
                    Blit(sheet, img, cx + size + Gap, y0 + Gap, Zoom);
                }
                x0 += CellW(size);
            }
            report.AppendLine($"P{r + 1:00} prompt: {prompts[r]}");
            DrawText(sheet, prompts[r], Gap, y0 + rowH - LabelH);
            log?.WriteLine($"row {r + 1}/{prompts.Length}");
        }
        File.WriteAllBytes(outPng, Png.Encode(sheet));
        File.WriteAllText(outPng + ".txt", report.ToString());
        return 0;
    }

    private static void Blit(Image dst, Image src, int ox, int oy, int k)
    {
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var p = Rgba.FromPacked(src.Get(x, y));
                uint v = p.A == 0 ? ((((x / 2 + y / 2) & 1) == 0) ? new Rgba(70, 70, 80, 255) : new Rgba(60, 60, 70, 255)).Packed : p.Packed;
                for (int dy = 0; dy < k; dy++)
                    for (int dx = 0; dx < k; dx++) dst.Set(ox + x * k + dx, oy + y * k + dy, v);
            }
    }

    // 3x5 pixel font for the label: letters (case-folded, accents stripped), digits and a little punctuation.
    private static readonly Dictionary<char, string> Font = new()
    {
        ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
        ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
        ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
        ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100",
        ['Q'] = "010101101110011", ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010",
        ['U'] = "101101101101111", ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
        ['Y'] = "101101010010010", ['Z'] = "111001010100111",
        ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "110001010100111", ['3'] = "110001010001110",
        ['4'] = "101101111001001", ['5'] = "111100110001110", ['6'] = "011100111101111", ['7'] = "111001010010010",
        ['8'] = "111101111101111", ['9'] = "111101111001110",
        [','] = "000000000010100", ['.'] = "000000000000010", [';'] = "000010000010100", ['-'] = "000000111000000",
    };

    private static void DrawText(Image dst, string s, int x, int y)
    {
        var norm = s.Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        foreach (var ch in norm)
        {
            if (Font.TryGetValue(char.ToUpperInvariant(ch), out var g))
                for (int i = 0; i < 15; i++)
                    if (g[i] == '1')
                        for (int dy = 0; dy < 2; dy++)
                            for (int dx = 0; dx < 2; dx++) dst.Set(x + (i % 3) * 2 + dx, y + (i / 3) * 2 + dy, 0xFFE0E0E0);
            x += 8;
            if (x >= dst.Width - 8) break;
        }
    }

    /// <summary>Command-line entry: <c>--ai-sheet out.png [--provider id]</c>. Returns the exit code.</summary>
    public static async Task<int> RunCommandAsync(string[] args)
    {
        int i = Array.IndexOf(args, "--ai-sheet");
        if (i < 0 || i + 1 >= args.Length) { Console.Error.WriteLine("Usage: --ai-sheet <out.png> [--provider id]"); return 1; }
        var settings = App.Settings;
        int p = Array.IndexOf(args, "--provider");
        IReadOnlyList<IImageGenerator> gens = p >= 0 && p + 1 < args.Length
            ? (AiProviders.Image(args[p + 1]) is { } g ? [g] : [])
            : AiRouting.ImageGenerators(settings);
        gens = gens.Where(g => !g.NeedsKey || AiRouting.HasKey(settings, g.Id)).ToList();
        return await RunAsync(Path.GetFullPath(args[i + 1]), gens, AiRouting.TextAi(settings),
            id => settings.AiKeys.TryGetValue(id, out var k) ? k : null, log: Console.Out);
    }
}
