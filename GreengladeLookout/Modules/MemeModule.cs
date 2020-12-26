using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bjerg;
using Discord.Commands;
using Microsoft.Extensions.Options;
using TipsyOwl;
using Version = Bjerg.Version;

namespace GreengladeLookout.Modules
{
    public class MemeModule : ModuleBase<SocketCommandContext>
    {
        public MemeModule(ICatalogService catalogService, LocaleService localeService, IOptionsSnapshot<TipsySettings> settings)
        {
            CatalogService = catalogService;
            LocaleService = localeService;
            Settings = settings.Value;
        }

        private ICatalogService CatalogService { get; }

        private LocaleService LocaleService { get; }

        private TipsySettings Settings { get; }

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

        [Command("champroll")]
        public async Task<RuntimeResult> ChamprollAsync()
        {
            Locale locale = await LocaleService.GetGuildLocaleAsync(Context.Guild);
            var version = new Version(Settings.LatestVersion.ToArray());

            Catalog? catalog = await CatalogService.GetCatalog(locale, version);
            if (catalog is null)
            {
                return TipsyRuntimeResult.FromError("Couldn't get list of cards.");
            }

            Catalog? homeCatalog = await CatalogService.GetHomeCatalog(version);
            if (homeCatalog is null)
            {
                return TipsyRuntimeResult.FromError("Couldn't get list of cards.");
            }

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

            string reply = LookoutReplies[rand.Next(0, LookoutReplies.Length)];
            string ca = GetChampString(champs[a]);
            string cb = GetChampString(champs[b]);

            var sb = new StringBuilder();
            _ = sb.AppendLine($"{reply}");
            _ = sb.AppendLine();
            _ = sb.AppendLine($"**{ca} × {cb}**");

            _ = ReplyAsync(sb.ToString());

            return TipsyRuntimeResult.FromSuccess();
        }
    }
}
