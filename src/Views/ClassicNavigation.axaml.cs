using System;
using System.ComponentModel;
using System.IO;

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    /// <summary>
    /// Workspace navigation for the opt-in Classic layout. It is a pure view over the existing
    /// <see cref="ViewModels.Repository"/>: the item order (File Status, History, Stashes) is mapped
    /// onto the unchanged SelectedViewIndex contract (1, 0, 2). Selection is two-way synchronized with
    /// the VM so global Ctrl+1/2/3 handled by the Launcher keep the highlight correct, and this control
    /// declares no hotkeys of its own.
    /// </summary>
    public partial class ClassicNavigation : UserControl
    {
        // ListBox item index -> ViewModels.Repository.SelectedViewIndex.
        private static readonly int[] ListItemToViewIndex = { 1, 0, 2 };
        // ViewModels.Repository.SelectedViewIndex -> ListBox item index.
        private static readonly int[] ViewIndexToListItem = { 1, 0, 2 };

        private bool _syncing = false;
        private ViewModels.Repository _watchedRepo = null;

        public ClassicNavigation()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // DataContext may be swapped while already attached: re-watch and drop any stale highlight.
            if (IsLoaded)
                Watch(Repo);

            SyncFromViewModel();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            Watch(Repo);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            // Never keep a VM reference alive while detached.
            Watch(null);
        }

        private ViewModels.Repository Repo => DataContext as ViewModels.Repository;

        private void Watch(ViewModels.Repository repo)
        {
            if (_watchedRepo != null)
                _watchedRepo.PropertyChanged -= OnRepoPropertyChanged;

            _watchedRepo = repo;

            if (repo != null)
                repo.PropertyChanged += OnRepoPropertyChanged;

            SyncFromViewModel();
        }

        private void OnRepoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Keep the highlight correct when the page changes by shortcuts, merge dialogs, etc.
            if (e.PropertyName == null || e.PropertyName == nameof(ViewModels.Repository.SelectedViewIndex))
                SyncFromViewModel();
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing || !ReferenceEquals(e.Source, NavList))
                return;

            var repo = Repo;
            var index = NavList.SelectedIndex;
            if (repo == null || index < 0 || index >= ListItemToViewIndex.Length)
                return;

            _syncing = true;
            repo.SelectedViewIndex = ListItemToViewIndex[index];
            _syncing = false;
        }

        private void SyncFromViewModel()
        {
            var repo = Repo;
            var page = repo != null ? repo.SelectedViewIndex : -1;
            var index = page >= 0 && page < ViewIndexToListItem.Length ? ViewIndexToListItem[page] : -1;

            if (NavList.SelectedIndex != index)
            {
                _syncing = true;
                NavList.SelectedIndex = index;
                _syncing = false;
            }
        }

        private async void OnOpenConfigure(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await this.ShowDialogAsync(new ViewModels.RepositoryConfigure(repo));
                e.Handled = true;
            }
        }

        // Same native calls as the original toolbar's "open with external tools" menu items; no new
        // process engine, no extra settings, and the missing-terminal case keeps reporting itself through
        // Native.OS.OpenTerminal's own notification.
        private void OnOpenInFileManager(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && Directory.Exists(repo.FullPath))
            {
                Native.OS.OpenInFileManager(repo.FullPath);
                e.Handled = true;
            }
        }

        private void OnOpenTerminal(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && Directory.Exists(repo.FullPath))
            {
                Native.OS.OpenTerminal(repo.FullPath);
                e.Handled = true;
            }
        }
    }
}
