using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.DTO;
using ZlagodaPrj.Models.ViewModels.CustomerCard;
using ZlagodaPrj.Models.ViewModels.Employee;

namespace ZlagodaPrj.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
    public class CustomersController : Controller
    {
        public async Task<IActionResult> Index(string surnameSearchString, int? minPercent, int? maxPercent)
        {
            /*if (minPercent != null && maxPercent != null && minPercent < maxPercent)
            {
                ModelState.AddModelError(string.Empty, "Max percent cannot be less than min percent");
                return View();
            }*/

            string userRole = UserManager.GetCurrentUserRole(HttpContext);

            // open connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"SELECT * FROM {CustomerCard.TABLE_NAME} where 1=1";

            if (!string.IsNullOrEmpty(surnameSearchString))
                cmd.CommandText += $" and {CustomerCard.COL_SURNAME} = '{surnameSearchString}'";

            if (userRole == RoleManager.MANAGER_ROLE)
            {
                if (minPercent != null)
                    cmd.CommandText += $" and {CustomerCard.COL_PERCENT} >= '{minPercent}'";

                if (maxPercent != null)
                    cmd.CommandText += $" and {CustomerCard.COL_PERCENT} <= '{maxPercent}'";
            }
            

            cmd.CommandText += $" order by {CustomerCard.COL_SURNAME} asc";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<CustomerCard> result = new List<CustomerCard>();
            while (await reader.ReadAsync())
            {
                CustomerCard customerCard = new()
                {
                    Number = (string)reader[CustomerCard.COL_NUMBER],
                    Surname = (string)reader[CustomerCard.COL_SURNAME],
                    Name = (string)reader[CustomerCard.COL_NAME],
                    Patronymic = reader[CustomerCard.COL_PATRONYMIC] as string,
                    Phone = (string)reader[CustomerCard.COL_PHONE],
                    City = (string)reader[CustomerCard.COL_CITY],
                    Street = (string)reader[CustomerCard.COL_STREET],
                    ZipCode = (string)reader[CustomerCard.COL_ZIP_CODE],
                    Percent = (int)reader[CustomerCard.COL_PERCENT]
                };

                result.Add(customerCard);
            }
            await con.CloseAsync();

            return View(new CustomerCardIndexPagedResult
            {
                CustomerCards = result,
                SurnameSearchString = surnameSearchString,
                MaxPercent = maxPercent ?? 100,
                MinPercent = minPercent ?? 0,
            });
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Create(CreateCustomerCardVM createModel)
        {
            // validate model
            ValidateCreateModel(createModel);
            if (!ModelState.IsValid) return View();

            // build command
            string cmd = $"insert into {CustomerCard.TABLE_NAME}" +
                $" ({CustomerCard.COL_NUMBER}, {CustomerCard.COL_SURNAME}, {CustomerCard.COL_NAME}," +
                $" {CustomerCard.COL_PATRONYMIC}, {CustomerCard.COL_PHONE}, {CustomerCard.COL_CITY}," +
                $" {CustomerCard.COL_STREET}, {CustomerCard.COL_ZIP_CODE}, {CustomerCard.COL_PERCENT})" +
                $" values" +
                $" ('{createModel.Number}', '{createModel.Surname}', '{createModel.Name}'," +
                $" '{createModel.Patronymic}', '{createModel.Phone}', '{createModel.City}'," +
                $" '{createModel.Street}', '{createModel.ZipCode}', '{createModel.Percent}')";

            // execute command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);
            
            // return the view
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(string? id)
        {
            if (id == null || id == string.Empty) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"Select * FROM {CustomerCard.TABLE_NAME} WHERE {CustomerCard.COL_NUMBER} = '{id}'";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            UpdateCustomerCardVM updateModel = new()
            {
                OldNumber = (string)reader[CustomerCard.COL_NUMBER],
                Number = (string)reader[CustomerCard.COL_NUMBER],
                Surname = (string)reader[CustomerCard.COL_SURNAME],
                Name = (string)reader[CustomerCard.COL_NAME],
                Patronymic = reader[CustomerCard.COL_PATRONYMIC] as string,
                Phone = (string)reader[CustomerCard.COL_PHONE],
                City = (string)reader[CustomerCard.COL_CITY],
                Street = (string)reader[CustomerCard.COL_STREET],
                ZipCode = (string)reader[CustomerCard.COL_ZIP_CODE],
                Percent = (int)reader[CustomerCard.COL_PERCENT]
            };

            await con.CloseAsync();
            return View(updateModel);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(UpdateCustomerCardVM updateModel)
        {
            // validate the model
            ValidateUpdateModel(updateModel);
            if (!ModelState.IsValid) return View();

            // set up command text
            string cmd = $"update {CustomerCard.TABLE_NAME}" +
                $" set ({CustomerCard.COL_NUMBER}, {CustomerCard.COL_SURNAME}, {CustomerCard.COL_NAME}," +
                $" {CustomerCard.COL_PATRONYMIC}, {CustomerCard.COL_PHONE}, {CustomerCard.COL_CITY}," +
                $" {CustomerCard.COL_STREET}, {CustomerCard.COL_ZIP_CODE}, {CustomerCard.COL_PERCENT})" +
                $" =" +
                $" ('{updateModel.Number}', '{updateModel.Surname}', '{updateModel.Name}'," +
                $" '{updateModel.Patronymic}', '{updateModel.Phone}', '{updateModel.City}'," +
                $" '{updateModel.Street}', '{updateModel.ZipCode}', '{updateModel.Percent}')" +
                $" where {CustomerCard.COL_NUMBER} = '{updateModel.OldNumber}'";

            // execute the command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);

            // return the view
            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null || id == string.Empty) return NotFound();
            
            string cmd = $"DELETE FROM {CustomerCard.TABLE_NAME} WHERE {CustomerCard.COL_NUMBER} = '{id}'";

            // execute command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);
            
            // return the view
            return RedirectToAction("Index");
        }

        private void ValidateCreateModel(CreateCustomerCardVM model)
        {
            if (model.Percent < 0 || model.Percent > 100)
                ModelState.AddModelError("", "Percent cannot be negative and can't be greater than 100");
        }

        private void ValidateUpdateModel(UpdateCustomerCardVM model)
        {
            if (model.Percent < 0 || model.Percent > 100)
                ModelState.AddModelError("", "Percent cannot be negative and can't be greater than 100");
        }
    }
}
