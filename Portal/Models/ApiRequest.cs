using System;
using IdentityModel.Client;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RKNet_Model;
using Newtonsoft.Json;
using System.Text;

namespace Portal.Models
{
    public static class ApiRequest
    {
        public static string Host; // хост присваивается в Startup

        //private static string Host = "https://api.ludilove.ru";
        //private static string Host = "https://localhost:5224";

        private static TokenResponse tokenResponse;
        private static DateTime tokenExpiration;
        
        public static string TokenUrl = "/connect/token";
        public static string ClientId = "RKNetWebPortal";
        public static string Secret = "Secret+Portal=2022";

        //-----------------------------------------------------------------------
        // Получение токена авторизации
        //-----------------------------------------------------------------------
        private static string OAuth2Token()
        {
            if (tokenResponse == null)
            {
                tokenResponse = new OAuth2Client().GetToken();
                tokenExpiration = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
            }
            else
            {
                if (DateTime.Now >= tokenExpiration)
                {
                    tokenResponse = new OAuth2Client().GetToken();
                    tokenExpiration = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
                }
            }
            return tokenResponse.AccessToken;
        }

        //-----------------------------------------------------------------------
        // Р-Кипер
        //-----------------------------------------------------------------------
        // запрос меню р-кипер
        public static RKNet_Model.Result<List<RKNet_Model.Menu.rkMenuItem>> GetRkMenu()
        {            
            var result = new RKNet_Model.Result<List<RKNet_Model.Menu.rkMenuItem>>();
            
            var requestUrl = Host + "/R_Keeper/GetRkMenu";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.Menu.rkMenuItem>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;           
        }

        //-----------------------------------------------------------------------
        // Меню доставки
        //-----------------------------------------------------------------------
        // запрос категории меню
        public static RKNet_Model.Result<RKNet_Model.Menu.Category> GetMenuCategory(int Id, bool withImage) // если Id == 0, то вызвращается корневой раздел меню
        {
            var result = new RKNet_Model.Result<RKNet_Model.Menu.Category>();

            var requestUrl = Host + $"/Menu/GetMenuCategory?Id={Id}&withImage={withImage}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<RKNet_Model.Menu.Category>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос пути к категории меню
        public static RKNet_Model.Result<List<RKNet_Model.Menu.Category>> GetMenuCategoryPath(int Id)
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.Menu.Category>>();
            var requestUrl = Host + "/Menu/GetMenuCategoryPath?Id=" + Id.ToString();

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.Menu.Category>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // изменение категории меню
        public static RKNet_Model.Result<string> EditMenuCategory(RKNet_Model.Menu.Category category)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/EditMenuCategory";
            try
            {
                var jsonObject = Newtonsoft.Json.JsonConvert.SerializeObject(category);
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.PostAsJsonAsync(requestUrl, category).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        // добавление категории меню
        public static RKNet_Model.Result<string> AddMenuCategory(RKNet_Model.Menu.Category category)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/AddMenuCategory";
            try
            {
                var jsonObject = Newtonsoft.Json.JsonConvert.SerializeObject(category);
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.PostAsJsonAsync(requestUrl, category).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        // удаление категории меню
        public static RKNet_Model.Result<string> DeleteMenuCategory(int Id)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/DeleteMenuCategory?Id=" + Id.ToString(); ;
            try
            {
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.GetAsync(requestUrl).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        // запрос изображения категории меню
        public static RKNet_Model.Result<byte[]> GetMenuCategoryImage(int Id)
        {
            var result = new RKNet_Model.Result<byte[]>();
            var requestUrl = Host + "/Menu/GetMenuCategoryImage?Id=" + Id.ToString();

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;                                        
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<byte[]>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос изображения позиции меню
        public static RKNet_Model.Result<byte[]> GetMenuItemImage(int Id)
        {
            var result = new RKNet_Model.Result<byte[]>();
            var requestUrl = Host + "/Menu/GetMenuItemImage?Id=" + Id.ToString();

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<byte[]>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос позиции меню по Id
        public static RKNet_Model.Result<RKNet_Model.Menu.Item> GetMenuItem(int Id, bool withImage)
        {
            var result = new RKNet_Model.Result<RKNet_Model.Menu.Item>();
            var requestUrl = $"{Host}/Menu/GetMenuItem?Id={Id}&withImage={withImage}";
            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<RKNet_Model.Menu.Item>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос позиции меню по коду Р-Кипер
        public static RKNet_Model.Result<RKNet_Model.Menu.Item> GetMenuItemByRkCode(int rkCode, bool withImage)
        {
            var result = new RKNet_Model.Result<RKNet_Model.Menu.Item>();
            var requestUrl = $"{Host}/Menu/GetMenuItemByRkCode?rkCode={rkCode}&withImage={withImage}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<RKNet_Model.Menu.Item>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос отключенных в Р-Кипер позиций меню
        public static RKNet_Model.Result<List<RKNet_Model.Menu.Item>> GetDisabledItems()
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.Menu.Item>>();
            var requestUrl = Host + $"/Menu/GetDisabledItems";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.Menu.Item>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }


        // запрос справочника типов количества блюд
        public static RKNet_Model.Result<List<RKNet_Model.Menu.MeasureUnit>> GetMeasureUnits()
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.Menu.MeasureUnit>>();
            var requestUrl = Host + "/Menu/GetMeasureUnits";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.Menu.MeasureUnit>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // изменение позиции меню
        public static RKNet_Model.Result<string> EditMenuItem(RKNet_Model.Menu.Item item)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/EditMenuItem";
            try
            {
                var jsonObject = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.PostAsJsonAsync(requestUrl, item).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        // добавление позиции меню
        public static RKNet_Model.Result<string> AddMenuItem(RKNet_Model.Menu.Item item)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/AddMenuItem";
            try
            {
                var jsonObject = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.PostAsJsonAsync(requestUrl, item).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        // удаление позиции меню
        public static RKNet_Model.Result<string> DeleteMenuItem(int Id)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/Menu/DeleteMenuItem?Id=" + Id.ToString(); ;
            try
            {
                using (var client = new HttpClient())
                {
                    client.SetBearerToken(OAuth2Token());
                    var response = client.GetAsync(requestUrl).Result;

                    // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        tokenResponse = null;
                        client.SetBearerToken(OAuth2Token());
                        response = client.GetAsync(requestUrl).Result;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = response.StatusCode.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        //-----------------------------------------------------------------------
        // Кассовые клиенты
        //-----------------------------------------------------------------------
        // получение списка кассовых клиентов
        public static RKNet_Model.Result<List<RKNet_Model.CashClient.CashClient>> GetCashClients()
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.CashClient.CashClient>>();
            var requestUrl = Host + "/CashClients/GetCashClients";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.CashClient.CashClient>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // получение списка файлов версий обновлений кассовых клиентов
        public static RKNet_Model.Result<List<RKNet_Model.CashClient.ClientVersion>> GetCashClientsVersions()
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.CashClient.ClientVersion>>();
            var requestUrl = Host + "/CashClients/GetCashClientsVersions";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.CashClient.ClientVersion>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // обновление всех кассовых клиентов
        public static RKNet_Model.Result<string> UpdateAllClients(string version)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/CashClients/UpdateAllClients?version=" + version;

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // обновление одного кассового клиента
        public static RKNet_Model.Result<string> UpdateOneClient(string clientId, string version)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/CashClients/UpdateOneClient?clientId=" + clientId + "&version=" + version;

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // отмена обновления одного клиента
        public static RKNet_Model.Result<string> CancelClientUpdate(string clientId)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/CashClients/CancelClientUpdate?clientId=" + clientId;

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // включение/выключение автообновления клиентов
        public static RKNet_Model.Result<string> CashClientsAutoUpdate(bool isEnabled)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + "/CashClients/CashClientsAutoUpdate?isEnabled=" + isEnabled;

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        //-----------------------------------------------------------------------
        // Стопы доставки
        //-----------------------------------------------------------------------

        // постановка блюда меню доставки на стоп
        public static RKNet_Model.Result<string> SetStopDeliveryItem(int ttId, int itemId, string userLogin)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + $"/menu/SetStopDeliveryItem?ttId={ttId}&itemId={itemId}&userLogin={userLogin}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // снятие блюда для доставки со стопа
        public static RKNet_Model.Result<string> RemoveStopDeliveryItem(int ttId, int itemId, string userLogin)
        {
            var result = new RKNet_Model.Result<string>();
            var requestUrl = Host + $"/menu/RemoveStopDeliveryItem?ttId={ttId}&itemId={itemId}&userLogin={userLogin}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<string>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос данных пользователя по логину
        public static RKNet_Model.Result<RKNet_Model.Account.User> GetUser(string userLogin)
        {
            var result = new RKNet_Model.Result<RKNet_Model.Account.User>();
            var requestUrl = Host + $"/Access/GetUser?userLogin={userLogin}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<RKNet_Model.Account.User>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос списка стопов доставки по ТТ
        public static RKNet_Model.Result<List<RKNet_Model.MSSQL.DeliveryItemStop>> GetDeliveryStopsByTT(string ttCode)
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.MSSQL.DeliveryItemStop>>();
            var requestUrl = Host + $"/Menu/GetDeliveryStopsByTT?ttCode={ttCode}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.MSSQL.DeliveryItemStop>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        // запрос кассовых стопов по тт
        public static RKNet_Model.Result<List<RKNet_Model.MSSQL.SkuStop>> GetCashStopsByTT(string ttCode)
        {
            var result = new RKNet_Model.Result<List<RKNet_Model.MSSQL.SkuStop>>();
            var requestUrl = Host + $"/Menu/GetCashStopsByTT?ttCode={ttCode}";

            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var response = client.GetAsync(requestUrl).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.GetAsync(requestUrl).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<RKNet_Model.Result<List<RKNet_Model.MSSQL.SkuStop>>>(json);
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = response.StatusCode.ToString();
                }
            }
            return result;
        }

        public static void SendMessage(MessageOrder messageOrder)
        {
            var requestUrl = Host + "/CashClients/SendMessages";
            using (var client = new HttpClient())
            {
                client.SetBearerToken(OAuth2Token());
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                var json = JsonConvert.SerializeObject(messageOrder, settings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync(requestUrl, content).Result;

                // ошибка авторизации (если по какой-то причине токен перестал быть актуальным, например, перезагрузка сервера)
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    tokenResponse = null;
                    client.SetBearerToken(OAuth2Token());
                    response = client.PostAsync(requestUrl, content).Result;
                }
                
            }
        }



    }

}
