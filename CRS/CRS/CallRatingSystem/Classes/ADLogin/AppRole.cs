using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.ADLogin
{
    public class AppRole
    {
        public Guid appRoleID { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string origin { get; set; }

        private void SetDefault()
        {
            appRoleID = new Guid();
            description = "";
            displayName = "";
            origin = "";
        }

        private AppRole(DataRow dr)
        {
            SetDefault();
            appRoleID = Guid.Parse(dr["AppRoleID"].ToString());
            description = dr["Description"].ToString();
            displayName = dr["DisplayName"].ToString();
            origin = dr["Origin"].ToString();
        }

        //public static List<AppRole> GetUserRolesForApplication(Guid UserID, Guid ClientID)
        //{
        //    DataTable dt = new DataTable();
        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@UserID", UserID.ToString()));
        //    parms.Add(new SqlParameter("@AppID", ClientID.ToString()));

        //    dt = SqlConnections.GetData("App_GetUserRolesForApplication", parms, CommandType.StoredProcedure);

        //    if (dt.Rows.Count > 0)
        //        return (from x in dt.AsEnumerable() select new AppRole(x)).ToList();
        //    return new List<AppRole>();
        //}

        //public static List<AppRole> GetApplicationRoles(Guid ClientID)
        //{
        //    DataTable dt = new DataTable();
        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@AppID", ClientID.ToString()));

        //    dt = SqlConnections.GetData("App_GetApplicationRoles", parms, CommandType.StoredProcedure);

        //    if (dt.Rows.Count > 0)
        //        return (from x in dt.AsEnumerable() select new AppRole(x)).ToList();
        //    return new List<AppRole>();
        //}

        //public static bool IsUserInRole(Guid UserID, Guid AppRoleID)
        //{
        //    DataTable dt = new DataTable();
        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@UserID", UserID));
        //    parms.Add(new SqlParameter("@RoleID", AppRoleID));

        //    dt = SqlConnections.GetData("App_IsUserInRole", parms, CommandType.StoredProcedure);

        //    return bool.Parse(dt.Rows[0][0].ToString());
        //}
    }
}