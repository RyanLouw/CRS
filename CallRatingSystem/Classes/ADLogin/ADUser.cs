using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace CallRatingSystem.Classes.ADLogin
{
    public class ADUser
    {
        public Guid userID { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public ClaimsPrincipal principal { get; set; }
        public List<AppRole> roles { get; set; }
        public string displayName { get { return name + (!string.IsNullOrWhiteSpace(surname) ? " " + surname : ""); } }

        private void SetDefault()
        {
            userID = new Guid();
            name = "";
            surname = "";
            email = "";
            roles = new List<AppRole>();
        }

        public ADUser()
        {
            SetDefault();
        }

        public static ADUser GetCurrentUser(HttpRequest Request)
        {
            if (IsSignedIn(Request))
            {
                ADUser usr = new ADUser(ClaimsPrincipal.Current);
                return usr;
            }
            return null;
        }

        private ADUser(ClaimsPrincipal claimsPrincipal)
        {
            principal = claimsPrincipal;
            name = principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname").Value;
            surname = principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname").Value;
            email = principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn").Value;
            userID = Guid.Parse(principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value);
            //roles = AppRole.GetUserRolesForApplication(userID, Guid.Parse(ADLoginConfigValidatorService.clientId));
        }

        public static bool IsSignedIn(HttpRequest Request)
        {
            return Request.IsAuthenticated;
        }
    }
}