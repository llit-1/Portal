using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Portal.Models;
using System;
using System.Linq;
using System.IO;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Portal.Controllers
{
    [Authorize(Roles = "menuDelivery")]
    public class MenuController : Controller
    {
        private readonly DB.MSSQLDBContext dbSql;

        public MenuController(DB.MSSQLDBContext dbSqlContext)
        {
            dbSql = dbSqlContext;
        }

        /// <summary>Стопы доставки ставятся и читаются через новую апишку (yeapi), а не через старую.</summary>
        private bool UseYeapiStops => User.IsInRole(Global.YeapiStopClient.RoleName);
        
        public IActionResult Index()
        {
            var user = new RKNet_Model.Account.User();
            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var userResult = ApiRequest.GetUser(userLogin);

            if(userResult.Ok)
            {
                user = userResult.Data;
            }
            return PartialView(user);         
        }
        public IActionResult MenuCategory(int Id)
        {
            var viewMenu = new ViewModels.Menu.MenuViewModel();

            var categoryResult = ApiRequest.GetMenuCategory(Id, false);
            if (!categoryResult.Ok) return new ObjectResult(categoryResult.ErrorMessage);
            viewMenu.MenuCategory = categoryResult.Data;

            var pathResult = ApiRequest.GetMenuCategoryPath(Id);
            if (!pathResult.Ok) return new ObjectResult(pathResult.ErrorMessage);
            viewMenu.CategoryPath = pathResult.Data;

            if(User.IsInRole("menuDelivery_stops"))
            {
                var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var userResult = ApiRequest.GetUser(userLogin);
                if (!userResult.Ok) return new ObjectResult(userResult.ErrorMessage);
                viewMenu.User = userResult.Data;

                if (userResult.Data.TTs.Count == 1)
                {
                    if (UseYeapiStops)
                    {
                        var yeapiStops = GetYeapiDeliveryStops(userResult.Data.TTs.First(), viewMenu.MenuCategory.Items);
                        if (!yeapiStops.Ok) return new ObjectResult(yeapiStops.ErrorMessage);
                        viewMenu.DeliveryStops = yeapiStops.Data;
                    }
                    else
                    {
                        var stopsResult = ApiRequest.GetDeliveryStopsByTT(userResult.Data.TTs.First().Code.ToString());
                        if (stopsResult.Ok) viewMenu.DeliveryStops = stopsResult.Data;
                    }

                    var cashStopsResult = ApiRequest.GetCashStopsByTT(userResult.Data.TTs.First().Code.ToString());
                    if (cashStopsResult.Ok) viewMenu.SkuStops = cashStopsResult.Data;
                }
            }
                  
            


            return PartialView(viewMenu);
        }

        // загрузка стопов доставки
        [Authorize(Roles = "menuDelivery_stops")]
        public IActionResult DeliveryStops(string ttCode)
        {
            var viewMenu = new ViewModels.Menu.MenuViewModel();

            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var userResult = ApiRequest.GetUser(userLogin);
            if (!userResult.Ok) return new ObjectResult(userResult.ErrorMessage);
            viewMenu.User = userResult.Data;

            if (UseYeapiStops)
            {
                var tt = userResult.Data.TTs.FirstOrDefault(t => t.Code.ToString() == ttCode);
                if (tt == null) return new ObjectResult($"Торговая точка с кодом {ttCode} не привязана к пользователю.");

                var locationGuid = ResolveLocationGuid(tt);
                if (locationGuid == null) return new ObjectResult($"Для ТТ {tt.Name} не найдена точка в справочнике локаций.");

                try
                {
                    foreach (var rkCode in new Global.YeapiStopClient().GetStoppedRkCodes(locationGuid.Value))
                    {
                        var itemResult = ApiRequest.GetMenuItemByRkCode(rkCode, false);
                        if (!itemResult.Ok) continue;
                        viewMenu.MenuCategory.Items.Add(itemResult.Data);
                        viewMenu.DeliveryStops.Add(new RKNet_Model.MSSQL.DeliveryItemStop { ItemId = itemResult.Data.Id, ItemRkCode = itemResult.Data.rkCode });
                    }
                }
                catch (Exception ex)
                {
                    return new ObjectResult("ошибка получения стопов из новой апишки: " + ex.Message);
                }

                return PartialView("MenuCategory", viewMenu);
            }

            var stopsResult = ApiRequest.GetDeliveryStopsByTT(ttCode);
            if (stopsResult.Ok) viewMenu.DeliveryStops = stopsResult.Data;

            foreach(var stop in viewMenu.DeliveryStops)
            {
                var itemResult = ApiRequest.GetMenuItem(stop.ItemId, false);
                if(itemResult.Ok) viewMenu.MenuCategory.Items.Add(itemResult.Data);
            }

            return PartialView("MenuCategory", viewMenu);
        }

        // загрузка стопов на кассах
        [Authorize(Roles = "menuDelivery_stops")]
        public IActionResult CashStops(string ttCode)
        {
            var viewMenu = new ViewModels.Menu.MenuViewModel();

            var stopsResult = ApiRequest.GetCashStopsByTT(ttCode);
            if (stopsResult.Ok) viewMenu.SkuStops = stopsResult.Data;

            foreach (var stop in viewMenu.SkuStops)
            {
                var itemResult = ApiRequest.GetMenuItemByRkCode(int.Parse(stop.SkuRkCode), false);
                if (itemResult.Ok) viewMenu.MenuCategory.Items.Add(itemResult.Data);
            }

            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var userResult = ApiRequest.GetUser(userLogin);

            if (!userResult.Ok) return new ObjectResult(userResult.ErrorMessage);
            viewMenu.User = userResult.Data;

            return PartialView("MenuCategory", viewMenu);
        }

        // загрузка отключенных в Р-Кипер блюд
        public IActionResult RkDisabledItems(string ttCode)
        {
            var viewMenu = new ViewModels.Menu.MenuViewModel();
            
            var disabledResult = ApiRequest.GetDisabledItems();
            if (disabledResult.Ok) viewMenu.MenuCategory.Items.AddRange(disabledResult.Data);

            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var userResult = ApiRequest.GetUser(userLogin);
            if (!userResult.Ok) return new ObjectResult(userResult.ErrorMessage);
            viewMenu.User = userResult.Data;

            return PartialView("MenuCategory", viewMenu);
        }

        public IActionResult GetRkMenu()
        {
            var result = ApiRequest.GetRkMenu();
            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult CategoryEditor(int Id)
        {
            var categoryResult = ApiRequest.GetMenuCategory(Id, true);
            if (!categoryResult.Ok) return new ObjectResult(categoryResult.ErrorMessage);
            return PartialView(categoryResult.Data);
        }
        
        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult EditCategory(string json)
        {

            json = json.Replace("%pp%", "+");
            json = json.Replace("%bkspc%", " ");

            var category = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Menu.Category>(json);

            var result = ApiRequest.EditMenuCategory(category);

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Изменение категории меню";
            log.Description = category.Name;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult AddCategory(string json)
        {
            json = json.Replace("%pp%", "+");
            json = json.Replace("%bkspc%", " ");

            var category = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Menu.Category>(json);           
            var result = ApiRequest.AddMenuCategory(category);

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Добавление категории меню";
            log.Description = category.Name;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult DeleteCategory(int Id)
        {
            var catName = ApiRequest.GetMenuCategory(Id, false).Data.Name;
            var result = ApiRequest.DeleteMenuCategory(Id);
            
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Удаление категории меню";
            log.Description = catName;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }
        
        [HttpGet("/menu/categorysImages/category{Id}.jpg")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 86400)]
        public IActionResult CategoryImage(int Id)
        {
            if (Id == 0)
            {
                return new ObjectResult("no Id");
            }

            var result = ApiRequest.GetMenuCategoryImage(Id);
            
            if(result.Ok)
            {
                if (result.Data != null)
                {
                    return File(result.Data, "image/jpeg", $"category{Id}.jpg");
                }
                else
                {
                    return new ObjectResult("no image in db");
                }
            }
            else
            {
                return new ObjectResult(result.ErrorMessage);
            }

        }
        
        [HttpGet("/menu/itemsImages/item{Id}.jpg")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 86400)]
        public IActionResult ItemImage(int Id)
        {
            if (Id == 0)
            {
                return new ObjectResult("no Id");
            }
            
            var result = ApiRequest.GetMenuItemImage(Id);

            if (result.Ok)
            {
                if(result.Data != null)
                {
                    return File(result.Data, "image/jpeg", $"item{Id}.jpg");
                }
                else
                {                    
                    return new ObjectResult("no image in db");
                }
            }
            else
            {
                return new ObjectResult(result.ErrorMessage);
            }
        }
        public IActionResult ResizeImage(string data)
        {           
            var base64Data = System.Text.RegularExpressions.Regex.Match(data, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            var binData = Convert.FromBase64String(base64Data);

            var ms = new MemoryStream(binData);
            Image image = Image.FromStream(ms);

            Size newSize = new Size(800, 600);
            var imgResized = new Bitmap(image, newSize);            

            using (var memStream = new MemoryStream())
            {
                imgResized.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                byte[] imageBytes = memStream.ToArray();                
                string base64String = Convert.ToBase64String(imageBytes);
                return new ObjectResult (base64String);
            }
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult ItemEditor(int Id)
        {
            var menuView = new ViewModels.Menu.MenuViewModel();

            var measureUnitsResult = ApiRequest.GetMeasureUnits();
            if (measureUnitsResult.Ok) menuView.MeasureUnits = measureUnitsResult.Data;

            var rkMenuResult = ApiRequest.GetRkMenu();
            menuView.rkMenuTree = TreeMenu(rkMenuResult.Data);           

            var menuItemResult = ApiRequest.GetMenuItem(Id, true);
            if (menuItemResult.Ok) menuView.Item = menuItemResult.Data;

            return PartialView(menuView);
        }

        [Authorize(Roles = "menuDelivery_edit")]

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult EditItem(string json)
        {

            json = json.Replace("%pp%", "+");
            json = json.Replace("%bkspc%", " ");

            var item = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Menu.Item>(json);
            var result = ApiRequest.EditMenuItem(item);

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Изменение позиции меню";
            log.Description = item.marketName;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult AddItem(string json)
        {
            json = json.Replace("%pp%", "+");
            json = json.Replace("%bkspc%", " ");

            var item = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Menu.Item>(json);
            var result = ApiRequest.AddMenuItem(item);

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Добавление позиции меню";
            log.Description = item.marketName;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_edit")]
        public IActionResult DeleteItem(int Id)
        {
            var itemName = ApiRequest.GetMenuItem(Id, false).Data.marketName;
            var result = ApiRequest.DeleteMenuItem(Id);

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Удаление позиции меню";
            log.Description = itemName;
            log.Object = result.Ok.ToString();
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_stops")]
        public IActionResult SetStopDeliveryItem(int ttId, int itemId)
        {
            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            if (UseYeapiStops)
                return new ObjectResult(ChangeYeapiStop(ttId, itemId, userLogin, stop: true));

            var result = ApiRequest.SetStopDeliveryItem(ttId, itemId, userLogin);            
            return new ObjectResult(result);
        }

        [Authorize(Roles = "menuDelivery_stops")]
        public IActionResult RemoveStopDeliveryItem(int ttId, int itemId)
        {
            var userLogin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            if (UseYeapiStops)
                return new ObjectResult(ChangeYeapiStop(ttId, itemId, userLogin, stop: false));

            var result = ApiRequest.RemoveStopDeliveryItem(ttId, itemId, userLogin);
            return new ObjectResult(result);
        }

        // ----- стопы доставки через новую апишку (роль menuDelivery_stops_yeapi) -----

        /// <summary>Ставит или снимает быстрый стоп в yeapi; ТТ проверяется по привязке пользователя, как в старой апишке.</summary>
        private RKNet_Model.Result<string> ChangeYeapiStop(int ttId, int itemId, string userLogin, bool stop)
        {
            var result = new RKNet_Model.Result<string>();

            var userResult = ApiRequest.GetUser(userLogin);
            if (!userResult.Ok)
            {
                result.Ok = false;
                result.ErrorMessage = userResult.ErrorMessage;
                return result;
            }

            var tt = userResult.Data.TTs.FirstOrDefault(t => t.Id == ttId);
            if (tt == null)
            {
                result.Ok = false;
                result.ErrorMessage = $"Торговая точка с Id = {ttId} не привязана к пользователю.";
                return result;
            }

            var locationGuid = ResolveLocationGuid(tt);
            if (locationGuid == null)
            {
                result.Ok = false;
                result.ErrorMessage = $"Для ТТ {tt.Name} не найдена точка в справочнике локаций — стоп в новую апишку не отправлен.";
                return result;
            }

            var itemResult = ApiRequest.GetMenuItem(itemId, false);
            if (!itemResult.Ok)
            {
                result.Ok = false;
                result.ErrorMessage = itemResult.ErrorMessage;
                return result;
            }

            var client = new Global.YeapiStopClient();
            return stop
                ? client.SetQuickStop(locationGuid.Value, itemResult.Data.rkCode, itemResult.Data.marketName, userResult.Data.Name)
                : client.RemoveQuickStop(locationGuid.Value, itemResult.Data.rkCode);
        }

        /// <summary>Стопы yeapi по точке в виде старых DeliveryItemStop — представление меню остаётся без изменений.</summary>
        private RKNet_Model.Result<List<RKNet_Model.MSSQL.DeliveryItemStop>> GetYeapiDeliveryStops(RKNet_Model.TT.TT tt, IEnumerable<RKNet_Model.Menu.Item> items)
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.MSSQL.DeliveryItemStop>> { Data = new List<RKNet_Model.MSSQL.DeliveryItemStop>() };

            var locationGuid = ResolveLocationGuid(tt);
            if (locationGuid == null)
            {
                result.Ok = false;
                result.ErrorMessage = $"Для ТТ {tt.Name} не найдена точка в справочнике локаций.";
                return result;
            }

            try
            {
                var stopped = new Global.YeapiStopClient().GetStoppedRkCodes(locationGuid.Value);
                result.Data = items
                    .Where(item => stopped.Contains(item.rkCode))
                    .Select(item => new RKNet_Model.MSSQL.DeliveryItemStop { ItemId = item.Id, ItemRkCode = item.rkCode })
                    .ToList();
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = "ошибка получения стопов из новой апишки: " + ex.Message;
            }
            return result;
        }

        /// <summary>
        /// ТТ портала → точка yeapi. Сопоставление как в синхронизации NX: Restaurant_Sifr = Locations.RKCode,
        /// для ТТ без кода Р-Кипер — по точному совпадению названия среди актуальных точек.
        /// </summary>
        private Guid? ResolveLocationGuid(RKNet_Model.TT.TT tt)
        {
            if (tt.Restaurant_Sifr != 0)
                return dbSql.Locations.Where(l => l.RKCode == tt.Restaurant_Sifr).Select(l => (Guid?)l.Guid).FirstOrDefault();

            var name = tt.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var matches = dbSql.Locations.Where(l => l.Actual == 1 && l.Name != null && l.Name.Trim() == name).Select(l => l.Guid).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : (Guid?)null;
        }

        // формирование структуры меню для ComboTree из меню Р-Кипер
        private List<ViewModels.Menu.treeMenuItem> TreeMenu(List<RKNet_Model.Menu.rkMenuItem> rkMenuItems)
        {
            var treeMenu = new List<ViewModels.Menu.treeMenuItem>();

            foreach (var rkItem in rkMenuItems)
            {
                var menuItem = new ViewModels.Menu.treeMenuItem();
                menuItem.isSelectable = !rkItem.isCategory;
                menuItem.title = rkItem.rkName;
                menuItem.id = rkItem.rkCode.ToString();
                menuItem.subs = TreeMenu(rkItem.rkMenuItems);
                menuItem.deliveryPrice = rkItem.deliveryPrice.ToString();
                treeMenu.Add(menuItem);
            }
            return treeMenu;
        }
    }
}
