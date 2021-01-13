using System.Threading.Tasks;
using Bjerg.Lor;
using TipsyOwl;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public class KeywordSearchViewBuilder : SearchViewBuilder<LorKeyword>
    {
        public KeywordSearchViewBuilder(KeywordViewBuilder keywordViewBuilder)
        {
            KeywordViewBuilder = keywordViewBuilder;
        }

        private KeywordViewBuilder KeywordViewBuilder { get; }

        public override string GetItemName(LorKeyword item)
        {
            return KeywordViewBuilder.GetKeywordString(item);
        }

        public override async Task<MessageView> BuildItemView(LorKeyword item)
        {
            return await KeywordViewBuilder.BuildView(item);
        }
    }
}
