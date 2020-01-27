using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SsoServiceCaller
{
    public class ApiCaller
    {
        private readonly Secrets _secrets;
        private readonly ConfigSettings _configSettings;

        public ApiCaller(IOptions<Secrets> secretOptions,
            IOptions<ConfigSettings> configSettings)
        {
            _secrets = secretOptions.Value;
            _configSettings = configSettings.Value;
        }

        public async Task<string> CallApiAsync()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (errors == SslPolicyErrors.None)
                    {
                        //ServicePointManager.ServerCertificateValidationCallback()
                        return true;
                    }

                    throw new AuthenticationException(
                        $"SSL Server certificate validation failed when trying to connect to {message.RequestUri}, Error: {errors}.");
                }
            };
            var certificates = new X509Certificate2Collection();
            certificates.Import(_configSettings.CertFileName, "", X509KeyStorageFlags.DefaultKeySet);

            handler.ClientCertificates.AddRange(certificates);
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(360)
            };

            var result = await client.GetAsync(_configSettings.TokenUrl).ConfigureAwait(false);

            var content = await result.Content.ReadAsStringAsync();
            dynamic parsedContent = JObject.Parse(content);
            var token = parsedContent.token.Value;
            var jwtToken = new JwtSecurityToken(token);
            Console.WriteLine("Token Claims:");
            var tokenClaims = JsonConvert.SerializeObject(jwtToken.Claims);
            Console.WriteLine(tokenClaims);
            return token;
        }
    }
}
