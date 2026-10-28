using System.Buffers.Binary;
using System.IO.Compression;
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
/// compressed cels, the layer-opacity and group-opacity header flags, a user-data chunk with the colour for every tag,
/// and also an old 0x0004 palette chunk when the palette has ≤ 256 opaque colours (as Aseprite does, for old readers).
/// </para>
/// <para>
/// Everything else goes into <see cref="Sprite.UnknownChunks"/> as raw chunks (6-byte chunk header included) and is
/// written back in the same place. Keys:
/// <c>ase:frame{N}:{k}:{type:X4}</c> — a chunk on its own at position k of frame N (colour profile, slices, tilesets,
/// tilemap cels, palettes of later frames…), put back at that position;
/// <c>ase:layer{i}:{name}:after</c>, <c>ase:frame{N}:cel{L}:after</c>, <c>ase:after-palette</c> — chunks (user data,
/// cel extra) attached to a layer, a cel or the sprite, written right after their owner;
/// <c>ase:layer{i}:{name}:extra</c> — layer flags EasyPixel does not model, tilemap type, tileset index and UUID;
/// <c>ase:frame{N}:cel{L}:z</c> — a cel's z-index; <c>ase:tag{i}:{name}:userdata</c> / <c>:repeat</c> — a tag's user-data
/// text/properties and its repeat count; <c>ase:header:grid</c> — pixel ratio and grid. Layer indices are file order,
/// which for a sprite read from a file is its layer list order. When an owner no longer exists (frame deleted, layer
/// renamed or moved, a tilemap layer drawn on), its data is dropped and <see cref="LostDataWarning"/> is reported.
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
    public const string NoTransparentIndexWarning = "Aseprite needs a transparent palette index; index 0 was used";

    private const ushort FileMagic = 0xA5E0, FrameMagic = 0xF1FA;
    private const int HeaderSize = 128, FrameHeaderSize = 16, ChunkHeaderSize = 6;
    private const int MaxCelPixels = 1 << 26;
    private const uint FlagLayerOpacity = 1, FlagGroupOpacity = 2, FlagLayerUuid = 4;
    private const int LayerVisible = 1, LayerEditable = 2;
    private const int ChunkOldPalette = 0x0004, ChunkOldPalette64 = 0x0011, ChunkLayer = 0x2004, ChunkCel = 0x2005,
        ChunkCelExtra = 0x2006, ChunkTags = 0x2018, ChunkPalette = 0x2019, ChunkUserData = 0x2020;
    private const int CelRaw = 0, CelLinked = 1, CelCompressed = 2;
    private const int LayerNormal = 0, LayerGroup = 1, LayerTilemap = 2;
    private const uint UserText = 1, UserColor = 2;
    /// <summary>Header bytes 34–43 as Aseprite writes them: pixel ratio 1:1, grid at 0,0 of 16×16.</summary>
    private static readonly byte[] DefaultGrid = { 1, 1, 0, 0, 0, 0, 16, 0, 16, 0 };

    public string Name => "Aseprite sprite";
    public string[] Extensions { get; } = { ".aseprite", ".ase" };
    public bool CanRead => true;
    public bool CanWrite => true;

    private static FormatException Damaged() => new("This Aseprite file is damaged or incomplete and could not be read.");

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
    private sealed record LayerOwner(int Index) : Owner;
    private sealed record CelOwner(int Frame, int Layer) : Owner;
    private sealed record TagsOwner(int First, int Count) : Owner { public int Next { get; set; } }

    private sealed class Reader
    {
        private readonly byte[] _data;
        private readonly List<string> _warnings;
        private Sprite _sp = null!;
        private int _depth;
        private uint _headerFlags;
        private bool _sawPalette;
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
            if (!grid.AsSpan().SequenceEqual(DefaultGrid) && grid.Any(b => b != 0)) _sp.UnknownChunks["ase:header:grid"] = grid;

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
                        _sawPalette = true;
                        owner = new PaletteOwner();
                        break;
                    case ChunkOldPalette or ChunkOldPalette64 when frame == 0:
                        ReadOldPalette(body, type == ChunkOldPalette64);
                        owner = new PaletteOwner();
                        break;
                    case ChunkLayer:
                        ReadLayer(body);
                        owner = new LayerOwner(_sp.Layers.Count - 1);
                        break;
                    case ChunkCel when ReadCel(body, frame) is int layer:
                        owner = new CelOwner(frame, layer);
                        break;
                    case ChunkTags:
                        int first = _sp.Tags.Count;
                        ReadTags(body);
                        owner = new TagsOwner(first, _sp.Tags.Count - first);
                        break;
                    case ChunkUserData or ChunkCelExtra when owner is not null && Attach(owner, type, raw, body):
                        break;
                    default:
                        _sp.UnknownChunks[$"ase:frame{frame}:{k}:{type:X4}"] = raw;
                        owner = null;
                        break;
                }
            }
        }

        /// <summary>Keeps a user-data / cel-extra chunk with the object it follows. False when it belongs to nothing we track.</summary>
        private bool Attach(Owner owner, int type, byte[] raw, Cursor body)
        {
            string? key = owner switch
            {
                PaletteOwner when type == ChunkUserData => "ase:after-palette",
                LayerOwner l => $"ase:layer{l.Index}:{_sp.Layers[l.Index].Name}:after",
                CelOwner c => $"ase:frame{c.Frame}:cel{c.Layer}:after",
                _ => null,
            };
            if (key != null)
            {
                _sp.UnknownChunks[key] = _sp.UnknownChunks.TryGetValue(key, out var prev) ? [.. prev, .. raw] : raw;
                return true;
            }
            if (owner is TagsOwner t && type == ChunkUserData && t.Next < t.Count)
            {
                int i = t.First + t.Next++;
                var tag = _sp.Tags[i];
                var all = body.Rest();
                var (flags, color) = UserData.Read(all);
                if (color is { } c) tag.Color = c;
                if ((flags & ~UserColor) != 0) _sp.UnknownChunks[$"ase:tag{i}:{tag.Name}:userdata"] = all;
                return true;
            }
            return false;
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

        private void ReadLayer(Cursor c)
        {
            int flags = c.U16(), type = c.U16(), level = c.U16();
            c.Skip(4); // default width/height, ignored
            int blend = c.U16();
            byte opacity = c.U8();
            c.Skip(3);
            string name = c.Str();
            uint tileset = type == LayerTilemap ? c.U32() : 0;
            byte[] uuid = (_headerFlags & FlagLayerUuid) != 0 ? c.Bytes(16) : Array.Empty<byte>();

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

            if ((flags & ~(LayerVisible | LayerEditable)) != 0 || type == LayerTilemap || uuid.Length > 0)
                _sp.UnknownChunks[$"ase:layer{_sp.Layers.Count - 1}:{name}:extra"] = LayerExtra.Encode(flags, type, tileset, uuid);
        }

        /// <summary>Reads an image or linked cel into the model and returns its layer index; null for a tilemap (or unknown) cel, kept as raw data.</summary>
        private int? ReadCel(Cursor c, int frame)
        {
            int li = c.U16();
            int x = c.S16(), y = c.S16();
            byte opacity = c.U8();
            int type = c.U16();
            short z = c.S16();
            c.Skip(5);
            if (type is not (CelRaw or CelLinked or CelCompressed)) return null;
            if (li >= _sp.Layers.Count) throw new FormatException("A cel in this Aseprite file points at a missing layer.");
            var layer = _sp.Layers[li];

            if (type == CelLinked)
            {
                int target = c.U16();
                if (target < frame && _sp.Cels.TryGetValue((layer, target), out var linked)) _sp.Cels[(layer, frame)] = linked;
            }
            else
            {
                int w = c.U16(), h = c.U16();
                if (w == 0 || h == 0) return li;
                if ((long)w * h > MaxCelPixels) throw new FormatException("A cel in this Aseprite file is too large.");
                int n = w * h * (_depth / 8);
                byte[] px = type == CelRaw ? c.Bytes(n) : Inflate(c.Rest(), n);
                _sp.Cels[(layer, frame)] = new Cel(DecodePixels(px, w, h), x, y) { Opacity = opacity / 255f };
            }
            if (z != 0) _sp.UnknownChunks[$"ase:frame{frame}:cel{li}:z"] = BitConverter.GetBytes(z);
            return li;
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
                if (repeat != 0) _sp.UnknownChunks[$"ase:tag{_sp.Tags.Count}:{tag.Name}:repeat"] = BitConverter.GetBytes((ushort)repeat);
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

    /// <summary>Layer data EasyPixel does not model: all flags, layer type, tileset index, UUID (0 or 16 bytes).</summary>
    private readonly record struct LayerExtra(int Flags, int Type, uint Tileset, byte[] Uuid)
    {
        public static byte[] Encode(int flags, int type, uint tileset, byte[] uuid) =>
            new Out().U16(flags).U16(type).U32(tileset).Bytes(uuid).ToArray();

        public static LayerExtra? Decode(byte[] b) => b.Length is 8 or 24
            ? new LayerExtra(BinaryPrimitives.ReadUInt16LittleEndian(b), BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(2)),
                BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4)), b[8..])
            : null;
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

    /// <summary>Splits concatenated raw chunks; null when the bytes are not whole chunks.</summary>
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
        private readonly Sprite _sp;
        private readonly List<string> _warnings;
        private readonly HashSet<string> _used = new();
        private bool _lost;
        /// <summary>Layers in file order: stacking order with each group right before its members.</summary>
        private readonly List<Layer> _order = new();
        private readonly HashSet<int> _tilemapLayers = new();

        public Writer(Sprite sp, List<string> warnings) { _sp = sp; _warnings = warnings; }

        private byte[]? Take(string key)
        {
            if (!_sp.UnknownChunks.TryGetValue(key, out var v)) return null;
            _used.Add(key);
            return v;
        }

        /// <summary>Attached raw chunks kept under <paramref name="key"/>; none (and a warning) when they are not whole chunks.</summary>
        private List<byte[]> TakeChunks(string key)
        {
            var raw = Take(key);
            if (raw == null) return new();
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
            var grid = Take("ase:header:grid") is { Length: 10 } g ? g : DefaultGrid;
            long total = HeaderSize + frames.Sum(f => (long)f.Length);
            if (total > uint.MaxValue) throw new InvalidOperationException("The sprite is too large for an Aseprite file.");

            var header = new Out().U32((uint)total).U16(FileMagic).U16(_sp.Frames.Count).U16(_sp.Width).U16(_sp.Height)
                .U16(indexed ? 8 : 32).U32(FlagLayerOpacity | FlagGroupOpacity | (uuids ? FlagLayerUuid : 0))
                .U16(Math.Clamp(_sp.Frames[0].DurationMs, 0, ushort.MaxValue)).Zero(8)
                .U8(indexed ? Math.Clamp(transparent, 0, 255) : 0).Zero(3)
                .U16(Math.Max(1, _sp.Palette.Colors.Count)).Bytes(grid).Zero(84).ToArray();
            s.Write(header);
            foreach (var f in frames) s.Write(f);

            if (_lost || _sp.UnknownChunks.Keys.Any(k => !_used.Contains(k))) _warnings.Add(LostDataWarning);
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
            var extras = new LayerExtra?[_order.Count];
            for (int i = 0; i < _order.Count; i++)
            {
                if (Take($"ase:layer{i}:{_order[i].Name}:extra") is not { } raw) continue;
                extras[i] = LayerExtra.Decode(raw);
                if (extras[i] == null) _lost = true;
            }
            uuids = extras.Any(e => e is { Uuid.Length: 16 });

            var result = new List<List<byte[]>>();
            for (int i = 0; i < _order.Count; i++)
            {
                var l = _order[i];
                var extra = extras[i];
                int type = l.IsGroup ? LayerGroup : LayerNormal;
                if (extra?.Type == LayerTilemap && !l.IsGroup)
                {
                    if (_sp.Cels.Keys.Any(k => k.Item1 == l)) _lost = true; // drawn on: saved as a normal layer
                    else { type = LayerTilemap; _tilemapLayers.Add(i); }
                }
                int flags = (extra?.Flags ?? 0) & ~(LayerVisible | LayerEditable) | (l.Visible ? LayerVisible : 0) | (l.Locked ? 0 : LayerEditable);
                var body = new Out().U16(flags).U16(type).U16(Level(l)).U16(0).U16(0).U16((int)l.Blend).U8(l.Opacity).Zero(3).Str(l.Name);
                if (type == LayerTilemap) body.U32(extra!.Value.Tileset);
                if (uuids) body.Bytes(extra is { Uuid.Length: 16 } e ? e.Uuid : Guid.NewGuid().ToByteArray());
                var chunks = new List<byte[]> { Chunk(ChunkLayer, body) };
                chunks.AddRange(TakeChunks($"ase:layer{i}:{l.Name}:after"));
                result.Add(chunks);
            }
            return result;
        }

        private byte[] WriteFrame(int f, List<List<byte[]>> layerChunks)
        {
            // Attached chunks are flagged so kept chunks are never put between an owner and what belongs to it.
            var chunks = new List<(byte[] Bytes, bool Attached)>();
            void Owned(byte[] owner, IEnumerable<byte[]> attached)
            {
                chunks.Add((owner, false));
                foreach (var a in attached) chunks.Add((a, true));
            }

            if (f == 0)
            {
                var palette = PaletteChunks();
                var spriteData = TakeChunks("ase:after-palette");
                if (palette.Count == 0 && spriteData.Count > 0) _lost = true;
                for (int i = 0; i < palette.Count; i++) Owned(palette[i], i == palette.Count - 1 ? spriteData : Array.Empty<byte[]>());
                foreach (var lc in layerChunks) Owned(lc[0], lc.Skip(1));
            }

            for (int i = 0; i < _order.Count; i++)
            {
                var layer = _order[i];
                if (!_sp.Cels.TryGetValue((layer, f), out var cel)) continue;
                if (layer.IsGroup) { _lost = true; continue; } // Aseprite groups have no pixels
                Owned(CelChunk(i, f, layer, cel), TakeChunks($"ase:frame{f}:cel{i}:after"));
            }

            if (f == 0 && _sp.Tags.Count > 0)
            {
                var (tags, userData) = TagChunks();
                Owned(tags, userData);
            }

            InsertKept(f, chunks);

            int n = chunks.Count;
            var frame = new Out().U32((uint)(FrameHeaderSize + chunks.Sum(c => c.Bytes.Length))).U16(FrameMagic)
                .U16(Math.Min(n, 0xFFFF)).U16(Math.Clamp(_sp.Frames[f].DurationMs, 0, ushort.MaxValue)).Zero(2).U32((uint)n);
            foreach (var c in chunks) frame.Bytes(c.Bytes);
            return frame.ToArray();
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

        /// <summary>The tags chunk and one user-data chunk per tag carrying its colour (and any text/properties it had).</summary>
        private (byte[] Tags, List<byte[]> UserData) TagChunks()
        {
            var body = new Out().U16(_sp.Tags.Count).Zero(8);
            var userData = new List<byte[]>();
            for (int i = 0; i < _sp.Tags.Count; i++)
            {
                var t = _sp.Tags[i];
                int repeat = Take($"ase:tag{i}:{t.Name}:repeat") is { Length: 2 } r ? BinaryPrimitives.ReadUInt16LittleEndian(r) : 0;
                body.U16(Math.Clamp(t.From, 0, ushort.MaxValue)).U16(Math.Clamp(t.To, 0, ushort.MaxValue))
                    .U8(t.Dir switch { LoopDir.Reverse => 1, LoopDir.PingPong => 2, _ => 0 }).U16(repeat).Zero(6)
                    .U8(t.Color.R).U8(t.Color.G).U8(t.Color.B).U8(0).Str(t.Name);
                byte[] ud;
                try { ud = UserData.WithColor(Take($"ase:tag{i}:{t.Name}:userdata"), t.Color); }
                catch (FormatException) { _lost = true; ud = UserData.WithColor(null, t.Color); }
                userData.Add(Chunk(ChunkUserData, new Out().Bytes(ud)));
            }
            return (Chunk(ChunkTags, body), userData);
        }

        private byte[] CelChunk(int li, int f, Layer layer, Cel cel)
        {
            if (cel.X is < short.MinValue or > short.MaxValue || cel.Y is < short.MinValue or > short.MaxValue)
                throw new InvalidOperationException("A cel lies too far outside the canvas for an Aseprite file.");
            short z = Take($"ase:frame{f}:cel{li}:z") is { Length: 2 } zb ? BinaryPrimitives.ReadInt16LittleEndian(zb) : (short)0;
            int link = -1;
            for (int g = 0; g < f && link < 0; g++)
                if (_sp.Cels.TryGetValue((layer, g), out var other) && ReferenceEquals(other, cel)) link = g;

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

        /// <summary>Puts the chunks kept on their own from frame <paramref name="f"/> back at their old positions.</summary>
        private void InsertKept(int f, List<(byte[] Bytes, bool Attached)> chunks)
        {
            string prefix = $"ase:frame{f}:";
            var kept = new List<(int Pos, string Key)>();
            foreach (var key in _sp.UnknownChunks.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var parts = key[prefix.Length..].Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int pos) && parts[1].Length == 4) kept.Add((pos, key));
            }
            foreach (var (pos, key) in kept.OrderBy(k => k.Pos))
            {
                var raw = _sp.UnknownChunks[key];
                if (SplitChunks(raw) is not { Count: 1 }) continue; // not one whole chunk: left unused → warning
                int type = BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(4));
                // A tilemap cel only makes sense on the tilemap layer it was read with.
                if (type == ChunkCel && (raw.Length < 8 || !_tilemapLayers.Contains(BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(6))))) continue;
                _used.Add(key);
                int at = Math.Min(pos, chunks.Count);
                while (at < chunks.Count && chunks[at].Attached) at++;
                chunks.Insert(at, (raw, false));
            }
        }
    }
}
