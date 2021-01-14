using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bjerg.CatalogSearching.Services;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public class AnythingSearchViewBuilder : SearchViewBuilder<CatalogItemUnion>
    {
        public AnythingSearchViewBuilder(CardboardSearchViewBuilder cardboardSearchViewBuilder, KeywordSearchViewBuilder keywordSearchViewBuilder, DeckSearchViewBuilder deckSearchViewBuilder)
        {
            CardboardSearchViewBuilder = cardboardSearchViewBuilder;
            KeywordSearchViewBuilder = keywordSearchViewBuilder;
            DeckSearchViewBuilder = deckSearchViewBuilder;
        }

        private CardboardSearchViewBuilder CardboardSearchViewBuilder { get; }

        private KeywordSearchViewBuilder KeywordSearchViewBuilder { get; }

        private DeckSearchViewBuilder DeckSearchViewBuilder { get; }

        public override string GetItemName(CatalogItemUnion item)
        {
            return item.T switch
            {
                CatalogItemUnion.Type.Card    => CardboardSearchViewBuilder.GetItemName(item.Card!),
                CatalogItemUnion.Type.Keyword => KeywordSearchViewBuilder.GetItemName(item),
                CatalogItemUnion.Type.Deck    => DeckSearchViewBuilder.GetItemName(item),
                _                             => string.Empty,
            };
        }

        public override async Task<IEnumerable<CatalogItemUnion>> ExpandItem(CatalogItemUnion item)
        {
            return item.T switch
            {
                CatalogItemUnion.Type.Card    => (await CardboardSearchViewBuilder.ExpandItem(item.Card!)).Select(CatalogItemUnion.AsCard),
                CatalogItemUnion.Type.Keyword => (await KeywordSearchViewBuilder.ExpandItem(item)).Select(CatalogItemUnion.AsKeyword),
                CatalogItemUnion.Type.Deck    => (await DeckSearchViewBuilder.ExpandItem(item)).Select(CatalogItemUnion.AsDeck),
                _                             => new[] { item },
            };
        }

        public override Task<MessageView> BuildItemView(CatalogItemUnion item)
        {
            return item.T switch
            {
                CatalogItemUnion.Type.Card    => CardboardSearchViewBuilder.BuildItemView(item.Card!),
                CatalogItemUnion.Type.Keyword => KeywordSearchViewBuilder.BuildItemView(item),
                CatalogItemUnion.Type.Deck    => DeckSearchViewBuilder.BuildItemView(item),
                _                             => Task.FromResult(new MessageView()),
            };
        }
    }
}
