using System.Threading.Tasks;
using System.IO;

namespace Portal.Services
{
    public interface IStreamVideoService
    {
        Task<Stream> GetVideoStream(string url);
    }
}
