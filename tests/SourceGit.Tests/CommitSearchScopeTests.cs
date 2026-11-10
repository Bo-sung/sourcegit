using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Headless.XUnit;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// A02 commit search scopes. The product's own SearchCommitContext/QueryCommits commands run
    /// against a synthetic repository and the hits are compared with independent git queries.
    /// Headless/logic evidence, separate from the desktop GUI evidence.
    /// </summary>
    public class CommitSearchScopeTests : IDisposable
    {
        private readonly GitRepositoryFixture _fixture;
        private readonly ViewModels.Repository _repo;
        private readonly ViewModels.Histories _histories;
        private readonly ViewModels.SearchCommitContext _search;

        private readonly string _shaAlpha;
        private readonly string _shaBeta;
        private readonly string _shaFeature;

        public CommitSearchScopeTests()
        {
            _fixture = GitRepositoryFixture.Create("search");

            _shaAlpha = _fixture.Commit("src/a.cs", "alpha\nNEEDLE_ONE\n", "alpha initial", "Alice <alice@sourcegit.invalid>");
            _fixture.NewBranch("feature");
            _fixture.Checkout("feature");
            _shaFeature = _fixture.Commit("docs/b.md", "zeta\n", "zeta feature only", "Bob <bob@sourcegit.invalid>");
            _fixture.Checkout("main");
            _shaBeta = _fixture.Commit("docs/c.md", "gamma\n", "beta alpha second", "Bob <bob@sourcegit.invalid>");

            _repo = TestBootstrap.OpenRepository(_fixture.Root, _fixture.GitDir);
            _histories = new ViewModels.Histories(_repo);
            _search = _histories.SearchCommitContext;
            _histories.IsSearchingCommits = true;
        }

        public void Dispose()
        {
            _histories.IsSearchingCommits = false;
            _fixture.Dispose();
        }

        [AvaloniaFact]
        public void ByMessage_MatchesSingleWord()
        {
            var hits = Search(Models.CommitSearchMethod.ByMessage, "alpha");
            Assert.Contains(_shaAlpha, hits);
            Assert.Contains(_shaBeta, hits);
            Assert.DoesNotContain(_shaFeature, hits);
        }

        [AvaloniaFact]
        public void ByMessage_RequiresAllWords()
        {
            var hits = Search(Models.CommitSearchMethod.ByMessage, "beta alpha");
            Assert.Equal(new[] { _shaBeta }, hits);
        }

        [AvaloniaFact]
        public void ByAuthor_IsCaseInsensitiveAndCoversAllBranchesByDefault()
        {
            // The default scope is every local/remote branch, so Bob's commit on 'feature' counts too.
            var hits = Search(Models.CommitSearchMethod.ByAuthor, "BOB");
            Assert.Contains(_shaBeta, hits);
            Assert.Contains(_shaFeature, hits);
            Assert.DoesNotContain(_shaAlpha, hits);

            _search.OnlySearchCurrentBranch = true;
            Assert.Equal(new[] { _shaBeta }, Search(Models.CommitSearchMethod.ByAuthor, "BOB"));
        }

        [AvaloniaFact]
        public void ByPath_EqualsGitPathHistory()
        {
            var hits = Search(Models.CommitSearchMethod.ByPath, "src/a.cs");
            Assert.Equal(new[] { _shaAlpha }, hits);

            var reference = _fixture.RunOutput("log", "--format=%H", "--all", "--", "src/a.cs")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Equal(reference.OrderBy(x => x), hits.OrderBy(x => x));
        }

        [AvaloniaFact]
        public void ByContent_FindsTheCommitThatIntroducedTheLine()
        {
            var hits = Search(Models.CommitSearchMethod.ByContent, "NEEDLE_ONE");
            Assert.Equal(new[] { _shaAlpha }, hits);
        }

        [AvaloniaFact]
        public void BySha_OnlyAcceptsARealCommit()
        {
            var hits = Search(Models.CommitSearchMethod.BySHA, _shaAlpha);
            Assert.Equal(new[] { _shaAlpha }, hits);

            Assert.Empty(Search(Models.CommitSearchMethod.BySHA, "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"));
        }

        [AvaloniaFact]
        public void CurrentBranchOnly_ScopeChangesReachableBranches()
        {
            _search.OnlySearchCurrentBranch = true;
            Assert.Empty(Search(Models.CommitSearchMethod.ByMessage, "zeta"));

            // The repository-wide scope searches all local and remote branches, so the commit that
            // lives only on 'feature' becomes reachable for the search.
            _search.OnlySearchCurrentBranch = false;
            Assert.Equal(new[] { _shaFeature }, Search(Models.CommitSearchMethod.ByMessage, "zeta"));
        }

        [AvaloniaFact]
        public void CurrentBranchOnly_MarksMergedFlagOfHits()
        {
            _search.OnlySearchCurrentBranch = true;
            Assert.All(SearchContext(Models.CommitSearchMethod.ByMessage, "alpha"), c => Assert.True(c.IsMerged));

            _search.OnlySearchCurrentBranch = false;
            var merged = SearchContext(Models.CommitSearchMethod.ByMessage, "zeta");
            Assert.False(merged[0].IsMerged);
        }

        [AvaloniaFact]
        public void EmptyFilter_StartsNoQuery()
        {
            _search.Method = (int)Models.CommitSearchMethod.ByMessage;
            _search.Filter = "alpha";
            _search.StartSearch();
            Assert.True(Pump.Until(() => _search.Results != null && !_search.IsQuerying), "the first search did not finish");

            _search.ClearFilter();
            Assert.Null(_search.Results);
            Assert.False(_search.IsQuerying);
        }

        [AvaloniaFact]
        public void SwitchingScopeCancelsThePreviousResult()
        {
            Assert.NotEmpty(Search(Models.CommitSearchMethod.ByMessage, "alpha"));
            Assert.Empty(Search(Models.CommitSearchMethod.ByPath, "nothing/here.txt"));
        }

        private List<string> Search(Models.CommitSearchMethod method, string filter)
        {
            return SearchContext(method, filter).Select(x => x.SHA).ToList();
        }

        private List<Models.Commit> SearchContext(Models.CommitSearchMethod method, string filter)
        {
            _search.Method = (int)method;
            _search.Filter = filter;
            _search.StartSearch();

            if (!Pump.Until(() => _search.Results != null && !_search.IsQuerying))
                throw new TimeoutException($"the commit search did not finish (method={method}, filter={filter})");

            return _search.Results ?? new List<Models.Commit>();
        }
    }
}
