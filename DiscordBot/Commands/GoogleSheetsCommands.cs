using Discord.Commands;
using System.Threading.Tasks;
using SuperNova.MEF.NetCore;
using SuperNova.Data.GoogleSheets.Contract;
using System.Composition;
using SuperNova.DiscordBot.Core;
using System;

namespace SuperNova.DiscordBot.Commands
{
    [DiscordCommand]
    public class GoogleSheetsCommands : ModuleBase<SocketCommandContext>
    {

        [Import]
        private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;

        public GoogleSheetsCommands()
        {
            MEFLoader.SatisfyImportsOnce(this);
        }

        #region Corp Price !cp

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(string commodity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                await ReplyAsync($"{commodity}: {info.CorpPrice}");
            }
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(string commodity, int quantity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                await ReplyAsync($"{quantity} {commodity} = {info.CorpPrice * quantity}");
            }
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(int quantity, string commodity)
        {
            await CorpPriceAsync(commodity, quantity);
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(string commodity, int quantity, decimal overridePrice)
        {
            await CorpPriceAsync(quantity, commodity, overridePrice);
        }

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(int quantity, string commodity, decimal overridePrice)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                var theoretical = quantity * info.CorpPrice;
                var actual = quantity * overridePrice;
                var difference = actual - theoretical;
                await ReplyAsync($"{quantity} {commodity} = {actual} ({Math.Abs((decimal)difference)} {(difference < 0 ? "under" : "over")} corp price.");
            }
        }

        #endregion

        [Command("commidityinfo")]
        [Alias("ci")]
        [Summary("Get information for a given commodity.")]
        public async Task CommodityInfoAsync(string commodity)
        {
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                await ReplyAsync(info.ToJsonString());
            }
        }

        
    }
}
