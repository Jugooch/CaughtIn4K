using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

public class RequireAdmin : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var user = context.User as IGuildUser;
        if (user == null)
        {
            return PreconditionResult.FromError("Command can only be run in a server context.");
        }

        if (user.GuildPermissions.Administrator)
        {
            return PreconditionResult.FromSuccess();
        }

        return PreconditionResult.FromError("You do not have the necessary permissions to run this command.");
    }
}