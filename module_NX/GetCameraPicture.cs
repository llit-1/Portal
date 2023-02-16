using System;
using System.Text;
using System.Net;
using System.Drawing;
using RKNet_Model.VMS.NX;

namespace module_NX
{
    public partial class NX
    {
        public RKNet_Model.Result<Bitmap> GetCameraPicture(DateTime dateTime, NxCamera camera, int height)
        {
            var result = new RKNet_Model.Result<Bitmap>();
            Bitmap camPicture;

            var authString = Convert.ToBase64String(Encoding.Default.GetBytes(camera.NxSystem.Login + ":" + camera.NxSystem.Password));
            var request = new WebClient();
            request.Headers["Authorization"] = "Basic " + authString;

            if (height < 300) height = 300;
            if (height > 10000) height = 10000;

            var timestring = dateTime.ToString("yyyy-MM-ddTHH") + "%3A" + dateTime.ToString("mm") + "%3A00.000";
            var connectionString = "http://" + camera.NxSystem.Host + ":" + camera.NxSystem.Port + "/ec2/cameraThumbnail?cameraId=" + camera.Guid + "&time=" + timestring + "&height=" + height.ToString() + "&imageFormat=jpg";


            try
            {
                var stream = request.OpenRead(connectionString);
                var img = Image.FromStream(stream);

                stream.Flush();
                stream.Close();
                request.Dispose();
                
                camPicture = new Bitmap(img);
                result.Data = camPicture;
                return result;
            }

            catch(Exception e)
            {
                result.Ok = false;
                result.Title = "Ошибка получения снимка с камеры: module_NX -> GetCameraPicture";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return result;
            }
        }

        public RKNet_Model.Result<Bitmap> GetCameraPicture(NxSystem nxSystem, DateTime dateTime, string cameraId, int height)
        {
            var result = new RKNet_Model.Result<Bitmap>();
            Bitmap camPicture;

            var authString = Convert.ToBase64String(Encoding.Default.GetBytes(nxSystem.Login + ":" + nxSystem.Password));
            var request = new WebClient();
            request.Headers["Authorization"] = "Basic " + authString;

            if (height < 300) height = 300;
            if (height > 10000) height = 10000;

            var timestring = dateTime.ToString("yyyy-MM-ddTHH") + "%3A" + dateTime.ToString("mm") + "%3A00.000";
            var connectionString = "http://" + nxSystem.Host + ":" + nxSystem.Port + "/ec2/cameraThumbnail?cameraId=" + cameraId + "&time=" + timestring + "&height=" + height.ToString() + "&imageFormat=jpg";


            try
            {
                var stream = request.OpenRead(connectionString);
                var img = Image.FromStream(stream);

                stream.Flush();
                stream.Close();
                request.Dispose();

                camPicture = new Bitmap(img);
                result.Data = camPicture;
                return result;
            }

            catch (Exception e)
            {
                result.Ok = false;
                result.Title = "Ошибка получения снимка с камеры: module_NX -> GetCameraPicture";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return result;
            }
        }
    }
}
