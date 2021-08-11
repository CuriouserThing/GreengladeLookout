using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.CatalogSearching;
using Bjerg.CatalogSearching.Services;
using Bjerg.DeckCoding;
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
        public MemeModule(ICatalogService catalogService, LocaleService localeService, ISearchService searchService, DeckViewBuilder deckViewBuilder, IOptionsSnapshot<TipsySettings> settings)
        {
            CatalogService  = catalogService;
            LocaleService   = localeService;
            SearchService   = searchService;
            DeckViewBuilder = deckViewBuilder;
            Settings        = settings.Value;
        }

        private ICatalogService CatalogService { get; }

        private LocaleService LocaleService { get; }

        private ISearchService SearchService { get; }

        private DeckViewBuilder DeckViewBuilder { get; }

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

        [Command("deckroll")]
        public async Task<RuntimeResult> DeckrollAsync()
        {
            return await DeckrollAsync(40);
        }

        [Command("deckroll")]
        public async Task<RuntimeResult> DeckrollAsync(int count)
        {
            count = Math.Min(120, Math.Abs(count));

            var rand = new Random();

            Locale locale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            Catalog catalog = await CatalogService.GetCatalog(locale, Version);

            List<LorFaction> regions = catalog.Regions.Values.ToList();

            int ra = rand.Next(0, regions.Count);
            LorFaction regionA = regions[ra];
            regions.RemoveAt(ra);
            LorFaction regionB = regions[rand.Next(0, regions.Count)];

            IGrouping<bool, ICard>[] groups = catalog.Cards.Values
                .Where(c => c.Collectible)
                .Where(c => c.Region?.Index == regionA.Index || c.Region?.Index == regionB.Index)
                .GroupBy(c => c.Rarity?.Key == "Champion")
                .ToArray();
            var champs = groups.Single(g => g.Key).ToList();
            var nonChamps = groups.Single(g => !g.Key).ToList();

            var ccs = new List<CardAndCount>();
            var rccs = new List<RawCardAndCount>();

            void AddCards(int numberOfCopies, List<ICard> source)
            {
                var n = 0;
                while (n < numberOfCopies && source.Count > 0)
                {
                    int c = rand.NextDouble() switch
                    {
                        < 0.75 => 3,
                        < 0.90 => 2,
                        _      => 1,
                    };
                    c =  Math.Min(numberOfCopies - n, c);
                    n += c;

                    var index = (int)(rand.NextDouble() * source.Count);

                    ICard card = source[index];
                    ccs.Add(new CardAndCount(card, c));
                    rccs.Add(new RawCardAndCount(card.Code.Set, card.Region!.Index, card.Code.Number, c));

                    source.RemoveAt(index);
                }
            }

            int champCount = count * 6 / 40;
            AddCards(champCount,         champs);
            AddCards(count - champCount, nonChamps);

            string code = Coding.GetCodeFromDeckCards(rccs);
            var deck = new Deck(code, locale, Version, ccs);

            MessageView view = await DeckViewBuilder.BuildView(deck);

            string reply = LookoutReplies[rand.Next(0, LookoutReplies.Length)] + "\n" + code;
            await ReplyAsync(reply);

            foreach (var info in view.Messages)
            {
                await ReplyAsync(info.Text, embed: info.Embed);
            }

            return WumpusRuntimeResult.FromSuccess();
        }
    }
}
