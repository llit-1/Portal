using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

                return new TokenData { token = responseContent.Trim('"') };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении токена: {ex.Message}");
                return new TokenData();
            }
        }

        public static async Task<SalaryCalculationResult> CalculateSalariesAsync(List<Guid> guids)
        {
            List<Guid> guidList = (guids ?? new List<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (!guidList.Any())
            {
                return SalaryCalculationResult.Fail(400, "Список табелей для расчета пуст");
            }

            const string requestUrl = "http://rknet-server:1571/api/CalculateSalary/SetTimeSheetSalaries";

            try
            {
                using HttpClient httpClient = new HttpClient();
                string payload = JsonSerializer.Serialize(guidList);
                using StringContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await httpClient.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return SalaryCalculationResult.Fail((int)response.StatusCode, "Не удалось выполнить массовый расчет зарплаты");
                }
            }
            catch
            {
                return SalaryCalculationResult.Fail(502, "Ошибка обращения к сервису массового расчета зарплаты");
            }

            return SalaryCalculationResult.Ok(guidList.Count);
        }

        public class SalaryCalculationResult
        {
            public bool IsSuccess { get; set; }
            public int StatusCode { get; set; }
            public string Message { get; set; }
            public int Calculated { get; set; }

            public static SalaryCalculationResult Ok(int calculated)
            {
                return new SalaryCalculationResult
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Message = string.Empty,
                    Calculated = calculated
                };
            }

            public static SalaryCalculationResult Fail(int statusCode, string message)
            {
                return new SalaryCalculationResult
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    Message = message ?? "Ошибка расчета зарплаты",
                    Calculated = 0
                };
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
