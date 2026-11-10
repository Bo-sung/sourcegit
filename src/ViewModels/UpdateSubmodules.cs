using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class UpdateSubmodules : Popup
    {
        public bool HasPreSelectedSubmodule
        {
            get;
        }

        public List<Models.Submodule> Submodules
        {
            get => _repo.Submodules;
        }

        public Models.Submodule SelectedSubmodule
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                    RefreshTargets();
            }
        }

        public bool UpdateAll
        {
            get => _updateAll;
            set
            {
                if (SetProperty(ref _updateAll, value))
                    RefreshTargets();
            }
        }

        public bool IsEnableInitVisible
        {
            get;
            set;
        } = true;

        public bool EnableInit
        {
            get => _enableInit;
            set => SetProperty(ref _enableInit, value);
        }

        public bool EnableRecursive
        {
            get => _enableRecursive;
            set => SetProperty(ref _enableRecursive, value);
        }

        public bool EnableRemote
        {
            get => _enableRemote;
            set => SetProperty(ref _enableRemote, value);
        }

        /// <summary>
        /// Paths the executed command will pass to `git submodule update`.
        /// Read-only presentation of the already-loaded Repository.Submodules snapshot.
        /// </summary>
        public string TargetPaths
        {
            get => _targetPaths;
        }

        public bool HasTargets
        {
            get => _targets.Count > 0;
        }

        public string ModifiedTargetPaths
        {
            get => _modifiedTargetPaths;
        }

        public string UnmergedTargetPaths
        {
            get => _unmergedTargetPaths;
        }

        public bool HasModifiedTargets
        {
            get => _modifiedTargetPaths.Length > 0;
        }

        public bool HasUnmergedTargets
        {
            get => _unmergedTargetPaths.Length > 0;
        }

        public bool HasDirtyTargets
        {
            get => HasModifiedTargets || HasUnmergedTargets;
        }

        /// <summary>
        /// Targets that still need a clone and whose .gitmodules URL points at a local path.
        /// Git blocks the `file` transport for submodule clones unless `protocol.file.allow` is
        /// set, so this is the exact setup where the update can fail with `transport 'file' not allowed`.
        /// </summary>
        public bool HasLocalURLTargets
        {
            get => _localURLTargetPaths.Length > 0;
        }

        public string LocalURLTargetPaths
        {
            get => _localURLTargetPaths;
        }

        public UpdateSubmodules(Repository repo, Models.Submodule selected)
        {
            _repo = repo;

            if (selected != null)
            {
                _updateAll = false;
                SelectedSubmodule = selected;
                IsEnableInitVisible = selected.Status == Models.SubmoduleStatus.NotInited;
                EnableInit = selected.Status == Models.SubmoduleStatus.NotInited;
                HasPreSelectedSubmodule = true;
            }
            else if (repo.Submodules.Count > 0)
            {
                SelectedSubmodule = repo.Submodules[0];
                IsEnableInitVisible = true;
                HasPreSelectedSubmodule = false;
            }

            EnableRecursive = _repo.Settings.EnableRecursiveWhenAutoUpdatingSubmodules;
            RefreshTargets();
        }

        public override async Task<bool> Sure()
        {
            var targets = CollectTargets();
            if (targets.Count == 0)
                return true;

            var log = _repo.CreateLog("Update Submodule");
            using var lockWatcher = _repo.LockWatcher();
            Use(log);

            var succ = await new Commands.Submodule(_repo.FullPath)
                .Use(log)
                .UpdateAsync(targets, EnableInit, EnableRecursive, EnableRemote);

            log.Complete();
            _repo.MarkSubmodulesDirtyManually();
            return succ;
        }

        /// <summary>
        /// Same rule as Sure(): `All` passes every loaded path, otherwise the single selected path.
        /// </summary>
        private List<string> CollectTargets()
        {
            var targets = new List<string>();
            if (_updateAll)
            {
                foreach (var submodule in _repo.Submodules)
                    targets.Add(submodule.Path);
            }
            else if (_selected != null)
            {
                targets.Add(_selected.Path);
            }

            return targets;
        }

        /// <summary>
        /// Recomputes the target summary from the loaded model only. No git query is started here.
        /// </summary>
        private void RefreshTargets()
        {
            var targets = CollectTargets();
            var modified = new List<string>();
            var unmerged = new List<string>();
            var local_urls = new List<string>();

            if (targets.Count > 0)
            {
                var set = new HashSet<string>(targets, StringComparer.Ordinal);
                foreach (var submodule in _repo.Submodules)
                {
                    if (!set.Contains(submodule.Path))
                        continue;

                    if (submodule.Status == Models.SubmoduleStatus.Modified)
                        modified.Add(submodule.Path);
                    else if (submodule.Status == Models.SubmoduleStatus.Unmerged)
                        unmerged.Add(submodule.Path);

                    if (IsLocalURLTarget(submodule))
                        local_urls.Add(submodule.Path);
                }
            }

            _targets = targets;
            _targetPaths = string.Join(Environment.NewLine, targets);
            _modifiedTargetPaths = string.Join(Environment.NewLine, modified);
            _unmergedTargetPaths = string.Join(Environment.NewLine, unmerged);
            _localURLTargetPaths = string.Join(Environment.NewLine, local_urls);

            OnPropertyChanged(nameof(TargetPaths));
            OnPropertyChanged(nameof(HasTargets));
            OnPropertyChanged(nameof(ModifiedTargetPaths));
            OnPropertyChanged(nameof(UnmergedTargetPaths));
            OnPropertyChanged(nameof(HasModifiedTargets));
            OnPropertyChanged(nameof(HasUnmergedTargets));
            OnPropertyChanged(nameof(HasDirtyTargets));
            OnPropertyChanged(nameof(HasLocalURLTargets));
            OnPropertyChanged(nameof(LocalURLTargetPaths));
        }

        /// <summary>
        /// `true` for a URL that git would fetch through the `file` transport: an explicit `file://`
        /// URL or a scheme-less path. scp-like `user@host:path` URLs are remote and never match.
        /// </summary>
        private static bool IsLocalURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            var idx = url.IndexOf("://", StringComparison.Ordinal);
            if (idx >= 0)
                return url.AsSpan(0, idx).Equals("file", StringComparison.OrdinalIgnoreCase);

            // An `@` before the first `:` means the scp-like remote form.
            var colon = url.IndexOf(':', StringComparison.Ordinal);
            if (colon > 0)
            {
                var at = url.IndexOf('@', StringComparison.Ordinal);
                if (at >= 0 && at < colon)
                    return false;
            }

            return true;
        }

        private static bool IsLocalURLTarget(Models.Submodule submodule)
        {
            // Only an uninitialized target clones; an initialized one is updated from its own repo.
            return submodule.Status == Models.SubmoduleStatus.NotInited && IsLocalURL(submodule.URL);
        }

        private readonly Repository _repo = null;
        private Models.Submodule _selected = null;
        private bool _updateAll = true;
        private bool _enableInit = true;
        private bool _enableRecursive = true;
        private bool _enableRemote = false;
        private List<string> _targets = [];
        private string _targetPaths = string.Empty;
        private string _modifiedTargetPaths = string.Empty;
        private string _unmergedTargetPaths = string.Empty;
        private string _localURLTargetPaths = string.Empty;
    }
}
