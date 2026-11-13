using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Task
{
    public class TaskEnum
    {
        public enum ContactPerson
        {
            DSP = 1,
            Nurse = 2,
            Patient = 3,
            PV = 4,
            Doctor = 5,
            Funder = 6,
            MarketAccess = 7,
            PharmacyClinic = 8,

        }

        public enum TaskMessage
        {

            IntroductionCall = 1,
            RescheduleTask = 2,
            StartReMotivationProcess = 3,
            ObtainLabResults = 4,
            ObtainDocumentation = 5,
            FollowUpOnReMotDocs = 6,
            FollowUpMedicalAidToObtainAuth3Days = 7,
            FollowUpMedicalAidToObtainAuth5Days = 8,
            FollowUpMedicalAidToObtainAuth7Days = 9,
            ObtainnewRx = 10,
            ArrangeDelivery = 11,
            ConfirmDeliveryReceived = 12,
            ComplianceCall = 13,
            CompleteConsentFormAuthStep = 14,
            CompleteMotivationFormAuthStep = 15,
            CompletePrescriptionAuthStep = 16,
            CompleteLabResultsAuthStep = 17,
            CompleteSubmittofunderAuthStep = 18,
            CompleteObtainfunderoutcomeAuthStep = 19,
            CompleteAdditionalMotivationFormAuthStep = 20,
            Other = 21,
            SubmitToFunder = 22,
            Appeal = 23,
            StartNewAuthProcess = 24,
            ApprovalSMS = 25,
            WelcomeSMS = 26,
            ReceivedDeliverySMS = 27,
            Call1 = 28,
            Call2 = 29,
            Call3 = 30,
            Call4 = 31,
            Call5 = 32,
            BookingReminderSMS = 33,
            CompletePhonePatientBookingStep = 34,
            CompleteContactHospitalBookingStep = 35,
            CompleteObtainRxBookingStep = 36,
            CompleteObtainAuthNumberBookingStep = 37,
            CompleteOrderStockBookingStep = 38,
            CompleteEstimatedBookingDateBookingStep = 39,
            CompleteConfirmInjectionReceivedBookingStep = 40,
            ArrangeBooking = 41,
            FollowUpMissedInjection = 42,
            ArrangeBooking2 = 43,
            InjectionConfirmation = 44,
            AuthOutcomeEmail = 45,
            AdherenceCall3Months = 46,
            AdherenceCall6Months = 47,
            AdherenceCall7Months = 48,
            NewPatientHCPEmail = 49,
            SubmitToFunderHCPEmail = 50,
            RxExpirySMS = 51,
            AuthExpirySMS = 52

        }

        public enum TaskStatus
        {
            Completed = 1,
            NotCompleted = 2,
        }
    }
}