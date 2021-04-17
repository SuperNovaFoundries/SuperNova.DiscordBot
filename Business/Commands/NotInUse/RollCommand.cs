//using Discord.Commands;
//using SuperNova.DiscordBot.Business.Modules.Dice;
//using SuperNova.DiscordBot.Data.Core;
//using System.Threading.Tasks;
//using System.Net;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.Extensions.Logging;
//using SuperNova.DiscordBot.Common.Core;
//using SuperNova.DiscordBot.Common.Utils;
//using System;
//using System.Composition;
//using SuperNova.AWS.Logging.Contract;

//namespace DiscordBot.Business.Commands
//{
//	[DiscordCommand]
//	public class RollCommand : ModuleBase<SocketCommandContext>
//	{
//		[Import]
//		private IServiceLoggerFactory _logFactory { get; set; } = null;
//		private ILogger _logger = null;
//		private Dictionary<string, string> nickNames { get; set; } = new Dictionary<string, string>
//		{
//			{"KeyserSoze", "Jacques"},
//			{"Gen. Lee Bad", "Holgar" },
//			{"K-Hop", "The DM" },
//			{"ZSA", "Dragar" },
//			{"Hopper", "Unuuth" },
//			{"Noikkor", "Badric" },
//		};


//		[Command("roll")]
//		[Alias("r")]
//		[Summary("Roll some dice!")]
//		public async Task RollAsync([Summary("e.g. 2d6+5")] string expression)
//		{
//			try
//			{
//				var key = nickNames.Keys.FirstOrDefault(k => Context.User.Username == k || Context.User.Username.Contains(k));
//				var name = !key.IsNullOrEmpty()
//					? nickNames[key]
//					: Context.User.Username;
//				var result = new Dice().Roll(expression);
				
//				if(result.Valid)
//				{
//					var response = $"```css\n [{name}] rolled [{expression}] and got [{result.Result}]. {{{result.Arithmetic}}}\n```";
//					if (response.Length >= 2000) throw new Exception();
//					await ReplyAsync(response);
//				}
//				else
//				{
//					throw new Exception(result.ToJsonString());
//				}
//			}
//			catch (Exception ex)
//			{
//				await ReplyAsync("\"That seems unnecessary.\" \n\t -Zach, 2019\n");
//				await ReplyAsync(ex.ToJsonString());
//			}
			

//		}

//		[Command("roll")]
//		[Alias("r")]
//		[Summary("Roll some dice!")]
//		public async Task RollAsync([Summary("e.g. 2d6+5")] string expression, string reason)
//		{

//			var key = nickNames.Keys.FirstOrDefault(k => Context.User.Username == k || Context.User.Username.Contains(k));
//			var name = !key.IsNullOrEmpty() 
//				? nickNames[key] 
//				: Context.User.Username;

//			//await Context.Message.DeleteAsync(new Discord.RequestOptions
//			//{
//			//	AuditLogReason = "Deleting Message to reduce dice roll verbosity."
//			//});

//			_logger.LogInformation($"Username that rolled: {Context.User.Username}, Expression: {expression}, Reason: {reason}");

//			try
//			{
//				var result = new Dice().Roll(expression);
//				if (result.Valid)
//				{
//					var response = $"```css\n [{name}] rolled [{reason}] and got [{result.Result}]. {{{result.Arithmetic}}}\n```";
//					if (response.Length >= 2000) throw new Exception();
//					await ReplyAsync(response);
//				}
//				else
//				{
//					throw new Exception();
//				}
//			}
//			catch (Exception ex)
//			{

//				await ReplyAsync("\"That seems unnecessary.\" \n\t -Zach, 2019\n");
//				await ReplyAsync(ex.Message + ex.StackTrace);
//			}
			

//		}
//	}


//}
