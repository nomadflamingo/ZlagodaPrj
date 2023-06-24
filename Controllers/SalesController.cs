using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Npgsql;
using System.Reflection;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.ViewModels.Sale;
using ZlagodaPrj.Models.ViewModels.StoreProduct;

namespace ZlagodaPrj.Controllers
{
    public class SalesController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public IActionResult Create(string? parentCheckId)
        {
            if (string.IsNullOrEmpty(parentCheckId))
                return NotFound("Parent check number was not set");

            ViewData["parentCheckId"] = parentCheckId;
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_CASHIERS_POLICY)]
        public async Task<IActionResult> Create(CreateUpdateSaleVM model, string parentCheckId)
        {
            // validate model
            ValidateModel(model);
            if (!ModelState.IsValid)
            {
                ViewData["parentCheckId"] = parentCheckId;
                return View();
            }

            // get discount

            // get upc price
            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"" +
                $"Select {StoreProduct.COL_PRICE}, {StoreProduct.COL_AMOUNT} " +
                $"FROM {StoreProduct.TABLE_NAME} " +
                $"WHERE {StoreProduct.COL_UPC} = '{model.Upc}'";
            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) return NotFound($"Store product with UPC '{model.Upc}' not found");
            await reader.ReadAsync();
            decimal price = (decimal)reader[StoreProduct.COL_PRICE];
            int totalProductAmount = (int)reader[StoreProduct.COL_AMOUNT];
            await reader.CloseAsync();

            // check that customer can buy this amount of product based on store product amount
            if (totalProductAmount < model.Amount)
            {
                ModelState.AddModelError(string.Empty, $"Cannot add to check. Not enough product amount in stock. " +
                    $"You tried buying: {model.Amount} Product in stock: {totalProductAmount}");
                ViewData["parentCheckId"] = parentCheckId;
                return View();
            }
            totalProductAmount -= model.Amount;

            // decrease the number of product
            string decreaseCmd =
                $" update {StoreProduct.TABLE_NAME}" +
                $" set {StoreProduct.COL_AMOUNT} = '{totalProductAmount}'" +
                $" where {StoreProduct.COL_UPC} = '{model.Upc}'";
            await ConnectionManager.ExecuteNonQueryAsync(decreaseCmd);

            // insert sale into sales
            string insertCmd = $"insert into {Sale.TABLE_NAME}" +
                $" ({Sale.COL_UPC}, {Sale.COL_CHECK_NUMBER}, " +
                $" {Sale.COL_AMOUNT}, {Sale.COL_SELLING_PRICE})" +
                $" values" +
                $" ('{model.Upc}', '{parentCheckId}'," +
                $" '{model.Amount}', '{price}')";
            await ConnectionManager.ExecuteNonQueryAsync(insertCmd);

            // get check's total sum
            string selectCheckSum =
                $" select {Check.COL_SUM_TOTAL}" +
                $" from {Check.TABLE_NAME}" +
                $" where {Check.COL_NUMBER} = '{parentCheckId}'";
            cmd.CommandText = selectCheckSum;
            using NpgsqlDataReader reader2 = await cmd.ExecuteReaderAsync();
            await reader2.ReadAsync();
            decimal totalSum = (decimal)reader2[Check.COL_SUM_TOTAL];
            totalSum += price * model.Amount;
            decimal vat = totalSum * (decimal)0.2;
            await reader2.CloseAsync();

            // update check's vat and total sum
            string updateCheckCmd = $"update {Check.TABLE_NAME}" +
                $" set ({Check.COL_SUM_TOTAL}, {Check.COL_VAT})" +
                $" =" +
                $" ('{totalSum}', '{vat}')" +
                $" where {Check.COL_NUMBER} = '{parentCheckId}'";
            await ConnectionManager.ExecuteNonQueryAsync(updateCheckCmd);

            return RedirectToAction("Details", "Checks", new { id = parentCheckId });
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(string? upc, string? checkNumber)
        {
            if (string.IsNullOrEmpty(upc) || string.IsNullOrEmpty(checkNumber))
                return NotFound("UPC or checkNumber are not set (or both)");

            // get upc amount and selling price from sale record
            using var conGetUpcAmount = ConnectionManager.CreateConnection();
            await conGetUpcAmount.OpenAsync();
            using var cmdGetUpcAmount = new NpgsqlCommand();
            cmdGetUpcAmount.Connection = conGetUpcAmount;
            cmdGetUpcAmount.CommandText = $"" +
                $"Select {Sale.COL_AMOUNT}, {Sale.COL_SELLING_PRICE} " +
                $"FROM {Sale.TABLE_NAME} " +
                $"WHERE {Sale.COL_UPC} = '{upc}'";
            using NpgsqlDataReader readerGetUpcAmount = await cmdGetUpcAmount.ExecuteReaderAsync();
            if (!readerGetUpcAmount.HasRows) return NotFound($"Store product with UPC '{upc}' not found");  // should never happen
            await readerGetUpcAmount.ReadAsync();
            int amount = (int)readerGetUpcAmount[Sale.COL_AMOUNT];
            decimal price = (decimal)readerGetUpcAmount[Sale.COL_SELLING_PRICE];
            await readerGetUpcAmount.CloseAsync();
            await conGetUpcAmount.CloseAsync();

            // get check's total sum
            using var con1 = ConnectionManager.CreateConnection();
            con1.Open();
            using var cmd1 = new NpgsqlCommand();
            cmd1.Connection = con1;
            string selectCheckSum =
                $" select {Check.COL_SUM_TOTAL}" +
                $" from {Check.TABLE_NAME}" +
                $" where {Check.COL_NUMBER} = '{checkNumber}'";
            cmd1.CommandText = selectCheckSum;
            using NpgsqlDataReader reader1 = await cmd1.ExecuteReaderAsync();
            await reader1.ReadAsync();
            decimal totalSum = (decimal)reader1[Check.COL_SUM_TOTAL];

            totalSum -= price * amount;
            decimal vat = totalSum * (decimal)0.2;
            await reader1.CloseAsync();

            string cmdUpdateSum = $"Update {Check.TABLE_NAME}" +
                $" set ({Check.COL_SUM_TOTAL}, {Check.COL_VAT}) = ('{totalSum}', '{vat}') where {Check.COL_NUMBER} = '{checkNumber}'";
            await ConnectionManager.ExecuteNonQueryAsync(cmdUpdateSum);

            string cmdMain = $"DELETE FROM {Sale.TABLE_NAME}" +
                $" WHERE ({Sale.COL_UPC}, {Sale.COL_CHECK_NUMBER}) = ('{upc}', '{checkNumber}')";
            await ConnectionManager.ExecuteNonQueryAsync(cmdMain);

            // get total product amount
            using var selectTotalAmountCmd = await ConnectionManager.CreateCommandAsync($"select {StoreProduct.COL_AMOUNT} from {StoreProduct.TABLE_NAME} where {StoreProduct.COL_UPC} = '{upc}'");
            using var readerAbc = await selectTotalAmountCmd.ExecuteReaderAsync();
            await readerAbc.ReadAsync();
            int totalAmount = (int)readerAbc[StoreProduct.COL_AMOUNT];
            totalAmount += amount;

            string cmdRestoreStock = $"update {StoreProduct.TABLE_NAME}" +
                $" set {StoreProduct.COL_AMOUNT} = {totalAmount} where {StoreProduct.COL_UPC} = '{upc}'";
            await ConnectionManager.ExecuteNonQueryAsync(cmdRestoreStock);

            // return the view
            return RedirectToAction("Details", "Checks", new { id = checkNumber });
        }


        private void ValidateModel(CreateUpdateSaleVM model)
        {
            if (model.Amount <= 0)
                ModelState.AddModelError(string.Empty, "Amount should be positive");
        }
    }
}
