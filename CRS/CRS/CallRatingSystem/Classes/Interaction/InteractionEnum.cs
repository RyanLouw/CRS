using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Interaction
{
    public class InteractionEnum
    {
        public enum InteractionStatus
        {
            Complete = 1,
            Unreachable = 2,
            Reschedule = 3
        }

        public enum InteractionType
        {
            Call = 1,
            Email = 2,
            SMSWhatsapp = 3
        }

    }
}