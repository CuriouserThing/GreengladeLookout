using System.Threading.Tasks;
using Bjerg;
using Microsoft.Extensions.Options;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public class CardRelationSearchViewBuilder : CardSearchViewBuilder
    {
        public CardRelationSearchViewBuilder(CardRelationViewBuilder cardViewBuilder, ICatalogService catalogService, IOptionsSnapshot<TipsySettings> settings) : base(catalogService, settings)
        {
            CardViewBuilder = cardViewBuilder;
        }

        private CardRelationViewBuilder CardViewBuilder { get; }

        public override async Task<MessageView> BuildItemView(ICard item)
        {
            return await CardViewBuilder.BuildView(item);
        }
    }
}
