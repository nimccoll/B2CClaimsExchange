using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace SignInClaimsAPI
{
    public static class CustomRegistration
    {
        [FunctionName("CustomRegistration")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string userId = req.Query["userId"];

            log.LogInformation($"User Id is {userId}");

            return userId != null
                ? (ActionResult)new OkObjectResult(
                    new ResponseContent
                    {
                        version = "1.0.0",
                        status = (int)HttpStatusCode.OK,
                        consId = DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                    })
                : new BadRequestObjectResult("Please pass a userId on the query string");
        }

        public class ResponseContent
        {
            public string version { get; set; }
            public int status { get; set; }
            public string consId { get; set; }
        }
    }
}
