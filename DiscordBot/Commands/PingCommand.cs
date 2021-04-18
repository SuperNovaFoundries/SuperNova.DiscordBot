using Discord.Commands;
using SuperNova.DiscordBot.Core;
using System.Threading.Tasks;

namespace SuperNova.DiscordBot.Commands
{
    [DiscordCommand]
    public class PingCommand : ModuleBase<SocketCommandContext>
    {

        [Command("ping")]
        [Alias("p")]
        [Summary("This is a summary!")]
        public async Task PingAsync()
        {
            await ReplyAsync("pong");
        }

    }
}
