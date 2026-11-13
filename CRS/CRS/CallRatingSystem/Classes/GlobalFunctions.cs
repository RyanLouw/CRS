using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using CallRatingSystem.Classes.Task;

namespace CallRatingSystem.Classes
{
    public class GlobalFunctions
    {
        //public static string GetPatientPrimaryProduct()
        //{
        //    string result = "";

        //    DataTable dt = new DataTable();

        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));

        //    dt = SqlConnections.GetData("CSP_GetPatientPrimaryProduct", parms, CommandType.StoredProcedure);

        //    if (dt.Rows.Count > 0)
        //    {

        //        foreach (DataRow row in dt.Rows)
        //        {
        //            if (row.Field<string>("Product") != string.Empty)
        //            {
        //                result = row.Field<string>("Product");
        //            }
        //        }

        //    }

        //    return result;
        //}

        //public static string GetPatientCellNumber()
        //{
        //    string result = "";

        //    DataTable dt = new DataTable();

        //    if (GlobalProperties.PatientID > 0)
        //    {
        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));

        //        dt = SqlConnections.GetData("CSP_GetPatientCellNumber", parms, CommandType.StoredProcedure);

        //    }

        //    foreach (DataRow row in dt.Rows)
        //    {
        //        if (row.Field<string>("CellNumber") != null)
        //        {
        //            result = row.Field<string>("CellNumber").ToString().Trim();
        //        }
        //    }

        //    return result;
        //}

        //public static void SendPatientSMSAddInteraction(string SmsMessages, TaskEnum.TaskMessage TaskMessage)
        //{
        //    string Message = SmsMessages;
        //    string Number = GetPatientCellNumber();

        //    if (ConfigurationManager.AppSettings["Debug"].ToString() == "1") //debug
        //    {
        //        Number = ConfigurationManager.AppSettings["DebugNumber"].ToString();
        //        SendSMSFunctions.SendSMS(Message, Number);
        //        SendSMSFunctions.AddInteraction(Message, (long)TaskMessage);
        //    }
        //    else if (ConfigurationManager.AppSettings["Debug"].ToString() == "2")//Test - No Media
        //    {

        //    }
        //    else if (ConfigurationManager.AppSettings["Debug"].ToString() == "0")//Live
        //    {
        //        if (Number.Trim() != string.Empty)
        //        {
        //            SendSMSFunctions.SendSMS(Message, Number);
        //            SendSMSFunctions.AddInteraction(Message, (long)TaskMessage);
        //        }


        //    }
        //}


        //public static void PopulateFunderOptions(DropDownList ddlMedAidOption, DropDownList ddlFunder)
        //{
        //    ddlMedAidOption.Items.Clear();

        //    long FunderID = Convert.ToInt64(ddlFunder.SelectedValue.Trim());

        //    if (FunderID > 0)
        //    {
        //        DataTable dt = new DataTable();

        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@FunderID", FunderID));

        //        dt = SqlConnections.GetData("CSP_GetMedAidOptions", parms, CommandType.StoredProcedure);

        //        DAO.LoadDropdown(ddlMedAidOption, dt);
        //    }
        //    else
        //    {
        //        DataTable dt = new DataTable();

        //        DAO.LoadDropdown(ddlMedAidOption, dt);
        //    }
        //}

    }



}