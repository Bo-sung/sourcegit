using Avalonia.Media;

using SourceGit;

namespace SourceGit.Models
{
    public class ResetMode(string n, string a, string k, IBrush b)
    {
        public static readonly ResetMode[] Supported =
        [
            new ResetMode("Soft", "--soft", "S", Brushes.Green),
            new ResetMode("Mixed", "--mixed", "M", Brushes.Orange),
            new ResetMode("Merge", "--merge", "G", Brushes.Purple),
            new ResetMode("Keep", "--keep", "K", Brushes.Purple),
            new ResetMode("Hard", "--hard", "H", Brushes.Red),
        ];

        public string Id => n;
        public string Name => App.Text($"Reset.Mode.{n}");
        public string Desc => App.Text($"Reset.Mode.Desc.{n}");
        public string Arg => a;
        public string Key => k;
        public IBrush Color => b;
    }
}
