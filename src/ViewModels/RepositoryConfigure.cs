using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class RepositoryConfigure : ObservableObject
    {
        public string UserName
        {
            get;
            set;
        }

        public string UserEmail
        {
            get;
            set;
        }

        public List<string> Remotes
        {
            get;
        }

        /// <summary>
        /// The remote list for the default-remote chooser. It keeps one empty entry first,
        /// so an unset default stays visible and can be selected again to clear a set default.
        /// </summary>
        public List<string> RemotesWithAuto
        {
            get;
        }

        public string DefaultRemote
        {
            get => _repo.Settings.DefaultRemote;
            set
            {
                if (_repo.Settings.DefaultRemote != value)
                {
                    _repo.Settings.DefaultRemote = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PreferredMergeMode
        {
            get => _repo.Settings.PreferredMergeMode;
            set
            {
                if (_repo.Settings.PreferredMergeMode != value)
                {
                    _repo.Settings.PreferredMergeMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool GPGCommitSigningEnabled
        {
            get => _gpgCommitSign;
            set
            {
                if (SetProperty(ref _gpgCommitSign, value))
                    RaiseSigningInfoChanged();
            }
        }

        public bool GPGTagSigningEnabled
        {
            get => _gpgTagSign;
            set
            {
                if (SetProperty(ref _gpgTagSign, value))
                    RaiseSigningInfoChanged();
            }
        }

        public string GPGUserSigningKey
        {
            get => _gpgUserSigningKey;
            set
            {
                if (SetProperty(ref _gpgUserSigningKey, value))
                    RaiseSigningInfoChanged();
            }
        }

        /// <summary>
        /// The checkboxes above only cover commit.gpgsign and tag.gpgsign. Whether a signed commit can
        /// actually run also depends on gpg.format and the signing program, and those are not editable
        /// here, so state the effective values: a repository value first, then the global value, then
        /// Git's own default (openpgp / gpg).
        /// </summary>
        public string SigningFormat
        {
            get; private set;
        }

        public string SigningProgram
        {
            get; private set;
        }

        /// <summary>A configured program path that does not exist; a bare name is resolved by Git itself.</summary>
        public bool HasSigningProgramMissing
        {
            get; private set;
        }

        /// <summary>Signed commits are requested without any user.signingkey.</summary>
        public bool HasSigningKeyMissing
        {
            get; private set;
        }

        /// <summary>Git's own commit.template, the other source of commit-message templates.</summary>
        public string GitCommitTemplatePath
        {
            get; private set;
        }

        public bool HasGitCommitTemplate
        {
            get; private set;
        }

        public bool HasGitCommitTemplateMissing
        {
            get; private set;
        }

        /// <summary>Shared issue-tracker rules live in this file, private ones in the local Git config.</summary>
        public string IssueTrackerSharedPath
        {
            get => $"{_repo.FullPath}/.issuetracker";
        }

        public bool HasIssueTrackerSharedFile
        {
            get; private set;
        }

        private void RaiseSigningInfoChanged()
        {
            var fmt = _cached.GetValueOrDefault("gpg.format", "");
            var model = Models.GPGFormat.Supported.Find(x => x.Value.Equals(fmt, StringComparison.OrdinalIgnoreCase));
            if (model == null)
            {
                fmt = "openpgp";
                model = Models.GPGFormat.Supported[0];
            }

            var program = _cached.GetValueOrDefault($"gpg.{fmt}.program", "");
            if (program.Length == 0)
                program = _cached.GetValueOrDefault("gpg.program", model.Program);

            SigningFormat = fmt;
            SigningProgram = program;
            HasSigningProgramMissing = (GPGCommitSigningEnabled || GPGTagSigningEnabled) && Path.IsPathRooted(program) && !File.Exists(program);
            HasSigningKeyMissing = GPGCommitSigningEnabled && GPGUserSigningKey.Length == 0;

            OnPropertyChanged(nameof(SigningFormat));
            OnPropertyChanged(nameof(SigningProgram));
            OnPropertyChanged(nameof(HasSigningProgramMissing));
            OnPropertyChanged(nameof(HasSigningKeyMissing));
        }

        public string HttpProxy
        {
            get => _httpProxy;
            set => SetProperty(ref _httpProxy, value);
        }

        public string ConventionalTypesOverride
        {
            get => _repo.Settings.ConventionalTypesOverride;
            set
            {
                if (_repo.Settings.ConventionalTypesOverride != value)
                {
                    _repo.Settings.ConventionalTypesOverride = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnablePruneOnFetch
        {
            get;
            set;
        }

        public bool AskBeforeAutoUpdatingSubmodules
        {
            get => _repo.Settings.AskBeforeAutoUpdatingSubmodules;
            set => _repo.Settings.AskBeforeAutoUpdatingSubmodules = value;
        }

        public bool EnableRecursiveWhenAutoUpdatingSubmodules
        {
            get => _repo.Settings.EnableRecursiveWhenAutoUpdatingSubmodules;
            set => _repo.Settings.EnableRecursiveWhenAutoUpdatingSubmodules = value;
        }

        public AvaloniaList<Models.CommitTemplate> CommitTemplates
        {
            get => _repo.Settings.CommitTemplates;
        }

        public Models.CommitTemplate SelectedCommitTemplate
        {
            get => _selectedCommitTemplate;
            set => SetProperty(ref _selectedCommitTemplate, value);
        }

        public AvaloniaList<Models.IssueTracker> IssueTrackers
        {
            get;
        } = [];

        public Models.IssueTracker SelectedIssueTracker
        {
            get => _selectedIssueTracker;
            set => SetProperty(ref _selectedIssueTracker, value);
        }

        public List<string> AvailableOpenAIServices
        {
            get;
            private set;
        }

        public string PreferredOpenAIService
        {
            get => _repo.Settings.PreferredOpenAIService;
            set => _repo.Settings.PreferredOpenAIService = value;
        }

        public AvaloniaList<Models.CustomAction> CustomActions
        {
            get => _repo.Settings.CustomActions;
        }

        public Models.CustomAction SelectedCustomAction
        {
            get => _selectedCustomAction;
            set => SetProperty(ref _selectedCustomAction, value);
        }

        public RepositoryConfigure(Repository repo)
        {
            _repo = repo;

            Remotes = new List<string>();
            foreach (var remote in _repo.Remotes)
                Remotes.Add(remote.Name);

            RemotesWithAuto = new List<string>(Remotes);
            RemotesWithAuto.Insert(0, string.Empty);

            AvailableOpenAIServices = new List<string>() { "---" };
            foreach (var service in Preferences.Instance.OpenAIServices)
                AvailableOpenAIServices.Add(service.Name);

            if (!AvailableOpenAIServices.Contains(PreferredOpenAIService))
                PreferredOpenAIService = "---";

            _cached = new Commands.Config(repo.FullPath).ReadAll();
            if (_cached.TryGetValue("user.name", out var name))
                UserName = name;
            if (_cached.TryGetValue("user.email", out var email))
                UserEmail = email;
            if (_cached.TryGetValue("commit.gpgsign", out var gpgCommitSign))
                GPGCommitSigningEnabled = gpgCommitSign == "true";
            if (_cached.TryGetValue("tag.gpgsign", out var gpgTagSign))
                GPGTagSigningEnabled = gpgTagSign == "true";
            if (_cached.TryGetValue("user.signingkey", out var signingKey))
                GPGUserSigningKey = signingKey;
            if (_cached.TryGetValue("http.proxy", out var proxy))
                HttpProxy = proxy;
            if (_cached.TryGetValue("fetch.prune", out var prune))
                EnablePruneOnFetch = (prune == "true");

            foreach (var rule in _repo.IssueTrackers)
            {
                IssueTrackers.Add(new()
                {
                    IsShared = rule.IsShared,
                    Name = rule.Name,
                    RegexString = rule.RegexString,
                    URLTemplate = rule.URLTemplate,
                });
            }

            HasIssueTrackerSharedFile = File.Exists(IssueTrackerSharedPath);
            OnPropertyChanged(nameof(HasIssueTrackerSharedFile));

            var template = _cached.GetValueOrDefault("commit.template", "");
            HasGitCommitTemplate = template.Length > 0;
            GitCommitTemplatePath = ExpandHomePath(template);
            // Git resolves a relative commit.template against the repository root, while File.Exists
            // would use this process's own working directory.
            var absolute = GitCommitTemplatePath;
            if (HasGitCommitTemplate && !Path.IsPathRooted(absolute))
                absolute = Path.Combine(_repo.FullPath, absolute);
            HasGitCommitTemplateMissing = HasGitCommitTemplate && !File.Exists(absolute);
            OnPropertyChanged(nameof(GitCommitTemplatePath));
            OnPropertyChanged(nameof(HasGitCommitTemplate));
            OnPropertyChanged(nameof(HasGitCommitTemplateMissing));

            RaiseSigningInfoChanged();
        }

        private static string ExpandHomePath(string value)
        {
            if (!value.StartsWith("~/", StringComparison.Ordinal) && !value.StartsWith("~\\", StringComparison.Ordinal))
                return value;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, value.Substring(2));
        }

        public void ClearHttpProxy()
        {
            HttpProxy = string.Empty;
        }

        public void AddCommitTemplate()
        {
            var template = new Models.CommitTemplate() { Name = "New Template" };
            _repo.Settings.CommitTemplates.Add(template);
            SelectedCommitTemplate = template;
        }

        public void RemoveSelectedCommitTemplate()
        {
            if (_selectedCommitTemplate != null)
                _repo.Settings.CommitTemplates.Remove(_selectedCommitTemplate);
            SelectedCommitTemplate = null;
        }

        public List<string> GetRemoteVisitUrls()
        {
            var outs = new List<string>();
            foreach (var remote in _repo.Remotes)
            {
                if (remote.TryGetVisitURL(out var url))
                    outs.Add(url);
            }
            return outs;
        }

        public void AddIssueTracker(string name, string regex, string url)
        {
            var rule = new Models.IssueTracker()
            {
                IsShared = false,
                Name = GetUniqueIssueTrackerName(name),
                RegexString = regex,
                URLTemplate = url,
            };

            IssueTrackers.Add(rule);
            SelectedIssueTracker = rule;
        }

        /// <summary>
        /// Rules are stored under issuetracker.&lt;name&gt;, so two rules with one name cannot both survive
        /// a save. Give a new rule a unique name right away instead of adding a second row that would
        /// overwrite the first one, or hit the duplicate key when the save collects the old rules.
        /// </summary>
        private string GetUniqueIssueTrackerName(string name)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in IssueTrackers)
                names.Add(rule.Name);
            foreach (var rule in _repo.IssueTrackers)
                names.Add(rule.Name);

            if (!names.Contains(name))
                return name;

            var index = 2;
            while (names.Contains($"{name} ({index})"))
                index++;

            return $"{name} ({index})";
        }

        public void RemoveIssueTracker()
        {
            if (_selectedIssueTracker is { } rule)
                IssueTrackers.Remove(rule);

            SelectedIssueTracker = null;
        }

        public void AddNewCustomAction()
        {
            SelectedCustomAction = _repo.Settings.AddNewCustomAction();
        }

        public void RemoveSelectedCustomAction()
        {
            _repo.Settings.RemoveCustomAction(_selectedCustomAction);
            SelectedCustomAction = null;
        }

        public void MoveSelectedCustomActionUp()
        {
            if (_selectedCustomAction != null)
                _repo.Settings.MoveCustomActionUp(_selectedCustomAction);
        }

        public void MoveSelectedCustomActionDown()
        {
            if (_selectedCustomAction != null)
                _repo.Settings.MoveCustomActionDown(_selectedCustomAction);
        }

        public async Task SaveAsync()
        {
            _repo.Settings.Save();

            await SetIfChangedAsync("user.name", UserName, "");
            await SetIfChangedAsync("user.email", UserEmail, "");
            await SetIfChangedAsync("commit.gpgsign", GPGCommitSigningEnabled ? "true" : "false", "false");
            await SetIfChangedAsync("tag.gpgsign", GPGTagSigningEnabled ? "true" : "false", "false");
            await SetIfChangedAsync("user.signingkey", GPGUserSigningKey, "");
            await SetIfChangedAsync("http.proxy", HttpProxy, "");
            await SetIfChangedAsync("fetch.prune", EnablePruneOnFetch ? "true" : "false", "false");

            await ApplyIssueTrackerChangesAsync();
        }

        private async Task SetIfChangedAsync(string key, string value, string defValue)
        {
            if (value != _cached.GetValueOrDefault(key, defValue))
                await new Commands.Config(_repo.FullPath).SetAsync(key, value);
        }

        private async Task ApplyIssueTrackerChangesAsync()
        {
            var changed = false;
            var oldRules = new Dictionary<string, Models.IssueTracker>();
            foreach (var rule in _repo.IssueTrackers)
            {
                // One name may exist both in the shared store and in the local config; keep the last one
                // rather than throwing on the duplicate key and aborting the whole save.
                oldRules[rule.Name] = rule;
            }

            foreach (var rule in IssueTrackers)
            {
                if (oldRules.TryGetValue(rule.Name, out var old))
                {
                    if (old.IsShared != rule.IsShared)
                    {
                        changed = true;
                        await new Commands.IssueTracker(_repo.FullPath, old.IsShared).RemoveAsync(old.Name);
                        await new Commands.IssueTracker(_repo.FullPath, rule.IsShared).AddAsync(rule);
                    }
                    else
                    {
                        if (!old.RegexString.Equals(rule.RegexString, StringComparison.Ordinal))
                        {
                            changed = true;
                            await new Commands.IssueTracker(_repo.FullPath, old.IsShared).UpdateRegexAsync(rule);
                        }

                        if (!old.URLTemplate.Equals(rule.URLTemplate, StringComparison.Ordinal))
                        {
                            changed = true;
                            await new Commands.IssueTracker(_repo.FullPath, old.IsShared).UpdateURLTemplateAsync(rule);
                        }
                    }

                    oldRules.Remove(rule.Name);
                }
                else
                {
                    changed = true;
                    await new Commands.IssueTracker(_repo.FullPath, rule.IsShared).AddAsync(rule);
                }
            }

            if (oldRules.Count > 0)
            {
                changed = true;

                foreach (var kv in oldRules)
                    await new Commands.IssueTracker(_repo.FullPath, kv.Value.IsShared).RemoveAsync(kv.Key);
            }

            if (changed)
            {
                _repo.IssueTrackers.Clear();
                _repo.IssueTrackers.AddRange(IssueTrackers);
            }
        }

        private readonly Repository _repo;
        private readonly Dictionary<string, string> _cached;
        private bool _gpgCommitSign = false;
        private bool _gpgTagSign = false;
        private string _gpgUserSigningKey = "";
        private string _httpProxy;
        private Models.CommitTemplate _selectedCommitTemplate = null;
        private Models.IssueTracker _selectedIssueTracker = null;
        private Models.CustomAction _selectedCustomAction = null;
    }
}
