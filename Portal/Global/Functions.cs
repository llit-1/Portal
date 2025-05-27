using Microsoft.AspNetCore.Http;
using Portal.Models.MSSQL;
using System;
using System.Linq;
using static Portal.Controllers.StockController;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

namespace Portal.Global
{
    public static class Functions
    {
        // Не используется
        public static bool CheckSessionID(DB.MSSQLDBContext dbSqlContext, string sessionID)
        {
            var session = dbSqlContext.UserSessions.FirstOrDefault(x => x.SessionID == sessionID);

            if (session == null)
            {
                return false;
            }

            if (DateTime.Now > session.Date.AddHours(1))
            {
                return false;
            }

            return true;

        }
        public static async Task<TokenData> GetTokenFromApi()
        {
            try
            {
                var user = new UserData
                {
                    login = "assassin",
                    password = "nothing is true everything is permitted"
                };

                using var httpClient = new HttpClient();

                var json = JsonSerializer.Serialize(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync("https://warehouseapi.ludilove.ru/api/Authorization/login", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                // Тут мы просто оборачиваем строку в объект
                return new TokenData { token = responseContent.Trim('"') };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении токена: {ex.Message}");
                return new TokenData();
            }
        }

        public class UserData
        {
            public string login { get; set; }
            public string password { get; set; }
        }

        public class TokenData
        {
            public string token { get; set; }
        }

    }
}

    