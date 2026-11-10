using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class Pull : Popup
    {
        public List<Models.Remote> Remotes => _repo.Remotes;
        public Models.Branch Current { get; }

        public bool HasSpecifiedRemoteBranch
        {
            get;
            private set;
        }

        public Models.Remote SelectedRemote
        {
            get => _selectedRemote;
            set
            {
                if (SetProperty(ref _selectedRemote, value))
                {
                    PostRemoteSelected();
                    RefreshTargetSummary();
                }
            }
        }

        public List<Models.Branch> RemoteBranches
        {
            get => _remoteBranches;
            private set => SetProperty(ref _remoteBranches, value);
        }

        [Required(ErrorMessage = "Remote branch to pull is required!!!")]
        public Models.Branch SelectedBranch
        {
            get => _selectedBranch;
            set
            {
                if (SetProperty(ref _selectedBranch, value, true))
                    RefreshTargetSummary();
            }
        }

        public bool HasLocalChanges
        {
            get => _repo.LocalChangesCount > 0;
        }

        public Models.DealWithLocalChanges DealWithLocalChanges
        {
            get;
            set;
        }

        public bool UseRebase
        {
            get => _repo.UIStates.PreferRebaseInsteadOfMerge;
            set => _repo.UIStates.PreferRebaseInsteadOfMerge = value;
        }

        public Pull(Repository repo, Models.Branch specifiedRemoteBranch)
        {
            _repo = repo;
            Current = repo.CurrentBranch;
            CanTerminate = true;

            DealWithLocalChanges = Preferences.Instance.UseStashAndReapplyByDefault ?
                Models.DealWithLocalChanges.StashAndReapply :
                Models.DealWithLocalChanges.DoNothing;

            if (specifiedRemoteBranch != null)
            {
                _selectedRemote = repo.Remotes.Find(x => x.Name == specifiedRemoteBranch.Remote);
                _selectedBranch = specifiedRemoteBranch;

                var branches = new List<Models.Branch>();
                foreach (var branch in _repo.Branches)
                {
                    if (branch.Remote == specifiedRemoteBranch.Remote)
                        branches.Add(branch);
                }

                _remoteBranches = branches;
                HasSpecifiedRemoteBranch = true;
            }
            else
            {
                Models.Remote autoSelectedRemote = null;
                if (!string.IsNullOrEmpty(Current.Upstream))
                {
                    var remoteNameEndIdx = Current.Upstream.IndexOf('/', 13);
                    if (remoteNameEndIdx > 0)
                    {
                        var remoteName = Current.Upstream.Substring(13, remoteNameEndIdx - 13);
                        autoSelectedRemote = _repo.Remotes.Find(x => x.Name == remoteName);
                    }
                }

                if (autoSelectedRemote == null)
                {
                    Models.Remote remote = null;
                    if (!string.IsNullOrEmpty(_repo.Settings.DefaultRemote))
                        remote = _repo.Remotes.Find(x => x.Name == _repo.Settings.DefaultRemote);
                    _selectedRemote = remote ?? _repo.Remotes[0];
                }
                else
                {
                    _selectedRemote = autoSelectedRemote;
                }

                PostRemoteSelected();
                HasSpecifiedRemoteBranch = false;
            }

            RefreshTargetSummary();
        }

        public override async Task<bool> Sure()
        {
            using var lockWatcher = _repo.LockWatcher();

            var log = _repo.CreateLog("Pull");
            Use(log);

            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;

            var changes = await new Commands.CountLocalChanges(_repo.FullPath, false).GetResultAsync();
            var needPopStash = false;
            if (changes > 0)
            {
                if (DealWithLocalChanges == Models.DealWithLocalChanges.DoNothing)
                {
                    // Do nothing, just let the pull command fail and show the error to user
                }
                else if (DealWithLocalChanges == Models.DealWithLocalChanges.StashAndReapply)
                {
                    var succ = await new Commands.Stash(_repo.FullPath).Use(log).PushAsync("PULL_AUTO_STASH", false);
                    if (!succ)
                    {
                        log.Complete();
                        _repo.MarkWorkingCopyDirtyManually();
                        _cancellation = null;
                        return false;
                    }

                    needPopStash = true;
                }
                else
                {
                    await Commands.Discard.AllAsync(_repo.FullPath, true, false, false, log);
                }
            }

            bool rs = false;
            if (!token.IsCancellationRequested)
            {
                var target = _selectedBranch;
                if (!string.IsNullOrEmpty(Current.Upstream) && target.FullName.Equals(Current.Upstream, StringComparison.Ordinal))
                    target = null;

                rs = await new Commands.Pull(
                    _repo.FullPath,
                    _selectedRemote,
                    target,
                    UseRebase).WithCancellation(token).Use(log).ExecAsync();

                if (rs)
                {
                    await _repo.AutoUpdateSubmodulesAsync(log);

                    if (needPopStash)
                        await new Commands.Stash(_repo.FullPath).Use(log).PopAsync("stash@{0}");
                }
            }

            log.Complete();

            if (_repo.SelectedViewIndex == 0)
            {
                var head = await new Commands.QueryRevisionByRefName(_repo.FullPath, "HEAD").GetResultAsync();
                _repo.NavigateToCommit(head, true);
            }

            _cancellation = null;
            return rs;
        }

        public override void Terminate()
        {
            // Just fire cancel event and UI will auto wait the `Sure` complete
            var _ = _cancellation?.CancelAsync();
        }

        #region Pre-execution target summary (A03/A04/A21)
        // Read-only presentation state. It reuses the already loaded remotes/branches and
        // never starts a process, never changes git config and never changes the URL or
        // the arguments used by `Commands.Pull`.

        /// <summary>Name of the remote the pull will read from.</summary>
        public string TargetRemoteNames => _targetRemoteNames;

        /// <summary>Sanitized endpoint of that remote, credentials already dropped.</summary>
        public string TargetEndpoints => _targetEndpoints;

        /// <summary>True when the endpoint could not be shown safely.</summary>
        public bool IsTargetEndpointHidden => _isTargetEndpointHidden;

        /// <summary>True when a credential, query or fragment was dropped from the endpoint.</summary>
        public bool IsTargetSecretHidden => _isTargetSecretHidden;

        /// <summary>`remote/branch -> local branch`.</summary>
        public string TargetMapping => _targetMapping;

        /// <summary>True when no remote branch is selected yet.</summary>
        public bool IsMappingMissing => _isMappingMissing;

        /// <summary>True when the selected remote branch is the upstream of the current branch.</summary>
        public bool IsTargetUpstream => _isTargetUpstream;

        /// <summary>True when the selected remote branch is NOT the upstream of the current branch.</summary>
        public bool IsTargetNotUpstream => _isTargetNotUpstream;

        /// <summary>True when the current branch has no tracking branch at all.</summary>
        public bool IsTargetNoUpstream => _isTargetNoUpstream;

        public bool IsAuthSSH => _authMode == AuthMode.SSH;
        public bool IsAuthHTTPS => _authMode == AuthMode.HTTPS;
        public bool IsAuthAnonymous => _authMode == AuthMode.Anonymous;
        public bool IsAuthMixed => _authMode == AuthMode.Mixed;
        public bool IsSSHKeyConfigured => _isSSHKeyConfigured;
        public bool IsSSHKeyDefault => _isSSHKeyDefault;

        private enum AuthMode { None, SSH, HTTPS, Anonymous, Mixed }

        private void RefreshTargetSummary()
        {
            var remote = _selectedRemote;
            var names = remote == null ? new List<string>() : new List<string>() { remote.Name };
            var endpoints = new List<string>();
            _isTargetEndpointHidden = remote == null;
            _isTargetSecretHidden = false;
            _isSSHKeyConfigured = false;
            _isSSHKeyDefault = false;

            var mode = Models.EndpointAuthMode.Unknown;
            if (remote != null)
            {
                var display = Models.RemoteEndpointDisplay.Parse(remote.URL);
                if (display.IsDisplayable)
                    endpoints.Add(display.Display);
                else
                    _isTargetEndpointHidden = true;

                _isTargetSecretHidden = display.HasHiddenParts;
                mode = display.AuthMode;

                if (display.IsSSH)
                {
                    _isSSHKeyConfigured = !string.IsNullOrEmpty(remote.PrivateSSHKey);
                    _isSSHKeyDefault = !_isSSHKeyConfigured;
                }
            }

            if (mode == Models.EndpointAuthMode.SSH)
                _authMode = AuthMode.SSH;
            else if (mode == Models.EndpointAuthMode.HTTPS)
                _authMode = AuthMode.HTTPS;
            else if (mode == Models.EndpointAuthMode.Unknown)
                _authMode = AuthMode.None;
            else
                _authMode = AuthMode.Anonymous;

            var target = _selectedBranch;
            var upstream = Current?.Upstream;
            _isMappingMissing = target == null;
            _targetMapping = target == null ? "" : $"{target.FriendlyName} \u2192 {Current?.Name ?? ""}";
            _isTargetUpstream = target != null && !string.IsNullOrEmpty(upstream) &&
                target.FullName.Equals(upstream, StringComparison.Ordinal);
            _isTargetNoUpstream = target != null && string.IsNullOrEmpty(upstream);
            _isTargetNotUpstream = target != null && !string.IsNullOrEmpty(upstream) && !_isTargetUpstream;

            _targetRemoteNames = string.Join(", ", names);
            _targetEndpoints = string.Join(Environment.NewLine, endpoints);

            RaiseTargetSummary();
        }

        private void RaiseTargetSummary()
        {
            OnPropertyChanged(nameof(TargetRemoteNames));
            OnPropertyChanged(nameof(TargetEndpoints));
            OnPropertyChanged(nameof(IsTargetEndpointHidden));
            OnPropertyChanged(nameof(IsTargetSecretHidden));
            OnPropertyChanged(nameof(TargetMapping));
            OnPropertyChanged(nameof(IsMappingMissing));
            OnPropertyChanged(nameof(IsTargetUpstream));
            OnPropertyChanged(nameof(IsTargetNotUpstream));
            OnPropertyChanged(nameof(IsTargetNoUpstream));
            OnPropertyChanged(nameof(IsAuthSSH));
            OnPropertyChanged(nameof(IsAuthHTTPS));
            OnPropertyChanged(nameof(IsAuthAnonymous));
            OnPropertyChanged(nameof(IsAuthMixed));
            OnPropertyChanged(nameof(IsSSHKeyConfigured));
            OnPropertyChanged(nameof(IsSSHKeyDefault));
        }
        #endregion

        private void PostRemoteSelected()
        {
            var remoteName = _selectedRemote.Name;
            var branches = new List<Models.Branch>();
            foreach (var branch in _repo.Branches)
            {
                if (!branch.IsLocal && branch.Remote.Equals(remoteName, StringComparison.Ordinal))
                    branches.Add(branch);
            }

            Models.Branch selected = null;
            if (!string.IsNullOrEmpty(Current.Upstream))
                selected = branches.Find(x => x.FullName.Equals(Current.Upstream, StringComparison.Ordinal));

            if (selected == null)
                selected = branches.Find(x => x.Name.Equals(Current.Name, StringComparison.Ordinal));

            RemoteBranches = branches;
            SelectedBranch = selected;
        }

        private readonly Repository _repo = null;
        private Models.Remote _selectedRemote = null;
        private List<Models.Branch> _remoteBranches = null;
        private Models.Branch _selectedBranch = null;
        private string _targetRemoteNames = "";
        private string _targetEndpoints = "";
        private string _targetMapping = "";
        private bool _isTargetEndpointHidden = false;
        private bool _isTargetSecretHidden = false;
        private bool _isMappingMissing = false;
        private bool _isTargetUpstream = false;
        private bool _isTargetNotUpstream = false;
        private bool _isTargetNoUpstream = false;
        private AuthMode _authMode = AuthMode.None;
        private bool _isSSHKeyConfigured = false;
        private bool _isSSHKeyDefault = false;
        private CancellationTokenSource _cancellation = null;
    }
}
