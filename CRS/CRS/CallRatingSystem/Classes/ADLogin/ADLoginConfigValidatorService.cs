using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.ADLogin
{
    public class ADLoginConfigValidatorService
    {
        public static readonly string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public static readonly string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        public static readonly string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        public static readonly string redirectUri = ConfigurationManager.AppSettings["ida:PostLoginRedirectUri"];
        public static readonly string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        public static readonly string applicationSecret = ConfigurationManager.AppSettings["ida:ApplicationSecret"];
        public static readonly string domain = ConfigurationManager.AppSettings["ida:Domain"];
        public static readonly string[] Scope = ConfigurationManager.AppSettings["ida:Scope"].Split(';');
        public static readonly string objectID = ConfigurationManager.AppSettings["ida:ObjectID"];

        public static string ValidateConfig()
        {
            string message = null;
            Guid result;

            if (string.IsNullOrWhiteSpace(clientId))
            {
                message = "ClientId is empty. Please register your application as Native app in https://dev.powerbi.com/apps and fill client Id in web.config.";
            }
            // Application Id must be a Guid object.
            else if (!Guid.TryParse(clientId, out result))
            {
                message = "ClientId must be a Guid object. please register your application as Native app in https://dev.powerbi.com/apps and fill client Id in web.config.";
            }
            else if (string.IsNullOrWhiteSpace(aadInstance))
            {
                message = "AADInstance is empty. Please set AADInstance in the web config file";
            }
            else if (string.IsNullOrWhiteSpace(tenantId))
            {
                message = "TenantId is empty. Please set TenantId in the web config file";
            }
            else if (string.IsNullOrWhiteSpace(redirectUri))
            {
                message = "RedirectUri is empty. Please set RedirectUri in the web config file";
            }
            else if (string.IsNullOrWhiteSpace(postLogoutRedirectUri))
            {
                message = "PostLogoutRedirectUri is empty. Please set PostLogoutRedirectUri in the web config file";
            }

            return message;
        }
    }
}