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
    public static class CustomRegistration
    {
        private static HttpClient _httpClient;
        private static readonly string _baseUrl;
        private static readonly string _luminateAPIKey;
        private static readonly string _luminateUserId;
        private static readonly string _luminatePassword;

        static CustomRegistration()
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

        [FunctionName("CustomRegistration")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CustomRegistration function processed a request.");

            IActionResult result;
            string userId = req.Query["userId"];

            log.LogInformation($"User Id is {userId}");

            if (userId != null)
            {
                string response = await LuminateGetUser(userId, log); // Check if the user already exists in Luminate Online
                if (!string.IsNullOrEmpty(response))
                {
                    JObject user = JObject.Parse(response);
                    result = new OkObjectResult(
                    new ResponseContent
                    {
                        version = "1.0.0",
                        status = (int)HttpStatusCode.OK,
                        consId = user["getConsResponse"]["cons_id"].Value<string>()
                    });
                }
                else
                {
                    response = await LuminateCreateUser(userId, log); // Create a new user in Luminate Online
                    if (!string.IsNullOrEmpty(response))
                    {
                        JObject newUser = JObject.Parse(response);
                        result = new OkObjectResult(
                        new ResponseContent
                        {
                            version = "1.0.0",
                            status = (int)HttpStatusCode.OK,
                            consId = newUser["createConsResponse"]["cons_id"].Value<string>()
                        });
                    }
                    else
                    {
                        result = new BadRequestObjectResult("Call to Luminate Online failed. See log for further details.");
                    }
                }
            }
            else
            {
                result = new BadRequestObjectResult("Please pass a userId on the query string");
            }

            return result;
        }

        private static async Task<string> LuminateGetUser(string emailAddress, ILogger log)
        {
            string content = string.Empty;
            string url = $"{_baseUrl}/site/SRConsAPI?method=getUser&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
            HttpResponseMessage response = await _httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                log.LogError($"Call to Luminate Online getUser failed. The details of the error are: {errorMessage}");
            }

            return content;
        }

        private static async Task<string> LuminateCreateUser(string emailAddress, ILogger log)
        {
            string content = string.Empty;
            string url = $"{_baseUrl}/site/SRConsAPI?method=create&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
            HttpResponseMessage response = await _httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                log.LogError($"Call to Luminate Online create failed. The details of the error are: {errorMessage}");
            }

            return content;
        }

        public class ResponseContent
        {
            public string version { get; set; }
            public int status { get; set; }
            public string consId { get; set; }
        }
    }
}
