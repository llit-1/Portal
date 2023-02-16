using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace Portal.Services
{
    public class StreamVideoService : IStreamVideoService
    {
        private HttpClient client;

        public StreamVideoService()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            
            client = new HttpClient(clientHandler);
        }

        public async Task<Stream> GetVideoStream(string url)
        {
            return await client.GetStreamAsync(url);
        }

        ~StreamVideoService()
        {
            if (client != null)
            {
                client.Dispose();
            }                
        }
    }
}
