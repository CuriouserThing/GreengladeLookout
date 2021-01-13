using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bjerg.CatalogSearching;
using Bjerg.CatalogSearching.Services;
using WumpusHall;

namespace GreengladeLookout.ViewBuilding
{
    public abstract class SearchViewBuilder<T> : IViewBuilder<TranslatedSearchResult<T>> where T : class
    {
        public async Task<MessageView> BuildView(TranslatedSearchResult<T> item)
        {
            IReadOnlyList<ItemMatch<T>> matches = item.Matches;

            if (matches.Count == 0)
            {
                return new MessageView($"No results for `{item.SearchTerm}`.");
            }

            string? didYouMean = null;
            if (matches.Count > 1)
            {
                IEnumerable<string?> names = matches.Skip(1).Select(m => $"**{GetItemName(m.Item)}**");
                string namesOutput = string.Join(" | ", names);
                didYouMean = $"Did you mean: {namesOutput}";
                if (didYouMean.Length > 2048)
                {
                    string s = matches.Count == 2 ? "" : "s";
                    didYouMean = $"*{matches.Count - 1} other result{s}*";
                }
            }

            T uItem = matches[0].Item;
            item.TranslationMap.TryGetValue(uItem, out T? tItem);
            IEnumerable<T> expansion = await ExpandItem(tItem ?? uItem);
            MessageView[] views = await Task.WhenAll(expansion.Select(BuildItemView));

            var messages = new List<MessageInfo>();
            if (didYouMean is null)
            {
                // Just render all item views
                foreach (MessageView view in views) { messages.AddRange(view.Messages); }
            }
            else if (views.Length == 1 && views[0].Messages.Count == 1)
            {
                // Send the text reply in the same message as the single item message if possible
                MessageInfo itemMessage = views[0].Messages[0];
                if (itemMessage.Text is null)
                {
                    messages.Add(new MessageInfo { Text = didYouMean, Embed = itemMessage.Embed });
                }
                else
                {
                    messages.Add(didYouMean);
                    messages.Add(itemMessage);
                }
            }
            else
            {
                // Send the text reply separately from the multiple item messages
                messages.Add(didYouMean);
                foreach (MessageView view in views) { messages.AddRange(view.Messages); }
            }

            return new MessageView(messages);
        }

        public abstract string GetItemName(T item);

        public virtual Task<IEnumerable<T>> ExpandItem(T item)
        {
            IEnumerable<T> empty = new[] { item };
            return Task.FromResult(empty);
        }

        public abstract Task<MessageView> BuildItemView(T item);
    }
}
