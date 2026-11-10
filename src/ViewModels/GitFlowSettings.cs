using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    /// <summary>
    /// A11 - read and change the git-flow configuration of one repository.
    ///
    /// This popup only ever writes the `gitflow.*` keys it shows. It never creates, renames,
    /// deletes or checks out a branch, so changing a prefix cannot silently move anything: it
    /// only changes which branches the client treats as feature/release/hotfix. Inspection works
    /// without a git-flow dependency, because reading the configuration does not need one; only
    /// the start/finish commands are guarded by <see cref="GitFlowAvailability"/>.
    ///
    /// Two on-disk shapes exist (see <see cref="Models.GitFlow.Parse"/>): the legacy
    /// `gitflow.branch.master|develop` + `gitflow.prefix.*` form, and the `git-flow-next` form
    /// `gitflow.branch.&lt;name&gt;.type|parent|prefix|...`. Both are editable:
    ///
    /// * Legacy - one key per value, written directly.
    /// * Next - the base branches are *key maps* (`gitflow.branch.&lt;name&gt;.*` plus the `parent` and
    ///   `startpoint` back-references of the other branches). Renaming a base branch therefore
    ///   copies the whole map to the new name, repoints every reference, and only then removes the
    ///   old keys. A write that fails stops the save there and the rename is undone as far as git
    ///   lets it; whatever could not be undone is named in the failure summary rather than being
    ///   passed off as a clean save. Topic parents, merge strategies, `bugfix`/`support` entries and
    ///   every other key are left untouched.
    ///
    /// The keys are always listed, so nothing stays hidden in either shape.
    /// </summary>
    public partial class GitFlowSettings : Popup
    {
        private enum ConfigShape
        {
            None = 0,
            Legacy,
            Next,
            Unknown,
        }

        [GeneratedRegex(@"^[\w\-/\.]+$")]
        private static partial Regex REG_TAG_PREFIX();

        [Required(ErrorMessage = "Production branch name is required!!!")]
        [CustomValidation(typeof(GitFlowSettings), nameof(ValidateBaseBranch))]
        public string Production
        {
            get => _production;
            set
            {
                if (SetProperty(ref _production, value, true))
                {
                    OnPropertyChanged(nameof(ProductionImpact));
                    OnPropertyChanged(nameof(HasMissingBranch));
                    OnPropertyChanged(nameof(DevelopImpact));
                }
            }
        }

        [Required(ErrorMessage = "Development branch name is required!!!")]
        [CustomValidation(typeof(GitFlowSettings), nameof(ValidateBaseBranch))]
        public string Develop
        {
            get => _develop;
            set
            {
                if (SetProperty(ref _develop, value, true))
                {
                    OnPropertyChanged(nameof(DevelopImpact));
                    OnPropertyChanged(nameof(HasMissingBranch));
                    OnPropertyChanged(nameof(ProductionImpact));
                }
            }
        }

        [Required(ErrorMessage = "Feature prefix is required!!!")]
        [RegularExpression(@"^[\w\-\.]+/$", ErrorMessage = "Bad feature prefix format!")]
        [CustomValidation(typeof(GitFlowSettings), nameof(ValidatePrefix))]
        public string FeaturePrefix
        {
            get => _featurePrefix;
            set
            {
                if (SetProperty(ref _featurePrefix, value, true))
                    OnPropertyChanged(nameof(FeatureImpact));
            }
        }

        [Required(ErrorMessage = "Release prefix is required!!!")]
        [RegularExpression(@"^[\w\-\.]+/$", ErrorMessage = "Bad release prefix format!")]
        [CustomValidation(typeof(GitFlowSettings), nameof(ValidatePrefix))]
        public string ReleasePrefix
        {
            get => _releasePrefix;
            set
            {
                if (SetProperty(ref _releasePrefix, value, true))
                    OnPropertyChanged(nameof(ReleaseImpact));
            }
        }

        [Required(ErrorMessage = "Hotfix prefix is required!!!")]
        [RegularExpression(@"^[\w\-\.]+/$", ErrorMessage = "Bad hotfix prefix format!")]
        [CustomValidation(typeof(GitFlowSettings), nameof(ValidatePrefix))]
        public string HotfixPrefix
        {
            get => _hotfixPrefix;
            set
            {
                if (SetProperty(ref _hotfixPrefix, value, true))
                    OnPropertyChanged(nameof(HotfixImpact));
            }
        }

        [CustomValidation(typeof(GitFlowSettings), nameof(ValidateTagPrefix))]
        public string TagPrefix
        {
            get => _tagPrefix;
            set => SetProperty(ref _tagPrefix, value, true);
        }

        /// <summary>Which git-flow dependency was detected for the selected Git executable.</summary>
        public string DependencyState
        {
            get;
            private set;
        }

        /// <summary>The config file a save would write, and whether other scopes also hold git-flow keys.</summary>
        public string ScopeState
        {
            get;
            private set;
        }

        /// <summary>False when the repository holds no git-flow key at all.</summary>
        public bool HasKeys
        {
            get => Keys.Count > 0;
        }

        /// <summary>Non-empty when the settings are read-only, and why.</summary>
        public string LimitationState
        {
            get;
            private set;
        }

        public bool ShowLimitation
        {
            get => !string.IsNullOrEmpty(LimitationState);
        }

        public bool ShowEditor
        {
            get => _writable && (_shape == ConfigShape.Legacy || _shape == ConfigShape.Next);
        }

        /// <summary>False when the keys only exist in a config file above this repository.</summary>
        public bool Writable
        {
            get => _writable;
        }

        /// <summary>True when the base branches are key maps whose rename rewrites references.</summary>
        public bool ShowNextNote
        {
            get => _shape == ConfigShape.Next;
        }

        /// <summary>True when the git-flow keys live only in the global config.</summary>
        public bool ShowGlobalOverrideNote
        {
            get;
            private set;
        }

        /// <summary>Parsed values, shown read-only when the editor is not available.</summary>
        public string ParsedState
        {
            get;
            private set;
        }

        /// <summary>Every `gitflow.*` key Git resolves for this repository, so nothing stays hidden.</summary>
        public List<KeyValuePair<string, string>> Keys
        {
            get;
        } = new List<KeyValuePair<string, string>>();

        public string NoBranchChangeState
        {
            get;
            private set;
        }

        public bool HasMissingBranch
        {
            get
            {
                if (!ShowEditor)
                    return false;

                return !HasLocalBranch(_production) || !HasLocalBranch(_develop);
            }
        }

        public string ProductionImpact
        {
            get => BranchImpactOf(_origProduction, _production);
        }

        public string DevelopImpact
        {
            get => BranchImpactOf(_origDevelop, _develop);
        }

        public string FeatureImpact
        {
            get => PrefixImpactOf(_origFeaturePrefix, _featurePrefix);
        }

        public string ReleaseImpact
        {
            get => PrefixImpactOf(_origReleasePrefix, _releasePrefix);
        }

        public string HotfixImpact
        {
            get => PrefixImpactOf(_origHotfixPrefix, _hotfixPrefix);
        }

        public GitFlowSettings(Repository repo)
        {
            _repo = repo;

            // The repository's own file decides what a save can change; the merged read decides what
            // is worth showing. The two differ when a file above the repository holds git-flow keys,
            // and writing those locally would only shadow them (final review N-2).
            var local = new Commands.Config(repo.FullPath).ReadLocalAll();
            var merged = new Commands.Config(repo.FullPath).ReadAll();
            var global = new Commands.Config(null).ReadAll();

            ShowGlobalOverrideNote = CountGitFlowKeys(global) > 0;

            foreach (var kv in CollectGitFlowKeys(merged))
                Keys.Add(kv);
            Keys.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

            _shape = DetectShape(merged);
            _writable = Keys.Count == 0 || CountGitFlowKeys(local) > 0;

            DependencyState = Native.OS.GitFlowVersion switch
            {
                Models.GitFlowVersion.Next => App.Text("GitFlow.Settings.DepNext"),
                Models.GitFlowVersion.Legacy => App.Text("GitFlow.Settings.DepLegacy"),
                _ => App.Text("GitFlow.Settings.DepUnknown"),
            };

            ScopeState = string.Format(App.Text("GitFlow.Settings.Scope"), CountGitFlowKeys(local), CountGitFlowKeys(global));

            if (!_writable)
            {
                // Keys inherited from a user or system file are listed, but this window writes the
                // repository's own file only: values written there would be shadowed by the inherited
                // ones, and a half-overridden map is not something a rollback can undo.
                LimitationState = App.Text("GitFlow.Settings.ReadOnlyInherited");
            }
            else if (_shape == ConfigShape.Unknown)
            {
                LimitationState = App.Text("GitFlow.Settings.ReadOnlyUnknown");
            }
            else if (_shape == ConfigShape.None)
            {
                // Keys can exist without this client being able to say who owns them: no dependency
                // detected and no `gitflow.initialized=true`. Saying "there are no keys" in that state
                // contradicts the key list shown below it (integration review E-4).
                LimitationState = Keys.Count > 0
                    ? App.Text("GitFlow.Settings.ReadOnlyUnclassified")
                    : App.Text("GitFlow.Settings.ReadOnlyNone");
            }

            var flow = new Models.GitFlow();
            flow.Parse(local);

            // Models.GitFlow.Parse only reads the git-flow-next form while the git-flow-next
            // dependency is detected. The keys themselves are enough to show and edit what is
            // stored, so a next-shaped repository is readable and writable even when the tool
            // is missing; the write path is <see cref="SaveNextAsync"/>.
            if (_shape == ConfigShape.Next)
            {
                ParseNext(local, flow);
                CollectNextNames(local);
            }

            ParsedState = string.Format(
                App.Text("GitFlow.Settings.Parsed"),
                string.IsNullOrEmpty(flow.ProductionBranch) ? "-" : flow.ProductionBranch,
                string.IsNullOrEmpty(flow.DevelopmentBranch) ? "-" : flow.DevelopmentBranch,
                string.IsNullOrEmpty(flow.FeaturePrefix) ? "-" : flow.FeaturePrefix,
                string.IsNullOrEmpty(flow.ReleasePrefix) ? "-" : flow.ReleasePrefix,
                string.IsNullOrEmpty(flow.HotfixPrefix) ? "-" : flow.HotfixPrefix);

            NoBranchChangeState = App.Text("GitFlow.Settings.NoBranchChange");

            _production = flow.ProductionBranch;
            _develop = flow.DevelopmentBranch;
            _featurePrefix = flow.FeaturePrefix;
            _releasePrefix = flow.ReleasePrefix;
            _hotfixPrefix = flow.HotfixPrefix;
            _tagPrefix = local.TryGetValue("gitflow.prefix.versiontag", out var tag) ? tag : string.Empty;
            if (_shape == ConfigShape.Next)
                _tagPrefix = NextTagPrefix(local);
            _origTag = _tagPrefix;

            _origProduction = _production;
            _origDevelop = _develop;
            _origFeaturePrefix = _featurePrefix;
            _origReleasePrefix = _releasePrefix;
            _origHotfixPrefix = _hotfixPrefix;
        }

        public static ValidationResult ValidateBaseBranch(string name, ValidationContext ctx)
        {
            if (ctx.ObjectInstance is GitFlowSettings editor)
            {
                if (!Models.RefName.IsValidBranchName(name))
                    return new ValidationResult("Bad branch name format!");

                if (editor._production.Equals(editor._develop, StringComparison.Ordinal))
                    return new ValidationResult("Develop branch has the same name with production branch!");

                // In the next shape a branch is a key map (`gitflow.branch.<name>.*`) that other
                // entries reference by name, so a name another entry already owns would collide
                // instead of renaming anything. The name the edited field itself stores is not a
                // collision - otherwise no save of an unchanged field could ever pass validation.
                if (editor._shape == ConfigShape.Next && editor._nextNames.Contains(name))
                {
                    var own = ctx.MemberName == nameof(Develop) ? editor._origDevelop : editor._origProduction;
                    if (!EqualsText(name, own))
                        return new ValidationResult("Another git-flow branch already uses that name!");
                }
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidatePrefix(string prefix, ValidationContext ctx)
        {
            if (ctx.ObjectInstance is GitFlowSettings editor)
            {
                // Repository.GetGitFlowType tests the prefixes in this order, so two equal prefixes
                // would silently misclassify branches instead of failing.
                var count = 0;
                if (editor._featurePrefix.Equals(prefix, StringComparison.Ordinal)) count++;
                if (editor._releasePrefix.Equals(prefix, StringComparison.Ordinal)) count++;
                if (editor._hotfixPrefix.Equals(prefix, StringComparison.Ordinal)) count++;
                if (count > 1)
                    return new ValidationResult("Feature, release and hotfix prefixes must be different!");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateTagPrefix(string tagPrefix, ValidationContext ctx)
        {
            if (!string.IsNullOrWhiteSpace(tagPrefix) && !REG_TAG_PREFIX().IsMatch(tagPrefix))
                return new ValidationResult("Bad tag prefix format!");

            return ValidationResult.Success;
        }

        public override async Task<bool> Sure()
        {
            // Unrecognized shapes must not be able to write anything, whatever the fields hold.
            if (_shape is not (ConfigShape.Legacy or ConfigShape.Next))
                return true;

            if (!HasChanges())
                return true;

            // One save, one notification: the batch reports its own summary below, so the commands
            // it runs must not raise a notification per key.
            var batch = new KeyBatch(_repo.FullPath);

            // The save plans from the repository's own config file, because that is the only file it
            // can write and undo in. Keys shown here may come from a file above the repository, and
            // writing them locally would leave values the inherited ones still win over - so a
            // configuration that exists only up there is reported instead of shadowed, and nothing
            // is written.
            var localSnapshot = await batch.ReadLocalAsync();
            if (batch.ReadFailed)
            {
                _repo.SendNotification(batch.BuildFailureMessage(), true);
                return false;
            }

            if (Keys.Count > 0 && CountGitFlowKeys(localSnapshot) == 0)
            {
                batch.FailInherited("gitflow.*");
                _repo.SendNotification(batch.BuildFailureMessage(), true);
                return false;
            }

            // The same reasoning one step further: an entry whose keys are split between this file and
            // a file above must not be renamed either, because only the local half would move and the
            // half above would keep naming the old branch. Refused here, before anything is written.
            if (!await SureSplitsAreCleanAsync(batch, localSnapshot))
            {
                _repo.SendNotification(batch.BuildFailureMessage(), true);
                return false;
            }

            // No ConfigureAwait(false) anywhere in this method: the tail reads the locale dictionary
            // and refreshes the set that the UI thread's validation reads, so it must stay on the UI
            // thread (the same rule GitFailureHints follows with Dispatcher.UIThread). The awaited
            // writes themselves still run on the thread pool.
            if (_shape == ConfigShape.Next)
                await SaveNextAsync(batch);
            else
                await SaveLegacyAsync(batch);

            if (batch.HasFailure)
            {
                _repo.SendNotification(batch.BuildFailureMessage(), true);
                return false;
            }

            // Update the in-memory model the same way the init popup does, so the branch tree
            // grouping follows the saved prefixes without any branch operation.
            _repo.GitFlow.ProductionBranch = _production;
            _repo.GitFlow.DevelopmentBranch = _develop;
            _repo.GitFlow.FeaturePrefix = _featurePrefix;
            _repo.GitFlow.ReleasePrefix = _releasePrefix;
            _repo.GitFlow.HotfixPrefix = _hotfixPrefix;

            _origProduction = _production;
            _origDevelop = _develop;
            _origFeaturePrefix = _featurePrefix;
            _origReleasePrefix = _releasePrefix;
            _origHotfixPrefix = _hotfixPrefix;
            _origTag = _tagPrefix;

            if (_shape == ConfigShape.Next)
                CollectNextNames(await batch.ReadMergedAsync());

            _repo.SendNotification(App.Text("GitFlow.Settings.Saved"));
            return true;
        }

        /// <summary>
        /// Names the git-flow entries that are split across configuration files - some keys in this
        /// repository's own file, the rest in a file above it. Renaming such an entry, or rewriting the
        /// tag prefix of a branch that is only partly here, would move half of an entry and leave the
        /// other half naming what no longer exists, which no rollback can repair. A split that the save
        /// does not touch is left alone: nothing about that entry changes.
        /// </summary>
        private async Task<bool> SureSplitsAreCleanAsync(KeyBatch batch, Dictionary<string, string> local)
        {
            var merged = await batch.ReadMergedAsync().ConfigureAwait(false);
            var split = new List<string>();

            void CheckEntry(string prefix, bool touched)
            {
                if (!touched)
                    return;

                var here = 0;
                var above = 0;
                foreach (var kv in merged)
                {
                    if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (local.ContainsKey(kv.Key))
                        here++;
                    else
                        above++;
                }

                if (here > 0 && above > 0)
                    split.Add(prefix + "*");
            }

            // Only the entries this save actually renames are checked, and only their own keys: an
            // inherited key of some other branch is none of this save's business.
            if (!string.IsNullOrEmpty(_origProduction))
                CheckEntry("gitflow.branch." + _origProduction + ".", !EqualsText(_origProduction, _production));

            if (!string.IsNullOrEmpty(_origDevelop))
                CheckEntry("gitflow.branch." + _origDevelop + ".", !EqualsText(_origDevelop, _develop));

            if (split.Count == 0)
                return true;

            foreach (var key in split)
                batch.FailInherited(key);

            return false;
        }

        /// <summary>Legacy shape: one flat key per edited value.</summary>
        private async Task SaveLegacyAsync(KeyBatch batch)
        {
            await batch.WriteAsync("gitflow.branch.master", _origProduction, _production).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.branch.develop", _origDevelop, _develop).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.prefix.feature", _origFeaturePrefix, _featurePrefix).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.prefix.release", _origReleasePrefix, _releasePrefix).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.prefix.hotfix", _origHotfixPrefix, _hotfixPrefix).ConfigureAwait(false);

            // Empty means "no version tag prefix", which git-flow stores as an empty value (InitAsync
            // writes it with allowEmpty), so keep the same convention instead of unsetting it.
            if (!EqualsText(_origTag, _tagPrefix))
                await batch.SetAsync("gitflow.prefix.versiontag", _tagPrefix, true).ConfigureAwait(false);
        }

        /// <summary>
        /// git-flow-next shape. A branch is a key map (`gitflow.branch.&lt;name&gt;.*`) that other
        /// entries point at through `parent`/`startpoint`, so renaming a base branch copies the
        /// whole map, repoints every reference, and only then drops the old keys. The batch stops at
        /// the first key it cannot write and a rename that did not finish is undone as far as git
        /// lets it, so a save never leaves a half-renamed entry behind without saying so. Two guards
        /// keep that true for a whole save, not just for one rename: a rename never starts once an
        /// earlier write failed, and renaming both base branches in one save re-reads the keys in
        /// between so the second copy cannot carry a `parent` that the first rename has already
        /// moved. Prefixes are single keys, and the tag prefix maps onto `tagprefix` of every topic
        /// branch that tags. Everything else (`bugfix`, `support`, strategies, `autoupdate`,
        /// `worktree`, ...) is left untouched.
        /// </summary>
        private async Task SaveNextAsync(KeyBatch batch)
        {
            var local = await batch.ReadLocalAsync().ConfigureAwait(false);
            if (batch.ReadFailed)
                return;

            if (!EqualsText(_origProduction, _production))
                await RenameNextBranchAsync(batch, local, _origProduction, _production).ConfigureAwait(false);

            if (!EqualsText(_origDevelop, _develop))
            {
                // The second rename has to see what the first one wrote. The first rename repoints
                // every `parent`/`startpoint` that named the old production branch; copying from the
                // snapshot taken before it would write the pre-rename `parent` into the new develop
                // key map and then delete the reference the first rename had just updated, leaving a
                // dangling name. Re-reading only when a rename actually happened keeps the common
                // (prefix-only) save at a single read.
                if (!EqualsText(_origProduction, _production))
                {
                    local = await batch.ReadLocalAsync().ConfigureAwait(false);
                    if (batch.ReadFailed)
                        return;
                }

                await RenameNextBranchAsync(batch, local, _origDevelop, _develop).ConfigureAwait(false);
            }

            await batch.WriteAsync("gitflow.branch.feature.prefix", _origFeaturePrefix, _featurePrefix).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.branch.release.prefix", _origReleasePrefix, _releasePrefix).ConfigureAwait(false);
            await batch.WriteAsync("gitflow.branch.hotfix.prefix", _origHotfixPrefix, _hotfixPrefix).ConfigureAwait(false);

            if (EqualsText(_origTag, _tagPrefix) || batch.HasFailure)
                return;

            foreach (var kv in local)
            {
                if (!kv.Key.StartsWith("gitflow.branch.", StringComparison.OrdinalIgnoreCase) ||
                    !kv.Key.EndsWith(".tag", StringComparison.OrdinalIgnoreCase) ||
                    !kv.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var name = NextBranchNameOf(kv.Key, ".tag");
                if (name.Equals(_origProduction, StringComparison.Ordinal) || name.Equals(_origDevelop, StringComparison.Ordinal))
                    continue;

                // An absent `tagprefix` is how git-flow-next stores "tag without a prefix", so an
                // empty field has to unset the key instead of writing an empty value.
                await batch.SetAsync("gitflow.branch." + name + ".tagprefix", _tagPrefix).ConfigureAwait(false);

                if (batch.HasFailure)
                    break;
            }
        }

        /// <summary>Renames one `gitflow.branch.&lt;name&gt;` entry together with its whole key map.</summary>
        private static async Task RenameNextBranchAsync(KeyBatch batch, Dictionary<string, string> local, string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || EqualsText(from, to))
                return;

            // Never start a rename once something in this save failed: the copy would be followed by
            // a skipped reference update and a skipped delete, which leaves the old and the new key
            // map both present (a half rename). Same rule as the guards inside this method.
            if (batch.HasFailure)
                return;

            var oldPrefix = "gitflow.branch." + from + ".";
            var oldKeys = new List<KeyValuePair<string, string>>();
            foreach (var kv in local)
            {
                if (kv.Key.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    oldKeys.Add(kv);
            }

            if (oldKeys.Count == 0)
            {
                // Nothing to copy under this name. Either the entry is gone, or it is only read from a
                // config file above this repository: the write path reads the repository's own file
                // (`config --local -l`), so an inherited-only entry cannot be copied, repointed or
                // rolled back here - and writing a new map locally would leave a shadow that the
                // inherited keys still win over. Both cases are reported instead of half-renaming.
                if (await batch.HasMergedKeysUnderAsync(oldPrefix).ConfigureAwait(false))
                    batch.FailInherited(oldPrefix.TrimEnd('.'));
                else
                    batch.Fail(oldPrefix.TrimEnd('.'));
                return;
            }

            // Everything the rename may change, remembered as it was read, so a rename that cannot
            // finish can be put back: the keys it creates are dropped and the keys it alters are
            // written back to the value they held.
            var restore = new List<KeyValuePair<string, string>>(oldKeys);
            var created = new List<string>();
            foreach (var kv in local)
            {
                if ((kv.Key.EndsWith(".parent", StringComparison.OrdinalIgnoreCase) || kv.Key.EndsWith(".startpoint", StringComparison.OrdinalIgnoreCase)) &&
                    kv.Value.Equals(from, StringComparison.Ordinal))
                {
                    restore.Add(kv);
                }
            }

            // 1. copy the entry.
            foreach (var kv in oldKeys)
            {
                var key = "gitflow.branch." + to + kv.Key.Substring(oldPrefix.Length - 1);

                // A destination key that already held a value is overwritten, not created, so the
                // undo has to write that value back. Deleting it instead would drop a setting this
                // save never made, and such a key can exist: the collision check only knows names
                // that own a key map, so a single key left under the destination name (`autoupdate`,
                // a strategy, ...) is not caught by validation. A destination that holds *several*
                // values cannot be overwritten at all - git refuses a single-value write over them -
                // so an overwrite that succeeded always had exactly one value to restore.
                var overwrote = local.TryGetValue(key, out var previous);
                if (await batch.SetAsync(key, kv.Value, true).ConfigureAwait(false))
                {
                    if (overwrote)
                        restore.Add(new KeyValuePair<string, string>(key, previous));
                    else
                        created.Add(key);

                    continue;
                }

                await RollbackRenameAsync(batch, restore, created).ConfigureAwait(false);
                return;
            }

            // 2. repoint the references the rest of the tree holds to this entry.
            foreach (var kv in local)
            {
                if ((!kv.Key.EndsWith(".parent", StringComparison.OrdinalIgnoreCase) && !kv.Key.EndsWith(".startpoint", StringComparison.OrdinalIgnoreCase)) ||
                    !kv.Value.Equals(from, StringComparison.Ordinal))
                {
                    continue;
                }

                if (await batch.SetAsync(kv.Key, to, true).ConfigureAwait(false))
                    continue;

                await RollbackRenameAsync(batch, restore, created).ConfigureAwait(false);
                return;
            }

            // 3. drop the old entry last, which keeps the settings valid if a write failed.
            foreach (var kv in oldKeys)
            {
                if (await batch.SetAsync(kv.Key, string.Empty).ConfigureAwait(false))
                    continue;

                await RollbackRenameAsync(batch, restore, created).ConfigureAwait(false);
                return;
            }
        }

        /// <summary>
        /// Undo a rename that could not finish: the keys it had created are dropped and every key it
        /// had altered is written back to the value it held before, which returns the stored map to
        /// what the popup read. A key that cannot be restored is listed in the summary the popup
        /// shows: claiming a clean undo on a write that failed would be a false success.
        /// </summary>
        private static async Task RollbackRenameAsync(KeyBatch batch, List<KeyValuePair<string, string>> restore, List<string> created)
        {
            // The map has to be put back before the references to it are moved back, otherwise a
            // reader that looks in between finds a name with no entry under it.
            foreach (var kv in restore)
            {
                if (kv.Value.Length == 0)
                {
                    if (!await batch.RestoreAsync(kv.Key, string.Empty).ConfigureAwait(false)) batch.NoteLeftover(kv.Key);
                }
                else if (!await batch.RestoreAsync(kv.Key, kv.Value).ConfigureAwait(false))
                {
                    batch.NoteLeftover(kv.Key);
                }
            }

            foreach (var key in created)
            {
                if (!await batch.RemoveAsync(key).ConfigureAwait(false)) batch.NoteLeftover(key);
            }
        }

        /// <summary>
        /// The line the failure summary prints for one key a rollback could not undo: a command that
        /// drops exactly that key and nothing else. The key goes in single quotes with every `'`
        /// closed and re-opened, because a ref name may hold `$` or a backtick and this line is meant
        /// to be pasted into a shell - a line that let the shell expand the key would run something
        /// other than what it shows. Nothing in the client runs it; the popup only reports it.
        /// </summary>
        public static string CleanupLine(string key)
        {
            return "git config --local --unset-all '" + key.Replace("'", "'\\''") + "'";
        }

        private static bool EqualsText(string a, string b)
        {
            return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);
        }

        private bool HasChanges()
        {
            return !EqualsText(_origProduction, _production) ||
                !EqualsText(_origDevelop, _develop) ||
                !EqualsText(_origFeaturePrefix, _featurePrefix) ||
                !EqualsText(_origReleasePrefix, _releasePrefix) ||
                !EqualsText(_origHotfixPrefix, _hotfixPrefix) ||
                !EqualsText(_origTag, _tagPrefix);
        }

        private static int CountGitFlowKeys(Dictionary<string, string> config)
        {
            var count = 0;
            foreach (var kv in config)
            {
                if (kv.Key.StartsWith("gitflow.", StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        private static List<KeyValuePair<string, string>> CollectGitFlowKeys(Dictionary<string, string> config)
        {
            var rs = new List<KeyValuePair<string, string>>();
            foreach (var kv in config)
            {
                if (kv.Key.StartsWith("gitflow.", StringComparison.OrdinalIgnoreCase))
                    rs.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            }

            return rs;
        }

        /// <summary>
        /// Detects the stored shape with the same conditions Models.GitFlow.Parse applies, so the
        /// editor never edits keys that the parser would ignore.
        /// </summary>
        private static ConfigShape DetectShape(Dictionary<string, string> config)
        {
            if (!config.TryGetValue("gitflow.initialized", out var initialized) ||
                !initialized.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                var hasLegacy = config.ContainsKey("gitflow.branch.master") || config.ContainsKey("gitflow.prefix.feature");
                if (!hasLegacy && Native.OS.GitFlowVersion == Models.GitFlowVersion.Next && HasNextBaseBranch(config))
                    return ConfigShape.Next;

                return hasLegacy ? ConfigShape.Legacy : ConfigShape.None;
            }

            if (HasNextBaseBranch(config))
                return ConfigShape.Next;

            return config.ContainsKey("gitflow.branch.master") || config.ContainsKey("gitflow.prefix.feature")
                ? ConfigShape.Legacy
                : ConfigShape.Unknown;
        }

        /// <summary>
        /// Reads the git-flow-next form (gitflow.branch.&lt;name&gt;.type|parent|prefix) straight from the
        /// stored keys. A base branch with a parent is the development branch, the one without it is
        /// the production branch, exactly like Models.GitFlow.Parse, and the names are the real key
        /// names, which is what the rename path in <see cref="RenameNextBranchAsync"/> needs.
        /// </summary>
        private static void ParseNext(Dictionary<string, string> config, Models.GitFlow flow)
        {
            foreach (var kv in config)
            {
                if (!kv.Key.StartsWith("gitflow.branch.", StringComparison.Ordinal))
                    continue;

                if (kv.Key.EndsWith(".type", StringComparison.Ordinal) && kv.Value.Equals("base", StringComparison.Ordinal))
                {
                    var name = kv.Key.Substring("gitflow.branch.".Length, kv.Key.Length - "gitflow.branch.".Length - ".type".Length);
                    if (config.ContainsKey($"gitflow.branch.{name}.parent"))
                        flow.DevelopmentBranch = name;
                    else
                        flow.ProductionBranch = name;
                }
                else if (kv.Key.EndsWith(".prefix", StringComparison.Ordinal))
                {
                    var type = kv.Key.Substring("gitflow.branch.".Length, kv.Key.Length - "gitflow.branch.".Length - ".prefix".Length);
                    if (type.Equals("feature", StringComparison.Ordinal))
                        flow.FeaturePrefix = kv.Value;
                    else if (type.Equals("release", StringComparison.Ordinal))
                        flow.ReleasePrefix = kv.Value;
                    else if (type.Equals("hotfix", StringComparison.Ordinal))
                        flow.HotfixPrefix = kv.Value;
                }
            }
        }

        private static string NextBranchNameOf(string key, string suffix)
        {
            const string prefix = "gitflow.branch.";
            return key.Substring(prefix.Length, key.Length - prefix.Length - suffix.Length);
        }

        /// <summary>Collects the branch names that already own a key map, for collision checks.</summary>
        private void CollectNextNames(Dictionary<string, string> config)
        {
            _nextNames.Clear();

            foreach (var kv in config)
            {
                if (kv.Key.StartsWith("gitflow.branch.", StringComparison.OrdinalIgnoreCase) &&
                    (kv.Key.EndsWith(".type", StringComparison.OrdinalIgnoreCase) || kv.Key.EndsWith(".prefix", StringComparison.OrdinalIgnoreCase)))
                {
                    _nextNames.Add(NextBranchNameOf(kv.Key, kv.Key.EndsWith(".type", StringComparison.OrdinalIgnoreCase) ? ".type" : ".prefix"));
                }
            }
        }

        /// <summary>The tag prefix is stored per tagging branch; they share one value in practice.</summary>
        private static string NextTagPrefix(Dictionary<string, string> config)
        {
            foreach (var kv in config)
            {
                if (kv.Key.StartsWith("gitflow.branch.", StringComparison.OrdinalIgnoreCase) && kv.Key.EndsWith(".tagprefix", StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return string.Empty;
        }

        private static bool HasNextBaseBranch(Dictionary<string, string> config)
        {
            foreach (var kv in config)
            {
                if (kv.Key.StartsWith("gitflow.branch.", StringComparison.Ordinal) &&
                    kv.Key.EndsWith(".type", StringComparison.Ordinal) &&
                    kv.Value.Equals("base", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasLocalBranch(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (var b in _repo.Branches)
            {
                if (b.IsLocal && b.Name.Equals(name, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private int CountBranches(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return 0;

            var count = 0;
            foreach (var b in _repo.Branches)
            {
                if (b.IsLocal && b.Name.StartsWith(prefix, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private string BranchImpactOf(string from, string to)
        {
            var toText = HasLocalBranch(to)
                ? App.Text("GitFlow.Settings.BranchExists")
                : App.Text("GitFlow.Settings.BranchMissing");

            return $"{ImpactArrow(from, to)} · {toText}";
        }

        private string PrefixImpactOf(string from, string to)
        {
            return $"{ImpactArrow(from, to)} \u00b7 {App.Text("GitFlow.Settings.MatchCount")} {CountBranches(from)} \u2192 {CountBranches(to)}";
        }

        private static string ImpactArrow(string from, string to)
        {
            if (EqualsText(from, to))
                return string.IsNullOrEmpty(from) ? "-" : from;

            return $"{(string.IsNullOrEmpty(from) ? "-" : from)} \u2192 {(string.IsNullOrEmpty(to) ? "-" : to)}";
        }

        private readonly Repository _repo;
        private ConfigShape _shape = ConfigShape.None;
        private bool _writable = true;
        private readonly HashSet<string> _nextNames = new(StringComparer.Ordinal);

        /// <summary>
        /// The keys one save of this popup writes. The commands run with `RaiseError` off, so a save
        /// that fails on several keys gives one summary at the end instead of one notification per
        /// key, and the git text of the first failure is kept with it as the evidence. Once a write
        /// failed the batch stops answering writes; what to undo is the caller's call.
        ///
        /// Two reads exist and they are not interchangeable. <see cref="ReadLocalAsync"/> is the
        /// repository's own config file (`config --local -l`) and is what a save plans from: writes,
        /// unsets and the rollback values all address that file only, so a decision is never taken
        /// from a value that this popup could not undo. <see cref="ReadMergedAsync"/> is what git
        /// resolves for the repository (system + global + local) and is used to *show* keys and to
        /// name the branches a rename would collide with, where seeing the inherited values is the
        /// point. A save never writes a global or system file: the commands are built with the
        /// repository path, which selects `--local`.
        /// </summary>
        private sealed class KeyBatch
        {
            private readonly Commands.Config _config;
            private readonly List<string> _failed = new();
            private readonly List<string> _leftover = new();
            private readonly List<string> _inherited = new();
            private readonly List<string> _errors = new();
            private int _errorKept = 0;
            private bool _readFailed = false;

            public KeyBatch(string repository)
            {
                _config = new Commands.Config(repository);
                _config.RaiseError = false;
                _config.ErrorReceiver = CollectError;
            }

            public bool HasFailure => _failed.Count > 0 || _leftover.Count > 0 || _inherited.Count > 0 || _readFailed;

            /// <summary>True when the snapshot a save plans from could not be read at all.</summary>
            public bool ReadFailed => _readFailed;

            /// <summary>The repository's own config file - the file every write of this batch lands in.</summary>
            public async Task<Dictionary<string, string>> ReadLocalAsync()
            {
                // A failed read has to stop the save: without a snapshot there is nothing to tell the
                // save what it overwrites, and writing on that guess is how a setting gets lost. The
                // read reports itself through the same error receiver a failed write uses, so the
                // count around it is what says "the snapshot was never read".
                var before = _errorTotal;
                var snapshot = await _config.ReadLocalAllAsync().ConfigureAwait(false);
                if (_errorTotal > before)
                    _readFailed = true;

                return snapshot;
            }

            /// <summary>Everything git resolves for this repository, for display and collision names.</summary>
            public Task<Dictionary<string, string>> ReadMergedAsync() => _config.ReadAllAsync();

            /// <summary>
            /// Whether a key prefix exists in the merged view while the local file has nothing under
            /// it, which is what an inherited-only (global/system) git-flow entry looks like from here.
            /// </summary>
            public async Task<bool> HasMergedKeysUnderAsync(string prefix)
            {
                foreach (var kv in await _config.ReadAllAsync().ConfigureAwait(false))
                {
                    if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            public async Task<bool> SetAsync(string key, string value, bool allowEmpty = false)
            {
                if (HasFailure)
                    return false;

                if (await _config.SetAsync(key, value, allowEmpty).ConfigureAwait(false))
                    return true;

                _failed.Add(key);
                return false;
            }

            /// <summary>
            /// A key that only needs writing when the field it stands for changed. Once the save has
            /// failed this ends without writing *and* without naming the key: listing a key that was
            /// never attempted would tell the user something was wrong with that key, which is not
            /// true - the summary already names the key that failed.
            /// </summary>
            public async Task<bool> WriteAsync(string key, string from, string to)
            {
                if (string.Equals(from ?? string.Empty, to ?? string.Empty, StringComparison.Ordinal))
                    return true;

                if (HasFailure)
                    return false;

                return await SetAsync(key, to).ConfigureAwait(false);
            }

            /// <summary>A key this save cannot write at all, for a reason git did not report.</summary>
            public void Fail(string key) => _failed.Add(key);

            /// <summary>
            /// A key that lives in a config file above this repository. This popup writes the local
            /// file only, so such a key is reported as out of reach instead of being shadowed locally.
            /// </summary>
            public void FailInherited(string key)
            {
                foreach (var x in _inherited)
                {
                    if (x.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                _inherited.Add(key);
            }

            /// <summary>Put a key back to a remembered value; the failure is a leftover, not a failed write.</summary>
            public Task<bool> RestoreAsync(string key, string value) => _config.SetAsync(key, value, !string.IsNullOrEmpty(value));

            /// <summary>Drop a key this save created; the failure is a leftover, not a failed write.</summary>
            public Task<bool> RemoveAsync(string key) => _config.SetAsync(key, string.Empty);

            public void NoteLeftover(string key)
            {
                foreach (var x in _leftover)
                {
                    if (x.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                _leftover.Add(key);
            }

            public string BuildFailureMessage()
            {
                if (_readFailed)
                    return string.Format(App.Text("GitFlow.Settings.SaveReadFailed"), LastError());

                var msg = string.Format(App.Text("GitFlow.Settings.SaveFailed"), string.Join(", ", _failed));

                if (_inherited.Count > 0)
                    msg += "\n" + string.Format(App.Text("GitFlow.Settings.SaveInherited"), string.Join(", ", _inherited));

                if (_leftover.Count > 0)
                    msg += "\n" + string.Format(App.Text("GitFlow.Settings.SaveLeftover"), string.Join(", ", _leftover), CleanupCommands(_leftover));

                if (_errorKept > 0)
                    msg += "\n\n" + string.Format(App.Text("GitFlow.Settings.SaveError"), _errors[0]);

                return msg;
            }

            /// <summary>One runnable line per leftover key, see <see cref="CleanupLine"/>.</summary>
            private static string CleanupCommands(List<string> keys)
            {
                var lines = new List<string>(keys.Count);
                foreach (var key in keys)
                    lines.Add(CleanupLine(key));

                return string.Join("\n", lines);
            }

            private int _errorTotal = 0;

            private string LastError()
            {
                return _errorKept > 0 ? _errors[0] : App.Text("GitFlow.Settings.SaveErrorUnknown");
            }

            private void CollectError(string error)
            {
                _errorTotal++;

                if (_errorKept >= 3)
                    return;

                _errorKept++;

                // Only the first failure is quoted, and only its first line: the batch reports one
                // reason, and a local path is all a key write can name (no endpoint, no credential).
                foreach (var line in error.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    _errors.Add(line.Trim());
                    break;
                }
            }
        }


        private string _production = string.Empty;
        private string _develop = string.Empty;
        private string _featurePrefix = string.Empty;
        private string _releasePrefix = string.Empty;
        private string _hotfixPrefix = string.Empty;
        private string _tagPrefix = string.Empty;

        private string _origProduction = string.Empty;
        private string _origDevelop = string.Empty;
        private string _origFeaturePrefix = string.Empty;
        private string _origReleasePrefix = string.Empty;
        private string _origHotfixPrefix = string.Empty;
        private string _origTag = string.Empty;
    }
}
