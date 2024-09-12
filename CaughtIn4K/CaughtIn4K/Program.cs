using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    private DiscordSocketClient _client;
    private CommandHandler _commandHandler;

    static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public async Task RunBotAsync()
    {
        var services = ConfigureServices();
        _client = services.GetRequiredService<DiscordSocketClient>();
        _client.Log += Log;

        string token = "MTI3ODA2Njc4NjI0MzMxMzc0OA.Gmjoe-.dTNOgA-66j_NlEyfOdvU8u1uPBTzGpLjJ4b86c";
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _commandHandler = services.GetRequiredService<CommandHandler>();
        await _commandHandler.InstallCommandsAsync();

        await Task.Delay(-1);
    }

    private static ServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandler>()
            .AddSingleton<AudioService>()
            .BuildServiceProvider();
    }

    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
}