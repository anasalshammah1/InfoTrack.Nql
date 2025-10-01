using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace InfoTrack.NaqelAPI.Class
{
    public class RouteOptimizationDataType
    {
        #region Default 

        [DataContract]
        public class DefaultResult
        {
            [DataMember]
            public bool HasError { get; set; }

            [DataMember]
            public string ErrorMessage { get; set; }

            public DefaultResult()
            {
                HasError = false;
                ErrorMessage = "";
            }
        }

        [DataContract]
        public class DefaultRequest
        {
            [DataMember]
            public int AppTypeID { get; set; }

            [DataMember]
            public string AppVersion { get; set; }

            [DataMember]
            public int LanguageID { get; set; }

            public DefaultRequest()
            {
                AppTypeID = 1;
                AppVersion = "1.0";
                LanguageID = 1;
            }
        }

        [DataContract]
        public class LocationCoordinate
        {
            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }
        }

        #endregion

        #region Request

        [DataContract]
        public class GetPasswordRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class CheckTruckIDRequest : DefaultRequest
        {
            [DataMember]
            public int TruckID { get; set; }
        }

        [DataContract]
        public class GetDeliveryStatusRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class GetCheckPointTypeRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class GetStationRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class GetWaybillDetailsRequest : DefaultRequest
        {
            [DataMember]
            public int WaybillNo { get; set; }
        }

        [DataContract]
        public class GetLoadTypeRequest : DefaultRequest
        {
            [DataMember]
            public int ClientID { get; set; }
        }

        [DataContract]
        public class GetShipmentForPickingRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class ShipmentsForDeliverySheetRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class CheckWaybillAlreadyPickedUpRequest : DefaultRequest
        {
            [DataMember]
            public int WaybillNo { get; set; }
        }

        [DataContract]
        public class CheckBeforeSubmitCODRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public int DeliverySheetID { get; set; }

            [DataMember]
            public double TotalCash { get; set; }

            [DataMember]
            public double TotalPOS { get; set; }
        }

        [DataContract]
        public class DeliverySheetCheckingRequest : DefaultRequest
        {
            [DataMember]
            public int WaybillNo { get; set; }
        }

        [DataContract]
        public class CheckPendingCODRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class OptimizedOutOfDeliveryShipmentRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class NewCallRequest : DefaultRequest
        {
            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public string MobileNo { get; set; }
        }

        [DataContract]
        public class GetBookingDetailsRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class UserMELoginRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public int StateID { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public string HHDName { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            [DataMember]
            public int TruckID { get; set; }

            public UserMELoginRequest()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class OnDeliveryRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string WaybillNo { get; set; }

            [DataMember]
            public string ReceiverName { get; set; }

            [DataMember]
            public int PiecesCount { get; set; }

            [DataMember]
            public DateTime TimeIn { get; set; }

            [DataMember]
            public DateTime TimeOut { get; set; }

            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public int StationID { get; set; }

            [DataMember]
            public bool IsPartial { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public double ReceivedAmt { get; set; }

            [DataMember]
            public double POSAmount { get; set; }

            [DataMember]
            public double CashAmount { get; set; }

            [DataMember]
            public string ReceiptNo { get; set; }

            [DataMember]
            public int StopPointsID { get; set; }

            [DataMember]
            public List<OnDeliveryDetailRequest> OnDeliveryDetailRequestList { get; set; }

            public OnDeliveryRequest()
            {
                OnDeliveryDetailRequestList = new List<RouteOptimizationDataType.OnDeliveryDetailRequest>();
                ReceivedAmt = 0;
                CashAmount = 0;
                POSAmount = 0;
            }
        }

        [DataContract]
        public class OnDeliveryDetailRequest : DefaultRequest
        {
            [DataMember]
            public String BarCode { get; set; }
        }

        [DataContract]
        public class GetUserMEDataRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public string Passowrd { get; set; }
        }

        [DataContract]
        public class CheckNewVersionRequest : DefaultRequest
        {
            [DataMember]
            public int AppSystemSettingID { get; set; }

            [DataMember]
            public string CurrentVersion { get; set; }
        }

        [DataContract]
        public class NotDeliveredRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string WaybillNo { get; set; }

            [DataMember]
            public DateTime TimeIn { get; set; }

            [DataMember]
            public DateTime TimeOut { get; set; }

            [DataMember]
            public int UserID { get; set; }

            [DataMember]
            public int StationID { get; set; }

            [DataMember]
            public int PiecesCount { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public int DeliveryStatusID { get; set; }

            [DataMember]
            public string Notes { get; set; }

            [DataMember]
            public List<NotDeliveredDetailRequest> NotDeliveredDetailRequestList { get; set; }

            public NotDeliveredRequest()
            {
                NotDeliveredDetailRequestList = new List<RouteOptimizationDataType.NotDeliveredDetailRequest>();
            }
        }

        [DataContract]
        public class NotDeliveredDetailRequest : DefaultRequest
        {
            [DataMember]
            public String BarCode { get; set; }
        }

        [DataContract]
        public class BringPickUpDataRequest : DefaultRequest
        {
            [DataMember]
            public int WaybillNo { get; set; }
        }

        [DataContract]
        public class BringMyRouteShipmentsRequest : DefaultRequest
        {
            [DataMember]
            public int EmployID { get; set; }
        }

        [DataContract]
        public class PickUpRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string WaybillNo { get; set; }

            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public int FromStationID { get; set; }

            [DataMember]
            public int ToStationID { get; set; }

            [DataMember]
            public int PiecesCount { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public DateTime TimeIn { get; set; }

            [DataMember]
            public DateTime TimeOut { get; set; }

            [DataMember]
            public int UserMEID { get; set; }

            [DataMember]
            public int StationID { get; set; }

            [DataMember]
            public string RefNo { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public double ReceivedAmount { get; set; }

            [DataMember]
            public List<PickUpDetailRequest> PickUpDetailRequestList { get; set; }

            public PickUpRequest()
            {
                ReceivedAmount = 0;
                PickUpDetailRequestList = new List<RouteOptimizationDataType.PickUpDetailRequest>();
            }
        }

        [DataContract]
        public class PickUpDetailRequest : DefaultRequest
        {
            [DataMember]
            public string BarCode { get; set; }
        }

        [DataContract]
        public class SendCheckPointRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public int CheckPointTypeID { get; set; }

            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public List<CheckPointWaybillDetails> CheckPointWaybillDetailsRequestList { get; set; }

            public SendCheckPointRequest()
            {
                CheckPointWaybillDetailsRequestList = new List<CheckPointWaybillDetails>();
            }
        }

        [DataContract]
        public class CheckPointWaybillDetails : DefaultRequest
        {
            [DataMember]
            public string WaybillNo { get; set; }
        }

        [DataContract]
        public class OnCLoadingForDeliverySheetRequest : DefaultRequest
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int CourierID { get; set; }

            [DataMember]
            public int UserID { get; set; }

            [DataMember]
            public DateTime CTime { get; set; }

            [DataMember]
            public int PieceCount { get; set; }

            [DataMember]
            public string TruckID { get; set; }

            [DataMember]
            public int WaybillCount { get; set; }

            [DataMember]
            public int StationID { get; set; }

            [DataMember]
            public List<OnCLoadingForDeliverySheetWaybill> OnCLoadingForDeliverySheetWaybillList { get; set; }

            [DataMember]
            public List<OnCLoadingForDeliverySheetPiece> OnCLoadingForDeliverySheetPieceList { get; set; }

            public OnCLoadingForDeliverySheetRequest()
            {
                OnCLoadingForDeliverySheetWaybillList = new List<OnCLoadingForDeliverySheetWaybill>();
                OnCLoadingForDeliverySheetPieceList = new List<OnCLoadingForDeliverySheetPiece>();
            }
        }

        [DataContract]
        public class OnCLoadingForDeliverySheetWaybill : DefaultRequest
        {
            [DataMember]
            public string WaybillNo { get; set; }
        }

        [DataContract]
        public class OnCLoadingForDeliverySheetPiece : DefaultRequest
        {
            [DataMember]
            public string BarCode { get; set; }
        }

        #endregion

        #region Results

        [DataContract]
        public class DeliveryStatusResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            public DeliveryStatusResult(int id, string code, string name, string fname)
            {
                ID = id;
                Code = code;
                Name = name;
                FName = fname;
            }
        }

        [DataContract]
        public class CheckPointTypeResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }

            public CheckPointTypeResult(int id, string name, string fname)
            {
                ID = id;
                Name = name;
                FName = fname;
            }
        }

        [DataContract]
        public class StationResult : DefaultResult
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

            public StationResult(int id, string code, string name, string fname, int countryID)
            {
                ID = id;
                Code = code;
                Name = name;
                FName = fname;
                countryID = CountryID;
            }
        }

        [DataContract]
        public class WaybillDetailsResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public int PiecesCount { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public string BillingType { get; set; }

            [DataMember]
            public double CODAmount { get; set; }

            [DataMember]
            public string ConsigneeName { get; set; }

            [DataMember]
            public string ConsigneeFName { get; set; }

            [DataMember]
            public string PhoneNo { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Address { get; set; }

            [DataMember]
            public string SecondLine { get; set; }

            [DataMember]
            public string Near { get; set; }

            [DataMember]
            public string CityName { get; set; }

            [DataMember]
            public string CityFName { get; set; }

            [DataMember]
            public LocationCoordinate locationCoordinate { get; set; }

            [DataMember]
            public List<String> BarCodeList { get; set; }

            public WaybillDetailsResult()
            {
                ID = 0;
                WaybillNo = 0;
                PiecesCount = 0;
                Weight = 0;
                CODAmount = 0;
                locationCoordinate = new LocationCoordinate();
                locationCoordinate.Latitude = "";
                locationCoordinate.Longitude = "";
                SecondLine = "";
                Near = "";
                BarCodeList = new List<string>();
            }
        }

        [DataContract]
        public class CheckWaybillAlreadyPickedUpResult : DefaultResult
        {
            [DataMember]
            public bool hasPickedUp { get; set; }

            public CheckWaybillAlreadyPickedUpResult()
            {
                hasPickedUp = false;
            }
        }

        [DataContract]
        public class CheckBeforeSubmitCODResult : DefaultResult
        {
            [DataMember]
            public string Notes { get; set; }

            //[DataMember]
            //public List<WaybillsNoDeliveryScan> WaybillsNoDeliveryScan { get; set; }

            //public CheckBeforeSubmitCODResult()
            //{
            //    WaybillsNoDeliveryScan = new List<RouteOptimizationDataType.WaybillsNoDeliveryScan>();
            //}
        }

        [DataContract]
        public class DeliverySheetCheckingResult : DefaultResult
        {
            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public int PiecesCount { get; set; }

            [DataMember]
            public List<DeliverySheetCheckingPieces> DeliverySheetCheckingPiecesList { get; set; }

            public DeliverySheetCheckingResult()
            {
                DeliverySheetCheckingPiecesList = new List<DeliverySheetCheckingPieces>();
            }
        }

        [DataContract]
        public class CheckPendingCODResult : DefaultResult
        {
            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public DateTime DeliveryDate { get; set; }

            [DataMember]
            public double Amount { get; set; }
        }

        [DataContract]
        public class GetLoadTypeResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string FName { get; set; }
        }

        [DataContract]
        public class OptimizedOutOfDeliveryShipmentResult : DefaultResult
        {
            [DataMember]
            public string WaybillNo { get; set; }
        }

        [DataContract]
        public class DeliverySheetCheckingPieces : DefaultResult
        {
            [DataMember]
            public string BarCode { get; set; }
        }

        //[DataContract]
        //public class WaybillsNoDeliveryScan : DefaultResult
        //{
        //    [DataMember]
        //    public int WaybillNo { get; set; }
        //}

        [DataContract]
        public class BookingDetailsResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int BookingRefNo { get; set; }

            [DataMember]
            public string ClientName { get; set; }

            public BookingDetailsResult()
            {
                BookingRefNo = 0;
            }
        }

        [DataContract]
        public class SendUserMeLoginResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public SendUserMeLoginResult()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class SendOnDeliveryResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public SendOnDeliveryResult()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class GetUserMEDataResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int EmployID { get; set; }

            [DataMember]
            public string EmployName { get; set; }

            [DataMember]
            public string EmployFName { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Password { get; set; }

            [DataMember]
            public int StationID { get; set; }

            [DataMember]
            public string StationCode { get; set; }

            [DataMember]
            public string StationName { get; set; }

            [DataMember]
            public string StationFName { get; set; }

            [DataMember]
            public int RoleMEID { get; set; }

            [DataMember]
            public int StatusID { get; set; }

            public GetUserMEDataResult()
            {
                MobileNo = "";
                StationCode = "";
                StationName = "";
                StationFName = "";
            }
        }

        [DataContract]
        public class ShipmentsForDeliveryResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public double PiecesCount { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public string BillingType { get; set; }

            [DataMember]
            public double CODAmount { get; set; }

            [DataMember]
            public string ConsigneeName { get; set; }

            [DataMember]
            public string ConsigneeFName { get; set; }

            [DataMember]
            public string PhoneNo { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Address { get; set; }

            [DataMember]
            public string SecondLine { get; set; }

            [DataMember]
            public string Near { get; set; }

            [DataMember]
            public LocationCoordinate LocationCoordinate { get; set; }

            [DataMember]
            public List<ShipmentsForDeliveryPieces> ShipmentsForDeliveryPiecesList { get; set; }

            public ShipmentsForDeliveryResult()
            {
                LocationCoordinate = new LocationCoordinate();
                ShipmentsForDeliveryPiecesList = new List<ShipmentsForDeliveryPieces>();
            }
        }

        [DataContract]
        public class ShipmentsForDeliveryPieces : DefaultRequest
        {
            [DataMember]
            public String BarCode { get; set; }
        }

        [DataContract]
        public class ShipmentsForPickingResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public int WaybillNo { get; set; }

            [DataMember]
            public double PiecesCount { get; set; }

            [DataMember]
            public double Weight { get; set; }

            [DataMember]
            public string BillingType { get; set; }

            [DataMember]
            public double CODAmount { get; set; }

            [DataMember]
            public string ConsigneeName { get; set; }

            [DataMember]
            public string ConsigneeFName { get; set; }

            [DataMember]
            public string PhoneNo { get; set; }

            [DataMember]
            public string MobileNo { get; set; }

            [DataMember]
            public string Address { get; set; }

            [DataMember]
            public string SecondLine { get; set; }

            [DataMember]
            public string Near { get; set; }

            [DataMember]
            public LocationCoordinate LocationCoordinate { get; set; }

            public ShipmentsForPickingResult()
            {
                LocationCoordinate = new LocationCoordinate();
            }
        }

        [DataContract]
        public class CheckNewVersionResult : DefaultResult
        {
            [DataMember]
            public bool HasNewVersion { get; set; }

            [DataMember]
            public string NewVersion { get; set; }

            [DataMember]
            public string WhatIsNew { get; set; }

            [DataMember]
            public bool IsMandatory { get; set; }
        }

        [DataContract]
        public class SendNotDeliveredResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public SendNotDeliveredResult()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class BringPickUpDataResult : DefaultResult
        {
            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public int OriginStationID { get; set; }

            [DataMember]
            public int DestinationStationID { get; set; }

            [DataMember]
            public double PiecesCount { get; set; }

            [DataMember]
            public double Weight { get; set; }
        }

        [DataContract]
        public class BringMyRouteShipmentsResult : DefaultResult
        {
            [DataMember]
            public int OrderNo { get; set; }

            [DataMember]
            public string ItemNo { get; set; }

            /// <summary>
            /// 1 - Deliver
            /// 2 - PickUp
            /// 3 - POD
            /// </summary>
            [DataMember]
            public int TypeID { get; set; }

            [DataMember]
            public string BillingType { get; set; }

            [DataMember]
            public double CODAmount { get; set; }

            [DataMember]
            public int DeliverySheetID { get; set; }

            [DataMember]
            public DateTime Date { get; set; }

            [DataMember]
            public DateTime ExpectedTime { get; set; }

            [DataMember]
            public string Latitude { get; set; }

            [DataMember]
            public string Longitude { get; set; }

            [DataMember]
            public int ClientID { get; set; }

            [DataMember]
            public string ClientName { get; set; }

            [DataMember]
            public string ClientFName { get; set; }

            [DataMember]
            public int ClientAddressID { get; set; }

            [DataMember]
            public string ClientAddressPhoneNumber { get; set; }

            [DataMember]
            public string ClientAddressFirstAddress { get; set; }

            [DataMember]
            public string ClientAddressSecondAddress { get; set; }

            [DataMember]
            public string ClientAddressLocation { get; set; }

            [DataMember]
            public int ClientContactID { get; set; }

            [DataMember]
            public string ClientContactName { get; set; }

            [DataMember]
            public string ClientContactFName { get; set; }

            [DataMember]
            public string ClientContactPhoneNumber { get; set; }

            [DataMember]
            public string ClientContactMobileNo { get; set; }

            [DataMember]
            public int ConsigneeID { get; set; }

            [DataMember]
            public string ConsigneeName { get; set; }

            [DataMember]
            public string ConsigneeFName { get; set; }

            [DataMember]
            public int ConsigneeDetailID { get; set; }

            [DataMember]
            public string ConsigneePhoneNumber { get; set; }

            [DataMember]
            public string ConsigneeFirstAddress { get; set; }

            [DataMember]
            public string ConsigneeSecondAddress { get; set; }

            [DataMember]
            public string ConsigneeNear { get; set; }

            [DataMember]
            public string ConsigneeMobile { get; set; }

            [DataMember]
            public string ConsigneeLatitude { get; set; }

            [DataMember]
            public string ConsigneeLongitude { get; set; }

            [DataMember]
            public string Origin { get; set; }

            [DataMember]
            public string Destination { get; set; }

            [DataMember]
            public bool PODNeeded { get; set; }

            [DataMember]
            public string PODDetail { get; set; }

            [DataMember]
            public string PODTypeCode { get; set; }

            [DataMember]
            public string PODTypeName { get; set; }


            public BringMyRouteShipmentsResult()
            {
                OrderNo = 0;
                Date = DateTime.Now;
                ExpectedTime = DateTime.Now;
                Latitude = "";
                Longitude = "";
                ConsigneeLatitude = "";
                ConsigneeLongitude = "";
                DeliverySheetID = 0;
                Origin = "";
                Destination = "";
            }
        }

        [DataContract]
        public class SendPickUpResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public SendPickUpResult()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class SendCheckPointResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public SendCheckPointResult()
            {
                IsSync = false;
            }
        }

        [DataContract]
        public class OnCLoadingForDeliverySheetResult : DefaultResult
        {
            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public bool IsSync { get; set; }

            public OnCLoadingForDeliverySheetResult()
            {
                IsSync = false;
            }
        }

        #endregion
    }
}