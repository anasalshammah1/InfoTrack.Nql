using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class BookingShipmentDetails
    {
        private InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDocment;

        public BookingShipmentDetails()
        {

        }

        public ClientInformation ClientInfo = new ClientInformation();
        public int BillingType;
        public DateTime PickUpReqDateTime;
        public int PicesCount = 0;
        public double Weight = 0;
        public string PickUpPoint = "";
        public string SpecialInstruction = "";

        public int OriginStationID;
        public int DestinationStationID;

        public DateTime OfficeUpTo;
        public string ContactPerson = "";
        public string ContactNumber = "";
        //public bool PickUpFromSeller = false;
        public int LoadTypeID = 0;
        internal int ServiceTypeID = 0;
        internal int ProductTypeID = 0;
        internal int WaybillNo = 0;

        internal Result CheckBookingValues(BookingShipmentDetails _BookingShipmentDetails)
        {
            Result result = new Result() { HasError = true };

            if (_BookingShipmentDetails.PickUpPoint.Trim().Length > 500)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("PickUpPoint cannot be empty and should be less than 500 letters.");
                return result;
            }

            if (_BookingShipmentDetails.SpecialInstruction.Trim().Length > 500)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("SpecialInstruction should be less than 500 letters.");
                return result;
            }

            if (PicesCount <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPicesCount");
                return result;
            }

            if (Weight <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongWeight");
                return result;
            }

            if (PickUpReqDateTime == null || PickUpReqDateTime.Date < DateTime.Now.Date)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPickUpReqDateTime");
                return result;
            }

            if (OfficeUpTo == null || OfficeUpTo.Date < DateTime.Now.Date)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongOfficeUpTo");
                return result;
            }

            if (string.IsNullOrWhiteSpace(ContactPerson) || ContactPerson.Trim().Length > 250)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongContactPerson");
                return result;
            }

            if (string.IsNullOrWhiteSpace(ContactNumber) || ContactNumber.Trim().Length > 50)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongContactNumber");
                return result;
            }

            if (BillingType <= 0 || !GlobalVar.GV.IsBillingCorrect(BillingType, _BookingShipmentDetails.ClientInfo.ClientID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return result;
            }

            if (OriginStationID <= 0 || !GlobalVar.GV.IsStationCorrect(OriginStationID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongOriginStationID");
                return result;
            }

            if (DestinationStationID <= 0 || !GlobalVar.GV.IsStationCorrect(DestinationStationID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongDestinationStationID");
                return result;
            }

            if (_BookingShipmentDetails.LoadTypeID <= 0 || !GlobalVar.GV.IsLoadTypeCorrect(_BookingShipmentDetails.ClientInfo, _BookingShipmentDetails.LoadTypeID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
                return result;
            }

            ServiceTypeID = GlobalVar.GV.GetServiceTypeID(_BookingShipmentDetails.ClientInfo, LoadTypeID);
            ProductTypeID = GlobalVar.GV.GetProductTypeID(_BookingShipmentDetails.ClientInfo, LoadTypeID);

            result.HasError = false;
            return result;
        }

        internal Result CheckBeforeCancellingBooking(ClientInformation ClientInfo, string RefNo, string CanceledReason, int? Key = 0)
        {
            Result result = new Result();

            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
                return result;

            if (RefNo == null || RefNo == "" || !GlobalVar.GV.IsNumber(RefNo))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErRefNo");
                return result;
            }

            if (Key < 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErKey");
                return result;
            }

            if (string.IsNullOrEmpty(CanceledReason))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCanceledReason");
                return result;
            }

            if (!IsBookingExist(ClientInfo, RefNo, Key))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErRefNo");
                return result;
            }

            if (GetBookingStatus(ClientInfo, RefNo, Key) != Convert.ToInt32(EnumList.BookingState.Booked))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErBookingStatusChanged");
                return result;
            }

            return result;
        }

        internal bool IsBookingExist(ClientInformation clientinfo, string RefNo, int? key = 0)
        {
            bool result = false;
            dcDocment = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (key.Value > 0
                && dcDocment.Bookings.Where(P => P.ClientID == clientinfo.ClientID && P.RefNo == RefNo && P.ID == key && P.IsCanceled == false).Any())
                return true;

            if (key.Value == 0
                && dcDocment.Bookings.Where(P => P.ClientID == clientinfo.ClientID && P.RefNo == RefNo && P.IsCanceled == false).Any())
                return true;

            return result;
        }

        internal int GetBookingStatus(ClientInformation clientinfo, string RefNo, int? key = 0)
        {
            int result = 1;
            dcDocment = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (IsBookingExist(clientinfo, RefNo, key) && key.Value > 0)
                result = dcDocment.Bookings.First(P => P.ClientID == clientinfo.ClientID && P.RefNo == RefNo && P.ID == key && P.IsCanceled == false).CurrentStatusID;

            if (IsBookingExist(clientinfo, RefNo, key) && key.Value == 0)
                result = dcDocment.Bookings.First(P => P.ClientID == clientinfo.ClientID && P.RefNo == RefNo && P.IsCanceled == false).CurrentStatusID;
            return result;
        }
    }
}