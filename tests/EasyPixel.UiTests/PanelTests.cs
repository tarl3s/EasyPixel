using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using EasyPixel.Core.Model;
using EasyPixel.Dialogs;
using EasyPixel.Panels;
using EasyPixel.Tools;
using Image = EasyPixel.Core.Model.Image;
using PixelRect = EasyPixel.Core.Model.PixelRect;

namespace EasyPixel.UiTests;

/// <summary>The layers panel, timeline, palette and the Sprite / Layer / Frame / Select / View / Help menus.</summary>
public class PanelTests
{
    private static readonly uint Red = new Rgba(255, 0, 0, 255).Packed;

    // ---- layers ----

    [AvaloniaFact]
    public void Layer_AddUndo()
    {
        using var h = new Harness();
        var first = h.Session.Layer;
        h.Key(Key.N, RawInputModifiers.Shift);
        Assert.Equal(2, h.Doc.Layers.Count);
        var added = h.Doc.Layers[1];
        Assert.Same(added, h.Session.Layer);
        Assert.Same(added, h.Window.Layers.Shown[0]); // the panel lists the top layer first
        h.Drag((2, 2));
        Assert.Equal(new Rgba(0, 0, 0, 255).Packed, h.Doc.Cels[(added, 0)].Image.Get(2, 2));

        h.Key(Key.Z, RawInputModifiers.Control); // the dot
        h.Key(Key.Z, RawInputModifiers.Control); // the layer
        Assert.Single(h.Doc.Layers);
        Assert.Same(first, h.Session.Layer);
        Assert.Single(h.Window.Layers.Shown);
        h.Key(Key.Y, RawInputModifiers.Control);
        Assert.Equal(2, h.Doc.Layers.Count);
    }

    [AvaloniaFact]
    public void DuplicateLayer_HasNoAseId()
    {
        using var h = new Harness();
        var l = h.Session.Layer;
        l.Extra["ase:id"] = new byte[] { 3, 0, 0, 0 };
        l.Extra["ase:uuid"] = new byte[16];
        h.Drag((1, 1));
        h.Window.DuplicateLayer();
        var copy = h.Session.Layer;
        Assert.NotSame(l, copy);
        Assert.False(copy.Extra.ContainsKey("ase:id"));
        Assert.False(copy.Extra.ContainsKey("ase:uuid"));
        Assert.EndsWith("copy", copy.Name);
        Assert.Equal(h.Doc.Cels[(l, 0)].Image.Get(1, 1), h.Doc.Cels[(copy, 0)].Image.Get(1, 1));
    }

    [AvaloniaFact]
    public async Task LayerSettings_Groups_AndMoves_AreUndoable()
    {
        using var h = new Harness();
        var bottom = h.Session.Layer;
        h.Window.NewLayer();
        var top = h.Session.Layer;
        h.Window.ToggleVisible(top);
        h.Window.ToggleLock(top);
        h.Window.SetLayerOpacity(top, 128);
        h.Window.SetLayerBlend(top, BlendMode.Screen);
        h.Dialogs.Texts.Enqueue("Ink");
        await h.Window.RenameLayerAsync(top);
        Assert.Equal(("Ink", false, true, (byte)128, BlendMode.Screen), (top.Name, top.Visible, top.Locked, top.Opacity, top.Blend));
        for (int i = 0; i < 5; i++) h.Window.Undo();
        Assert.Equal((true, false, (byte)255, BlendMode.Normal), (top.Visible, top.Locked, top.Opacity, top.Blend));

        h.Window.GroupLayer();
        var group = top.Parent!;
        Assert.True(group.IsGroup);
        Assert.Equal(new[] { group, top, bottom }, h.Window.Layers.Shown);
        h.Window.PutIntoGroup(bottom, group);
        Assert.Same(group, bottom.Parent);
        Assert.Equal(new[] { group, bottom, top }, h.Window.Layers.Shown); // on top inside the group
        h.Session.Layer = bottom;
        h.Window.MoveLayer(-1);
        Assert.Equal(new[] { group, top, bottom }, h.Window.Layers.Shown);
        h.Window.MoveOutOfGroup(bottom);
        Assert.Null(bottom.Parent);
        Assert.Equal(new[] { bottom, group, top }, h.Window.Layers.Shown);
        h.Window.Undo();
        h.Window.Undo();
        h.Window.Undo();
        Assert.Null(bottom.Parent);
        Assert.Equal(new[] { group, top, bottom }, h.Window.Layers.Shown);

        h.Session.Layer = group;
        h.Window.RemoveLayer(); // the group and the layer in it
        Assert.Equal(new[] { bottom }, h.Doc.Layers);
        h.Window.RemoveLayer(); // the last one stays
        Assert.Single(h.Doc.Layers);
        h.Window.Undo();
        Assert.Equal(3, h.Doc.Layers.Count);
    }

    [AvaloniaFact]
    public void ArrowKeys_ChangeFrameAndLayer()
    {
        using var h = new Harness();
        h.Window.NewLayer();
        h.Window.NewEmptyFrame();
        Assert.Equal(1, h.Session.Frame);
        h.Key(Key.Left);
        Assert.Equal(0, h.Session.Frame);
        h.Key(Key.Right);
        Assert.Equal(1, h.Session.Frame);
        h.Key(Key.Down);
        Assert.Same(h.Doc.Layers[0], h.Session.Layer);
        h.Key(Key.Up);
        Assert.Same(h.Doc.Layers[1], h.Session.Layer);
    }

    // ---- frames and tags ----

    [AvaloniaFact]
    public void Frame_AddUndo()
    {
        using var h = new Harness();
        h.Drag((2, 2));
        h.Key(Key.N, RawInputModifiers.Alt);
        Assert.Equal(2, h.Doc.Frames.Count);
        Assert.Equal(1, h.Session.Frame);
        Assert.Equal(h.Doc.Cels[(h.Session.Layer, 0)].Image.Get(2, 2), h.Pixel(2, 2)); // Aseprite's New Frame copies the frame
        h.Key(Key.Z, RawInputModifiers.Control);
        Assert.Single(h.Doc.Frames);
        Assert.Equal(0, h.Session.Frame);
        h.Key(Key.Y, RawInputModifiers.Control);
        Assert.Equal(2, h.Doc.Frames.Count);
    }

    [AvaloniaFact]
    public async Task Frames_RemoveMoveDuration_AndTags()
    {
        using var h = new Harness();
        h.Window.NewEmptyFrame();
        h.Window.NewEmptyFrame();
        h.Window.GoToFrame(0);
        h.Drag((1, 1));
        h.Window.MoveFrame(+1);
        Assert.Equal(1, h.Session.Frame);
        Assert.NotEqual(0u, h.Pixel(1, 1));
        h.Window.GoToFrame(0);
        h.Window.GoToFrame(2, extend: true);
        h.Dialogs.Numbers.Enqueue(40);
        await h.Window.FrameDurationAsync();
        Assert.All(h.Doc.Frames, f => Assert.Equal(40, f.DurationMs));

        h.Window.GoToFrame(1);
        h.Window.GoToFrame(2, extend: true);
        h.Dialogs.Texts.Enqueue("run");
        await h.Window.NewTagAsync();
        var tag = Assert.Single(h.Doc.Tags);
        Assert.Equal(("run", 1, 2), (tag.Name, tag.From, tag.To));
        h.Window.SetTagDirection(tag, LoopDir.PingPong);
        h.Window.SetTagColor(tag, MainWindow.TagColors[3]);
        h.Dialogs.Texts.Enqueue("dash");
        await h.Window.RenameTagAsync(tag);
        Assert.Equal(("dash", LoopDir.PingPong), (tag.Name, tag.Dir));
        h.Window.Undo();
        h.Window.Undo();
        h.Window.Undo();
        Assert.Equal(("run", LoopDir.Forward), (tag.Name, tag.Dir));
        h.Window.RemoveTag(tag);
        Assert.Empty(h.Doc.Tags);
        h.Window.Undo();
        Assert.Single(h.Doc.Tags);

        h.Window.GoToFrame(2);
        h.Window.RemoveFrame();
        Assert.Equal(2, h.Doc.Frames.Count);
        Assert.Equal(1, h.Session.Frame);
        h.Window.RemoveFrame();
        h.Window.RemoveFrame(); // the last frame stays
        Assert.Single(h.Doc.Frames);
    }

    [AvaloniaFact]
    public void TagPlayback_StaysInTagRange()
    {
        using var h = new Harness();
        for (int i = 0; i < 4; i++) h.Window.NewEmptyFrame(); // 5 frames
        var tag = new Tag { Name = "walk", From = 1, To = 3 };
        h.Doc.Tags.Add(tag);
        h.Window.GoToFrame(2);

        h.Key(Key.Enter); // play
        Assert.True(h.Window.IsPlaying);
        var seen = new List<int>();
        for (int i = 0; i < 6; i++) { h.Window.PlayStep(); seen.Add(h.Session.Frame); }
        Assert.Equal(new[] { 3, 1, 2, 3, 1, 2 }, seen);
        h.Key(Key.Enter); // stop
        Assert.False(h.Window.IsPlaying);

        tag.Dir = LoopDir.PingPong;
        h.Window.TogglePlay();
        seen.Clear();
        for (int i = 0; i < 6; i++) { h.Window.PlayStep(); seen.Add(h.Session.Frame); }
        Assert.Equal(new[] { 3, 2, 1, 2, 3, 2 }, seen);
        h.Window.StopPlaying();

        tag.Dir = LoopDir.Reverse;
        h.Window.SetLoop(false);
        h.Window.GoToFrame(3);
        h.Window.TogglePlay();
        h.Window.PlayStep();
        h.Window.PlayStep();
        Assert.Equal(1, h.Session.Frame);
        h.Window.PlayStep(); // the end without looping
        Assert.False(h.Window.IsPlaying);
        Assert.Equal(1, h.Session.Frame);

        h.Window.GoToFrame(0); // outside the tag: everything, forward
        h.Window.SetLoop(true);
        h.Window.TogglePlay();
        Assert.Equal((0, 4, LoopDir.Forward), h.Window.Playback.Range);
        h.Key(Key.Escape);
        Assert.False(h.Window.IsPlaying);
    }

    [Fact]
    public void Playback_SingleFrameAndNonLoopingPingPong()
    {
        var s = new Sprite(2, 2);
        var p = new Playback { Loop = false };
        p.Start(s, 0);
        Assert.Null(p.Next(0));
        s.Frames.Add(new Frame());
        s.Frames.Add(new Frame());
        s.Tags.Add(new Tag { From = 0, To = 2, Dir = LoopDir.PingPong });
        p.Start(s, 0);
        var seen = new List<int>();
        for (int f = 0; p.Next(f) is { } n; f = n) seen.Add(n);
        Assert.Equal(new[] { 1, 2, 1, 0 }, seen);
    }

    [AvaloniaFact]
    public async Task OnionSettings_AndPreview_FollowTheFrame()
    {
        using var h = new Harness();
        h.Dialogs.Onions.Enqueue(new OnionOptions(2, 0, 100, true));
        await h.Window.OnionSettingsAsync();
        Assert.Equal((2, 0, (byte)100, true), (h.Window.Canvas.Onion.Before, h.Window.Canvas.Onion.After, h.Window.Canvas.Onion.Opacity, h.Window.Canvas.Onion.Tint));
        h.Window.SetOnion(true);
        Assert.True(h.Window.Canvas.ShowOnion);

        h.Window.NewEmptyFrame();
        var preview = h.Window.ShowPreview(full: false);
        Harness.Pump();
        preview.Zoom = 8;
        Assert.Equal(8, preview.Zoom);
        h.Window.GoToFrame(0);
        preview.Redraw();
        Assert.Equal(0, preview.ShownFrame);
        h.Window.GoToFrame(1);
        preview.Redraw();
        Assert.Equal(1, preview.ShownFrame);
        Assert.Same(preview, h.Window.ShowPreview(full: false)); // one floating preview
        preview.Close();

        h.Key(Key.F);
        var full = h.Window.OwnedWindows.OfType<PreviewWindow>().Single();
        Assert.Equal(Avalonia.Controls.WindowState.FullScreen, full.WindowState);
        full.Close();
        Harness.Pump();
    }

    [AvaloniaFact]
    public async Task DrawAnimateExportGif_EndToEnd()
    {
        using var h = new Harness(8, 8);
        h.Drag((1, 1), (3, 1));
        h.Key(Key.N, RawInputModifiers.Alt); // frame 2 = copy of frame 1
        h.Key(Key.E);
        h.Drag((1, 1));                      // erase one pixel in frame 2
        h.Dialogs.Numbers.Enqueue(250);
        await h.Window.FrameDurationAsync();
        h.Key(Key.Enter);
        h.Window.PlayStep();
        Assert.Equal(0, h.Session.Frame);
        h.Key(Key.Enter);

        var gif = Harness.TempFile("walk.gif");
        h.Dialogs.Exports.Enqueue(new ExportOptions(ExportKind.Gif));
        h.Dialogs.SavePaths.Enqueue(gif);
        h.Dialogs.ConfirmAnswers.Enqueue(true);
        await h.Window.ExportAsync();
        Sprite read;
        using (var f = File.OpenRead(gif)) read = new Core.Formats.GifFormat().Read(f, new List<string>());
        Assert.Equal(2, read.Frames.Count);
        Assert.Equal(250, read.Frames[1].DurationMs);
        var a = new Image(8, 8);
        var b = new Image(8, 8);
        Core.Render.Compositor.Flatten(read, 0, a, new PixelRect(0, 0, 8, 8));
        Core.Render.Compositor.Flatten(read, 1, b, new PixelRect(0, 0, 8, 8));
        Assert.NotEqual(0u, a.Get(1, 1) >> 24);
        Assert.Equal(0u, b.Get(1, 1) >> 24);
        Assert.NotEqual(0u, b.Get(2, 1) >> 24);
    }

    // ---- palette ----

    [AvaloniaFact]
    public async Task PaletteFile_RoundTrip_AndLoadUndoes()
    {
        using var h = new Harness();
        var pal = h.Window.PaletteView;
        h.Window.Tools.SetPrimary(new Rgba(10, 20, 30, 255));
        pal.AddPrimary();
        h.Window.Tools.SetPrimary(new Rgba(200, 100, 0, 255));
        pal.AddPrimary();
        var saved = h.Doc.Palette.Colors.ToList();
        foreach (var ext in new[] { ".gpl", ".hex", ".pal" })
        {
            var path = Harness.TempFile("palette" + ext);
            await pal.SaveAsync(path);
            Assert.True(File.Exists(path));
            h.Dialogs.OpenPaths.Enqueue(path);
            await pal.LoadAsync();
            Assert.Equal(saved, h.Doc.Palette.Colors);
        }
        var bad = Harness.TempFile("bad.gpl");
        File.WriteAllText(bad, "not a palette");
        await pal.LoadAsync(bad);
        Assert.Contains(h.Dialogs.Told, t => t.Contains("bad.gpl"));
        Assert.Equal(saved, h.Doc.Palette.Colors);

        pal.Select(0);
        pal.RemoveSelected();
        Assert.Single(h.Doc.Palette.Colors);
        h.Window.Undo();
        Assert.Equal(saved, h.Doc.Palette.Colors);
    }

    [AvaloniaFact]
    public void IndexedPalette_MoveAndRemove_KeepPicture()
    {
        using var h = new Harness(4, 4, ColorMode.Indexed);
        h.Window.Tools.SetPrimary(new Rgba(255, 0, 0, 255));
        h.Drag((1, 1));
        h.Window.Tools.SetPrimary(new Rgba(0, 255, 0, 255));
        h.Drag((2, 1));
        Assert.Equal(3, h.Doc.Palette.Colors.Count); // transparent, red, green
        var pal = h.Window.PaletteView;
        pal.Select(2);
        pal.MoveSelected(-1);
        Assert.Equal(new Rgba(0, 255, 0, 255), h.Doc.Palette.Colors[1]);
        Assert.Equal(2u, h.Pixel(1, 1)); // red pixel follows its colour
        Assert.Equal(1u, h.Pixel(2, 1));
        Assert.Equal(1, pal.Selected);
        h.Window.Undo();
        Assert.Equal(1u, h.Pixel(1, 1));
        pal.Select(1);
        pal.RemoveSelected();
        Assert.Equal(2, h.Doc.Palette.Colors.Count);
        h.Window.Undo();
        Assert.Equal(3, h.Doc.Palette.Colors.Count);
        Assert.Equal(1u, h.Pixel(1, 1));
    }

    [AvaloniaFact]
    public void ColorPicker_SetsPrimary_AndFollowsIt()
    {
        using var h = new Harness();
        var picker = h.Window.Colors.Picker;
        h.Window.Tools.SetPrimary(new Rgba(0, 0, 255, 255));
        Assert.Equal(new Rgba(0, 0, 255, 255), picker.Color);
        picker.PickSquare(1, 1); // full saturation and value at the blue hue
        Assert.Equal(new Rgba(0, 0, 255, 255), h.Window.Tools.Primary);
        picker.PickSquare(0, 1); // white
        Assert.Equal(new Rgba(255, 255, 255, 255), h.Window.Tools.Primary);
        h.Window.Colors.Hex.Text = "#12345678";
        h.Window.Colors.Hex.Focus();
        h.Key(Key.Enter);
        Assert.Equal(new Rgba(0x12, 0x34, 0x56, 0x78), h.Window.Tools.Primary);
    }

    // ---- Sprite menu ----

    [AvaloniaFact]
    public async Task SpriteMenu_SizeRotateFlipMode_AllUndo()
    {
        using var h = new Harness(4, 2);
        h.Window.Tools.SetPrimary(new Rgba(255, 0, 0, 255));
        h.Drag((0, 0));
        h.Dialogs.CanvasSizes.Enqueue(new CanvasSizeOptions(6, 4, Anchor.BottomRight));
        await h.Window.CanvasSizeAsync();
        Assert.Equal((6, 4), (h.Doc.Width, h.Doc.Height));
        Assert.Equal((6, 4), (h.Session.Sel.Width, h.Session.Sel.Height));
        Assert.Equal(Red, h.Pixel(2, 2));
        h.Window.Undo();
        Assert.Equal((4, 2), (h.Doc.Width, h.Doc.Height));

        h.Dialogs.Sizes.Enqueue((8, 4));
        await h.Window.ResizeAsync();
        Assert.Equal(Red, h.Pixel(1, 1));
        h.Window.Undo();

        h.Window.ChangeSprite("Rotate", s => Core.Raster.SpriteTransforms.Rotate(s, Core.Raster.SpriteRotation.Clockwise90));
        Assert.Equal((2, 4), (h.Doc.Width, h.Doc.Height));
        Assert.Equal(Red, h.Pixel(1, 0));
        h.Window.Undo();
        Assert.Equal(Red, h.Pixel(0, 0));

        h.Window.SetColorMode(ColorMode.Indexed);
        Assert.Equal(ColorMode.Indexed, h.Doc.Mode);
        Assert.True(h.Window.ModeIndexedItem.IsChecked);
        h.Window.Undo();
        Assert.Equal(ColorMode.Rgba, h.Doc.Mode);
        Assert.Equal(Red, h.Pixel(0, 0));
    }

    [AvaloniaFact]
    public async Task SpriteMenu_OutlinePixelateReduce()
    {
        using var h = new Harness(8, 8);
        h.Window.Tools.SetPrimary(new Rgba(255, 0, 0, 255));
        h.Drag((3, 3));
        h.Window.Tools.SetPrimary(new Rgba(0, 0, 255, 255));
        h.Window.OutlineCel();
        Assert.Equal(new Rgba(0, 0, 255, 255).Packed, h.Pixel(2, 3));
        Assert.Equal(0u, h.Pixel(2, 2)); // no diagonals
        h.Window.Undo();
        Assert.Equal(0u, h.Pixel(2, 3));

        h.Dialogs.Numbers.Enqueue(4);
        await h.Window.PixelateAsync();
        Assert.Equal(0u, h.Pixel(3, 3)); // one red pixel in a 4x4 block of nothing
        h.Window.Undo();

        h.Dialogs.Numbers.Enqueue(2);
        await h.Window.ReduceColorsAsync();
        Assert.Equal(Red, h.Pixel(3, 3));
        Assert.Single(h.Doc.Palette.Colors);
        h.Window.Undo();
        Assert.Empty(h.Doc.Palette.Colors);
    }

    // ---- Select menu ----

    [AvaloniaFact]
    public void SelectMenu_InvertAndByColour()
    {
        using var h = new Harness(4, 4);
        h.Window.Tools.SetPrimary(new Rgba(255, 0, 0, 255));
        h.Drag((1, 1));
        h.Drag((3, 2));
        h.Window.SelectByColor(new Rgba(255, 0, 0, 255));
        Assert.True(h.Session.Sel.Contains(1, 1));
        Assert.True(h.Session.Sel.Contains(3, 2));
        Assert.False(h.Session.Sel.Contains(0, 0));
        h.Key(Key.I, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.False(h.Session.Sel.Contains(1, 1));
        Assert.True(h.Session.Sel.Contains(0, 0));
    }

    // ---- shortcuts, view, help, settings ----

    [AvaloniaFact]
    public void ToolShortcuts_FollowAseprite()
    {
        using var h = new Harness();
        (Key, ToolKind)[] keys =
        {
            (Key.E, ToolKind.Eraser), (Key.G, ToolKind.Bucket), (Key.L, ToolKind.Line), (Key.U, ToolKind.Rectangle), (Key.U, ToolKind.Ellipse),
            (Key.M, ToolKind.RectSelect), (Key.M, ToolKind.EllipseSelect), (Key.Q, ToolKind.Lasso), (Key.W, ToolKind.MagicWand),
            (Key.V, ToolKind.Move), (Key.I, ToolKind.Eyedropper), (Key.B, ToolKind.Pencil),
        };
        foreach (var (key, tool) in keys)
        {
            h.Key(key);
            Assert.Equal(tool, h.Window.Tools.Current);
        }
        var (p, s) = (h.Window.Tools.Primary, h.Window.Tools.Secondary);
        h.Key(Key.X);
        Assert.Equal((s, p), (h.Window.Tools.Primary, h.Window.Tools.Secondary));
    }

    [AvaloniaFact]
    public async Task View_TabTiledGrid_HelpAndSettings()
    {
        using var h = new Harness();
        h.Key(Key.Tab);
        Assert.False(h.Window.RightPanel.IsVisible);
        Assert.False(h.Window.TimelineBar.IsVisible);
        h.Window.TogglePanels();
        Assert.True(h.Window.RightPanel.IsVisible);

        h.Window.Canvas.Tiled = true;
        h.Dialogs.Sizes.Enqueue((4, 4));
        await h.Window.GridSettingsAsync();
        Assert.True(h.Window.Canvas.ShowTileGrid);
        Assert.Equal((4, 4), h.Window.Canvas.TileGrid);
        h.Window.Canvas.ZoomAt(16, new Avalonia.Point(100, 100));
        Assert.NotNull(h.Window.CaptureRenderedFrame()); // tiles and grid draw without errors
        h.Window.Canvas.Tiled = false;

        await h.Window.ShowShortcutsAsync();
        var list = Assert.Single(h.Dialogs.ShortcutLists);
        Assert.Contains(list, s => s.Keys == "Alt+N");
        Assert.Contains(list, s => s.Keys == "B" && s.Action == "Pencil");

        h.Dialogs.SettingsChoices.Enqueue(new SettingsChoice("pt-BR", "light"));
        await h.Window.SettingsAsync();
        Assert.Equal(("pt-BR", "light"), (App.Settings.Language, App.Settings.Theme));
        Assert.Contains(h.Dialogs.Told, t => t.Contains("next time"));
        App.Settings.Language = "auto";
        App.Settings.Theme = "dark";
        App.Settings.Save();
    }

    // ---- review fixes ----

    [AvaloniaFact]
    public void Playback_SurvivesUndoOfAFrame()
    {
        using var h = new Harness();
        h.Window.NewEmptyFrame();
        h.Window.NewEmptyFrame();       // 3 frames, on the last
        h.Window.TogglePlay();
        h.Window.Undo();                // back to 2 frames while playing
        h.Window.PlayStep();            // used to index a frame that no longer exists
        Assert.False(h.Window.IsPlaying);
        Assert.InRange(h.Session.Frame, 0, 1);
        h.Window.TogglePlay();
        h.Window.PlayStep();
        Assert.True(h.Window.IsPlaying);
        h.Window.StopPlaying();
    }

    [AvaloniaFact]
    public void CrashGuard_LogsKeepsWorkAndTells()
    {
        using var h = new Harness();
        var folder = Path.Combine(TestAppBuilder.TempRoot, "crash");
        CrashGuard.Folder = folder;
        CrashGuard.Window = h.Window;
        CrashGuard.Install();
        try
        {
            h.Drag((1, 1)); // unsaved work
            // Run the jobs the way the app's main loop does (a dispatcher frame), not the test pump.
            var frame = new Avalonia.Threading.DispatcherFrame();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => throw new InvalidOperationException("boom in a posted action"));
            Avalonia.Threading.Dispatcher.UIThread.Post(() => frame.Continue = false);
            Avalonia.Threading.Dispatcher.UIThread.PushFrame(frame);
            Harness.Pump();
            Assert.Contains("boom in a posted action", File.ReadAllText(CrashGuard.LogPath));
            var copy = Assert.Single(Directory.GetFiles(folder, "easypixel_recovery_*.epx"));
            using (var f = File.OpenRead(copy)) Assert.Equal(16, new Core.Formats.EpxFormat().Read(f, new List<string>()).Width);
            Assert.Contains(h.Dialogs.Told, t => t.Contains("boom in a posted action") && t.Contains(copy));
            Assert.True(h.Window.IsVisible); // the editor stays open
        }
        finally
        {
            CrashGuard.UninstallUiHandler(); // later tests must see their own exceptions
            CrashGuard.Window = null;
        }
    }

    [AvaloniaFact]
    public async Task LoadingAShortPalette_IntoIndexed_KeepsUsedColours()
    {
        using var h = new Harness(4, 4, ColorMode.Indexed);
        foreach (var c in new[] { new Rgba(255, 0, 0, 255), new Rgba(0, 255, 0, 255), new Rgba(0, 0, 255, 255) })
        {
            h.Window.Tools.SetPrimary(c);
            h.Drag((h.Doc.Palette.Colors.Count, 0));
        }
        Assert.Equal(4, h.Doc.Palette.Colors.Count); // transparent + 3, index 3 used
        var path = Harness.TempFile("two.hex");
        File.WriteAllText(path, "ffffff\n000000\n");
        await h.Window.PaletteView.LoadAsync(path);
        Assert.Equal(4, h.Doc.Palette.Colors.Count);
        Assert.Equal(new Rgba(0, 0, 255, 255), h.Doc.Palette.Colors[3]);
        Assert.Contains(h.Dialogs.Told, t => t.Contains("extended"));
    }

    [AvaloniaFact]
    public void RgbaRemoveColour_ShiftsTransparentIndex()
    {
        using var h = new Harness();
        h.Doc.Palette.Colors.AddRange(new[] { new Rgba(1, 1, 1, 255), new Rgba(2, 2, 2, 255), new Rgba(0, 0, 0, 0) });
        h.Doc.Palette.TransparentIndex = 2;
        h.Window.PaletteView.Rebuild();
        h.Window.PaletteView.Select(0);
        h.Window.PaletteView.RemoveSelected();
        Assert.Equal(1, h.Doc.Palette.TransparentIndex);
        h.Window.Undo();
        Assert.Equal(2, h.Doc.Palette.TransparentIndex);
    }

    [AvaloniaFact]
    public void OpacitySpinner_IsOneUndoStep()
    {
        using var h = new Harness();
        var l = h.Session.Layer;
        for (int o = 250; o >= 200; o -= 10) h.Window.SetLayerOpacity(l, o);
        Assert.Equal(1, h.Session.History.Count);
        h.Window.Undo();
        Assert.Equal((byte)255, l.Opacity);
    }

    [AvaloniaFact]
    public void CollapsedGroupFromAseprite_StartsFolded()
    {
        using var h = new Harness();
        var s = new Sprite(4, 4);
        var inner = s.Layers[0];
        var g = LayerTree.Group(s, inner, "G");
        g.Extra["ase:flags"] = new byte[] { 1 | 2 | 32, 0, 1, 0, 0, 0, 0, 0 };
        h.Window.SetSession(new EditorSession(s));
        Assert.Equal(new[] { g }, h.Window.Layers.Shown);
    }
}
