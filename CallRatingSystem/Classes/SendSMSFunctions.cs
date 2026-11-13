using SMSAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CallRatingSystem.Classes;

namespace CallRatingSystem.Classes
{
    public class SendSMSFunctions
    {
        //public static void SendSMS(string Message, string Number)
        //{

        //    DateTime SendingDate = DateTime.Now;

        //    if (SMS.Initialize(ConfigurationManager.AppSettings["SMS_User"].ToString(), ConfigurationManager.AppSettings["SMS_Password"].ToString()))
        //    {
        //        string Res;

        //        Res = SMS.SendSMS(Message, Number, SendingDate);


        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));
        //        parms.Add(new SqlParameter("@Message", Message));
        //        parms.Add(new SqlParameter("@AddressUsed", Number));
        //        parms.Add(new SqlParameter("@Result", Res));
        //        parms.Add(new SqlParameter("@DateRequested", DateTime.Now));
        //        if (Res.Contains("Message sent with ID"))
        //        {
        //            string UID = Res.Split(' ')[Res.Split(' ').Length - 1];
        //            SMSSendResult res = GetHistory(UID);
        //            if (res.StatusCode != null && res.StatusCode != "NOTFOUND")
        //            {
        //                parms.Add(new SqlParameter("@ServerResult", res.StatusCode));
        //                parms.Add(new SqlParameter("@ServerTime", DateTime.Parse(res.StatusDate + " " + res.StatusTime)));


        //            }
        //            parms.Add(new SqlParameter("@UID", UID));
        //        }


        //        SqlConnections.ExecuteQuery("CSP_AddMediaHistory", parms, CommandType.StoredProcedure);

        //    }
        //}

        public static SMSSendResult GetHistory(string UID)
        {
            int total = 0;
            List<SMSSendResult> Res = new List<SMSSendResult>();
            while (total < 5)
            {
                Thread.Sleep(1000);
                Res = SMS.GetSentDetails(new List<string>() { UID });
                total++;
                if (Res[0].Status != "NOTFOUND")
                {
                    break;
                }
            }

            return Res[0];
        }

        //public static void AddInteraction(string InteractionNote, long TaskMessageID) //Add interactions for each sms that is sent to the patient
        //{

        //    if (GlobalProperties.PatientID > 0)
        //    {
        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));
        //        parms.Add(new SqlParameter("@CreatedDate", DateTime.Now));
        //        parms.Add(new SqlParameter("@CreatedBy", ConfigurationManager.AppSettings["AdminUserID"].ToString()));
        //        parms.Add(new SqlParameter("@InteractionTypeID", Interaction.InteractionEnum.InteractionType.SMSWhatsapp));
        //        parms.Add(new SqlParameter("@ContactPersonID", Task.TaskEnum.ContactPerson.Patient));
        //        parms.Add(new SqlParameter("@InteractionStatusID", Interaction.InteractionEnum.InteractionStatus.Complete));
        //        parms.Add(new SqlParameter("@TaskMessageID", TaskMessageID));

        //        if (InteractionNote != string.Empty)
        //        {
        //            parms.Add(new SqlParameter("@InteractionNote", InteractionNote));
        //        }

        //        SqlConnections.ExecuteQuery("CSP_AddInteraction", parms, CommandType.StoredProcedure);

        //    }


        //}

        //public static void AddInteraction(string InteractionNote, long TaskMessageID, Guid UserID) //Add interactions for each sms that is sent to the patient
        //{

        //    if (GlobalProperties.PatientID > 0)
        //    {

        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));
        //        parms.Add(new SqlParameter("@CreatedDate", DateTime.Now));
        //        parms.Add(new SqlParameter("@CreatedBy", UserID));
        //        parms.Add(new SqlParameter("@InteractionTypeID", Interaction.InteractionEnum.InteractionType.SMSWhatsapp));
        //        parms.Add(new SqlParameter("@ContactPersonID", Task.TaskEnum.ContactPerson.Patient));
        //        parms.Add(new SqlParameter("@InteractionStatusID", Interaction.InteractionEnum.InteractionStatus.Complete));
        //        parms.Add(new SqlParameter("@TaskMessageID", TaskMessageID));

        //        if (InteractionNote != string.Empty)
        //        {
        //            parms.Add(new SqlParameter("@InteractionNote", InteractionNote));
        //        }

        //        SqlConnections.ExecuteQuery("CSP_AddInteraction", parms, CommandType.StoredProcedure);

        //    }


        //}



    }
}