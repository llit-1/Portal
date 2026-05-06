using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Portal.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Portal.Global
{
    public class NxRestClient
    {
        private readonly string _host;
        private readonly string _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string[] _baseUrls;

        public NxRestClient()
        {
            var section = SettingsInternal.Configuration?.GetSection("NxRest");
            _host = GetRequiredSetting(section?["Host"], "NxRest:Host");
            _port = GetRequiredSetting(section?["Port"], "NxRest:Port");
            _username = GetRequiredSetting(section?["Username"], "NxRest:Username");
            _password = GetRequiredSetting(section?["Password"], "NxRest:Password");

            _baseUrls = new[]
            {
                $"https://{_host}:{_port}"
            };
        }

        public string SystemName => $"{_host}:{_port}";

        public List<NxItemInfo> GetUserGroups()
        {
            var groupsJson = GetArrayFromNx("/rest/v4/userGroups");

            return groupsJson
                .OrderBy(group => (string)group["name"])
                .Select(group => new NxItemInfo
                {
                    Id = group["id"]?.ToString(),
                    Name = group["name"]?.ToString()
                })
                .ToList();
        }

        public string GetUserGroupName(Guid groupId)
        {
            var group = GetUserGroups()
                .FirstOrDefault(item => string.Equals(item.Id, groupId.ToString(), StringComparison.OrdinalIgnoreCase));

            return group?.Name;
        }

        public List<NxUserInfo> GetUsers()
        {
            var usersJson = GetArrayFromNx("/rest/v4/users");

            return usersJson
                .OfType<JObject>()
                .Select(user => new NxUserInfo
                {
                    Id = user["id"]?.ToString(),
                    Name = user["name"]?.ToString(),
                    Login = user["login"]?.ToString()
                })
                .ToList();
        }

        public bool UserExists(string login, IEnumerable<NxUserInfo> users)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                return false;
            }

            var normalizedLogin = login.Trim();
            var nxUsers = users?.ToList() ?? GetUsers();

            return nxUsers.Any(user =>
                string.Equals(user.Id, normalizedLogin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Name, normalizedLogin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Login, normalizedLogin, StringComparison.OrdinalIgnoreCase));
        }

        public NxItemInfo GetOrCreateUserGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                throw new Exception("NX group name is empty.");
            }

            var normalizedName = groupName.Trim();
            var existingGroup = GetUserGroups()
                .FirstOrDefault(item => string.Equals(item.Name?.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));

            if (existingGroup != null)
            {
                return existingGroup;
            }

            var createdGroup = PostObjectToNx("/rest/v4/userGroups", new JObject
            {
                ["name"] = normalizedName,
                ["type"] = "local"
            });

            return new NxItemInfo
            {
                Id = createdGroup["id"]?.ToString(),
                Name = createdGroup["name"]?.ToString() ?? normalizedName
            };
        }

        public void SyncUserGroups(string login, IEnumerable<Guid> requiredGroupIds, IEnumerable<Guid> managedGroupIds)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("NX user login is empty.");
            }

            var user = GetUserByLogin(login);
            var currentGroupIds = (user["groupIds"] as JArray ?? new JArray())
                .Select(item => item.ToString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();
            var requiredGroups = new HashSet<string>(
                requiredGroupIds.Select(item => item.ToString()),
                StringComparer.OrdinalIgnoreCase);
            var managedGroups = new HashSet<string>(
                managedGroupIds.Select(item => item.ToString()),
                StringComparer.OrdinalIgnoreCase);

            var newGroupIds = currentGroupIds
                .Where(groupId => !managedGroups.Contains(groupId) || requiredGroups.Contains(groupId))
                .ToList();

            foreach (var groupId in requiredGroups)
            {
                if (!newGroupIds.Any(item => string.Equals(item, groupId, StringComparison.OrdinalIgnoreCase)))
                {
                    newGroupIds.Add(groupId);
                }
            }

            var userId = user["id"]?.ToString();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new Exception($"NX user '{login}' does not have id.");
            }

            Console.WriteLine($"NX sync. Service user: {_username}");
            Console.WriteLine($"NX sync. Target login: {login}");
            Console.WriteLine($"NX sync. Target user id: {userId}");
            Console.WriteLine($"NX sync. Current groups: {string.Join(", ", currentGroupIds)}");
            Console.WriteLine($"NX sync. New groups: {string.Join(", ", newGroupIds)}");

            PatchObjectToNx($"/rest/v4/users/{Uri.EscapeDataString(userId)}", new JObject
            {
                ["groupIds"] = new JArray(newGroupIds)
            });
        }

        public NxUserAccessState GetUserAccessState(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("NX user login is empty.");
            }

            var user = GetUserByLogin(login);
            var userId = user["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new Exception($"NX user '{login}' does not have id.");
            }

            return new NxUserAccessState
            {
                Login = login,
                UserId = userId,
                Permissions = user["permissions"]?.ToString(),
                GroupIds = (user["groupIds"] as JArray ?? new JArray())
                    .Select(item => item.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList(),
                ResourceAccessRights = (user["resourceAccessRights"] as JObject != null)
                    ? new JObject((JObject)user["resourceAccessRights"])
                    : new JObject()
            };
        }

        public void ClearUserAccess(string login)
        {
            var state = GetUserAccessState(login);
            try
            {
                PatchUserAccess(state.UserId, Array.Empty<string>(), new JObject(), state.Permissions);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("403"))
                {
                    throw;
                }

                Console.WriteLine($"NX clear access fallback for '{login}': {ex.Message}");
                PatchObjectToNx($"/rest/v4/users/{Uri.EscapeDataString(state.UserId)}", new JObject
                {
                    ["groupIds"] = new JArray()
                });
            }
        }

        public void RestoreUserAccess(NxUserAccessState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            PatchUserAccess(
                state.UserId,
                state.GroupIds ?? new List<string>(),
                state.ResourceAccessRights ?? new JObject(),
                state.Permissions);
        }

        private JObject GetUserByLogin(string login)
        {
            try
            {
                return GetObjectFromNx($"/rest/v4/users/{Uri.EscapeDataString(login)}");
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("404"))
                {
                    throw;
                }
            }

            var users = GetArrayFromNx("/rest/v4/users");
            var user = users
                .OfType<JObject>()
                .FirstOrDefault(item =>
                    string.Equals(item["id"]?.ToString(), login, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item["name"]?.ToString(), login, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item["login"]?.ToString(), login, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new Exception($"Пользователь '{login}' на стороне NX не найден.");
            }

            return user;
        }

        private JArray GetArrayFromNx(string path)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Exception lastException = null;
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var token = Login(baseUrl);
                    var request = (HttpWebRequest)WebRequest.Create(baseUrl + path);
                    request.Method = "GET";
                    request.Accept = "application/json";
                    request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        return JArray.Parse(reader.ReadToEnd());
                    }
                }
                catch (Exception ex)
                {
                    lastException = WrapNxException(ex, "GET", baseUrl, path);
                }
            }

            throw lastException ?? new Exception("NX request failed.");
        }

        private JObject GetObjectFromNx(string path)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Exception lastException = null;
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var token = Login(baseUrl);
                    var request = (HttpWebRequest)WebRequest.Create(baseUrl + path);
                    request.Method = "GET";
                    request.Accept = "application/json";
                    request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        return JObject.Parse(reader.ReadToEnd());
                    }
                }
                catch (Exception ex)
                {
                    lastException = WrapNxException(ex, "GET", baseUrl, path);
                }
            }

            throw lastException ?? new Exception("NX request failed.");
        }

        private JObject PatchObjectToNx(string path, JObject body)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Exception lastException = null;
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var token = Login(baseUrl);
                    var request = (HttpWebRequest)WebRequest.Create(baseUrl + path);
                    request.Method = "PATCH";
                    request.Accept = "application/json";
                    request.ContentType = "application/json";
                    request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

                    var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
                    using (var requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                    }

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var responseText = reader.ReadToEnd();
                        return string.IsNullOrWhiteSpace(responseText)
                            ? new JObject()
                            : JObject.Parse(responseText);
                    }
                }
                catch (Exception ex)
                {
                    lastException = WrapNxException(ex, "PATCH", baseUrl, path, body);
                }
            }

            throw lastException ?? new Exception("NX request failed.");
        }

        private void PatchUserAccess(string userId, IEnumerable<string> groupIds, JObject resourceAccessRights, string permissions)
        {
            var body = new JObject
            {
                ["groupIds"] = new JArray((groupIds ?? Array.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item))),
                ["resourceAccessRights"] = resourceAccessRights ?? new JObject()
            };

            PatchObjectToNx($"/rest/v4/users/{Uri.EscapeDataString(userId)}", body);
        }

        private JObject PostObjectToNx(string path, JObject body)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            Exception lastException = null;
            foreach (var baseUrl in _baseUrls)
            {
                try
                {
                    var token = Login(baseUrl);
                    var request = (HttpWebRequest)WebRequest.Create(baseUrl + path);
                    request.Method = "POST";
                    request.Accept = "application/json";
                    request.ContentType = "application/json";
                    request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

                    var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
                    using (var requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                    }

                    using (var response = (HttpWebResponse)request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var responseText = reader.ReadToEnd();
                        return string.IsNullOrWhiteSpace(responseText)
                            ? new JObject()
                            : JObject.Parse(responseText);
                    }
                }
                catch (Exception ex)
                {
                    lastException = WrapNxException(ex, "POST", baseUrl, path, body);
                }
            }

            throw lastException ?? new Exception("NX request failed.");
        }

        private Exception WrapNxException(Exception ex, string method, string baseUrl, string path, JObject body = null)
        {
            if (ex is WebException webException)
            {
                var responseText = TryReadWebExceptionResponse(webException);
                var bodyText = body == null ? string.Empty : $"\nRequest body: {body.ToString(Formatting.None)}";
                var responsePart = string.IsNullOrWhiteSpace(responseText) ? string.Empty : $"\nResponse: {responseText}";

                return new Exception(
                    $"NX {method} {baseUrl}{path} failed: {webException.Message}{bodyText}{responsePart}",
                    ex);
            }

            return new Exception($"NX {method} {baseUrl}{path} failed: {ex.Message}", ex);
        }

        private string TryReadWebExceptionResponse(WebException ex)
        {
            try
            {
                using (var responseStream = ex.Response?.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        return string.Empty;
                    }

                    using (var reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private string Login(string baseUrl)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var request = (HttpWebRequest)WebRequest.Create(baseUrl + "/rest/v1/login/sessions");
            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentType = "application/json";

            var body = JsonConvert.SerializeObject(new
            {
                username = _username,
                password = _password,
                setCookie = false
            });

            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var responseText = reader.ReadToEnd();
                var json = JObject.Parse(responseText);
                var token = json["token"]?.ToString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new Exception("NX did not return token.");
                }

                return token;
            }
        }

        private static string GetRequiredSetting(string value, string key)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception($"Missing NX setting: {key}.");
            }

            return value;
        }

        public class NxItemInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class NxUserInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Login { get; set; }
        }

        public class NxUserAccessState
        {
            public string Login { get; set; }
            public string UserId { get; set; }
            public string Permissions { get; set; }
            public List<string> GroupIds { get; set; }
            public JObject ResourceAccessRights { get; set; }
        }
    }
}
