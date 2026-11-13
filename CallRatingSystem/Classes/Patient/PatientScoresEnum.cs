using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CallRatingSystem.Classes.Patient
{
    public class PatientScoresEnum
    {
        public enum BiometricsSource
        {
            Lab = 1,
            Clinic = 2,
            PatientReported = 3,
            GlucometerValue = 4
        }

        public enum ClinicalIndicator
        {
            EASI = 1,
            DLQI = 2,
            BSA = 3,
            LDL = 4,
            HDL = 5,
            Triglicyrides = 6,
            BloodPressure = 7,
            Microalbuminuria = 8,
            Eyes = 9,
            Feet = 10,
            Smoking = 11,
            BMI = 12,
            GeneralRecommendation = 13,
            Weight = 14
        }
    }
}