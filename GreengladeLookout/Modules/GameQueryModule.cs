using System.Linq;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.CatalogSearching.Services;
using Bjerg.Lor;
using Discord.Commands;
using GreengladeLookout.ViewBuilding;
using Microsoft.Extensions.Options;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout.Modules
{
    public class GameQueryModule : ModuleBase<SocketCommandContext>
    {
        public GameQueryModule(LocaleService localeService, ISearchService searchService, CardboardSearchViewBuilder cardboardSearchViewBuilder, CardFlavorSearchViewBuilder cardFlavorSearchViewBuilder, CardRelationSearchViewBuilder cardRelationSearchViewBuilder, KeywordSearchViewBuilder keywordSearchViewBuilder, DeckSearchViewBuilder deckSearchViewBuilder, AnythingSearchViewBuilder anythingSearchViewBuilder, IOptionsSnapshot<TipsySettings> tipsySettings)
        {
            LocaleService = localeService;
            SearchService = searchService;
            CardboardSearchViewBuilder = cardboardSearchViewBuilder;
            CardFlavorSearchViewBuilder = cardFlavorSearchViewBuilder;
            CardRelationSearchViewBuilder = cardRelationSearchViewBuilder;
            KeywordSearchViewBuilder = keywordSearchViewBuilder;
            DeckSearchViewBuilder = deckSearchViewBuilder;
            AnythingSearchViewBuilder = anythingSearchViewBuilder;
            TipsySettings = tipsySettings.Value;
        }

        private LocaleService LocaleService { get; }

        private ISearchService SearchService { get; }

        private CardboardSearchViewBuilder CardboardSearchViewBuilder { get; }

        private CardFlavorSearchViewBuilder CardFlavorSearchViewBuilder { get; }

        private CardRelationSearchViewBuilder CardRelationSearchViewBuilder { get; }

        private KeywordSearchViewBuilder KeywordSearchViewBuilder { get; }

        private DeckSearchViewBuilder DeckSearchViewBuilder { get; }

        private AnythingSearchViewBuilder AnythingSearchViewBuilder { get; }

        private TipsySettings TipsySettings { get; }

        private Version Version => new(TipsySettings.LatestVersion.ToArray());

        [Command("search")]
        public async Task SearchAsync([Remainder] string query)
        {
            Locale searchLocale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var parameters = new SearchParameters(query, searchLocale, Version);
            TranslatedSearchResult<CatalogItemUnion> result = await SearchService.FindAnything(parameters);

            MessageView view = await AnythingSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }

        [Command("keyword")]
        public async Task KeywordAsync([Remainder] string name)
        {
            Locale searchLocale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var parameters = new SearchParameters(name, searchLocale, Version);
            TranslatedSearchResult<LorKeyword> result = await SearchService.FindKeyword(parameters);

            MessageView view = await KeywordSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }

        private async Task<TranslatedSearchResult<ICard>> FindCard(string nameOrCode)
        {
            Locale searchLocale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var parameters = new SearchParameters(nameOrCode, searchLocale, Version);
            return await SearchService.FindCard(parameters);
        }

        [Command("card")]
        public async Task CardAsync([Remainder] string nameOrCode)
        {
            TranslatedSearchResult<ICard> result = await FindCard(nameOrCode);

            MessageView view = await CardboardSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }

        [Command("flavor")]
        public async Task FlavorAsync([Remainder] string nameOrCode)
        {
            TranslatedSearchResult<ICard> result = await FindCard(nameOrCode);

            MessageView view = await CardFlavorSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }

        [Command("related")]
        public async Task RelatedAsync([Remainder] string nameOrCode)
        {
            TranslatedSearchResult<ICard> result = await FindCard(nameOrCode);

            MessageView view = await CardRelationSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }

        [Command("deck")]
        public async Task DeckAsync(string code)
        {
            Locale searchLocale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var parameters = new SearchParameters(code, searchLocale, Version);
            TranslatedSearchResult<Deck> result = await SearchService.FindDeck(parameters);

            MessageView view = await DeckSearchViewBuilder.BuildView(result);
            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }
        }
    }
}
