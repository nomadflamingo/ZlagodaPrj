using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Npgsql;
using System.Security.Claims;
using System.Security.Cryptography;
using ZlagodaPrj.Models;

namespace ZlagodaPrj.Data
{
    public static class UserManager
    {
        public static string HashPassword(string password)
        {
            var sha = SHA256.Create();
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha.ComputeHash(textBytes);
            return BitConverter
                .ToString(hashBytes)
                .Replace("-", string.Empty);
        }

        /// <summary>
        /// Adds ClaimsPrincipal to the response using HttpContext therefore signing user in.
        /// Returns true if login was successful. Returns false if username or password were incorrect
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<bool> AuthenticateUserAsync(HttpContext context, string userId, string password)
        {
            // create and open the connection
            using var con = ConnectionManager.CreateConnection();
            await con.OpenAsync();

            // create command
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"" +
                $"SELECT {Employee.COL_ID}, {Employee.COL_PASSWORD_HASH}, {Employee.COL_ROLE} " +
                $"from {Employee.TABLE_NAME} " +
                $"where ({Employee.COL_ID} = '{userId}')";

            // execute the command
            using var reader = await cmd.ExecuteReaderAsync();


            // read the date
            await reader.ReadAsync();

            // get the fields from data
            if (!reader.HasRows)
                return false;

            // if no password field, user not found
            if (reader[Employee.COL_PASSWORD_HASH] is not string storedPasswordHash) return false;

            // if password is present but role field is not, something went wrong
            if (reader[Employee.COL_ROLE] is not string role)
                throw new KeyNotFoundException($"For some reason couldn't find the role of the employee with id {userId}");

            // close connection
            await con.CloseAsync();

            // if passwords don't match, failed login attempt
            if (storedPasswordHash != HashPassword(password)) return false;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);


            return true;
        }

        public static bool IsSignedIn(HttpContext context)
        {
            Claim? claim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier, null);

            if (claim == null) { return false; }
            else { return true; }
        }

        public static string? GetCurrentUserRole(HttpContext context)
        {
            Claim? claim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role, null);

            if (claim == null) { return null; }
            return claim.Value;
        }

        public static string? GetCurrentUserId(HttpContext context)
        {
            Claim? claim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier, null);

            if (claim == null) { return null; }
            return claim.Value;
        }

    }
}
