using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TipsyOwl;

namespace GreengladeLookout
{
    public class BasicCommandResultHandler : ICommandResultHandler
    {
        public async Task HandleResult(Optional<CommandInfo> info, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess && ShouldPrintError(result.Error))
            {
                string message = MessageFromError(result.Error, result.ErrorReason);
                Embed embed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription(message)
                    .WithColor(Color.Red)
                    .Build();
                _ = await context.Channel.SendMessageAsync(embed: embed);
            }
        }

        private static bool ShouldPrintError(CommandError? error)
        {
            switch (error)
            {
                case CommandError.UnknownCommand:
                case CommandError.UnmetPrecondition:
                    return false;
                case CommandError.ParseFailed:
                case CommandError.BadArgCount:
                case CommandError.ObjectNotFound:
                case CommandError.MultipleMatches:
                case CommandError.Exception:
                case CommandError.Unsuccessful:
                case null:
                default:
                    return true;
            }
        }

        private static string MessageFromError(CommandError? error, string? errorReason)
        {
            const string unknownErrorReason = "Unknown error encountered.";

            switch (error)
            {
                case CommandError.UnknownCommand:
                case CommandError.ParseFailed:
                case CommandError.BadArgCount:
                case CommandError.UnmetPrecondition:
                case CommandError.Unsuccessful:
                    return errorReason ?? unknownErrorReason;
                case CommandError.ObjectNotFound:
                case CommandError.MultipleMatches:
                case CommandError.Exception:
                    return "Bot bug encountered.";
                case null:
                default:
                    return unknownErrorReason;
            }
        }
    }
}
