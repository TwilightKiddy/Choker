using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Choker
{
    public class CommandErroredModule
    {
        public static async Task CmdErroredHandler(CommandsNextExtension _, CommandErrorEventArgs e)
        {
            var exception = e.Exception;
            if (exception is ChecksFailedException checksFailedException)
                foreach (var failedCheck in checksFailedException.FailedChecks)
                {
                    if (failedCheck is RequireGroupLevelAttribute groupLevelAttribute)
                        await e.Context.RespondAsync($"You must be a member of a group with permisson level of at least {groupLevelAttribute.RequiredLevel}.");
                    else if (failedCheck is RequirePermissionsAttribute permissionsAttribute)
                        if ((permissionsAttribute.Permissions & Permissions.Administrator) != 0)
                            await e.Context.RespondAsync($"This command requires an *administrator* permission to execute.");
                        else
                            await e.Context.RespondAsync("You lack the permissions to run this command.");
                }
            else if (exception is CommandNotFoundException notFoundException)
                await e.Context.RespondAsync($"Command `{notFoundException.CommandName}` does not exist. Try `help`.");
        }
    }
}
