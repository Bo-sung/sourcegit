using System.Collections.Generic;

namespace SourceGit.Models
{
    /// <summary>
    /// Describes how local changes are handled after a stash is created. The
    /// texts are locale keys resolved at bind time, so the list stays correct
    /// after a language switch without rebuilding it.
    /// </summary>
    public class DealWithChangesAfterStashing(string labelKey, string descKey)
    {
        public string Label => App.Text(labelKey);

        public string Desc => App.Text(descKey);

        public static readonly List<DealWithChangesAfterStashing> Supported = [
            new ("Stash.Mode.Discard", "Stash.Mode.Discard.Desc"),
            new ("Stash.Mode.KeepIndex", "Stash.Mode.KeepIndex.Desc"),
            new ("Stash.Mode.KeepAll", "Stash.Mode.KeepAll.Desc"),
        ];
    }
}
