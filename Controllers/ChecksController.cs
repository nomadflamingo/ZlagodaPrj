using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models.DTO;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.ViewModels.Check;
using System.Xml.Linq;
using ZlagodaPrj.Models.ViewModels.Sale;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace ZlagodaPrj.Controllers
{
    public class ChecksController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Index()
        {
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"SELECT * FROM {Check.TABLE_NAME}";
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<CheckDTO> result = new();
            while (await reader.ReadAsync())
            {
                CheckDTO check = new()
                {
                    Number = (string)reader[Check.COL_NUMBER],
                    CashierId = (string)reader[Check.COL_CASHIER_ID],
                    CardNumber = reader[Check.COL_CARD_NUMBER] as string,
                    PrintDate = (DateTime)reader[Check.COL_PRINT_DATE],
                    SumTotal = (decimal)reader[Check.COL_SUM_TOTAL],
                    Vat = (decimal)reader[Check.COL_VAT],
                };

                result.Add(check);
            }

            return View(result);
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Details(string? id)
        {
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            // select the check
            cmd.CommandText =
                $" SELECT * FROM {Check.TABLE_NAME}" +
                $" where {Check.COL_NUMBER} = '{id}'";
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) return NotFound($"Check with number '{id}' not found");
            await reader.ReadAsync();
            CheckDTO check = new()
            {
                Number = (string)reader[Check.COL_NUMBER],
                CashierId = (string)reader[Check.COL_CASHIER_ID],
                CardNumber = reader[Check.COL_CARD_NUMBER] as string,
                PrintDate = (DateTime)reader[Check.COL_PRINT_DATE],
                SumTotal = (decimal)reader[Check.COL_SUM_TOTAL],
                Vat = (decimal)reader[Check.COL_VAT],
            };
            await reader.CloseAsync();

            // select all sales that match the specified check
            string selectSales =
                $" SELECT * FROM {Sale.TABLE_NAME}" +
                $" where {Sale.COL_CHECK_NUMBER} = '{id}'";
            cmd.CommandText = selectSales;
            using NpgsqlDataReader reader2 = await cmd.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                check.Sales.Add(new()
                {
                    Upc = (string)reader[Sale.COL_UPC],
                    Amount = (int)reader[Sale.COL_AMOUNT],
                    Price = (decimal)reader[Sale.COL_SELLING_PRICE],
                });
            }
            await reader2.CloseAsync();

            return View(check);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public IActionResult Create()
        {
            return View(new CreateUpdateCheckVM()); 
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public async Task<IActionResult> Create(CreateUpdateCheckVM model)
        {
            ValidateCreateUpdateModel(model);
            if (!ModelState.IsValid) return View();

            string? cashierId = UserManager.GetCurrentUserId(HttpContext);
            if (string.IsNullOrEmpty(cashierId))  // should never happen
            {
                ModelState.AddModelError(string.Empty, "Unknown cashier id. Try signing in and out again");
                return View();
            }

            // open the connection
            string cmd = $"insert into {Check.TABLE_NAME} " +
                $" ({Check.COL_NUMBER}, {Check.COL_CASHIER_ID}, {Check.COL_CARD_NUMBER}," +
                $" {Check.COL_PRINT_DATE}, {Check.COL_SUM_TOTAL}, {Check.COL_VAT})" +
                $" values ('{model.Number}', '{cashierId}', '{model.CardNumber}', " +
                $" '{DateTime.UtcNow}', '0', '0')";

            await ConnectionManager.ExecuteNonQueryAsync(cmd);
            return RedirectToAction("Details", routeValues: new{ id = model.Number });
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound("Check number not set");


            // find all sales associated with that check
            string selectAllSalesCmd = $"select {Sale.COL_UPC}" +
                $" from {Sale.TABLE_NAME}" +
                $" where {Sale.COL_CHECK_NUMBER} = '{id}'";

            using var selectCmd = await ConnectionManager.CreateCommandAsync(selectAllSalesCmd);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string upc = (string)reader[Sale.COL_UPC];
                IActionResult? deleteSaleErrorRes = await SalesController.DeleteSaleAsync(upc, id, this);
                if (deleteSaleErrorRes != null)
                    return deleteSaleErrorRes;
            }

            string cmd = $"DELETE FROM {Check.TABLE_NAME}" +
                $" WHERE {Check.COL_NUMBER} = '{id}'";

            // execute command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);

            // return the view
            return RedirectToAction("Index");
        }

        private void ValidateCreateUpdateModel(CreateUpdateCheckVM model)
        {
        }
    }
}
