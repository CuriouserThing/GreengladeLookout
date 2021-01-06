using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.CatalogSearching;
using Bjerg.Lor;
using Discord;
using Discord.Commands;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout
{
    public class GameQueryHandler
    {
        public GameQueryHandler(ICatalogService catalogService, Version version, Locale locale, CardEmbedFactory cardEmbedFactory, KeywordEmbedFactory keywordEmbedFactory, DeckEmbedFactory deckEmbedFactory, IMessageChannel channel)
        {
            CatalogService = catalogService;
            Version = version;
            Locale = locale;
            CardEmbedFactory = cardEmbedFactory;
            KeywordEmbedFactory = keywordEmbedFactory;
            DeckEmbedFactory = deckEmbedFactory;
            Channel = channel;
        }

        private ICatalogService CatalogService { get; }

        private Version Version { get; }

        private Locale Locale { get; }

        private CardEmbedFactory CardEmbedFactory { get; }

        private KeywordEmbedFactory KeywordEmbedFactory { get; }

        private DeckEmbedFactory DeckEmbedFactory { get; }

        private IMessageChannel Channel { get; }

        public async Task<RuntimeResult> HandleQueryAsync(string query)
        {
            Catalog? catalog = await CatalogService.GetCatalog(Locale, Version);
            if (catalog is null)
            {
                return WumpusRuntimeResult.FromError("Couldn't get list of cards.");
            }

            Catalog? homeCatalog = await CatalogService.GetHomeCatalog(Version);
            if (homeCatalog is null)
            {
                return WumpusRuntimeResult.FromError("Couldn't get list of cards.");
            }

            string lookup = query.Trim();

            var stringMatcherFactory = new StringMatcherFactory(s => new LevenshteinSubstringMatcher(s, SubstringBookendWeight, SubstringBookendTaper));

            var cardSearcher = new CatalogItemSearcher<ICard>(
                    catalog,
                    new CardNameGrouper(),
                    stringMatcherFactory,
                    new BasicCardSelector(catalog, homeCatalog),
                    new UncollectibleCardMatchDownscaler(UncollectibleCardDownscaleFactor, GlobalCardDownscaleFactor) { PreserveStrongMatches = true })
                { MatchThreshold = StringMatchThreshold };

            var keywordSearcher = new CatalogItemSearcher<LorKeyword>(
                    catalog,
                    new KeywordNameGrouper { IncludeVocabTerms = true },
                    stringMatcherFactory,
                    new BasicKeywordSelector(),
                    new GlobalItemMatchDownscaler<LorKeyword>(GlobalKeywordDownscaleFactor) { PreserveStrongMatches = true })
                { MatchThreshold = StringMatchThreshold };

            var omniSearcher = new OmniSearcher(homeCatalog, catalog, cardSearcher, keywordSearcher, DeckEmbedFactory, CardEmbedFactory, KeywordEmbedFactory)
            {
                SearchDeckByCode = SearchDeckByCode,
                SearchCardByCode = SearchCardByCode,
                SearchCardsByName = SearchCardsByName,
                SearchKeywordsByName = SearchKeywordsByName,
            };

            IReadOnlyList<IEmbeddable> result = omniSearcher.Search(lookup);

            if (result.Count == 0)
            {
                _ = await Channel.SendMessageAsync($"No results for `{lookup}`.");
                return WumpusRuntimeResult.FromSuccess();
            }

            string? didYouMean = null;
            if (result.Count > 1)
            {
                IEnumerable<string?> names = result.Skip(1).Select(c => $"**{c.Name}**");
                string namesOutput = string.Join(" | ", names);
                didYouMean = $"Did you mean: {namesOutput}";
                if (didYouMean.Length > 2048)
                {
                    string s = result.Count == 2 ? "" : "s";
                    didYouMean = $"*{result.Count - 1} other result{s}*";
                }
            }

            IReadOnlyList<Embed> embeds = result[0].GetAllEmbeds();

            if (didYouMean is null)
            {
                // Just send all embeds
                foreach (Embed embed in embeds)
                {
                    _ = await Channel.SendMessageAsync(embed: embed);
                }
            }
            else if (embeds.Count == 1)
            {
                // Send the text reply in the same message as the single embed
                _ = await Channel.SendMessageAsync(didYouMean, embed: embeds[0]);
            }
            else
            {
                // Send the text reply separately from the multiple embeds
                _ = await Channel.SendMessageAsync(didYouMean);
                foreach (Embed embed in embeds)
                {
                    _ = await Channel.SendMessageAsync(embed: embed);
                }
            }

            return WumpusRuntimeResult.FromSuccess();
        }

        #region Config

        public bool SearchDeckByCode { get; init; } = false;

        public bool SearchCardByCode { get; init; } = false;

        public bool SearchCardsByName { get; init; } = false;

        public bool SearchKeywordsByName { get; init; } = false;

        public float StringMatchThreshold { get; init; } = 0.5f;

        public float SubstringBookendWeight { get; init; } = 1f;

        public float SubstringBookendTaper { get; init; } = 1f;

        public float UncollectibleCardDownscaleFactor { get; init; } = 1f;

        public float GlobalCardDownscaleFactor { get; init; } = 1f;

        public float GlobalKeywordDownscaleFactor { get; init; } = 1f;

        #endregion
    }
}
