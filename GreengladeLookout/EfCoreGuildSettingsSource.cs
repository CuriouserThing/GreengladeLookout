using System.Threading.Tasks;
using GreengladeLookout.Entities;
using TipsyOwl;

namespace GreengladeLookout
{
    public class EfCoreGuildSettingsSource : IGuildSettingsSource
    {
        public EfCoreGuildSettingsSource(GreengladeContext context, DefaultGuildSettingsSource defaultSource)
        {
            Context = context;
            DefaultSource = defaultSource;
        }

        public GreengladeContext Context { get; }

        public DefaultGuildSettingsSource DefaultSource { get; }

        public async Task<GuildSettings> GetSettings(ulong guild)
        {
            GuildSettings settings = DefaultSource.DefaultSettings;
            Guild? dbGuild = await Context.FindAsync<Guild>(guild);
            if (dbGuild is null)
            {
                return settings;
            }
            else
            {
                return new GuildSettings
                {
                    CommandPrefix = dbGuild.CommandPrefix ?? settings.CommandPrefix,
                    AllowInlineCommands = settings.AllowInlineCommands,
                    InlineCommandAlias = settings.InlineCommandAlias,
                    InlineCommandOpener = settings.InlineCommandOpener,
                    InlineCommandCloser = settings.InlineCommandCloser,
                    Locale = dbGuild.Locale ?? settings.Locale,
                };
            }
        }
    }
}
