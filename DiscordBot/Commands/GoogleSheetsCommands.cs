﻿using Discord.Commands;
using System.Threading.Tasks;
using SuperNova.MEF.NetCore;
using SuperNova.Data.GoogleSheets.Contract;
using System.Composition;
using SuperNova.DiscordBot.Core;
using System;
using Discord.WebSocket;
using System.Linq;
using System.Collections.Generic;
using Discord;

namespace SuperNova.DiscordBot.Commands
{

    public class BidderRegistration
    {
        public string DiscordId { get; set; }
        public string Name { get; set; }
        public string RegistrationCode { get; set; }
        public bool Validated { get; set; }
    }

    public class BidderAction
    {
        public string DiscordId { get; set; }
        public string BidHash { get; set; }
    }

    [DiscordCommand]
    public class VickeryBiddingCommands : ModuleBase<SocketCommandContext>
    {
        [Import]
        private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;
        private static Random random = new Random();
        private readonly string _sheetId = "1xtV9MTohgWfm3oN7kmms0Fa4Lw_Wg8358qrPXWvA3nM";
        public VickeryBiddingCommands()
        {
            MEFLoader.SatisfyImportsOnce(this);
        }

        [Command("register_corp")]
        [Summary("Register a new account for government contract bidding. register_corp {!register_corp {PrunCorpName}")]
        public async Task RegisterNewBidderAsync(string prunUsername)
        {
            if(Context.Channel.Name != "public-bidding")
            {
                await Context.User.SendMessageAsync("This command is only valid in the public bidding channel.");
                return;
            }
            try
            {
                var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
                var bidderRegistration = await GetRegistrationAsync(prunUsername);
                if (bidderRegistration != null)
                {
                    await Context.User.SendMessageAsync("You already have a registration. If you are having issues, please contact an SNF admin for assitance.");
                    return;
                }

                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var code = new string(Enumerable.Range(1, 10).Select(_ => chars[random.Next(chars.Length)]).ToArray());
                bidderRegistration = new BidderRegistration
                {
                    DiscordId = discordId,
                    Name = prunUsername,
                    RegistrationCode = code,
                    Validated = false
                };
                var list = new List<IList<object>>() {
                    new List<object> { 
                        bidderRegistration.Name, bidderRegistration.DiscordId, bidderRegistration.RegistrationCode, bidderRegistration.Validated.ToString().ToUpper() 
                    }
                };
                var response = await _sheetsProxy.AppendRange(_sheetId, "Registrations!A1", list);
                await Context.User.SendMessageAsync($"Your unique registration code is {bidderRegistration.RegistrationCode}. You must send a message with this code to an SNF Admin in-game. If you have questions, please contact an SNF admin for assistance.");
            }
            catch (Exception)
            {
                await ReplyAsync("I fell and broke my hip...");
                throw;
            }
        }

        public async Task<string> PlaceBid(string contractId, string bidHash)
        {
            var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
            var bidderRegistration = await GetRegistrationAsync(discordId);
            if (bidderRegistration == null)
            {
                return "You are not registered to place a bid. Register for bidding or contact an SNF admin for assistance.";
            }
            if (!bidderRegistration.Validated)
            {
                return "You are not yet validated to place a bid. Complete your registration or contact an SNF admin for assistance.";
            }

            return "You are validated, but this feature is not ready yet.";
        }


        [Command("validate_corp")]
        [Summary("Validate a registration code received from a user in game. !validate_corp {username} {code}")]
        public async Task ValidateRegistration(string prunUserName, string registrationCode)
        {
            if (Context.Channel.Name != "billing-admin")
            {
                return;
            }
            
            var registration = await GetRegistrationAsync(prunUserName);
            if (registration == null)
            {
                await ReplyAsync("This user is not registered...");
            }
            if (registration.Validated)
            {
                await ReplyAsync("This user is already validated...");
            }

            if (registration.RegistrationCode != registrationCode)
            {
                await ReplyAsync("The registration codes do not match!!!");
            }

            registration.Validated = true;

            var registrations = await GetAllBidders();
            registrations[registrations.FindIndex(r => r.Name == registration.Name)] = registration;

            var list = new List<IList<object>>();
            foreach (var reg in registrations)
            {
                list.Add(new List<object> { reg.Name, reg.DiscordId, reg.RegistrationCode, reg.Validated });
            }

            await _sheetsProxy.UpdateRange(_sheetId, "A2:D", list);

        }
        private async Task<List<BidderRegistration>> GetAllBidders()
        {
            var list = new List<BidderRegistration>();
            var range = "Registrations!A2:D";

            var results = await _sheetsProxy.GetRange(_sheetId, range);

            if (results?.Values == null) return list;

            foreach (var thing in results?.Values)
            {
                list.Add(new BidderRegistration
                {
                    Name = thing[0].ToString(),
                    DiscordId = thing[1].ToString(),
                    RegistrationCode = thing[2].ToString(),
                    Validated = bool.Parse(thing[3].ToString().ToLower())
                });
            }
            return list;
        }

        private async Task<BidderRegistration> GetRegistrationAsync(string prunUserName)
        {
            var bidders = await GetAllBidders();
            var match = bidders.FirstOrDefault(b => b.Name == prunUserName);
            return match;
        }

    }





    [DiscordCommand]
    public class GoogleSheetsCommands : ModuleBase<SocketCommandContext>
    {

        [Import]
        private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;
        private readonly string _sheetId = "1ENpnE964UInUGfjzt8ShAbKtqNF8byDGfT01vZHlNAk";
        private List<string> _publicChannels = new List<string>
        {
            "introduction",
            "lobby",
            "public-shipping",
            "applications",
            "role-request",
            "public-bidding",
            "bot-test"
        };

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
            await CorpPriceAsync(commodity, null);
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
            var channel = Context.Channel;
            if (_publicChannels.Contains(channel.Name))
            {
                return;
            }
            
            
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(_sheetId, "Corp-Prices!C45:N386", commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                var theoretical = quantity * info.CorpPrice;
                var actual = quantity * overridePrice;
                var difference = actual - theoretical;
                await ReplyAsync($"{quantity} {commodity} = {actual} ({Math.Abs((decimal)difference)} {(difference < 0 ? "under" : "over")} corp price.)");
            }
        }

        #endregion

        [Command("corpprice")]
        [Alias("cp")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task CorpPriceAsync(string commodity, int? quantity)
        {


            var channel = Context.Channel;
            if (_publicChannels.Contains(channel.Name))
            {
                return;
            }

            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(_sheetId, "Corp-Prices!C45:N386", commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                if (quantity != null)
                {
                    await ReplyAsync($"{quantity} {commodity} = {info.CorpPrice * quantity}");
                }
                else
                {
                    await ReplyAsync($"{commodity}: {info.CorpPrice}");
                }
            }
        }


        [Command("commidityinfo")]
        [Alias("ci")]
        [Summary("Get information for a given commodity.")]
        public async Task CommodityInfoAsync(string commodity)
        {
            var channel = Context.Channel;
            if (_publicChannels.Contains(channel.Name))
            {
                return;
            }
            
            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(_sheetId, "Corp-Prices!C45:N386", commodity.ToUpper());
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
