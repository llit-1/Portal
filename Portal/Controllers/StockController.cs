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
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using OfficeOpenXml;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using System.Globalization;
using Portal.Models.MSSQL.Location;
using Portal.Models.MSSQL;

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

        public IActionResult TabMenu()
        {
            return PartialView();
        }

        public IActionResult StockTransfer()
        {
            List<Location> locations = dbSql.Locations.OrderBy(x => x.Name).ToList();

            return PartialView(locations);
        }

        public async Task<IActionResult> StockSearch()
        {
            DataStockSearch data = await GetDataForStockSearch();
            return PartialView(data);
        }

        public IActionResult StockSearchCard(string code = null) {
            
            return PartialView("StockSearchCard", code);
        }

        static async Task<DataStockSearch> GetDataForStockSearch()
        {
            try
            {
                using var httpClient = new HttpClient();
                using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/getlazymodel");
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<DataStockSearch>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new DataStockSearch();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error in GetDataForStockSearch: {ex.Message}");
                return new DataStockSearch();
            }
        }

        public class DataStockSearch
        {

            public List<Portal.Models.MSSQL.WarehouseHolder> Holders { get; set; }
            public List<Location> Locations { get; set; }
            public List<WarehouseCategories> MainCategories { get; set; } = new();
        }


        public async Task<IActionResult> Stock(int actual = 0)
        {
            var categories = await GetAsync();

            if (actual == 1)
            {
                categories = categories.Where(x => x.Actual == 1).ToList();
            }


            ViewBag.Actual = actual;

            return PartialView(categories);
        }

        public IActionResult LoadModal(int id)
        {
            Models.MSSQL.WarehouseCategories warehouseCategories;

            if (id != 0)
            {
                warehouseCategories = GetOneCategoryAsync(id).Result;
            }
            else
            {
                warehouseCategories = new Models.MSSQL.WarehouseCategories();
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

            var category = new Models.MSSQL.WarehouseCategories
            {
                Id = Id,
                Name = Name,
                Parent = Parent,
                Actual = int.Parse(Actual)
            };

            if (Img != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await Img.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    category.Img = imageBytes;
                }
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


        [HttpPatch]
        public async Task<IActionResult> SaveSubCategory([FromForm] int Id, [FromForm] string Name, [FromForm] int? Parent, [FromForm] string Actual, [FromForm] IFormFile? Img)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Actual))
            {
                return BadRequest("Required fields are missing.");
            }

            var parent = dbSql.WarehouseCategories.FirstOrDefault(x => x.Id == Id).Parent;

            var category = new Models.MSSQL.WarehouseCategories
            {
                Id = Id,
                Name = Name,
                Parent = parent,
                Actual = int.Parse(Actual)
            };

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

        public async Task<IActionResult> SaveNewCategory([FromForm] int? Id, [FromForm] string Name, [FromForm] int? Parent, [FromForm] string Actual, [FromForm] IFormFile? Img)
        {
            if (Name == null)
            {
                return BadRequest("��� ������");
            }

            // ������ ������ ���������
            var category = new Models.MSSQL.WarehouseCategories
            {
                Id = Id,
                Name = Name,
                Parent = Parent,
                Actual = int.Parse(Actual)
            };

            // ���� ����������� ��������, ����������� ��� � ������ ������
            if (Img != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await Img.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    category.Img = imageBytes;
                }
            }

            // ����������� ������ � JSON
            var jsonContent = JsonConvert.SerializeObject(category);

            // ���������� ������ �� ������ ������ � ������� JSON
            using var httpClient = new HttpClient();
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // ���������� ������ �� ������ ������
            using HttpResponseMessage response = await httpClient.PostAsync($"https://warehouseapi.ludilove.ru/api/category/SetCategory", content);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                // �������� ������ ��� �������
                var errorResponse = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorResponse);
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

        static async Task<List<Models.MSSQL.WarehouseCategories>> GetAsync()
        {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/maincategories");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<Models.MSSQL.WarehouseCategories>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        static async Task<Models.MSSQL.WarehouseCategories> GetOneCategoryAsync(int id)
        {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync("https://warehouseapi.ludilove.ru/api/category/maincategories");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var categories = System.Text.Json.JsonSerializer.Deserialize<List<Models.MSSQL.WarehouseCategories>>(jsonResponse, new JsonSerializerOptions
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


        public async Task<IActionResult> AddSubCategoryItem([FromBody] Models.MSSQL.WarehouseCategories data)
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

        public IActionResult SearchItem(string item = "Sams")
        {
            // �����
            var result = dbSql.WarehouseCategories
                              .Where(x => x.Name.ToLower().Contains(item.ToLower()))
                              .ToList();

            // ���� ������ �� �������, ���������� JSON � ����������
            if (result.Count == 0)
            {
                return Json(new { message = "������ �� �������" });
            }

            // ������ ������ ��� �������� �����������
            List<object[]> items = new List<object[]>();

            // ��������� ��� ��������� ������� ��� ���������� ���������� �������� � ���� ������
            var allCategories = dbSql.WarehouseCategories.ToDictionary(x => x.Id);

            foreach (var elem in result)
            {
                string text = "";
                int? id = null;

                // ���� ���� ������������ ���������
                if (elem.Parent != null)
                {
                    // ���� ������������ ���������
                    if (allCategories.TryGetValue(elem.Parent.Value, out var parentCategory))
                    {
                        // ���� � ������������ ��������� ���� ��� ���� ������������ ���������
                        if (parentCategory.Parent != null)
                        {
                            if (allCategories.TryGetValue(parentCategory.Parent.Value, out var grandParentCategory))
                            {
                                text += grandParentCategory.Name + " / " + parentCategory.Name + " / " + elem.Name;
                                id = grandParentCategory.Id;
                            }
                        }
                        else
                        {
                            text += parentCategory.Name + " / " + elem.Name;
                            id = parentCategory.Id;
                        }
                    }
                }
                else
                {
                    text += elem.Name;
                    id = elem.Id;
                }

                // ��������� ������ [id, text] � ������
                items.Add(new object[] { id, text });
            }

            // ���������� JSON � ������������
            return Json(items);
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
    }
}