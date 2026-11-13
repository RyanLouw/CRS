using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes
{
    public class GlobalProperties
    {
        public static long PatientID
        {
            get
            {

                if (HttpContext.Current.Session["PatientID"] != null)
                {
                    return (long)HttpContext.Current.Session["PatientID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["PatientID"] = value;

            }
        }
        public static long PatientStatusID
        {
            get
            {

                if (HttpContext.Current.Session["PatientStatusID"] != null)
                {
                    return (long)HttpContext.Current.Session["PatientStatusID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["PatientStatusID"] = value;

            }
        }

        public static Guid UserID
        {
            get
            {

                if (HttpContext.Current.Session["UserID"] != null)
                {
                    return new Guid(HttpContext.Current.Session["UserID"].ToString());
                }
                return new Guid();

            }

            set
            {

                HttpContext.Current.Session["UserID"] = value;

            }
        }

        public static long AuthNumberID
        {
            get
            {

                if (HttpContext.Current.Session["AuthNumberID"] != null)
                {
                    return (long)HttpContext.Current.Session["AuthNumberID"];
                }
                return 0;

            }

            set
            {

                HttpContext.Current.Session["AuthNumberID"] = value;

            }
        }
        public static long TaskID
        {
            get
            {

                if (HttpContext.Current.Session["TaskID"] != null)
                {
                    return (long)HttpContext.Current.Session["TaskID"];
                }
                return 0;

            }

            set
            {

                HttpContext.Current.Session["TaskID"] = value;

            }
        }
        public static long AuthProcessID
        {
            get
            {

                if (HttpContext.Current.Session["AuthProcessID"] != null)
                {
                    return (long)HttpContext.Current.Session["AuthProcessID"];
                }
                return 0;

            }

            set
            {

                HttpContext.Current.Session["AuthProcessID"] = value;

            }
        }

        public static long AuthProcessStepsID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemAuthProcessStepsID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemAuthProcessStepsID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemAuthProcessStepsID"] = value;

            }
        }

        public static long AuthStepID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemAuthStepID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemAuthStepID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemAuthStepID"] = value;

            }
        }

        public static long BookingProcessID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemBookingProcessID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemBookingProcessID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemBookingProcessID"] = value;

            }
        }

        public static long BookingProcessStepsID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemBookingProcessStepsID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemBookingProcessStepsID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemBookingProcessStepsID"] = value;

            }
        }

        public static long BookingStepID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemBookingStepID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemBookingStepID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemBookingStepID"] = value;

            }
        }

        public static long BookingAuthProcessID
        {
            get
            {

                if (HttpContext.Current.Session["CallRatingSystemBookingAuthProcessID"] != null)
                {
                    return (long)HttpContext.Current.Session["CallRatingSystemBookingAuthProcessID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["CallRatingSystemBookingAuthProcessID"] = value;

            }
        }

        public static long TaskMessageID
        {
            get
            {

                if (HttpContext.Current.Session["TaskMessageID"] != null)
                {
                    return (long)HttpContext.Current.Session["TaskMessageID"];
                }
                return 0;

            }
            set
            {

                HttpContext.Current.Session["TaskMessageID"] = value;

            }
        }

        public static string CaseManagerEmail
        {
            get
            {

                if (HttpContext.Current.Session["CaseManagerEmail"] != null)
                {
                    return HttpContext.Current.Session["CaseManagerEmail"].ToString();
                }
                return "";

            }
            set
            {

                HttpContext.Current.Session["CaseManagerEmail"] = value;

            }
        }

        public static string Role
        {
            get
            {

                if (HttpContext.Current.Session["Role"] != null)
                {
                    return HttpContext.Current.Session["Role"].ToString();
                }
                return "";

            }
            set
            {

                HttpContext.Current.Session["Role"] = value;

            }
        }
        public static string EmployeeId
        {
            get
            {

                if (HttpContext.Current.Session["EmployeeId"] != null)
                {
                    return HttpContext.Current.Session["EmployeeId"].ToString();
                }
                return "";

            }
            set
            {

                HttpContext.Current.Session["EmployeeId"] = value;

            }
        }

    }
}