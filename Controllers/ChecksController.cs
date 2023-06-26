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
using System;

namespace ZlagodaPrj.Controllers
{
    public class ChecksController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Index(string searchString, DateTime? startTime, DateTime? endTime, string submitButton)
        {
            if (startTime != null && endTime != null && startTime > endTime)
                ModelState.AddModelError(string.Empty, "End time cannot be earlier than start time");

            if (submitButton == "Show for today")
            {
                startTime = DateTime.Today;
                endTime = DateTime.Today.AddDays(1);
            }

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            string userRole = UserManager.GetCurrentUserRole(HttpContext);
            string userId = UserManager.GetCurrentUserId(HttpContext);

            cmd.CommandText = $"SELECT * FROM {Check.TABLE_NAME} where 1=1";
            if (userRole == RoleManager.CASHIER_ROLE)
            {
                cmd.CommandText += $" and {Check.COL_CASHIER_ID} = '{userId}'";
            }
            else
            {
                if (!string.IsNullOrEmpty(searchString))
                    cmd.CommandText += $" and {Check.COL_CASHIER_ID} = '{searchString}'";
            }

            if (startTime != null)
                cmd.CommandText += $" and {Check.COL_PRINT_DATE} >= '{((DateTime)startTime).ToUniversalTime()}'";

            if (endTime != null)
                cmd.CommandText += $" and {Check.COL_PRINT_DATE} <= '{((DateTime)endTime).ToUniversalTime()}'";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<CheckDTO> checks = new();
            while (await reader.ReadAsync())
            {
                CheckDTO check = new()
                {
                    Number = (string)reader[Check.COL_NUMBER],
                    CashierId = (string)reader[Check.COL_CASHIER_ID],
                    CardNumber = reader[Check.COL_CARD_NUMBER] as string,
                    PrintDate = ((DateTime)reader[Check.COL_PRINT_DATE]).ToLocalTime(),
                    SumTotal = (decimal)reader[Check.COL_SUM_TOTAL],
                    Vat = (decimal)reader[Check.COL_VAT],
                };

                checks.Add(check);
            }

            return View(new ChecksIndexPagedResult()
            {
                Checks = checks,
                CurrentEmployeeIdSearchString = searchString,
                StartTime = startTime ?? DateTime.MinValue,
                EndTime = endTime ?? DateTime.MaxValue.AddTicks(-(DateTime.MaxValue.Ticks % TimeSpan.TicksPerSecond)).AddSeconds(-DateTime.MaxValue.Second),
            });
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
            return View(new CreateCheckVM()); 
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public async Task<IActionResult> Create(CreateCheckVM model, string submitButton, int? saleId)
        {
            if (submitButton == "Remove Sale")
            {
                model.Sales.RemoveAt((int)saleId);
                return View(model);
            }
            else if (submitButton == "Add Sale")
            {
                model.Sales.Add(new());
                return View(model);
            }

            ValidateCreateUpdateModel(model);
            if (!ModelState.IsValid) return View(model);

            string? cashierId = UserManager.GetCurrentUserId(HttpContext);
            if (string.IsNullOrEmpty(cashierId))  // should never happen
            {
                ModelState.AddModelError(string.Empty, "Unknown cashier id. Try signing in and out again");
                return View(model);
            }

            // calculate the sum (get all the upc prices and check the amounts)
            decimal totalSum = 0;
            List<int> inStockAmounts = new();
            List<decimal> sellingPrices = new();

            foreach (CreateSaleVM sale in model.Sales)
            {

                string getAmountAndPriceCmdText = $"select {StoreProduct.COL_AMOUNT}, {StoreProduct.COL_PRICE}" +
                    $" from {StoreProduct.TABLE_NAME}" +
                    $" where {StoreProduct.COL_UPC} = '{sale.Upc}'";

                using var getAmountAndPriceCmd = await ConnectionManager.CreateCommandAsync(getAmountAndPriceCmdText);
                using var getAmountAndPriceReader = await getAmountAndPriceCmd.ExecuteReaderAsync();

                if (!getAmountAndPriceReader.HasRows)
                {
                    ModelState.AddModelError(string.Empty, $"Upc '{sale.Upc}' not found");
                    return View(model);
                }

                await getAmountAndPriceReader.ReadAsync();

                decimal price = (decimal)getAmountAndPriceReader[StoreProduct.COL_PRICE];
                int inStock = (int)getAmountAndPriceReader[StoreProduct.COL_AMOUNT];

                if (sale.Amount > inStock)
                {
                    ModelState.AddModelError(string.Empty, $"Cannot buy more of product '{sale.Upc}' than there is in stock");
                    return View(model);
                }

                totalSum += price * sale.Amount;
                sellingPrices.Add(price);
                inStockAmounts.Add(inStock);
            }

            // get the percent
            if (model.CardNumber != null)
            {
                string getPercentCmdText = $"select {CustomerCard.COL_PERCENT} from {CustomerCard.TABLE_NAME} where {CustomerCard.COL_NUMBER} = '{model.CardNumber}'";
                using var getPercentCmd = await ConnectionManager.CreateCommandAsync(getPercentCmdText);
                using var getPercentReader = await getPercentCmd.ExecuteReaderAsync();

                if (!getPercentReader.HasRows)
                {
                    ModelState.AddModelError(string.Empty, $"Card '{model.CardNumber}' not found");
                    return View(model);
                }

                await getPercentReader.ReadAsync();

                int percent = (int)getPercentReader[CustomerCard.COL_PERCENT];

                totalSum = totalSum * (1-(((decimal)0.01)*percent));
            }


            // insert the check
            if (model.CardNumber != null)
            {
                string insertCheckCmd = $"insert into {Check.TABLE_NAME} " +
                    $" ({Check.COL_NUMBER}, {Check.COL_CASHIER_ID}, {Check.COL_CARD_NUMBER}," +
                    $" {Check.COL_PRINT_DATE}, {Check.COL_SUM_TOTAL}, {Check.COL_VAT})" +
                    $" values ('{model.Number}', '{cashierId}', '{model.CardNumber}', " +
                    $" '{DateTime.UtcNow}', '{totalSum}', '{totalSum * (decimal)0.2}')";
                await ConnectionManager.ExecuteNonQueryAsync(insertCheckCmd);
            }
            else
            {
                string insertCheckCmd = $"insert into {Check.TABLE_NAME} " +
                    $" ({Check.COL_NUMBER}, {Check.COL_CASHIER_ID}, " +
                    $" {Check.COL_PRINT_DATE}, {Check.COL_SUM_TOTAL}, {Check.COL_VAT})" +
                    $" values ('{model.Number}', '{cashierId}', " +
                    $" '{DateTime.UtcNow}', '{totalSum}', '{totalSum * (decimal)0.2}')";
                await ConnectionManager.ExecuteNonQueryAsync(insertCheckCmd);
            }
                


            // insert all the sales
            for (int i = 0; i < model.Sales.Count; i++)
            {
                CreateSaleVM sale = model.Sales[i];

                // insert sale into sales
                string insertCmd = $"insert into {Sale.TABLE_NAME}" +
                    $" ({Sale.COL_UPC}, {Sale.COL_CHECK_NUMBER}, " +
                    $" {Sale.COL_AMOUNT}, {Sale.COL_SELLING_PRICE})" +
                    $" values" +
                    $" ('{sale.Upc}', '{model.Number}'," +
                    $" '{sale.Amount}', '{sellingPrices[i]}')";
                await ConnectionManager.ExecuteNonQueryAsync(insertCmd);

                // subtract amounts
                int newAmount = inStockAmounts[i] - sale.Amount;
                string updateAmountCmd = $"update {StoreProduct.TABLE_NAME}" +
                    $" set {StoreProduct.COL_AMOUNT} = '{newAmount}' where {StoreProduct.COL_UPC} = '{sale.Upc}'";
                await ConnectionManager.ExecuteNonQueryAsync(updateAmountCmd);
            }

            return RedirectToAction("Details", routeValues: new { id = model.Number });
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

        private void ValidateCreateUpdateModel(CreateCheckVM model)
        {
            if (model.Sales != null)
            {
                foreach (CreateSaleVM sale in model.Sales)
                {
                    if (sale.Amount <= 0)
                    {
                        ModelState.AddModelError(string.Empty, "Amount should be positive");
                    }
                }
            }
            
        }
    }
}
