using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using static Portal.Controllers.TimesheetsFactoryController;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Portal.Controllers
{
    [Authorize]
    public class StockController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public StockController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }

        public async Task<IActionResult> Stock()
        {
            var categories = await GetAsync();
            return PartialView(categories);
        }

        public IActionResult LoadModal(int id)
        {
            WarehouseCategories warehouseCategories;

            if (id != 0)
            {
                warehouseCategories = GetOneCategoryAsync(id).Result;
            }
            else
            {
                warehouseCategories = new WarehouseCategories();
            }

            return PartialView("LoadModalStock", warehouseCategories);
        }

        [HttpPatch]
        public async Task<IActionResult> SaveCategory([FromForm] int Id, [FromForm] string Name, [FromForm] int? Parent, [FromForm] string Actual, [FromForm] IFormFile? Img)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Actual))
            {
                return BadRequest("Required fields are missing.");
            }

            var category = new WarehouseCategories
            {
                Id = Id,
                Name = Name,
                Parent = Parent,
                Actual = int.Parse(Actual)
            };

            if (Img != null)
            {
                using var memoryStream = new MemoryStream();
                await Img.CopyToAsync(memoryStream);
                category.Img = memoryStream.ToArray();
            }

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(category), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PatchAsync("https://warehouseapi.ludilove.ru/api/category/UpdateCategory", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }

        public async Task<IActionResult> SaveNewCategory([FromBody] WarehouseCategories data)
        {
            if (data == null)
            {
                return BadRequest("Data is null");
            }

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PostAsync($"https://warehouseapi.ludilove.ru/api/category/SetCategory", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }

        public async Task<IActionResult> DeleteCategory (int categoryId)
        {
            using var httpClient = new HttpClient();

            using HttpResponseMessage response = await httpClient.DeleteAsync($"https://warehouseapi.ludilove.ru/api/category/DeleteCategory?id=" + categoryId.ToString());

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }


        public async Task<IActionResult> SwitchActual(int id)
        {
            using var httpClient = new HttpClient();
            var content = new StringContent("", Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PatchAsync($"https://warehouseapi.ludilove.ru/api/category/UpdateCategoryActual?id={id}", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }

        static async Task<List<WarehouseCategories>> GetAsync()
        {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/maincategories");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<WarehouseCategories>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        static async Task<WarehouseCategories> GetOneCategoryAsync(int id)
        {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/maincategories");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var categories = System.Text.Json.JsonSerializer.Deserialize<List<WarehouseCategories>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return categories.FirstOrDefault(x => x.Id == id);
        }

        public async Task<IActionResult> StockTable(int id) {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/ChildCategories?id=" + id);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var categoriesHierarchy = System.Text.Json.JsonSerializer.Deserialize<List<CategoriesHierarchy>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            CategoriesHierarchyWithCategoryID categoriesHierarchyWithCategoryID = new CategoriesHierarchyWithCategoryID();

            categoriesHierarchyWithCategoryID.Categories = categoriesHierarchy;
            categoriesHierarchyWithCategoryID.CategoryID = id;

            return PartialView(categoriesHierarchyWithCategoryID);
        }

        public IActionResult LoadModalStockTable(int id)
        {
            return PartialView(id);
        }


        public async Task<IActionResult> AddSubCategoryItem([FromBody] WarehouseCategories data)
        {
            if (data == null)
            {
                return BadRequest("Data is null");
            }

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.PostAsync($"https://warehouseapi.ludilove.ru/api/category/SetCategory", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }



        public class CategoriesHierarchyWithCategoryID
        {
            public int CategoryID { get; set; }
            public List<CategoriesHierarchy> Categories { get; set; }
        }

        public class CategoriesHierarchy
        {
            
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public List<CategoriesHierarchy> Categories { get; set; } = new();
            public int Actual { get; set; }
        }

        public class WarehouseCategories
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int? Id { get; set; }
            public string Name { get; set; }
            public int? Parent { get; set; }
            public byte[]? Img { get; set; }
            public int Actual { get; set; }
        }
    }
}