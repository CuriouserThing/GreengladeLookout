using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.Options;
using WumpusHall;

namespace GreengladeLookout.Modules
{
    [RequireContext(ContextType.Guild)]
    public class MetaModule : ModuleBase<SocketCommandContext>
    {
        // Regular bullet point
        private const string B = "🔸";

        // Title bullet point
        private const string TitleB = "🔷";

        // New line
        private const string BlankLine = "_\u200b_";

        public MetaModule(IGuildSettingsSource guildSettings, IOptionsSnapshot<GreengladeSettings> settings)
        {
            GuildSettings = guildSettings;
            Settings = settings.Value;
        }

        private IGuildSettingsSource GuildSettings { get; }

        private GreengladeSettings Settings { get; }

        private string BotName => Settings.BotName ?? Context.Guild.CurrentUser.Username;

        [Command("about")]
        public async Task AboutAsync()
        {
            ulong id = Context.Guild.Id;
            GuildSettings guild = await GuildSettings.GetSettings(id);
            string? prefix = guild.CommandPrefix;

            // ---------------------------------------------------------------------------------------------------------
            string inv = prefix ?? $"@{Context.Guild.CurrentUser.Nickname ?? Context.Guild.CurrentUser.Username} ";
            StringBuilder dsb = new StringBuilder()
                .AppendLine($"**{BotName}** is a lightweight Legends of Runeterra utility with a handful of commands for displaying decks, cards, and more.")
                .AppendLine()
                .AppendLine("Use the `help` command for info on available commands:")
                .AppendLine($"`{inv}help`")
                .AppendLine(BlankLine);

            // ---------------------------------------------------------------------------------------------------------
            EmbedBuilder eb = new EmbedBuilder().WithDescription(dsb.ToString());

            if (Settings.InfoMaintainerName != null)
            {
                eb.AddField(
                    $"{TitleB} Developer",
                    $"`{Settings.InfoMaintainerName}`\n\nFeel free to message with questions, bug reports, and feature requests!\n{BlankLine}");
            }

            if (Settings.InfoInviteLink != null)
            {
                eb.AddField(
                    $"{TitleB} Invite",
                    $"{Settings.InfoInviteLink}\n\nAny user with server permission to invite bots can also configure {Settings.BotName} once invited. Use the `config` command in your server for options.\n{BlankLine}");
            }

            if (Settings.InfoSourceLink != null)
            {
                eb.AddField(
                    $"{TitleB} Source",
                    $"{Settings.InfoSourceLink}");
            }

            // ---------------------------------------------------------------------------------------------------------
            await ReplyAsync(embed: eb.Build());
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            ulong id = Context.Guild.Id;
            GuildSettings guild = await GuildSettings.GetSettings(id);
            string? prefix = guild.CommandPrefix;
            string p = prefix ?? "";

            // Description ---------------------------------------------------------------------------------------------
            const string cmd = "card daring poro";
            StringBuilder dsb = new StringBuilder()
                .AppendLine($"Hello! **{BotName}** is a lightweight Legends of Runeterra utility with a handful of commands for displaying decks, cards, and more.")
                .AppendLine()
                .AppendLine("You can invoke any command by mentioning the bot, e.g.:")
                .AppendLine($"`@{Context.Guild.CurrentUser.Username} {cmd}`");

            if (prefix != null)
            {
                dsb.AppendLine()
                    .AppendLine($"...or using the prefix `{prefix}`, e.g.:")
                    .AppendLine($"`{prefix}{cmd}`");
            }

            dsb.AppendLine()
                .AppendLine("Phrases shown below in [brackets] are *required* parameters. Phrases shown below in {braces} are *optional* parameters.")
                .AppendLine(BlankLine);

            // General commands ----------------------------------------------------------------------------------------
            StringBuilder gcsb = new StringBuilder()
                .AppendLine($"{B}`{p}about`: Info about {BotName}.")
                .AppendLine()
                .AppendLine($"{B}`{p}invite`: Instructions for inviting {BotName} to your server.")
                .AppendLine(BlankLine);

            // Search commands -----------------------------------------------------------------------------------------
            StringBuilder scsb = new StringBuilder()
                .AppendLine($"{B}`{p}deck [deck-code]`: Print all the cards in a deck and any related info.")
                .AppendLine()
                .AppendLine($"{B}`{p}card [card-identifier]`: Print generic info on a card (everything printed on it in-game).")
                .AppendLine()
                .AppendLine($"{B}`{p}flavor [card-identifier]`: Print a card's full art and flavor text.")
                .AppendLine()
                .AppendLine($"{B}`{p}related [card-identifier]`: Print a list of a card's related cards (e.g. champion spells and created cards).")
                .AppendLine()
                .AppendLine($"{B}`{p}keyword [keyword-name]`: Print a keyword's description.")
                .AppendLine()
                .AppendLine($"{B}`{p}search [item-identifier]`: The `deck`, `card`, and `keyword` commands merged into a single command.");

            string? ip = guild.InlineCommandOpener;
            string? si = guild.InlineCommandCloser;
            if (guild.AllowInlineCommands && ip != null && si != null)
            {
                // This assumes that the InlineCommandAlias is "search", but this help doc is so hardwired anyway that I don't care :v
                scsb.AppendLine()
                    .AppendLine($"You can also invoke the `search` command inline using `{ip}` and `{si}` (e.g. `take a look at {ip}CEAQCAYJEIAAA{si} {ip}lulu{si} {ip}scout{si}`).");
            }

            scsb.AppendLine(BlankLine);

            // Other commands ------------------------------------------------------------------------------------------
            StringBuilder ocsb = new StringBuilder()
                .AppendLine($"{B}`{p}champroll {{card-identifier}}`: Roll a pair of random champions to build a deck with! Or, optionally, specify one champ and randomly roll the other.")
                .AppendLine(BlankLine);

            // Command parameters --------------------------------------------------------------------------------------
            StringBuilder cpsb = new StringBuilder()
                .AppendLine($"{B}`deck-code`: An exported deck code (e.g. `CICQCAQBAIAQGAICAEBQIBICAEASAMQEAECAQGJUHICACAIBAQAQCBA3AEBACCQBAMCAWBABAEASUAIBAQNACAYBCYBAGBANCQ`)")
                .AppendLine()
                .AppendLine($"{B}`card-identifier`: Either a card name (e.g. `magician`) or a card code (e.g. `02BW006`)")
                .AppendLine()
                .AppendLine($"{B}`keyword-name`: A keyword or vocab term name (e.g. `frost` or `allegiance`)")
                .AppendLine()
                .AppendLine($"{B}`item-identifier`: Either a `deck-code`, a `card-identifier`, or a `keyword-name`")
                .AppendLine()
                .AppendLine($"{BotName} allows partial names and minor misspellings in name parameters. Name strings aren't case-sensitive.");

            // ---------------------------------------------------------------------------------------------------------
            Embed embed = new EmbedBuilder()
                .WithDescription(dsb.ToString())
                .AddField(new EmbedFieldBuilder()
                              .WithName($"{TitleB} General Commands")
                              .WithValue(gcsb))
                .AddField(new EmbedFieldBuilder()
                              .WithName($"{TitleB} Search Commands")
                              .WithValue(scsb))
                .AddField(new EmbedFieldBuilder()
                              .WithName($"{TitleB} Other Commands")
                              .WithValue(ocsb))
                .AddField(new EmbedFieldBuilder()
                              .WithName($"{TitleB} Command Parameters")
                              .WithValue(cpsb))
                .Build();

            try
            {
                await Context.User.SendMessageAsync(embed: embed);
                await Context.Message.AddReactionAsync(new Emoji("✉"));
            }
            catch (HttpException)
            {
                await ReplyAsync("Wasn't able to DM you.");
            }
        }

        [Command("invite")]
        public async Task InviteAsync()
        {
            string desc;
            if (Settings.InfoInviteLink != null)
            {
                desc = $"Invite {BotName} to your server with this link:\n{Settings.InfoInviteLink}.\n\nTry the `config` command on your server for server-specific configuration options.";
            }
            else if (Settings.InfoMaintainerName != null)
            {
                desc = $"The invite link for {BotName} isn't known. Try contacting the maintainer `{Settings.InfoMaintainerName}` for info.";
            }
            else
            {
                desc = $"$The invite link for {BotName} isn't known.";
            }

            // ---------------------------------------------------------------------------------------------------------
            Embed embed = new EmbedBuilder()
                .WithDescription(desc)
                .Build();
            _ = await ReplyAsync(embed: embed);
        }

        [Command("config")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ConfigAsync()
        {
            ulong id = Context.Guild.Id;
            GuildSettings guild = await GuildSettings.GetSettings(id);
            string? prefix = guild.CommandPrefix;
            string p = prefix ?? "";

            // ---------------------------------------------------------------------------------------------------------
            StringBuilder dsb = new StringBuilder()
                .AppendLine("Additional, server-specific commands for configuring the bot. Anyone with `Administrator` or `Manage Server` perms can invoke these.")
                .AppendLine()
                .AppendLine($"{B}`{p}set prefix [prefix]`: Set the command prefix the bot listens for on this server. `>` by default.")
                .AppendLine()
                .AppendLine($"{B}`{p}set locale [locale]`: Set the locale (language-country) the bot uses on this server for printing cards etc. `en-US` by default.")
                .AppendLine()
                .AppendLine($"{B}`{p}locales`: List all available locale strings.");

            // ---------------------------------------------------------------------------------------------------------
            Embed embed = new EmbedBuilder()
                .WithDescription(dsb.ToString())
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}
