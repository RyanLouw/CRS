using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Task
{
    public class CloseTaskNoTaskID
    {
        public long PatientID { get; set; }
        public long TaskID { get; set; }
        public long TaskStatusID { get; set; }
        public long TaskMessageID { get; set; }
        public Guid UserID { get; set; }
        public DateTime CompletedDate { get; set; }
    }
}