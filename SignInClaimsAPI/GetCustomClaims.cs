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
    public static class GetCustomClaims
    {
        [FunctionName("GetCustomClaims")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetCustomClaims function processed a request.");

            string consId = req.Query["consId"];

            log.LogInformation($"Cons Id is {consId}");

            return consId != null
                ? (ActionResult)new OkObjectResult(
                    new ResponseContent
                    {
                        version = "1.0.0",
                        status = (int)HttpStatusCode.OK,
                        routingId = Guid.NewGuid().ToString(),
                        nonce = Guid.NewGuid().ToString()
                    })
                : new BadRequestObjectResult("Please pass a consId on the query string");
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
