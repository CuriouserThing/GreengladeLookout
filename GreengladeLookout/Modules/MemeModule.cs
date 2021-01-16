using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.CatalogSearching;
using Bjerg.CatalogSearching.Services;
using Bjerg.Lor;
using Discord.Commands;
using Microsoft.Extensions.Options;
using TipsyOwl;
using WumpusHall;
using Version = Bjerg.Version;

namespace GreengladeLookout.Modules
{
    public class MemeModule : ModuleBase<SocketCommandContext>
    {
        public MemeModule(ICatalogService catalogService, LocaleService localeService, ISearchService searchService, IOptionsSnapshot<TipsySettings> settings)
        {
            CatalogService = catalogService;
            LocaleService = localeService;
            SearchService = searchService;
            Settings = settings.Value;
        }

        private ICatalogService CatalogService { get; }

        private LocaleService LocaleService { get; }

        private ISearchService SearchService { get; }

        private TipsySettings Settings { get; }

        private Version Version => new(Settings.LatestVersion.ToArray());

        private static string[] LookoutReplies { get; } =
        {
            "*What do these yordle eyes see?* :3",
            "*What have we here~?* :3",
            "*There, through the trees!* :o",
            "*They're here. Sound the alarm!* :o",
            "*I see you there!* >:o",
            "*They're coming...* :x",
        };

        private static bool CardIsValidChamp(ICard card, Catalog homeCatalog)
        {
            if (homeCatalog.Cards.TryGetValue(card.Code, out ICard? homeCard))
            {
                return homeCard.Collectible && homeCard.Supertype?.Name == "Champion";
            }
            else
            {
                return false;
            }
        }

        private string GetChampString(ICard champ)
        {
            string name = champ.Name ?? champ.Code;
            if (champ.Region != null && Settings.RegionIconEmotes.TryGetValue(champ.Region.Key, out ulong emoteId))
            {
                return $"<:{champ.Region.Abbreviation}:{emoteId}> {name}";
            }
            else
            {
                return name;
            }
        }

        private string GetRegionString(LorFaction region)
        {
            string name = region.Name;
            if (Settings.RegionIconEmotes.TryGetValue(region.Key, out ulong emoteId))
            {
                return $"<:{region.Abbreviation}:{emoteId}> {name}";
            }
            else
            {
                return name;
            }
        }

        private async Task Reply(Catalog catalog, ICard champA, ICard champB, Random rand)
        {
            string reply = LookoutReplies[rand.Next(0, LookoutReplies.Length)];
            string ca = GetChampString(champA);
            string cb = GetChampString(champB);

            string cc = "";
            if (champA.Region == champB.Region)
            {
                LorFaction[] regions = catalog.Regions.Values.ToArray();
                LorFaction region = regions[rand.Next(0, regions.Length)];
                cc = region == champA.Region ? " *(mono-region!!)*" : $" **+ {GetRegionString(region)}**";
            }

            string msg = new StringBuilder()
                .AppendLine($"{reply}")
                .AppendLine()
                .AppendLine($"**{ca} × {cb}**{cc}")
                .ToString();
            await ReplyAsync(msg);
        }

        [Command("champroll")]
        public async Task ChamprollAsync()
        {
            Locale locale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            Catalog catalog = await CatalogService.GetCatalog(locale, Version);
            Catalog homeCatalog = await CatalogService.GetHomeCatalog(Version);

            ICard[] champs = catalog.Cards.Values
                .Where(c => CardIsValidChamp(c, homeCatalog))
                .ToArray();

            var rand = new Random();
            int a = rand.Next(0, champs.Length);
            int b;
            do
            {
                b = rand.Next(0, champs.Length);
            } while (a == b);

            await Reply(catalog, champs[a], champs[b], rand);
        }

        [Command("champroll")]
        public async Task<RuntimeResult> ChamprollAsync(string champNameOrCode)
        {
            Locale searchLocale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var parameters = new SearchParameters(champNameOrCode, searchLocale, Version);
            TranslatedSearchResult<ICard> result = await SearchService.FindCard(parameters);

            Catalog homeCatalog = await CatalogService.GetHomeCatalog(Version);
            ItemMatch<ICard>? match = result.Matches.FirstOrDefault(m => CardIsValidChamp(m.Item, homeCatalog));

            if (match is null)
            {
                return WumpusRuntimeResult.FromError($"Couldn't find a champ from `{champNameOrCode}`.");
            }
            else
            {
                ICard champA = match.Item;
                Catalog catalog = await CatalogService.GetCatalog(searchLocale, Version);
                ICard[] champs = catalog.Cards.Values
                    .Where(c => c.Code != champA.Code && CardIsValidChamp(c, homeCatalog))
                    .ToArray();
                var rand = new Random();
                int b = rand.Next(0, champs.Length);

                await Reply(catalog, champA, champs[b], rand);
                return WumpusRuntimeResult.FromSuccess();
            }
        }
    }
}
