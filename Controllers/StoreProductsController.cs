using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Npgsql;
using System.Reflection;
using System.Security.Permissions;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.DTO;
using ZlagodaPrj.Models.ViewModels.Product;
using ZlagodaPrj.Models.ViewModels.StoreProduct;

namespace ZlagodaPrj.Controllers
{
    public class StoreProductsController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Index(bool showOnlyOnSale, bool showOnlyNonSale, string upcSearchString, string sortBy = "amount")
        {
            string sortColumn = "";
            
            switch (sortBy)
            {
                case "amount":
                    sortColumn = $"{StoreProduct.COL_AMOUNT} desc";
                    break;
                case "product_name":
                    sortColumn = $"{Product.TABLE_NAME}.{Product.COL_NAME} asc";
                    break;
                case "none":
                    sortColumn = "amount";
                    break;
                default:
                    throw new Exception();  // should never happen
            }

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"" +
                $"SELECT" +
                $" {StoreProduct.COL_UPC}, {StoreProduct.COL_UPC_PROM}, {StoreProduct.COL_PRICE}," +
                $" {StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}, {Product.TABLE_NAME}.{Product.COL_NAME}, " +
                $" {Product.TABLE_NAME}.{Product.COL_CHARACTERISTICS}" +
                $" FROM {StoreProduct.TABLE_NAME} left outer join {Product.TABLE_NAME}" +
                $" on {StoreProduct.TABLE_NAME}.{StoreProduct.COL_PRODUCT_ID} = {Product.TABLE_NAME}.{Product.COL_ID}" +
                $" where 1=1 ";

            if (showOnlyOnSale)
                cmd.CommandText += $" and {StoreProduct.COL_IS_PROM} = '{true}'";
            if (showOnlyNonSale)
                cmd.CommandText += $" and {StoreProduct.COL_IS_PROM} = '{false}'";

            if (!string.IsNullOrEmpty(upcSearchString))
                cmd.CommandText += $" and {StoreProduct.COL_UPC} = '{upcSearchString}'";

            cmd.CommandText += $" order by {sortColumn}";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<StoreProductDTO> storeProducts = new();
            while (await reader.ReadAsync())
            {
                StoreProductDTO product = new()
                {
                    Upc = (string)reader[StoreProduct.COL_UPC],
                    UpcProm = reader[StoreProduct.COL_UPC_PROM] as string,
                    ProductName = (string)reader[Product.COL_NAME],
                    Price = (decimal)reader[StoreProduct.COL_PRICE],
                    Amount = (int)reader[StoreProduct.COL_AMOUNT],
                    IsProm = (bool)reader[StoreProduct.COL_IS_PROM],
                    Characteristics = (string)reader[Product.COL_CHARACTERISTICS],
                };

                storeProducts.Add(product);
            }

            StoreProductsListPagedResult result = new()
            {
                StoreProducts = storeProducts,
                ShowOnlyOnSale = showOnlyOnSale,
                ShowOnlyNonSale = showOnlyNonSale,
                UpcSearchString = upcSearchString,
            };

            return View(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Create(CreateUpdateStoreProductVM model)
        {
            ValidateModel(model);
            if (!ModelState.IsValid) return View();

            // open the connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var selectCmd = new NpgsqlCommand();
            selectCmd.Connection = con;

            // find the product id by name
            selectCmd.CommandText = $"SELECT {Product.COL_ID}" +
                $" FROM {Product.TABLE_NAME}" +
                $" where {Product.COL_NAME} = '{model.ProductName}'";

            using NpgsqlDataReader reader = await selectCmd.ExecuteReaderAsync();

            // if product not found
            if (!reader.HasRows)
                return NotFound($"Product '{model.ProductName}' not found");

            // get product number
            await reader.ReadAsync();
            int productId = (int)reader[Product.COL_ID];

            // close the reader
            await reader.CloseAsync();

            // get all store products that are associated with the same product (should be maximum one)
            selectCmd.CommandText = $"select " +
                $"{StoreProduct.COL_UPC}, {StoreProduct.COL_IS_PROM}, {StoreProduct.COL_PRICE} " +
                $"from {StoreProduct.TABLE_NAME} " +
                $"where {StoreProduct.COL_PRODUCT_ID} = '{productId}'";
            using NpgsqlDataReader reader2 = await selectCmd.ExecuteReaderAsync();

            // If there is at least one product
            if (await reader2.ReadAsync())
            {
                // read the fields of the productstore entry that is already in the database
                string upc = (string)reader[StoreProduct.COL_UPC];
                bool isProm = (bool)reader[StoreProduct.COL_IS_PROM];
                decimal price = (decimal)reader[StoreProduct.COL_PRICE];

                if (upc == model.Upc)
                {
                    ModelState.AddModelError(string.Empty, $"StoreProduct with Upc '{upc}' already exists");
                    return View();
                }

                if (isProm == model.IsProm)  // if storeproduct with same type (prom, non prom) already exists 
                {
                    ModelState.AddModelError(string.Empty, "StoreProduct with the same type (prom, non prom) for the specified product already exists");
                    return View();
                }

                if (await reader2.ReadAsync())  // if there is another product, bail out
                {
                    ModelState.AddModelError(string.Empty, "Two StoreProducts for the specified product already exist. Cannot add more than two");
                    return View();
                }

                await con.CloseAsync();

                // if adding promotional product, and non promotional exists
                // (it exists, because the reader got at least one row)
                if (model.IsProm)  
                {
                    // create the main command (insert prom product first)
                    string mainCmdAlter = $"insert into {StoreProduct.TABLE_NAME} " +
                        $"({StoreProduct.COL_UPC}, {StoreProduct.COL_PRODUCT_ID}, {StoreProduct.COL_PRICE}, " +
                        $"{StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}) " +
                        $"values ('{model.Upc}', '{productId}', '{price*(decimal)0.8}', " +
                        $"'{model.Amount}', '{model.IsProm}')";

                    await ConnectionManager.ExecuteNonQueryAsync(mainCmdAlter);
                    

                    // update non-prom product so that it now points to the prom one
                    string updateNonPromCmd = $"update {StoreProduct.TABLE_NAME} " +
                        $" set ({StoreProduct.COL_UPC_PROM})" +
                        $" =" +
                        $" row('{model.Upc}')" +
                        $" where {StoreProduct.COL_UPC} = '{upc}'";

                    await ConnectionManager.ExecuteNonQueryAsync(updateNonPromCmd);

                } 
                else  // if adding non-prom product and prom exists
                {
                    // add reference to prom to non-prom
                    string mainCmdAlter = $"insert into {StoreProduct.TABLE_NAME} " +
                        $"({StoreProduct.COL_UPC}, {StoreProduct.COL_UPC_PROM}, {StoreProduct.COL_PRODUCT_ID}, " +
                        $"{StoreProduct.COL_PRICE}, {StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}) " +
                        $"values ('{model.Upc}', '{upc}', '{productId}', " +
                        $"'{model.Price}', '{model.Amount}', '{model.IsProm}')";

                    await ConnectionManager.ExecuteNonQueryAsync(mainCmdAlter);
                }

                return RedirectToAction("Index");  // quit early in both cases
            }
            else  // no storeproducts for this product yet (can't add sale product before the main one)
            {
                if (model.IsProm)
                {
                    ModelState.AddModelError(string.Empty, "Cannot add sale storeproduct because there is no non-sale storeproduct for the product. Add non-sale version first");
                    return View();
                }
            }
            await con.CloseAsync();

            // create the main command
            string mainCmd = $"insert into {StoreProduct.TABLE_NAME} " +
                $"({StoreProduct.COL_UPC}, {StoreProduct.COL_PRODUCT_ID}, {StoreProduct.COL_PRICE}, " +
                $"{StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}) " +
                $"values ('{model.Upc}', '{productId}', '{model.Price}', " +
                $"'{model.Amount}', '{model.IsProm}')";

            await ConnectionManager.ExecuteNonQueryAsync(mainCmd);
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText =
                $"SELECT" +
                $" {StoreProduct.COL_UPC}, {StoreProduct.COL_PRICE}," +
                $" {StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}, {Product.TABLE_NAME}.{Product.COL_NAME}" +
                $" FROM {StoreProduct.TABLE_NAME} left outer join {Product.TABLE_NAME}" +
                $" on {StoreProduct.TABLE_NAME}.{StoreProduct.COL_PRODUCT_ID} = {Product.TABLE_NAME}.{Product.COL_ID}" +
                $" where {StoreProduct.COL_UPC} = '{id}'";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<StoreProductDTO> result = new();
            await reader.ReadAsync();
            CreateUpdateStoreProductVM product = new()
            {
                OldUpc = (string)reader[StoreProduct.COL_UPC],
                Upc = (string)reader[StoreProduct.COL_UPC],
                ProductName = (string)reader[Product.COL_NAME],
                Price = (decimal)reader[StoreProduct.COL_PRICE],
                Amount = (int)reader[StoreProduct.COL_AMOUNT],
                OldIsProm = (bool)reader[StoreProduct.COL_IS_PROM],
                IsProm = (bool)reader[StoreProduct.COL_IS_PROM],
            };

            return View(product);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        // TODO: add checks everywhere
        public async Task<IActionResult> Update(CreateUpdateStoreProductVM model)
        {
            ValidateModel(model);
            if (!ModelState.IsValid) return View();

            // open the connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var selectCmd = new NpgsqlCommand();
            selectCmd.Connection = con;

            // find the product id by name
            selectCmd.CommandText = $"SELECT {Product.COL_ID}" +
                $" FROM {Product.TABLE_NAME}" +
                $" where {Product.COL_NAME} = '{model.ProductName}'";

            using NpgsqlDataReader reader = await selectCmd.ExecuteReaderAsync();

            // if product not found
            if (!reader.HasRows)
                return NotFound($"Product '{model.ProductName}' not found");

            // get product number
            await reader.ReadAsync();
            int productId = (int)reader[Product.COL_ID];

            // close the reader
            await reader.CloseAsync();

            // count all store products that are associated with the same product (should be maximum one)
            selectCmd.CommandText = $"select count(*)" +
                $"from {StoreProduct.TABLE_NAME} " +
                $"where {StoreProduct.COL_PRODUCT_ID} = '{productId}'";
            using NpgsqlDataReader reader2 = await selectCmd.ExecuteReaderAsync();
            await reader2.ReadAsync();

            // can't change prom boolean because the opposite already exists
            if ((long)reader2["count"] > 1 && model.OldIsProm != model.IsProm) 
            {
                ModelState.AddModelError(string.Empty, "StoreProduct with the same type (prom, non prom) for the specified product already exists");
                return View();
            }

            string mainCmd = $"update {StoreProduct.TABLE_NAME} " +
                $"set ({StoreProduct.COL_UPC}, {StoreProduct.COL_PRODUCT_ID}, {StoreProduct.COL_PRICE}, " +
                $"{StoreProduct.COL_AMOUNT}, {StoreProduct.COL_IS_PROM}) " +
                $" = ('{model.Upc}', '{productId}', '{model.Price}', " +
                $"'{model.Amount}', '{model.IsProm}')" +
                $" where {StoreProduct.COL_UPC} = '{model.OldUpc}'";

            await ConnectionManager.ExecuteNonQueryAsync(mainCmd);
            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            string cmd = $"DELETE FROM {StoreProduct.TABLE_NAME} WHERE {StoreProduct.COL_UPC} = '{id}'";

            // execute command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);

            // return the view
            return RedirectToAction("Index");
        }

        private void ValidateModel(CreateUpdateStoreProductVM model)
        {
            if (model.Price <= 0)
                ModelState.AddModelError(string.Empty, "Price should be positive");

            if (model.Amount <= 0)
                ModelState.AddModelError(string.Empty, "Amount should be positive");

            if (model.Price != null && model.IsProm)
                ModelState.AddModelError(string.Empty, "Price is calculated automatically for sale items. Leave the field empty");

            if (model.Price == null && !model.IsProm)
                ModelState.AddModelError(string.Empty, "Price should be indicated for non-sale items");

        }
    }
}
