using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.ViewModels;

namespace ZlagodaPrj.Data
{
    public static class RoleManager
    {
        public const string MANAGER_ROLE = "Manager";
        public const string CASHIER_ROLE = "Cashier";

        public const string ONLY_MANAGERS_POLICY = "Only Managers";
        public const string ONLY_CASHIERS_POLICY = "Only Cashiers";
        public const string CASHIERS_OR_MANAGERS_POLICY = "Cashiers Or Managers";


        public static bool IsAuthorized(HttpContext context)
		{
			string? userRole = UserManager.GetCurrentUserRole(context);

            return userRole != null;
		}

		public static bool HasRole(HttpContext context, string role) {
            string? userRole = UserManager.GetCurrentUserRole(context);

            if (userRole == null) return false;
            return userRole == role;
        }

        public static bool HasAnyRole(HttpContext context, List<string> roles)
        {
            string? role = UserManager.GetCurrentUserRole(context);

            if (role == null) return false;
            return roles.Contains(role);
        }

        private static async Task<bool> HasAnyRoleAsync(string userId, List<string> roles)
        {
            string role = await GetUserRolesAsync(userId);

            return roles.Contains(role);
        }

        private static async Task<string> GetUserRolesAsync(string userId)
        {
            using NpgsqlConnection con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = $"select {Employee.COL_ROLE} from {Employee.TABLE_NAME} where {Employee.COL_ID} = {userId}";

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();

            string role = reader[Employee.COL_ROLE] as string;

            await con.CloseAsync();

            return role;
        }

        
    }
}
