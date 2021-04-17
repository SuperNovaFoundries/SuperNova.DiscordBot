using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using SuperNova.DiscordBot.Data.Contract;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using SuperNova.AWS.Logging;
using SuperNova.MEF.NetCore;
using Microsoft.Extensions.Logging;

namespace SuperNova.DiscordBot.Business.Lambda
{
    public class DiscordBot : LoggingResource
    {

        [Import]
        private IConnectionService _connectionService { get; set; } = null;

        private int TimeToRun { get; } = 894000;

        public DiscordBot() : base(nameof(DiscordBot))
        {
            try
            {
                MEFLoader.SatisfyImportsOnce(this);
            }
            catch(Exception ex)
            {
                throw new Exception("MEF FAILED!!" + ex.Message + ex.StackTrace + ex.InnerException?.Message + ex.InnerException?.StackTrace);
            }
            

        }

        [LambdaSerializer(typeof(JsonSerializer))]
        public async Task RunAsync(ILambdaContext context)
        {
            try
            {
                var waitHandle = new AutoResetEvent(false);
                
                await _connectionService.InitializeAsync(Client_Ready, await GetTokenAsync(), TimeToRun, waitHandle);
                
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

        public async Task<string> GetTokenAsync()
        {
            using var client = new AmazonSecretsManagerClient();
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = "supernova/setup/discordtoken"
            });

            if (response.SecretString == null)
            {
                Logger.LogError("FATAL: Unable to retrieve discord token!!");
                return string.Empty;
            }
            return response.SecretString;
        }


    }



}
