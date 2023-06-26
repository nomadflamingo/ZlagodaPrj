using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.DTO;
using ZlagodaPrj.Models.ViewModels.CustomerCard;
using ZlagodaPrj.Models.ViewModels.Product;

namespace ZlagodaPrj.Controllers
{
    public class ProductsController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Index(string productNameSearchString, string categoryName)
        {
            // open connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"" +
                $"SELECT " +
                $" {Product.TABLE_NAME}.{Product.COL_ID}," +
                $" {Product.TABLE_NAME}.{Product.COL_NAME}," +
                $" {Product.TABLE_NAME}.{Product.COL_CHARACTERISTICS}," +
                $" {Category.TABLE_NAME}.{Category.COL_NAME}" +
                $" FROM {Product.TABLE_NAME} left outer join {Category.TABLE_NAME}" +
                $" on {Product.TABLE_NAME}.{Category.COL_NUMBER} = {Category.TABLE_NAME}.{Product.COL_CATEGORY_NUMBER}" +
                $" where 1=1";

            if (!string.IsNullOrEmpty(productNameSearchString))
                cmd.CommandText += $" and {Product.TABLE_NAME}.{Product.COL_NAME} = '{productNameSearchString}'";

            if (!string.IsNullOrEmpty(categoryName))
                cmd.CommandText += $" and {Category.TABLE_NAME}.{Category.COL_NAME} = '{categoryName}'";

            cmd.CommandText += $" order by {Product.COL_NAME} asc";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            List<ProductDTO> result = new List<ProductDTO>();
            while (await reader.ReadAsync())
            {
                ProductDTO product = new()
                {
                    Id = (int)reader[Product.COL_ID],
                    Name = (string)reader[Product.COL_NAME],
                    Characteristics = (string)reader[Product.COL_CHARACTERISTICS],
                    CategoryName = (string)reader[Category.COL_NAME]
                };

                result.Add(product);
            }
            await con.CloseAsync();


            // get all the categories
            string getCategoriesCmdString = $"select * from {Category.TABLE_NAME} order by {Category.COL_NAME} asc";
            using var getCategoriesCmd = await ConnectionManager.CreateCommandAsync(getCategoriesCmdString);
            using var getCategoriesReader = await getCategoriesCmd.ExecuteReaderAsync();
            List<Category> categories = new();
            while (await getCategoriesReader.ReadAsync())
            {
                categories.Add(new()
                {
                    Number = (int)getCategoriesReader[Category.COL_NUMBER],
                    Name = getCategoriesReader[Category.COL_NAME] as string
                });
            }


            return View(new ProductsListPagedResult
            {
                Products = result,
                ProductNameSearchString = productNameSearchString,
                CategoryName = categoryName,
                Categories = categories,
            });
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Create(CreateUpdateProductVM model)
        {
            ValidateModel(model);
            if (!ModelState.IsValid) return View();

            // open the connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            // find the category
            cmd.CommandText = $"SELECT {Category.COL_NUMBER}" +
                $" FROM {Category.TABLE_NAME}" +
                $" where {Category.COL_NAME} = '{model.Category}'";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // if category not found
            if (!reader.HasRows)
                return NotFound($"Category '{model.Category}' not found");

            // get cat number
            await reader.ReadAsync();
            int categoryNumber = (int)reader[Category.COL_NUMBER];

            // close the reader
            await reader.CloseAsync();

            // create the main command
            cmd.CommandText = $"insert into {Product.TABLE_NAME} " +
                $" ({Product.COL_NAME}," +
                $" {Product.COL_CHARACTERISTICS}, {Product.COL_CATEGORY_NUMBER})" +
                $" values" +
                $" ('{model.Name}'," +
                $" '{model.Characteristics}', '{categoryNumber}')";

            await cmd.ExecuteNonQueryAsync();

            await con.CloseAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"" +
                $"SELECT " +
                $" {Product.TABLE_NAME}.{Product.COL_ID}," +
                $" {Product.TABLE_NAME}.{Product.COL_NAME}," +
                $" {Product.TABLE_NAME}.{Product.COL_CHARACTERISTICS}," +
                $" {Category.TABLE_NAME}.{Category.COL_NAME}" +
                $" FROM {Product.TABLE_NAME} left outer join {Category.TABLE_NAME}" +
                $" on {Product.TABLE_NAME}.{Category.COL_NUMBER} = {Category.TABLE_NAME}.{Product.COL_CATEGORY_NUMBER}" +
                $" where {Product.COL_ID} = '{id}'";

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            CreateUpdateProductVM model = new()
            {
                Id = (int)reader[Product.COL_ID],
                Name = (string)reader[Product.COL_NAME],
                Category = (string)reader[Category.COL_NAME],
                Characteristics = (string)reader[Product.COL_CHARACTERISTICS],
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(CreateUpdateProductVM model)
        {
            ValidateModel(model);
            if (!ModelState.IsValid) return View();

            // open the connection
            using var con = ConnectionManager.CreateConnection();
            con.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            // find the category
            cmd.CommandText = $"SELECT {Category.COL_NUMBER}" +
                $" FROM {Category.TABLE_NAME}" +
                $" where {Category.COL_NAME} = '{model.Category}'";

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            // if category not found
            if (!reader.HasRows)
                return NotFound($"Category '{model.Category}' not found");

            // get cat number
            await reader.ReadAsync();
            int categoryNumber = (int)reader[Category.COL_NUMBER];

            // close the reader
            await reader.CloseAsync();


            // create main update command
            cmd.CommandText = $"update {Product.TABLE_NAME}" +
               $" set ({Product.COL_NAME}, {Product.COL_CHARACTERISTICS}, {Product.COL_CATEGORY_NUMBER})" +
               $" =" +
               $" ('{model.Name}', '{model.Characteristics}', '{categoryNumber}')" +
               $" where {Product.COL_ID} = '{model.Id}'";

            await cmd.ExecuteNonQueryAsync();
            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            string cmd = $"DELETE FROM {Product.TABLE_NAME} WHERE {Product.COL_ID} = '{id}'";

            // execute command
            await ConnectionManager.ExecuteNonQueryAsync(cmd);

            // return the view
            return RedirectToAction("Index");
        }


        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> TopProductsByCity(string city)
        {
            string cmdText = $"select count(*), pr.{Product.COL_NAME}" +
                $" from {Product.TABLE_NAME} pr" +
                $" inner join {StoreProduct.TABLE_NAME} sp" +
                $"   on sp.{StoreProduct.COL_PRODUCT_ID} = pr.{Product.COL_ID}" +
                $" inner join {Sale.TABLE_NAME} sa" +
                $"   on sa.{Sale.COL_UPC} = sp.{StoreProduct.COL_UPC}" +
                $" inner join {Check.TABLE_NAME} ch" +
                $"   on ch.{Check.COL_NUMBER} = sa.{Sale.COL_CHECK_NUMBER}" +
                $" inner join {CustomerCard.TABLE_NAME} cc" +
                $"   on cc.{CustomerCard.COL_NUMBER} = ch.{Check.COL_CARD_NUMBER}" +
                $" where cc.{CustomerCard.COL_CITY} = '{city}'" +
                $" group by pr.{Product.COL_ID}" +
                $" order by count desc";

            using NpgsqlCommand cmd = await ConnectionManager.CreateCommandAsync(cmdText);
            using var reader = await cmd.ExecuteReaderAsync();

            TopProductsByCityPagedResult result = new()
            {
                SearchCityString = city,
                ProductsInfo = new()
            };

            while (await reader.ReadAsync())
            {
                result.ProductsInfo.Add(new()
                {
                    SalesCount = (long)reader["count"],
                    ProductName = (string)reader[Product.COL_NAME],

                });
            }

            return View(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> ProductsSoldInEveryCity()
        {
            string cmdText = $"select {Product.COL_ID}, {Product.COL_NAME}" +
                $" from {Product.TABLE_NAME} pr1" +
                $" where not exists (" +
                $"   select {CustomerCard.COL_CITY}" +
                $"   from {CustomerCard.TABLE_NAME} cc1" +
                $"   where not exists (" +
                $"     select *" +
                $"     from {Sale.TABLE_NAME} sa" +
                $"     inner join {StoreProduct.TABLE_NAME} sp" +
                $"       on sp.{StoreProduct.COL_UPC} = sa.{Sale.COL_UPC}" +
                $"     inner join {Check.TABLE_NAME} ch" +
                $"       on ch.{Check.COL_NUMBER} = sa.{Sale.COL_CHECK_NUMBER}" +
                $"     inner join {CustomerCard.TABLE_NAME} cc" +
                $"       on cc.{CustomerCard.COL_NUMBER} = ch.{Check.COL_CARD_NUMBER}" +
                $"     where cc.{CustomerCard.COL_CITY} = cc1.{CustomerCard.COL_CITY}" +
                $"     and pr1.{Product.COL_ID} = sp.{StoreProduct.COL_PRODUCT_ID}" +
                $"   )" +
                $")";

            using NpgsqlCommand cmd = await ConnectionManager.CreateCommandAsync(cmdText);
            using var reader = await cmd.ExecuteReaderAsync();

            List<ProductsSoldInEveryCity> result = new List<ProductsSoldInEveryCity>();
            while (await reader.ReadAsync())
            {
                ProductsSoldInEveryCity product = new()
                {
                    Id = (int)reader[Product.COL_ID],
                    Name = (string)reader[Product.COL_NAME],
                };

                result.Add(product);
            }

            return View(result);

        }


        private void ValidateModel(CreateUpdateProductVM model)
        {

        }
    }
}
