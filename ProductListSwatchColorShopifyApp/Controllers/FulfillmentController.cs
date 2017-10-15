using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ProductListSwatchColorShopifyApp.Common;
using RestSharp;
using Newtonsoft.Json;
using ProductListSwatchColorShopifyApp.Models;
using System.IO;
using System.Net;

namespace ProductListSwatchColorShopifyApp.Controllers
{
    public class FulfillmentController : Controller
    {
        private string apiKey = Constants.ShopifyApiKey;
        private string secretKey = Constants.ShopifySecretKey;
        private string appUrl = Constants.AppUrl;

        // GET: fulfillment
        public ActionResult install(string shop, string signature, string timestamp)
        {
            string r = string.Format("https://{0}/admin/oauth/authorize?client_id={1}&scope=read_fulfillments,write_fulfillments&redirect_uri=https://{2}/fulfillment/auth", shop, apiKey, appUrl);
            return Redirect(r);
        }

        public ActionResult auth(string shop, string code)
        {
            string u = string.Format("https://{0}/admin/oauth/access_token", shop);

            var client = new RestClient(u);

            var request = new RestRequest(Method.POST);

            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Content-Type", "application/json");

            request.AddParameter("application/x-www-form-urlencoded", "client_id=" + apiKey + "&client_secret=" + secretKey + "&code=" + code, ParameterType.RequestBody);

            var response = client.Execute(request);

            var r = JsonConvert.DeserializeObject<dynamic>(response.Content);
            var accessToken = r.access_token;

            //Part 5
            //create a un-install web hook
            //you want to know when customers delete your app from their shop

            string unInstallUrlCallback = "https://549653d4.ngrok.io/fulfillment/uninstall";

            string shopAdmin = string.Format("https://{0}/admin/", shop);

            var webHook = new WebHookBucket();
            webHook.Whook = new WebHook { Format = "json", Topic = "app/uninstalled", Address = unInstallUrlCallback };

            CreateUninstallHook(shopAdmin, "webhooks.json", Method.POST, (string)accessToken, webHook);
            return View();
        }

        [HttpPost]
        public ActionResult uninstall()
        {
            var req = Request.InputStream;
            var json = new StreamReader(req).ReadToEnd();

            return new HttpStatusCodeResult(200);

        }

        public ActionResult chargeresult(string shop, string charge_id)
        {

            string token = GetShopToken(shop);
            string shopAdmin = string.Format("https://{0}/admin/", shop);

            //FIND OUT IF THE CUSTOMER ACCEPT THE CHARGE OR NOT
            var values = CheckChargeStatus(token, shop, charge_id);

            //UPDATE DETAILS


            //SEE THE STATUS
            if (values.recurring_application_charge.status == "declined")
            {
                //send customer somewere
                return RedirectToAction("Contact", "Home");
            }
            else if (values.recurring_application_charge.status == "accepted")
            {
                //save charge id


                //activate charge
                ChargeResult copyValues = values;
                copyValues.recurring_application_charge.confirmation_url = null;

                var active = ActivateCharge(token, shop, charge_id, copyValues);

                if (active == HttpStatusCode.OK)
                {

                    //CUSTOMER ACCEPTED CHARGE GO TO APP
                    return RedirectToAction("About", "Home");
                }
                else
                {

                    //fail to actived

                    //send customer to error page
                    return RedirectToAction("Contact", "Home");
                }

            }
            else
            {
                return RedirectToAction("Contact", "Home");
            }

        }

        public string CreateUninstallHook(string baseUrl, string endPoint, RestSharp.Method method, string token, WebHookBucket webhook)
        {
            var client = new RestClient(baseUrl);

            var request = new RestRequest(endPoint, method);

            request.RequestFormat = DataFormat.Json;

            request.AddHeader("X-Shopify-Access-Token", token);

            string json = JsonConvert.SerializeObject(webhook, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            // execute the request
            var r = client.Execute(request);

            return r.Content;
        }

        public IRestResponse CreateCharged(string token, string shop, Recurring recurring)
        {
            var client = new RestClient("https://" + shop + "/admin/");

            var request = new RestRequest("recurring_application_charges.json", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("X-Shopify-Access-Token", token);
            string json = JsonConvert.SerializeObject(recurring, Formatting.Indented);

            request.AddParameter("application/json", json, ParameterType.RequestBody);

            // execute the request
            var result = client.Execute(request);
            return result;

        }

        private string AskCustomerCharge(string shop, string token)
        {
            //create charged
            var recurring = new Recurring();

            recurring.Charge = new ChargeDetails
            {
                Name = "Fulfillment App Pro",
                Price = "20.00",
                ReturnUrl = string.Format("https://{0}/fulfillment/chargeresult?shop={1}", appUrl, shop),
                TrialDays = "2",
                Test = "true"
            };

            var chargeResultDetails = CreateCharged(token, shop, recurring);

            if (chargeResultDetails.StatusCode == HttpStatusCode.Created)
            {
                ChargeResult chargeResult = JsonConvert.DeserializeObject<ChargeResult>(chargeResultDetails.Content);

                //save to DB


                //send to charge link for customer to accept or declined charge

                return chargeResult.recurring_application_charge.confirmation_url;

            }
            return "";

        }

        private void ProcessUnInstalled(string jsonData)
        {
            using (var con = new SwatchProductShopifyEntities())
            {

                var json = JsonConvert.DeserializeObject<dynamic>(jsonData);

                string domain = json.domain.ToString();
                string myshopify_domain = json.myshopify_domain.ToString();

                if (con.ShopToken.Any(c => c.Shop == domain) || con.ShopToken.Any(c => c.Shop == myshopify_domain))
                {

                    var updateRow = con.ShopToken.FirstOrDefault(c => c.Shop == domain);
                    updateRow.UninstallDate = DateTime.Now;
                    updateRow.Token = "";
                    con.SaveChanges();
                }

            }
        }
    }
}