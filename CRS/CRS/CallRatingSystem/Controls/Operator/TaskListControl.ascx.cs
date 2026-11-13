using CallRatingSystem.Classes;
using CallRatingSystem.Classes.Task;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CallRatingSystem.Controls.Operator
{
    public partial class TaskListControl : System.Web.UI.UserControl
    {
        public static long TaskID = 0;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //DAO.LoadDropdown(ddlTaskMessage, "TaskMessageTasklistFilterActive");
                //DAO.LoadDropdown(ddlServiceType, "TaskListFilterPatientStatus");

                //long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
                //long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());

                //LoadTasks(DateTime.Now, TaskMessage, ServiceTypeID);
            }
        }

        protected void btnDueTasks_Click(object sender, EventArgs e)
        {
            long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
            long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
            LoadTasks(DateTime.Now, TaskMessage, ServiceTypeID);
        }

        protected void btnWeek_Click(object sender, EventArgs e)
        {
            long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
            long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
            LoadTasks(DateTime.Now.AddDays(7), TaskMessage, ServiceTypeID);
        }

        protected void btnMonth_Click(object sender, EventArgs e)
        {
            long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
            long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
            LoadTasks(DateTime.Now.AddMonths(1), TaskMessage, ServiceTypeID);
        }

        protected void btnYear_Click(object sender, EventArgs e)
        {
            long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
            long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
            LoadTasks(DateTime.Now.AddYears(1), TaskMessage, ServiceTypeID);
        }

        protected void btnReschedule_Click(object sender, EventArgs e)
        {
            GridViewRow row = (GridViewRow)((Button)sender).NamingContainer;
            Label lblTaskID = (Label)row.FindControl("lblTaskID");
            TaskID = Convert.ToInt64(lblTaskID.Text.Trim());

            ScriptManager.RegisterStartupScript(this, GetType(), "ShowRescheduleModal", "$('#RescheduleModal').modal('show');", true);
        }

        protected void btnCompleteTask_Click(object sender, EventArgs e)
        {
            //GridViewRow row = (GridViewRow)((Button)sender).NamingContainer;
            //Label lblTaskID = (Label)row.FindControl("lblTaskID");
            //Label lblPatientID = (Label)row.FindControl("lblPatientID");
            //Label lblTaskMessageID = (Label)row.FindControl("lblTaskMessageID");

            //TaskID = Convert.ToInt64(lblTaskID.Text.Trim());
            //GlobalProperties.PatientID = Convert.ToInt64(lblPatientID.Text.Trim());
            //long TaskMessageID = Convert.ToInt64(lblTaskMessageID.Text.Trim());

            //if (TaskID > 0 && GlobalProperties.PatientID > 0)
            //{
            //    CloseTaskParameters closeTaskParameters = new CloseTaskParameters();

            //    closeTaskParameters.PatientID = GlobalProperties.PatientID;
            //    closeTaskParameters.UserID = GlobalProperties.UserID;
            //    closeTaskParameters.TaskID = TaskID;
            //    closeTaskParameters.TaskMessageID = TaskMessageID;
            //    closeTaskParameters.CompletedDate = DateTime.Now;
            //    closeTaskParameters.TaskStatusID = (long)TaskEnum.TaskStatus.Completed;

            //    TaskFunctions.CloseTask(closeTaskParameters);

            //    long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
            //    long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
            //    LoadTasks(DateTime.Now, TaskMessage, ServiceTypeID);
            //}
        }

        protected void grvTaskList_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Select")
            {

                int rowIndex;
                if (int.TryParse(e.CommandArgument.ToString(), out rowIndex))
                {

                    Label TaskID;
                    TaskID = (Label)grvTaskList.Rows[rowIndex].FindControl("lblTaskID");
                    GlobalProperties.TaskID = (long)Convert.ToInt64(TaskID.Text);

                    Label lblPatientID;
                    lblPatientID = (Label)grvTaskList.Rows[rowIndex].FindControl("lblPatientID");
                    GlobalProperties.PatientID = (long)Convert.ToInt64(lblPatientID.Text);

                    Label lblTaskMessageID;
                    lblTaskMessageID = (Label)grvTaskList.Rows[rowIndex].FindControl("lblTaskMessageID");
                    GlobalProperties.TaskMessageID = (long)Convert.ToInt64(lblTaskMessageID.Text);



                    Response.Redirect("Patient.aspx");


                }
            }
        }

        private void LoadTasks(DateTime dateTime, long TaskMessage, long ServiceTypeID)
        {

            //List<SqlParameter> parms = new List<SqlParameter>();
            //parms.Add(new SqlParameter("@DueDate", dateTime));

            //if (TaskMessage > 0)
            //    parms.Add(new SqlParameter("@TaskMessageID", TaskMessage));

            //if (ServiceTypeID > 0)
            //    parms.Add(new SqlParameter("@ServiceTypeID", ServiceTypeID));

            //grvTaskList.DataSource = SqlConnections.GetData("CSP_GetTaskList", parms, CommandType.StoredProcedure);
            //grvTaskList.DataBind();

        }

        protected override void Render(HtmlTextWriter writer)
        {
            foreach (GridViewRow r in grvTaskList.Rows)
            {
                if (r.RowType == DataControlRowType.DataRow)
                {
                    r.BackColor = System.Drawing.Color.White;
                    r.Attributes["onmouseover"] = GridViewRowStyle.OnMouseOverRow;
                    r.Attributes["onmouseout"] = GridViewRowStyle.OnMouseOutRow;

                    //DateTime? DueDate = new DateTime();
                    //DueDate = Convert.ToDateTime(r.Cells[6].Text.Trim());

                    DateTime? DueDate = new DateTime();
                    Label lblDueDate = (Label)r.FindControl("lblDueDate");
                    DueDate = Convert.ToDateTime(lblDueDate.Text.Trim());

                    if (DueDate != null)
                    {
                        if (DueDate < DateTime.Today)
                        {
                            r.BackColor = System.Drawing.Color.Red;
                            r.ForeColor = System.Drawing.Color.White;
                            r.Attributes["onmouseout"] = GridViewRowStyle.DueTask;
                        }

                    }

                    if (GlobalProperties.Role.Contains("Admin") == true)
                    {
                        Button btnCompleteTask = (Button)r.FindControl("btnCompleteTask");
                        btnCompleteTask.Visible = true;
                    }

                    for (int i = 0; i < r.Cells.Count; i++)
                    {
                        r.Cells[i].Attributes["onmouseover"] = GridViewRowStyle.OnMouseOverColumn;
                        r.Cells[i].Attributes["onmouseout"] = GridViewRowStyle.OnMouseOutColumn;
                        r.Cells[i].ToolTip = GridViewRowStyle.TooltipMessage;
                        r.Cells[i].Attributes["onclick"] = this.Page.ClientScript.GetPostBackClientHyperlink(this.grvTaskList, "Select$" + r.RowIndex, true);

                    }
                }
            }

            base.Render(writer);
        }

        protected void btnSaveRescheduleTask_Click(object sender, EventArgs e)
        {
            if (TaskID > 0)
            {
                DateTime? RescheduleDate = Convert.ToDateTime(txtReschedule.Text.Trim());

                if (RescheduleDate != null)
                {
                    RescheduleTask(TaskID, RescheduleDate);

                    long TaskMessage = Convert.ToInt64(ddlTaskMessage.SelectedValue.Trim());
                    long ServiceTypeID = Convert.ToInt64(ddlServiceType.SelectedValue.Trim());
                    LoadTasks(DateTime.Now, TaskMessage, ServiceTypeID);
                    ClearFields();
                }

            }
        }

        private void ClearFields()
        {
            txtReschedule.Text = "";
        }

        private void RescheduleTask(long taskID, DateTime? rescheduleDate)
        {
            if (taskID > 0 && rescheduleDate != null)
            {
                //if (rescheduleDate > DateTime.Now)
                //{
                //    List<SqlParameter> parms = new List<SqlParameter>();
                //    parms.Add(new SqlParameter("@TaskID", taskID));
                //    parms.Add(new SqlParameter("@RescheduleDate", rescheduleDate));
                //    SqlConnections.ExecuteQuery("CSP_RescheduleTask", parms, CommandType.StoredProcedure);


                //}
            }
        }


    }
}