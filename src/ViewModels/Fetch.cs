using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class Fetch : Popup
    {
        public List<Models.Remote> Remotes
        {
            get => _repo.Remotes;
        }

        public bool IsFetchAllRemoteVisible
        {
            get;
        }

        public bool FetchAllRemotes
        {
            get => _fetchAllRemotes;
            set
            {
                if (SetProperty(ref _fetchAllRemotes, value))
                {
                    if (IsFetchAllRemoteVisible)
                        _repo.UIStates.FetchAllRemotes = value;

                    RefreshTargetSummary();
                }
            }
        }

        public Models.Remote SelectedRemote
        {
            get => _selectedRemote;
            set
            {
                if (SetProperty(ref _selectedRemote, value))
                    RefreshTargetSummary();
            }
        }

        #region Pre-execution target summary (A03/A04/A21)
        // Read-only presentation state only. It reuses the already loaded remotes and
        // never starts a process, never touches git config and never changes the URLs
        // used by `Commands.Fetch`.

        /// <summary>Names of every remote this popup will fetch from.</summary>
        public string TargetRemoteNames => _targetRemoteNames;

        /// <summary>`name -> sanitized endpoint` lines, credentials already dropped.</summary>
        public string TargetEndpoints => _targetEndpoints;

        /// <summary>True when at least one endpoint could not be shown safely.</summary>
        public bool IsTargetEndpointHidden => _isTargetEndpointHidden;

        /// <summary>True when a credential, query or fragment was dropped from an endpoint.</summary>
        public bool IsTargetSecretHidden => _isTargetSecretHidden;

        public bool IsAuthSSH => _authMode == AuthMode.SSH;
        public bool IsAuthHTTPS => _authMode == AuthMode.HTTPS;
        public bool IsAuthAnonymous => _authMode == AuthMode.Anonymous;
        public bool IsAuthMixed => _authMode == AuthMode.Mixed;
        public bool IsSSHKeyConfigured => _isSSHKeyConfigured;
        public bool IsSSHKeyDefault => _isSSHKeyDefault;

        private enum AuthMode { None, SSH, HTTPS, Anonymous, Mixed }

        private void RefreshTargetSummary()
        {
            var remotes = new List<Models.Remote>();
            if (FetchAllRemotes)
            {
                foreach (var remote in _repo.Remotes)
                {
                    if (remote != null)
                        remotes.Add(remote);
                }
            }
            else if (SelectedRemote != null)
            {
                remotes.Add(SelectedRemote);
            }

            var names = new List<string>();
            var endpoints = new List<string>();
            var modes = new HashSet<Models.EndpointAuthMode>();
            var sshCount = 0;
            var sshKeyCount = 0;
            _isTargetEndpointHidden = false;
            _isTargetSecretHidden = false;

            foreach (var remote in remotes)
            {
                names.Add(remote.Name);

                var display = Models.RemoteEndpointDisplay.Parse(remote.URL);
                if (display.IsDisplayable)
                    endpoints.Add(FetchAllRemotes ? $"{remote.Name} \u2192 {display.Display}" : display.Display);
                else
                    _isTargetEndpointHidden = true;

                if (display.HasHiddenParts)
                    _isTargetSecretHidden = true;

                if (display.AuthMode != Models.EndpointAuthMode.Unknown)
                    modes.Add(display.AuthMode);

                if (display.IsSSH)
                {
                    sshCount++;
                    if (!string.IsNullOrEmpty(remote.PrivateSSHKey))
                        sshKeyCount++;
                }
            }

            if (modes.Count == 0)
                _authMode = AuthMode.None;
            else if (modes.Count == 1)
                _authMode = modes.Contains(Models.EndpointAuthMode.SSH) ? AuthMode.SSH : (modes.Contains(Models.EndpointAuthMode.HTTPS) ? AuthMode.HTTPS : AuthMode.Anonymous);
            else
                _authMode = AuthMode.Mixed;

            _isSSHKeyConfigured = sshCount > 0 && sshKeyCount == sshCount;
            _isSSHKeyDefault = sshCount > 0 && sshKeyCount == 0;

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
            OnPropertyChanged(nameof(IsAuthSSH));
            OnPropertyChanged(nameof(IsAuthHTTPS));
            OnPropertyChanged(nameof(IsAuthAnonymous));
            OnPropertyChanged(nameof(IsAuthMixed));
            OnPropertyChanged(nameof(IsSSHKeyConfigured));
            OnPropertyChanged(nameof(IsSSHKeyDefault));
        }
        #endregion

        public bool NoTags
        {
            get => _repo.UIStates.FetchWithoutTags;
            set => _repo.UIStates.FetchWithoutTags = value;
        }

        public bool Force
        {
            get => _repo.UIStates.EnableForceOnFetch;
            set => _repo.UIStates.EnableForceOnFetch = value;
        }

        public Fetch(Repository repo, Models.Remote preferredRemote = null)
        {
            _repo = repo;
            IsFetchAllRemoteVisible = repo.Remotes.Count > 1 && preferredRemote == null;
            _fetchAllRemotes = IsFetchAllRemoteVisible && _repo.UIStates.FetchAllRemotes;
            CanTerminate = true;

            if (preferredRemote != null)
            {
                SelectedRemote = preferredRemote;
            }
            else if (!string.IsNullOrEmpty(_repo.Settings.DefaultRemote))
            {
                var def = _repo.Remotes.Find(r => r.Name == _repo.Settings.DefaultRemote);
                SelectedRemote = def ?? _repo.Remotes[0];
            }
            else
            {
                SelectedRemote = _repo.Remotes[0];
            }

            RefreshTargetSummary();
        }

        public override async Task<bool> Sure()
        {
            using var lockWatcher = _repo.LockWatcher();

            var navigateToUpstreamHEAD = _repo.IsDashboardVisible &&
                _repo.Histories.SelectedCommits.Count == 1 &&
                _repo.Histories.SelectedCommits[0].IsCurrentHead;

            var notags = _repo.UIStates.FetchWithoutTags;
            var force = _repo.UIStates.EnableForceOnFetch;
            var log = _repo.CreateLog("Fetch");
            Use(log);

            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;

            if (FetchAllRemotes)
            {
                foreach (var remote in _repo.Remotes)
                {
                    await new Commands.Fetch(_repo.FullPath, remote, notags, force)
                        .WithCancellation(token)
                        .Use(log)
                        .ExecAsync();

                    if (token.IsCancellationRequested)
                        break;
                }
            }
            else
            {
                await new Commands.Fetch(_repo.FullPath, SelectedRemote, notags, force)
                    .WithCancellation(token)
                    .Use(log)
                    .ExecAsync();
            }

            log.Complete();

            if (navigateToUpstreamHEAD && !token.IsCancellationRequested)
            {
                var upstream = _repo.CurrentBranch?.Upstream;
                if (!string.IsNullOrEmpty(upstream))
                {
                    var upstreamHead = await new Commands.QueryRevisionByRefName(_repo.FullPath, upstream.Substring(13)).GetResultAsync();
                    _repo.NavigateToCommit(upstreamHead, true);
                }
            }

            if (!token.IsCancellationRequested)
                _repo.MarkFetched();

            _cancellation = null;
            return true;
        }

        public override void Terminate()
        {
            // Just fire cancel event and UI will auto wait the `Sure` complete
            var _ = _cancellation?.CancelAsync();
        }

        private readonly Repository _repo = null;
        private Models.Remote _selectedRemote = null;
        private bool _fetchAllRemotes = false;
        private string _targetRemoteNames = "";
        private string _targetEndpoints = "";
        private bool _isTargetEndpointHidden = false;
        private bool _isTargetSecretHidden = false;
        private AuthMode _authMode = AuthMode.None;
        private bool _isSSHKeyConfigured = false;
        private bool _isSSHKeyDefault = false;
        private CancellationTokenSource _cancellation = null;
    }
}
