using System;

namespace CallRatingSystem.Classes
{
    public class CallInfo
    {
        public string CallID { get; set; }
        public string Extension { get; set; }
        public DateTime? CallDateTime { get; set; }
        public string DurationMinutes { get; set; }
        public bool IsAnswered { get; set; }
        public string Incoming { get; set; }
        public string RecordingURL { get; set; }
        public string RecordingID { get; set; }
        public string CallerNumber { get; set; }
        public string CallerFullName { get; set; }
    }

    public class CallUrlResponse
    {
        public string Url { get; set; }
    }
}