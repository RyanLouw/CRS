using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Patient
{
    public class Booking
    {
        public enum BookingProcessStep
        {
            PhonePatient = 1,
            ContactHospital = 2,
            ObtainRx = 3,
            ObtainAuthNumber = 4,
            OrderStock = 5,
            EstimatedBookingDate = 6,
            ConfirmInjectionReceived = 7

        }

        public enum MissedBookingReason
        {
            Travel = 1,
            Financial = 2,
            Personal = 3
            
        }

        public enum BookingStatus
        {
            MissedAppointment = 1,
            CompletedAppointment = 2

        }
    }
}