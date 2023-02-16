using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.PowerBi
{
    // Приложение с правами на доступ к отчетам Power Bi. Настраивается в разделе Azure AD -> Регистрация приложений
    public class PBIAppIntegration
    {
        public Guid ApplicationId { get; set; }
        public string ApplicationSecret { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string AuthorityUrl { get; set; }
        public string ResourceUrl { get; set; }
        public string ApiUrl { get; set; }
        public string EmbedUrlBase { get; set; }
        
    }
}
