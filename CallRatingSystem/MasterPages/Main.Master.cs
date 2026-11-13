using CallRatingSystem.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Specialized;
using System.Web.Security;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Security.Claims;
using System.Security.Principal;

namespace CallRatingSystem.MasterPages
{
    public partial class Main : System.Web.UI.MasterPage
    {
        private static string redirectUri = ConfigurationManager.AppSettings["ida:PostLoginRedirectUri"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string AdminRole = ConfigurationManager.AppSettings["AdminRole"];
        protected void Page_Load(object sender, EventArgs e)
        {

            if (!Request.IsAuthenticated)
            {

                //HttpContext.Current.GetOwinContext().Authentication.Challenge(
                //    new AuthenticationProperties { RedirectUri = redirectUri },
                //    OpenIdConnectAuthenticationDefaults.AuthenticationType);

            }
            else
            {
                var userID = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                //var surname = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value;
                //var name = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value;
                //var email = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").Value;
                var Role = ClaimsPrincipal.Current.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;

                GlobalProperties.UserID = new Guid(userID.ToString());
                //GlobalProperties.CaseManagerEmail = email.ToString().Trim();
                GlobalProperties.Role = Role.ToString().Trim();

                //Label1.Text = name + " " + surname + "<br/>" + email + "<br/>" + userID + "<br/>";

                //lkAdmin.Visible = true; //testing
                var InAdminRole = HttpContext.Current.User.IsInRole(AdminRole);

                //if (InAdminRole)
                //{

                //}
                //else
                //{

                //}

                //GlobalProperties.Role = "IT";

                if (GlobalProperties.Role.Contains("Admin") == true)
                {
                    lkAdmin.Visible = true;
                }
                else if (GlobalProperties.Role.Contains("IT") == true)
                {
                    lkAdmin.Visible = true;
                }
                else
                {
                    lkAdmin.Visible = false;
                }


            }


        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Request.GetOwinContext().Authentication.SignOut();
            HttpContext.Current.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);

            string url = string.Format("https://login.microsoftonline.com/{0}/oauth2/logout?post_logout_redirect_uri={1}", tenantId, postLogoutRedirectUri);
            Response.Redirect(url);
        }

        protected void btnAdmin_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Pages/Admin.aspx", false);
        }
    }
}