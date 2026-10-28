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
    public Task<string?> PickSaveFile(string title, string suggestedName, IReadOnlyList<FileType> types) =>
        Task.FromResult(SavePaths.Count > 0 ? SavePaths.Dequeue() : null);
}
