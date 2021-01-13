using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bjerg;
using Bjerg.Lor;
using Microsoft.Extensions.Options;
using TipsyOwl;

namespace GreengladeLookout.ViewBuilding
{
    public abstract class CardSearchViewBuilder : SearchViewBuilder<ICard>
    {
        protected CardSearchViewBuilder(ICatalogService catalogService, IOptionsSnapshot<TipsySettings> settings)
        {
            CatalogService = catalogService;
            Settings = settings.Value;
        }

        private ICatalogService CatalogService { get; }

        private TipsySettings Settings { get; }

        public override string GetItemName(ICard item)
        {
            string cardName = item.Name ?? "Unknown Card";

            if (!item.Collectible) { return cardName; }

            string regionKey, regionAbbr;
            LorFaction? region = item.Region;
            if (region is null)
            {
                regionKey = "All";
                regionAbbr = "x"; // use a dummy char for the emote name
            }
            else
            {
                regionKey = region.Key;
                regionAbbr = region.Abbreviation; // use two-letter faction code for the emote name
            }

            return Settings.RegionIconEmotes.TryGetValue(regionKey, out ulong regionEmote)
                ? $"<:{regionAbbr}:{regionEmote}> {cardName}"
                : $"{cardName}";
        }

        protected async Task<IEnumerable<ICard>> ExpandIfChamp(ICard card)
        {
            Locale loc = card.Locale;
            Version ver = card.Version;
            Catalog hCat = await CatalogService.GetHomeCatalog(ver);

            ICard homeCard = hCat.Cards[card.Code];

            // Expand the selected card if it's a lv. 1 champ
            if (card.Collectible && card.Code.TNumber == 0 && homeCard.Supertype?.Name == "Champion")
            {
                Catalog lCat = await CatalogService.GetCatalog(loc, ver);
                return ExpandChamp(card, lCat, hCat);
            }
            else
            {
                return new[] { card };
            }
        }

        private static IEnumerable<ICard> ExpandChamp(ICard card, Catalog localCatalog, Catalog homeCatalog)
        {
            // The potential lv. 2 champs for this card are cards that share its code (sans T number)
            CardCode bc = card.Code;
            ICard[] champCards = localCatalog.Cards.Values
                .Where(c =>
                           c.Code.Number == bc.Number &&
                           c.Code.TNumber != 0 &&
                           c.Code.Faction == bc.Faction &&
                           c.Code.Set == bc.Set)
                .ToArray();

            // Of these, we [currently] only recognize the lv. 2 champ as the *single* card that either:

            // - Shares its name with the lv. 1 champ (e.g. Anivia but not Eggnivia)
            ICard[] sameNames = champCards
                .Where(c => c.Name == card.Name)
                .ToArray();
            if (sameNames.Length == 1) { return new[] { card, sameNames[0] }; }

            // - Is a Champion Unit (e.g. Spider Queen Elise)
            ICard[] champUnits = champCards
                .Select(c => homeCatalog.Cards[c.Code])
                .Where(c => c.Supertype?.Name == "Champion" && c.Type?.Name == "Unit")
                .ToArray();
            if (champUnits.Length == 1) { return new[] { card, champUnits[0] }; }

            // ...otherwise, just return the lv. 1 champ
            return new[] { card };
        }
    }
}
