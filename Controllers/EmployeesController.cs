using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.DTO;
using ZlagodaPrj.Models.ViewModels.Employee;

namespace ZlagodaPrj.Controllers
{
    public class EmployeesController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Index(string surnameSearchString, string idSearchString, DateTime? startTime, DateTime? endTime, bool includeTotalSold = false, bool showOnlyCashiers = false)
		{
            // open connection
			using var con = ConnectionManager.CreateConnection();
			con.Open();

			using var cmd = new NpgsqlCommand();
			cmd.Connection = con;


            if (!includeTotalSold)
            {
                cmd.CommandText = $"SELECT * FROM {Employee.TABLE_NAME} where 1=1 ";

                if (showOnlyCashiers)
                    cmd.CommandText += $" and {Employee.COL_ROLE} = '{RoleManager.CASHIER_ROLE}' ";

                if (!string.IsNullOrEmpty(surnameSearchString))
                    cmd.CommandText += $" and {Employee.COL_SURNAME} = '{surnameSearchString}'";

                if (!string.IsNullOrEmpty(idSearchString))
                    cmd.CommandText += $" and {Employee.COL_ID} = '{idSearchString}' ";

                cmd.CommandText += $" order by {Employee.COL_SURNAME} asc";
            }
            else
            {
                cmd.CommandText = $"select " +
                    $" sum({Sale.COL_AMOUNT}), emp.* " +
                    $" from {Sale.TABLE_NAME} sa " +
                    $" inner join {Check.TABLE_NAME} ch " +
                    $"   on ch.{Check.COL_NUMBER} = sa.{Sale.COL_CHECK_NUMBER} " +
                    $" inner join {Employee.TABLE_NAME} emp " +
                    $"   on emp.{Employee.COL_ID} = ch.{Check.COL_CASHIER_ID} " +
                    $" where 1=1 ";

                if (!string.IsNullOrEmpty(surnameSearchString))
                    cmd.CommandText += $" and emp.{Employee.COL_SURNAME} = '{surnameSearchString}'";

                if (!string.IsNullOrEmpty(idSearchString))
                    cmd.CommandText += $" and emp.{Employee.COL_ID} = '{idSearchString}' ";

                if (startTime != null)
                    cmd.CommandText += $" and ch.{Check.COL_PRINT_DATE} >= '{((DateTime)startTime).ToUniversalTime()}' ";

                if (endTime != null)
                    cmd.CommandText += $" and ch.{Check.COL_PRINT_DATE} <= '{((DateTime)endTime).ToUniversalTime()}' ";

                cmd.CommandText += $" group by emp.{Employee.COL_ID}";
                cmd.CommandText += $" order by sum desc";
            }

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
			List<EmployeeDTO> result = new List<EmployeeDTO>();
            long totalSold = 0;
			while (await reader.ReadAsync())
			{
                EmployeeDTO emp;
                if (!includeTotalSold)
                {
                    emp = new()
                    {
                        Id = (string)reader[Employee.COL_ID],
                        Surname = (string)reader[Employee.COL_SURNAME],
                        Name = (string)reader[Employee.COL_NAME],
                        Patronymic = reader[Employee.COL_NAME] as string,
                        Role = (string)reader[Employee.COL_ROLE],
                        Salary = (decimal)reader[Employee.COL_SALARY],
                        BirthDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_BIRTHDATE]),
                        StartDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_STARTDATE]),
                        Phone = (string)reader[Employee.COL_PHONE],
                        City = (string)reader[Employee.COL_CITY],
                        Street = (string)reader[Employee.COL_STREET],
                        ZipCode = (string)reader[Employee.COL_ZIP_CODE],
                    };
                }
                else
                {
                    totalSold += (long)reader["sum"];
                    emp = new()
                    {
                        Id = (string)reader[Employee.COL_ID],
                        Surname = (string)reader[Employee.COL_SURNAME],
                        Name = (string)reader[Employee.COL_NAME],
                        Patronymic = reader[Employee.COL_NAME] as string,
                        Role = (string)reader[Employee.COL_ROLE],
                        Salary = (decimal)reader[Employee.COL_SALARY],
                        BirthDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_BIRTHDATE]),
                        StartDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_STARTDATE]),
                        Phone = (string)reader[Employee.COL_PHONE],
                        City = (string)reader[Employee.COL_CITY],
                        Street = (string)reader[Employee.COL_STREET],
                        ZipCode = (string)reader[Employee.COL_ZIP_CODE],
                        TotalSold = (long)reader["sum"],
                    };
                }

				result.Add(emp);
			}
			await con.CloseAsync();

			return View(new EmployeeIndexPagedResult
            {
                Employees = result,
                SurnameSearchString = surnameSearchString,
                IncludeTotalSold = includeTotalSold,
                IdSearchString = idSearchString,
                StartTime = startTime ?? DateTime.MinValue,
                EndTime = endTime ?? DateTime.MaxValue.AddTicks(-(DateTime.MaxValue.Ticks % TimeSpan.TicksPerSecond)).AddSeconds(-DateTime.MaxValue.Second),
                TotalSold = totalSold,
            });
		}

        [HttpGet("/Employees/Cashiers/MyInfo")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public async Task<IActionResult> CashierMyInfo()
        {
            // get current cashier id
            string userId = UserManager.GetCurrentUserId(HttpContext);

            // open connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"SELECT * FROM {Employee.TABLE_NAME} " +
                $"where {Employee.COL_ID} = '{userId}' ";


            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            
            EmployeeDTO emp = new()
            {
                Id = (string)reader[Employee.COL_ID],
                Surname = (string)reader[Employee.COL_SURNAME],
                Name = (string)reader[Employee.COL_NAME],
                Patronymic = reader[Employee.COL_NAME] as string,
                Role = (string)reader[Employee.COL_ROLE],
                Salary = (decimal)reader[Employee.COL_SALARY],
                BirthDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_BIRTHDATE]),
                StartDate = DateOnly.FromDateTime((DateTime)reader[Employee.COL_STARTDATE]),
                Phone = (string)reader[Employee.COL_PHONE],
                City = (string)reader[Employee.COL_CITY],
                Street = (string)reader[Employee.COL_STREET],
                ZipCode = (string)reader[Employee.COL_ZIP_CODE]
            };

            con.Close();


            return View(emp);
        }

        [HttpGet("/Employees/Update/{id?}")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(string? id)
        {
            if (id == null || id == string.Empty) return NotFound();

            // open connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"SELECT * FROM {Employee.TABLE_NAME} where {Employee.COL_ID} = '{id}'";

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            UpdateEmployeeVM updateEmployeeVM = new()
            {
                OldNumber = (string)reader[Employee.COL_ID],
                Id = (string)reader[Employee.COL_ID],
                Surname = (string)reader[Employee.COL_SURNAME],
                Name = (string)reader[Employee.COL_NAME],
                Patronymic = reader[Employee.COL_NAME] as string,
                Role = (string)reader[Employee.COL_ROLE],
                Salary = (decimal)reader[Employee.COL_SALARY],
                BirthDate = (DateTime)reader[Employee.COL_BIRTHDATE],
                StartDate = (DateTime)reader[Employee.COL_STARTDATE],
                Phone = (string)reader[Employee.COL_PHONE],
                City = (string)reader[Employee.COL_CITY],
                Street = (string)reader[Employee.COL_STREET],
                ZipCode = (string)reader[Employee.COL_ZIP_CODE],
            };

            await con.CloseAsync();
            return View(updateEmployeeVM);
        }

        [HttpPost("/Employees/Update/{id?}")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(UpdateEmployeeVM updateModel)
        {
            // validate the model TODO: add all checks
            ValidateUpdateModel(updateModel);
            if (!ModelState.IsValid) return View();

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            // hash the password
            string hashedPassword = UserManager.HashPassword(updateModel.Password);

            cmd.CommandText = $"UPDATE {Employee.TABLE_NAME}" +
                $" SET ({Employee.COL_ID}, {Employee.COL_SURNAME}, {Employee.COL_NAME}," +
                $" {Employee.COL_PATRONYMIC}, {Employee.COL_ROLE}, {Employee.COL_SALARY}, " +
                $" {Employee.COL_BIRTHDATE}, {Employee.COL_STARTDATE}, {Employee.COL_PHONE}," +
                $" {Employee.COL_CITY}, {Employee.COL_STREET}, {Employee.COL_ZIP_CODE}, " +
                $" {Employee.COL_PASSWORD_HASH})" +
                $" = " +
                $"('{updateModel.Id}', '{updateModel.Surname}', '{updateModel.Name}', '{updateModel.Patronymic}'," +
                $"'{updateModel.Role}', '{updateModel.Salary}', '{updateModel.BirthDate}', '{updateModel.StartDate}'," +
                $"'{updateModel.Phone}', '{updateModel.City}', '{updateModel.Street}', '{updateModel.ZipCode}'," +
                $"'{hashedPassword}') " +
                $"where {Employee.COL_ID} = '{updateModel.OldNumber}'";

            int result = await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("/Employees/Delete/{id?}")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null || id == string.Empty) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = $"DELETE FROM {Employee.TABLE_NAME} WHERE {Employee.COL_ID} = '{id}'";

            int result = await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

		[HttpGet("/Account/Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("/Account/Login")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(SignInVM signInModel)
        {
            if (!ModelState.IsValid) return View(signInModel);

            bool signInSuccess = await UserManager.AuthenticateUserAsync(HttpContext, signInModel.EmployeeId, signInModel.Password);

            if (!signInSuccess)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/Employees/Register")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public IActionResult Register()
        {
			return View();
        }

        [HttpPost("/Employees/Register")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Register(CreateEmployeeVM registerModel)
        {
            // TODO: do all the other checks
            ValidateRegisterModel(registerModel);
			if (!ModelState.IsValid) return View();

            // hash the password
            string viewModelPasswordHash = UserManager.HashPassword(registerModel.Password);

            // create the command
            string cmd = $"insert into {Employee.TABLE_NAME}" +
                $"({Employee.COL_ID}, {Employee.COL_SURNAME}, {Employee.COL_NAME}, {Employee.COL_PATRONYMIC}, " +
                $"{Employee.COL_ROLE}, {Employee.COL_SALARY}, {Employee.COL_BIRTHDATE}, {Employee.COL_STARTDATE}, " +
                $"{Employee.COL_PHONE}, {Employee.COL_CITY}, {Employee.COL_STREET}, {Employee.COL_ZIP_CODE}, " +
                $"{Employee.COL_PASSWORD_HASH})" +
                $" values " +
                $"('{registerModel.Id}', '{registerModel.Surname}', '{registerModel.Name}', '{registerModel.Patronymic}'," +
                $"'{registerModel.Role}', '{registerModel.Salary}', '{registerModel.BirthDate}', '{registerModel.StartDate}'," +
                $"'{registerModel.Phone}', '{registerModel.City}', '{registerModel.Street}', '{registerModel.ZipCode}'," +
                $"'{viewModelPasswordHash}')";

            // execute the command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);

            // return the view
            return RedirectToAction("Index");

        }

        [Authorize]
        [HttpGet("/Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            // Clear the existing external cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/Account/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void ValidateRegisterModel(CreateEmployeeVM registerModel)
        {
            bool isAdult = registerModel.BirthDate.AddYears(18) <= DateTime.Now;
            if (!isAdult)
                ModelState.AddModelError(string.Empty, "Employee should be at least 18 years old");

            if (registerModel.Salary < 0)
                ModelState.AddModelError(string.Empty, "Salary can't be negative");

            // check the password match
            if (registerModel.Password != registerModel.PasswordConfirm)
                ModelState.AddModelError(string.Empty, "Passwords don't match");

            // check that role was selected
            if (registerModel.Role == string.Empty)
                ModelState.AddModelError(string.Empty, "No role was selected");
        }

        private void ValidateUpdateModel(UpdateEmployeeVM registerModel)
        {
            bool isAdult = registerModel.BirthDate.AddYears(18) <= DateTime.Now;
            if (!isAdult)
                ModelState.AddModelError(string.Empty, "Employee should be at least 18 years old");

            if (registerModel.Salary < 0)
                ModelState.AddModelError(string.Empty, "Salary can't be negative");

            // check the password match
            if (registerModel.Password != registerModel.PasswordConfirm)
                ModelState.AddModelError(string.Empty, "Passwords don't match");

            // check that role was selected
            if (registerModel.Role == string.Empty)
                ModelState.AddModelError(string.Empty, "No role was selected");
        }
    }
}
