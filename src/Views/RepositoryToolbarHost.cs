using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace SourceGit.Views
{
    // Select once at construction; never create a second repository or watcher.
    public class RepositoryToolbarHost : ContentControl
    {
        private RepositoryToolbar _upstream = null;
        private Button _more = null;
        private ViewModels.LauncherPage _page = null;

        public RepositoryToolbarHost()
        {
            if (!ViewModels.Preferences.Instance.IsClassicLayoutActive)
            {
                Content = new RepositoryToolbar { Height = 36 };
                return;
            }

            var commands = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                MinHeight = 52,
            };
            commands.Children.Add(new ClassicToolbar());

            // Keep the upstream toolbar's advanced actions available without duplicating its view.
            var upstream = new RepositoryToolbar { Width = 960, Height = 36 };
            _upstream = upstream;
            // The persistent host owns these shortcuts in Classic mode, including before More opens.
            foreach (var button in upstream.GetLogicalDescendants().OfType<Button>())
                button.HotKey = null;

            var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            AddShortcut(commands, new KeyGesture(Key.B, modifier), repo =>
            {
                repo.CreateNewBranch();
                return Task.CompletedTask;
            });
            AddShortcut(commands, new KeyGesture(Key.Down, modifier), repo => repo.FetchAsync(true));
            AddShortcut(commands, new KeyGesture(Key.Down, modifier | KeyModifiers.Shift), repo => repo.PullAsync(true));
            AddShortcut(commands, new KeyGesture(Key.Up, modifier | KeyModifiers.Shift), repo => repo.PushAsync(true));
            var more = new Button
            {
                Margin = new Thickness(8, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Flyout = new Flyout
                {
                    Content = upstream,
                    Placement = PlacementMode.BottomEdgeAlignedRight,
                },
            };
            _more = more;
            // Bind as a dynamic resource instead of App.Text: a frozen string would keep the old
            // language on this label when the locale is switched while the app is running.
            more.Bind(Button.ContentProperty, new DynamicResourceExtension("Text.Classic.More"));
            // Flyout content is outside the normal visual tree.
            more.DataContextChanged += (_, _) => SyncDataContext();
            DataContextChanged += (_, _) => SyncDataContext();
            Grid.SetColumn(more, 1);
            commands.Children.Add(more);
            Content = commands;
            SyncDataContext();
        }

        // A flyout is drawn in the window's popup layer, above everything the page itself draws, while
        // a repository popup is page content. So an action picked from the More flyout - a settings
        // dialog, a fetch popup - opened behind the icon row and looked like nothing had happened.
        // The flyout content here is a toolbar, not a menu, so Avalonia does not dismiss it on click.
        // Dismissing it when the page actually shows a popup fixes every entry point at once and
        // touches no command, no popup and no shortcut: by the time the popup exists the action has
        // already run, and the popup lives in the page rather than in the dismissed flyout.
        private void SyncDataContext()
        {
            _upstream.DataContext = DataContext;
            WatchPage(FindPage(DataContext as ViewModels.Repository));
        }

        private void WatchPage(ViewModels.LauncherPage page)
        {
            if (ReferenceEquals(page, _page))
                return;

            if (_page != null)
                _page.PropertyChanged -= OnPageChanged;

            _page = page;

            if (_page != null)
                _page.PropertyChanged += OnPageChanged;
        }

        private void OnPageChanged(object sender, PropertyChangedEventArgs ev)
        {
            if (ev.PropertyName != nameof(ViewModels.LauncherPage.Popup) || _page?.Popup == null)
                return;

            _more?.Flyout?.Hide();
            Dispatcher.UIThread.Post(FocusPopup);
        }

        // Hiding a flyout hands focus back to the button that opened it, which would leave Tab and
        // typing outside the dialog that just appeared. Focus moves into the popup itself; popups that
        // place focus on their own (search fields, commit editors) simply win afterwards.
        private void FocusPopup()
        {
            if (_page?.Popup == null)
                return;

            var popup = _page.Popup;
            var pageView = this.GetLogicalAncestors().OfType<LauncherPage>().FirstOrDefault();
            var host = pageView?.GetLogicalDescendants().OfType<Control>().FirstOrDefault(x => ReferenceEquals(x.DataContext, popup));
            var target = host?.GetLogicalDescendants().OfType<InputElement>().FirstOrDefault(x => x is { Focusable: true, IsEffectivelyVisible: true });

            target?.Focus(NavigationMethod.Unspecified);
        }

        private static ViewModels.LauncherPage FindPage(ViewModels.Repository repo)
        {
            if (repo == null || App.GetLauncher() is not ViewModels.Launcher launcher)
                return null;

            foreach (var page in launcher.Pages)
            {
                if (page.Node != null && string.Equals(page.Node.Id, repo.FullPath, StringComparison.Ordinal))
                    return page;
            }

            return null;
        }

        private void AddShortcut(Grid host, KeyGesture gesture, Func<ViewModels.Repository, Task> action)
        {
            host.Children.Add(new Button
            {
                IsVisible = false,
                HotKey = gesture,
                Command = new AsyncRelayCommand(async () =>
                {
                    if (App.GetLauncher() is { CommandPalette: not null })
                        return;

                    if (DataContext is ViewModels.Repository repo && repo.CanCreatePopup())
                        await action(repo);
                }),
            });
        }
    }
}
