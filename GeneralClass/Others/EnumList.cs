using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class EnumList
    {
        public enum ServiceList : int
        {
            Document = 3,
            Express = 4,
            International = 6,
            International_Courier = 7,
            Domestic_Courier = 8
        }

        public enum LoadType : int
        {
            Express = 1,
            HW = 2,
            LTL = 3,
            Box = 4,
            Pallet = 7,
            Half_Load = 9,
            Full_Load = 10,
            C12D = 11,
            Drums = 29,
            Document = 35,
            Non_Document = 36,
            Express_Domestic = 39,
            HW_Domestic = 40,
            NAQEL_Express_Box_10_Kg = 41
        }

        internal enum BookingState : int
        {
            Booked = 1,
            Courier_Informed = 2,
            Cancel_Booking = 3,
            Scheduled = 4,
            Picked_Up = 5,
            Miss_PickUp = 6,
            Acknowledge = 7
        }

        internal enum APIRequestType : int
        {
            LoadMasterData = 1,
            Creating_New_Booking = 2,
            Cancel_Existing_Booking = 3,
            Trace_Shipment_By_WaybillNo = 4,
            Create_New_Shipment = 5,
            Trace_Shipment_By_RefNo = 6,
            Get_Shipment_Details_ByWaybillNo = 7,
            Get_Shipment_Details_ByRefNo = 8,
            CreateWaybillRange = 9,
            UpdateWaybill = 10,
            TraceByMultiWaybillNo = 11,
            MultiWayBillTracking = 20,
            MultiWayBillTrackingAllStaus = 21,
            TraceByMultiWaybillNoNewCheckPoints = 23,
            StickerPrinting = 22
        }

        internal enum ProductType : int
        {
            Retail_Outlet = 5,
            Home_Delivery = 6,
            Express = 7,
            IRS = 9
        }

        internal enum MethodType : int
        {
            CreateBooking = 1,
            CreateWaybill = 2,
            UpdateWaybill = 3,
            GetDeliveryServicePrice = 4,
            CreateWaybillAlt = 5,
            CreateWaybillForASR = 6,
            CreateWaybillForASRAlt = 7,
            GetLabelSticker = 8,
            CancelWaybill = 9,
            CancelRTO = 10,
            UpdateReweight = 11,
            SchedulWaybill = 12
        }

        public enum FileType : int
        {
            Claims = 1,
            DHLRequest = 2,
            DHLSuccessRespons = 3,
            DHLFailedRespons = 4,
            WaybillAttachments = 5,
            HHDFiles = 6,
            ArchiveFilesExported = 7,
            VehicleAccident = 8,
            ArchiveFilesImported = 9,
            SFDA = 10,
            DeliverySheetScan = 11
        }

        public enum PurposeList : int
        {
            SharingLocationSMS = 1,
            OCCSummary = 2,
            VehicleBreakDown = 3,
            EService = 4,
            QualityDepartmentMessages = 5,
            Ticketing = 6,
            GPSTracking = 7,
            Rating = 8,
            NoAnswer = 9,
            ReminderSharingLocationSMS = 10,
            RequestNotification = 11,
            OutForDelivery = 12,
            WaybillNotification = 13,
            advertisement = 14,
            RetailOutletRating = 15,
            ForgotPassword = 16,
            RefuseDelivery = 17,
            ExpectedOutForDeliveryEnglishSMS = 18,
            GiftVoucher = 19,
            FutureDeliverySMS = 20,
            OnHold = 21
        }

        
        public enum SMSSendingStatus : int
        {
            Success = 1,
            Invalid_UserName_and_Password = 10,
            Invalid_TagName_Format = 20,
            Invalid_TagName = 30,
            Insufficient_Fund = 40,
            Invalid_Recepient_Number_And_Replacement_List_Length = 50,
            Invalid_varsList_And_Replacement_List_Length = 51,
            Invalid_Mobile_Number = 60,
            System_Error = 70,
            Invalid_DateTime = 80,
            Serialization_Error = 90,
            Check_Mobile_No = 100,
            Sent = 101,
            Queued = 102,
            Rejected = 103,
            Failed = 104
        }

        //public enum BillingType : int
        //{
        //    On_Account = 1,
        //    Cash = 2,
        //    Cash_On_Delivery = 5
        //}

        //public enum StationList : int
        //{
        //    KSA_SHARORAH = 142,
        //    KSA_RABIG = 146,
        //    KSA_RIYADH = 501
        //}
    }
}