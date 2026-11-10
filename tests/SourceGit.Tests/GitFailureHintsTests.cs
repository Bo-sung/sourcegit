using System;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// The failure classes of <see cref="SourceGit.Models.GitFailureHints"/>, proved against the stderr
    /// a real Git produced in the localhost fixtures of Docs/ALL_AUTOMATED_AUTH_20261110.md and
    /// Docs/ALL_SSH_AUTH_20261110.md. <c>KeyFor</c> is pure, so this is CLI/classifier evidence: the
    /// desktop dialog that shows the hint is a separate, GUI-only question. The negative controls are
    /// the other half of the claim - a fix that widened the TLS match must not take over a connection,
    /// a credential or a missing-repository failure. Only loopback fixture values appear here.
    /// </summary>
    public class GitFailureHintsTests
    {
        // The untrusted-root line exactly as Git 2.56.0.windows.4 with schannel printed it against the
        // self-signed fixture certificate. The sentence after the constant is localized on a real
        // machine; the classification may not depend on it, so both forms are asserted.
        private const string RealSchannelUntrustedRoot =
            "fatal: unable to access 'https://127.0.0.1:58694/probe.git/': schannel: SEC_E_UNTRUSTED_ROOT (0x80090325) - 신뢰되지 않은 기관에서 인증서 체인을 발급했습니다.";

        private const string SchannelUntrustedRootAsciiOnly =
            "fatal: unable to access 'https://127.0.0.1:58694/probe.git/': schannel: SEC_E_UNTRUSTED_ROOT (0x80090325)";

        [Fact]
        public void ASelfSignedChainFromSchannelIsACertificateFailure()
        {
            Assert.Equal("Classic.Transfer.FailTLSCert", SourceGit.Models.GitFailureHints.KeyFor(RealSchannelUntrustedRoot));
        }

        [Fact]
        public void TheCertificateClassDoesNotDependOnTheLocalizedSentence()
        {
            Assert.Equal("Classic.Transfer.FailTLSCert", SourceGit.Models.GitFailureHints.KeyFor(SchannelUntrustedRootAsciiOnly));
            Assert.Equal("Classic.Transfer.FailTLSCert", SourceGit.Models.GitFailureHints.KeyFor(SchannelUntrustedRootAsciiOnly.ToUpperInvariant()));
        }

        [Fact]
        public void AnExpiredCertificateIsStillACertificateFailure()
        {
            const string line = "fatal: unable to access 'https://127.0.0.1:58694/probe.git/': schannel: SEC_E_CERT_EXPIRED (0x80090328)";
            Assert.Equal("Classic.Transfer.FailTLSCert", SourceGit.Models.GitFailureHints.KeyFor(line));
        }

        // The wording the other TLS backends already produced must keep working unchanged.
        [Fact]
        public void TheOpenSslCertificateWordingIsUnchanged()
        {
            Assert.Equal("Classic.Transfer.FailTLSCert",
                SourceGit.Models.GitFailureHints.KeyFor("fatal: unable to access 'https://127.0.0.1:58694/probe.git/': SSL certificate problem: unable to get local issuer certificate"));
        }

        [Fact]
        public void ACertificateHandshakeWithoutATrustProblemIsNotClaimedAsOne()
        {
            // No certificate-trust name in here, so the TLS hint must stay empty.
            Assert.DoesNotContain("FailTLSCert", KeyOrNone("fatal: unable to access 'https://127.0.0.1:58694/probe.git/': schannel: handshake failed"));
            Assert.DoesNotContain("FailTLSCert", KeyOrNone("schannel: server closed abruptly (missing close_notify)"));
        }

        [Fact]
        public void RealCredentialAndSshFailuresKeepTheirOwnClasses()
        {
            Assert.Equal("Classic.Transfer.FailCredential",
                SourceGit.Models.GitFailureHints.KeyFor("fatal: Authentication failed for 'http://127.0.0.1:58691/probe.git/'"));
            Assert.Equal("Classic.Transfer.FailSSHHostKey",
                SourceGit.Models.GitFailureHints.KeyFor("Host key verification failed."));
            Assert.Equal("Classic.Transfer.FailSSHPermission",
                SourceGit.Models.GitFailureHints.KeyFor("git@127.0.0.1: Permission denied (publickey)."));
        }

        [Fact]
        public void AConnectionThatNeverOpenedAndTheFollowUpLineAreNotCertificateFailures()
        {
            Assert.Equal("Classic.Transfer.FailConnect",
                SourceGit.Models.GitFailureHints.KeyFor("fatal: unable to access 'https://127.0.0.1:58694/probe.git/': Failed to connect to 127.0.0.1:58694 after 0 ms: Could not connect to server"));
            // The bare summary line that follows every SSH failure names no class of its own.
            Assert.Equal(string.Empty, SourceGit.Models.GitFailureHints.KeyFor("fatal: Could not read from remote repository."));
        }

        /// <summary>
        /// The narrow defect of Docs/REPOSITORY_FAILURE_FIX_20261110.md, kept as an active regression:
        /// Git writes the URL inside the sentence, <c>repository '&lt;url&gt;' not found</c>, while the classifier
        /// held only the fixed phrase <c>repository not found</c>, so a real HTTP 404 clone produced no hint.
        /// Proved with the real stderr of the localhost fixture in Docs/ALL_AUTOMATED_AUTH_20261110.md and
        /// with the wording a hosted remote prints without a URL.
        /// </summary>
        [Fact]
        public void TheRealMissingRepositoryLineIsAClassifiedRepositoryFailure()
        {
            Assert.Equal("Classic.Transfer.FailRepository",
                SourceGit.Models.GitFailureHints.KeyFor("fatal: repository 'https://127.0.0.1:58694/probe.git/' not found"));
            Assert.Equal("Classic.Transfer.FailRepository",
                SourceGit.Models.GitFailureHints.KeyFor("remote: Repository not found."));
            // The same failure seen under CRLF output (Windows spelling of the same Git message).
            Assert.Equal("Classic.Transfer.FailRepository",
                SourceGit.Models.GitFailureHints.KeyFor("Cloning into 'probe'...\r\nfatal: repository '/fixture/probe.git/' not found\r\n"));
        }

        [Fact]
        public void ACommandOrPathThatIsNotFoundIsNotAMissingRepository()
        {
            Assert.Equal("none", KeyOrNone("sh: line 1: git-lfs: command not found"));
            Assert.Equal("none", KeyOrNone("fatal: cannot open directory '/fixture/missing': No such file or directory"));
        }

        [Fact]
        public void OnlyTheQuotedRepositorySubjectFailsAsRepositoryAndNotAnotherSubjectOrReason()
        {
            // Both messages carry "repository" and "not found" on the SAME line, yet neither is Git's
            // "repository '<path or url>' not found": the missing thing is a file inside a repository,
            // and in the other case a helper. A bare conjunction of the two words would call both a
            // missing remote, so the match has to follow Git's own word order and quoted subject.
            Assert.Equal("none", KeyOrNone("fatal: file 'config' not found in repository"));
            Assert.Equal("none", KeyOrNone("fatal: helper not found for repository"));
        }

        // An empty classification is not a string an Assert.DoesNotContain can be handed directly.
        private static string KeyOrNone(string error)
        {
            var key = SourceGit.Models.GitFailureHints.KeyFor(error);
            return string.IsNullOrEmpty(key) ? "none" : key;
        }
    }
}
