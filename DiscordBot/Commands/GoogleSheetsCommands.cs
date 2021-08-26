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
using SuperNova.DiscordBot.Contract;

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

    public class CutOffTimes
    {
        public DateTime? BidCutoff;
        public DateTime? VerifyCutoff;

    }

    [DiscordCommand]
    public class VickeryBiddingCommands : ModuleBase<SocketCommandContext>
    {
        [Import]
        private IGoogleSheetsProxy _sheetsProxy { get; set; } = null;

        //[Import]
        //private IConnectionService _connectionService { get; set; } = null;

        private static Test LogTest = new Test("VickeryBidder");
        private readonly string _bidSheedId = Environment.GetEnvironmentVariable("BID_SHEET");
        private readonly string _corpSheetId = Environment.GetEnvironmentVariable("CORP_SHEET");
        private static Random random = new Random();
        private readonly string _sheetId = "1xtV9MTohgWfm3oN7kmms0Fa4Lw_Wg8358qrPXWvA3nM";
        public VickeryBiddingCommands()
        {
            MEFLoader.SatisfyImportsOnce(this);
        }

        [Command("debug_test1")]
        public async Task DebugTest1(){

            LogTest.LogInformation(_bidSheedId);
            LogTest.LogInformation(_corpSheetId);
            LogTest.LogInformation("DONE!!!");
            await ReplyAsync("Done!");
        }


        private async Task<CutOffTimes> GetCutoffTimesAsync(string contractId)
        {
            var bidSheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";
            var values = await _sheetsProxy.GetRange(bidSheetId, $"{contractId}_Bidding!D3:D4");
            if (values.Values == null)
            {
                return new CutOffTimes
                {
                    BidCutoff = null,
                    VerifyCutoff = null
                };
            }
            return new CutOffTimes
            {
                BidCutoff = DateTime.Parse(values.Values[0][0].ToString()),
                VerifyCutoff = DateTime.Parse(values.Values[1][0].ToString())
            };
        }

        [Command("cancel_bid")]
        [Summary("Cancels a bid you have placed")]
        public async Task CancelBid(string contractId, string hash)
        {
            try
            {
                if (!Context.IsPrivate) return;
                var registration = await GetRegistrationAsync(string.Empty, $"{Context.User.Username}#{Context.User.Discriminator}");
                if (registration == null)
                {
                    await ReplyAsync("You are not registered to place a bid. Register for bidding or contact an admin for assistance.");
                    return;
                }
                if (!registration.Validated)
                {
                    await ReplyAsync("You are not registered to place a bid. Complete your registration or contact an admin for assistance.");
                    return;
                }

                var cutoffTimes = await GetCutoffTimesAsync(contractId);
                
                if (cutoffTimes.VerifyCutoff == null || cutoffTimes.BidCutoff == null)
                {
                    await ReplyAsync("This contract has not yet been open for bidding.");
                    return;
                }

                if (DateTime.UtcNow > cutoffTimes.BidCutoff)
                {
                    await ReplyAsync($"You can no longer cancel this bid. The bidding phase for this contract closed on {((DateTime)cutoffTimes.BidCutoff).ToLongDateString()} at {((DateTime)cutoffTimes.BidCutoff).ToLongTimeString()} UTC.");
                    return;
                }


                var allBids = await GetAllBidsAsync(contractId);
                var bid = allBids?.ToList()?.FirstOrDefault(b => b.BidderHash == hash && b.BidderName == registration.Name);
                if (bid == null)
                {
                    await ReplyAsync("Could not locate a bid matching this hash. Check your entry or contact an admin for assistance.");
                    return;
                }

                allBids.Remove(bid);
                var list = new List<IList<object>>(allBids.Select(bid => new List<object> { bid.PostedAt, bid.BidderName, bid.BidderHash, bid.Verified, bid.PlainText }))
                {
                    new List<object> { "", "", "", "", "" }
                };

                var bidSheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";

                await _sheetsProxy.UpdateRange(bidSheetId, $"{contractId}_Bidding!A7:E", list);

                await ReplyAsync("Your bid was successfully cancelled.");
            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip... " + ex.Message + ex.StackTrace);
                throw ex;
            }

        }

        [Command("start_bid")]
        [Summary("Opens a contract up for bidding")]
        public async Task StartBidding(string contractId, int bidDays, int verifyDays)
        {

            try
            {
                if (Context.IsPrivate) return;
                if (Context.Channel.Name != "bidding-admin") return;

                var sheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";

                var bidCutoff = DateTime.UtcNow.Date.AddDays(bidDays + 1).AddMinutes(-1);
                var verifyCutoff = DateTime.UtcNow.Date.AddDays(bidDays + verifyDays + 1).AddMinutes(-1);

                var currentCutoffs = await _sheetsProxy.GetRange(sheetId, $"{contractId}_Bidding!D3:D4");
                if (currentCutoffs.Values != null)
                {
                    await ReplyAsync("It looks like this bid has already started...");
                    return;
                }
                var list = new List<IList<object>>() {
                    new List<object> { bidCutoff.ToString() },
                    new List<object> { verifyCutoff.ToString() }
                };

                await _sheetsProxy.UpdateRange(sheetId, $"{contractId}_Bidding!D3:D4", list);
            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip... " + ex.Message + ex.StackTrace);
                throw ex;
            }

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
                using var client = new DiscordSocketClient();
                if (client.GetChannel(853788435113312277) is IMessageChannel channel)
                {
                    await channel.SendMessageAsync($"{prunUsername} just requested a registration code with discord username {discordId}");
                }
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
                    await ReplyAsync("You are not registered to place a bid. Register for bidding or contact an admin for assistance.");
                    return;
                }
                if (!bidderRegistration.Validated)
                {
                    await ReplyAsync("You are not registered to place a bid. Complete your registration or contact an admin for assistance.");
                    return;
                }

                var cutoffs = await GetCutoffTimesAsync(contractId);

                if (cutoffs.VerifyCutoff == null || cutoffs.BidCutoff == null)
                {
                    await ReplyAsync("This contract has not yet been open for bidding.");
                    return;
                }

                if (DateTime.UtcNow > cutoffs.BidCutoff)
                {
                    await ReplyAsync($"The bidding phase for this contract closed on {((DateTime)cutoffs.BidCutoff).ToLongDateString()} at {((DateTime)cutoffs.BidCutoff).ToLongTimeString()} UTC.");
                    return;

                }

                var list = new List<IList<object>>() {
                    new List<object> { DateTime.Now.ToUniversalTime().ToString("dd MMM yyy HH':'mm':'ss 'UTC'"), bidderRegistration.Name, bidHash, "FALSE", "<Hidden>"}
                };
                var bidSheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";
                var response = await _sheetsProxy.AppendRange(bidSheetId, $"{contractId}_Bidding!A6", list);

                try
                {
                    LogTest.LogInformation("OOGA BOOGA");
                    LogTest.LogInformation(response.ToJsonString());
                }
                catch (Exception)
                {
                    await ReplyAsync("Logging failed...");
                    //eat - testing logging from within command.
                }

                await ReplyAsync("Your bid has been registered and is now viewable in the contract page.");
                
                //if (_connectionService.Client.GetChannel(853788435113312277) is IMessageChannel channel)
                //{
                //    await channel.SendMessageAsync($"{bidderRegistration.Name} just placed a bid for {contractId}");
                //}

            }
            catch (Exception ex)
            {
                await ReplyAsync("I fell and broke my hip..." + ex.Message + ex.StackTrace);
                throw ex;
            }

        }


        [Command("hash")]
        [Summary("Hash a set of text. This is a secure function only available in private messages. No information is stored or logged by the bot, but use at your own risk.")]
        public async Task HashTest([Remainder] string toHash)
        {
            if (!Context.IsPrivate) return;
            await ReplyAsync($"Your hash is {getHash(toHash)}");
        }

        private string getHash(string text)
        {
            using SHA256 sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }


        [Command("verify_bid")]
        [Summary("Verify a bid based on your plain-text")]
        public async Task VerifyBid(string contractId, [Remainder] string plainText)
        {
            try
            {
                if (!Context.IsPrivate) return;

                var discordId = $"{Context.User.Username}#{Context.User.Discriminator}";
                var registration = await GetRegistrationAsync(string.Empty, discordId);
                if (registration == null)
                {
                    await ReplyAsync("You are not registered to place a bid. Register for bidding or contact an admin for assistance.");
                    return;
                }

                var bids = await GetAllBidsAsync(contractId);
                var userBids = bids.Where(b => b.BidderName == registration.Name).ToList();
                if (userBids.Count == 0)
                {
                    await ReplyAsync("You do not have any bids to verify.");
                    return;
                }

                var cutoffs = await GetCutoffTimesAsync(contractId);

                if (cutoffs.VerifyCutoff == null || cutoffs.BidCutoff == null)
                {
                    await ReplyAsync("This contract has not yet been open for bidding.");
                    return;
                }

                if (DateTime.UtcNow > cutoffs.VerifyCutoff)
                {
                    await ReplyAsync($"The bid verification period for this contract closed on {((DateTime)cutoffs.BidCutoff).ToLongDateString()}.");
                    return;

                }

                if (DateTime.UtcNow < cutoffs.BidCutoff)
                {
                    await ReplyAsync($"The bid verification phase for this contract has not begun yet. It will begin on {((DateTime)cutoffs.BidCutoff).ToLongDateString()}.");
                    return;
                }


                var verified = false;
                using var client = new DiscordSocketClient();
                foreach (var bid in userBids)
                {
                    if (bid.BidderHash != getHash(plainText)) continue;
                    if (bid.Verified)
                    {
                        await ReplyAsync("This bid is already verified.");
                        return;
                    }
                    verified = true;
                    bid.PlainText = plainText;
                    bid.Verified = true;
                    bids[bids.FindIndex(r => r.BidderHash == bid.BidderHash)] = bid;
                    await ReplyAsync("Your bid was verified successfully.");
                    break;
                }
                if (!verified)
                {
                    await ReplyAsync("I was unable to verify your bid. Please check your entry and try again or contact an admin for assistance");
                }

                var list = new List<IList<object>>(bids.Select(bid => new List<object> { bid.PostedAt, bid.BidderName, bid.BidderHash, bid.Verified, bid.PlainText }));
                var bidSheetId = "1qWTf-pyPrTXM005QU6wfc85b-h-WTJt6ojV2e0Bi26E";

                await _sheetsProxy.UpdateRange(bidSheetId, $"{contractId}_Bidding!A7:E", list);

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

        private async Task<List<ContractBid>> GetAllBidsAsync(string contractId)
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
                    Verified = bool.TryParse(thing[3]?.ToString().ToLower(), out var valid) && valid,
                    PlainText = thing[4]?.ToString() ?? string.Empty
                });
            }
            return list;
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


        private static Test LogTest = new Test("GoogleSheets");
        [Import]
        private IConnectionService _connectionService { get; set; } = null;

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
                var discriminator = FixDiscriminatorValue(Context.User.DiscriminatorValue);
                var authorized = await IsMember($"{Context.User.Username}#{discriminator}");
                if (!authorized)
                {
                    await ReplyAsync($"Couldn't authorize this request... Is {Context.User.Username}#{discriminator} your user name?");
                    return;
                }
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
                var discriminator = FixDiscriminatorValue(Context.User.DiscriminatorValue);
                var authorized = await IsMember($"{Context.User.Username}#{discriminator}");
                if (!authorized)
                {
                    await ReplyAsync($"Couldn't authorize this request... Is {Context.User.Username}#{discriminator} your user name?");
                    return;
                }
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


        [Command("vallis")]
        [Alias("vallis")]
        [Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task VallisBufferPriceAsync(int quantity, string commodity)
        {
            var channel = Context.Channel;
            if (_publicChannels.Contains(channel.Name))
            {
                return;
            }
            if (Context.IsPrivate)
            {

                var discriminator = FixDiscriminatorValue(Context.User.DiscriminatorValue);
                var authorized = await IsMember($"{Context.User.Username}#{discriminator}");
                if (!authorized)
                {
                    await ReplyAsync($"Couldn't authorize this request... Is {Context.User.Username}#{discriminator} your user name?");
                    return;
                }
            }

            var range = await _sheetsProxy.GetRange("1tyYLfgAqD7Mm1Lv8-fc59RuPdPZ_pa0HYjY7TVI_KKo", "PL-VallisMgmt!AI3:AJ");

            if (range.Values == null)
            {
                await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
                return;
            }

            var match = range.Values.FirstOrDefault(i => i[0].ToString() == commodity.ToUpper());
            if (match == null || match.Count == 0)
            {
                await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
                return;
            }
            if (!decimal.TryParse(match[1].ToString(), out var price))
            {
                await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
                return;
            }

            var total = price * quantity;
            await ReplyAsync($"{quantity} {commodity} = {total} NCC");

        }

        private string FixDiscriminatorValue(ushort discriminatorValue)
        {
            LogTest.LogInformation($"Fixing Discriminator Value: {discriminatorValue}");
            string value = Context.User.DiscriminatorValue.ToString();
            if (value.Length < 4)
            {
                var numberOfZerosToInsert = 4 - value.Length;
                for (int i = 0; i < numberOfZerosToInsert; i++)
                {
                    value = value.Insert(0, "0");
                }
            }
            LogTest.LogInformation($"Fixed discriminator value: {value}");
            return value;
        }

        [Command("vallis")]
        [Alias("vallis")]
        [Summary("Get Vallis buffer price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task VallisBufferPriceAsync(string commodity)
        {
            await VallisBufferPriceAsync(1, commodity);

        }

        [Command("vallis")]
        [Alias("vallis")]
        [Summary("Get Vallis buffer price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        public async Task VallisBufferPriceAsync(string commodity, int quantity)
        {
            await VallisBufferPriceAsync(quantity, commodity);

        }

        //[Command("montem")]
        //[Alias("montem")]
        //[Summary("Get corporate price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        //public async Task MontemBufferPriceAsync(int quantity, string commodity)
        //{
        //    var channel = Context.Channel;
        //    if (_publicChannels.Contains(channel.Name))
        //    {
        //        return;
        //    }
        //    if (Context.IsPrivate)
        //    {
        //        var authorized = await IsMember($"{Context.User.Username}#{Context.User.DiscriminatorValue}");
        //        if (!authorized)
        //        {
        //            authorized = await IsMember($"{Context.User.Username}#{Context.User.Discriminator}");
        //            if(!authorized) return;
        //        }
        //    }

        //    var range = await _sheetsProxy.GetRange("1tyYLfgAqD7Mm1Lv8-fc59RuPdPZ_pa0HYjY7TVI_KKo", "PL-VallisMgmt!AI3:AJ");

        //    if (range.Values == null)
        //    {
        //        await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
        //        return;
        //    }

        //    var match = range.Values.FirstOrDefault(i => i[0].ToString() == commodity.ToUpper());
        //    if (match == null || match.Count == 0)
        //    {
        //        await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
        //        return;
        //    }
        //    if (!decimal.TryParse(match[1].ToString(), out var price))
        //    {
        //        await ReplyAsync($"Couldn't find a buffer price on Vallis for {commodity}");
        //        return;
        //    }

        //    var total = price * quantity;
        //    await ReplyAsync($"{quantity} {commodity} = {total} NCC");

        //}

        //[Command("montem")]
        //[Alias("montem")]
        //[Summary("Get montem buffer price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        //public async Task MontemBufferPriceAsync(string commodity)
        //{
        //    await VallisBufferPriceAsync(1, commodity);

        //}

        //[Command("montem")]
        //[Alias("montem")]
        //[Summary("Get montem buffer price for a given commodity. '3 BDE' 'BDE 3' 'BDE' '3 BDE 2400.34' and 'BDE 3 2400.34' are all valid.")]
        //public async Task MontemBufferPriceAsync(string commodity, int quantity)
        //{
        //    await VallisBufferPriceAsync(quantity, commodity);

        //}

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
                var discriminator = FixDiscriminatorValue(Context.User.DiscriminatorValue);
                var authorized = await IsMember($"{Context.User.Username}#{discriminator}");
                if (!authorized)
                {
                    await ReplyAsync($"Couldn't authorize this request... Is {Context.User.Username}#{discriminator} your user name?");
                    return;
                }
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
                var discriminator = FixDiscriminatorValue(Context.User.DiscriminatorValue);
                var authorized = await IsMember($"{Context.User.Username}#{discriminator}");
                if (!authorized)
                {
                    await ReplyAsync($"Couldn't authorize this request... Is {Context.User.Username}#{discriminator} your user name?");
                    return;
                }
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
                _members = values?.Values?.Where(m => m.Count == 1).Select(m => m[0].ToString().ToLower()).ToList();
                

            }
            return _members.Contains(discordId.ToLower());
        }

    }
}
