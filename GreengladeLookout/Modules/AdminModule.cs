using System.Text;
using System.Threading.Tasks;
using Bjerg;
using Discord;
using Discord.Commands;
using GreengladeLookout.Entities;
using TipsyOwl;

namespace GreengladeLookout.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        public AdminModule(GreengladeContext dbContext, LocaleService localeService)
        {
            DbContext = dbContext;
            LocaleService = localeService;
        }

        private GreengladeContext DbContext { get; }

        private LocaleService LocaleService { get; }

        private async Task GenericReplyAsync(string message)
        {
            Embed embed = new EmbedBuilder()
                .WithDescription(message)
                .Build();
            _ = await ReplyAsync(embed: embed);
        }

        [Command("set prefix")]
        public async Task<RuntimeResult> SetPrefixAsync(string prefix)
        {
            ulong id = Context.Guild.Id;
            Guild? guild = await DbContext.Guilds.FindAsync(id);
            string? oldPrefix = guild?.CommandPrefix;

            if (oldPrefix == prefix)
            {
                return TipsyRuntimeResult.FromError($"Command prefix is already `{prefix}`");
            }

            if (guild is null)
            {
                guild = new Guild
                {
                    Id = id,
                    CommandPrefix = prefix,
                };
                _ = DbContext.Add(guild);
            }
            else
            {
                guild.CommandPrefix = prefix;
                _ = DbContext.Update(guild);
            }

            _ = await DbContext.SaveChangesAsync();

            if (oldPrefix is null)
            {
                await GenericReplyAsync($"Set command prefix to `{prefix}`");
            }
            else
            {
                await GenericReplyAsync($"Changed command prefix from `{oldPrefix}` to `{prefix}`");
            }

            return TipsyRuntimeResult.FromSuccess();
        }

        [Command("set locale")]
        public async Task<RuntimeResult> SetLocaleAsync(string locale)
        {
            ulong id = Context.Guild.Id;
            Guild? guild = await DbContext.Guilds.FindAsync(id);
            string? oldLocale = guild?.Locale;
            if (!LocaleService.TryParseLocale(locale, out Locale? loc))
            {
                return TipsyRuntimeResult.FromError($"`{locale}` isn't a valid locale name. These are the recognized locales:\n\n{GetLocaleList()}");
            }

            Locale? oldLoc = null;
            if (oldLocale != null && LocaleService.TryParseLocale(oldLocale, out oldLoc) && loc! == oldLoc!)
            {
                return TipsyRuntimeResult.FromError($"Locale is already `{oldLoc}`");
            }

            if (!LocaleService.LocaleIsRecognized(loc!))
            {
                return TipsyRuntimeResult.FromError($"`{loc}` isn't a recognized LoR locale. These are the recognized locales:\n\n{GetLocaleList()}");
            }

            if (guild is null)
            {
                guild = new Guild
                {
                    Id = id,
                    Locale = locale,
                };
                _ = DbContext.Add(guild);
            }
            else
            {
                guild.Locale = locale;
                _ = DbContext.Update(guild);
            }

            _ = await DbContext.SaveChangesAsync();

            if (oldLoc is null)
            {
                await GenericReplyAsync($"Set locale to `{loc}`");
            }
            else
            {
                await GenericReplyAsync($"Changed locale from `{oldLoc}` to `{loc}`");
            }

            return TipsyRuntimeResult.FromSuccess();
        }

        [Command("locales")]
        public async Task LocalesAsync()
        {
            Embed embed = new EmbedBuilder()
                .WithTitle("Available Locales")
                .WithDescription(GetLocaleList())
                .Build();
            _ = await ReplyAsync(embed: embed);
        }

        private string GetLocaleList()
        {
            var sb = new StringBuilder();
            foreach (Locale loc in LocaleService.GetRecognizedLocales())
            {
                sb.AppendLine($"🔸`{loc}`");
            }

            return sb.ToString();
        }
    }
}
