using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System.IO;
using System.Linq;
using System.Net.Http;
using DSharpPlus.VoiceNext.EventArgs;
using System.Threading;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using R128.Lufs;

using Timer = System.Timers.Timer;

namespace Choker
{
    [Group("snitch"), Aliases("s")]
    [Description("Commands to protect a voice chat from loud people.")]
    public class ChokeModule : BaseCommandModule
    {
        private const int InfoLevel = 0;
        private const int UseLevel = 1;
        private const int ManagementLevel = 2;

        private const double AudioSampleRate = 48000.0;
        private const double LUFSToPercent = 10.0 / 7.0;

        private ChokeDataStash stash = new ChokeDataStash();

        [Command("start"), Aliases("join", "j", "+")]
        [RequireGroupLevel(UseLevel)]
        [Description("Start watching over a channel specified or the one that you're currently in.")]
        public async Task StartCommand(
            CommandContext ctx,
            [Description("A channel to join. Leave blank if you want the bot to join the channel you're sitting in.")]
            [RemainingText]
            string channel = null)
        {
            var connection = ctx.Client.GetVoiceNext().GetConnection(ctx.Guild);
            if (connection?.TargetChannel != null)
            {
                await ctx.RespondAsync($"The bot is already in a channel `{connection.TargetChannel.Name}`.");
                return;
            }

            DiscordChannel ch = ResolveChannel(ctx.Guild, channel);
            ch ??= ctx.Member.VoiceState?.Channel;
            
            if (ch == null)
            {
                await ctx.RespondAsync("Couldn't find a channel to join.");
                return;
            }

            stash.StartServerSession(ctx.Guild.Id);
            connection = await ch.ConnectAsync();
            connection.VoiceReceived += VoiceReceiveHandler;
        }

        [Command("move"), Aliases("m", ">")]
        [RequireGroupLevel(UseLevel)]
        [Description("Move bot to the channel specified.")]
        public async Task MoveCommand(
            CommandContext ctx,
            [Description("A channel to move to. Leave blank if you want the bot to move to the channel you're sitting in.")]
            [RemainingText]
            string channel = null) 
        {
            var connection = ctx.Client.GetVoiceNext().GetConnection(ctx.Guild);
            if (connection?.TargetChannel == null)
            {
                await ctx.RespondAsync($"The bot is not currently in a channel.");
                return;
            }

            DiscordChannel ch = ResolveChannel(ctx.Guild, channel);
            ch ??= ctx.Member.VoiceState?.Channel;

            if (ch == null)
            {
                await ctx.RespondAsync("Couldn't find a channel to move to.");
                return;
            }

            connection.VoiceReceived -= VoiceReceiveHandler;
            connection.Dispose();
            connection = await ch.ConnectAsync();
            connection.VoiceReceived += VoiceReceiveHandler;
        }


        private DiscordChannel ResolveChannel(DiscordGuild guild, string input)
        {
            if (input == null)
                return null;

            if (ulong.TryParse(input, out ulong id))
                if (guild.Channels.ContainsKey(id))
                    return guild.Channels[id];

            int min = int.MaxValue;
            DiscordChannel result = null;
            DiscordChannel[] channels = guild.Channels.Values.Where(ch => ch.Type == ChannelType.Voice && ch.Id != guild.AfkChannel.Id).ToArray();
            foreach (var channel in channels)
            {
                int tmp = Utils.Levenshtein(channel.Name, input);
                if (tmp < min)
                {
                    min = tmp;
                    result = channel;
                }
            }
            return result;
        }

        [Command("stop"), Aliases("leave", "l", "-")]
        [RequireGroupLevel(UseLevel)]
        [Description("Leave from the channel the bot is currently in.")]
        public async Task StopCommand(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();

            var connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                await ctx.RespondAsync("Not snitching right now.");
            }
            else
            {
                connection.VoiceReceived -= VoiceReceiveHandler;
                connection.Dispose();
                stash.Remove(ctx.Guild.Id);
            }
        }

        [Command("statistics"), Aliases("stats", "stat")]
        [RequireGroupLevel(InfoLevel)]
        [Description("Lookup the statistics for a user.")]
        public async Task StatisticsCommand(
            CommandContext ctx,
            [Description("A user to look for. Leave blank to look for youself.")]
            DiscordUser user = null)
        {
            user ??= ctx.User;
            await ctx.RespondAsync($"{user.Username}#{user.Discriminator} got choked {await DataBase.GetUserChokes(user.Id)} times.");
        }

        [Command("top"), Aliases("t")]
        [RequireGroupLevel(InfoLevel)]
        [Description("Get a top of users choked.")]
        public async Task TopCommand(
            CommandContext ctx,
            [Description("A number of users to look for. Defaults to 5. Must be in range from 1 to 30.")]
            int number = 5)
        {
            if (number < 1)
                number = 1;
            if (number > 30)
                number = 30;

            var members = (await ctx.Guild.GetAllMembersAsync()).ToList();

            var top = await DataBase.GetTop(members.Select(m => m.Id).ToArray(), number);
            
            string response = $"All-time top {number} of people choked (this includes chokes on all servers):\n";
            for (int i = 0; i < top.Count; i++)
            {
                var user = members.Find(m => m.Id == top[i].Key);
                response += $"`{(i < 9 && number > 9 ? " " : "") + (i + 1)}.` {user.Username}#{user.Discriminator}: {top[i].Value}";
                if (i < number - 1)
                    response += '\n';
            }

            for (int i = top.Count; i < number; i++)
            {
                response += $"`{(i < 9 && number > 9 ? " " : "") + (i + 1)}.` ---------------";
                if (i < number - 1)
                    response += '\n';
            }

            await ctx.RespondAsync(response);
        }

        [Command("threshold"), Aliases("thd")]
        [RequireGroupLevel(ManagementLevel)]
        [Description("Set a threshold of how loud a person needs to be to get muted. Range of 0-100, real number, measured in percent. Requires an *administrator* permisson to execute.")]
        public async Task ThresholdCommand(
            CommandContext ctx,
            [Description("A real, dot-separated number in range from 0 to 100.")]
            double percent)
        {
            if (percent < 0.0 || percent > 100.0)
            {
                await ctx.RespondAsync($"The value must be between 0 and 100.\nGiven: `{percent}`.");
                return;
            }

            await DataBase.SetServerMaxLoudness(ctx.Guild.Id, percent);
            await ctx.RespondAsync($"Setting a threshold for this server to {string.Format("{0:0.##}", percent)}%.");
        }

        [Command("interval"), Aliases("int")]
        [RequireGroupLevel(ManagementLevel)]
        [Description("Set an interval of how long must a loud sound be for a person get muted. Range of 20-1000, integer number, measured in milliseconds. Requires an *administrator* permisson to execute.")]
        public async Task IntervalCommand(
            CommandContext ctx,
            [Description("An integer number in range from 20 to 1000.")]
            int interval)
        {
            if (interval < 20 || interval > 1000)
            {
                await ctx.RespondAsync($"The value must be between 20 and 1000.\nGiven: `{interval}`.");
                return;
            }

            await DataBase.SetServerInterval(ctx.Guild.Id, interval);
            await ctx.RespondAsync($"Setting an interval for this server to {interval} ms.");
        }

        [Command("mutetime"), Aliases("mute_time", "mute-time", "mt")]
        [RequireGroupLevel(ManagementLevel)]
        [Description("Set a mute time for ones that got muted. -1 means infinity, up to 300'000. 0 is not allowed. Measured in milliseconds. Requires an *administrator* permisson to execute.")]
        public async Task MuteTimeCommand(
            CommandContext ctx,
            [Description("An integer number in range from -1 to 300'000, excluding 0.")]
            int muteTime)
        {
            if (muteTime < -1 && muteTime > 300000 || muteTime == 0)
            {
                await ctx.RespondAsync($"The value must be between -1 and 300'000, excluding 0.\nGiven: `{muteTime}`.");
                return;
            }

            await DataBase.SetServerMuteTime(ctx.Guild.Id, muteTime);
            await ctx.RespondAsync($"Setting mute time for this server to {(muteTime == -1 ? "∞" : string.Format("{0:0.##} s", muteTime / 1000.0))}.");
        }

        [Command("configuration"), Aliases("config", "info", "i")]
        [RequireGroupLevel(InfoLevel)]
        [Description("Outputs current server's configuration.")]
        public async Task InfoCommand(CommandContext ctx)
        {
            var configuration = await DataBase.GetServerConfiguration(ctx.Guild.Id);

            await ctx.RespondAsync(
                $"`{ctx.Guild.Name}`\n" +
                $"Loudness threshold: {string.Format("{0:0.##}", configuration.MaxLoudness)}%.\n" +
                $"Interval: {configuration.Interval} ms.\n" +
                $"Mute time: {(configuration.MuteTime == -1 ? "∞" : string.Format("{0:0.##} s", configuration.MuteTime / 1000.0))}.");
        }

        [Command("session"), Aliases("s")]
        [RequireGroupLevel(InfoLevel)]
        [Description("Outputs current session's statistics.")]
        public async Task SessionCommand(CommandContext ctx)
        {
            var serverSessionStash = stash.GetServerSessionStash(ctx.Guild.Id);

            if (serverSessionStash == null)
            {
                await ctx.RespondAsync("Not snitching right now.");
                return;
            }
            var topChokedUsers = (from value in serverSessionStash
                                  orderby value.Value.SessionChokes descending
                                  select value).ToList();

            string response =
                $"Started snitching at {serverSessionStash.SessionStart.ToString("dd.MM.yy H:mm:ss")}.\n" +
                $"Choked users {serverSessionStash.Sum(x => x.Value.SessionChokes)} times since then.\n" +
                $"Top 5 choked users:\n";

            for (int i = 0; i < topChokedUsers.Count; i++)
            {
                var member = await ctx.Guild.GetMemberAsync(topChokedUsers[i].Key);
                response += $"`{i + 1}.` {member.Username}#{member.Discriminator}: {topChokedUsers[i].Value.SessionChokes}";
                if (i < 4)
                    response += '\n';
            }

            for (int i = topChokedUsers.Count; i < 5; i++)
            {
                response += $"`{i + 1}.` ---------------";
                if (i < 4)
                    response += '\n';
            }

            await ctx.RespondAsync(response);
        }
        private async Task VoiceReceiveHandler(VoiceNextConnection connection, VoiceReceiveEventArgs args)
        {
            var guild = connection.TargetChannel.Guild;
            var user = args.User;

            if (guild == null || user == null)
                return;

            var configuration = await DataBase.GetServerConfiguration(guild.Id);
            UserChokeData userData = await stash[guild.Id].GetOrCreateUserData(user.Id);

            if (ProcessSoundData(configuration, userData, args.PcmData.ToArray()))
            {
                var member = await guild.GetMemberAsync(user.Id);
                await member.SetMuteAsync(true, $"Being louder than {string.Format("{0:0.##}", configuration.MaxLoudness)}% for {configuration.Interval} ms.");
                userData.Count = 0;
                userData.Time = DateTime.Now;
                userData.SessionChokes++;
                userData.LastMute = DateTime.Now;
                await DataBase.SetUserChokes(user.Id, ++userData.Chokes);
                if (configuration.MuteTime != -1)
                {
                    var timer = new Timer()
                    {
                        AutoReset = false,
                        Interval = configuration.MuteTime
                    };
                    timer.Elapsed += async (s, e) =>
                    {
                        await member.SetMuteAsync(false, $"{configuration.MuteTime} ms expired.");
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }
        private bool ProcessSoundData(DataBase.ServerConfiguration configuration, UserChokeData userData, byte[] data)
        {
            if (DateTime.Now.Subtract(userData.Time).TotalMilliseconds < 30.0)
            {
                if (userData.Count >= configuration.Interval / 20)
                    userData.PcmData.RemoveRange(0, data.Length);
                else
                    userData.Count++;
                userData.PcmData.AddRange(data);
            }
            else
            {
                userData.PcmData.Clear();
                userData.PcmData.AddRange(data);
                userData.Count = 1;
            }

            userData.Time = DateTime.Now;

            if (userData.Count >= configuration.Interval / 20)
                if (Loudness(userData.PcmData.ToArray()) > configuration.MaxLoudness &&
                    DateTime.Now.Subtract(userData.LastMute).TotalMilliseconds > 500.0)
                    return true;

            return false;
        }
        private double Loudness(byte[] pcmData)
        {
            List<double> data = new List<double>();
            for (int i = 0; i < pcmData.Length; i += 2)
                data.Add((double)BitConverter.ToInt16(pcmData, i) / (short.MaxValue + 1));
            R128LufsMeter r128LufsMeter = new R128LufsMeter();
            r128LufsMeter.Prepare(AudioSampleRate, 1);
            r128LufsMeter.StartIntegrated();
            r128LufsMeter.ProcessBuffer(new double[][] { data.ToArray() }, null);
            r128LufsMeter.StopIntegrated();

            double loudness = r128LufsMeter.IntegratedLoudness;
            
            if (double.IsNaN(loudness))
                return 0;
            if (loudness <= -70.0)
                return 0;
            if (loudness >= 0.0)
                return 100.0;
            return (loudness + 70.0) * LUFSToPercent;
        }
    }
}
