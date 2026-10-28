using EasyPixel.Dialogs;

namespace EasyPixel.UiTests;

/// <summary>Scripted answers for the main window's questions; records what was asked.</summary>
internal sealed class FakeDialogs : IDialogs
{
    public Queue<SaveChoice> SaveChoices { get; } = new();
    public Queue<bool> ConfirmAnswers { get; } = new();
    public Queue<NewSpriteOptions?> NewSprites { get; } = new();
    public Queue<ExportOptions?> Exports { get; } = new();
    public Queue<string?> OpenPaths { get; } = new();
    public Queue<string?> SavePaths { get; } = new();

    public int SaveQuestions { get; private set; }
    public List<string> Confirms { get; } = new();
    public List<string> Told { get; } = new();

    public Task<SaveChoice> AskSaveChanges(string docName)
    {
        SaveQuestions++;
        return Task.FromResult(SaveChoices.Count > 0 ? SaveChoices.Dequeue() : SaveChoice.Cancel);
    }

    public Task<bool> Confirm(string message, string yes, string no)
    {
        Confirms.Add(message);
        return Task.FromResult(ConfirmAnswers.Count > 0 && ConfirmAnswers.Dequeue());
    }

    public Task Tell(string message)
    {
        Told.Add(message);
        return Task.CompletedTask;
    }

    public Task<NewSpriteOptions?> AskNewSprite() => Task.FromResult(NewSprites.Count > 0 ? NewSprites.Dequeue() : null);
    public Task<ExportOptions?> AskExport() => Task.FromResult(Exports.Count > 0 ? Exports.Dequeue() : null);
    public Task<string?> PickOpenFile(IReadOnlyList<FileType> types) => Task.FromResult(OpenPaths.Count > 0 ? OpenPaths.Dequeue() : null);
    public Queue<int?> Numbers { get; } = new();
    public Queue<string?> Texts { get; } = new();
    public Queue<(int Width, int Height)?> Sizes { get; } = new();
    public Queue<CanvasSizeOptions?> CanvasSizes { get; } = new();
    public Queue<OnionOptions?> Onions { get; } = new();
    public Queue<SettingsChoice?> SettingsChoices { get; } = new();
    public List<IReadOnlyList<(string Keys, string Action)>> ShortcutLists { get; } = new();

    public Task<int?> AskNumber(string title, string label, int value, int min, int max) => Task.FromResult(Numbers.Count > 0 ? Numbers.Dequeue() : null);
    public Task<string?> AskText(string title, string label, string value) => Task.FromResult(Texts.Count > 0 ? Texts.Dequeue() : null);
    public Task<(int Width, int Height)?> AskSize(string title, int width, int height) => Task.FromResult(Sizes.Count > 0 ? Sizes.Dequeue() : null);
    public Task<CanvasSizeOptions?> AskCanvasSize(int width, int height) => Task.FromResult(CanvasSizes.Count > 0 ? CanvasSizes.Dequeue() : null);
    public Queue<int?> Choices { get; } = new();
    public List<IReadOnlyList<string>> ChoiceLists { get; } = new();
    public Task<int?> Choose(string title, string message, IReadOnlyList<string> options)
    {
        ChoiceLists.Add(options);
        return Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : null);
    }
    public Task<OnionOptions?> AskOnion(OnionOptions current) => Task.FromResult(Onions.Count > 0 ? Onions.Dequeue() : null);
    public Task<SettingsChoice?> AskSettings(SettingsChoice current) => Task.FromResult(SettingsChoices.Count > 0 ? SettingsChoices.Dequeue() : null);

    public Task ShowShortcuts(IReadOnlyList<(string Keys, string Action)> shortcuts)
    {
        ShortcutLists.Add(shortcuts);
        return Task.CompletedTask;
    }

    public Task<string?> PickSaveFile(string title, string suggestedName, IReadOnlyList<FileType> types) =>
        Task.FromResult(SavePaths.Count > 0 ? SavePaths.Dequeue() : null);
}
