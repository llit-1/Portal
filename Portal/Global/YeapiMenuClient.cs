using Newtonsoft.Json;
using Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Portal.Global
{
    /// <summary>
    /// Меню доставки из новой апишки (yeapi): группы, актуальные позиции и картинки.
    /// В yeapi у позиции нет внутреннего Id — ключ это код R-Keeper, поэтому в модели портала
    /// Id заполняется кодом R-Keeper. Редактирование меню осталось на старой апишке и
    /// сопоставляет позицию по этому же коду (см. MenuController.ItemEditor).
    /// </summary>
    public class YeapiMenuClient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // список групп апишка отдаёт вместе с картинками (под 700 КБ), а нужен он на каждой загрузке экрана,
        // поэтому держим его недолго в памяти
        private static readonly TimeSpan GroupsCacheTime = TimeSpan.FromMinutes(1);
        private static readonly object GroupsLock = new object();
        private static List<GroupDto> _groupsCache;
        private static DateTime _groupsCacheTime;

        private readonly string _baseUrl;

        public YeapiMenuClient()
        {
            _baseUrl = (SettingsInternal.Configuration?["Yeapi:BaseUrl"] ?? "https://yeapi.ludilove.ru").TrimEnd('/');
        }

        /// <summary>Корень меню: актуальные группы, позиций на верхнем уровне нет.</summary>
        public RKNet_Model.Menu.Category GetRootCategory()
        {
            return new RKNet_Model.Menu.Category
            {
                Id = 0,
                Categories = GetGroups().Select(ToCategory).ToList(),
                Items = new List<RKNet_Model.Menu.Item>(),
            };
        }

        /// <summary>Группа меню с актуальными позициями. Группы в yeapi плоские, вложенных категорий нет.</summary>
        public RKNet_Model.Menu.Category GetCategory(int groupId)
        {
            var group = GetGroups().FirstOrDefault(g => g.Id == groupId);
            if (group == null)
                throw new Exception($"Группа меню {groupId} не найдена в новой апишке.");

            var category = ToCategory(group);
            category.Items = Get<List<ItemDto>>($"/ItemManager/groups/{groupId}/items/actual")
                .Select(ToItem)
                .ToList();
            return category;
        }

        /// <summary>Позиция по коду R-Keeper. Null, если позиции в меню доставки нет.</summary>
        public RKNet_Model.Menu.Item GetItem(int rkCode)
        {
            var item = Get<ItemDto>($"/ItemManager/item/{rkCode}", allowNotFound: true);
            return item == null ? null : ToItem(item);
        }

        /// <summary>Картинка группы. Null, если картинки нет.</summary>
        public byte[] GetGroupImage(int groupId) => GetBytes($"/menu/groups/{groupId}/image");

        /// <summary>Картинка позиции по коду R-Keeper. Null, если картинки нет.</summary>
        public byte[] GetItemImage(int rkCode) => GetBytes($"/menu/items/{rkCode}/image");

        private List<GroupDto> GetGroups()
        {
            lock (GroupsLock)
            {
                if (_groupsCache != null && DateTime.Now - _groupsCacheTime < GroupsCacheTime)
                    return _groupsCache;

                _groupsCache = Get<List<GroupDto>>("/ItemManager/getallgroups")
                    .Where(g => g.Actual == 1)
                    .OrderBy(g => g.YaName)
                    .ToList();
                _groupsCacheTime = DateTime.Now;
                return _groupsCache;
            }
        }

        private RKNet_Model.Menu.Category ToCategory(GroupDto group) => new RKNet_Model.Menu.Category
        {
            Id = group.Id,
            Name = group.YaName,
            ImageUpdated = group.ImgUpdated?.ToString("s"),
            // картинка грузится отдельным запросом (/menu/categorysImages/category{Id}.jpg), как и раньше
            Image = null,
            Items = new List<RKNet_Model.Menu.Item>(),
            Categories = new List<RKNet_Model.Menu.Category>(),
        };

        private RKNet_Model.Menu.Item ToItem(ItemDto item) => new RKNet_Model.Menu.Item
        {
            Id = item.RkCode, // в yeapi позиция опознаётся кодом R-Keeper
            rkCode = item.RkCode,
            rkName = item.RkName,
            marketName = item.YeName,
            Description = item.Description,
            rkDeliveryPrice = (int)Math.Round(item.Price),
            Measure = item.Measure,
            MeasureUnit = string.IsNullOrEmpty(item.MeasureUnit) ? null : new RKNet_Model.Menu.MeasureUnit { Name = item.MeasureUnit },
            Enabled = item.Actual == 1,
            // картинка грузится отдельным запросом (/menu/itemsImages/item{Id}.jpg), как и раньше
            Image = null,
        };

        private T Get<T>(string path, bool allowNotFound = false) where T : class
        {
            using var response = Http.GetAsync(_baseUrl + path).GetAwaiter().GetResult();
            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Новая апишка меню ответила {(int)response.StatusCode}: {body}");

            return string.IsNullOrWhiteSpace(body) ? null : JsonConvert.DeserializeObject<T>(body);
        }

        private byte[] GetBytes(string path)
        {
            using var response = Http.GetAsync(_baseUrl + path).GetAwaiter().GetResult();
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Новая апишка меню ответила {(int)response.StatusCode} на запрос картинки.");

            var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return bytes != null && bytes.Length > 0 ? bytes : null;
        }

        private class GroupDto
        {
            public int Id { get; set; }
            public string YaName { get; set; }
            public DateTime? ImgUpdated { get; set; }
            public int Actual { get; set; }
        }

        private class ItemDto
        {
            [JsonProperty("rkcode")]
            public int RkCode { get; set; }
            public int YeGroup { get; set; }
            public string RkName { get; set; }
            public string YeName { get; set; }
            public string Description { get; set; }
            public double Price { get; set; }
            public int Measure { get; set; }
            public string MeasureUnit { get; set; }
            public int Actual { get; set; }
        }
    }
}
