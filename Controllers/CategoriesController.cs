using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;
using ZlagodaPrj.Data;
using ZlagodaPrj.Models;
using ZlagodaPrj.Models.ViewModels.Category;

namespace ZlagodaPrj.Controllers
{
    public class CategoriesController : Controller
    {
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.CASHIERS_OR_MANAGERS_POLICY)]
        public async Task<IActionResult> Index()
        {
            // TODO check role

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = $"SELECT * FROM {Category.TABLE_NAME}";


            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            var result = new List<Category>();
            while (await reader.ReadAsync())
            {
                Category category = new()
                {
                    Number = (int)reader[Category.COL_NUMBER],
                    Name = reader[Category.COL_NAME] as string
                };

                result.Add(category);
            }
            con.Close();

            return View(result);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public IActionResult Create()
        {
            // TODO check role
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Create(CreateCategoryVM categoryViewModel)
        {
            // TODO check role
            if (!ModelState.IsValid) return View(categoryViewModel);

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = $"INSERT INTO {Category.TABLE_NAME} ({Category.COL_NAME}) values ('{categoryViewModel.Name}')";
            await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }


        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null || id < 0) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = $"SELECT * FROM {Category.TABLE_NAME} WHERE {Category.COL_NUMBER} = {id}";

           
            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            
            await reader.ReadAsync();
            UpdateCategoryVM category = new()
            {
                Number = (int)id,
                Name = reader[Category.COL_NAME] as string
            };


            con.Close();

            return View(category);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Update(UpdateCategoryVM vm)
        {
            if (vm.Number <= 0) return NotFound();

            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;


            cmd.CommandText = $"UPDATE {Category.TABLE_NAME} " +
                $" SET ({Category.COL_NAME}) = ROW('{vm.Name}')" +
                $" WHERE {Category.COL_NUMBER} = {vm.Number}";

            int result = await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = RoleManager.ONLY_MANAGERS_POLICY)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id < 0) return NotFound();


            using var con = ConnectionManager.CreateConnection();
            con.Open();

            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = $"DELETE FROM {Category.TABLE_NAME} WHERE {Category.COL_NUMBER} = {id}";

            int result = await cmd.ExecuteNonQueryAsync();

            return RedirectToAction("Index");
        }
    }
}
