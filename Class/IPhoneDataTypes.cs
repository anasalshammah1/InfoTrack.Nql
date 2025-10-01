using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class IPhoneDataTypes
    {
        [DataContract]
        public class IPhoneTrackingResult
        {
            [DataMember]
            public string StationCode { get; set; }

            [DataMember]
            public string StationName { get; set; }

            [DataMember]
            public string StationFName { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public string Activity { get; set; }

            [DataMember]
            public string ActivityFName { get; set; }

            [DataMember]
            public int EventCode { get; set; }

            [DataMember]
            public int TrackingTypeID { get; set; }

            [DataMember]
            public string ImageURL { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public DateTime PickupDate { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public double PiecesCount { get; set; }

            [DataMember]
            public string OrgName { get; set; }

            [DataMember]
            public string OrgFName { get; set; }

            [DataMember]
            public string DestName { get; set; }

            [DataMember]
            public string DestFName { get; set; }

            [DataMember]
            public bool IsDelivered { get; set; }
        }

        [DataContract]
        public class CountryResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            [DataMember]
            public string CountryCode { get; set; }

            [DataMember]
            public string FlagPath { get; set; }
        }

        [DataContract]
        public class StationResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            [DataMember]
            public int CountryID { get; set; }
        }

        //[DataContract]
        //public class City
        //{
        //    [DataMember]
        //    public int ID { get; set; }

        //    [DataMember]
        //    public string Name { get; set; }

        //    [DataMember]
        //    public string FName { get; set; }

        //    [DataMember]
        //    public int CountryID { get; set; }
        //}

        [DataContract]
        public class LoadTypeResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }
        }

        [DataContract]
        public class ComplaintTypeResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }
        }

        [DataContract]
        public class OurLocationsResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            [DataMember]
            public string CityName { get; set; }

            [DataMember]
            public string CityFName { get; set; }

            [DataMember]
            public string CountryName { get; set; }

            [DataMember]
            public string CountryFName { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public string NationalAddressName { get; set; }

            [DataMember]
            public string NationalAddressFName { get; set; }

            [DataMember]
            public string OpeningTime { get; set; }

            [DataMember]
            public string ClosingTime { get; set; }

            [DataMember]
            public bool IsRetailOutlet { get; set; }
        }

        [DataContract]
        public class AppMobileVerificationResult : DefaultDetailsResult
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public int MobileNoVerificationCode { get; set; }

            [DataMember]
            public DateTime Date { get; set; }
        }

        [DataContract]
        public class DefaultDetailsResult
        {
            [DataMember]
            public bool HasError { get; set; }

            [DataMember]
            public string ErrorMessage { get; set; }

            public DefaultDetailsResult()
            {
                HasError = false;
                ErrorMessage = "";
            }
        }

        [DataContract]
        public class RateResult : DefaultDetailsResult
        {
            [DataMember]
            public double Rate { get; set; }

            [DataMember]
            public string TransitTime { get; set; }

            public RateResult()
            {
                Rate = 0;
                TransitTime = "Your shipment will be delivered within 2 business days.";
            }
        }

        //[DataContract]
        //public class FleetDetails
        //{
        //    //[DataMember]
        //    //public string DeviceName { get; set; }

        //    [DataMember]
        //    public string FleetNo { get; set; }

        //    //[DataMember]
        //    //public bool IsConnected { get; set; }

        //    //[DataMember]
        //    //public string LastStatus { get; set; }

        //    //[DataMember]
        //    //public string LastActiveTime { get; set; }

        //    [DataMember]
        //    public string EmployeeID { get; set; }

        //    [DataMember]
        //    public string ContactNo { get; set; }

        //    [DataMember]
        //    public string EmployeeName { get; set; }

        //    //[DataMember]
        //    //public System.Drawing.Bitmap VehicleImage { get; set; }

        //    //[DataMember]
        //    //public string GroupCode { get; set; }

        //    //[DataMember]
        //    //public string GroupName { get; set; }

        //    [DataMember]
        //    public string StationCode { get; set; }

        //    [DataMember]
        //    public string StationName { get; set; }

        //    [DataMember]
        //    public string ChassisNumber { get; set; }

        //    //[DataMember]
        //    //public string Configuration { get; set; }

        //    //[DataMember]
        //    //public string Model { get; set; }

        //    //[DataMember]
        //    //public string Make { get; set; }

        //    //[DataMember]
        //    //public string StartOdometer { get; set; }

        //    //[DataMember]
        //    //public string CurrentStatus { get; set; }

        //    [DataMember]
        //    public string Longitude { get; set; }

        //    [DataMember]
        //    public string Latitude { get; set; }

        //    [DataMember]
        //    public double CurrentSpeed { get; set; }

        //    //[DataMember]
        //    //public string DeviceCode { get; set; }

        //    //[DataMember]
        //    //public string Photo { get; set; }

        //    //[DataMember]
        //    //public System.Drawing.Image Photo1 { get; set; }

        //    //[DataMember]
        //    //public string Department { get; set; }

        //    //[DataMember]
        //    //public string City { get; set; }

        //    //[DataMember]
        //    //public string Designation { get; set; }

        //    //[DataMember]
        //    //public string PreviousStatusActiveTime { get; set; }

        //    //[DataMember]
        //    //public string PreviousLocation { get; set; }

        //    //[DataMember]
        //    //public double MaxSpeed { get; set; }

        //    //[DataMember]
        //    //public double DeviceOdometer { get; set; }

        //    [DataMember]
        //    public double CurrentOdometer { get; set; }
        //}

        [DataContract]
        public class AppClientDetailsResult : DefaultDetailsResult
        {
            [DataMember]
            public int ID { get; set; }

            //[DataMember]
            //public int AppClientID { get; set; }

            [DataMember]
            public string UserName { get; set; }

            [DataMember]
            public string Password { get; set; }

            [DataMember]
            public string EMail { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public bool isMobileVerified { get; set; }

            [DataMember]
            public int LanguageID { get; set; }

            [DataMember]
            public bool GetNotifications { get; set; }

            public AppClientDetailsResult()
            {
                EMail = "";
                GetNotifications = true;
                ID = 0;
                UserName = "";
                LanguageID = 1;
                Password = "";
                //AppClientID = 0;
                Date = DateTime.Now;
            }
        }

        [DataContract]
        public class SyncResult : RefNoResult
        {
            [DataMember]
            public int ID { get; set; }
        }

        [DataContract]
        public class RefNoResult : DefaultDetailsResult
        {
            [DataMember]
            public string RefNo { get; set; }
        }

        [DataContract]
        public class ComplaintDataRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public int ComplaintTypeID { get; set; }

            [DataMember]
            public string ComplaintDetails { get; set; }

            [DataMember]
            public int AppClientID { get; set; }
        }

        [DataContract]
        public class VerifiedMobleNoRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public string MobileNo { get; set; }
        }

        [DataContract]
        public class WaybillListResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public string OriginName { get; set; }

            [DataMember]
            public string OriginFName { get; set; }

            [DataMember]
            public string DestinationName { get; set; }

            [DataMember]
            public string DestinationFName { get; set; }

            [DataMember]
            public DateTime Date { get; set; }
        }

        [DataContract]
        public class AdvResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string ImageURL { get; set; }

            [DataMember]
            public string Link { get; set; }

            [DataMember]
            public int TypeID { get; set; }
        }

        [DataContract]
        public class NewsResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public string ImageURL { get; set; }

            [DataMember]
            public string NewsDetailsEn { get; set; }

            [DataMember]
            public string NewsDetailsAr { get; set; }
        }

        [DataContract]
        public class TrackingDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Reference { get; set; }
        }

        [DataContract]
        public class GetRateDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public int FromCountry { get; set; }

            [DataMember]
            public int FromCity { get; set; }

            [DataMember]
            public int ToCountry { get; set; }

            [DataMember]
            public int ToCity { get; set; }

            [DataMember]
            public int LoadType { get; set; }

            [DataMember]
            public double Weight { get; set; }
        }

        [DataContract]
        public class SignUpDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Password { get; set; }

            [DataMember]
            public string DeviceToken { get; set; }
        }

        [DataContract]
        public class MobileVerificationRequest : DefaultDetailsRequest
        {
            [DataMember]
            public string MobileNo { get; set; }
        }

        [DataContract]
        public class AccountDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public string EMail { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Password { get; set; }

            [DataMember]
            public bool NeedNotification { get; set; }
        }

        [DataContract]
        public class ClientDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string EMail { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string AddressFirstLine { get; set; }

            [DataMember]
            public string AddressSecondLine { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public int CountryID { get; set; }

            [DataMember]
            public int CityID { get; set; }

            [DataMember]
            public string FloorNo { get; set; }

            [DataMember]
            public string DeliveryInstruction { get; set; }
        }

        [DataContract]
        public class CreatingAccountResult : DefaultDetailsResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public int ClientAddressID { get; set; }

            [DataMember]
            public int ClientContactID { get; set; }
        }

        [DataContract]
        public class GetCurrentLOcationByWaybillResult : DefaultDetailsResult
        {
            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            public GetCurrentLOcationByWaybillResult()
            {
                Date = new DateTime();
                Latitude = "0";
                Longitude = "0";
            }
        }

        [DataContract]
        public class DefaultDetailsRequest : DefaultDetailsResult
        {
            [DataMember]
            public int AppTypeID { get; set; }

            [DataMember]
            public string AppVersion { get; set; }

            [DataMember]
            public int LanguageID { get; set; }
        }

        [DataContract]
        public class GetCurrentLOcationByWaybillRequest : DefaultDetailsRequest
        {
            [DataMember]
            public string WaybillNo { get; set; }
        }

        [DataContract]
        public class BookingDetailsRequest : DefaultDetailsRequest
        {
            [DataMember]
            public int AppClientID { get; set; }

            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public int ClientAddressID { get; set; }

            [DataMember]
            public int ClientContactID { get; set; }

            [DataMember]
            public int FromCountry { get; set; }

            [DataMember]
            public int FromCity { get; set; }

            [DataMember]
            public int ToCountry { get; set; }

            [DataMember]
            public int ToCity { get; set; }

            [DataMember]
            public int LoadType { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public double PiecesCount { get; set; }

            [DataMember]
            public DateTime PickupDate { get; set; }

            [DataMember]
            public DateTime ClosingTime { get; set; }

            [DataMember]
            public string PickupNotes { get; set; }
        }
    }
}