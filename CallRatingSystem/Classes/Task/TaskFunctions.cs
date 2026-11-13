using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Task
{
    public class TaskFunctions
    {
        //public static void CompleteTask(CompleteTaskParameters task)
        //{

        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //    parms.Add(new SqlParameter("@TaskID", task.TaskID));
        //    parms.Add(new SqlParameter("@CompletedDate", Convert.ToDateTime(DateTime.Now)));
        //    parms.Add(new SqlParameter("@CompletedBy", task.UserID));

        //    SqlConnections.ExecuteQuery("CSP_CompleteTask", parms, CommandType.StoredProcedure);

        //}
        //public static void CompleteTaskByTaskMessage(CompleteTaskParameters task)
        //{

        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //    parms.Add(new SqlParameter("@TaskID", task.TaskID));
        //    parms.Add(new SqlParameter("@CompletedDate", Convert.ToDateTime(DateTime.Now)));
        //    parms.Add(new SqlParameter("@CompletedBy", task.UserID));
        //    parms.Add(new SqlParameter("@TaskMessageID", task.TaskMessageID));

        //    SqlConnections.ExecuteQuery("CSP_CompleteTaskByTaskMessage", parms, CommandType.StoredProcedure);

        //}

        //public static long AddTaskReturnTaskID(AddTaskParameters task)
        //{
        //    long TaskID = 0;
        //    if (task.PatientID > 0)
        //    {
        //        List<SqlParameter> parms = new List<SqlParameter>();

        //        parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //        parms.Add(new SqlParameter("@CreatedDate", task.CreatedDate));
        //        parms.Add(new SqlParameter("@CreatedBy", task.UserID));
        //        parms.Add(new SqlParameter("@TaskStatusID", task.TaskStatusID));
        //        parms.Add(new SqlParameter("@ContactPersonID", task.ContactPersonID));
        //        parms.Add(new SqlParameter("@TaskMessageID", task.TaskMessageID));


        //        if (task.Note != null)
        //        {
        //            if (task.Note.Trim() != string.Empty)
        //                parms.Add(new SqlParameter("@Note", task.Note.Trim()));
        //        }


        //        if (task.NextScheduledDate != null)
        //        {
        //            parms.Add(new SqlParameter("@NextSheduledDate", task.NextScheduledDate));
        //        }

        //        if (task.DueDate != null)
        //        {
        //            parms.Add(new SqlParameter("@DueDate", task.DueDate));
        //        }
        //        else
        //        {
        //            if (task.NextScheduledDate != null)
        //            {
        //                parms.Add(new SqlParameter("@DueDate", task.NextScheduledDate));
        //            }
        //            else
        //            {
        //                parms.Add(new SqlParameter("@DueDate", DateTime.Now.AddDays(1)));
        //            }
        //        }

        //        TaskID = SqlConnections.ExecuteQueryAndGetID("CSP_AddTask", parms, CommandType.StoredProcedure);

        //    }


        //    return TaskID;
        //}

        //public static void CloseTask(CloseTaskParameters task)
        //{

        //    List<SqlParameter> parms = new List<SqlParameter>();
        //    parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //    parms.Add(new SqlParameter("@CompletedDate", task.CompletedDate));
        //    parms.Add(new SqlParameter("@CompletedBy", task.UserID));
        //    if (task.TaskMessageID > 0)
        //    {
        //        parms.Add(new SqlParameter("@TaskMessageID", task.TaskMessageID));
        //    }

        //    if (task.TaskID > 0)
        //    {
        //        parms.Add(new SqlParameter("@TaskID", task.TaskID));
        //    }
        //    SqlConnections.ExecuteQuery("CSP_CloseTask", parms, CommandType.StoredProcedure);


        //}

        //public static void CloseTaskNoTaskID(CloseTaskNoTaskID task)
        //{

        //    List<SqlParameter> parms = new List<SqlParameter>();
        //    parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //    parms.Add(new SqlParameter("@CompletedDate", task.CompletedDate));
        //    parms.Add(new SqlParameter("@CompletedBy", task.UserID));
        //    if (task.TaskMessageID > 0)
        //    {
        //        parms.Add(new SqlParameter("@TaskMessageID", task.TaskMessageID));
        //    }

        //    SqlConnections.ExecuteQuery("CSP_CloseTaskNoTaskID", parms, CommandType.StoredProcedure);


        //}

        //public static void CloseAllTasks(CloseAllTasksParameters task)
        //{

        //    List<SqlParameter> parms = new List<SqlParameter>();
        //    parms.Add(new SqlParameter("@PatientID", task.PatientID));
        //    parms.Add(new SqlParameter("@CompletedDate", task.CompletedDate));
        //    parms.Add(new SqlParameter("@CompletedBy", task.UserID));
        //    SqlConnections.ExecuteQuery("CSP_CloseAllTasks", parms, CommandType.StoredProcedure);

        //}

        public static DateTime AddWorkdays(DateTime originalDate, int workDays)
        {
            DateTime tmpDate = originalDate;

            while (workDays > 0)
            {
                tmpDate = tmpDate.AddDays(1);
                if (tmpDate.DayOfWeek < DayOfWeek.Saturday &&
                    tmpDate.DayOfWeek > DayOfWeek.Sunday)
                    workDays--;
            }

            if (tmpDate < DateTime.Today)
            {
                tmpDate = DateTime.Today;
            }

            return tmpDate;
        }

        public static DateTime AddWorkdays(DateTime originalDate)
        {
            DateTime tmpDate = originalDate;

            if (originalDate.DayOfWeek == DayOfWeek.Saturday)
            {
                tmpDate = originalDate.AddDays(2);
            }
            if (originalDate.DayOfWeek == DayOfWeek.Sunday)
            {
                tmpDate = originalDate.AddDays(1);
            }

            return tmpDate;


        }

        //public static void AddTask(long TaskMessage, DateTime NextScheduledDate, long ContactPersonID)
        //{
        //    AddTaskParameters addTaskParameters = new AddTaskParameters();
        //    addTaskParameters.PatientID = GlobalProperties.PatientID;
        //    addTaskParameters.CreatedDate = DateTime.Now;
        //    addTaskParameters.UserID = GlobalProperties.UserID;
        //    addTaskParameters.TaskStatusID = (long)TaskEnum.TaskStatus.NotCompleted;
        //    addTaskParameters.ContactPersonID = ContactPersonID;
        //    addTaskParameters.TaskMessageID = TaskMessage;
        //    addTaskParameters.NextScheduledDate = NextScheduledDate;
        //    TaskFunctions.AddTaskReturnTaskID(addTaskParameters);
        //}

        //public static void CloseTasks(TaskEnum.TaskMessage taskMessage)
        //{
        //    CloseTaskNoTaskID closeTaskNoTaskID = new CloseTaskNoTaskID();
        //    closeTaskNoTaskID.PatientID = GlobalProperties.PatientID;
        //    closeTaskNoTaskID.TaskMessageID = (long)taskMessage;
        //    closeTaskNoTaskID.UserID = GlobalProperties.UserID;
        //    closeTaskNoTaskID.CompletedDate = DateTime.Now;

        //    TaskFunctions.CloseTaskNoTaskID(closeTaskNoTaskID);
        //}

        //public static void CloseTasks(long taskMessageID)
        //{
        //    CloseTaskNoTaskID closeTaskNoTaskID = new CloseTaskNoTaskID();
        //    closeTaskNoTaskID.PatientID = GlobalProperties.PatientID;
        //    closeTaskNoTaskID.TaskMessageID = taskMessageID;
        //    closeTaskNoTaskID.UserID = GlobalProperties.UserID;
        //    closeTaskNoTaskID.CompletedDate = DateTime.Now;

        //    TaskFunctions.CloseTaskNoTaskID(closeTaskNoTaskID);
        //}



    }
}