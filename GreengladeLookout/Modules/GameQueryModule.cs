using System.Linq;
using System.Threading.Tasks;
using Bjerg;
using Discord.Commands;
using Microsoft.Extensions.Options;
using TipsyOwl;

namespace GreengladeLookout.Modules
{
    public class GameQueryModule : ModuleBase<SocketCommandContext>
    {
        public GameQueryModule(ICatalogService catalogService, CardEmbedFactory cardEmbedFactory, KeywordEmbedFactory keywordEmbedFactory, DeckEmbedFactory deckEmbedFactory, IOptionsSnapshot<TipsySettings> tipsySettings, IOptionsSnapshot<GreengladeSettings> greengladeSettings)
        {
            CatalogService = catalogService;
            CardEmbedFactory = cardEmbedFactory;
            KeywordEmbedFactory = keywordEmbedFactory;
            DeckEmbedFactory = deckEmbedFactory;
            TipsySettings = tipsySettings.Value;
            GreengladeSettings = greengladeSettings.Value;
        }

        private ICatalogService CatalogService { get; }

        private CardEmbedFactory CardEmbedFactory { get; }

        private KeywordEmbedFactory KeywordEmbedFactory { get; }

        private DeckEmbedFactory DeckEmbedFactory { get; }

        private TipsySettings TipsySettings { get; }

        private GreengladeSettings GreengladeSettings { get; }

        private Version Version => new Version(TipsySettings.LatestVersion.ToArray());

        [Command("search")]
        public async Task<RuntimeResult> SearchAsync([Remainder] string query)
        {
            var handler = new GameQueryHandler(CatalogService, Version, CardEmbedFactory, KeywordEmbedFactory, DeckEmbedFactory, Context.Channel)
            {
                SearchDeckByCode = true,
                SearchCardByCode = true,
                SearchCardsByName = true,
                SearchKeywordsByName = true,
                StringMatchThreshold = GreengladeSettings.StringMatchThreshold,
                SubstringBookendWeight = GreengladeSettings.SubstringBookendWeight,
                SubstringBookendTaper = GreengladeSettings.SubstringBookendTaper,
                UncollectibleCardDownscaleFactor = GreengladeSettings.UncollectibleCardDownscaleFactor,
                GlobalKeywordDownscaleFactor = GreengladeSettings.GlobalKeywordDownscaleFactor
            };

            return await handler.HandleQueryAsync(query);
        }

        [Command("keyword")]
        public async Task<RuntimeResult> KeywordAsync([Remainder] string name)
        {
            var handler = new GameQueryHandler(CatalogService, Version, CardEmbedFactory, KeywordEmbedFactory, DeckEmbedFactory, Context.Channel)
            {
                SearchKeywordsByName = true,
                StringMatchThreshold = GreengladeSettings.StringMatchThreshold,
                SubstringBookendWeight = GreengladeSettings.SubstringBookendWeight,
                SubstringBookendTaper = GreengladeSettings.SubstringBookendTaper
            };

            return await handler.HandleQueryAsync(name);
        }

        [Command("card")]
        public async Task<RuntimeResult> CardAsync([Remainder] string name)
        {
            var handler = new GameQueryHandler(CatalogService, Version, CardEmbedFactory, KeywordEmbedFactory, DeckEmbedFactory, Context.Channel)
            {
                SearchCardByCode = true,
                SearchCardsByName = true,
                StringMatchThreshold = GreengladeSettings.StringMatchThreshold,
                SubstringBookendWeight = GreengladeSettings.SubstringBookendWeight,
                SubstringBookendTaper = GreengladeSettings.SubstringBookendTaper,
                UncollectibleCardDownscaleFactor = GreengladeSettings.UncollectibleCardDownscaleFactor
            };

            return await handler.HandleQueryAsync(name);
        }

        [Command("deck")]
        public async Task<RuntimeResult> DeckAsync(string code)
        {
            var handler = new GameQueryHandler(CatalogService, Version, CardEmbedFactory, KeywordEmbedFactory, DeckEmbedFactory, Context.Channel) {SearchDeckByCode = true};

            return await handler.HandleQueryAsync(code);
        }
    }
}
