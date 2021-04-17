using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using DiscordBot.Data.Contract;
using Common;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.IO;
using Microsoft.Extensions.Logging;
using System;
using SuperNova.DiscordBot.Common.Core.Lambda;

namespace SuperNova.DiscordBot.Business.Lambda
{
    public class DiscordBot : LoggingResource
    {

        [Import]
        private IConnectionService _connectionService { get; set; } = null;

        private int TimeToRun { get; } = 894000;

        public DiscordBot() : base("SuperNova.DiscordBot")
        {
            MEFLoader.SatisfyImportsOnce(this);

        }

        [LambdaSerializer(typeof(JsonSerializer))]
        public async Task RunAsync(ILambdaContext context)
        {
            try
            {
                var waitHandle = new AutoResetEvent(false);

                using var client = new AmazonSecretsManagerClient();
                var response = await client.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = "supernova/setup/discordtoken"
                });

                if (response.SecretString == null)
                {
                    Logger.LogError("FATAL: Unable to retrieve discord token!!");
                    return;
                }
                await _connectionService.InitializeAsync(Client_Ready, response.SecretString, TimeToRun, waitHandle);

                waitHandle.WaitOne();

                await _connectionService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unhandled error: {ex.Message}. Stack Trace: {ex.StackTrace}");
                Logger.LogError($"Discord bot exited unexpectedly!");
            }
        }

        public async Task Client_Ready()
        {
            await _connectionService.Client.SetGameAsync("Assimilating nueral interactions...");
        }


    }



}
