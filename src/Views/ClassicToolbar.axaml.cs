using System;
using System.Collections.Generic;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    /// <summary>
    /// Labelled command bar for the opt-in Classic layout.
    /// It is a pure view over the existing <see cref="ViewModels.Repository"/>: every action calls a
    /// method or popup VM that already exists, so no Git execution lives in this file.
    /// It declares no HotKey at all, so toolbar hotkeys are migrated once, elsewhere.
    /// Availability is computed in one place (<see cref="ComputeStates"/>) and used both to enable the
    /// buttons and to guard the click handlers.
    /// </summary>
    public partial class ClassicToolbar : UserControl
    {
        public ClassicToolbar()
        {
            InitializeComponent();
        }

        private ViewModels.Repository Repo => DataContext as ViewModels.Repository;

        private ViewModels.Repository _watchedRepo = null;
        private ViewModels.WorkingCopy _watchedCopy = null;
        private ViewModels.LauncherPage _watchedPage = null;
        private ViewModels.Popup _watchedPopup = null;

        private static readonly HashSet<string> WatchedRepoProps = new(StringComparer.Ordinal)
        {
            nameof(ViewModels.Repository.CurrentBranch),
            nameof(ViewModels.Repository.Branches),
            nameof(ViewModels.Repository.Remotes),
            nameof(ViewModels.Repository.LocalChangesCount),
            nameof(ViewModels.Repository.InProgressContext),
            nameof(ViewModels.Repository.IsAutoFetching),
            nameof(ViewModels.Repository.IsSkippingOrAbortingMerge),
            nameof(ViewModels.Repository.WorkingCopy),
        };

        private static readonly HashSet<string> WatchedCopyProps = new(StringComparer.Ordinal)
        {
            nameof(ViewModels.WorkingCopy.IsCommitting),
            nameof(ViewModels.WorkingCopy.IsStaging),
            nameof(ViewModels.WorkingCopy.IsUnstaging),
            nameof(ViewModels.WorkingCopy.InProgressContext),
        };

        // The owning page VM owns the popup this toolbar has to treat as "busy"; neither Popup nor
        // Popup.InProgress is reachable from the repository VM.
        private static readonly HashSet<string> WatchedPageProps = new(StringComparer.Ordinal)
        {
            nameof(ViewModels.LauncherPage.Popup),
        };

        private static readonly HashSet<string> WatchedPopupProps = new(StringComparer.Ordinal)
        {
            nameof(ViewModels.Popup.InProgress),
        };

        private struct States
        {
            public bool Commit;
            public bool Pull;
            public bool Push;
            public bool Fetch;
            public bool Branch;
            public bool Merge;
            public bool Stash;
            public bool Tag;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Rebind on a rebind: never keep the previous repository/page/popup references alive.
            if (IsLoaded)
                Rebind();
            else
                UnwatchAll();

            RefreshStates();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            Rebind();
            RefreshStates();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            // Nothing may stay subscribed while detached.
            UnwatchAll();
        }

        private void Rebind()
        {
            UnwatchAll();

            var repo = Repo;
            if (repo == null)
                return;

            _watchedRepo = repo;
            _watchedRepo.PropertyChanged += OnRepoPropertyChanged;
            WatchCopy(repo);

            EnsurePage();
        }

        private void UnwatchAll()
        {
            if (_watchedRepo != null)
            {
                _watchedRepo.PropertyChanged -= OnRepoPropertyChanged;
                _watchedRepo = null;
            }

            if (_watchedCopy != null)
            {
                _watchedCopy.PropertyChanged -= OnCopyPropertyChanged;
                _watchedCopy = null;
            }

            if (_watchedPopup != null)
            {
                _watchedPopup.PropertyChanged -= OnPopupPropertyChanged;
                _watchedPopup = null;
            }

            if (_watchedPage != null)
            {
                _watchedPage.PropertyChanged -= OnPagePropertyChanged;
                _watchedPage = null;
            }
        }

        private void WatchCopy(ViewModels.Repository repo)
        {
            if (_watchedCopy != null)
                _watchedCopy.PropertyChanged -= OnCopyPropertyChanged;

            _watchedCopy = repo.WorkingCopy;

            if (_watchedCopy != null)
                _watchedCopy.PropertyChanged += OnCopyPropertyChanged;
        }

        /// <summary>
        /// Subscribe to the owning LauncherPage VM found through the visual ancestor. Called again when a
        /// state refresh still has no page, so a page DataContext that lands after Loaded is not missed.
        /// </summary>
        private void EnsurePage()
        {
            if (_watchedPage != null)
                return;

            if (this.FindAncestorOfType<LauncherPage>()?.DataContext is ViewModels.LauncherPage page)
            {
                _watchedPage = page;
                _watchedPage.PropertyChanged += OnPagePropertyChanged;
                WatchPopup();
            }
        }

        /// <summary> Re-point the popup subscription after a popup opened, was replaced, or closed. </summary>
        private void WatchPopup()
        {
            var popup = _watchedPage?.Popup;
            if (ReferenceEquals(_watchedPopup, popup))
                return;

            if (_watchedPopup != null)
                _watchedPopup.PropertyChanged -= OnPopupPropertyChanged;

            _watchedPopup = popup;

            if (_watchedPopup != null)
                _watchedPopup.PropertyChanged += OnPopupPropertyChanged;
        }

        private void OnRepoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || WatchedRepoProps.Contains(e.PropertyName))
            {
                // WorkingCopy is created by Repository.Open(); adopt it as soon as it exists.
                if (_watchedCopy == null && _watchedRepo != null)
                    WatchCopy(_watchedRepo);

                RefreshStates();
            }
        }

        private void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || WatchedPageProps.Contains(e.PropertyName))
            {
                WatchPopup();
                RefreshStates();
            }
        }

        private void OnCopyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || WatchedCopyProps.Contains(e.PropertyName))
                RefreshStates();
        }

        private void OnPopupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || WatchedPopupProps.Contains(e.PropertyName))
                RefreshStates();
        }

        private void RefreshStates()
        {
            if (_watchedPage == null && IsLoaded)
                EnsurePage();

            var s = ComputeStates();

            Set(BtnCommit, s.Commit);
            Set(BtnPull, s.Pull);
            Set(BtnPush, s.Push);
            Set(BtnFetch, s.Fetch);
            Set(BtnBranch, s.Branch);
            Set(BtnMerge, s.Merge);
            Set(BtnStash, s.Stash);
            Set(BtnTag, s.Tag);
        }

        private States ComputeStates()
        {
            var repo = Repo;
            if (repo == null)
                return new States();

            var copy = repo.WorkingCopy;

            // CanCreatePopup() rejects exactly a popup that is InProgress plus an auto-fetch in flight;
            // a running commit/stage/unstage or a merge skip-abort is refused by the VMs the same way.
            var busy = !repo.CanCreatePopup() ||
                        repo.IsSkippingOrAbortingMerge ||
                        copy is { IsCommitting: true } ||
                        copy is { IsStaging: true } ||
                        copy is { IsUnstaging: true };

            // No HEAD means an empty repository. A detached HEAD is NOT null: it is the current entry with
            // IsDetachedHead, whose Name is a "(HEAD detached at ...)" label and no refname, so it cannot
            // be pulled, pushed or merged into. Creating a branch/tag is still valid there because those
            // popups base on Branch.Head (a sha) and handle a detached HEAD themselves.
            var head = repo.CurrentBranch;
            var hasHead = head != null;
            var attachedHead = head != null && !head.IsDetachedHead;
            var hasRemote = repo.Remotes.Count > 0;

            return new States
            {
                // Commit only switches to the existing Local Changes page; git cannot commit into a bare repo.
                Commit = !repo.IsBare && !busy,
                Pull = !repo.IsBare && attachedHead && hasRemote && !busy,
                Push = attachedHead && hasRemote && !busy,
                Fetch = hasRemote && !busy,
                Branch = hasHead && !busy,
                Merge = !repo.IsBare && attachedHead && !busy && repo.InProgressContext == null && HasMergeSource(repo),
                Stash = !repo.IsBare && !busy && repo.LocalChangesCount > 0,
                Tag = hasHead && !busy,
            };
        }

        private static bool HasMergeSource(ViewModels.Repository repo)
        {
            foreach (var branch in repo.Branches)
            {
                if (!branch.IsCurrent && !branch.IsDetachedHead)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A merge menu outlives the state it was built from: HEAD may have moved and branches may have
        /// been added, renamed, or deleted while it was open. Returns the branch to merge now, or null.
        /// </summary>
        private static Models.Branch RevalidateMergeSource(ViewModels.Repository repo, Models.Branch head, Models.Branch source)
        {
            var current = repo.CurrentBranch;
            if (repo.IsBare || current == null || current.IsDetachedHead || repo.InProgressContext != null || !repo.CanCreatePopup())
                return null;

            // The menu was built for this exact HEAD; anything else is a different merge.
            if (!current.Name.Equals(head.Name, StringComparison.Ordinal) ||
                !current.Head.Equals(head.Head, StringComparison.Ordinal))
                return null;

            foreach (var branch in repo.Branches)
            {
                if (branch.IsCurrent || branch.IsDetachedHead)
                    continue;

                if (branch.IsLocal == source.IsLocal &&
                    branch.Remote == source.Remote &&
                    branch.Name.Equals(source.Name, StringComparison.Ordinal))
                    return branch;
            }

            return null;
        }

        private static void Set(Button button, bool enabled)
        {
            if (button != null && button.IsEnabled != enabled)
                button.IsEnabled = enabled;
        }

        private void OnCommit(object sender, RoutedEventArgs e)
        {
            // Never commits: it selects the existing Local Changes page (1 = WorkingCopy).
            if (ComputeStates().Commit && Repo is { } repo)
                repo.SelectedViewIndex = 1;

            e.Handled = true;
        }

        private async void OnPull(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Pull && Repo is { } repo)
                await repo.PullAsync(false);

            e.Handled = true;
        }

        private async void OnPush(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Push && Repo is { } repo)
                await repo.PushAsync(false);

            e.Handled = true;
        }

        private async void OnFetch(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Fetch && Repo is { } repo)
                await repo.FetchAsync(false);

            e.Handled = true;
        }

        private void OnBranch(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Branch && Repo is { } repo)
                repo.CreateNewBranch();

            e.Handled = true;
        }

        private void OnTag(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Tag && Repo is { } repo)
                repo.CreateNewTag();

            e.Handled = true;
        }

        private async void OnStash(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Stash && Repo is { } repo)
                await repo.StashAllAsync(false);

            e.Handled = true;
        }

        private void OnMerge(object sender, RoutedEventArgs e)
        {
            if (ComputeStates().Merge && sender is Control control && Repo is { CurrentBranch: { } head } repo)
            {
                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;

                AddMergeItems(menu, repo, head, true);
                var locals = menu.Items.Count;
                AddMergeItems(menu, repo, head, false);
                if (menu.Items.Count > locals)
                    menu.Items.Insert(locals, new MenuItem() { Header = "-" });

                if (menu.Items.Count > 0)
                    menu.Open(control);
            }

            e.Handled = true;
        }

        private void AddMergeItems(ContextMenu menu, ViewModels.Repository repo, Models.Branch head, bool local)
        {
            foreach (var branch in repo.Branches)
            {
                if (branch.IsCurrent || branch.IsDetachedHead || branch.IsLocal != local)
                    continue;

                var source = branch;

                var item = new MenuItem();
                item.Header = source.FriendlyName;
                item.Icon = this.CreateMenuIcon(local ? "Icons.Branch" : "Icons.Remote");
                item.Click += (_, ev) =>
                {
                    var merged = RevalidateMergeSource(repo, head, source);
                    if (merged != null)
                        repo.ShowPopup(new ViewModels.Merge(repo, merged, repo.CurrentBranch.Name, false));

                    ev.Handled = true;
                };

                menu.Items.Add(item);
            }
        }
    }
}
