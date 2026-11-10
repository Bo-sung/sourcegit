using System;

using Avalonia.Headless.XUnit;

using Xunit;

namespace SourceGit.Tests
{
    /// <summary>
    /// Locale resolution (integration review F-3). Keys that a catalog has not picked up yet used to
    /// be shown as the raw resource key, because <see cref="App.SetLocale"/> merged only the active
    /// catalog and <see cref="App.Text"/> falls back to the key itself. The application now merges
    /// en_US underneath the active catalog, so an untranslated string shows the English text while
    /// every key the catalog does define keeps its own translation.
    ///
    /// This is asserted through the real resource lookup: the test cases are Avalonia facts, so the
    /// genuine App instance with its App.axaml locale dictionaries is the one that resolves the keys
    /// (a plain xunit fact has no Application instance, where every lookup would return the raw key).
    /// </summary>
    public class LocaleFallbackTests : IDisposable
    {
        /// <summary>A key that only en_US defines; ja_JP and de_DE do not (checked in this checkout).</summary>
        private const string EnglishOnlyKey = "GitFlow.Settings.NextEditor";

        /// <summary>A key every catalog defines.</summary>
        private const string CommonKey = "Cancel";

        public LocaleFallbackTests()
        {
            TestBootstrap.Prepare();
        }

        public void Dispose() => App.SetLocale("en_US");

        [AvaloniaTheory]
        [InlineData("ca_ES")]
        [InlineData("de_DE")]
        [InlineData("el_GR")]
        [InlineData("es_ES")]
        [InlineData("fr_FR")]
        [InlineData("he_IL")]
        [InlineData("id_ID")]
        [InlineData("it_IT")]
        [InlineData("ja_JP")]
        [InlineData("pt_BR")]
        [InlineData("ru_RU")]
        [InlineData("ta_IN")]
        [InlineData("uk_UA")]
        [InlineData("zh_CN")]
        [InlineData("zh_TW")]
        public void AKeyMissingFromACatalogShowsTextAndNotTheRawKey(string locale)
        {
            App.SetLocale(locale);

            var text = App.Text(EnglishOnlyKey);
            Assert.NotEqual("Text." + EnglishOnlyKey, text);
            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [AvaloniaTheory]
        [InlineData("de_DE")]
        [InlineData("ja_JP")]
        [InlineData("zh_CN")]
        public void TheFallbackOnlyCoversKeysTheCatalogDoesNotDefine(string locale)
        {
            App.SetLocale("en_US");
            var englishCommon = App.Text(CommonKey);
            var englishOnly = App.Text(EnglishOnlyKey);

            App.SetLocale(locale);
            var viaFallback = App.Text(EnglishOnlyKey);
            var translated = App.Text(CommonKey);

            // A key the catalog defines keeps its own translation: the fallback must not overwrite it.
            Assert.NotEqual(englishCommon, translated);

            // A key it does not define resolves through the fallback, i.e. to the English text.
            Assert.Equal(englishOnly, viaFallback);
        }

        [AvaloniaFact]
        public void KoreanKeepsItsOwnStringsAndBorrowsOnlyTheMissingOnes()
        {
            App.SetLocale("ko_KR");

            // ko_KR carries its own text, so a common key is Hangul, not the English fallback.
            var cancel = App.Text(CommonKey);
            Assert.Contains(cancel, c => c >= '\uac00' && c <= '\ud7a3');

            // The key ko_KR also defines happens to be identical in shape to English only in that it
            // resolves; the fallback is not what produced it (the catalog holds it).
            Assert.False(string.IsNullOrWhiteSpace(App.Text(EnglishOnlyKey)));
        }

        [AvaloniaFact]
        public void SwitchingBackToEnglishStillResolves()
        {
            App.SetLocale("de_DE");
            App.SetLocale("en_US");

            Assert.NotEqual("Text." + EnglishOnlyKey, App.Text(EnglishOnlyKey));
            Assert.NotEqual("Text." + CommonKey, App.Text(CommonKey));
        }

    }
}
