using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Task
{
    public class CloseAllTasksParameters
    {
        public long PatientID { get; set; }
        public Guid UserID { get; set; }
        public DateTime CompletedDate { get; set; }
    }
}