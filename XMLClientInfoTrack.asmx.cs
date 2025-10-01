using InfoTrack.BusinessLayer.DContext;
using InfoTrack.NaqelAPI;
using InfoTrack.NaqelAPI.App_Data;
using InfoTrack.NaqelAPI.GeneralClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Runtime.Serialization;
using InfoTrack.BusinessLayer.DContext;

namespace InfoTrack.NaqelAPI
{
    /// <summary>
    /// Summary description for XMLClientInfoTrack
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class XMLClientInfoTrack : System.Web.Services.WebService
    {
        InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDocument;
        //InfoTrack.NaqelAPI. ClientInfoTrackDataDataContext dcClientInfoTrack;

        [WebMethod]
        public ClientData CheckClientPassword(byte[] ID, int ClientContactID, string Password)
        {
            ClientData result = new NaqelAPI.ClientData();

            if (!GlobalVar.GV.IsSecure(ID)) return result;
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.ClientContacts.Where(P => P.ID == ClientContactID && P.StatusID != 3).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.ClientContact ClientContactInstance = dcMaster.ClientContacts.First(P => P.ID == ClientContactID && P.StatusID != 3);
                InfoTrack.BusinessLayer.DContext.ClientAddress ClientAddress = dcMaster.ClientAddresses.First(P => P.ID == ClientContactInstance.ClientAddressID && P.StatusID != 3);

                //if (ClientContactInstance.Password == GlobalVar.GV.security.Encrypt(Password))
                //if (ClientContactID == 68948)
                {
                    result.IsPasswordCorrect = true;
                    result.ClientContactID = ClientContactID;
                    result.ClientContactName = ClientContactInstance.Name;
                    result.ClientContactPhoneNumber = ClientContactInstance.PhoneNumber;
                    result.ClientContactMobileNo = ClientContactInstance.PhoneNumber;
                    result.ClientContactEmail = ClientContactInstance.Email;

                    InfoTrack.BusinessLayer.DContext.ClientAddress ClientAddressInstance = dcMaster.ClientAddresses.First(P => P.ID == ClientContactInstance.ClientAddressID);
                    result.ClientAddressID = ClientAddressInstance.ID;
                    result.ClientAddressFirstAddress = ClientAddressInstance.FirstAddress;
                    result.ClientAddressLocation = ClientAddressInstance.Location;
                    result.ClientAddressCityCode = GlobalVar.GV.GetCityCodeByCityID(ClientAddressInstance.CityID);
                    result.ClientAddressCountryCode = GlobalVar.GV.GetCountryCodeByCityID(ClientAddressInstance.CityID);
                    result.ClientAddressPhoneNo = ClientAddressInstance.PhoneNumber;

                    result.ClientID = ClientAddress.ClientID;
                }
            }

            return result;
        }

        [WebMethod]
        public Result CreateWaybill(byte[] ID, ManifestShipmentDetails ManifestShipmentDetailInstance, List<string> PiecesBarCodeList)
        {
            Result result = new Result();

            XMLShippingService instance = new XMLShippingService();
            if (ManifestShipmentDetailInstance.RefNo != null &&
                ManifestShipmentDetailInstance.RefNo != "")
            {
                //Check if this reference already exists in the system for this customer.
                dcDocument = new  DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (dcDocument.CustomerWayBills.Where(P => P.ClientID == ManifestShipmentDetailInstance.ClientInfo.ClientID &&
                                                     P.StatusID != 3 &&
                                                     P.RefNo == ManifestShipmentDetailInstance.RefNo).Count() > 0)
                {
                    CustomerWayBill instanceCustomerWayBill = dcDocument.CustomerWayBills.First(P => P.ClientID == ManifestShipmentDetailInstance.ClientInfo.ClientID &&
                                                     P.StatusID != 3 &&
                                                     P.RefNo == ManifestShipmentDetailInstance.RefNo);
                    result.HasError = true;
                    result.Message = "This waybill with RefNo " + ManifestShipmentDetailInstance.RefNo.ToString() + " already uploaded to our system, please check this waybill no :" + instanceCustomerWayBill.WayBillNo.ToString();
                }
                else
                    result = instance.CreateWaybillWithPiecesBarCode(ManifestShipmentDetailInstance, PiecesBarCodeList);
            }

            return result;
        }

        [WebMethod]
        public List<rpCustomerWaybillwtihPieceBarCode> GetCustomerWaybill(byte[] ID, ClientInformation ClientInfo, string PiecesBarCode, string RefNo, int WaybillNo,bool CreatePickUpRecord)
        {
            List<rpCustomerWaybillwtihPieceBarCode> result = new List<rpCustomerWaybillwtihPieceBarCode>();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (PiecesBarCode != "")
            {
                if (dcMaster.rpCustomerWaybillwtihPieceBarCodes.Where(P => P.CustomerPieceBarCode == PiecesBarCode).Count() > 0)
                    result.Add(dcMaster.rpCustomerWaybillwtihPieceBarCodes.First(P => P.CustomerPieceBarCode == PiecesBarCode));
            }
            else
                if (RefNo != "")
                {
                    if (dcMaster.rpCustomerWaybillwtihPieceBarCodes.Where(P => P.RefNo == RefNo).Count() > 0)
                        return dcMaster.rpCustomerWaybillwtihPieceBarCodes.Where(P => P.RefNo == RefNo).ToList();
                }
                else
                    if (WaybillNo > 0)
                    {
                        if (dcMaster.rpCustomerWaybillwtihPieceBarCodes.Where(P => P.WayBillNo == WaybillNo).Count() > 0)
                            return dcMaster.rpCustomerWaybillwtihPieceBarCodes.Where(P => P.WayBillNo == WaybillNo).ToList();
                    }

            return result;
        }

        //[WebMethod]
        //public Result CheckClientPassword(byte[] ID, int ClientID, string Password)
        //{
        //    Result result = new Result();
        //    if (!GlobalVar.GV.IsSecure(ID)) return result;
        //    dc = new DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    if (dc.APIClientAccesses.Where(P => P.ClientID == ClientID && P.StatusID == 1).Count() > 0)
        //    {
        //        APIClientAccess instance = dc.APIClientAccesses.First(P => P.ClientID == ClientID && P.StatusID == 1);
        //        InfoTrack.Common.Security security = new InfoTrack.Common.Security();
        //        if (instance.ClientPassword != security.Encrypt(Password))
        //        {
        //            result.HasError = true;
        //            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
        //            return result;
        //        }
        //    }
        //    else
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
        //        return result;
        //    }

        //    return result;
        //}

        //[WebMethod]
        //public Result CreateBooking(byte[] ID, ClientBoooking _ClientBoooking)
        //{
        //    Result result = new Result();
        //    if (!GlobalVar.GV.IsSecure(ID)) return result;
        //    result = _ClientBoooking.ClientInfo.CheckClientInfo(_ClientBoooking.ClientInfo, false);
        //    if (result.HasError)
        //        return result;

        //    //result = _ClientBoooking.CheckBookingValues(_ClientBoooking);
        //    //if (result.HasError)
        //    //    return result;

        //    _ClientBoooking.PickUpPoint = GlobalVar.GV.GetString(_ClientBoooking.PickUpPoint);
        //    _ClientBoooking.SpecialInstruction = GlobalVar.GV.GetString(_ClientBoooking.SpecialInstruction);
        //    _ClientBoooking.ContactPerson = GlobalVar.GV.GetString(_ClientBoooking.ContactPerson);
        //    _ClientBoooking.ContactNumber = GlobalVar.GV.GetString(_ClientBoooking.ContactNumber);

        //    InfoTrack.NaqelAPI. Booking NewBooking = new InfoTrack.NaqelAPI. Booking();
        //    NewBooking.RefNo = "";
        //    string lastRefNo = "";
        //    dc = new InfoTrack.NaqelAPI. DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    if (dc.Bookings.Where(P => P.RefNo.StartsWith(GlobalVar.GV.GetFromToday(GlobalVar.GV.GetCurrendDate()))).Count() > 0)
        //    {
        //        List<InfoTrack.NaqelAPI. Booking> bookinglist =
        //            dc.Bookings.Where(P => P.RefNo.StartsWith(GlobalVar.GV.GetFromToday(GlobalVar.GV.GetCurrendDate()))).ToList();
        //        lastRefNo = bookinglist[bookinglist.Count - 1].RefNo.ToString();
        //    }
        //    NewBooking.RefNo = GlobalVar.GV.GetBookingRefNo(lastRefNo, Convert.ToInt32(501), GlobalVar.GV.GetCurrendDate());

        //    if (_ClientBoooking.WaybillNo > 0)
        //        NewBooking.WaybillNo = _ClientBoooking.WaybillNo;

        //    NewBooking.ClientID = _ClientBoooking.ClientInfo.ClientID;
        //    NewBooking.ClientAddressID = _ClientBoooking.ClientInfo.ClientAddressID;
        //    NewBooking.ClientContactID = _ClientBoooking.ClientInfo.ClientContactID;
        //    NewBooking.BillingTypeID = Convert.ToInt32(_ClientBoooking.BillingType);
        //    NewBooking.BookingDate = GlobalVar.GV.GetCurrendDate();
        //    NewBooking.PickUpReqDT = _ClientBoooking.PickUpReqDT;
        //    NewBooking.PicesCount = _ClientBoooking.PicesCount;
        //    NewBooking.Weight = _ClientBoooking.Weight;
        //    NewBooking.Width = 1;
        //    NewBooking.Length = 1;
        //    NewBooking.Height = 1;

        //    NewBooking.PickUpPoint = _ClientBoooking.PickUpPoint;
        //    NewBooking.SpecialInstruction = _ClientBoooking.SpecialInstruction;

        //    NewBooking.OriginStationID = Convert.ToInt32(_ClientBoooking.OriginStationID);
        //    NewBooking.DestinationStationID = Convert.ToInt32(_ClientBoooking.DestinationStationID);
        //    NewBooking.RouteID = _ClientBoooking.ClientInfo.ClientAddress.RouteID;
        //    NewBooking.IsEmergency = false;
        //    NewBooking.IsInsurance = false;
        //    NewBooking.InsuranceValue = 0;
        //    NewBooking.InsuranceCost = 0;
        //    NewBooking.OfficeUpTo = _ClientBoooking.OfficeUpTo;
        //    NewBooking.IsPickedUp = false;
        //    NewBooking.IsCanceled = false;
        //    NewBooking.CourierInformed = false;
        //    NewBooking.CurrentStatusID = Convert.ToInt32(EnumList.BookingState.Booked);
        //    NewBooking.IsSpecialBooking = false;
        //    NewBooking.IsMissPickUp = false;
        //    NewBooking.Various = false;
        //    NewBooking.IsSync = false;
        //    NewBooking.ContactPerson = _ClientBoooking.ContactPerson;
        //    NewBooking.ContactNumber = _ClientBoooking.ContactNumber;
        //    NewBooking.LoadTypeID = Convert.ToInt32(EnumList.LoadType.Express);
        //    NewBooking.IsDGR = false;
        //    NewBooking.IsApproved = false;
        //    NewBooking.IsScheduleBooking = false;
        //    NewBooking.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
        //    NewBooking.IsShippingAPI = true;

        //    dc = new InfoTrack.NaqelAPI. DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    try
        //    {
        //        dc.Bookings.InsertOnSubmit(NewBooking);
        //        dc.SubmitChanges();
        //    }
        //    catch (Exception e)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErAnErrorDuringCreatingBooking");
        //        GlobalVar.GV.AddErrorMessage(e, _ClientBoooking.ClientInfo);
        //    }

        //    if (!result.HasError)
        //    {
        //        result.BookingRefNo = NewBooking.RefNo;
        //        result.Key = NewBooking.ID;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewBookingSuccess");
        //    }

        //    GlobalVar.GV.CreateShippingAPIRequest(_ClientBoooking.ClientInfo, EnumList.APIRequestType.Creating_New_Booking,
        //                                          NewBooking.RefNo.ToString(), result.Key);

        //    return result;
        //}

        //[WebMethod]
        //public  ClientData LoadMasterData(byte[] ID)
        //{
        //     ClientData result = new ClientData();
        //    if (!GlobalVar.GV.IsSecure(ID)) return result;

        //     ClientDataTableAdapters.CityTableAdapter cityAdapter = new  ClientDataTableAdapters.CityTableAdapter();
        //    cityAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    cityAdapter.Fill(result.City);

        //     ClientDataTableAdapters.StationTableAdapter stationAdapter = new  ClientDataTableAdapters.StationTableAdapter();
        //    stationAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    stationAdapter.Fill(result.Station);

        //     ClientDataTableAdapters.LoadTypeTableAdapter loadtypeAdapter = new  ClientDataTableAdapters.LoadTypeTableAdapter();
        //    loadtypeAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    loadtypeAdapter.Fill(result.LoadType);

        //     ClientDataTableAdapters.ODAStationTableAdapter odaStationAdapter = new  ClientDataTableAdapters.ODAStationTableAdapter();
        //    odaStationAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    odaStationAdapter.Fill(result.ODAStation);

        //    return result;
        //}
    }
}