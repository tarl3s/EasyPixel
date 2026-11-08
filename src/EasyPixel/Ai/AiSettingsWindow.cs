using Avalonia.Controls;
using Avalonia.Layout;
using EasyPixel.Core.Ai;
using EasyPixel.Dialogs;

namespace EasyPixel.Ai;

/// <summary>Edit → AI settings: which image generator and text AI to use, and the keys (kept in settings.json on this computer).</summary>
internal sealed class AiSettingsWindow : DialogWindow
{
    private readonly ComboBox _image, _text;
    private readonly TextBox _account = new() { MinWidth = 260 };
    private readonly Dictionary<string, Func<string>> _keyOf = new();
    private readonly List<string> _imageIds = new() { "auto" }, _textIds = new() { "auto" };

    public AiSettingsWindow(AiChoice current, Action<string> openLink) : base(L.T("AI settings"))
    {
        var imageItems = new List<string> { L.T("Automatic (free ones with a key)") };
        foreach (var p in AiProviders.Images)
        {
            _imageIds.Add(p.Id);
            imageItems.Add(ProviderName(p.Id) + " — " + (p.Free ? L.T("Free") : L.T("Paid")) + (p.NeedsKey ? ", " + L.T("needs a key") : ""));
        }
        _image = new ComboBox { MinWidth = 260, ItemsSource = imageItems, SelectedIndex = Math.Max(0, _imageIds.IndexOf(current.Image)) };
        var textItems = new List<string> { L.T("Automatic") };
        foreach (var t in AiProviders.Texts) { _textIds.Add(t.Id); textItems.Add(ProviderName(t.Id)); }
        _text = new ComboBox { MinWidth = 260, ItemsSource = textItems, SelectedIndex = Math.Max(0, _textIds.IndexOf(current.Text)) };
        AddRow(L.T("Pictures"), _image);
        AddRow(L.T("Text (translation)"), _text);
        AddControl(new TextBlock { Text = L.T("On my computer — coming soon"), Opacity = 0.5, IsEnabled = false });

        AddText(L.T("Keys stay on this computer and are only sent to the service they belong to."));
        var ids = AiProviders.Images.Where(p => p.NeedsKey).Select(p => p.Id)
            .Concat(AiProviders.Texts.Where(t => t.NeedsKey).Select(t => t.Id)).Distinct();
        foreach (var id in ids) AddProvider(id, current.Keys.GetValueOrDefault(id) ?? "", openLink);

        AddButton(L.T("OK"), isDefault: true);
        AddButton(L.T("Cancel"), isCancel: true);
    }

    private static string ProviderName(string id) => id switch
    {
        "cloudflare" => "Cloudflare", "pollinations" => "Pollinations", "openai" => "OpenAI",
        "gemini" => "Google Gemini", "retrodiffusion" => "Retro Diffusion", _ => id,
    };

    private static string? SignUp(string id) => id switch
    {
        "cloudflare" => "https://dash.cloudflare.com/sign-up",
        "pollinations" => "https://enter.pollinations.ai/keys",
        _ => null,
    };

    private void AddProvider(string id, string key, Action<string> openLink)
    {
        AddControl(new TextBlock { Text = ProviderName(id), FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 6, 0, 0) });
        TextBox Masked(string text) => new() { Text = text, PasswordChar = '•', MinWidth = 260 };
        var status = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 440 };
        if (id == "cloudflare")
        {
            var parts = key.Split(':', 2);
            _account.Text = parts[0];
            var token = Masked(parts.Length > 1 ? parts[1] : "");
            AddRow(L.T("Account ID"), _account);
            AddRow(L.T("API token"), token);
            _keyOf[id] = () => string.IsNullOrWhiteSpace(token.Text) && string.IsNullOrWhiteSpace(_account.Text)
                ? "" : $"{_account.Text?.Trim()}:{token.Text?.Trim()}";
        }
        else
        {
            var box = Masked(key);
            AddRow(L.T("Key"), box);
            _keyOf[id] = () => box.Text?.Trim() ?? "";
        }
        var test = new Button { Content = L.T("Test") };
        test.Click += async (_, _) =>
        {
            test.IsEnabled = false;
            status.Text = L.T("Testing…");
            var (ok, msg) = await AiKeyTester.TestAsync(id, _keyOf[id]());
            status.Text = (ok ? "✓ " : "✗ ") + msg;
            test.IsEnabled = true;
        };
        var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        line.Children.Add(test);
        if (SignUp(id) is { } url)
        {
            var link = new Button { Content = L.T("Get a free key") };
            link.Click += (_, _) => openLink(url);
            line.Children.Add(link);
        }
        AddControl(line);
        AddControl(status);
        if (id == "cloudflare") AddText(L.T("Cloudflare: sign up, open Workers AI, copy the Account ID and make an API token with the Workers AI permission."));
        if (id == "pollinations") AddText(L.T("Pollinations: sign in and create a secret key (it starts with sk_)."));
    }

    public AiChoice Choice
    {
        get
        {
            var keys = new Dictionary<string, string>();
            foreach (var (id, get) in _keyOf) if (get() is { Length: > 0 } k && k != ":") keys[id] = k;
            return new AiChoice(_imageIds[Math.Max(0, _image.SelectedIndex)], _textIds[Math.Max(0, _text.SelectedIndex)], keys);
        }
    }
}
