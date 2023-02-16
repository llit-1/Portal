using System;
using System.Collections.Generic;
using System.Text;
using RKNet_Model.VMS.NX;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections;


namespace module_NX
{
    public partial class NX
    {
        public RKNet_Model.Result<string> GetSystemName(NxSystem nxSystem)
        {
            var result = new RKNet_Model.Result<string>();
            
            var authString = Convert.ToBase64String(Encoding.Default.GetBytes(nxSystem.Login + ":" + nxSystem.Password));
            var request = new WebClient();
            request.Headers["Authorization"] = "Basic " + authString;
            
            var connectionString = "http://" + nxSystem.Host + ":" + nxSystem.Port + "/ec2/getSettings?format=json";

            try
            {
                var stream = request.OpenRead(connectionString);

                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(stream))
                {
                    var jsn = sr.ReadToEnd();

                    var settings = JsonConvert.DeserializeObject<List<Api.JsonItem>>(jsn);
                    var systemName = settings.First(s => s.name == "systemName");

                    result.Data = systemName.value;
                }

                stream.Flush();
                stream.Close();
                request.Dispose();
            }

            catch(Exception e)
            {
                result.Ok = false;
                result.Title = "Ошибка запроса к системе NX: module_NX -> GetSystemName";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();

            }
            return result;
        }
    }
}
