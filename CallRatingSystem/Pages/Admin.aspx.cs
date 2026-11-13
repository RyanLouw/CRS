using CallRatingSystem.Classes;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Web;
using System.Configuration;
using Microsoft.Owin.Security;

namespace CallRatingSystem.Pages
{
    public partial class Admin : System.Web.UI.Page
    {
        private static string redirectUri = ConfigurationManager.AppSettings["ida:PostLoginRedirectUri"];
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!Request.IsAuthenticated)
            {

                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUri },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);

            }
            else
            {
                if (GlobalProperties.Role.Contains("IT") == false && GlobalProperties.Role.Contains("Admin") == false)
                {
                    Response.Redirect("CallList.aspx");
                }
            }

            
        }
    }
}