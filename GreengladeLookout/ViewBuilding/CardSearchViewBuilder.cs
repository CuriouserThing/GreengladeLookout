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
            // The potential lv. 2+ champs for this card are champion units that share its code (sans T number).
            // We group the cards by name and evaluate the grouped cards together.
            CardCode bc = card.Code;
            IGrouping<string?, ICard>[] hNameGroups = homeCatalog.Cards.Values
                .Where(c =>
                           c.Code.Number == bc.Number &&
                           c.Code.TNumber != 0 &&
                           c.Code.Faction == bc.Faction &&
                           c.Code.Set == bc.Set &&
                           c.Supertype?.Name == "Champion" &&
                           c.Type?.Name == "Unit")
                .GroupBy(c => c.Name)
                .ToArray();

            // Only attempt to find lv. 2+ champs if either:
            // - There's a single name-group of cards (e.g. Spider Queen Elise)
            // - There's a name-group of cards with the same name as the lv. 1 champ (e.g. Anivia but not Eggnivia)
            ICard hCard = homeCatalog.Cards[card.Code];
            IGrouping<string?, ICard>? hNameGroup = hNameGroups.Length == 1 ? hNameGroups[0] : hNameGroups.SingleOrDefault(g => g.Key == hCard.Name);
            if (hNameGroup is null)
            {
                return new[] { card };
            }

            // If there's only one lv. 2+ champ in the group, assume it's the lv. 2 champ (e.g. Garen)
            // If there are two lv. 2+ champs in the group, assume the one with a level-up is lv. 2 and the one with no level-up is lv. 3 (e.g. Renekton) 
            // Otherwise, we can't assume anything.
            ICard[] hCards = hNameGroup.ToArray();
            ICard? hLv2 = null, hLv3 = null;
            if (hCards.Length == 1)
            {
                hLv2 = hCards[0];
            }
            else if (hCards.Length == 2)
            {
                bool aLeveled = string.IsNullOrWhiteSpace(hCards[0].LevelupDescription);
                bool bLeveled = string.IsNullOrWhiteSpace(hCards[1].LevelupDescription);

                if (!bLeveled && aLeveled)
                {
                    hLv2 = hCards[1];
                    hLv3 = hCards[0];
                }

                if (!aLeveled && bLeveled)
                {
                    hLv2 = hCards[0];
                    hLv3 = hCards[1];
                }
            }

            // We're done! Return either one, two, or three levels of champs.

            if (hLv2 is null)
            {
                return new[] { card };
            }

            ICard lLv2 = localCatalog.Cards[hLv2.Code];
            if (hLv3 is null)
            {
                return new[] { card, lLv2 };
            }

            ICard lLv3 = localCatalog.Cards[hLv3.Code];
            return new[] { card, lLv2, lLv3 };
        }
    }
}
