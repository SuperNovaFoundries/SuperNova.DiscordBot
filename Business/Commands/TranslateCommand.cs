using Discord.Commands;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Net;
using SuperNova.DiscordBot.Data.Core;
using SuperNova.MEF.NetCore;
using SuperNova.AWS.Logging.Contract;
using System.Composition;
using SuperNova.DiscordBot.Common.Utils;
using Amazon.Translate;
using Amazon.Translate.Model;

namespace DiscordBot.Business.Commands
{


    [DiscordCommand]
    public class TranslateCommand : ModuleBase<SocketCommandContext>
    {
        [Import]
        private IServiceLoggerFactory LogFactory { get; set; } = null;
        private ILogger Logger { get; }

        private Dictionary<string, string> _languageMap = new Dictionary<string, string>
        {
            {"Arabic", "ar"},
            {"Chinese(s)" , "zh"},
            {"Chinese(t)" , "zh-TW"},
            {"Czech" , "cs"},
            {"Danish" , "da"},
            {"Dutch" , "nl"},
            {"English" , "en"},
            {"Finnish" , "fi"},
            {"French" , "fr"},
            {"German" , "de"},
            {"Hebrew" , "he"},
            {"Indonesian" , "id"},
            {"Italian" , "it"},
            {"Japanese" , "ja"},
            {"Korean" , "ko"},
            {"Polish" , "pl"},
            {"Portuguese" , "pt"},
            {"Russian" , "ru"},
            {"Spanish" , "es"},
            {"Swedish" , "sv"},
            {"Turkish" , "tr"},
        };

        public string GetHelp() => $"You can translate text to/from the following languages: {string.Join(", ", _languageMap.Keys.ToList())}. Try !translate Russian \"Your mother dresses you funny.\"";


        [Command("translate")]
        [Summary("Translate some text!")]
        public async Task TranslateAsync([Summary("!translate Russian \"In Soviet Russia, car drives you!!\"")] string to, string toTranslate)
        {
            if (!_languageMap.Keys.Contains(to))
            {
                await ReplyAsync("Huh? " + GetHelp());
                return;
            }
            try
            {
                var result = await new AmazonTranslateClient().TranslateTextAsync(new TranslateTextRequest
                {
                    SourceLanguageCode = "auto",
                    TargetLanguageCode = _languageMap[to],
                    Text = toTranslate

                });
                if (result.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(result.ToJsonString());
                }

                Logger.LogInformation($"{result.ToJsonString()}");

                await ReplyAsync(result.TranslatedText);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToJsonString());
                await ReplyAsync("Uh oh, something went wrong... Check the logs.");
            }
        }

        [Command("translate")]
        [Summary("Translate some text!")]
        public async Task TranslateAsync() => await ReplyAsync(GetHelp());

        public TranslateCommand()  {
            MEFLoader.SatisfyImportsOnce(this);
            Logger = LogFactory.GetLogger(nameof(TranslateCommand));
        }
    }


}
