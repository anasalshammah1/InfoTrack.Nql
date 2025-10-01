using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class ClientTrackingDetails
    {
        public ClientInformation ClientInfo;
        public DateTime? FromDate;
        public DateTime? ToDate;
        public int PageCount;
    }

    public class TrackingDetails
    {
        public int WaybillNo;
        public string RefNo;
        public DateTime Date;
        public string Activity;
        public string Name; // ConsigneeName
        public string FirstAddress; //ConsigneeAddress
        public string PhoneNumber; // ConsigneePhoneNumber
        public string Mobile; //ConsigneeMobile
        //public decimal RecordsCount;
        public decimal NumberOfPages;
        //public DateTime Date;
        //public string ArabicActivity;
        //public string StationCode;
        //public bool HasError;
        //public string ErrorMessage;
        //public string Comments;
        //public int ActivityCode;
        //public int EventCode;
        //public int DeliveryStatusID;
        //public string DeliveryStatusMessage;
        //public string ConsigneeEmail;

    }

    public class TrackingPageDetail
    {
        //public int RecordsCount;
        public int NumberOfPages;
    }

    public class ReturnTrackingDetails
    {
        public int WaybillNo;
        public string RefNo;
        public DateTime Date;
        public string Activity;
        public string Name; // ConsigneeName
        public string FirstAddress; //ConsigneeAddress
        public string PhoneNumber; // ConsigneePhoneNumber
        public string Mobile; //ConsigneeMobile
        public int RecordsCount;
        public int NumberOfPages;

    }

    public class PODTrackingStatus
    {
        public int WaybillNo;
        public DateTime Date;
        public string Activity;
        public string ArActivity;
        public bool HasError = false;
        public string ErrorMessage = "";
    }

    public class LastEventTrackingStatus
    {
        public int WaybillNo;
        public DateTime Date;
        public int EventCode;
        public string Activity;
        public string ArActivity;
        public bool HasError = false;
        public string ErrorMessage = "";
    }

}