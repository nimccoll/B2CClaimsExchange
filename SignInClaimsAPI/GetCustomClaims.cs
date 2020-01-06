using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SignInClaimsAPI
{
    public static class GetCustomClaims
    {
        private static HttpClient _httpClient;
        private static readonly string _baseUrl;
        private static readonly string _luminateAPIKey;
        private static readonly string _luminateUserId;
        private static readonly string _luminatePassword;

        static GetCustomClaims()
        {
            string clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);

            _httpClient = new HttpClient();
            _baseUrl = Environment.GetEnvironmentVariable("LuminateOnlineUrl", EnvironmentVariableTarget.Process);
            KeyVaultClient keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    AuthenticationContext context = new AuthenticationContext(authority);
                    ClientCredential credential = new ClientCredential(clientId, clientSecret);
                    AuthenticationResult result = await context.AcquireTokenAsync(resource, credential);

                    return result.AccessToken;
                }));
            _luminateAPIKey = keyVaultClient.GetSecretAsync(Environment.GetEnvironmentVariable("LuminateAPIKey", EnvironmentVariableTarget.Process)).Result.Value;
            _luminateUserId = keyVaultClient.GetSecretAsync(Environment.GetEnvironmentVariable("LuminateUserId", EnvironmentVariableTarget.Process)).Result.Value;
            _luminatePassword = keyVaultClient.GetSecretAsync(Environment.GetEnvironmentVariable("LuminatePassword", EnvironmentVariableTarget.Process)).Result.Value;
        }

        [FunctionName("GetCustomClaims")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetCustomClaims function processed a request.");

            IActionResult result;
            string consId = req.Query["consId"];

            log.LogInformation($"Cons Id is {consId}");

            if (consId != null)
            {
                string response = await LuminateGetSingleSignOnToken(consId, log);
                if (!string.IsNullOrEmpty(response))
                {
                    JObject token = JObject.Parse(response);
                    result = new OkObjectResult(
                        new ResponseContent
                        {
                            version = "1.0.0",
                            status = (int)HttpStatusCode.OK,
                            routingId = token["getSingleSignOnTokenResponse"]["routing_id"].Value<string>(),
                            nonce = token["getSingleSignOnTokenResponse"]["nonce"].Value<string>()
                        });
                }
                else
                {
                    result = new BadRequestObjectResult("Call to Luminate Online failed. See log for further details.");
                }
            }
            else
            {
                result = new BadRequestObjectResult("Please pass a consId on the query string");
            }

            return result;
        }

        private static async Task<string> LuminateGetSingleSignOnToken(string consId, ILogger log)
        {
            string content = string.Empty;
            string url = $"{_baseUrl}/site/SRConsAPI?method=getSingleSignOnToken&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&cons_id={consId}";
            HttpResponseMessage response = await _httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                log.LogError($"Call to Luminate Online getSingleSignOnToken failed. The details of the error are: {errorMessage}");
            }

            return content;
        }

        public class ResponseContent
        {
            public string version { get; set; }
            public int status { get; set; }
            public string routingId { get; set; }
            public string nonce { get; set; }
        }
    }
}
