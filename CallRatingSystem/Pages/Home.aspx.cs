using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Web;
using Microsoft.Owin.Security;
using System.Configuration;

namespace CallRatingSystem.Pages
{
    public partial class Home : System.Web.UI.Page
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
                Response.Redirect("~/Pages/CallList.aspx", false);
            }
        }
    }
}