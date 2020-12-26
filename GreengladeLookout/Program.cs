using System;
using System.Reflection;
using System.Threading.Tasks;
using Bjerg;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
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
                .AddJsonFile(metaConfig["TIPSY_SETTINGS_PATH"],      true, true)
                .Build();

            var discordConfig = new DiscordSocketConfig { MessageCacheSize = 100 };
            var client = new DiscordSocketClient(discordConfig);

            var guildInfoSource = new DefaultGuildSettingsSource(new GuildSettings
            {
                CommandPrefix = ">",
                AllowInlineCommands = true,
                InlineCommandAlias = "search",
                InlineCommandOpener = "<<",
                InlineCommandCloser = ">>",
            });

            IServiceProvider services = new ServiceCollection()
                .AddLogging(b => b.AddSerilog(dispose: true))
                .Configure<GreengladeSettings>(config)
                .Configure<TipsySettings>(config)
                .AddDbContextPool<GreengladeContext>(options => options.UseSqlite(config["CONNECTION_STRING"]))
                .AddSingleton(guildInfoSource)
                .AddScoped<IGuildSettingsSource, EfCoreGuildSettingsSource>()
                .AddSingleton(client)
                .AddSingleton<IDataDragonFetcher, RiotDataDragonFetcher>()
                .AddSingleton<ICatalogService, BasicCatalogService>()
                .AddScoped<CardEmbedFactory>()
                .AddScoped<KeywordEmbedFactory>()
                .AddScoped<DeckEmbedFactory>()
                .AddScoped<LocaleService>()
                .AddSingleton<CommandService>()
                .AddScoped<CommandDispatcher>()
                .AddScoped<ICommandResultHandler, BasicCommandResultHandler>()
                .BuildServiceProvider();

            CommandService commands = services.GetRequiredService<CommandService>();
            _ = await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            using var handler = DiscordEventHandler.CreateAndRegister(client, commands, services);

            await client.LoginAsync(TokenType.Bot, config["DISCORD_TOKEN"]);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
