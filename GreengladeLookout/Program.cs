using System;
using System.Reflection;
using System.Threading.Tasks;
using Bjerg;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TipsyOwl;

namespace GreengladeLookout
{
    public static class Program
    {
        private static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            IConfiguration metaConfig = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile(metaConfig["GREENGLADE_SETTINGS_PATH"], true, true)
                .AddJsonFile(metaConfig["TIPSY_SETTINGS_PATH"], true, true)
                .Build();

            var discordConfig = new DiscordSocketConfig {MessageCacheSize = 100};
            var client = new DiscordSocketClient(discordConfig);

            IGuildSettingsSource guildInfoSource = new DefaultGuildSettingsSource(new CommandSettings
            {
                CommandPrefix = ">",
                AllowInlineCommands = true,
                InlineCommandAlias = "search",
                InlineCommandOpener = "<<",
                InlineCommandCloser = ">>"
            });

            IServiceProvider services = new ServiceCollection()
                .Configure<GreengladeSettings>(config)
                .Configure<TipsySettings>(config)
                .AddLogging(b => b.AddSerilog(dispose: true))
                .AddSingleton(client)
                .AddSingleton(guildInfoSource)
                .AddSingleton<IDataDragonFetcher, RiotDataDragonFetcher>()
                .AddSingleton<ICatalogService, BasicCatalogService>()
                .AddScoped<CardEmbedFactory>()
                .AddScoped<KeywordEmbedFactory>()
                .AddScoped<DeckEmbedFactory>()
                .AddSingleton<CommandService>()
                .BuildServiceProvider();

            ICommandResultHandler commandResultHandler = new BasicCommandResultHandler();

            CommandService commands = services.GetService<CommandService>();
            _ = await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            using var handler = CommandHandler.CreateAndRegister(client, commands, guildInfoSource, commandResultHandler, services);

            await client.LoginAsync(TokenType.Bot, config["DISCORD_TOKEN"]);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
