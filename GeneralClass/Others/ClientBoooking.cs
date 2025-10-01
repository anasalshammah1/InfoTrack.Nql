using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass
{
    public class ClientBoooking
    {
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        private InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc;

        public ClientInformation ClientInfo = new ClientInformation();

        internal string RefNo = "";
        internal int ClientID = 0;
        internal int ClientAddressID = 0;
        internal int ClientContactID = 0;
        internal int BillingType;
        internal DateTime PickUpReqDT = new DateTime();
        internal float PicesCount = 0;
        internal float Weight = 0;
        internal string PickUpPoint = "";
        internal string SpecialInstruction = "";
        internal int OriginStationID = 0;
        internal int DestinationStationID = 0;
        internal bool IsEmergency = false;
        internal bool IsInsurance = false;
        internal float InsuranceValue = 0;
        internal float InsuranceCost = 0;
        internal DateTime OfficeUpTo = new DateTime();
        internal bool IsPickedUp = false;
        internal bool IsCanceled = false;
        internal bool CourierInformed = false;
        internal int CurrentStatusID = 1;
        internal bool IsSpecialBooking = false;

        internal string ContactPerson = "";
        internal string ContactNumber = "";
        internal int LoadTypeID = 0;
        internal int ServiceTypeID = 0;
        internal int WaybillNo = 0;

        //internal Result CheckBookingValues(BookingShipmentDetails _BookingShipmentDetails)
        //{
        //    Result result = new Result();

        //    if (BillingType <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
        //        return result;
        //    }

        //    if (!GlobalVar.GV.IsBillingCorrect(BillingType))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
        //        return result;
        //    }

        //    if (PickUpReqDateTime == null || PickUpReqDateTime.Year < 2014)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPickUpReqDateTime");
        //        return result;
        //    }

        //    if (PicesCount <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPicesCount");
        //        return result;
        //    }

        //    if (Weight <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongWeight");
        //        return result;
        //    }

        //    if (OriginStationID <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongOriginStationID");
        //        return result;
        //    }

        //    if (!GlobalVar.GV.IsStationCorrect(OriginStationID))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongOriginStationID");
        //        return result;
        //    }

        //    if (DestinationStationID <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongDestinationStationID");
        //        return result;
        //    }

        //    if (!GlobalVar.GV.IsStationCorrect(DestinationStationID))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongDestinationStationID");
        //        return result;
        //    }

        //    if (OfficeUpTo == null || OfficeUpTo.Year < 2014)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongOfficeUpTo");
        //        return result;
        //    }

        //    if (ContactPerson == "")
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongContactPerson");
        //        return result;
        //    }

        //    if (ContactNumber == "")
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongContactNumber");
        //        return result;
        //    }

        //    if (_BookingShipmentDetails.LoadTypeID <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
        //        return result;
        //    }

        //    if (!GlobalVar.GV.IsLoadTypeCorrect(_BookingShipmentDetails.ClientInfo, _BookingShipmentDetails.LoadTypeID))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
        //        return result;
        //    }

        //    ServiceTypeID = GlobalVar.GV.GetServiceTypeID(_BookingShipmentDetails.ClientInfo, LoadTypeID);

        //    result = _BookingShipmentDetails.ClientInfo.CheckClientInfo(true);
        //    if (result.HasError)
        //        return result;
            
        //    return result;
        //}

        //internal Result CheckBeforeCancellingBooking(ClientInformation ClientInfo, string RefNo, int Key, string CanceledReason)
        //{
        //    Result result = new Result();

        //    if (ClientInfo.CheckClientInfo(false).HasError)
        //        return result;

        //    if (RefNo == null || RefNo == "" || !GlobalVar.GV.IsNumber(RefNo))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErRefNo");
        //        return result;
        //    }

        //    if (Key <= 0)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErKey");
        //        return result;
        //    }

        //    if (CanceledReason == null || CanceledReason == "")
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCanceledReason");
        //        return result;
        //    }

        //    if (!IsBookingExist(ClientInfo, RefNo, Key))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErRefNo");
        //        return result;
        //    }

        //    if (GetBookingStatus(ClientInfo, RefNo, Key) != Convert.ToInt32(EnumList.BookingState.Booked))
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErBookingStatusChanged");
        //        return result;
        //    }

        //    return result;
        //}

        //internal bool IsBookingExist(ClientInformation clientinfo, string RefNo, int key)
        //{
        //    bool result = false;
        //    dcData = new  DataDataContext(GlobalVar.GV.GetERPNaqelConnection());
        //    if (dcData.Bookings.Where(P => P.ClientID == clientinfo.ClientID &&
        //                                   P.RefNo == RefNo &&
        //                                   P.ID == key).Count() > 0)
        //        result = true;
        //    return result;
        //}

        //internal int GetBookingStatus(ClientInformation clientinfo, string RefNo, int key)
        //{
        //    int result = 1;
        //    dcData = new  DataDataContext(GlobalVar.GV.GetERPNaqelConnection());
        //    if (IsBookingExist(clientinfo, RefNo, key))
        //        result = dcData.Bookings.First(P => P.ClientID == clientinfo.ClientID && P.RefNo == RefNo && P.ID == key).CurrentStatusID;
        //    return result;
        //}
    }
}