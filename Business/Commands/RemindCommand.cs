using Discord.Commands;
using Discord;
using SuperNova.DiscordBot.Data.Core;
using System.Threading.Tasks;
using SuperNova.MEF.NetCore;
using SuperNova.Data.GoogleSheets.Contract;
using System.Composition;
using SuperNova.DiscordBot.Common.Utils;

namespace SuperNova.DiscordBot.Business.Commands
{
    [DiscordCommand]
    public class RemindCommand : ModuleBase<SocketCommandContext>
    {

        [Command("remind")]
        [Alias("r")]
        [Summary("Remind you of something after a given time (in minutes)")]
        public async Task RemindAsync(string reminder, int time)
        {
            var user = Context.Message.Author;
            if (time > 60 || time < 0)
            {
                await user.SendMessageAsync("Unfortunately, only reminders <= 60 minutes (and greater than 0) are supported currently.");
            }
            else
            {
                await ReplyAsync("Ok.");
                await Task.Delay(time * 60000);
                await user.SendMessageAsync($"REMINDER: {reminder}");
            }
        }

        [Command("remind")]
        [Alias("r")]
        [Summary("Remind you of something after a given time (in minutes)")]
        public async Task RemindAsync()
        {
            await ReplyAsync("To set a reminder, try this command: <!remind \"Remind me to take out the trash!\" 45> - the time is in minutes, and timers up to 1 hour are supported.");
        }
    }


    [DiscordCommand]
    public class GoogleSheetsCommands : ModuleBase<SocketCommandContext>
    {
        [Import] private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;
        public GoogleSheetsCommands()
        {
            MEFLoader.SatisfyImportsOnce(this);
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity")]
        public async Task CorpPriceAsync(string commodity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity);
            await ReplyAsync($"{commodity} CP: {info.CorpPrice}");
           
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity")]
        public async Task CorpPriceAsync(string commodity, int quantity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity);
            await ReplyAsync($"{quantity}*{commodity} (CP): {info.CorpPrice*quantity}");

        }

        [Command("commidityinfo")]
        [Alias("ci")]
        [Summary("Get information about a given commodity")]
        public async Task CommodityInfoAsync(string commodity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity);
            await ReplyAsync(info.ToJsonString());
        }

    }
}
