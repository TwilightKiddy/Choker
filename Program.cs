using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Choker
{
    class Program
    {
        private const string TokenPlaceholder = "XXXXXXXXXXXXXXXXXXXXXXXX.XXXXXX.XXXXXXXXXXXXXXXXXXXXXXXXXXX";
        private struct Settings
        {
            public string Token { get; set; }
            public TokenType TokenType { get; set; }
            public string Activity { get; set; }
            public ActivityType? ActivityType { get; set; }
            public string[] DefaultPrefixes { get; set; }
        }

        public static void Main()
            => MainAsync().GetAwaiter().GetResult();
        private static async Task MainAsync()
        {
            Settings settings;

            try
            {
                settings = await GetSettings();
            }
            catch
            {
                Console.WriteLine("An error accured while reading 'settings.json'.");
                Console.ReadKey();
                return;
            }

            if(settings.Token == TokenPlaceholder)
            {
                Console.WriteLine("A new 'settings.json' was generated. Edit it so the token matches your bot's.");
                Console.ReadKey();
                return;
            }

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = settings.Token,
                TokenType = settings.TokenType
            });

            PrefixModule.AddDefaultPrefixes(settings.DefaultPrefixes);

            discord.UseVoiceNext(new VoiceNextConfiguration()
            {
                EnableIncoming = true
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                PrefixResolver = new PrefixResolverDelegate(PrefixModule.PrefixResolver),
                EnableMentionPrefix = true
            });

            await DataBase.Initialize();

            commands.CommandErrored += CommandErroredModule.CmdErroredHandler;

            commands.RegisterCommands<ChokeModule>();
            commands.RegisterCommands<PrefixModule>();
            commands.RegisterCommands<GroupPermissionsModule>();

            if (settings.ActivityType == null)
                await discord.ConnectAsync();
            else
                await discord.ConnectAsync(new DiscordActivity(settings.Activity, settings.ActivityType.Value));

            await Task.Delay(-1);

        }

        private static async Task<Settings> GetSettings()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            Settings settings;

            if(File.Exists("settings.json"))
                try
                {
                    using (var readerStream = File.OpenRead("settings.json"))
                    {
                        settings = await JsonSerializer.DeserializeAsync<Settings>(readerStream, options);
                        await readerStream.FlushAsync();
                    }
                    settings.DefaultPrefixes = settings.DefaultPrefixes.Where(x => x != "").ToArray();
                }
                catch
                {
                    throw new Exception("Error reading JSON file.");
                }
            else
            {
                settings = new Settings {
                    Token = TokenPlaceholder,
                    TokenType = TokenType.Bot,
                    Activity = "",
                    ActivityType = null,
                    DefaultPrefixes = new string[] { "!" }
                };
            }

            if (!File.Exists("settings.json"))
                using (var streamWriter = new StreamWriter("settings.json"))
                {
                    try
                    {
                        await streamWriter.WriteAsync(JsonSerializer.Serialize(settings, options));
                    }
                    finally
                    {
                        await streamWriter.FlushAsync();
                        streamWriter.Close();
                    }
                }

            return settings;
        }
    }
}
