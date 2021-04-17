//using Amazon.Translate.Model;
//using Discord.Commands;
//using DiscordBot.Data.Core;
//using System.Threading.Tasks;
//using Amazon.Translate;
//using DiscordBot.Common.Core;
//using Microsoft.Extensions.Logging;
//using DiscordBot.Common.Utils;
//using System.Collections.Generic;
//using System.Linq;
//using System;
//using System.Net;

//namespace DiscordBot.Business.Commands
//{
//	[DiscordCommand]
//	public class TranslateCommand : ModuleBase<SocketCommandContext>
//	{
//		private Dictionary<string, string> _languageMap = new Dictionary<string, string>
//		{
//			{"Arabic", "ar"},
//			{"Chinese(s)" , "zh"},
//			{"Chinese(t)" , "zh-TW"},
//			{"Czech" , "cs"},
//			{"Danish" , "da"},
//			{"Dutch" , "nl"},
//			{"English" , "en"},
//			{"Finnish" , "fi"},
//			{"French" , "fr"},
//			{"German" , "de"},
//			{"Hebrew" , "he"},
//			{"Indonesian" , "id"},
//			{"Italian" , "it"},
//			{"Japanese" , "ja"},
//			{"Korean" , "ko"},
//			{"Polish" , "pl"},
//			{"Portuguese" , "pt"},
//			{"Russian" , "ru"},
//			{"Spanish" , "es"},
//			{"Swedish" , "sv"},
//			{"Turkish" , "tr"},
//		};

//		public string GetHelp() => $"You can translate text to/from the following languages: {string.Join(", ", _languageMap.Keys.ToList())}. Simply try !translate Russian \"Your mother dresses you funny.\"";

//		private ILogger Log = Logger.GetLogger(nameof(TranslateCommand));

//		[Command("translate")]
//		[Summary("Translate some text!")]
//		public async Task TranslateAsync([Summary("!translate Russian \"Hello there!\"")] string to, string toTranslate)
//		{
//			if (!_languageMap.Keys.Contains(to))
//			{
//				await ReplyAsync("Huh? " + GetHelp());
//				return;
//			}
//			try
//			{
//				var result = await new AmazonTranslateClient().TranslateTextAsync(new TranslateTextRequest
//				{
//					SourceLanguageCode = "auto",
//					TargetLanguageCode = _languageMap[to],
//					Text = toTranslate
					
//				});
//				if(result.HttpStatusCode != HttpStatusCode.OK)
//				{
//					throw new Exception(result.ToJsonString());
//				}

//				Log.LogInformation($"{result.ToJsonString()}");

//				await ReplyAsync(result.TranslatedText);
//			}
//			catch(Exception ex)
//			{
//				Log.LogError(ex.ToJsonString());
//				await ReplyAsync("Uh oh, something went wrong... Check the logs.");
//			}
//		}

//		[Command("translate")]
//		[Summary("Translate some text!")]
//		public async Task TranslateAsync() => await ReplyAsync(GetHelp());
		

//	}


//}
