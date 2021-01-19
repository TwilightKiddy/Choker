using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;

namespace Choker
{
    [Group("permissions"), Aliases("permission", "perm", "perms", "p"), RequirePermissions(Permissions.Administrator)]
    [Description("Commands related to assigning permissons to groups. Requires an *administrator* permisson to execute.")]
    public class GroupPermissionsModule : BaseCommandModule
    {
        [Command("list"), Aliases("ls", "l")]
        [Description("Lists all the groups on the server.")]
        internal async Task ListCommand(CommandContext ctx)
        {
            string response = 
                "```\n" +
                "   # Lvl Color   Name\n";
            var roles = GetTimeSortedRoles(ctx.Guild);
            var roleLevels = await DataBase.GetRoleLevels(roles.Select(r => r.Id).ToArray());
            for (int i = 0; i < roles.Count; i++)
            {
                response += string.Format("{0,3}. {1,3} {2} {3}\n",
                    (i + 1).ToString("###"),
                    roleLevels.GetValueOrDefault(roles[i].Id, (uint)0).ToString("##0"),
                    roles[i].Color,
                    roles[i].Name.Replace("@", ""));
            }
            response += "```";
            await ctx.RespondAsync(response);
        }

        [Command("set"), Aliases("s")]
        [Description("Assigns a permission level to a role.")]
        internal async Task SetCommand(
            CommandContext ctx,
            [Description("A level that should be assigned to a role. Must be in range from 0 to 999.")]
            uint level,
            [RemainingText]
            [Description("Target role. May be a number from the `list`, may be an ID, may be a name.")]
            string role)
        {
            if (level < 0)
                level = 0;
            if (level > 999)
                level = 999;

            DiscordRole targetRole = ResolveRole(ctx.Guild, role);
            if (targetRole == null)
            {
                await ctx.RespondAsync($"Couldn't find role `{role}`.");
                return;
            }

            await DataBase.SetRoleLevel(targetRole.Id, level);
            await ctx.RespondAsync($"Setting permission level of role {targetRole.Name} to {level}.");
        }

        private DiscordRole ResolveRole(DiscordGuild guild, string input)
        {
            if (input == null)
                return null;

            var roles = GetTimeSortedRoles(guild);

            if (ulong.TryParse(input, out ulong num))
            {
                if (num <= (ulong)roles.Count && num != 0)
                    return roles[(int)(num - 1)];
                var tmp = roles.Find(r => r.Id == num);
                if (tmp != null)
                    return tmp;
            }

            int min = int.MaxValue;
            DiscordRole result = null;
            foreach (var role in roles)
            {
                int tmp = Utils.Levenshtein(role.Name, input);
                if (tmp < min)
                {
                    min = tmp;
                    result = role;
                }
            }
            return result;
        }

        internal static List<DiscordRole> GetTimeSortedRoles(DiscordGuild guild)
        {
            var result = new List<DiscordRole>(guild.Roles.Values);
            result.Sort((leftRole, rightRole) =>
            {
                if (leftRole.CreationTimestamp == rightRole.CreationTimestamp)
                    return 0;
                if (leftRole.CreationTimestamp > rightRole.CreationTimestamp)
                    return 1;
                else
                    return -1;
            });
            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireGroupLevelAttribute : CheckBaseAttribute
    {
        public int RequiredLevel { get; private set; }

        public RequireGroupLevelAttribute(int level)
        {
            RequiredLevel = level;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            => await DataBase.GetRoleLevelHighest(ctx.Member.Roles.Select(role => role.Id).ToArray()) >= RequiredLevel;
    }
}
