using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Task
{
    public class AddTaskParameters
    {
        public long PatientID { get; set; }
        public long TaskID { get; set; }
        public long TaskStatusID { get; set; }
        public long TaskMessageID { get; set; }
        public long ContactPersonID { get; set; }
        public DateTime? NextScheduledDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Note { get; set; }
        public Guid UserID { get; set; }
    }
}