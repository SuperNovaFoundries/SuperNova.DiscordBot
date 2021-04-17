using Common;
using Microsoft.Extensions.Logging;
using SuperNova.AWS.Logging.Contract;
using System.Composition;
namespace SuperNova.DiscordBot.Common.Core.Lambda
{

    public class LoggingResource
    {
        [Import]
        private IServiceLoggerFactory LogFactory { get; set; } = null;
        private readonly string _name;
        protected ILogger Logger { get; }

        public LoggingResource()
        {
            MEFLoader.SatisfyImportsOnce(this);
            _name = "LoggingResource";
        }
        protected LoggingResource(string name)
        {
            MEFLoader.SatisfyImportsOnce(this);

            _name = name;
            Logger = LogFactory.GetLogger(_name);
            Logger.LogInformation(_name);
        }
    }
}