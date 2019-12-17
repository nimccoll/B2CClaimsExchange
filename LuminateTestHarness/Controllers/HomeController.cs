using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LuminateTestHarness.Controllers
{
    public class HomeController : Controller
    {
        private KeyVaultClient _keyVaultClient;

        public HomeController()
        {
            _keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    AuthenticationContext context = new AuthenticationContext(authority);
                    ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["ClientSecret"]);
                    AuthenticationResult result = await context.AcquireTokenAsync(resource, credential);

                    return result.AccessToken;
                }));
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetUser()
        {
            return View();
        }

        [HttpPost]
        [ActionName("GetUser")]
        [ValidateAntiForgeryToken]
        public ActionResult GetUserPost()
        {
            string baseUrl = ConfigurationManager.AppSettings["LuminateOnlineUrl"];
            string luminateAPIKey = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateAPIKey"]).Result.Value;
            string luminateUserId = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateUserId"]).Result.Value;
            string luminatePassword = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminatePassword"]).Result.Value;
            string emailAddress = Request.Form["emailAddress"];
            string url = $"{baseUrl}/site/SRConsAPI?method=getUser&api_key={luminateAPIKey}&login_name={luminateUserId}&login_password={luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
            string response = CallLuminateOnline(url);
            JObject user = JObject.Parse(response);
            ViewBag.Email = emailAddress;
            ViewBag.ConsId = user["getConsResponse"]["cons_id"].Value<string>();

            return View();
        }

        [HttpGet]
        public ActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ActionName("CreateUser")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUserPost()
        {
            string baseUrl = ConfigurationManager.AppSettings["LuminateOnlineUrl"];
            string luminateAPIKey = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateAPIKey"]).Result.Value;
            string luminateUserId = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateUserId"]).Result.Value;
            string luminatePassword = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminatePassword"]).Result.Value;
            string emailAddress = Request.Form["emailAddress"];
            string url = $"{baseUrl}/site/SRConsAPI?method=create&api_key={luminateAPIKey}&login_name={luminateUserId}&login_password={luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
            string response = CallLuminateOnline(url);
            JObject user = JObject.Parse(response);
            ViewBag.Email = emailAddress;
            ViewBag.ConsId = user["createConsResponse"]["cons_id"].Value<string>();

            return View();
        }

        [HttpGet]
        public ActionResult GetToken()
        {
            return View();
        }

        [HttpPost]
        [ActionName("GetToken")]
        [ValidateAntiForgeryToken]
        public ActionResult GetTokenPost()
        {
            string baseUrl = ConfigurationManager.AppSettings["LuminateOnlineUrl"];
            string luminateAPIKey = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateAPIKey"]).Result.Value;
            string luminateUserId = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateUserId"]).Result.Value;
            string luminatePassword = _keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminatePassword"]).Result.Value;
            string consId = Request.Form["consId"];
            string url = $"{baseUrl}/site/SRConsAPI?method=getSingleSignOnToken&api_key={luminateAPIKey}&login_name={luminateUserId}&login_password={luminatePassword}&v=1.0&response_format=json&cons_id={consId}";
            string response = CallLuminateOnline(url);
            JObject token = JObject.Parse(response);
            ViewBag.ConsId = consId;
            ViewBag.RoutingId = token["getSingleSignOnTokenResponse"]["routing_id"].Value<string>();
            ViewBag.Nonce = token["getSingleSignOnTokenResponse"]["nonce"].Value<string>();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private string CallLuminateOnline(string url)
        {
            string content = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Accept = "application/json";
            WebResponse response = request.GetResponse();
            content = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return content;
        }
    }
}