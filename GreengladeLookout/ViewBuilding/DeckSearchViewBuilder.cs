using System.Threading.Tasks;
using Bjerg;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public class DeckSearchViewBuilder : SearchViewBuilder<Deck>
    {
        public DeckSearchViewBuilder(DeckViewBuilder deckViewBuilder)
        {
            DeckViewBuilder = deckViewBuilder;
        }

        private DeckViewBuilder DeckViewBuilder { get; }
        
        public override string GetItemName(Deck item)
        {
            return item.Code;
        }

        public override async Task<MessageView> BuildItemView(Deck item)
        {
            return await DeckViewBuilder.BuildView(item);
        }
    }
}
