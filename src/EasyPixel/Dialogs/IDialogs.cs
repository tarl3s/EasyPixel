using EasyPixel.Core.Formats;
using EasyPixel.Core.Model;

namespace EasyPixel.Dialogs;

public enum SaveChoice { Save, DontSave, Cancel }

public sealed record NewSpriteOptions(int Width, int Height, ColorMode Mode);

public enum ExportKind { Png, Gif, Sheet }

public sealed record ExportOptions(ExportKind Kind, SheetKind Layout = SheetKind.Horizontal, int Columns = 4, int Padding = 0);

/// <summary>A file type offered by a save picker: its name and extensions (with the dot).</summary>
public sealed record FileType(string Name, string[] Extensions);

public sealed record CanvasSizeOptions(int Width, int Height, Anchor Anchor);

/// <summary>Settings: language code ("auto" or one of <see cref="L.Languages"/>), theme ("dark" / "light") and whether to check for updates.</summary>
public sealed record SettingsChoice(string Language, string Theme, bool CheckForUpdates = true);

/// <summary>File → Import sprite sheet: the size of one cell and the gap between cells.</summary>
public sealed record SliceOptions(int CellWidth, int CellHeight, int Padding);

public sealed record OnionOptions(int Before, int After, int Opacity, bool Tint);

/// <summary>
/// Every question the main window asks the person. The app shows real windows (<see cref="AppDialogs"/>); the UI tests
/// answer from a script. All texts passed in are already translated.
/// </summary>
public interface IDialogs
{
    /// <summary>"Save changes to {name}?" with Save / Don't save / Cancel.</summary>
    Task<SaveChoice> AskSaveChanges(string docName);

    /// <summary>A yes/no question; true = <paramref name="yes"/>.</summary>
    Task<bool> Confirm(string message, string yes, string no);

    Task Tell(string message);

    Task<NewSpriteOptions?> AskNewSprite();

    Task<ExportOptions?> AskExport();

    Task<string?> PickOpenFile(IReadOnlyList<FileType> types);

    Task<string?> PickSaveFile(string title, string suggestedName, IReadOnlyList<FileType> types);

    /// <summary>One whole number between <paramref name="min"/> and <paramref name="max"/>.</summary>
    Task<int?> AskNumber(string title, string label, int value, int min, int max);

    /// <summary>A line of text (a name).</summary>
    Task<string?> AskText(string title, string label, string value);

    /// <summary>A width and height (Sprite → Resize, View → Grid settings).</summary>
    Task<(int Width, int Height)?> AskSize(string title, int width, int height);

    /// <summary>Sprite → Canvas size: new size and where the picture stays.</summary>
    Task<CanvasSizeOptions?> AskCanvasSize(int width, int height);

    /// <summary>Pick one of <paramref name="options"/> (shown as given); the index, or null when cancelled.</summary>
    Task<int?> Choose(string title, string message, IReadOnlyList<string> options);

    /// <summary>File → Import sprite sheet: how to cut the picture into frames.</summary>
    Task<SliceOptions?> AskSlice(int cellWidth, int cellHeight);

    Task<OnionOptions?> AskOnion(OnionOptions current);

    Task<SettingsChoice?> AskSettings(SettingsChoice current);

    /// <summary>Help → Keyboard shortcuts: keys and what they do (already translated).</summary>
    Task ShowShortcuts(IReadOnlyList<(string Keys, string Action)> shortcuts);
}
