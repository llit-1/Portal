using System;
using System.Collections.Generic;
using System.Text;
using RKNet_Model.VMS.NX;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections;
using System.Drawing;

namespace module_NX
{
    public partial class NX
    {
        public RKNet_Model.Result<FullInfo> GetFullInfo(NxSystem nxSystem)
        {
            var result = new RKNet_Model.Result<FullInfo>();

            var authString = Convert.ToBase64String(Encoding.Default.GetBytes(nxSystem.Login + ":" + nxSystem.Password));
            var request = new WebClient();
            request.Headers["Authorization"] = "Basic " + authString;

            var connectionString = "http://" + nxSystem.Host + ":" + nxSystem.Port + "/ec2/getFullInfo?format=json";

            try
            {
                var stream = request.OpenRead(connectionString);

                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(stream))
                {
                    var jsn = sr.ReadToEnd();
                    result.Data = JsonConvert.DeserializeObject<FullInfo>(jsn);
                }

                stream.Flush();
                stream.Close();
                request.Dispose();
            }

            catch (Exception e)
            {
                result.Ok = false;
                result.Title = "Ошибка запроса к системе NX: module_NX -> GetFullInfo";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();

            }
            return result;
        }

        public class FullInfo
        {
            public List<camera> cameras { get; set; }
            public List<server> servers { get; set; }
            public List<cameraUserAttributes> cameraUserAttributesList { get; set; }
            public List<allPropertie> allProperties { get; set; }

            public class camera
            {
                public string id { get; set; }
                public string parentId { get; set; }
                public string name { get; set; }
            }

            public class server
            {
                public string id { get; set; }
                public string name { get; set; }
                public string sysName { get; set; }
                public List<cameraUserAttributes> cameras { get; set; }

                public server()
                {
                    cameras = new List<cameraUserAttributes>();
                }
            }

            public class cameraUserAttributes
            {
                public string cameraId { get; set; }
                public string cameraName { get; set; }
                public string preferredServerId { get; set; }
                public Bitmap cameraImage { get; set; }
            }

            public class allPropertie
            {
                public  string name { get; set; }
                public string resourceId { get; set; }
                public string value { get; set; }
            }
        }
    }
}
