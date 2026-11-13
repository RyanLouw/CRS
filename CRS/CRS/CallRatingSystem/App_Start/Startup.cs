using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Owin.Host.SystemWeb;
using CallRatingSystem.Classes;

namespace CallRatingSystem
{
    public class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:PostLoginRedirectUri"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        string authority = aadInstance + tenantId;

        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager()
                //CookieManager = new Classes.SameSiteCookieManager(new SystemWebCookieManager())
            });
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    // Sets the client ID, authority, and redirect URI as obtained from Web.config
                    ClientId = clientId,
                    Authority = authority,
                    RedirectUri = redirectUri,

                    //RequireHttpsMetadata = false,//Ruhan fix for localhost
                    // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it's using the home page
                    PostLogoutRedirectUri = postLogoutRedirectUri,
                    Scope = OpenIdConnectScope.OpenIdProfile,
                    // ResponseType is set to request the code id_token, which contains basic information about the signed-in user
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                    // To only allow users from a single organization, set ValidateIssuer to true and the 'tenant' setting in Web.config to the tenant name
                    // To allow users from only a list of specific organizations, set ValidateIssuer to true and use the ValidIssuers parameter
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false, // Simplification (see note below)
                        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = OnAuthenticationFailed
                    },
                    CookieManager = new Classes.SameSiteCookieManager(new SystemWebCookieManager())
                });
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            context.HandleResponse();
            context.Response.Redirect("ErrorPage.aspx?errormessage=" + context.Exception.Message);
            return Task.FromResult(0);
        }
    }
}