//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C User Management Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using AADB2C.GraphService;
using AADB2C.UserAdmin.Models;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AADB2C.UserAdmin.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _tenant = ConfigurationManager.AppSettings["b2c:Tenant"];
        private readonly string _clientId = ConfigurationManager.AppSettings["b2c:ClientId"];
        private readonly string _clientSecret = ConfigurationManager.AppSettings["b2c:ClientSecret"];

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AccountModel model)
        {
            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            bool success = await b2CGraphClient.CreateAccount("emailAddress",
                model.signInName,
                model.issuer,
                model.issuerUserId,
                model.email,
                model.password,
                model.displayName,
                model.firstName,
                model.lastName,
                model.extension_jdrfConsId,
                true);
            if (success)
            {
                ViewBag.Message = "User created successfully!";
            }
            else
            {
                ViewBag.Message = "User creation failed!";
            }
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string emailAddress)
        {
            AccountModel model;
            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            GraphAccountModel user = await b2CGraphClient.GetUser(emailAddress);

            if (user != null)
            {
                model = new AccountModel()
                {
                    signInName = user.signInNames[0].value,
                    displayName = user.displayName,
                    firstName = user.givenName,
                    lastName = user.surname,
                    extension_jdrfConsId = user.extension_jdrfConsId
                };
            }
            else
            {
                return RedirectToAction("Find");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(AccountModel model)
        {
            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            await b2CGraphClient.UpdateUser(model.signInName, model.displayName, model.firstName, model.lastName, model.extension_jdrfConsId);
            ViewBag.Message = "User updated successfully!";
            return View();
        }

        [HttpGet]
        public ActionResult Find()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Find")]
        public async Task<ActionResult> FindPost()
        {
            string emailAddress = Request.Form["emailAddress"];
            B2CGraphClient b2CGraphClient = new B2CGraphClient(_tenant, _clientId, _clientSecret);
            string json = await b2CGraphClient.SearcUserBySignInNames(emailAddress);
            GraphAccounts graphAccounts = GraphAccounts.Parse(json);

            if (graphAccounts != null && graphAccounts.value != null)
            {
                return RedirectToAction("Edit", new { emailAddress = emailAddress });
            }
            else
            {
                ViewBag.Message = "User not found!";
            }

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
    }
}