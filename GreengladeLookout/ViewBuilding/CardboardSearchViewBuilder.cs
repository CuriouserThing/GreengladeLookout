using System.Collections.Generic;
using System.Threading.Tasks;
using Bjerg;
using Microsoft.Extensions.Options;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public class CardboardSearchViewBuilder : CardSearchViewBuilder
    {
        public CardboardSearchViewBuilder(CardboardViewBuilder cardViewBuilder, ICatalogService catalogService, IOptionsSnapshot<TipsySettings> settings) : base(catalogService, settings)
        {
            CardViewBuilder = cardViewBuilder;
        }

        private CardboardViewBuilder CardViewBuilder { get; }

        public override async Task<IEnumerable<ICard>> ExpandItem(ICard item)
        {
            return await ExpandIfChamp(item);
        }

        public override async Task<MessageView> BuildItemView(ICard item)
        {
            return await CardViewBuilder.BuildView(item);
        }
    }
}
