using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperNova.DiscordBot.Contract
{
    public interface IConnectionService
    {
        DiscordSocketClient Client { get; }
        CommandService Commands { get; }
        ServiceProvider Services { get; }
        bool PendingDisconnect { get; }

        Task InitializeAsync(Func<Task> OnReady, string token, int timeToRun, EventWaitHandle waitHandle);
        Task DisconnectAsync();
    }



}
