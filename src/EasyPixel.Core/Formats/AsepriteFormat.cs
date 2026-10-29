using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using EasyPixel.Core.Model;

namespace EasyPixel.Core.Formats;

/// <summary>
/// Aseprite <c>.aseprite</c>/<c>.ase</c> files, after the public spec (aseprite/docs/ase-file-specs.md).
/// <para>
/// Modelled: header (size, colour depth, transparent index), layers (0x2004: visibility, lock, opacity, blend,
/// groups through the child level), cels (0x2005: raw, linked, compressed image), tags (0x2018; the colour also comes
/// from the tag's user data, as Aseprite 1.3 writes it), palette (0x2019; the old 0x0004/0x0011 chunks only when the
/// file has no 0x2019). Greyscale files are read as RGBA. Saving writes RGBA (depth 32) or indexed (depth 8),
/// compressed cels, the layer-opacity header flag (and the group one when there are groups), a user-data chunk with
/// the colour for every tag, and after the 0x2019 palette an old 0x0004 chunk when the palette has ≤ 256 opaque
/// colours (as Aseprite does, for old readers).
/// </para>
/// <para>
/// Everything else is kept as raw chunks (6-byte chunk header included) on the object that owns it, so it follows that
/// object through edits and goes away with it:
/// <see cref="Layer.Extra"/> — <c>ase:after</c> (user data), <c>ase:flags</c> (all layer flags, type, tileset index),
/// <c>ase:uuid</c>, <c>ase:id</c> (file index at reading; names the layer for its tilemap cels), and EasyPixel's own <c>easypixel:*</c> keys (as the layer's user-data text when it has none of its own);
/// <see cref="Cel.Extra"/> — <c>ase:after</c> (user data, cel extra), <c>ase:z</c> (z-index);
/// <see cref="Tag.Extra"/> — <c>ase:userdata</c> (user-data body with text/properties), <c>ase:repeat</c>;
/// <see cref="Frame.Extra"/> — <c>ase:chunks</c> (the frame's other chunks — slices, palettes of later frames… — each
/// with the user data that follows it, in their order, written after the frame's modelled chunks) and
/// <c>ase:tilemap-cel:{id}</c> (a tilemap cel, written with the layer whose <c>ase:id</c> is id).
/// <see cref="Sprite.UnknownChunks"/> holds file-level data: <c>ase:before-palette</c> (colour profile…),
/// <c>ase:sprite-userdata</c>, <c>ase:before-layers</c> (tilesets…), <c>ase:header:grid</c> (pixel ratio and grid).
/// Data that cannot be written (a tilemap layer that was drawn on, a damaged entry, file-level keys of another
/// format) is dropped with <see cref="LostDataWarning"/>.
/// </para>
/// <para>Cel opacity is stored in 1/255 steps. A linked cel is the same <see cref="Cel"/> object under two frames.</para>
/// </summary>
public sealed class AsepriteFormat : ISpriteFormat
{
    public const string LostDataWarning = "Some Aseprite data (slices/user data/tilesets) could not be kept";
    public const string GreyscaleWarning = "Greyscale sprite converted to RGBA";
    public const string PingPongReverseWarning = "Ping-pong reverse tags were changed to ping-pong";
    public const string PaletteCutWarning = "The palette was cut to 256 colours";
    public const string PaletteNamesWarning = "Palette colour names were not kept";
    public const string BlendWarning = "An unknown layer blend mode was changed to Normal";
    public const string BrokenCelWarning = "Some empty or broken cels were skipped";
    public const string NoTransparentIndexWarning = "Aseprite needs a transparent palette index; index 0 was used";

    private const ushort FileMagic = 0xA5E0, FrameMagic = 0xF1FA;
    private const int HeaderSize = 128, FrameHeaderSize = 16, ChunkHeaderSize = 6;
    private const int MaxCelPixels = 1 << 26;
    private const uint FlagLayerOpacity = 1, FlagGroupOpacity = 2, FlagLayerUuid = 4;
    private const int LayerVisible = 1, LayerEditable = 2, LayerCollapsed = 32;
    private const int ChunkOldPalette = 0x0004, ChunkOldPalette64 = 0x0011, ChunkLayer = 0x2004, ChunkCel = 0x2005,
        ChunkCelExtra = 0x2006, ChunkTags = 0x2018, ChunkPalette = 0x2019, ChunkUserData = 0x2020;
    private const int CelRaw = 0, CelLinked = 1, CelCompressed = 2;
    private const int LayerNormal = 0, LayerGroup = 1, LayerTilemap = 2;
    private const uint UserText = 1, UserColor = 2;
    /// <summary>Header bytes 34–43 as Aseprite 1.3 writes them: pixel ratio 1:1, grid at 0,0 of 16×16.</summary>
    private static readonly byte[] DefaultGrid = { 1, 1, 0, 0, 0, 0, 16, 0, 16, 0 };

    private const string KeyBeforePalette = "ase:before-palette", KeySpriteUserData = "ase:sprite-userdata",
        KeyBeforeLayers = "ase:before-layers", KeyGrid = "ase:header:grid";
    private const string KeyAfter = "ase:after", KeyFlags = "ase:flags", KeyUuid = "ase:uuid", KeyId = "ase:id",
        KeyZ = "ase:z", KeyUserData = "ase:userdata", KeyRepeat = "ase:repeat", KeyChunks = "ase:chunks",
        KeyTilemapCel = "ase:tilemap-cel:";

    /// <summary>True when an Aseprite file showed this group folded (its "collapsed" layer flag, kept in Extra).</summary>
    public static bool IsCollapsed(Layer layer) =>
        layer.IsGroup && layer.Extra.TryGetValue(KeyFlags, out var b) && b.Length >= 2
        && (BinaryPrimitives.ReadUInt16LittleEndian(b) & LayerCollapsed) != 0;

    public string Name => "Aseprite sprite";
    public string[] Extensions { get; } = { ".aseprite", ".ase" };
    public bool CanRead => true;
    public bool CanWrite => true;

    private static FormatException Damaged() => new("This Aseprite file is damaged or incomplete and could not be read.");

    private static void Append(Dictionary<string, byte[]> dict, string key, byte[] raw) =>
        dict[key] = dict.TryGetValue(key, out var prev) ? [.. prev, .. raw] : raw;

    // ───────────────────────────── reading ─────────────────────────────

    public Sprite Read(Stream s, List<string> warnings)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        try { return new Reader(ms.ToArray(), warnings).Run(); }
        catch (FormatException) { throw; }
        catch (Exception e) when (e is InvalidDataException or OverflowException or ArgumentException or IOException)
        {
            throw new FormatException(Damaged().Message, e);
        }
    }

    /// <summary>Bounds-checked little-endian reader over part of a byte array; running past the end means a damaged file.</summary>
    private sealed class Cursor
    {
        private readonly byte[] _b;
        private readonly int _end;
        public int Pos { get; private set; }
        public Cursor(byte[] b, int start, int end) { _b = b; Pos = start; _end = end; }
        public int Remaining => _end - Pos;

        private ReadOnlySpan<byte> Take(int n)
        {
            if (n < 0 || Remaining < n) throw Damaged();
            var span = _b.AsSpan(Pos, n);
            Pos += n;
            return span;
        }

        public byte U8() => Take(1)[0];
        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        public short S16() => BinaryPrimitives.ReadInt16LittleEndian(Take(2));
        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
        public byte[] Bytes(int n) => Take(n).ToArray();
        public byte[] Rest() => Bytes(Remaining);
        public void Skip(int n) => Take(n);
        public string Str() => Encoding.UTF8.GetString(Take(U16()));
    }

    /// <summary>What a following user-data / cel-extra chunk belongs to.</summary>
    private abstract record Owner;
    private sealed record PaletteOwner : Owner;
    /// <summary>Following chunks are appended to <c>Dict[Key]</c>.</summary>
    private sealed record ExtraOwner(Dictionary<string, byte[]> Dict, string Key, bool IsLayer = false) : Owner;
    private sealed record LinkedCelOwner(Cel Cel) : Owner;
    private sealed record TagsOwner(int First, int Count) : Owner { public int Next { get; set; } }

    private sealed class Reader
    {
        private readonly byte[] _data;
        private readonly List<string> _warnings;
        private Sprite _sp = null!;
        private int _depth;
        private uint _headerFlags;
        private bool _sawPalette, _sawAnyPalette;
        private Rgba[]? _oldPalette;
        private int _oldPaletteSize;
        /// <summary>The last layer read at each child level, to find a layer's group.</summary>
        private readonly List<Layer> _levels = new();

        public Reader(byte[] data, List<string> warnings) { _data = data; _warnings = warnings; }

        private void Warn(string w) { if (!_warnings.Contains(w)) _warnings.Add(w); }

        public Sprite Run()
        {
            if (_data.Length < 6 || BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(4)) != FileMagic)
                throw new FormatException("This is not an Aseprite file.");
            var h = new Cursor(_data, 0, _data.Length);
            uint fileSize = h.U32();
            if (fileSize < HeaderSize || fileSize > _data.Length) throw Damaged();
            h.Skip(2);
            int frames = h.U16(), width = h.U16(), height = h.U16();
            _depth = h.U16();
            _headerFlags = h.U32();
            int speed = h.U16();
            h.Skip(8);
            int transparent = h.U8();
            h.Skip(3 + 2); // reserved, colour count (the palette chunks say it)
            byte[] grid = h.Bytes(DefaultGrid.Length);
            if (frames == 0 || width == 0 || height == 0) throw Damaged();
            if (_depth is not (8 or 16 or 32)) throw new FormatException($"This Aseprite file uses an unknown colour depth ({_depth}).");
            if (_depth == 16) Warn(GreyscaleWarning);

            var mode = _depth == 8 ? ColorMode.Indexed : ColorMode.Rgba;
            _sp = new Sprite(width, height, mode);
            // The constructor adds a default layer, frame and palette; the file defines everything itself.
            _sp.Layers.Clear();
            _sp.Frames.Clear();
            _sp.Palette = new Palette { TransparentIndex = mode == ColorMode.Indexed ? transparent : -1 };
            if (!grid.AsSpan().SequenceEqual(DefaultGrid)) _sp.UnknownChunks[KeyGrid] = grid;

            int pos = HeaderSize;
            for (int f = 0; f < frames; f++)
            {
                var fh = new Cursor(_data, pos, (int)fileSize);
                uint frameBytes = fh.U32();
                if (fh.U16() != FrameMagic || frameBytes < FrameHeaderSize || frameBytes > fileSize - pos) throw Damaged();
                int oldCount = fh.U16(), duration = fh.U16();
                fh.Skip(2);
                uint newCount = fh.U32();
                _sp.Frames.Add(new Frame { DurationMs = duration == 0 && speed > 0 ? speed : duration });
                int end = pos + (int)frameBytes;
                ReadChunks(f, newCount == 0 ? (uint)oldCount : newCount, fh.Pos, end);
                pos = end;
            }

            if (!_sawPalette && _oldPalette != null) _sp.Palette.Colors.AddRange(_oldPalette.Take(_oldPaletteSize));
            if (_sp.Layers.Count == 0) _sp.Layers.Add(new Layer());
            ClampTags();
            return _sp;
        }

        private void ReadChunks(int frame, uint count, int start, int end)
        {
            Owner? owner = null;
            int pos = start;
            for (int k = 0; k < count; k++)
            {
                var ch = new Cursor(_data, pos, end);
                uint size = ch.U32();
                if (size < ChunkHeaderSize || size > end - pos) throw Damaged();
                int type = ch.U16();
                var body = new Cursor(_data, pos + ChunkHeaderSize, pos + (int)size);
                byte[] raw = _data[pos..(pos + (int)size)];
                pos += (int)size;

                switch (type)
                {
                    case ChunkPalette when frame == 0:
                        ReadPalette(body);
                        _sawPalette = _sawAnyPalette = true;
                        owner = new PaletteOwner();
                        break;
                    case ChunkOldPalette or ChunkOldPalette64 when frame == 0:
                        ReadOldPalette(body, type == ChunkOldPalette64);
                        _sawAnyPalette = true;
                        owner = new PaletteOwner();
                        break;
                    case ChunkLayer:
                        owner = new ExtraOwner(ReadLayer(body).Extra, KeyAfter, IsLayer: true);
                        break;
                    case ChunkCel:
                        owner = ReadCel(body, frame, raw);
                        break;
                    case ChunkTags:
                        int first = _sp.Tags.Count;
                        ReadTags(body);
                        owner = new TagsOwner(first, _sp.Tags.Count - first);
                        break;
                    case ChunkUserData or ChunkCelExtra when owner is not null && Attach(owner, frame, type, raw, body):
                        break;
                    default:
                        // A chunk we don't model: kept in order, together with the chunks that follow it.
                        var run = RunFor(frame);
                        Append(run.Dict, run.Key, raw);
                        owner = run;
                        break;
                }
            }
        }

        /// <summary>Where unmodelled chunks of this place in the file are kept.</summary>
        private ExtraOwner RunFor(int frame) =>
            frame == 0 && _sp.Layers.Count == 0
                ? new ExtraOwner(_sp.UnknownChunks, _sawAnyPalette ? KeyBeforeLayers : KeyBeforePalette)
                : new ExtraOwner(_sp.Frames[frame].Extra, KeyChunks);

        /// <summary>Keeps a user-data / cel-extra chunk with the object it follows. False when it belongs to nothing we track.</summary>
        private bool Attach(Owner owner, int frame, int type, byte[] raw, Cursor body)
        {
            switch (owner)
            {
                case ExtraOwner e:
                    if (e.IsLayer && type == ChunkUserData && !e.Dict.ContainsKey(KeyAfter) && EasyPixelKeys.TryRead(body.Rest(), e.Dict)) return true;
                    Append(e.Dict, e.Key, raw);
                    return true;
                case PaletteOwner when type == ChunkUserData:
                    Append(_sp.UnknownChunks, KeySpriteUserData, raw);
                    return true;
                case LinkedCelOwner l:
                    // The shared cel already holds its user data from the first frame; anything else stays with this frame.
                    if (l.Cel.Extra.TryGetValue(KeyAfter, out var after) && after.AsSpan().IndexOf(raw.AsSpan()) >= 0) return true;
                    Append(_sp.Frames[frame].Extra, KeyChunks, raw);
                    return true;
                case TagsOwner t when type == ChunkUserData && t.Next < t.Count:
                    var tag = _sp.Tags[t.First + t.Next++];
                    var all = body.Rest();
                    var (flags, color) = UserData.Read(all);
                    if (color is { } c) tag.Color = c;
                    if ((flags & ~UserColor) != 0) tag.Extra[KeyUserData] = all;
                    return true;
                default:
                    return false;
            }
        }

        private void ReadPalette(Cursor c)
        {
            uint size = c.U32(), first = c.U32(), last = c.U32();
            c.Skip(8);
            if (size > Palette.MaxColors) Warn(PaletteCutWarning);
            int keep = (int)Math.Min(size, Palette.MaxColors);
            var colors = _sp.Palette.Colors;
            if (colors.Count > keep) colors.RemoveRange(keep, colors.Count - keep);
            while (colors.Count < keep) colors.Add(new Rgba(0, 0, 0, 255));
            for (long i = first; i <= last; i++)
            {
                int flags = c.U16();
                var color = new Rgba(c.U8(), c.U8(), c.U8(), c.U8());
                if ((flags & 1) != 0) { c.Str(); Warn(PaletteNamesWarning); }
                if (i < keep) colors[(int)i] = color;
            }
        }

        private void ReadOldPalette(Cursor c, bool sixBits)
        {
            _oldPalette ??= Enumerable.Repeat(new Rgba(0, 0, 0, 255), Palette.MaxColors).ToArray();
            int packets = c.U16(), index = 0;
            for (int p = 0; p < packets; p++)
            {
                index += c.U8();
                int n = c.U8();
                if (n == 0) n = 256;
                for (int i = 0; i < n; i++, index++)
                {
                    byte r = c.U8(), g = c.U8(), b = c.U8();
                    if (sixBits) { r = Scale6(r); g = Scale6(g); b = Scale6(b); }
                    if (index < Palette.MaxColors) _oldPalette[index] = new Rgba(r, g, b, 255);
                }
                _oldPaletteSize = Math.Max(_oldPaletteSize, Math.Min(index, Palette.MaxColors));
            }
        }

        private static byte Scale6(byte v) { v &= 63; return (byte)(v << 2 | v >> 4); }

        private Layer ReadLayer(Cursor c)
        {
            int flags = c.U16(), type = c.U16(), level = c.U16();
            c.Skip(4); // default width/height, ignored
            int blend = c.U16();
            byte opacity = c.U8();
            c.Skip(3);
            string name = c.Str();
            uint tileset = type == LayerTilemap ? c.U32() : 0;
            byte[]? uuid = (_headerFlags & FlagLayerUuid) != 0 ? c.Bytes(16) : null;

            bool isGroup = type == LayerGroup;
            bool groupValid = !isGroup || (_headerFlags & FlagGroupOpacity) != 0;
            if (blend > (int)BlendMode.Divide) { Warn(BlendWarning); blend = 0; }

            int lvl = Math.Min(level, _levels.Count);
            var parent = lvl > 0 && _levels[lvl - 1].IsGroup ? _levels[lvl - 1] : null;
            var layer = new Layer
            {
                Name = name,
                Visible = (flags & LayerVisible) != 0,
                Locked = (flags & LayerEditable) == 0,
                Opacity = (_headerFlags & FlagLayerOpacity) != 0 && groupValid ? opacity : (byte)255,
                Blend = groupValid ? (BlendMode)blend : BlendMode.Normal,
                IsGroup = isGroup,
                Parent = parent,
            };
            _levels.RemoveRange(lvl, _levels.Count - lvl);
            _levels.Add(layer);
            _sp.Layers.Add(layer);

            if ((flags & ~(LayerVisible | LayerEditable)) != 0 || type == LayerTilemap)
                layer.Extra[KeyFlags] = new Out().U16(flags).U16(type).U32(tileset).ToArray();
            if (uuid != null) layer.Extra[KeyUuid] = uuid;
            return layer;
        }

        /// <summary>Reads a cel into the model (or keeps a tilemap cel as raw data); returns what following user data belongs to.</summary>
        private Owner? ReadCel(Cursor c, int frame, byte[] raw)
        {
            int li = c.U16();
            int x = c.S16(), y = c.S16();
            byte opacity = c.U8();
            int type = c.U16();
            short z = c.S16();
            c.Skip(5);
            if (li >= _sp.Layers.Count) throw new FormatException("A cel in this Aseprite file points at a missing layer.");
            var layer = _sp.Layers[li];

            if (type is not (CelRaw or CelLinked or CelCompressed))
            {
                if (!layer.Extra.ContainsKey(KeyId)) layer.Extra[KeyId] = BitConverter.GetBytes(li);
                var key = KeyTilemapCel + BitConverter.ToInt32(layer.Extra[KeyId]);
                Append(_sp.Frames[frame].Extra, key, raw);
                return new ExtraOwner(_sp.Frames[frame].Extra, key);
            }
            if (type == CelLinked)
            {
                int target = c.U16();
                if (target < frame && _sp.Cels.TryGetValue((layer, target), out var linked))
                {
                    _sp.Cels[(layer, frame)] = linked;
                    return new LinkedCelOwner(linked);
                }
                Warn(BrokenCelWarning);
                return null;
            }
            int w = c.U16(), h = c.U16();
            if (w == 0 || h == 0) { Warn(BrokenCelWarning); return null; }
            if ((long)w * h > MaxCelPixels) throw new FormatException("A cel in this Aseprite file is too large.");
            int n = w * h * (_depth / 8);
            byte[] px = type == CelRaw ? c.Bytes(n) : Inflate(c.Rest(), n);
            var cel = new Cel(DecodePixels(px, w, h), x, y) { Opacity = opacity / 255f };
            if (z != 0) cel.Extra[KeyZ] = BitConverter.GetBytes(z);
            _sp.Cels[(layer, frame)] = cel;
            return new ExtraOwner(cel.Extra, KeyAfter);
        }

        private static byte[] Inflate(byte[] compressed, int n)
        {
            var px = new byte[n];
            using var zs = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
            if (zs.ReadAtLeast(px, n, throwOnEndOfStream: false) < n) throw Damaged();
            return px;
        }

        private Image DecodePixels(byte[] px, int w, int h)
        {
            var img = new Image(w, h);
            var p = img.Pixels;
            for (int i = 0; i < p.Length; i++)
                p[i] = _depth switch
                {
                    32 => BinaryPrimitives.ReadUInt32LittleEndian(px.AsSpan(i * 4)), // R,G,B,A bytes = Rgba.Packed
                    16 => new Rgba(px[i * 2], px[i * 2], px[i * 2], px[i * 2 + 1]).Packed,
                    _ => px[i],
                };
            return img;
        }

        private void ReadTags(Cursor c)
        {
            int count = c.U16();
            c.Skip(8);
            for (int i = 0; i < count; i++)
            {
                int from = c.U16(), to = c.U16(), dir = c.U8(), repeat = c.U16();
                c.Skip(6);
                var color = new Rgba(c.U8(), c.U8(), c.U8(), 255);
                c.Skip(1);
                var tag = new Tag
                {
                    From = from, To = to, Color = color, Name = c.Str(),
                    Dir = dir switch { 1 => LoopDir.Reverse, 2 or 3 => LoopDir.PingPong, _ => LoopDir.Forward },
                };
                if (dir == 3) Warn(PingPongReverseWarning);
                if (repeat != 0) tag.Extra[KeyRepeat] = BitConverter.GetBytes((ushort)repeat);
                _sp.Tags.Add(tag);
            }
        }

        private void ClampTags()
        {
            int last = _sp.Frames.Count - 1;
            foreach (var t in _sp.Tags)
            {
                t.To = Math.Clamp(t.To, 0, last);
                t.From = Math.Clamp(t.From, 0, t.To);
            }
        }
    }

    /// <summary>User-data chunk bodies: flags, optional text, optional colour, then properties (kept as bytes).</summary>
    private static class UserData
    {
        public static (uint Flags, Rgba? Color) Read(byte[] body)
        {
            var r = new Cursor(body, 0, body.Length);
            uint flags = r.U32();
            if ((flags & UserText) != 0) r.Str();
            return (flags, (flags & UserColor) != 0 ? new Rgba(r.U8(), r.U8(), r.U8(), r.U8()) : null);
        }

        /// <summary>The body with its colour set to <paramref name="color"/>, keeping text and properties.</summary>
        public static byte[] WithColor(byte[]? body, Rgba color)
        {
            uint flags = 0;
            byte[] text = Array.Empty<byte>(), rest = Array.Empty<byte>();
            if (body != null)
            {
                var r = new Cursor(body, 0, body.Length);
                flags = r.U32();
                int textStart = r.Pos;
                if ((flags & UserText) != 0) r.Str();
                text = body[textStart..r.Pos];
                if ((flags & UserColor) != 0) r.Skip(4);
                rest = body[r.Pos..];
            }
            return new Out().U32(flags | UserColor).Bytes(text).U8(color.R).U8(color.G).U8(color.B).U8(color.A).Bytes(rest).ToArray();
        }
    }

    /// <summary>
    /// EasyPixel's own layer keys (<c>easypixel:*</c> in <see cref="Layer.Extra"/>, e.g. a practice's guide mark and exercise
    /// id) travel as the layer's user-data text, one <c>key=base64</c> per line, written only when the layer has no user
    /// data of its own. Reading turns such a text back into the keys (keeping no raw chunk), so the file round-trips byte
    /// for byte.
    /// </summary>
    private static class EasyPixelKeys
    {
        private const string Prefix = "easypixel:";

        public static string? Text(Dictionary<string, byte[]> extra)
        {
            var lines = extra.Where(kv => kv.Key.StartsWith(Prefix, StringComparison.Ordinal) && !kv.Key.Contains('=') && !kv.Key.Contains('\n'))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => kv.Key + "=" + Convert.ToBase64String(kv.Value)).ToList();
            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public static bool TryRead(byte[] body, Dictionary<string, byte[]> into)
        {
            try
            {
                var r = new Cursor(body, 0, body.Length);
                if (r.U32() != UserText) return false;
                var text = r.Str();
                if (r.Remaining != 0 || !text.StartsWith(Prefix, StringComparison.Ordinal)) return false;
                var found = new Dictionary<string, byte[]>();
                foreach (var line in text.Split('\n'))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= Prefix.Length || !line.StartsWith(Prefix, StringComparison.Ordinal)) return false;
                    found[line[..eq]] = Convert.FromBase64String(line[(eq + 1)..]);
                }
                foreach (var (k, v) in found) into[k] = v;
                return true;
            }
            catch (FormatException) { return false; } // not ours (Damaged() and bad base64 are both FormatException)
        }
    }

    // ───────────────────────────── writing ─────────────────────────────

    private sealed class Out
    {
        private readonly MemoryStream _ms = new();
        public Out U8(int v) { _ms.WriteByte((byte)v); return this; }
        public Out U16(int v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)v); _ms.Write(b); return this; }
        public Out S16(int v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteInt16LittleEndian(b, (short)v); _ms.Write(b); return this; }
        public Out U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); _ms.Write(b); return this; }
        public Out Zero(int n) { for (int i = 0; i < n; i++) _ms.WriteByte(0); return this; }
        public Out Bytes(ReadOnlySpan<byte> b) { _ms.Write(b); return this; }
        public Out Str(string s) { var b = Encoding.UTF8.GetBytes(s); U16(b.Length); return Bytes(b); }
        public byte[] ToArray() => _ms.ToArray();
    }

    private static byte[] Chunk(int type, Out body)
    {
        var d = body.ToArray();
        return new Out().U32((uint)(ChunkHeaderSize + d.Length)).U16(type).Bytes(d).ToArray();
    }

    /// <summary>Splits concatenated raw chunks into copies; null when the bytes are not whole chunks.</summary>
    private static List<byte[]>? SplitChunks(byte[] b)
    {
        var list = new List<byte[]>();
        for (int p = 0; p < b.Length;)
        {
            if (b.Length - p < ChunkHeaderSize) return null;
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(p));
            if (size < ChunkHeaderSize || size > b.Length - p) return null;
            list.Add(b[p..(p + (int)size)]);
            p += (int)size;
        }
        return list;
    }

    public void Write(Sprite sp, Stream s, List<string> warnings) => new Writer(sp, warnings).Run(s);

    private sealed class Writer
    {
        private static readonly HashSet<string> FileLevelKeys = new() { KeyBeforePalette, KeySpriteUserData, KeyBeforeLayers, KeyGrid };

        private readonly Sprite _sp;
        private readonly List<string> _warnings;
        private bool _lost;
        /// <summary>Layers in file order: stacking order with each group right before its members.</summary>
        private readonly List<Layer> _order = new();
        /// <summary>File index of each layer written as a tilemap, by its <c>ase:id</c>.</summary>
        private readonly Dictionary<int, int> _tilemapIndexById = new();
        /// <summary><c>ase:id</c> of every layer still in the sprite.</summary>
        private readonly HashSet<int> _knownIds = new();

        public Writer(Sprite sp, List<string> warnings) { _sp = sp; _warnings = warnings; }

        /// <summary>The raw chunks stored under <paramref name="key"/>; none (and a warning) when they are not whole chunks.</summary>
        private List<byte[]> Chunks(Dictionary<string, byte[]> dict, string key)
        {
            if (!dict.TryGetValue(key, out var raw)) return new();
            var list = SplitChunks(raw);
            if (list == null) { _lost = true; return new(); }
            return list;
        }

        public void Run(Stream s)
        {
            if (_sp.Width > ushort.MaxValue || _sp.Height > ushort.MaxValue || _sp.Frames.Count > ushort.MaxValue)
                throw new InvalidOperationException("Aseprite files hold at most 65535 frames of at most 65535×65535 pixels.");
            AddInFileOrder(null);

            var layerChunks = WriteLayers(out bool uuids);
            var frames = new List<byte[]>();
            for (int f = 0; f < _sp.Frames.Count; f++) frames.Add(WriteFrame(f, layerChunks));

            bool indexed = _sp.Mode == ColorMode.Indexed;
            int transparent = _sp.Palette.TransparentIndex;
            if (indexed && transparent < 0) { _warnings.Add(NoTransparentIndexWarning); transparent = 0; }
            var grid = DefaultGrid;
            if (_sp.UnknownChunks.TryGetValue(KeyGrid, out var g))
            {
                if (g.Length == DefaultGrid.Length) grid = g; else _lost = true;
            }
            long total = HeaderSize + frames.Sum(f => (long)f.Length);
            if (total > uint.MaxValue) throw new InvalidOperationException("The sprite is too large for an Aseprite file.");

            uint flags = FlagLayerOpacity | (_order.Any(l => l.IsGroup) ? FlagGroupOpacity : 0) | (uuids ? FlagLayerUuid : 0);
            var header = new Out().U32((uint)total).U16(FileMagic).U16(_sp.Frames.Count).U16(_sp.Width).U16(_sp.Height)
                .U16(indexed ? 8 : 32).U32(flags).U16(Math.Clamp(_sp.Frames[0].DurationMs, 0, ushort.MaxValue)).Zero(8)
                .U8(indexed ? Math.Clamp(transparent, 0, 255) : 0).Zero(3)
                .U16(Math.Max(1, _sp.Palette.Colors.Count)).Bytes(grid).Zero(84).ToArray();
            s.Write(header);
            foreach (var f in frames) s.Write(f);

            if (_lost || _sp.UnknownChunks.Keys.Any(k => !FileLevelKeys.Contains(k))) _warnings.Add(LostDataWarning);
        }

        private void AddInFileOrder(Layer? parent)
        {
            foreach (var l in _sp.Layers)
            {
                var p = l.Parent != null && l.Parent.IsGroup && _sp.Layers.Contains(l.Parent) ? l.Parent : null;
                if (p != parent || _order.Contains(l)) continue;
                _order.Add(l);
                if (l.IsGroup) AddInFileOrder(l);
            }
        }

        private int Level(Layer l)
        {
            int n = 0;
            for (var p = l.Parent; p != null && _order.Contains(p) && n <= _order.Count; p = p.Parent) n++;
            return n;
        }

        /// <summary>For every layer in file order: its layer chunk followed by the chunks attached to it.</summary>
        private List<List<byte[]>> WriteLayers(out bool uuids)
        {
            uuids = _order.Any(l => l.Extra.TryGetValue(KeyUuid, out var u) && u.Length == 16);
            var result = new List<List<byte[]>>();
            for (int i = 0; i < _order.Count; i++)
            {
                var l = _order[i];
                int flagsKept = 0, type = l.IsGroup ? LayerGroup : LayerNormal;
                uint tileset = 0;
                if (l.Extra.TryGetValue(KeyFlags, out var fx))
                {
                    if (fx.Length != 8) _lost = true;
                    else
                    {
                        flagsKept = BinaryPrimitives.ReadUInt16LittleEndian(fx);
                        if (BinaryPrimitives.ReadUInt16LittleEndian(fx.AsSpan(2)) == LayerTilemap && !l.IsGroup)
                        {
                            if (_sp.Cels.Keys.Any(k => k.Item1 == l)) _lost = true; // drawn on: saved as a normal layer
                            else { type = LayerTilemap; tileset = BinaryPrimitives.ReadUInt32LittleEndian(fx.AsSpan(4)); }
                        }
                    }
                }
                if (l.Extra.TryGetValue(KeyId, out var idb) && idb.Length == 4)
                {
                    int id = BitConverter.ToInt32(idb);
                    _knownIds.Add(id);
                    if (type == LayerTilemap) _tilemapIndexById[id] = i;
                }

                int flags = flagsKept & ~(LayerVisible | LayerEditable) | (l.Visible ? LayerVisible : 0) | (l.Locked ? 0 : LayerEditable);
                var body = new Out().U16(flags).U16(type).U16(Level(l)).U16(0).U16(0).U16((int)l.Blend).U8(l.Opacity).Zero(3).Str(l.Name);
                if (type == LayerTilemap) body.U32(tileset);
                if (uuids) body.Bytes(l.Extra.TryGetValue(KeyUuid, out var u) && u.Length == 16 ? u : NewUuid(i, l.Name));
                var chunks = new List<byte[]> { Chunk(ChunkLayer, body) };
                chunks.AddRange(Chunks(l.Extra, KeyAfter));
                if (!l.Extra.ContainsKey(KeyAfter) && EasyPixelKeys.Text(l.Extra) is { } mine)
                    chunks.Add(Chunk(ChunkUserData, new Out().U32(UserText).Str(mine)));
                result.Add(chunks);
            }
            return result;
        }

        /// <summary>A UUID for a layer that has none, derived from its place and name so saving twice gives the same file.</summary>
        private static byte[] NewUuid(int index, string name) => MD5.HashData(Encoding.UTF8.GetBytes($"easypixel-layer:{index}:{name}"));

        private byte[] WriteFrame(int f, List<List<byte[]>> layerChunks)
        {
            var chunks = new List<byte[]>();
            var frameExtra = _sp.Frames[f].Extra;

            if (f == 0)
            {
                chunks.AddRange(Chunks(_sp.UnknownChunks, KeyBeforePalette));
                chunks.AddRange(PaletteChunks());
                chunks.AddRange(Chunks(_sp.UnknownChunks, KeySpriteUserData));
                chunks.AddRange(Chunks(_sp.UnknownChunks, KeyBeforeLayers));
                foreach (var lc in layerChunks) chunks.AddRange(lc);
            }

            for (int i = 0; i < _order.Count; i++)
            {
                var layer = _order[i];
                if (!_sp.Cels.TryGetValue((layer, f), out var cel)) continue;
                if (layer.IsGroup) { _lost = true; continue; } // Aseprite groups have no pixels
                int link = LinkedFrame(layer, cel, f);
                chunks.Add(CelChunk(i, cel, link));
                if (link < 0) chunks.AddRange(Chunks(cel.Extra, KeyAfter));
            }
            foreach (var key in frameExtra.Keys.Where(k => k.StartsWith(KeyTilemapCel, StringComparison.Ordinal)))
            {
                bool idOk = int.TryParse(key.AsSpan(KeyTilemapCel.Length), out int id);
                if (idOk && _tilemapIndexById.TryGetValue(id, out int li))
                {
                    var tc = Chunks(frameExtra, key);
                    if (tc.Count == 0 || tc[0].Length < 8) { _lost = true; continue; }
                    BinaryPrimitives.WriteUInt16LittleEndian(tc[0].AsSpan(6), (ushort)li); // its layer's current index
                    InsertCelInLayerOrder(chunks, tc, li);
                }
                else if (!idOk || _knownIds.Contains(id)) _lost = true; // the layer is still there but no longer a tilemap
            }

            if (f == 0 && _sp.Tags.Count > 0) chunks.AddRange(TagChunks());
            chunks.AddRange(Chunks(frameExtra, KeyChunks));

            int n = chunks.Count;
            var frame = new Out().U32((uint)(FrameHeaderSize + chunks.Sum(c => c.Length))).U16(FrameMagic)
                .U16(Math.Min(n, 0xFFFF)).U16(Math.Clamp(_sp.Frames[f].DurationMs, 0, ushort.MaxValue)).Zero(2)
                .U32(n < 0xFFFF ? 0u : (uint)n); // as Aseprite: the new count only when the old field is too small
            foreach (var c in chunks) frame.Bytes(c);
            return frame.ToArray();
        }

        /// <summary>Puts a tilemap cel (and its user data) before the first cel of a higher layer, as Aseprite orders cels.</summary>
        private static void InsertCelInLayerOrder(List<byte[]> chunks, List<byte[]> cel, int layerIndex)
        {
            int at = chunks.FindIndex(c => BinaryPrimitives.ReadUInt16LittleEndian(c.AsSpan(4)) == ChunkCel
                                           && BinaryPrimitives.ReadUInt16LittleEndian(c.AsSpan(6)) > layerIndex);
            chunks.InsertRange(at < 0 ? chunks.Count : at, cel);
        }

        private int LinkedFrame(Layer layer, Cel cel, int f)
        {
            for (int g = 0; g < f; g++)
                if (_sp.Cels.TryGetValue((layer, g), out var other) && ReferenceEquals(other, cel)) return g;
            return -1;
        }

        private List<byte[]> PaletteChunks()
        {
            var colors = _sp.Palette.Colors;
            var list = new List<byte[]>();
            if (colors.Count == 0) return list;
            var body = new Out().U32((uint)colors.Count).U32(0).U32((uint)colors.Count - 1).Zero(8);
            foreach (var c in colors) body.U16(0).U8(c.R).U8(c.G).U8(c.B).U8(c.A);
            list.Add(Chunk(ChunkPalette, body));
            if (colors.Count <= 256 && colors.All(c => c.A == 255))
            {
                var old = new Out().U16(1).U8(0).U8(colors.Count == 256 ? 0 : colors.Count);
                foreach (var c in colors) old.U8(c.R).U8(c.G).U8(c.B);
                list.Add(Chunk(ChunkOldPalette, old));
            }
            return list;
        }

        /// <summary>The tags chunk, then one user-data chunk per tag carrying its colour (and any text/properties it had).</summary>
        private List<byte[]> TagChunks()
        {
            var body = new Out().U16(_sp.Tags.Count).Zero(8);
            var userData = new List<byte[]>();
            foreach (var t in _sp.Tags)
            {
                int repeat = t.Extra.TryGetValue(KeyRepeat, out var r) && r.Length == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(r) : 0;
                body.U16(Math.Clamp(t.From, 0, ushort.MaxValue)).U16(Math.Clamp(t.To, 0, ushort.MaxValue))
                    .U8(t.Dir switch { LoopDir.Reverse => 1, LoopDir.PingPong => 2, _ => 0 }).U16(repeat).Zero(6)
                    .U8(t.Color.R).U8(t.Color.G).U8(t.Color.B).U8(0).Str(t.Name);
                byte[] ud;
                try { ud = UserData.WithColor(t.Extra.GetValueOrDefault(KeyUserData), t.Color); }
                catch (FormatException) { _lost = true; ud = UserData.WithColor(null, t.Color); }
                userData.Add(Chunk(ChunkUserData, new Out().Bytes(ud)));
            }
            return [Chunk(ChunkTags, body), .. userData];
        }

        private byte[] CelChunk(int li, Cel cel, int link)
        {
            if (cel.X is < short.MinValue or > short.MaxValue || cel.Y is < short.MinValue or > short.MaxValue)
                throw new InvalidOperationException("A cel lies too far outside the canvas for an Aseprite file.");
            short z = cel.Extra.TryGetValue(KeyZ, out var zb) && zb.Length == 2 ? BinaryPrimitives.ReadInt16LittleEndian(zb) : (short)0;
            var body = new Out().U16(li).S16(cel.X).S16(cel.Y).U8((int)Math.Round(Math.Clamp(cel.Opacity, 0f, 1f) * 255))
                .U16(link >= 0 ? CelLinked : CelCompressed).S16(z).Zero(5);
            if (link >= 0) return Chunk(ChunkCel, body.U16(link));

            var img = cel.Image;
            bool indexed = _sp.Mode == ColorMode.Indexed;
            var px = new byte[img.Pixels.Length * (indexed ? 1 : 4)];
            for (int i = 0; i < img.Pixels.Length; i++)
                if (indexed) px[i] = (byte)img.Pixels[i];
                else BinaryPrimitives.WriteUInt32LittleEndian(px.AsSpan(i * 4), img.Pixels[i]);
            var compressed = new MemoryStream();
            using (var zs = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) zs.Write(px);
            return Chunk(ChunkCel, body.U16(img.Width).U16(img.Height).Bytes(compressed.ToArray()));
        }
    }
}
