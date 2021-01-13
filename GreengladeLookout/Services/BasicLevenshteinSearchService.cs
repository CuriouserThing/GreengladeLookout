using System.Collections.Generic;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.CatalogSearching;
using Bjerg.CatalogSearching.Services;
using Bjerg.Lor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GreengladeLookout.Services
{
    public class BasicLevenshteinSearchService : ISearchService
    {
        public BasicLevenshteinSearchService(ICatalogService catalogService, IOptionsSnapshot<GreengladeSettings> greengladeSettings, ILogger<BasicLevenshteinSearchService> logger)
        {
            CatalogService = catalogService;
            GreengladeSettings = greengladeSettings.Value;
            Logger = logger;
        }

        private ICatalogService CatalogService { get; }

        private GreengladeSettings GreengladeSettings { get; }

        private ILogger Logger { get; }

        public async Task<TranslatedSearchResult<ICard>> FindCard(SearchParameters parameters)
        {
            string sTerm = parameters.SearchTerm;
            Locale sLoc = parameters.SearchLocale;
            Locale? tLoc = parameters.TranslationLocale;
            Version ver = parameters.Version;

            Catalog sCat = await CatalogService.GetCatalog(sLoc, ver);
            Catalog? tCat = tLoc is null ? null : await CatalogService.GetCatalog(tLoc, ver);

            if (TryFindCardFromCode(sTerm, tCat ?? sCat, out TranslatedSearchResult<ICard>? codeResult))
            {
                return codeResult!;
            }

            var cardSearcher = new CatalogItemSearcher<ICard>
            (
                sCat,
                new CardNameGrouper()
            )
            {
                TermTargetCanonicalizer = new TrimmedAndLowerCanonicalizer(),
                TermMatcherFactory = new LevenshteinSubstringMatcher.Factory(GreengladeSettings.SubstringBookendWeight, GreengladeSettings.SubstringBookendTaper),
                KeyStrengthThreshold = GreengladeSettings.StringMatchThreshold,
                KeyConflictResolution = KeyConflictResolution.Flattened,
                MatchGroupResolver = new BasicCardGroupResolver(),
                ItemStrengthDownscalers = new[] { new UncollectibleCardStrengthDownscaler(GreengladeSettings.UncollectibleCardDownscaleFactor) },
                ItemDownscaleCurve = ItemDownscaleCurve.Biased,
                ItemMatchSorter = new DescendingMatchStrengthSorter<ICard>(),
            };

            SearchResult<ICard> result = cardSearcher.Search(sTerm);
            if (tCat is null)
            {
                return TranslatedSearchResult<ICard>.FromUntranslatedSearch(result);
            }

            var tMap = new Dictionary<ICard, ICard>();
            foreach (var match in result.Matches)
            {
                ICard sItem = match.Item;
                if (tCat.Cards.TryGetValue(sItem.Code, out var tItem))
                {
                    tMap.Add(sItem, tItem);
                }
                else
                {
                    LogCouldNotTranslateWarning(sItem, sLoc, tCat.Locale, ver);
                }
            }

            return TranslatedSearchResult<ICard>.FromTranslatedSearch(result, tCat.Locale, tMap);
        }

        public async Task<TranslatedSearchResult<LorKeyword>> FindKeyword(SearchParameters parameters)
        {
            string sTerm = parameters.SearchTerm;
            Locale sLoc = parameters.SearchLocale;
            Locale? tLoc = parameters.TranslationLocale;
            Version ver = parameters.Version;

            Catalog sCat = await CatalogService.GetCatalog(sLoc, ver);
            Catalog? tCat = tLoc is null ? null : await CatalogService.GetCatalog(tLoc, ver);

            var keywordSearcher = new CatalogItemSearcher<LorKeyword>
            (
                sCat,
                new KeywordNameGrouper { IncludeVocabTerms = true, IncludeDescriptionlessKeywords = false }
            )
            {
                TermTargetCanonicalizer = new TrimmedAndLowerCanonicalizer(),
                TermMatcherFactory = new LevenshteinSubstringMatcher.Factory(GreengladeSettings.SubstringBookendWeight, GreengladeSettings.SubstringBookendTaper),
                KeyStrengthThreshold = GreengladeSettings.StringMatchThreshold,
                ItemMatchSorter = new DescendingMatchStrengthSorter<LorKeyword>(),
            };

            SearchResult<LorKeyword> result = keywordSearcher.Search(sTerm);
            if (tCat is null)
            {
                return TranslatedSearchResult<LorKeyword>.FromUntranslatedSearch(result);
            }

            var tMap = new Dictionary<LorKeyword, LorKeyword>();
            foreach (var match in result.Matches)
            {
                LorKeyword sItem = match.Item;
                if (tCat.Keywords.TryGetValue(sItem.Key, out var tItem))
                {
                    tMap.Add(sItem, tItem);
                }
                else if (tCat.VocabTerms.TryGetValue(sItem.Key, out var vtItem))
                {
                    tItem = new LorKeyword(sItem.Key, vtItem.Name, vtItem.Description);
                    tMap.Add(sItem, tItem);
                }
                else
                {
                    LogCouldNotTranslateWarning(sItem, sLoc, tCat.Locale, ver);
                }
            }

            return TranslatedSearchResult<LorKeyword>.FromTranslatedSearch(result, tCat.Locale, tMap);
        }

        public async Task<TranslatedSearchResult<Deck>> FindDeck(SearchParameters parameters)
        {
            string term = parameters.SearchTerm;
            Locale loc = parameters.TranslationLocale ?? parameters.SearchLocale;
            Version ver = parameters.Version;
            Catalog cat = await CatalogService.GetCatalog(loc, ver);

            if (TryFindDeckFromCode(term, cat, out TranslatedSearchResult<Deck>? result))
            {
                return result!;
            }
            else
            {
                return TranslatedSearchResult<Deck>.FromUntranslatedSearch(new SearchResult<Deck>(term, loc, ver));
            }
        }

        public async Task<TranslatedSearchResult<CatalogItemUnion>> FindAnything(SearchParameters parameters)
        {
            return TranslatedSearchResult<CatalogItemUnion>.MergeSearchResults
            (
                await FindCard(parameters),
                await FindKeyword(parameters),
                await FindDeck(parameters),
                new DescendingMatchStrengthSorter<CatalogItemUnion>()
            );
        }

        private static bool TryFindCardFromCode(string term, Catalog catalog, out TranslatedSearchResult<ICard>? result)
        {
            if (catalog.Cards.TryGetValue(term, out ICard? card))
            {
                var usr = SearchResult<ICard>.FromSingleItem(term, catalog.Locale, catalog.Version, card);
                result = TranslatedSearchResult<ICard>.FromUntranslatedSearch(usr);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static bool TryFindDeckFromCode(string term, Catalog catalog, out TranslatedSearchResult<Deck>? result)
        {
            if (Deck.TryFromCode(term, catalog, out Deck? deck))
            {
                var usr = SearchResult<Deck>.FromSingleItem(term, catalog.Locale, catalog.Version, deck!);
                result = TranslatedSearchResult<Deck>.FromUntranslatedSearch(usr);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private void LogCouldNotTranslateWarning<T>(T item, Locale locale, Locale translationLocale, Version version)
        {
            Logger.LogWarning($"Couldn't translate {item} from {locale} to {translationLocale}, using version {version}.");
        }
    }
}
