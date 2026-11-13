using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Patient
{
    public class PatientEnum
    {
        public enum ProgramStatus
        {
            Active = 1,
            Pending = 2,
            Paused = 3,
            Disenrolled = 4
        }
        public enum PatientType
        {
            Managed = 1,
            Unmanaged = 2
        }

        public enum PauseReason
        {
            Pregnancy = 1,
            Hospitalization = 2,
            MedicalDiagnosis = 3,
            IncompleteDocs = 4
        }

        public enum DisenrollReasons
        {
            Unreachable = 1,
            Deceased = 1,
            StoppedTreatmentFunding = 1,
            StoppedTreatmentAE = 1,
            StoppedTreatmentNeverStarted = 1
        }

        public enum PatientStatus
        {
            Managed = 1,
            Unmanaged = 2
        }

        public enum PaymentOption
        {
            SelfFunded = 1,
            MedicalAidFunded = 2
        }

        public enum PatientCarePath
        {
            Reimbursement = 1,
            PatientSupport = 2,
            ReimbursementAndPatientSupport = 3
        }

    }
}