using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Choker.Properties;

namespace Choker
{
    internal static class DataBase
    {
        private static SqliteConnection Connection = new SqliteConnection("Data Source=data.db");

        internal static async Task Initialize()
        {
            await Connection.OpenAsync();

            var command = Connection.CreateCommand();
            command.CommandText = Resources.Initialize_Servers_Table;
            
            await command.ExecuteNonQueryAsync();
            await command.DisposeAsync();

            command = Connection.CreateCommand();
            command.CommandText = Resources.Initialize_Users_Table;

            await command.ExecuteNonQueryAsync();
            await command.DisposeAsync();

            command = Connection.CreateCommand();
            command.CommandText = Resources.Initialize_Roles_Table;

            await command.ExecuteNonQueryAsync();
            await command.DisposeAsync();
        }

        internal struct ServerConfiguration
        {
            public double MaxLoudness;
            public int Interval;
            public int MuteTime;

            public ServerConfiguration(double maxLoudness = 95.0, int interval = 300, int muteTime = 3000)
            {
                MaxLoudness = maxLoudness;
                Interval = interval;
                MuteTime = muteTime;
            }
        }

        internal static async Task<List<KeyValuePair<ulong, int>>> GetTop(ulong[] users, int number)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Top
                .Replace("$ids", string.Join(',', users));

            command.Parameters.AddWithValue("$num", number);

            var result = new List<KeyValuePair<ulong, int>>();

            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    result.Add(new KeyValuePair<ulong, int>(reader.GetFieldValue<ulong>(0), reader.GetInt32(1)));

            await command.DisposeAsync();

            return result;
        }

        internal static async Task<ServerConfiguration> GetServerConfiguration(ulong serverId)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Configuration;
            command.Parameters.AddWithValue("$id", serverId);

            ServerConfiguration result;

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                    result = new ServerConfiguration(reader.GetDouble(0), reader.GetInt32(1), reader.GetInt32(2));
                else
                    result = new ServerConfiguration();
            }

            await command.DisposeAsync();

            return result;
        }

        internal static async Task<string[]> GetServerPrefixes(ulong serverId)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Prefixes;
            command.Parameters.AddWithValue("$id", serverId);

            string[] result;

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                    result = JsonSerializer.Deserialize<string[]>(reader.GetString(0));
                else
                    result = new string[] { };
            }

            await command.DisposeAsync();

            return result;
        }

        internal static async Task SetServerPrefixes(ulong serverId, string[] prefixes)
        {
            if (prefixes == null)
                return;
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Prefixes;
            command.Parameters.AddWithValue("$id", serverId);
            command.Parameters.AddWithValue("$prefixes", JsonSerializer.Serialize(prefixes));
            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }

        internal static async Task<int> GetUserChokes(ulong userId)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Chokes;
            command.Parameters.AddWithValue("$id", userId);

            int result;

            using (var reader = await command.ExecuteReaderAsync())
                if (await reader.ReadAsync())
                    result = reader.GetInt32(0);
                else
                    result = 0;

            await command.DisposeAsync();

            return result;
        }

        internal static async Task<int> GetRoleLevelHighest(ulong[] roles)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Role_Level_Highest
                .Replace("$ids", string.Join(',', roles));

            int result = 0;

            using (var reader = await command.ExecuteReaderAsync())
                if (await reader.ReadAsync())
                    result = reader.GetInt32(0);

            await command.DisposeAsync();

            return result;
        }

        internal static async Task<Dictionary<ulong, uint>> GetRoleLevels(ulong[] roles)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Get_Role_Levels
                .Replace("$ids", string.Join(',', roles));

            var result = new Dictionary<ulong, uint>();

            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    result.Add(reader.GetFieldValue<ulong>(0), reader.GetFieldValue<uint>(1));

            await command.DisposeAsync();

            return result;
        }

        internal static async Task SetUserChokes(ulong userId, int chokes)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Chokes;
            command.Parameters.AddWithValue("$id", userId);
            command.Parameters.AddWithValue("$chokes", chokes);

            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }

        internal static async Task SetServerMaxLoudness(ulong serverId, double loudness)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Max_Loudness;
            command.Parameters.AddWithValue("$id", serverId);
            command.Parameters.AddWithValue("$max_loudness", loudness);
            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }

        internal static async Task SetServerInterval(ulong serverId, int interval)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Interval;
            command.Parameters.AddWithValue("$id", serverId);
            command.Parameters.AddWithValue("$interval", interval);
            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }

        internal static async Task SetServerMuteTime(ulong serverId, int muteTime)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Mute_Time;
            command.Parameters.AddWithValue("$id", serverId);
            command.Parameters.AddWithValue("$mute_time", muteTime);
            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }

        internal static async Task SetRoleLevel(ulong roleId, uint level)
        {
            var command = Connection.CreateCommand();
            command.CommandText = Resources.Set_Role_Level;
            command.Parameters.AddWithValue("$id", roleId);
            command.Parameters.AddWithValue("$level", level);

            await command.ExecuteNonQueryAsync();

            await command.DisposeAsync();
        }
    }
}
