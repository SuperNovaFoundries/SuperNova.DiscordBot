using Discord.Commands;
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
using SuperNova.AWS.Logging;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace SuperNova.DiscordBot.Commands
{

    public class BidderRegistration
    {
        public string DiscordId { get; set; }
        public string Name { get; set; }
        public string RegistrationCode { get; set; }
        public bool Validated { get; set; }
    }
    public class ContractBid
    {
        public string PostedAt { get; set; }
        public string BidderName { get; set; }
        public string BidderHash { get; set; }
        public bool Verified { get; set; }
        public string PlainText { get; set; }

    }
    public class BidderAction
    {
        public string DiscordId { get; set; }
        public string BidHash { get; set; }
    }

    public class Test : LoggingResource
    {
        public Test(string loggername) : base(loggername) { }

        public void LogInformation(string info)
        {
            Logger.LogInformation(info);
        }
    }

    [DiscordCommand]
    public class VickeryBiddingCommands : ModuleBase<SocketCommandContext>
    {
        [Import]
        private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;

        private static Test LogTest = new Test("VickeryBidder");

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

            if (!Context.IsPrivate)
            {
                return;
            }
            try
            {
                var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
                var bidderRegistration = await GetRegistrationAsync(prunUsername, discordId);
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

                var client = new DiscordSocketClient();
                var channel = client.GetChannel(853788435113312277) as IMessageChannel;
                await channel.SendMessageAsync($"{prunUsername} just requested a registration code with discord username {discordId}");
            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip..");
                throw ex;
            }
        }

        [Command("debug_multitest")]
        [Summary("Validate a registration code received from a user in game. !validate_corp {username} {code}")]
        public async Task MultiLineTest(string param1, string longText)
        {
            await ReplyAsync($"Parameter one was {param1}");
            await ReplyAsync($"Long text was " + longText);
        }

        [Command("bid")]
        [Summary("Place a bid for an infrastructure contract. !bid {contractId} {hash}")]
        public async Task BidCommand(string contractId, string bidHash)
        {
            try
            {
                if (!Context.IsPrivate) return;

                var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
                var bidderRegistration = await GetRegistrationAsync(string.Empty, discordId);
                if (bidderRegistration == null)
                {
                    await ReplyAsync("You are not registered to place a bid. Register for bidding or contact an SNF admin for assistance.");
                    return;
                }
                if (!bidderRegistration.Validated)
                {
                    await ReplyAsync("You are not yet validated to place a bid. Complete your registration or contact an SNF admin for assistance.");
                    return;
                }

                var currentBid = await GetContractBidAsync(contractId, bidderRegistration.Name);
                if (currentBid != null)
                {
                    //todo - allow deletion of bid
                    await ReplyAsync("You have already placed a bid for this contract. If you need to replace it, contact an administrator for assistance.");
                    return;
                }

                var list = new List<IList<object>>() {
                    new List<object> {
                        DateTime.Now.ToUniversalTime().ToString("dd MMM yyy HH':'mm':'ss 'UTC'"), bidderRegistration.Name, bidHash, string.Empty, string.Empty
                    }
                };
                var bidSheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";
                var response = await _sheetsProxy.AppendRange(bidSheetId, $"{contractId}_Bidding!A6", list);

                try
                {
                    LogTest.LogInformation(response.ToJsonString());
                }
                catch (Exception)
                {
                    await ReplyAsync("Logging failed...");
                    //eat - testing logging from within command.
                }

                await ReplyAsync("Your bid has been registered and is now viewable in the contract page.");

                var client = new DiscordSocketClient();
                var channel = client.GetChannel(853788435113312277) as IMessageChannel;
                await channel.SendMessageAsync($"{bidderRegistration.Name} just placed a bid for {contractId}");
            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip..." + ex.Message + ex.StackTrace);
                throw ex;
            }

        }

        [Command("create_hash")]
        [Summary("Hash a set of text. This is a secure function only available in private messages. No information is stored by the bot, but use at your own risk.")]
        public async Task HashTest(string toHash)
        {
            if (!Context.IsPrivate) return;

            using SHA256 sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(toHash));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            await ReplyAsync($"Your hash is {builder}.");
        }





        //[Command("verify_bid")]
        //[Summary("Verify a bid based on your plain-text")]
        public async Task VerifyBid(string contractId, string plainText)
        {
            try
            {
                if (!Context.IsPrivate) return;

                var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
                var registration = await GetRegistrationAsync(string.Empty, discordId);
                if (registration == null)
                {
                    await ReplyAsync("You are not registered to place bids... Contact an admin for assistance.");
                    return;
                }

                var bid = await GetContractBidAsync(contractId, registration.Name);
                if (bid == null)
                {
                    await ReplyAsync($"You don't currently have a bit for {contractId}. Check the name or contact an admin for assistance.");
                    return;
                }
                //get bid from sheets

                using SHA256 sha256Hash = SHA256.Create();
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                if (bid.BidderHash == builder.ToString())
                {
                    await ReplyAsync("Your bid was verified successfully. TODO");
                }
                else
                {
                    await ReplyAsync("Nope - they don't match. TODO");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip... " + ex.Message + ex.StackTrace);
                throw ex;
            }
        }


        [Command("validate_corp")]
        [Summary("Validate a registration code received from a user in game. !validate_corp {username} {code}")]
        public async Task ValidateRegistration(string prunUserName, string registrationCode)
        {
            if (Context.Channel.Name != "bidding-admin")
            {
                return;
            }

            if (Context.IsPrivate)
            {
                return;
            }

            var registration = await GetRegistrationAsync(prunUserName, string.Empty);
            if (registration == null)
            {
                await ReplyAsync("This user is not registered...");
                return;
            }
            if (registration.Validated)
            {
                await ReplyAsync("This user is already validated...");
                return;
            }

            if (registration.RegistrationCode != registrationCode)
            {
                await ReplyAsync("The registration codes do not match!!!");
                return;
            }
            try
            {
                registration.Validated = true;
                var registrations = await GetAllBidders();
                registrations[registrations.FindIndex(r => r.Name == registration.Name)] = registration;
                var list = new List<IList<object>>();
                foreach (var reg in registrations)
                {
                    list.Add(new List<object> { reg.Name, reg.DiscordId, reg.RegistrationCode, reg.Validated });
                }

                await _sheetsProxy.UpdateRange(_sheetId, "A2:D", list);
                await Context.User.SendMessageAsync("User was validated successfully.");
            }
            catch (Exception ex)
            {
                await Context.User.SendMessageAsync(ex.Message);
                throw;
            }
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

        private async Task<List<ContractBid>> GetAllBids(string contractId)
        {
            var list = new List<ContractBid>();
            var range = $"{contractId}_Bidding!A7:E";
            var sheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";

            var results = await _sheetsProxy.GetRange(sheetId, range);

            if (results?.Values == null) return list;

            foreach (var thing in results?.Values)
            {
                list.Add(new ContractBid
                {
                    PostedAt = thing[0]?.ToString() ?? string.Empty,
                    BidderName = thing[1]?.ToString() ?? string.Empty,
                    BidderHash = thing[2]?.ToString() ?? string.Empty,
                    Verified = bool.TryParse(thing[3].ToString().ToLower(), out var valid) && valid,
                    PlainText = thing[4]?.ToString() ?? string.Empty
                });
            }
            return list;
        }

        private async Task<ContractBid> GetContractBidAsync(string contractId, string userName)
        {
            var bids = await GetAllBids(contractId);
            return bids.FirstOrDefault(b => b.BidderName.ToLower() == userName.ToLower());
        }

        private async Task<BidderRegistration> GetRegistrationAsync(string prunUserName, string discordId)
        {
            var bidders = await GetAllBidders();
            var match = bidders.FirstOrDefault(b => b.Name.ToLower() == prunUserName.ToLower() || b.DiscordId == discordId);
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
        };
        private List<string> _members = new List<string>();

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
        public async Task CorpPriceAsync(string commodity, int quantity, decimal totalPrice)
        {
            var channel = Context.Channel;

            if (_publicChannels.Contains(channel.Name))
            {
                return;
            }

            if (Context.IsPrivate)
            {
                var authorized = await IsMember($"{Context.User.Username}#{Context.User.DiscriminatorValue}");
                if (!authorized) return;
            }




            var info = await _sheetsProxy.GetCorpCommodityInfoAsync(_sheetId, "Corp-Prices!C45:N386", commodity.ToUpper());
            if (info?.CorpPrice == null)
            {
                await ReplyAsync($"Couldn't find a corp price for {commodity}...");
            }
            else
            {
                var totalCorpPrice = quantity * info.CorpPrice;
                var difference = totalPrice - totalCorpPrice;
                var reply = difference < 0
                    ? $"{quantity} {commodity} at {totalPrice} is {-difference} less than the CorpPrice of {totalCorpPrice}"
                    : $"{quantity} {commodity} at {totalPrice} is {difference} more than the CorpPrice of {totalCorpPrice}";

                await ReplyAsync(reply);
            }
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
            if (Context.IsPrivate)
            {
                var authorized = await IsMember($"{Context.User.Username}#{Context.User.DiscriminatorValue}");
                if (!authorized) return;
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
            if (Context.IsPrivate)
            {
                var authorized = await IsMember($"{Context.User.Username}#{Context.User.DiscriminatorValue}");
                if (!authorized) return;
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
            if (Context.IsPrivate)
            {
                var authorized = await IsMember($"{Context.User.Username}#{Context.User.DiscriminatorValue}");
                if (!authorized) return;
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

        private async Task<bool> IsMember(string discordId)
        {
            if (_members.Count == 0)
            {
                var values = await _sheetsProxy.GetRange("1tyYLfgAqD7Mm1Lv8-fc59RuPdPZ_pa0HYjY7TVI_KKo", "Corp-Members!C2:C");
                _members = values?.Values?.Where(m => m.Count == 1).Select(m => m[0].ToString()).ToList();
            }
            return _members.Contains(discordId);
        }

    }
}
