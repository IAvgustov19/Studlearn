using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http;

namespace SW.Frontend.Utilities.Filters
{
    public class RecaptchaFilter : ActionFilterAttribute
    {
        public bool CapthcaRequired { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var captchaResponse = HttpContext.Current.Request.QueryString["g-recaptcha-response"] ?? "";
            if (string.IsNullOrEmpty(captchaResponse))
            {
                string value = "";
                using (var stream = new StreamReader(actionContext.Request.Content.ReadAsStreamAsync().Result))
                {
                    stream.BaseStream.Position = 0;
                    value = stream.ReadToEnd();
                }

                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                var query = HttpUtility.ParseQueryString(value);
                captchaResponse = query["g-recaptcha-response"];
            }

            if (string.IsNullOrEmpty(captchaResponse))
            {
                if (CapthcaRequired)
                {
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Recapthca is required");
                }
            }
            else
            {
                string privatekey = WebConfigurationManager.AppSettings["RecaptchaPrivateKey"];
                var isValid = Validate(captchaResponse, privatekey);
                actionContext.ActionArguments["CaptchaValid"] = isValid;
            }
        }

        public static bool Validate(string mainresponse, string privatekey)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify?secret=" + privatekey + "&response=" + mainresponse);

                WebResponse response = req.GetResponse();

                using (StreamReader readStream = new StreamReader(response.GetResponseStream()))
                {
                    string jsonResponse = readStream.ReadToEnd();

                    JsonResponseObject jobj = JsonConvert.DeserializeObject<JsonResponseObject>(jsonResponse);

                    return jobj.success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public class JsonResponseObject
        {
            public bool success { get; set; }
            [JsonProperty("error-codes")]
            public List<string> errorcodes { get; set; }
        }
    }
}
