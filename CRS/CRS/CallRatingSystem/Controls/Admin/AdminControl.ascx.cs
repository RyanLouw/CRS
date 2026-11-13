using CallRatingSystem.Classes;
using CallRatingSystem.Classes.Task;
using HW.ThreeCXIntegration.Calls;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace CallRatingSystem.Controls.Admin
{
    public partial class AdminControl : System.Web.UI.UserControl
    {
        private static readonly My3CXHelper _helper3CX = new My3CXHelper();

        private int CurrentPage
        {
            get => (int?)ViewState["CurrentPage"] ?? 1;
            set => ViewState["CurrentPage"] = value < 1 ? 1 : value;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                DAO.LoadDropdown(ddlHelpDeskStatusFilter, "HelpDeskStatus");
                DAO.LoadDropdown(ddlHelpDeskStatus, "HelpDeskStatus");
                DAO.LoadDropdown(ddlHelpDeskReason, "HelpDeskReason");

                CurrentPage = 1;                 // start clean
                hdnPageNum.Value = "1";          // keep in sync if you use it on the client
                LoadData();                      // first bind
            }
        }

        private void LoadData()
        {
            long helpDeskStatusFilter = 0;
            if (!string.IsNullOrWhiteSpace(ddlHelpDeskStatusFilter.SelectedValue))
                long.TryParse(ddlHelpDeskStatusFilter.SelectedValue.Trim(), out helpDeskStatusFilter);

            int pageIndex = CurrentPage - 1;

            var parms = new List<SqlParameter>();
            if (helpDeskStatusFilter > 0)
                parms.Add(new SqlParameter("@HelpDeskStatusFilter", helpDeskStatusFilter));
            parms.Add(new SqlParameter("@PageNum", pageIndex));

            DataTable dt = SqlConnections.GetData("CSP_GetCallRatingPage", parms, CommandType.StoredProcedure);
            rptTickets.DataSource = dt;
            rptTickets.DataBind();

            var parms2 = new List<SqlParameter>();
            if (helpDeskStatusFilter > 0)
                parms2.Add(new SqlParameter("@HelpDeskStatusFilter", helpDeskStatusFilter));
            DataTable dt2 = SqlConnections.GetData("CSP_GetCallRating", parms2, CommandType.StoredProcedure);

            int totalRows = dt2.Rows.Count;
            const int pageSize = 5; // matches your server paging
            int pageCount = Math.Max(1, (totalRows + pageSize - 1) / pageSize); // ceiling, at least 1

      
            if (CurrentPage > pageCount)
            {
                CurrentPage = pageCount;
                pageIndex = CurrentPage - 1;


                parms.RemoveAll(p => p.ParameterName == "@PageNum");
                parms.Add(new SqlParameter("@PageNum", pageIndex));
                dt = SqlConnections.GetData("CSP_GetCallRatingPage", parms, CommandType.StoredProcedure);
                rptTickets.DataSource = dt;
                rptTickets.DataBind();
            }


            currentPage.Text = CurrentPage.ToString();
            outOffCount.Text = pageCount.ToString();

            btnPrevPage.Visible = CurrentPage > 1;
            btnNextPage.Visible = CurrentPage < pageCount;


            hdnPageNum.Value = CurrentPage.ToString();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {

            CurrentPage = 1;
            hdnPageNum.Value = "1";
            LoadData();
        }

        protected void ddlHelpDeskStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {

            CurrentPage = 1;
            hdnPageNum.Value = "1";
            LoadData();
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            hdnPageNum.Value = CurrentPage.ToString();
            LoadData();
        }

        protected void btnPrevPage_Click(object sender, EventArgs e)
        {
            CurrentPage--;
            hdnPageNum.Value = CurrentPage.ToString();
            LoadData();
        }



        protected void btnSaveSelectCallIT_Click(object sender, EventArgs e)
        {
            hdnCallTime.Value = txtCallTime.Text.Trim();
            hdnNumberDialed.Value = txtNumberDialed.Text.Trim();
            hdncall_id.Value = txtcall_id.Text.Trim();
            hdnDisplayName.Value = txtDisplayName.Text.Trim();
            hdnExt.Value = txtExt.Text.Trim();
            hdnProblems.Value = txtProblems.Text.Trim();
            hdnOtherProblem.Value = txtOtherProblem.Text.Trim();
            hdnCallDateTime.Value = txtCallDateTime.Text.Trim();
            hdnClientIP.Value = txtClientIP.Text.Trim();
            hdnHostIP.Value = txtHostIP.Text.Trim();
            hdnHelpDeskStatusID.Value = txtHelpDeskStatusID.Text.Trim();
            hdnCallDate.Value = txtCallDate.Text.Trim();
            hdnMail.Value = txtMail.Text.Trim();

            long CallRatingID = Convert.ToInt64(txtCallRatingID.Text.Trim());
            long HelpDeskStatus = Convert.ToInt64(ddlHelpDeskStatus.SelectedValue.Trim());
            long HelpDeskReason = Convert.ToInt64(ddlHelpDeskReason.SelectedValue.Trim());

            if (CallRatingID > 0 && HelpDeskStatus > 0)
            {
                List<SqlParameter> parms = new List<SqlParameter>
                {
                    new SqlParameter("@CallRatingID", CallRatingID),
                    new SqlParameter("@ITUpdateByUserID", GlobalProperties.UserID),
                    new SqlParameter("@ITUpdateDate", DateTime.Now),
                    new SqlParameter("@HelpDeskStatusID", HelpDeskStatus),
                    new SqlParameter("@HelpDeskReasonID", HelpDeskReason),
                    new SqlParameter("@ITNote", txtITNote.Text.Trim())
                };

                DataTable dt = SqlConnections.GetData("CSP_ITUpdate", parms, CommandType.StoredProcedure);

                SendHelpDeskCallRatingEmail();

                if (HelpDeskStatus == 2) // Closed
                {
                    SendHelpDeskCallRatingStatusEmailToUser();
                }
            }

            ClearFields();
            LoadData();
        }

        private void ClearFields()
        {
            hdnCallRatingID.Value = "";
            hdnCallTime.Value = "";
            hdnNumberDialed.Value = "";
            hdncall_id.Value = "";
            hdnDisplayName.Value = "";
            hdnExt.Value = "";
            hdnProblems.Value = "";
            hdnOtherProblem.Value = "";
            hdnCallDateTime.Value = "";
            hdnClientIP.Value = "";
            hdnHostIP.Value = "";
            hdnHelpDeskStatusID.Value = "";
            hdnHelpDeskReasonID.Value = "";
            hdnCallDate.Value = "";
            hdnMail.Value = "";
            txtITNote.Text = "";

            ddlHelpDeskStatus.SelectedValue = "0";
            ddlHelpDeskReason.SelectedValue = "0";

            txtCallRatingID.Text = "";
            txtDisplayName.Text = "";
            txtCallDate.Text = "";
            txtHelpDeskStatus.Text = "";
            txtCallDateTime.Text = "";
            txtcall_id.Text = "";
            txtProblems.Text = "";
            txtOtherProblem.Text = "";
            txtClientIP.Text = "";
            txtHostIP.Text = "";
            txtExt.Text = "";
            txtNumberDialed.Text = "";
            txtHelpDeskReason.Text = "";
            txtDisplayITNote.Text = "";
            txtHelpDeskStatusID.Text = "";
            txtPharmacyID.Text = "";
            txtPharmacy.Text = "";
            txtMail.Text = "";
        }

        protected void btnSelectCallIT_Click(object sender, EventArgs e)
        {
            hdnCallRatingID.Value = "";
            hdnCallTime.Value = "";
            hdnNumberDialed.Value = "";
            hdncall_id.Value = "";
            hdnDisplayName.Value = "";
            hdnExt.Value = "";
            hdnProblems.Value = "";
            hdnOtherProblem.Value = "";
            hdnCallDateTime.Value = "";
            hdnClientIP.Value = "";
            hdnHostIP.Value = "";
            hdnHelpDeskStatusID.Value = "";
            hdnHelpDeskReasonID.Value = "";
            hdnCallDate.Value = "";
            hdnMail.Value = "";

            txtITNote.Text = "";
            ddlHelpDeskStatus.SelectedValue = "0";
            ddlHelpDeskReason.SelectedValue = "0";

            RepeaterItem row = (RepeaterItem)((Button)sender).NamingContainer;

            HiddenField CallRatingID = (HiddenField)row.FindControl("lblCallRatingID");
            hdnCallRatingID.Value = CallRatingID.Value.Trim();

            Label lblCallTime = (Label)row.FindControl("lblCallTime");
            Label lblNumberDialed = (Label)row.FindControl("lblNumberDialed");
            HiddenField lblcall_id = (HiddenField)row.FindControl("lblcall_id");
            Label lblDisplayName = (Label)row.FindControl("lblDisplayName");
            Label lblExt = (Label)row.FindControl("lblExt");
            Label lblProblems = (Label)row.FindControl("lblProblems");
            Label lblOtherProblem = (Label)row.FindControl("lblOtherProblem");
            Label lblCallDateTime = (Label)row.FindControl("lblCallDateTime");
            HiddenField lblClientIP = (HiddenField)row.FindControl("lblClientIP");
            HiddenField lblHostIP = (HiddenField)row.FindControl("lblHostIP");
            HiddenField lblHelpDeskStatusID = (HiddenField)row.FindControl("lblHelpDeskStatusID");
            HiddenField lblHelpDeskReasonID = (HiddenField)row.FindControl("lblHelpDeskReasonID");
            Label lblCallDate = (Label)row.FindControl("lblCallDate");
            Label lblMail = (Label)row.FindControl("lblMail");

            hdnCallTime.Value = lblCallTime.Text.Trim();
            hdnNumberDialed.Value = lblNumberDialed.Text.Trim();
            hdncall_id.Value = lblcall_id.Value.Trim();
            hdnDisplayName.Value = lblDisplayName.Text.Trim();
            hdnExt.Value = lblExt.Text.Trim();
            hdnProblems.Value = lblProblems.Text.Trim();
            hdnOtherProblem.Value = lblOtherProblem.Text.Trim();
            hdnCallDateTime.Value = lblCallDateTime.Text.Trim();
            hdnClientIP.Value = lblClientIP.Value.Trim();
            hdnHostIP.Value = lblHostIP.Value.Trim();
            hdnHelpDeskStatusID.Value = lblHelpDeskStatusID.Value.Trim();
            hdnCallDate.Value = lblCallDate.Text.Trim();
            hdnMail.Value = lblMail.Text.Trim();

            ScriptManager.RegisterStartupScript(this, GetType(), "SelectCallITModal", "$('#SelectCallITModal').modal('show');", true);
        }

        private void SendHelpDeskCallRatingEmail()
        {
            List<string> To = new List<string>() { "hwvoip@rootsystem.co.za" };
            List<string> Cc = new List<string>();
            string Subject = "Status Of Call Rating For Ext: " + hdnExt.Value.Trim();
            List<string> Attachments = new List<string>();
            string htmlMessage = EmailMessage.HelpDeskStatusEmailBody;

            string CurrentUserName = hdnDisplayName.Value.Trim();

            var ticked = new StringBuilder();

            foreach (var problem in hdnProblems.Value
                         .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(x => x.Trim())
                         .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                ticked.Append(problem).Append("; <br/>");
            }

            string Ticked = ticked.ToString();

            htmlMessage = htmlMessage.Replace("{HelpDeskStatus}", ddlHelpDeskStatus.SelectedItem.Text);
            htmlMessage = htmlMessage.Replace("{HelpDeskReason}", ddlHelpDeskReason.SelectedItem.Text);
            htmlMessage = htmlMessage.Replace("{ITNote}", txtITNote.Text);
            htmlMessage = htmlMessage.Replace("{IP}", hdnClientIP.Value);
            htmlMessage = htmlMessage.Replace("{HostIP}", hdnHostIP.Value);
            htmlMessage = htmlMessage.Replace("{Ext}", hdnExt.Value);
            htmlMessage = htmlMessage.Replace("{CallDate}", hdnCallDate.Value);
            htmlMessage = htmlMessage.Replace("{CallTime}", hdnCallTime.Value);
            htmlMessage = htmlMessage.Replace("{NumberDialed}", hdnNumberDialed.Value);
            htmlMessage = htmlMessage.Replace("{Pharmacy}", txtPharmacy.Text);

            if (Ticked != "{items}")
            {
               htmlMessage = htmlMessage.Replace("{Ticked}", Ticked);
            }
                
            else
            {
              htmlMessage = htmlMessage.Replace("{Ticked}", "");
            }
                

            if (hdnOtherProblem.Value.Trim() != String.Empty)
            {
              htmlMessage = htmlMessage.Replace("{Other}", hdnOtherProblem.Value);
            }
                
            else
            {
                htmlMessage = htmlMessage.Replace(EmailMessage.ReplaceOtherSection, "");
            }

            htmlMessage = htmlMessage.Replace("{DisplayName}", CurrentUserName);

            try
            {
                SendEmailFunctions.SendEmail(To, Cc, Subject, Attachments, htmlMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SendHelpDeskCallRatingEmail error");
            }
        }

        private void SendHelpDeskCallRatingStatusEmailToUser()
        {
            string UserEmail = hdnMail.Value.Trim();

            if (UserEmail.Trim() != String.Empty)
            {
                List<string> To = new List<string>() { UserEmail };
                List<string> Cc = new List<string>();
                string Subject = "Call Feedback : " + hdnNumberDialed.Value.Trim() + " at " + hdnCallTime.Value.Trim() + " " + hdnCallDate.Value.Trim();
                List<string> Attachments = new List<string>();
                string htmlMessage = EmailMessage.HelpDeskCallRatingStatusEmailToUser;

                var ticked = new StringBuilder();

                foreach (var problem in hdnProblems.Value
                             .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => x.Trim())
                             .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    ticked.Append(problem).Append("; <br/>");
                }

                string Ticked = ticked.ToString();

                htmlMessage = htmlMessage.Replace("{HelpDeskReason}", ddlHelpDeskReason.SelectedItem.Text);
                htmlMessage = htmlMessage.Replace("{ITNote}", txtITNote.Text);
                htmlMessage = htmlMessage.Replace("{CallDate}", hdnCallDate.Value);
                htmlMessage = htmlMessage.Replace("{CallTime}", hdnCallTime.Value);
                htmlMessage = htmlMessage.Replace("{NumberDialed}", hdnNumberDialed.Value);
                htmlMessage = htmlMessage.Replace("{Pharmacy}", txtPharmacy.Text);

                if (Ticked != "{items}")
                { 
                  htmlMessage = htmlMessage.Replace("{Ticked}", Ticked); 
                }

                else
                {
                  htmlMessage = htmlMessage.Replace("{Ticked}", "");
                }
                    

                if (hdnOtherProblem.Value.Trim() != String.Empty)
                {
                    htmlMessage = htmlMessage.Replace("{Other}", hdnOtherProblem.Value);
                }
                else
                {
                  htmlMessage = htmlMessage.Replace(EmailMessage.ReplaceOtherSection, "");
                }
                    

                try
                {
                    SendEmailFunctions.SendEmail(To, Cc, Subject, Attachments, htmlMessage);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SendHelpDeskCallRatingStatusEmailToUser error");
                }
            }
        }


        protected void btnDownloadCall_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            RepeaterItem item = button?.NamingContainer as RepeaterItem;

            if (item == null)
            {
                Log.Warning("Download clicked but the repeater row was not found.");
                ShowRecordingError("Could not find the selected call row.");
                return;
            }

            HiddenField hfRecording =
                item.FindControl("lblcall_id") as HiddenField;

            string recordingUrl = hfRecording?.Value?.Trim();

            Log.Information(
                "Admin download clicked. FieldFound: {FieldFound}, RecordingURL: {RecordingURL}",
                hfRecording != null,
                recordingUrl);

            if (string.IsNullOrWhiteSpace(recordingUrl))
            {
                ShowRecordingError("No recording is available for this call.");
                return;
            }

            Page.RegisterAsyncTask(new PageAsyncTask(async () =>
            {
                try
                {
                    string url = await SqlConnections.GetCallURLAsync(recordingUrl);

                    Log.Information(
                        "Admin download URL lookup completed. HasDownloadURL: {HasDownloadURL}",
                        !string.IsNullOrWhiteSpace(url));

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        ShowRecordingError(
                            "The recording download URL could not be retrieved.");
                        return;
                    }

                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "downloadRecording",
                        $"window.location.href='{HttpUtility.JavaScriptStringEncode(url)}';",
                        true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Admin recording download failed.");

                    ShowRecordingError("The recording could not be downloaded.");
                }
            }));
        }

        private void ShowRecordingError(string message)
        {
            ScriptManager.RegisterStartupScript(
                this,
                GetType(),
                "recordingError",
                $"toggleErrorHandler('Oh Snap!', '{HttpUtility.JavaScriptStringEncode(message)}');",
                true);
        }
    }
}
