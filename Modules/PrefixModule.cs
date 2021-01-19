using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Choker
{
    [Group("prefix"), RequireUserPermissions(Permissions.Administrator)]
    [Description("Prefix-related commands. Requires an *administrator* permisson to execute.")]
    internal class PrefixModule : BaseCommandModule
    {
        private static List<string> DefaultPrefixes = new List<string>();

        [Command("add"), Aliases("a", "+")]
        [Description("Adds all given prefixes. If there is at least one prefix present for the server, bot won't react to default ones.")]
        internal async Task PrefixAddCommand(
            CommandContext ctx,
            [Description("A space-separated list of prefixes to be added.")]
            params string[] args)
        {
            string[] prefixes = await DataBase.GetServerPrefixes(ctx.Guild.Id);
            prefixes = prefixes.Union(args.Where(x => x != "")).ToArray();
            await DataBase.SetServerPrefixes(ctx.Guild.Id, prefixes);
            await ctx.RespondAsync(
                "Updated prefixes.\n\n" +
                "Current prefix list:\n" +
                PrettyPrintPrefixes(await GetServerPrefixes(ctx.Guild.Id)));
        }

        [Command("remove"), Aliases("rem", "r", "-")]
        [Description("Removes all given prefixes. If no prefixes are left, bot falls for the default ones.")]
        internal async Task PrefixRemoveCommand(
            CommandContext ctx,
            [Description("A space-separated list of prefixes to be removed.")]
            params string[] args)
        {
            string[] prefixes = await DataBase.GetServerPrefixes(ctx.Guild.Id);
            prefixes = prefixes.Except(args).ToArray();
            await DataBase.SetServerPrefixes(ctx.Guild.Id, prefixes);
            await ctx.RespondAsync(
                "Updated prefixes.\n\n" +
                "Current prefix list:\n" +
                PrettyPrintPrefixes(await GetServerPrefixes(ctx.Guild.Id)));
        }

        [Command("list"), Aliases("ls", "l")]
        [Description("Lists all current prefixes.")]
        internal async Task PrefixListCommand(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "Updated prefixes.\n\n" +
                "Current prefix list:\n" +
                PrettyPrintPrefixes(await GetServerPrefixes(ctx.Guild.Id)));
        }

        private string PrettyPrintPrefixes(string[] prefixes)
        {
            string result = "";
            for (int i = 0; i < prefixes.Length; i++)
            {
                result += '`' + prefixes[i] + '`';
                if (i < prefixes.Length - 1)
                    result += ' ';
            }
            return result;
        }

        internal static void AddDefaultPrefixes(string[] prefixes)
            => DefaultPrefixes.AddRange(prefixes);

        internal static async Task<int> PrefixResolver(DiscordMessage msg)
        {
            int tmp;
            foreach (var prefix in await GetServerPrefixes(msg.Channel.GuildId))
            {
                tmp = msg.GetStringPrefixLength(prefix);
                if (tmp != -1)
                    return tmp;
            }

            return -1;
        }

        private static async Task<string[]> GetServerPrefixes(ulong serverId)
        {
            string[] serverPrefixes = await DataBase.GetServerPrefixes(serverId);
            if (serverPrefixes.Length != 0)
                return serverPrefixes;
            else
                return DefaultPrefixes.ToArray();
        }
    }
}
