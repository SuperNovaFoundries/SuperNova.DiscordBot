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

        private ILogger _logger = null;
        private string _name;
        protected ILogger Logger => _logger ??= LogFactory.GetLogger(_name);

        protected LoggingResource(string name)
        {
            MEFLoader.SatisfyImportsOnce(this);
        }
    }
}