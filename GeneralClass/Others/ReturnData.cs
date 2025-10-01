using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;

namespace InfoTrack.NaqelAPI
{
    [DataContract]
    public class Result
    {
        private string message = "";
        public string Message
        {
            get
            {
                if (HasError == false && WaybillNo == 0)
                    message = GlobalVar.GV.GetLocalizationMessage("SavedSuccessfully");
                else
                    message = GlobalVar.GV.GetLocalizationMessage(message);
                return message;
            }
            set { message = value; }
        }

        public bool HasError = false;
        public int WaybillNo = 0;
        public string BookingRefNo = "";
        public int Key = 0;

        private T ConvertTo<T>() where T : Result, new()
        {
            return new T
            {
                Message = Message,
                HasError = HasError,
                WaybillNo = WaybillNo,
                BookingRefNo = BookingRefNo,
                Key = Key
            };
        }

        public AsrResult ConvertToAsrResult()
        {
            return ConvertTo<AsrResult>();
        }
    }

    [DataContract]
    public class AsrResult : Result
    {
        public DateTime? PickUpDate = null;
    }

    [DataContract]
    public class CancelWaybillResult
    {
        [DataMember]
        public bool IsCanceled { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    [DataContract]
    public class TTresultPriceDetails //SARA
    {   // the defult costructor will intialize numeric values to 0 and sring to null
        public double Price_WithVAT;
        public double Price_WithoutVAT;
        public string Message;
    }
    
    [DataContract]
    public class MultiStickerResult
    {
        public bool HasError = false;
        public string Message = "";
        public byte[] StickerByte;
    }

    [DataContract]
    public class _Result
    {
        public bool HasError = false;
        public string Message = "";
    }

    [DataContract]
    public class CancelDeliveryRequestResult : _Result
    {
        public int WaybillNo;
    }

    [DataContract]
    public class DeliveryRequestResult : CancelDeliveryRequestResult
    {
        public int DeliveryRequestID;
    }

    [DataContract]
    public class CompalintRequestResult : _Result
    {
        public int WaybillNo;
    }

    [DataContract]
    public class CompalintResult : CompalintRequestResult
    {
        //public string ComplaintCause;
        public string ActionTaken;
        public string PreventiveAction;
    }

    [DataContract]
    public class CancelRTOResult : _Result
    {
        public int WaybillNo;
    }

    [DataContract]
    public class CreateRTORequestResult : CancelRTOResult
    {
    }

    [DataContract]
    public class PickupShipmentResult
    {
        public bool HasError = false;
        public string Message = "";
        public List<PickupShipment> PickupShipments;
    }

    [DataContract]
    public class ShipmentDetailResult
    {
        public bool HasError = false;
        public string Message = "";
        public List<ShipmentDetail> ShipmentDetailList;
    }

    public class ShipmentDetail
    {
        public int waybillno { get; set; }
        public string RefNo { get; set; }
        public string Contents { get; set; }
        public string GoodDesc { get; set; }
        public string CreationDate { get; set; }

    }

    [DataContract]
    public class TTresult
    {
        public int Days { get; set; }
        public string Message { get; set; }
    }
    
    [DataContract]
    public class TTresultPrice
    {
        public double Price { get; set; } = 0;
        public string Message { get; set; }
    }

    [DataContract]
    public class PickupShipment
    {
        public int WaybillNo;
        public string RefNo;
        public float PiecesCount = 0;
        public DateTime PickupTime;
    }

    [DataContract]
    public class ShipmentDestinationResult
    {
        public bool HasError = false;
        public string Message = "";
        public List<ShipmentDestination> ShipmentDestinations;
    }

    [DataContract]
    public class ShipmentDestination
    {
        public int WaybillNo;
        public string Destination;
        //public int ClientID;
        //public string ClientName;
    }

    [DataContract]
    public class ASRDetailResult
    {
        public bool HasError = false;
        public string Message = "";
        public List<ASRDetail> ASRDetails;
    }

    [DataContract]
    public class ASRDetail
    {
        public int WaybillNo;
        public string ReferenceNo;
        public float PiecesCount;
        public DateTime ManifestedDate;
        public string Origin;
        public string Destination;
        public string ConsigneeName;
        public string ConsigneeMobile;
        public string PhoneNo;
        public DateTime? PickUpDate;
        public bool IsPickedUp;
        public string LastStatus;
        public int AttemptedCount;
    }

    [DataContract]
    public class Tracking
    {
        [DataMember]
        public string StationCode { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public int ActivityCode { get; set; }

        [DataMember]
        public string Activity { get; set; }

        [DataMember]
        public string ArabicActivity { get; set; }

        [DataMember]
        public int WaybillNo { get; set; }

        [DataMember]
        public int ClientID { get; set; }

        [DataMember]
        public bool HasError { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string Comments { get; set; }

        [DataMember]
        public string RefNo { get; set; }

        [DataMember]
        public int DeliveryStatusID { get; set; }

        [DataMember]
        public string DeliveryStatusMessage { get; set; }

        [DataMember]
        public int EventCode { get; set; }
    }
    
    [DataContract]
    public class TrackingRestoreing
    {
        [DataMember]
        public string ErrorMessage { get; set; }
    }
    
    [DataContract]
    public class WayBillTracking
    {
        [DataMember]
        public int WaybillNo { get; set; }
        [DataMember]
        public int ClientID { get; set; }
        [DataMember]
        public string Org { get; set; }
        [DataMember]
        public string Dest { get; set; }
        [DataMember]
        public DateTime PickUpDate { get; set; }
        [DataMember]
        public string Weight { get; set; }
        [DataMember]
        public string ConsigneeName { get; set; }
        [DataMember]
        public string RefNo { get; set; }
        [DataMember]
        public DateTime LastEventTime { get; set; }
        [DataMember]
        public string LastEvent { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public bool HasError { get; set; }

    }

    [DataContract]
    public class IkeaTracking
    {
        [DataMember]
        public string StationCode { get; set; }

        [DataMember]
        public string Date { get; set; }

        [DataMember]
        public string Time { get; set; }

        [DataMember]
        public int ActivityCode { get; set; }

        [DataMember]
        public string Activity { get; set; }

        [DataMember]
        public string ArabicActivity { get; set; }

        [DataMember]
        public int WaybillNo { get; set; }

        [DataMember]
        public int ClientID { get; set; }

        [DataMember]
        public bool HasError { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string Comments { get; set; }

        [DataMember]
        public string RefNo { get; set; }

        [DataMember]
        public int DeliveryStatusID { get; set; }

        [DataMember]
        public string DeliveryStatusMessage { get; set; }

        [DataMember]
        public int EventCode { get; set; }

    }

    [DataContract]
    public class IkeaTrackingerror
    {

     public string ErrorMessage { get; set; }


    }

    [DataContract]
    public class NewCheckPointsTrack
    {
        [DataMember]
        public int WaybillNo { get; set; }
        [DataMember]
        public int ClientID { get; set; }
        [DataMember]
        public string StationName { get; set; }
        [DataMember]
        public string RefNo { get; set; }
        [DataMember]
        public string Activity { get; set; }
        [DataMember]
        public DateTime Date { get; set; }
        [DataMember]
        public int EventCode { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public bool HasError { get; set; }
    }

    [DataContract]
    public class WaybillDetail
    {
        [DataMember]
        public string WaybillNo { get; set; }

        [DataMember]
        public string RefNo { get; set; }

        [DataMember]
        public string SenderCompanyName { get; set; }

        [DataMember]
        public string SenderPhoneNumber1 { get; set; }

        [DataMember]
        public string SenderAddress { get; set; }

        [DataMember]
        public string SenderName { get; set; }

        [DataMember]
        public string SenderPhoneNumber2 { get; set; }

        [DataMember]
        public string SenderMobile { get; set; }

        [DataMember]
        public string ReceiverName { get; set; }

        [DataMember]
        public string ReceiverPhoneNumber { get; set; }

        [DataMember]
        public string ReceiverAddress { get; set; }

        [DataMember]
        public string ReceiverMobile { get; set; }

        [DataMember]
        public string ShipmentOrigin { get; set; }

        [DataMember]
        public string ShipmentDestination { get; set; }

        [DataMember]
        public string Weight { get; set; }

        [DataMember]
        public string PiecesCount { get; set; }

    }

    [DataContract]
    public class WaybillRange
    {
        private string message = "";
        public string Message
        {
            get
            {
                if (HasError == false)
                    message = GlobalVar.GV.GetLocalizationMessage("SavedSuccessfully");
                else
                    message = GlobalVar.GV.GetLocalizationMessage(message);
                return message;
            }
            set { message = value; }
        }

        public bool HasError;
        [System.ComponentModel.DefaultValue(0)]
        public int FromWaybillNo = 0;
        [System.ComponentModel.DefaultValue(0)]
        public int ToWaybillNo = 0;
    }

    [DataContract]
    public class ShipmentDetails
    {
        [DataMember]
        public double Weight { get; set; }

        [DataMember]
        public double PiecesCount { get; set; }

        [DataMember]
        public int ClientID { get; set; }

        [DataMember]
        public int OrgID { get; set; }

        [DataMember]
        public int DestID { get; set; }
    }

    [DataContract]
    public class LocationDetails
    {
        [DataMember]
        public bool HasLocation { get; set; }

        [DataMember]
        public string Latitude { get; set; }

        [DataMember]
        public string Longitude { get; set; }

        [DataMember]
        public string ConsigneeLocation { get; set; }

    }

    [DataContract]
    public class StorageLocation
    {
        [DataMember]
        public int BinID { get; set; }

        [DataMember]
        public string BinCode { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public int EmployID { get; set; }

        [DataMember]
        public string RackCode { get; set; }

        [DataMember]
        public string AreaCode { get; set; }

        [DataMember]
        public string TerminalCode { get; set; }

        [DataMember]
        public string CityCode { get; set; }

        [DataMember]
        public int DeliverySheetID { get; set; }

        [DataMember]
        public DateTime DeliverySheetDate { get; set; }

        [DataMember]
        public int CourierID { get; set; }

        [DataMember]
        public string CourierName { get; set; }
    }

    [DataContract]
    public class HoldingShipmentResult
    {
        [DataMember]
        public bool ShipmentHold { get; set; }

        [DataMember]
        public string Notes { get; set; }
    }

    [DataContract]
    public class ClientData
    {
        public bool IsPasswordCorrect = false;
        public int ClientID = 0;
        public int ClientAddressID = 0;
        public string ClientAddressFirstAddress = "";
        public string ClientAddressLocation = "";
        public string ClientAddressCountryCode = "";
        public string ClientAddressCityCode = "";
        public string ClientAddressPhoneNo = "";

        public int ClientContactID = 0;
        public string ClientContactName = "";
        public string ClientContactPhoneNumber = "";
        public string ClientContactMobileNo = "";
        public string ClientContactEmail = "";

        public int OriginID = -9;
    }

    [DataContract]
    public class RTOData
    {
        public int ClientID = 0;
        public int RTOWaybillNo = 0;
        public string WaybillNo = "";

    }

    [DataContract]
    public class LogFilesResult:_Result
    {
        public List<RequestFileData> FileData { get; set; }
    }

    public class RequestFileData
    {
        public string FileName { get; set; }
        public string Content { get; set; }
    }

    #region ParcelLocker
 
    public class PLResult
    {
        public string Message;
        public bool HasError = false;
        public List<ParcelInfo> ParcelInfos;

    }

    public class ParcelInfo
    {
        public int ParcelLockerID { get; set; }
        public string ParcelLockerName { get; set; }
        public string ParcelLockerAddress { get; set; }
        public string Location { get; set; }
        public string CityName { get; set; }
        public string CityCode { get; set; }
        public string Country { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        // Opening and closing hours for each day of the week
        public string MonOpeningHour { get; set; }
        public string MonClosingHour { get; set; }
        public string TuesOpeningHour { get; set; }
        public string TuesClosingHour { get; set; }
        public string WedOpeningHour { get; set; }
        public string WedClosingHour { get; set; }
        public string ThurOpeningHour { get; set; }
        public string ThurClosingHour { get; set; }
        public string FriOpeningHour { get; set; }
        public string FriClosingHour { get; set; }
        public string SatOpeningHour { get; set; }
        public string SatClosingHour { get; set; }
        public string SunOpeningHour { get; set; }
        public string SunClosingHour { get; set; }


    }

    public class ViwAPIParcelLockers
    {
        public int ParcelLockerID { get; set; }
        public string ParcelLockerName { get; set; }
        public string ParcelLockerAddress { get; set; }
        public string Location { get; set; }
        public string CityName { get; set; }
        public string CityCode { get; set; }
        public string Country { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        // Opening and closing hours for each day of the week
        public string MonOpeningHour { get; set; }
        public string MonClosingHour { get; set; }
        public string TuesOpeningHour { get; set; }
        public string TuesClosingHour { get; set; }
        public string WedOpeningHour { get; set; }
        public string WedClosingHour { get; set; }
        public string ThurOpeningHour { get; set; }
        public string ThurClosingHour { get; set; }
        public string FriOpeningHour { get; set; }
        public string FriClosingHour { get; set; }
        public string SatOpeningHour { get; set; }
        public string SatClosingHour { get; set; }
        public string SunOpeningHour { get; set; }
        public string SunClosingHour { get; set; }
    }


    #endregion ParcelLocker

    public class BulletDeliveryResult
    {
        public string Message;
        public bool HasError = false;
        public int WaybillNo = 0;
        public string BookingRefNo = "";
        public int Key = 0;

    }

    #region DistanceMatrix API Return Data
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Distance
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Duration
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class DurationInTraffic
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Element
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public DurationInTraffic duration_in_traffic { get; set; }
        public string status { get; set; }
    }

    public class BulletDeliveryResponse
    {

        public List<string> destination_addresses { get; set; }
        public List<string> origin_addresses { get; set; }
        public List<Row> rows { get; set; }
        public string status { get; set; }
    }

    public class Row
    {
        public List<Element> elements { get; set; }
    }


    #endregion DistanceMatrix API Return Data

    public class GetWaybillStickerDetailsResult
    {
        public bool HasError = false;
        public string Message = "";
        public byte[] StickerByte;
        public int WaybillNo { get; set; }
        public string RefNo { get; set; }
    }


    public class ScheduleWaybillResult
    {
        // public string BookingRefNo = null;
        public bool HasError = false;
        public string Message = "";

    }



    [XmlRoot(ElementName = "WaybillNo")]
    public class WaybillNo
    {

        [XmlElement(ElementName = "int")]
        public List<int> Int { get; set; }
    }

    public class ScheduleWaybill
    {

        [XmlElement(ElementName = "ClientInfo")]
        public ClientInformation ClientInfo { get; set; }

        [XmlElement(ElementName = "ScheduleDate")]
        public string ScheduleDate { get; set; }

        [XmlElement(ElementName = "WaybillNo")]
        public WaybillNo WaybillNo { get; set; }
    }



    public class SPLOfficeResult
    {
        public bool HasError = false;
        public string Message { get; set; }
        public List<OfficeInfo> OfficeList;
    }

    public class OfficeInfo
    {
        //public string Message = "";
        public int OfficeCode { get; set; }
        public string Name { get; set; }
        public string FName { get; set; }
        public string CityName { get; set; }
        public string RegionName { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; }


        public string StartWorkingHour { get; set; }


        public string EndWorkingHour { get; set; }
        public string SaturdayStartWorkingHour { get; set; }
        public string SaturdayEndtWorkingHour { get; set; }

        public string CityCode { get; set; }


    }




}