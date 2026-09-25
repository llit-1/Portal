using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Portal.Global
{
    /// <summary>
    /// Быстрые стопы доставки через новую апишку (yeapi, /api/stop) — те же, что ставит Office:
    /// стоп-пакет уровня 2 на одну позицию и одну точку до 00:00 по Москве.
    /// Позиция в yeapi — код R-Keeper, точка — GUID из таблицы Locations.
    /// </summary>
    public class YeapiStopClient
    {
        /// <summary>Роль, при которой «Меню доставки» ставит и показывает стопы через yeapi, а не через старую апишку.</summary>
        public const string RoleName = "menuDelivery_stops_yeapi";

        private const int QuickStopLevel = 2;
        private const int AuditorStopLevel = 1;

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private static readonly JsonSerializerSettings WriteSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "yyyy-MM-ddTHH:mm:ss",
        };

        private readonly string _stopUrl;

        public YeapiStopClient()
        {
            _stopUrl = (SettingsInternal.Configuration?["YeapiStops:StopUrl"] ?? "https://yeapi.ludilove.ru/api/stop").TrimEnd('/');
        }

        /// <summary>Коды R-Keeper позиций, которые сейчас в стопе на точке (любого уровня).</summary>
        public HashSet<int> GetStoppedRkCodes(Guid locationGuid)
        {
            var now = MoscowNow();
            return GetPacks()
                .SelectMany(pack => pack.Stops)
                .Where(stop => stop.LocationGUID == locationGuid && IsEffective(stop, now))
                .Select(stop => stop.Item)
                .ToHashSet();
        }

        /// <summary>Ставит позицию в стоп на точке до 00:00 по Москве.</summary>
        public RKNet_Model.Result<string> SetQuickStop(Guid locationGuid, int rkCode, string itemName, string userName)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                var now = MoscowNow();
                var alreadyStopped = GetPacks()
                    .SelectMany(pack => pack.Stops)
                    .Any(stop => stop.LocationGUID == locationGuid && stop.Item == rkCode && IsEffective(stop, now));
                if (alreadyStopped)
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Позиция \"{itemName}\" уже находится в стопе на этой точке.";
                    return result;
                }

                var end = now.Date.AddDays(1);
                var pack = new StopPackRequest
                {
                    ItemId = rkCode,
                    ItemName = itemName,
                    Begin = now,
                    End = end,
                    User = userName,
                    PermissionLevel = QuickStopLevel,
                    Stops = new List<StopRequest>
                    {
                        new StopRequest { LocationGUID = locationGuid, Item = rkCode, Begin = now, End = end },
                    },
                };
                Send(HttpMethod.Post, null, pack);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            return result;
        }

        /// <summary>Снимает быстрые стопы позиции на точке. Стоп аудитора (уровень 1) отсюда снять нельзя.</summary>
        public RKNet_Model.Result<string> RemoveQuickStop(Guid locationGuid, int rkCode)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                var now = MoscowNow();
                var packs = GetPacks();
                bool Matches(StopDto stop) => stop.LocationGUID == locationGuid && stop.Item == rkCode && IsEffective(stop, now);

                if (packs.Any(pack => pack.PermissionLevel == AuditorStopLevel && pack.Stops.Any(Matches)))
                {
                    result.Ok = false;
                    result.ErrorMessage = "Позиция заблокирована аудитором в разделе «Стоп-листы» Office — снять стоп отсюда нельзя.";
                    return result;
                }

                var changed = false;
                foreach (var pack in packs.Where(pack => pack.PermissionLevel == QuickStopLevel))
                {
                    var remaining = pack.Stops.Where(stop => !Matches(stop)).ToList();
                    if (remaining.Count == pack.Stops.Count)
                        continue;

                    changed = true;
                    if (remaining.Count == 0)
                    {
                        Send(HttpMethod.Delete, pack.Id, null);
                        continue;
                    }

                    Send(HttpMethod.Put, pack.Id, new StopPackRequest
                    {
                        ItemId = pack.ItemId,
                        ItemName = pack.ItemName,
                        LocationGUID = pack.LocationGUID,
                        LocationName = pack.LocationName,
                        Begin = pack.Begin,
                        End = pack.End,
                        User = pack.User,
                        PermissionLevel = pack.PermissionLevel,
                        Stops = remaining
                            .Select(stop => new StopRequest { LocationGUID = stop.LocationGUID, Item = stop.Item, Begin = stop.Begin, End = stop.End })
                            .ToList(),
                    });
                }

                if (!changed)
                {
                    result.Ok = false;
                    result.ErrorMessage = "Действующий стоп по позиции не найден — возможно, его уже сняли.";
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

        private List<StopPackDto> GetPacks()
        {
            using var response = Http.GetAsync(_stopUrl).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Новая апишка стопов ответила {(int)response.StatusCode}: {body}");

            return string.IsNullOrWhiteSpace(body)
                ? new List<StopPackDto>()
                : JsonConvert.DeserializeObject<List<StopPackDto>>(body) ?? new List<StopPackDto>();
        }

        private void Send(HttpMethod method, int? id, StopPackRequest body)
        {
            using var request = new HttpRequestMessage(method, id.HasValue ? $"{_stopUrl}/{id.Value}" : _stopUrl);
            if (body != null)
                request.Content = new StringContent(JsonConvert.SerializeObject(body, WriteSettings), Encoding.UTF8, "application/json");

            using var response = Http.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new Exception($"Новая апишка стопов ответила {(int)response.StatusCode}: {text}");
            }
        }

        private static bool IsEffective(StopDto stop, DateTime now) => stop.Begin <= now && stop.End > now;

        /// <summary>yeapi хранит время стопов по Москве без смещения.</summary>
        private static DateTime MoscowNow()
        {
            foreach (var id in new[] { "Russian Standard Time", "Europe/Moscow" })
            {
                try
                {
                    var moscow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id));
                    return DateTime.SpecifyKind(new DateTime(moscow.Year, moscow.Month, moscow.Day, moscow.Hour, moscow.Minute, moscow.Second), DateTimeKind.Unspecified);
                }
                catch (TimeZoneNotFoundException)
                {
                }
            }
            return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(3), DateTimeKind.Unspecified);
        }

        private class StopPackDto
        {
            public int Id { get; set; }
            public int? ItemId { get; set; }
            public string ItemName { get; set; }
            public Guid? LocationGUID { get; set; }
            public string LocationName { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
            public string User { get; set; }
            public int PermissionLevel { get; set; }
            public List<StopDto> Stops { get; set; } = new List<StopDto>();
        }

        private class StopDto
        {
            public int Id { get; set; }
            public Guid LocationGUID { get; set; }
            public int Item { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
        }

        private class StopPackRequest
        {
            public int? ItemId { get; set; }
            public string ItemName { get; set; }
            public Guid? LocationGUID { get; set; }
            public string LocationName { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
            public string User { get; set; }
            public int PermissionLevel { get; set; }
            public List<StopRequest> Stops { get; set; }
        }

        private class StopRequest
        {
            public Guid LocationGUID { get; set; }
            public int Item { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
        }
    }
}
