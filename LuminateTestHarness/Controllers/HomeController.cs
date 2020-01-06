using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace LuminateTestHarness.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _baseUrl;
        private readonly string _luminateAPIKey;
        private readonly string _luminateUserId;
        private readonly string _luminatePassword;

        public HomeController()
        {
            _baseUrl = ConfigurationManager.AppSettings["LuminateOnlineUrl"];
            KeyVaultClient keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) =>
                {
                    AuthenticationContext context = new AuthenticationContext(authority);
                    ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ClientId"], ConfigurationManager.AppSettings["ClientSecret"]);
                    AuthenticationResult result = await context.AcquireTokenAsync(resource, credential);

                    return result.AccessToken;
                }));
            _luminateAPIKey = keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateAPIKey"]).Result.Value;
            _luminateUserId = keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminateUserId"]).Result.Value;
            _luminatePassword = keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["LuminatePassword"]).Result.Value;
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
            string emailAddress = Request.Form["emailAddress"];
            string url = $"{_baseUrl}/site/SRConsAPI?method=getUser&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
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
            string emailAddress = Request.Form["emailAddress"];
            string url = $"{_baseUrl}/site/SRConsAPI?method=create&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&primary_email={emailAddress}";
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
            string consId = Request.Form["consId"];
            string url = $"{_baseUrl}/site/SRConsAPI?method=getSingleSignOnToken&api_key={_luminateAPIKey}&login_name={_luminateUserId}&login_password={_luminatePassword}&v=1.0&response_format=json&cons_id={consId}";
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