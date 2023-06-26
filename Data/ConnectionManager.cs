using Npgsql;

namespace ZlagodaPrj.Data
{
    public static class ConnectionManager
    {
        public static string? connectionString;
        

        public static NpgsqlConnection CreateConnection()
        {
            var con = new NpgsqlConnection(
                connectionString: connectionString);
            return con;
        }

        /*public static async Task<int> ExecuteNonQueryTextCommand(string cmd)
        {

        }*/

        /*public static async Task<NpgsqlDataReader> ExecuteReaderAsync(string cmdText)
        {
            using var con = CreateConnection();
            await con.OpenAsync();
            
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = cmdText;

            var reader = await cmd.ExecuteReaderAsync();

            await con.CloseAsync();
            return reader;
        }*/


        public static async Task<int> ExecuteNonQueryAsync(string cmdText)
        {
            using var con = CreateConnection();
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = cmdText;

            int res = await cmd.ExecuteNonQueryAsync();

            await con.CloseAsync();
            return res;
        }

        /// <summary>
        /// Connection should be opened. Doesn't close the connection
        /// </summary>
        /// <param name="cmdText">Text</param>
        /// <param name="con">Opened connection</param>
        /// <returns></returns>
        public static async Task<int> ExecuteNonQueryAsync(string cmdText, NpgsqlConnection con)
        {
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = cmdText;

            int res = await cmd.ExecuteNonQueryAsync();
            await cmd.DisposeAsync();
            return res;
        }

        public static async Task<NpgsqlCommand> CreateCommandAsync(string cmdText)
        {
            var con = CreateConnection();
            await con.OpenAsync();

            var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = cmdText;
            
            return cmd;
        }

    }
}
