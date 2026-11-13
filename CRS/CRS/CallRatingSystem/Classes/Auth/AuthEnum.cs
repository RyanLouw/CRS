using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Auth
{
    public class AuthEnum
    {
        public enum AuthProcessStep
        {
            ConsentForm = 1,
            MotivationForm = 2,
            Prescription = 3,
            LabResults = 4,
            SubmitToFunder = 5,
            ObtainFunderOutcome = 6,
            AdditionalMotivationForm = 7

        }

        public enum AuthType
        {
            New = 1,
            ReMotivation = 2,
            Appeal = 3

        }
        public enum AuthStatus
        {
            ApprovedWithNoCoPayment = 1,
            ApprovedWithCopayment = 2,
            Declined = 3,
            ApprovedExGratia = 4,
            ApprovedExGratiaWithCoPayment = 5

        }





    }
}