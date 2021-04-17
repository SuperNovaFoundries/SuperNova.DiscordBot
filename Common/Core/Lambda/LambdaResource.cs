﻿using Common;
using Microsoft.Extensions.Logging;
using SuperNova.AWS.Logging.Contract;
using System.Composition;
namespace SuperNova.DiscordBot.Common.Core.Lambda
{

    public class LoggingResource
    {
        [Import]
        private IServiceLoggerFactory _logFactory { get; set; } = null;
        private ILogger _logger = null;
        private readonly string _name;
        protected ILogger Logger => _logger ?? (_logger = _logFactory.GetLogger(_name));

        protected LoggingResource(string name)
        {
            MEFLoader.SatisfyImportsOnce(this);
            _name = name;
        }
    }
}