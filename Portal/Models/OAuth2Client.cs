using System;
using IdentityModel.Client;
using System.Threading.Tasks;
using System.Net.Http;

namespace Portal.Models
{
    public class OAuth2Client
    {
        public TokenResponse GetToken()
        {
            using (var client = new HttpClient())
            {
                var tokenResponse = client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = ApiRequest.Host + ApiRequest.TokenUrl,
                    //Scope = apiScope,
                    ClientId = ApiRequest.ClientId,                    
                    ClientSecret = ApiRequest.Secret
                });
                return tokenResponse.Result;
            }
        }
    }
}
