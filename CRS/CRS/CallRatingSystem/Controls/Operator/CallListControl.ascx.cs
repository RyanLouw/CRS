using CallRatingSystem.Classes;
using CallRatingSystem.Classes.Task;
using HW.ThreeCXIntegration.Calls;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;



namespace CallRatingSystem.Controls.Operator
{
    public partial class CallListControl : System.Web.UI.UserControl
    {
        private static readonly My3CXHelper _helper3CX = new My3CXHelper();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (GlobalProperties.Role.Contains("IT"))
            {
                Response.Redirect("Admin.aspx");
                return;
            }

            if (!string.IsNullOrWhiteSpace(GlobalProperties.Role) && !IsPostBack)
            {
                DAO.PopulateCheckboxList(chklReason, GetProblems(), "Problems", "ProblemsID");
                DAO.LoadDropdown(ddlPharmacy, "Pharmacy");

                RegisterLoadDataTask();
            }
        }
        private DataTable GetProblems()
        {
            return SqlConnections.GetData("CSP_GetProblems", new List<SqlParameter>(), CommandType.StoredProcedure);
        }
        private void RegisterLoadDataTask()
        {
            Page.RegisterAsyncTask(new PageAsyncTask(LoadDataAsync));
        }

        private async Task LoadDataAsync()
        {
            string ext = GetExt();
            DateTime date = DateTime.Today;

            try
            {
                List<CallInfo> calls =await SqlConnections.GetDataCallStatsAsync(ext, date);

                Log.Information(
                    "Calls returned. Extension: {Extension}, Count: {CallCount}",
                    ext, calls?.Count ?? 0);

                rptTicketsCallList.DataSource = calls ?? new List<CallInfo>();
                rptTicketsCallList.DataBind();
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to load calls. Extension: {Extension}, Date: {CallDate}",
                    ext, date);

                ShowRecordingError("Calls could not be loaded. Please contact IT.");
            }
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            RegisterLoadDataTask();
        }

        private string GetExt()
        {
            string result = "";

            if (GlobalProperties.UserID != new Guid())
            {
                DataTable dt = new DataTable();

                dt = SqlConnections.GetDataADUsersSQL(SQLStrings.GetUserExtQuery.Replace("{UserID}", GlobalProperties.UserID.ToString().Trim()));

                foreach (DataRow row in dt.Rows)
                {
                    if (row.Field<string>("Extension") != null)
                    {
                        result = row.Field<string>("Extension").Trim();
                        hdnExt.Value = row.Field<string>("Extension").Trim();
                    }
                    if (row.Field<string>("DisplayName") != null)
                    {
                        hdnDisplayName.Value = row.Field<string>("DisplayName").Trim();
                    }
                    if (row.Field<string>("GivenName") != null)
                    {
                        hdnGivenName.Value = row.Field<string>("GivenName").Trim();
                    }
                    if (row.Field<string>("Surname") != null)
                    {
                        hdnSurname.Value = row.Field<string>("Surname").Trim();
                    }
                }
            }
            return result;
        }

        private async Task SendHelpDeskCallRatingEmailAsync(string CallID, string Ext, string CallTime, string NumberDialed)
        {


            string ip = "";
            ip += System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            string HostIP = "";
            HostIP = Request.UserHostAddress.ToString();

            string localIp = Request.UserHostAddress;

            List<string> To = new List<string>() { "hwvoip@rootsystem.co.za" };
            List<string> Cc = new List<string>();
            string Subject = "Call Rating For Ext: " + Ext.Trim();
            List<string> Attachments = new List<string>();
            string htmlMessage = EmailMessage.HelpDeskEmailBody;


            string Other = txtComment.Text.Trim();

            int TickCount = 0;

            string Ticked = "";
            foreach (ListItem check in chklReason.Items)
            {
                if (check.Selected == true)
                {
                    Ticked += check.Text.Trim() + "; ";
                    TickCount++;

                }
            }

            if (TickCount == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(),
                    "NoItemsTickedAlert", "toggleErrorHandler('Oh Snap!','Please select a problem experienced with the call.');", true);
                return;
            }

            htmlMessage = htmlMessage.Replace("{IP}", ip.Trim().Split(':').First());
            htmlMessage = htmlMessage.Replace("{HostIP}", HostIP.Trim());

            htmlMessage = htmlMessage.Replace("{Ext}", Ext);
            //htmlMessage = htmlMessage.Replace("{CallTime}", CallTime);
            htmlMessage = htmlMessage.Replace("{CallDate}", CallTime.Split(' ').First().Trim());
            htmlMessage = htmlMessage.Replace("{CallTime}", CallTime.Split(' ')[1].Trim());
            htmlMessage = htmlMessage.Replace("{NumberDialed}", NumberDialed);
            htmlMessage = htmlMessage.Replace("{Pharmacy}", ddlPharmacy.SelectedItem.Text.Trim());

            if (Ticked != "{items}")
            {
                htmlMessage = htmlMessage.Replace("{Ticked}", Ticked);
            }
            else
            {
                htmlMessage = htmlMessage.Replace("{Ticked}", "");

            }

            if (Other.Trim() != String.Empty)
            {
                htmlMessage = htmlMessage.Replace("{Other}", Other);
            }
            else
            {
                htmlMessage = htmlMessage.Replace(EmailMessage.ReplaceOtherSection, "");
            }

            if (Other.Trim() == String.Empty && Ticked.Contains("Other;"))
            {
                ScriptManager.RegisterStartupScript(this, GetType(),
                    "OtherNoteAlert", "toggleErrorHandler('Oh Snap!','Please specify other.');", true);
                return;
            }

            htmlMessage = htmlMessage.Replace("{DisplayName}", hdnDisplayName.Value.Trim());

            List<SqlParameter> parms = new List<SqlParameter>();

            string problems = Ticked.Replace("<ul>", "").Replace("</ul>", "").Replace("<li>", "").Replace("</li>", "; ");



            DateTime? CallDateTime = null;
            if (CallTime != string.Empty)
            {
                CallDateTime = Convert.ToDateTime(CallTime);
            }

            long PharmacyID = Convert.ToInt64(ddlPharmacy.SelectedValue.Trim());

            parms.Add(new SqlParameter("@UserID", GlobalProperties.UserID));
            parms.Add(new SqlParameter("@CaptureDate", DateTime.Now));
            parms.Add(new SqlParameter("@Ext", Ext.Trim()));
            parms.Add(new SqlParameter("@CallDate", CallTime.Split(' ').First().Trim()));
            parms.Add(new SqlParameter("@CallTime", CallTime.Split(' ')[1].Trim()));
            parms.Add(new SqlParameter("@NumberPhoned ", NumberDialed));
            parms.Add(new SqlParameter("@Problems ", problems));
            if (Other.Trim() != string.Empty)
            {
                parms.Add(new SqlParameter("@OtherProblem ", Other.Trim()));
            }

            parms.Add(new SqlParameter("@CallDateTime ", CallDateTime));
            //parms.Add(new SqlParameter("@WiFi", false));
            //parms.Add(new SqlParameter("@LAN", false));
            parms.Add(new SqlParameter("@ClientIP", ip.Trim().Split(':').First()));
            parms.Add(new SqlParameter("@HostIP", HostIP.Trim()));
            parms.Add(new SqlParameter("@call_id", CallID));

            if (PharmacyID > 0)
            {
                parms.Add(new SqlParameter("@PharmacyID", PharmacyID));
            }

            hdnCallsEmailed.Value = hdnCallsEmailed.Value + CallID + "; ";

            SqlConnections.ExecuteQuery("CSP_AddCallRating", parms, CommandType.StoredProcedure);

            try
            {
                SendEmailFunctions.SendEmail(To, Cc, Subject, Attachments, htmlMessage);
                //ScriptManager.RegisterStartupScript(this, GetType(),
                //    "EmailSuccesfullAlert", "alert('Email was sent to Helpdesk.');", true);
                ScriptManager.RegisterStartupScript(this, GetType(), "EmailSuccesfullAlert", "toggleSuccessHandler('Success!','Email was sent to Helpdesk.')", true);
            }
            catch (Exception ex)
            {
                //ScriptManager.RegisterStartupScript(this, GetType(),
                //    "EmailFailAlert", "alert('Email failed.');", true);
                ScriptManager.RegisterStartupScript(this, GetType(), "toggleErrorHandler", "toggleErrorHandler('Oh Snap!','Email failed.')", true);
            }

            await LoadDataAsync();

        }

        protected void btnSelectCall_Click(object sender, EventArgs e)
        {

            hdnCallTime.Value = "";
            hdnNumberDialed.Value = "";
            lblDisplayCallTime.Text = "";
            lblDisplayNumberDialed.Text = "";
            chklReason.ClearSelection();
            txtComment.Text = "";
            hdncallId.Value = "";

            //GridViewRow row = (GridViewRow)((Button)sender).NamingContainer;
            RepeaterItem row = (RepeaterItem)((Button)sender).NamingContainer;

            Label lblCallTime = (Label)row.FindControl("lblCallTime");
            Label lblNumberDialed = (Label)row.FindControl("lblNumberDialed");
            //Label lblcall_id = (Label)row.FindControl("lblcall_id");
            HiddenField lblcall_id = (HiddenField)row.FindControl("lblcall_id");

            hdnCallTime.Value = lblCallTime.Text.Trim();
            hdnNumberDialed.Value = lblNumberDialed.Text.Trim();
            //hdncallId.Value = lblcall_id.Text.Trim();
            hdncallId.Value = lblcall_id.Value.Trim();

            lblDisplayCallTime.Text = lblCallTime.Text.Trim();
            lblDisplayNumberDialed.Text = lblNumberDialed.Text.Trim();



            ScriptManager.RegisterStartupScript(this, GetType(), "SelectCallModal", "$('#SelectCallModal').modal('show');", true);

        }

        protected async void btnSaveSelectCall_Click(object sender, EventArgs e)
        {
            try
            {
                long ext = Convert.ToInt64(hdnExt.Value.Trim());

                // SelectCallIT copies the selected row's RecordingURL here.
                string recordingUrl = txtcallId.Text.Trim();
                string callTime = txtCallTime.Text.Trim();
                string numberDialed = txtNumberDialed.Text.Trim();

                await SendHelpDeskCallRatingEmailAsync(
                    recordingUrl,
                    ext.ToString(),
                    callTime,
                    numberDialed);

                txtcallId.Text = "";
                txtCallTime.Text = "";
                txtNumberDialed.Text = "";
                hdnCallTime.Value = "";
                hdnNumberDialed.Value = "";
                lblDisplayCallTime.Text = "";
                lblDisplayNumberDialed.Text = "";
                chklReason.ClearSelection();
                txtComment.Text = "";
                hdncallId.Value = "";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error on SaveSelectCall");
            }
        }

        protected void rptTicketsCallList_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            foreach (RepeaterItem Rptitem in rptTicketsCallList.Items)
            {
                HiddenField lblcall_id = (HiddenField)Rptitem.FindControl("lblcall_id");
                if (hdnCallsEmailed.Value.Contains(lblcall_id.Value.Trim() + ";") && hdnCallsEmailed.Value.Trim() != String.Empty)
                {
                    ((HtmlGenericControl)(Rptitem.FindControl("myDiv"))).Attributes.Add("class", "visiibleSelct");
                }

                if (GlobalProperties.Role.Contains("PSP") == true
                    || GlobalProperties.Role.Contains("IT") == true
                    || GlobalProperties.Role.Contains("Admin") == true)
                {
                    Button btnDownloadCall = (Button)Rptitem.FindControl("btnDownloadCall");
                    btnDownloadCall.Visible = true;
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

            HiddenField hfCallId =
                item.FindControl("lblcall_id") as HiddenField;

            HiddenField hfRecordingUrl =
                item.FindControl("hfRecordingUrl") as HiddenField;

            string callId = hfCallId?.Value?.Trim();
            string recordingUrl = hfRecordingUrl?.Value?.Trim();

            Log.Information(
                "Download clicked. CallID: {CallID}, FieldFound: {FieldFound}, RecordingURL: {RecordingURL}",
                callId,
                hfRecordingUrl != null,
                recordingUrl);

            if (string.IsNullOrWhiteSpace(recordingUrl))
            {
                ShowRecordingError("No recording is available for this call.");
                return;
            }

            try
            {
                string url = SqlConnections
                    .GetCallURLAsync(recordingUrl)
                    .GetAwaiter()
                    .GetResult();

                Log.Information(
                    "Download URL lookup completed. CallID: {CallID}, HasDownloadURL: {HasDownloadURL}",
                    callId,
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
                Log.Error(ex, "Recording download failed. CallID: {CallID}", callId);

                ShowRecordingError("The recording could not be downloaded.");
            }
        }

        private void ShowRecordingError(string message)
        {
            string encodedMessage = HttpUtility.JavaScriptStringEncode(message);

            ScriptManager.RegisterStartupScript(
                this,
                GetType(),
                "recordingError",
                $"toggleErrorHandler('Oh Snap!', '{encodedMessage}');",
                true);
        }

    }
}