using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using SuperNova.AWS.Logging;
using SuperNova.MEF.NetCore;
using Microsoft.Extensions.Logging;
using SuperNova.DiscordBot.Contract;

namespace SuperNova.DiscordBot.Lambda
{
    public class DiscordBotLambda : LoggingResource
    {

        [Import]
        private IConnectionService _connectionService { get; set; } = null;

        /// <summary>
        /// Time to run in ms
        /// </summary>
        private int TimeToRun { get; } = 885000;

        public DiscordBotLambda() : base(nameof(DiscordBotLambda))
        {
            MEFLoader.SatisfyImportsOnce(this);
        }

        [LambdaSerializer(typeof(JsonSerializer))]
        public async Task RunAsync(ILambdaContext context)
        {
            Logger.LogInformation("Disconnecting in case we didn't get a chance to before.");
            await _connectionService.DisconnectAsync();
            
            try
            {
                var waitHandle = new AutoResetEvent(false);
                Logger.LogInformation($"Connecting - Timer set to {TimeToRun} ms.");
                await _connectionService.InitializeAsync(Client_Ready, await GetTokenAsync(), TimeToRun, waitHandle);
                waitHandle.WaitOne();
                Logger.LogInformation("Disconnecting - Lambda timout reached.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unhandled error: {ex.Message}. Stack Trace: {ex.StackTrace}");
                Logger.LogError($"Discord bot exited unexpectedly!");
            }
            finally
            {
                await _connectionService.DisconnectAsync();
            }
        }

        public async Task Client_Ready() => await _connectionService.Client.SetGameAsync("Chasing electrons");
        
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
