using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Net;
using Emailer;
using System.Net.Configuration;
using System.Configuration;
using SMSAPI;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;
using CallRatingSystem.Classes;
using System.Text.RegularExpressions;

namespace CallRatingSystem.Classes
{
    public class SendEmailFunctions
    {
        public static MailDetails GetMailSettings()
        {
            SmtpSection mailSettings = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
            return new MailDetails() { Password = mailSettings.Network.Password, Username = mailSettings.Network.UserName, SMTPServer = mailSettings.Network.Host };
        }

        public static void SendEmail(List<string> To, List<string> CC, string Subject, List<string> attachments, string htmlMessage, long ID, string IDFor)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            To = To.Distinct().ToList();
            CC = CC.Distinct().ToList();
            attachments = attachments.Distinct().ToList();

            string SaveMessage = "-*-" + Subject + "-*-" + htmlMessage;

            //for (int i = 0; i < To.Count; i++)
            //{
            //    if (CheckEmailAlreadySent(ID, SaveMessage, To[i]))
            //    {
            //        To[i] = string.Empty;
            //    }
            //}


            try
            {
                MailDetails Sender = GetMailSettings();

                SmtpClient smtpClient = new SmtpClient();
                NetworkCredential basicCredential = new NetworkCredential(Sender.Username, Sender.Password);
                MailMessage message = new MailMessage();
                MailAddress fromAddress = new MailAddress(Sender.Username);

                // setup up the host, increase the timeout to 5 minutes
                smtpClient.Host = Sender.SMTPServer;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = basicCredential;
                smtpClient.EnableSsl = true;
                smtpClient.Timeout = (60 * 5 * 1000);

                message.Body = htmlMessage;
                message.IsBodyHtml = true;

                foreach (string item in attachments)
                {
                    Attachment file = new Attachment(item);
                    message.Attachments.Add(file);
                }
                message.From = fromAddress;
                message.Subject = Subject;

                if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "0") //Live
                {
                    for (int i = 0; i < To.Count; i++)
                    {
                        if (To[i] != string.Empty && To[i].Trim() != ".")
                        {
                            message.To.Add(To[i]);
                        }

                    }
                    for (int i = 0; i < CC.Count; i++)
                    {
                        if (CC[i] != string.Empty && CC[i].Trim() != ".")
                        {
                            message.CC.Add(CC[i]);
                        }

                    }
                }
                else if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "1")//Debug Email
                {
                    string DebugEmail = ConfigurationManager.AppSettings["DebugEmailAddress"].ToString();
                    message.To.Add(DebugEmail);
                }
                else if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "2")//Test no media
                {

                }

                smtpClient.Send(message);

                for (int i = 0; i < To.Count; i++)
                {
                    if (To[i] != string.Empty && To[i].Trim() != ".")
                    {
                        //SendEmailFunctions.AddEmailHistory(ID, IDFor, To[i], SaveMessage, "");
                    }

                }

            }
            catch (Exception ex)
            {

                for (int i = 0; i < To.Count; i++)
                {
                    if (To[i] != string.Empty && To[i].Trim() != ".")
                    {
                        //SendEmailFunctions.AddEmailHistory(ID, IDFor, To[i], SaveMessage, ex.Message + "; " + ex.StackTrace + "; " + ex.InnerException);
                    }

                }

            }


        }

        //private static bool CheckEmailAlreadySent(long ID, string SaveMessage, string ToAddress)
        //{
        //    bool result = false;

        //    if (ID > 0 && SaveMessage.Trim() != string.Empty && ToAddress.Trim() != string.Empty)
        //    {
        //        DataTable dt = new DataTable();
        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@ID", ID));
        //        parms.Add(new SqlParameter("@Message", SaveMessage));
        //        parms.Add(new SqlParameter("@AddressUsed", ToAddress));
        //        dt = SqlConnections.GetData("CSP_CheckEmailAlreadySent", parms, CommandType.StoredProcedure);

        //        if (dt.Rows.Count > 0)
        //        {
        //            result = true;
        //        }

        //    }
        //    else
        //    {
        //        result = true;
        //    }

        //    return result;
        //}

        public static void SendEmail(List<string> To, List<string> CC, string Subject, List<string> attachments, string htmlMessage)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            MailDetails Sender = GetMailSettings();

            SmtpClient smtpClient = new SmtpClient();
            NetworkCredential basicCredential = new NetworkCredential(Sender.Username, Sender.Password);
            MailMessage message = new MailMessage();
            MailAddress fromAddress = new MailAddress(Sender.Username);

            // setup up the host, increase the timeout to 5 minutes
            smtpClient.Host = Sender.SMTPServer;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = basicCredential;
            smtpClient.EnableSsl = true;
            smtpClient.Timeout = (60 * 5 * 1000);

            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            foreach (string item in attachments)
            {
                Attachment file = new Attachment(item);
                message.Attachments.Add(file);
            }
            message.From = fromAddress;
            message.Subject = Subject;

            if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "0") //Live
            {
                for (int i = 0; i < To.Count; i++)
                {
                    if (To[i] != string.Empty && To[i].Trim() != ".")
                    {
                        message.To.Add(To[i]);
                    }

                }
                for (int i = 0; i < CC.Count; i++)
                {
                    if (CC[i] != string.Empty && CC[i].Trim() != ".")
                    {
                        message.CC.Add(CC[i]);
                    }

                }
            }
            else if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "1")//Debug Email
            {
                string DebugEmail = ConfigurationManager.AppSettings["DebugEmailAddress"].ToString();
                message.To.Add(DebugEmail);
            }
            else if (ConfigurationManager.AppSettings["DebugEmail"].ToString() == "2")//Test no media
            {

            }

            smtpClient.Send(message);
            smtpClient.Dispose();
        }

        //public static void AddEmailHistory(long ID, string IDFor, string To, string Message, string ExtraFailData)
        //{
        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@ID", ID));
        //    parms.Add(new SqlParameter("@IDFor", IDFor));
        //    parms.Add(new SqlParameter("@Message", Message));
        //    parms.Add(new SqlParameter("@AddressUsed", To));
        //    parms.Add(new SqlParameter("@DateRequested", DateTime.Now));
        //    parms.Add(new SqlParameter("@DateScheduled", DateTime.Now));


        //    if (ExtraFailData != string.Empty)
        //    {
        //        parms.Add(new SqlParameter("@Result", "Fail"));
        //        parms.Add(new SqlParameter("@ExtraFailData", ExtraFailData));
        //    }
        //    else
        //    {
        //        parms.Add(new SqlParameter("@Result", "Sucess"));
        //    }

        //    SqlConnections.ExecuteQuery("CSP_AddOtherMediaHistory", parms, CommandType.StoredProcedure);

        //}

        //public static void AddInteraction(string InteractionNote, long TaskMessageID, long InteractionType, long ContactPerson) //Add interactions for each email that is sent to the patient
        //{

        //    InteractionNote = StripHTML(InteractionNote);

        //    if (GlobalProperties.PatientID > 0)
        //    {

        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", GlobalProperties.PatientID));
        //        parms.Add(new SqlParameter("@CreatedDate", DateTime.Now));
        //        parms.Add(new SqlParameter("@CreatedBy", ConfigurationManager.AppSettings["AdminUserID"].ToString()));
        //        parms.Add(new SqlParameter("@InteractionTypeID", Interaction.InteractionEnum.InteractionType.Email));
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

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

    }
}