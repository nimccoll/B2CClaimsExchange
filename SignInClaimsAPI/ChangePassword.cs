using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Net;

namespace SignInClaimsAPI
{
    public static class ChangePassword
    {
        private static HttpClient _httpClient;
        private static readonly string _baseUrl;
        private static readonly string _luminateAPIKey;
        private static readonly string _luminateUserId;
        private static readonly string _luminatePassword;

        static ChangePassword()
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

        [FunctionName("ChangePassword")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("ChangePassword function processed a request.");

            IActionResult result;
            string userId = req.Query["userId"];
            string password = req.Query["pwd"];

            log.LogInformation($"User Id is {userId}");
            log.LogInformation($"Password is {password}");

            if (userId != null && password != null)
            {
                string response = await LuminateChangePassword(userId, password, log);
                if (!string.IsNullOrEmpty(response))
                {
                    JObject token = JObject.Parse(response);
                    result = new OkObjectResult(
                        new ResponseContent
                        {
                            version = "1.0.0",
                            status = (int)HttpStatusCode.OK,
                            message = token["updateConsResponse"]["message"].Value<string>(),
                            consId = token["updateConsResponse"]["cons_id"].Value<string>()
                        });
                }
                else
                {
                    result = new BadRequestObjectResult("Call to Luminate Online failed. See log for further details.");
                }
            }
            else
            {
                result = new BadRequestObjectResult("Please pass a userId and pwd on the query string");
            }

            return result;
        }

        private static async Task<string> LuminateChangePassword(string emailAddress, string password, ILogger log)
        {
            string content = string.Empty;
            string url = $"{_baseUrl}/site/SRConsAPI?method=update&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}&user_password={password}";
            HttpResponseMessage response = await _httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                log.LogError($"Call to Luminate Online update failed. The details of the error are: {errorMessage}");
            }

            return content;
        }
    }

    public class ResponseContent
    {
        public string version { get; set; }
        public int status { get; set; }
        public string message { get; set; }
        public string consId { get; set; }
    }
}
