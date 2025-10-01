using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;
using DevExpress.XtraReports.UI;
using InfoTrack.BusinessLayer.DContext;
using InfoTrack.NaqelAPI.Class;
using InfoTrack.NaqelAPI.GeneralClass.Others;
using System.Data.SqlClient;
using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using Dapper.Contrib.Ext;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Text;
using InfoTrack.NaqelAPI.BusinessObjects;
using DevExpress.Pdf;
using RestSharp;
using Newtonsoft.Json;
using System.Net.Http;
using DevExpress.XtraRichEdit.Model;
using System.Transactions;
using System.Threading;
using DevExpress.XtraReports.Serialization;
using System.Drawing;

namespace InfoTrack.NaqelAPI
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class XMLShippingService : System.Web.Services.WebService
    {
        InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dc;
        InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        InfoTrack.BusinessLayer.DContext.HHDDataContext dcHHD;
        string sqlCon = System.Configuration.ConfigurationManager.ConnectionStrings["DapperConnectionString"].ConnectionString;
        int WaybillNo = 0;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [WebMethod(Description = "You can use this function to create booking for a new pickup.")]
        public Result CreateBooking(BookingShipmentDetails _BookingShipmentDetail)
        {
            Result result = new Result();
            #region Check basic data
            // to check if clientcontactId/clientaddressid == 0
            bool needToCheckClientDetail = _BookingShipmentDetail.ClientInfo.ClientAddressID == 0 || _BookingShipmentDetail.ClientInfo.ClientContactID == 0;
            result = _BookingShipmentDetail.ClientInfo.CheckClientInfo(_BookingShipmentDetail.ClientInfo, needToCheckClientDetail);
            if (result.HasError)
                return result;

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            bool ClientHasCreateBookingPermit = dcMaster.APIClientAccesses
                    .Where(P => P.ClientID == _BookingShipmentDetail.ClientInfo.ClientID
                    && P.StatusID == 1 && P.IsCreateBooking == true)
                    .Any();

            if (!ClientHasCreateBookingPermit)
            {
                result.HasError = true;
                result.Message = "Your account has no permission to create booking, please contact Naqel team for further operation.";
                return result;
            }

            result = _BookingShipmentDetail.CheckBookingValues(_BookingShipmentDetail);
            if (result.HasError)
            {
                WritetoXML(_BookingShipmentDetail, _BookingShipmentDetail.ClientInfo, "New Booking", EnumList.MethodType.CreateBooking, result);
                return result;
            }
            #endregion

            // Check if any existing bookings
            var existingBookingRefNo = GetClientBookingRefNoToday(_BookingShipmentDetail.ClientInfo, _BookingShipmentDetail.PickUpPoint, _BookingShipmentDetail.PickUpReqDateTime, _BookingShipmentDetail.OriginStationID);
            if (existingBookingRefNo != "")
            {
                var existingBooking = dc.Bookings
                    .Where(b => b.ClientID == _BookingShipmentDetail.ClientInfo.ClientID
                    && b.RefNo == existingBookingRefNo
                    && b.IsCanceled == false)
                    .FirstOrDefault();

                if (existingBooking != null)
                {
                    result.HasError = false;
                    result.Key = existingBooking.ID;
                    result.BookingRefNo = existingBooking.RefNo;
                    result.WaybillNo = (int)existingBooking.WaybillNo;
                    result.Message = "";
                    return result;
                }
            }

            bool isCreateBookingNextday = IsCreateBookingNextDay(_BookingShipmentDetail.ClientInfo.ClientID);
            DateTime crntDt = GlobalVar.GV.GetCurrendDate();
            string refNoBookingDate = crntDt.ToString("yyMMdd");
            string lastRefNo = "", newRefNo = "";

            var lastBooking = dc.Bookings.Where(b => b.RefNo.StartsWith(refNoBookingDate)).OrderByDescending(b => b.ID).FirstOrDefault();
            if (lastBooking != null)
                lastRefNo = lastBooking.RefNo;

            newRefNo = GlobalVar.GV.GetBookingRefNo(lastRefNo, _BookingShipmentDetail.OriginStationID, crntDt); // 2nd parameter need to check (Origin station ID)

            Booking NewBooking = new Booking
            {
                RefNo = newRefNo,
                ClientID = _BookingShipmentDetail.ClientInfo.ClientID,
                ClientAddressID = _BookingShipmentDetail.ClientInfo.ClientAddressID,
                ClientContactID = _BookingShipmentDetail.ClientInfo.ClientContactID,
                BillingTypeID = _BookingShipmentDetail.BillingType,
                BookingDate = isCreateBookingNextday ? crntDt.AddDays(1) : crntDt,
                PickUpReqDT = _BookingShipmentDetail.PickUpReqDateTime.Date < (isCreateBookingNextday ? crntDt.AddDays(1) : crntDt).Date
                            ? _BookingShipmentDetail.PickUpReqDateTime.AddDays(1) : _BookingShipmentDetail.PickUpReqDateTime,
                PicesCount = _BookingShipmentDetail.PicesCount,
                Weight = Math.Round(_BookingShipmentDetail.Weight, 2),
                PickUpPoint = string.IsNullOrEmpty(_BookingShipmentDetail.ClientInfo.ClientAddress.ShipperName) ? _BookingShipmentDetail.PickUpPoint : _BookingShipmentDetail.ClientInfo.ClientAddress.ShipperName,
                SpecialInstruction = (isCreateBookingNextday ? "[+1] " : "") + _BookingShipmentDetail.SpecialInstruction,
                OriginStationID = _BookingShipmentDetail.OriginStationID,
                DestinationStationID = _BookingShipmentDetail.DestinationStationID,
                RouteID = _BookingShipmentDetail.ClientInfo.ClientAddress.RouteID,
                OfficeUpTo = _BookingShipmentDetail.OfficeUpTo,
                ContactPerson = _BookingShipmentDetail.ContactPerson,
                ContactNumber = _BookingShipmentDetail.ContactNumber,
                LoadTypeID = _BookingShipmentDetail.LoadTypeID,
                ProductTypeID = _BookingShipmentDetail.ProductTypeID,
                WaybillNo = _BookingShipmentDetail.WaybillNo,
                Width = 1,
                Length = 1,
                Height = 1,
                IsEmergency = false,
                IsInsurance = false,
                InsuranceValue = 0,
                InsuranceCost = 0,
                IsPickedUp = false,
                IsCanceled = false,
                CourierInformed = false,
                CurrentStatusID = (int)EnumList.BookingState.Booked,
                IsSpecialBooking = false,
                IsMissPickUp = false,
                Various = false,
                IsSync = false,
                IsDGR = false,
                IsApproved = false,
                IsScheduleBooking = false,
                IsShippingAPI = true,
                SourceID = 13
            };

            try
            {
                dc.Bookings.InsertOnSubmit(NewBooking);
                dc.SubmitChanges();
            }
            catch (Exception e)
            {
                LogException(e);
                WritetoXML(_BookingShipmentDetail, _BookingShipmentDetail.ClientInfo, "New Booking", EnumList.MethodType.CreateBooking, result);
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErAnErrorDuringCreatingBooking");
                GlobalVar.GV.AddErrorMessage(e, _BookingShipmentDetail.ClientInfo);
            }

            if (!result.HasError)
            {
                result.BookingRefNo = GlobalVar.GV.GetString(NewBooking.RefNo, 20);
                result.Key = NewBooking.ID;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewBookingSuccess");
            }

            GlobalVar.GV.CreateShippingAPIRequest(_BookingShipmentDetail.ClientInfo, EnumList.APIRequestType.Creating_New_Booking,
                                                  NewBooking.RefNo.ToString(), result.Key);
            return result;
        }

        [WebMethod(Description = "You can use this function to cancel the booking which you do it before.")]
        public Result CancelBooking(ClientInformation ClientInfo, string RefNo, string CancelReason, int? BookingKey = 0)
        {
            Result result = new Result();
            BookingShipmentDetails _BookingShipmentDetail = new BookingShipmentDetails();
            BookingKey = BookingKey ?? 0;
            result = _BookingShipmentDetail.CheckBeforeCancellingBooking(ClientInfo, RefNo, CancelReason, BookingKey);
            if (result.HasError)
                return result;

            CancelReason = GlobalVar.GV.GetString(CancelReason, 200);
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            Booking instance;
            if (BookingKey > 0)
                instance = dc.Bookings.First(P => P.ClientID == ClientInfo.ClientID && P.RefNo == RefNo && P.ID == BookingKey && P.IsCanceled == false);
            else
                instance = dc.Bookings.First(P => P.ClientID == ClientInfo.ClientID && P.RefNo == RefNo && P.IsCanceled == false);

            instance.IsCanceled = true;
            instance.CurrentStatusID = Convert.ToInt32(EnumList.BookingState.Cancel_Booking);
            instance.CanceledReason = CancelReason;

            try { dc.SubmitChanges(); }
            catch (Exception e)
            {
                LogException(e);
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErAnErrorDuringCancellingBooking");
                GlobalVar.GV.AddErrorMessage(e, ClientInfo);
            }

            if (!result.HasError)
            {
                result.HasError = false;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("BookingCanceledSuccessfully");
            }

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Cancel_Existing_Booking, RefNo, BookingKey);

            return result;
        }

        //public class GeoReverse
        //{
        //    public string Status { get; set; }
        //    public List<GeoAddressComp> Results { get; set; }
        //}

        //public class GeoAddressComp
        //{
        //    public string Formatted_address { get; set; }
        //}



        //private string GetNewLocation(string LatLng)
        //{
        //    var client = new RestClient("https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyALzDHbmpFmqHW_b8szroc2EzEtH05k1yc&latlng=" + LatLng);
        //    client.Timeout = -1;
        //    var request = new RestRequest(Method.GET);
        //    IRestResponse response = client.Execute(request);
        //    var tt = JsonConvert.DeserializeObject<GeoReverse>(response.Content);

        //    if (tt.Status == "OK" && tt.Results.Count() > 0)
        //        return tt.Results.First().Formatted_address;

        //    return "";
        //}

        [WebMethod(Description = "You can use this function to create a new waybill in the system.")]
        public Result CreateWaybill(ManifestShipmentDetails _ManifestShipmentDetails)
        {
            //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Start.");
            WritetoXMLUpdateWaybill(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill);
            //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Raw Data Saved.");
            Result result = new Result();
            try
            {
                dcMaster = new MastersDataContext();
                var tempLmClient = dcMaster.APILmSetups
                    .Where(p => p.ClientId == _ManifestShipmentDetails.ClientInfo.ClientID
                        && p.LoadTypeID == _ManifestShipmentDetails.LoadTypeID
                        && p.StatusID == 1)
                    .FirstOrDefault();

                if (tempLmClient != null)
                {
                    _ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode = tempLmClient.OrgCityCode;
                    _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode = tempLmClient.OrgCountryCode;
                }

                UpdateCountryCode(_ManifestShipmentDetails, "SA", "KSA");
                ValidateDDUClient(_ManifestShipmentDetails);

                #region LB & IQ CODCharge decimal validation / Done by Sara Almalki
                if (!ValidateCODChargeForLebanon(_ManifestShipmentDetails, ref result)) return result;
                if (!ValidateCODChargeForIraq(_ManifestShipmentDetails, ref result)) return result;
                if (!ValidateCODChargeForMorocco(_ManifestShipmentDetails, ref result)) return result;
                if (!ValidateClientIDForCOD(_ManifestShipmentDetails, ref result)) return result;

                // 9022477	Saudi Post Last mile (SPL)      9026333	SPL- Express Plus 
                // 203	SPL Retail Outlet Delivery
                if (_ManifestShipmentDetails.ClientInfo.ClientID == 9022477 && _ManifestShipmentDetails.LoadTypeID == 203)
                {
                    _ManifestShipmentDetails.ClientInfo.ClientID = 9026333;
                }
                #endregion

                #region SPL lastmile account convert
                var doc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (doc.APIClientAndSubClients
                    .Where(p => p.cClientID == _ManifestShipmentDetails.ClientInfo.ClientID && p.StatusId == 1 && p.pClientID == 9019722)
                    .Count() > 0)
                    _ManifestShipmentDetails.ClientInfo.ClientID = 9019722;

                if (_ManifestShipmentDetails.ClientInfo.ClientID == 9022477 && _ManifestShipmentDetails.LoadTypeID == 116)
                {
                    _ManifestShipmentDetails.ClientInfo.ClientID = 9027665;
                    _ManifestShipmentDetails.CreateBooking = true;
                }
                // for SPL testing 
                //if (_ManifestShipmentDetails.ClientInfo.ClientID == 9020077 && _ManifestShipmentDetails.LoadTypeID == 56)
                //{
                //    _ManifestShipmentDetails.CreateBooking = true;
                //}
                else if (doc.APIClientAndSubClients.Where(p => p.pClientID == _ManifestShipmentDetails.ClientInfo.ClientID).Count() > 0)
                {
                    var subClient = doc.APIClientAndSubClients.FirstOrDefault(p => p.pClientID == _ManifestShipmentDetails.ClientInfo.ClientID
                    && p.DestCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CountryCode))
                    && p.OrgCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode))
                    && p.StatusId == 1);
                    if (subClient != null)
                    {
                        _ManifestShipmentDetails.ClientInfo.ClientID = subClient.cClientID;
                        if (!subClient.isASR && subClient.LoadTypeID != null)
                        {
                            _ManifestShipmentDetails.LoadTypeID = Convert.ToInt32(subClient.LoadTypeID);
                        }
                    }
                }
                #endregion

                #region Data Validation
                // Check create booking permission
                bool ClientHasCreateBookingPermit = dcMaster.APIClientAccesses
                        .Where(P => P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID && P.StatusID == 1 && P.IsCreateBooking == true)
                        .Any();


                string B2BAccount = System.Configuration.ConfigurationManager.AppSettings["NeedBooking"].ToString();
                List<int> B2BAccountclientid = B2BAccount.Split(',').Select(Int32.Parse).ToList();

                if (B2BAccountclientid.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
                {
                    _ManifestShipmentDetails.CreateBooking = true;
                }

                if (_ManifestShipmentDetails.CreateBooking == true && !ClientHasCreateBookingPermit)
                {
                    result.HasError = true;
                    result.Message = "Your account has no permission to create booking, please contact Naqel team for further operation.";
                    return result;
                }

                // Check loadtype agreement
                CheckClientLoadType(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.LoadTypeID, ref result);
                if (result.HasError)
                    return result;

                //result = CheckCorrectDSCountry(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.LoadTypeID, Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CountryCode)));
                //if (result.HasError)
                //{
                //    return result;

                //}

                var tempClientID = _ManifestShipmentDetails.ClientInfo.ClientID;
                int tempPharmaClientID = GetPharmaClientID(tempClientID);

                int tempServiceTypeID = GlobalVar.GV.GetServiceTypeID(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.LoadTypeID);
                //bool IsCourierLoadType = (tempServiceTypeID == 7 || tempServiceTypeID == 8); //false
                bool IsCourierLoadType = false;
                string courierLoadTypes = System.Configuration.ConfigurationManager.AppSettings["CourierLoadTypes"].ToString();
                List<int> _courierLoadTypes = courierLoadTypes.Split(',').Select(Int32.Parse).ToList();
                IsCourierLoadType = _courierLoadTypes.Contains(_ManifestShipmentDetails.LoadTypeID);

                result = _ManifestShipmentDetails.ClientInfo.CheckClientInfo(_ManifestShipmentDetails.ClientInfo, true, tempPharmaClientID, IsCourierLoadType);
                if (result.HasError)
                {
                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                    return result;
                }

                result = _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeInfo(_ManifestShipmentDetails.ConsigneeInfo, _ManifestShipmentDetails.ClientInfo, IsCourierLoadType);
                if (result.HasError)
                {
                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                    return result;
                }

                result = _ManifestShipmentDetails.IsWaybillDetailsValid(_ManifestShipmentDetails, tempPharmaClientID, IsCourierLoadType);
                if (result.HasError)
                {
                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                    return result;
                }

                DocumentDataDataContext dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                #region Check NationalID for High Value DV shipments
                // _ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalIdExpiry = "2023-12-02";
                Regex regNationalIdExpiry = new Regex(@"^\d{4}-((0\d)|(1[012]))-(([012]\d)|3[01]$)");
                if (!string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalIdExpiry)
                    && !regNationalIdExpiry.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalIdExpiry))
                {
                    result.HasError = true;
                    result.Message = "Please Enter the date Format as YYYY-MM-DD in ConsigneeNationalIdExpiry feild";
                    return result;
                }

                Regex regConsigneeBirthDate = new Regex(@"^\d{4}-((0\d)|(1[012]))-(([012]\d)|3[01]$)");
                if (!string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.ConsigneeBirthDate)
                    && !regConsigneeBirthDate.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.ConsigneeBirthDate))
                {
                    result.HasError = true;
                    result.Message = "Please Enter the date Format as YYYY-MM-DD in ConsigneeBirthDate feild";
                    return result;
                }

                Regex regWhat3Words = new Regex(@"^\/\/\/[a-z]+\.[a-z]+\.[a-z]+$");
                if (!regWhat3Words.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.What3Words)
                    && !string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.What3Words))
                {
                    result.HasError = true;
                    result.Message = "Please Enter What3Word Format as ///***.****.**** ";
                    return result;
                }

                // International Packages to KSA need to provide consignee info for high DV shipments
                if (_ManifestShipmentDetails.ConsigneeInfo.CountryCode == "KSA"
                    && _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode != _ManifestShipmentDetails.ConsigneeInfo.CountryCode)
                {
                    var cur = dc.Currencies.FirstOrDefault(p => p.ID == _ManifestShipmentDetails.CurrenyID);
                    if (cur == null)
                    {
                        result.HasError = true;
                        result.Message = "Invalid Declared Value CurrencyID.";
                        return result;
                    }
                    double HighValueDV = _ManifestShipmentDetails.DeclareValue / cur.ExchangeRate;

                    if (HighValueDV > 266.67)
                    {
                        string list3 = System.Configuration.ConfigurationManager.AppSettings["OptionalConsigneeNationalIDList"].ToString();
                        List<int> ClientValidation = list3.Split(',').Select(Int32.Parse).ToList();

                        // LB consignee no need check NationalID
                        if (GlobalVar.GV.consigneeID_Validation(_ManifestShipmentDetails.ClientInfo.ClientID))
                        {
                            bool IsNationalIDValid = false;
                            bool IsPassportValid = false;

                            // Valid NationalID / Passport (https://en.wikipedia.org/wiki/Machine-readable_passport)
                            string tempConsigneeNationalID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalID.ToString();
                            if (tempConsigneeNationalID.Length == 10)
                            {
                                IsNationalIDValid = true;
                            }

                            // As Gateways team confirmation: For passport we need to accept Letters & Numbers. minimum 6 digits maximum 15 digits.  
                            Regex regPassport = new Regex(@"^[\dA-Z]{6,15}$");
                            // Expire date format should be 20**-**-** [yyyy-MM-dd]
                            Regex regPassportExp = new Regex(@"^(?<year>20\d\d)-(?<month>0[1-9]|1[012])-(?<day>0[1-9]|[12][0-9]|3[01])$");
                            DateTime foundDate;
                            if (regPassport.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.ConsigneePassportNo)
                                && Regex.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationality.Trim(), @"^[a-zA-Z\s]{3,100}$"))
                            {
                                Match matchResult = regPassportExp.Match(_ManifestShipmentDetails.ConsigneeInfo.ConsigneePassportExp);
                                if (matchResult.Success)
                                {
                                    int year = int.Parse(matchResult.Groups["year"].Value);
                                    int month = int.Parse(matchResult.Groups["month"].Value);
                                    int day = int.Parse(matchResult.Groups["day"].Value);

                                    if (year > DateTime.Now.Year
                                        || (year == DateTime.Now.Year && month > DateTime.Now.Month)
                                        || (year == DateTime.Now.Year && month == DateTime.Now.Month && day > DateTime.Now.Day))
                                    {
                                        try
                                        {
                                            foundDate = new DateTime(year, month, day);
                                            IsPassportValid = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            LogException(ex);
                                            // Invalid date
                                            // Console.WriteLine("Invalid date");
                                        }
                                    }
                                }
                            }

                            if (!IsNationalIDValid && !IsPassportValid)
                            {
                                result.HasError = true;
                                //result.Message = "Error happend while saving ConsigneeNationalID, Please insert a valid ID";
                                result.Message = "Invalid Consignee NationalID or Passport information.";
                                return result;
                            }
                        }

                    }
                }

                // Check DV Limit
                if (_ManifestShipmentDetails.ConsigneeInfo.CountryCode == "KSA"
                    && _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode != _ManifestShipmentDetails.ConsigneeInfo.CountryCode)
                {
                    Result res = new Result();

                    //normal shipments min DV is 5 USD(18 SAR), document shipments min DV is 0.5 SAR.

                    string list1 = System.Configuration.ConfigurationManager.AppSettings["DeclareValueNoMinLimitValidation"].ToString();
                    List<int> _clientid1 = list1.Split(',').Select(Int32.Parse).ToList();
                    // Only KSA shipments require check min DV limit
                    if (!_clientid1.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
                    {
                        double tempDVLimit = GetDVLimit(_ManifestShipmentDetails.CurrenyID, _ManifestShipmentDetails.LoadTypeID);
                        if (tempDVLimit == 0)
                        {
                            res.HasError = true;
                            res.Message = "Invaild Declare Value Curreny ID";
                            return res;
                        }

                        if (_ManifestShipmentDetails.DeclareValue < tempDVLimit)
                        {
                            if (_ManifestShipmentDetails.DeclareValue >= 1 && _ManifestShipmentDetails.ClientInfo.ClientID == 9019912)
                            {
                                // Shein min DV = 1 USD
                            }
                            else if (_ManifestShipmentDetails.DeclareValue >= 2 && _ManifestShipmentDetails.LoadTypeID == 65)
                            {
                                //IRC min DV = 2 USD 
                            }
                            else
                            {
                                WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                                res.HasError = true;
                                res.Message = "Please Check The Value Of Declare Value it's Too Low .";
                                return res;
                            }
                        }
                    }
                }
                #endregion

                #region Check commercial invoice
                if (_ManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0)
                {
                    Result res = new Result();
                    //_ManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
                    res = IsCommericalInvoiceValidBeforeWaybill(_ManifestShipmentDetails._CommercialInvoice, _ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.ConsigneeInfo.CountryCode, _ManifestShipmentDetails.DeclareValue, _ManifestShipmentDetails.CurrenyID);

                    if (res.HasError)
                    {
                        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, res);
                        res.HasError = true;
                        res.Message = "an error happened when saving the Commerical Invoice, please make sure to pass valid values:\n " + res.Message;
                        return res;
                    }

                    string Hs_valid = System.Configuration.ConfigurationManager.AppSettings["HSCode_Validation"].ToString();
                    List<int> Hs_validList = Hs_valid.Split(',').Select(Int32.Parse).ToList();

                    foreach (var _commercialInvoice in _ManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList)
                    {
                        if (Hs_validList.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
                        {
                            _commercialInvoice.CustomsCommodityCode = GlobalVar.GV.BirkenHSCode(_commercialInvoice.CustomsCommodityCode);
                        }
                    }
                }
                #endregion

                _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeData(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.ConsigneeInfo, IsCourierLoadType);
                if (_ManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID == 0 || _ManifestShipmentDetails.ConsigneeInfo.ConsigneeID == 0)
                {
                    result.HasError = true;
                    result.Message = "Error happend while saving Consignee Info, please insert valid data.. ";
                    return result;
                }

                // Incoterm VS IsCustomsDutyPayByConsignee : commented as it cause errors to current client 

                //if (_ManifestShipmentDetails.IsCustomDutyPayByConsignee == true && _ManifestShipmentDetails.Incoterm.ToLower() == "ddp")
                //{
                //    result.HasError = true;
                //    result.Message = "IsCustomsDutyPayByConsignee value should be false for DDP incoterm ";
                //    return result;
                //}
                //if (_ManifestShipmentDetails.IsCustomDutyPayByConsignee == false && _ManifestShipmentDetails.Incoterm.ToLower() == "ddu")
                //    {
                //    result.HasError = true;
                //    result.Message = "IsCustomsDutyPayByConsignee value should be true for DDU incoterm ";
                //    return result;
                //}
                #endregion

                #region Check existing Waybill record
                string list = System.Configuration.ConfigurationManager.AppSettings["NoCheckRefNoClientIDs"].ToString();
                List<int> _clientid = list.Split(',').Select(Int32.Parse).ToList();

                if (!string.IsNullOrWhiteSpace(_ManifestShipmentDetails.RefNo) && !_clientid.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
                {
                    List<ForwardWaybillInfo> waybillInfos = CheckExistingForwardWaybill(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, _ManifestShipmentDetails.LoadTypeID);
                    if (waybillInfos.Count() > 0)
                    {
                        ForwardWaybillInfo waybillInfo = waybillInfos[0];
                        result.WaybillNo = waybillInfo.WaybillNo;
                        result.BookingRefNo = waybillInfo.BookingRefNo;
                        result.Key = waybillInfo.ID;
                        result.HasError = false;
                        result.Message = "Waybill already generated with RefNo: " + _ManifestShipmentDetails.RefNo;
                        return result;
                    }
                }
                #endregion

                //checkin
                //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Date Validation Done.");
                #region Prepare new waybill data
                _ManifestShipmentDetails.DeliveryInstruction = GlobalVar.GV.GetString(_ManifestShipmentDetails.DeliveryInstruction, 200);
                CustomerWayBill NewWaybill = new CustomerWayBill();
                NewWaybill.ClientID = tempPharmaClientID == 0 ? _ManifestShipmentDetails.ClientInfo.ClientID : tempPharmaClientID;
                NewWaybill.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
                NewWaybill.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;
                NewWaybill.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
                NewWaybill.ServiceTypeID = _ManifestShipmentDetails.ServiceTypeID;
                NewWaybill.BillingTypeID = _ManifestShipmentDetails.BillingType;
                NewWaybill.IsCOD = _ManifestShipmentDetails.BillingType == 5 || _ManifestShipmentDetails.CODCharge > 0;
                NewWaybill.ConsigneeID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeID;
                NewWaybill.ConsigneeAddressID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID;
                NewWaybill.OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode, IsCourierLoadType), IsCourierLoadType);
                NewWaybill.DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode, _ManifestShipmentDetails.ConsigneeInfo.CountryCode, IsCourierLoadType), IsCourierLoadType);
                NewWaybill.OriginCityCode = GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode, IsCourierLoadType).ToString();
                NewWaybill.DestinationCityCode = GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode, _ManifestShipmentDetails.ConsigneeInfo.CountryCode, IsCourierLoadType).ToString();
                NewWaybill.CODCurrencyID = dc.Currencies.FirstOrDefault(p => p.CountryID == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CountryCode))).ID;
                NewWaybill.PickUpDate = DateTime.Now;
                NewWaybill.PicesCount = _ManifestShipmentDetails.PicesCount;
                NewWaybill.PromisedDeliveryDateFrom = _ManifestShipmentDetails.PromisedDeliveryDateFrom;
                NewWaybill.PromisedDeliveryDateTo = _ManifestShipmentDetails.PromisedDeliveryDateTo;

                //if (GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).HasValue)
                //    NewWaybill.ODADestinationID = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).Value;
                var odaStationId = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode);

                if (odaStationId.HasValue)
                    NewWaybill.ODADestinationID = odaStationId.Value;

                //NewWaybill.ODAOriginID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode));
                if (NewWaybill.ODADestinationID == 1237 && NewWaybill.ServiceTypeID == 4 && (NewWaybill.DestinationCityCode == "26"))//
                {
                    NewWaybill.ODADestinationID = null;
                }
                NewWaybill.InsuredValue = _ManifestShipmentDetails.InsuredValue;
                NewWaybill.DeclaredValue = Math.Round(_ManifestShipmentDetails.DeclareValue, 2);
                if (_ManifestShipmentDetails.ServiceTypeID == 4 && NewWaybill.DeclaredValue == 0)
                {
                    NewWaybill.DeclaredValue = _ManifestShipmentDetails.InsuredValue;
                }
                NewWaybill.IsInsurance = _ManifestShipmentDetails.InsuredValue > 0;
                NewWaybill.Weight = _ManifestShipmentDetails.Weight < 0.1 ? 0.1 : Math.Round(_ManifestShipmentDetails.Weight, 2);
                NewWaybill.Width = _ManifestShipmentDetails.Width;
                NewWaybill.Length = _ManifestShipmentDetails.Length;
                NewWaybill.Height = _ManifestShipmentDetails.Height;
                NewWaybill.VolumeWeight = Math.Round(_ManifestShipmentDetails.VolumetricWeight, 2);
                NewWaybill.BookingRefNo = "";
                NewWaybill.ManifestedTime = DateTime.Now;
                NewWaybill.GoodDesc = _ManifestShipmentDetails.GoodDesc;
                NewWaybill.Incoterm = _ManifestShipmentDetails.Incoterm;
                NewWaybill.IncotermID = GlobalVar.GV.GetIncotermID(_ManifestShipmentDetails.Incoterm);

                // NewWaybill.IsCustomDutyPayByConsignee = NewWaybill.IncotermID == 1 || _ManifestShipmentDetails.IsCustomDutyPayByConsignee;
                if (NewWaybill.IncotermID == 1)
                {
                    NewWaybill.IsCustomDutyPayByConsignee = true;
                }
                else
                {
                    NewWaybill.IsCustomDutyPayByConsignee = false;
                }

                NewWaybill.IncotermsPlaceAndNotes = _ManifestShipmentDetails.IncotermsPlaceAndNotes;

                NewWaybill.Latitude = _ManifestShipmentDetails.Latitude;
                NewWaybill.Longitude = _ManifestShipmentDetails.Longitude;
                NewWaybill.RefNo = GlobalVar.GV.GetString(_ManifestShipmentDetails.RefNo, 100);
                NewWaybill.IsPrintBarcode = false;
                NewWaybill.StatusID = 1;
                NewWaybill.PODDetail = "";
                NewWaybill.DeliveryInstruction = _ManifestShipmentDetails.DeliveryInstruction;
                NewWaybill.CODCharge = Math.Round(_ManifestShipmentDetails.CODCharge, 2);
                NewWaybill.Discount = 0;
                NewWaybill.NetCharge = 0;
                NewWaybill.OnAccount = 0;
                NewWaybill.ServiceCharge = 0;
                NewWaybill.ODAStationCharge = 0;
                NewWaybill.OtherCharge = 0;
                NewWaybill.PaidAmount = 0;
                NewWaybill.SpecialCharge = 0;
                NewWaybill.StandardShipment = 0;
                NewWaybill.StorageCharge = 0;
                int producttypeID = 0;
                dcMaster = new MastersDataContext();
                producttypeID = (int)dcMaster.LoadTypes
                                     .Where(LT => LT.ID == _ManifestShipmentDetails.LoadTypeID)
                                     .Select(LT => LT.ProductTypeID)
                                     .FirstOrDefault();
                NewWaybill.ProductTypeID = producttypeID;// Convert.ToInt32(EnumList.ProductType.Home_Delivery); 
                NewWaybill.IsShippingAPI = true;
                NewWaybill.PODTypeID = null;
                NewWaybill.PODDetail = "";
                NewWaybill.IsRTO = _ManifestShipmentDetails.ClientInfo.ClientID == 1024600 && _ManifestShipmentDetails.isRTO;
                NewWaybill.IsManifested = false;
                NewWaybill.GoodsVATAmount = _ManifestShipmentDetails.GoodsVATAmount;
                NewWaybill.Reference1 = _ManifestShipmentDetails.Reference1;
                NewWaybill.Reference2 = _ManifestShipmentDetails.Reference2;
                NewWaybill.Reference3 = _ManifestShipmentDetails.Reference3;
                NewWaybill.ConsigneeNationalID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalID.ToString();
                NewWaybill.CurrencyID = _ManifestShipmentDetails.CurrenyID;
                NewWaybill.IntegrationModeID = 1;
                #endregion

                if (WaybillNo > 0)
                {
                    if (!IsValidWBFormat(WaybillNo.ToString()))
                    {
                        result.HasError = true;
                        result.Message = "Invalid given WaybillNo.";
                        return result;
                    }
                    NewWaybill.WayBillNo = WaybillNo;
                }

                // LB shipments need to use USD for COD orders
                if (_ManifestShipmentDetails.ConsigneeInfo.CountryCode == "LB")
                    NewWaybill.CODCurrencyID = 4;

                //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Waybill Data Prepared.");

                #region "Migration to Stored Procedure"
                //CallSaveCustomerManifest
                bool needRetryCreateWaybill = true;
            RETRYCREATEWAYBILL:
                try
                {
                    string spName = "APICreateCustomerWaybill_WithPieceBarCode_NewLength";
                    var w = new DynamicParameters();
                    w.Add("@WayBillNo", NewWaybill.WayBillNo);
                    w.Add("@ClientID", NewWaybill.ClientID);
                    w.Add("@ClientAddressID", NewWaybill.ClientAddressID);
                    w.Add("@ClientContactID", NewWaybill.ClientContactID);
                    w.Add("@ServiceTypeID", NewWaybill.ServiceTypeID);
                    w.Add("@LoadTypeID", NewWaybill.LoadTypeID);
                    w.Add("@BillingTypeID", NewWaybill.BillingTypeID);
                    w.Add("@ConsigneeID", NewWaybill.ConsigneeID);
                    w.Add("@ConsigneeAddressID", NewWaybill.ConsigneeAddressID);
                    w.Add("@OriginStationID", NewWaybill.OriginStationID);
                    w.Add("@DestinationStationID", NewWaybill.DestinationStationID);
                    w.Add("@PickUpDate", NewWaybill.PickUpDate);
                    w.Add("@PicesCount", NewWaybill.PicesCount);
                    w.Add("@Weight", NewWaybill.Weight);
                    w.Add("@Width", NewWaybill.Width);
                    w.Add("@Length", NewWaybill.Length);
                    w.Add("@Height", NewWaybill.Height);
                    w.Add("@VolumeWeight", NewWaybill.VolumeWeight);
                    w.Add("@BookingRefNo", NewWaybill.BookingRefNo);
                    w.Add("@ManifestedTime", NewWaybill.ManifestedTime);
                    w.Add("@RefNo", NewWaybill.RefNo);
                    w.Add("@IsPrintBarcode", NewWaybill.IsPrintBarcode);
                    w.Add("@StatusID", NewWaybill.StatusID);
                    w.Add("@BookingID", NewWaybill.BookingID);
                    w.Add("@IsInsurance", NewWaybill.IsInsurance);
                    w.Add("@DeclaredValue", NewWaybill.DeclaredValue);
                    w.Add("@InsuredValue", NewWaybill.InsuredValue);
                    w.Add("@PODTypeID", NewWaybill.PODTypeID);
                    w.Add("@PODDetail", NewWaybill.PODDetail);
                    w.Add("@DeliveryInstruction", NewWaybill.DeliveryInstruction);
                    w.Add("@ServiceCharge", NewWaybill.ServiceCharge);
                    w.Add("@StandardShipment", NewWaybill.StandardShipment);
                    w.Add("@SpecialCharge", NewWaybill.SpecialCharge);
                    w.Add("@ODAStationCharge", NewWaybill.ODAStationCharge);
                    w.Add("@OtherCharge", NewWaybill.OtherCharge);
                    w.Add("@Discount", NewWaybill.Discount);
                    w.Add("@NetCharge", NewWaybill.NetCharge);
                    w.Add("@PaidAmount", NewWaybill.PaidAmount);
                    w.Add("@OnAccount", NewWaybill.OnAccount);
                    w.Add("@StandardTariffID", NewWaybill.StandardTariffID);
                    w.Add("@IsCOD", NewWaybill.IsCOD);
                    w.Add("@CODCharge", NewWaybill.CODCharge);
                    w.Add("@ProductTypeID", NewWaybill.ProductTypeID);
                    w.Add("@IsShippingAPI", NewWaybill.IsShippingAPI);
                    w.Add("@Contents", NewWaybill.Contents);
                    w.Add("@BatchNo", NewWaybill.BatchNo);
                    w.Add("@ODAOriginID", NewWaybill.ODAOriginID);
                    w.Add("@ODADestinationID", NewWaybill.ODADestinationID);
                    w.Add("@CreatedContactID", NewWaybill.CreatedContactID);
                    w.Add("@IsRTO", NewWaybill.IsRTO);
                    w.Add("@IsManifested", NewWaybill.IsManifested);
                    w.Add("@GoodDesc", NewWaybill.GoodDesc);
                    w.Add("@Incoterm", NewWaybill.Incoterm);
                    w.Add("@IncotermID", NewWaybill.IncotermID);
                    w.Add("@IncotermsPlaceAndNotes", NewWaybill.IncotermsPlaceAndNotes);
                    w.Add("@Latitude", NewWaybill.Latitude);
                    w.Add("@Longitude", NewWaybill.Longitude);
                    w.Add("@HSCode", NewWaybill.HSCode);
                    w.Add("@CustomDutyAmount", NewWaybill.CustomDutyAmount);
                    w.Add("@GoodsVATAmount", NewWaybill.GoodsVATAmount);
                    w.Add("@IsCustomDutyPayByConsignee", NewWaybill.IsCustomDutyPayByConsignee);
                    w.Add("@Reference1", NewWaybill.Reference1);
                    w.Add("@Reference2", NewWaybill.Reference2);
                    w.Add("@Reference3", NewWaybill.Reference3);
                    w.Add("@ConsigneeNationalID", NewWaybill.ConsigneeNationalID);
                    w.Add("@CurrencyID", NewWaybill.CurrencyID);
                    w.Add("@CODCurrencyID", NewWaybill.CODCurrencyID);
                    w.Add("@IsSentSMS", NewWaybill.IsSentSMS);
                    w.Add("@PromisedDeliveryDateFrom", NewWaybill.PromisedDeliveryDateFrom);
                    w.Add("@PromisedDeliveryDateTo", NewWaybill.PromisedDeliveryDateTo);
                    w.Add("@OriginCityCode", NewWaybill.OriginCityCode);
                    w.Add("@DestinationCityCode", NewWaybill.DestinationCityCode);
                    w.Add(name: "@RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                    w.Add(name: "@CustWaybillID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    w.Add("@IntegrationModeID", NewWaybill.IntegrationModeID);

                    //w.Add(name: "@WaybillNo", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    using (var db = new SqlConnection(sqlCon))
                    {
                        //GetWaybillNo on Saving
                        var returnCode = db.Execute(spName, param: w, commandType: CommandType.StoredProcedure, commandTimeout: 60);
                        NewWaybill.WayBillNo = w.Get<int>("@RetVal");
                        //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill SP Executed With Result Of: " + NewWaybill.WayBillNo);

                        if (NewWaybill.WayBillNo == -1)
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceNoExists");
                            return result;
                        }

                        NewWaybill.ID = w.Get<int>("@CustWaybillID");
                    }
                }
                catch (Exception e)
                {
                    //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill SP Excuted Error. Exception: " + e.Message.ToString() + "      " + e.StackTrace.ToString());
                    //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill SP Excuted Error.");
                    LogException(e);
                    if (needRetryCreateWaybill)
                    {
                        needRetryCreateWaybill = false;
                        Thread.Sleep(3000);
                        GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetails.ClientInfo, (_ManifestShipmentDetails.RefNo +"Retry waybill"));
                        goto RETRYCREATEWAYBILL;
                    }
                    result.HasError = true;
                    result.Message = "an error happen when saving the waybill details code : 120" +e.Message;
                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                    result.Message = "an error happen when saving the waybill details code : 120";

                    GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo);
                    //GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _ManifestShipmentDetails.ClientInfo);
                }

                // Modify ClientID if it's Pharma one 
                if (tempPharmaClientID != 0)
                {
                    var tempCustomerWaybills = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
                    tempCustomerWaybills.ClientID = tempClientID;
                    dc.SubmitChanges();
                }
                //Pass it to CommercialInvoice
                Result _result = new Result();

                //if (_ManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0)
                //{
                //    _ManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
                //    _result = IsCommericalInvoiceValid(_ManifestShipmentDetails._CommercialInvoice, _ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.ConsigneeInfo.CountryCode, _ManifestShipmentDetails.DeclareValue, _ManifestShipmentDetails.CurrenyID);
                //    if (_result.HasError)
                //    {
                //        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                //        _result.HasError = true;
                //        _result.Message = "An error happen when saving the Commerical Invoice, please make sure to pass valid values: " + _result.Message;
                //        return _result;
                //    }
                //}
                #endregion

                if (!result.HasError)
                {
                    result.WaybillNo = NewWaybill.WayBillNo;
                    result.Key = NewWaybill.ID;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");

                    try
                    {
                        string SplClient = System.Configuration.ConfigurationManager.AppSettings["SplClientID"].ToString();
                        List<int> splList = SplClient.Split(',').Select(Int32.Parse).ToList();

                        if (splList.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
                        {
                            var dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                            // Query using the custom class

                            var splBarcodes = dcMaster.splCustomerBarCodes
                                                .Where(p => p.splMasterPieceBarCode == NewWaybill.RefNo)
                                                .Select(p => new BarcodeDetail
                                                {
                                                    BarCode = p.splPieceBarCode,
                                                    Description = p.PieceDescription
                                                })
                                                .ToList();

                            var instances = new List<CustomerBarCode>();

                            long baseIdNumber = NewWaybill.WayBillNo;

                            for (int i = 0; i < splBarcodes.Count; i++)
                            {

                                string incrementedNumber = (i + 1).ToString("D5");
                                string barcode = baseIdNumber.ToString() + incrementedNumber;

                                var instance = new CustomerBarCode
                                {

                                    BarCode = Convert.ToInt64(barcode),
                                    CustomerWayBillsID = NewWaybill.ID,
                                    CustomerPieceBarCode = splBarcodes[i].BarCode,
                                    CustomerPieceDescription = splBarcodes[i].Description,
                                    StatusID = 1


                                };

                                // Add the instance to the list
                                instances.Add(instance);
                            }
                            dc.CustomerBarCodes.InsertAllOnSubmit(instances);
                            dc.SubmitChanges();


                        }
                        //var tempCustomerWaybills = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
                        //if (NewWaybill.ClientID == 9022477)
                        //{
                        //    _ManifestShipmentDetails.WaybillNo = NewWaybill.WayBillNo;
                        //    UpdateCustomerBarCode(_ManifestShipmentDetails);
                        //}
                        //if ((dcMaster.APIClientAccesses.Where(P => P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID && P.StatusID == 1 && P.IsCreateBooking == true).Any()) && _ManifestShipmentDetails.CreateBooking == true)
                        //if (_ManifestShipmentDetails.ClientInfo.ClientID != 9016808 && _ManifestShipmentDetails.CreateBooking == true)
                        if (_ManifestShipmentDetails.CreateBooking == true && ClientHasCreateBookingPermit)
                        {
                            //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Booking Creatation Start.");
                            string tempBookingRefNo = GetClientBookingRefNoToday(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.PickUpPoint, DateTime.Now, NewWaybill.OriginStationID);
                            result.BookingRefNo = tempBookingRefNo;

                            bool NeedKeepWaybillNoForBooking = false;
                            // FirstCry Express account 9023826 ASR orders need to specify WaybillNo for each booking
                            List<int> BookingWithWBClients = new List<int>() { 9023826, 9020077 };

                            if (BookingWithWBClients.Contains(_ManifestShipmentDetails.ClientInfo.ClientID) && _ManifestShipmentDetails.DeliveryInstruction.Trim().ToLower() == "asr")
                                NeedKeepWaybillNoForBooking = true;

                            var ExistingWaybillForBookingWithWBClients = dc.CustomerWayBills
                                .Where(w => w.StatusID == 1
                                && w.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID
                                && w.RefNo == _ManifestShipmentDetails.RefNo
                                && w.DeliveryInstruction.Trim().ToLower() == "asr")
                                .FirstOrDefault();

                            if (ExistingWaybillForBookingWithWBClients != null)
                                result.BookingRefNo = ExistingWaybillForBookingWithWBClients.BookingRefNo;

                            if (tempBookingRefNo == "")
                            {
                                BookingShipmentDetails _bookingDetails = new BookingShipmentDetails
                                {
                                    ClientInfo = _ManifestShipmentDetails.ClientInfo,
                                    BillingType = 1, // A // _ManifestShipmentDetails.BillingType;
                                    PicesCount = _ManifestShipmentDetails.PicesCount,
                                    Weight = _ManifestShipmentDetails.Weight,
                                    PickUpPoint = _ManifestShipmentDetails.PickUpPoint.Trim(),
                                    SpecialInstruction = "",
                                    OriginStationID = NewWaybill.OriginStationID,
                                    DestinationStationID = NewWaybill.DestinationStationID,
                                    OfficeUpTo = DateTime.Now,
                                    PickUpReqDateTime = DateTime.Now,
                                    ContactPerson = _ManifestShipmentDetails.ClientInfo.ClientContact.Name,
                                    ContactNumber = _ManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber,
                                    LoadTypeID = _ManifestShipmentDetails.LoadTypeID,
                                    WaybillNo = 0 // result.WaybillNo;
                                };
                                _bookingDetails.ClientInfo.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID; // TODO: remove
                                _bookingDetails.ClientInfo.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID; // TODO: remove

                                if (NeedKeepWaybillNoForBooking)
                                    _bookingDetails.WaybillNo = result.WaybillNo;

                                Result BookingResult = new Result();
                                try
                                {
                                    BookingResult = CreateBooking(_bookingDetails);
                                }
                                catch (Exception e)
                                {
                                    //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Booking Creatation Exception.");
                                    LogException(e);
                                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                                    BookingResult.HasError = true;
                                }

                                //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Booking Creatation Result: " + BookingResult.HasError);
                                if (!BookingResult.HasError)
                                    result.BookingRefNo = GlobalVar.GV.GetString(BookingResult.BookingRefNo, 20);
                            }

                            CustomerWayBill _objwaybill = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
                            _objwaybill.BookingRefNo = result.BookingRefNo;
                            dc.SubmitChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        GlobalVar.GV.AddErrorMessage(ex, _ManifestShipmentDetails.ClientInfo, (_ManifestShipmentDetails.RefNo + "Barcode Insertion"));
                        LogException(ex);
                    }

                    if (!_result.HasError && _ManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0)
                    {
                        //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Commercial Invoice Creatation Start.");
                        try
                        {
                            _ManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
                            ////if(_ManifestShipmentDetails.ClientInfo.ClientID == 9018737 && _ManifestShipmentDetails.ConsigneeInfo.CountryCode =="LB")

                            //// TODO: Sometimes facing password validation exception
                            //APIClientAccess instance = dcMaster.APIClientAccesses.FirstOrDefault(P =>
                            //P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID
                            //&& P.StatusID == 1 && P.IsRestrictedToCreateWaybill != true);

                            //InfoTrack.Common.Security security = new InfoTrack.Common.Security();
                            //_ManifestShipmentDetails.ClientInfo.Password = security.Decrypt(instance.ClientPassword.ToArray());

                            //CreateCommercialInvoice(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails._CommercialInvoice);

                            InsertCommercialInvoice(_ManifestShipmentDetails._CommercialInvoice, _ManifestShipmentDetails.ClientInfo.ClientID);
                        }
                        catch (Exception ex)
                        {
                            //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Commercial Invoice Creatation Exception.");
                            LogException(ex);
                            _result.HasError = true;
                            _result.Message = Convert.ToString(ex);
                            return _result;
                        }
                        //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Commercial Invoice Creatation Finished.");
                    }
                }

                //if (WaybillNo <= 0)
                //    GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
                //else
                //    GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
                //SaveToLogFile(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.RefNo, "Create Waybill Request Finish.");
                if (!String.IsNullOrEmpty(result.BookingRefNo))
                {
                    CustomerWayBill objwaybill = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
                    objwaybill.BookingRefNo = result.BookingRefNo;
                    dc.SubmitChanges();
                }

                var response = new APIResponse
                {
                    WaybillNo = result.WaybillNo,
                    ClientID = _ManifestShipmentDetails.ClientInfo.ClientID,
                    Message = result.Message,
                    Date = DateTime.Now,
                    RefNo = _ManifestShipmentDetails.RefNo
                };
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcMaster.APIResponses.InsertOnSubmit(response);
                dcMaster.SubmitChanges();
                return result;
            }
            catch (Exception ex)
            {
                GlobalVar.GV.AddErrorMessage(ex, _ManifestShipmentDetails.ClientInfo, (_ManifestShipmentDetails.RefNo + "General"));
                LogException(ex);
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details code : 120";
                return result;


            }
        }

        #region functions for create waybill
        private void UpdateCountryCode(ManifestShipmentDetails info, string fromCode, string toCode)
        {
            if (info.ClientInfo.ClientAddress.CountryCode == fromCode)
                info.ClientInfo.ClientAddress.CountryCode = toCode;

            if (info.ConsigneeInfo.CountryCode == fromCode)
                info.ConsigneeInfo.CountryCode = toCode;
        }

        private bool ValidateClientIDForCOD(ManifestShipmentDetails info, ref Result result)
        {
            // ClientID support COD only
            if (info.BillingType == 5)
            {
                string list0 = System.Configuration.ConfigurationManager.AppSettings["ClientIDWithCODOnly"].ToString();
                List<int> _clientid0 = list0.Split(',').Select(Int32.Parse).ToList();

                if (_clientid0.Contains(info.ClientInfo.ClientID))
                {
                    result.HasError = true;
                    result.Message = "Your account does not support COD type.";
                    return false;
                }
            }
            return true;
        }

        private bool ValidateCODChargeForLebanon(ManifestShipmentDetails info, ref Result result)
        {
            if (info.BillingType == 5 && info.ConsigneeInfo.CountryCode == "LB")
            {
                var CODtemp = info.CODCharge;
                if (CODtemp % 1 != 0) // to get after decimal points value
                {
                    result.HasError = true;
                    result.Message = "Incorrect value in CODCharge field, for Lebanon CODCharge field accepts whole numbers or .0 only";
                    return false;
                }
            }
            return true;
        }

        private bool ValidateCODChargeForIraq(ManifestShipmentDetails info, ref Result result)
        {
            if (info.BillingType == 5 && info.ConsigneeInfo.CountryCode == "IQ")
            {
                var CODtemp = info.CODCharge;
                List<double> ValidDecimals = new List<double> { 0, 0.25, 0.5, 0.75 };
                var ModValue = CODtemp % 1;

                if (!ValidDecimals.Contains(ModValue))
                {
                    result.HasError = true;
                    result.Message = "Incorrect decimal format in CODCharge field, for Iraq CODCharge field accepts .000 , .250 , .500, or .750 only";
                    return false;
                }
            }
            return true;
        }

        private bool ValidateCODChargeForMorocco(ManifestShipmentDetails info, ref Result result)
        {
            if (info.BillingType == 5 && info.ConsigneeInfo.CountryCode == "MA")
            {
                string CountrywithCOD = System.Configuration.ConfigurationManager.AppSettings["CountrywithCOD"].ToString();
                List<int> _CountrywithCOD = CountrywithCOD.Split(',').Select(Int32.Parse).ToList();
                bool is_CountrywithCOD = _CountrywithCOD.Contains(385); // To check MA 385 only

                if (!is_CountrywithCOD)
                {
                    result.HasError = true;
                    result.Message = "COD billing type is not supported in Morocco.";
                    return false;
                }
            }
            return true;
        }

        private void ValidateDDUClient(ManifestShipmentDetails manifestShipmentDetails)
        {
            // DDU client validation
            string dduClientsConfig = System.Configuration.ConfigurationManager.AppSettings["DDUDefault"].ToString();
            List<int> dduClientIds = dduClientsConfig.Split(',').Select(Int32.Parse).ToList();

            if (dduClientIds.Contains(manifestShipmentDetails.ClientInfo.ClientID))
            {
                // Ensure Incoterm is either "DDU" or "DDP"
                if (manifestShipmentDetails.Incoterm.ToLower() != "ddu" && manifestShipmentDetails.Incoterm.ToLower() != "ddp")
                {
                    manifestShipmentDetails.Incoterm = "DDU";
                }

                // Set custom duty payment by consignee
                manifestShipmentDetails.IsCustomDutyPayByConsignee = true;
            }
        }

        private int GetPharmaClientID(int ClientID)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            string sql = @"select ParentClientID from ParentAndChild where ChildClientID =  " + ClientID;
            using (SqlConnection myConnection = new SqlConnection(con))
                try
                {
                    var ClientIDList = myConnection.Query(sql).ToList();
                    return ClientIDList.Count == 0 ? 0 : (int)ClientIDList[0].ParentClientID;
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            return 0;
        }
        #endregion

        private string GetClientBookingRefNoToday(ClientInformation ClientInfo, string PickUpPoint, DateTime PickUpReqDT, int OriginStationID)
        {
            bool isBookingNextDay = IsCreateBookingNextDay(ClientInfo.ClientID);
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            DateTime DtStart, DtEnd, PickDtStart, PickDtEnd;
            DtStart = DateTime.Now.AddDays(1).Date;
            DtEnd = DateTime.Now.AddDays(2).Date;

            if (!isBookingNextDay)
            {
                DtStart = DateTime.Now.Date;
                DtEnd = DateTime.Now.AddDays(1).Date;
            }

            if (PickUpReqDT.Date < DtStart.Date)
                PickUpReqDT = PickUpReqDT.AddDays(1);

            PickDtStart = PickUpReqDT.Date;
            PickDtEnd = PickUpReqDT.AddDays(1).Date;

            var t = dc.Bookings.Where(b => b.ClientID == ClientInfo.ClientID
                                        && b.IsCanceled == false
                                        && b.IsShippingAPI == true
                                        && b.ClientAddressID == ClientInfo.ClientAddressID
                                        && b.ClientContactID == ClientInfo.ClientContactID
                                        && b.PickUpPoint == PickUpPoint
                                        && b.OriginStationID == OriginStationID
                                        && b.BookingDate >= DtStart && b.BookingDate < DtEnd
                                        && b.PickUpReqDT >= PickDtStart && b.PickUpReqDT < PickDtEnd
                                    ).ToList();

            return t.Any() ? t.Last().RefNo : "";
        }

        bool IsCreateBookingNextDay(int ClientID)
        {
            bool birdWingsCreateBookingNextday = false;
            if (ClientID == 9022674 && (DateTime.Now.Hour > 16 || (DateTime.Now.Hour == 16 && DateTime.Now.Minute > 30)))
                birdWingsCreateBookingNextday = true;
            return birdWingsCreateBookingNextday;
        }

        [WebMethod(Description = "You can use this function to create an ASR waybill in the system.")]
        public AsrResult CreateWaybillForASR(AsrManifestShipmentDetails _AsrManifestShipmentDetails)
        {
            AsrResult result = new AsrResult();
            Result tempResult = new Result();
            WritetoXMLUpdateWaybill(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR);
            try
            {
                var doc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (doc.APIClientAndSubClients.Where(p => p.pClientID == _AsrManifestShipmentDetails.ClientInfo.ClientID).Count() > 0)
                {
                    var subClient = doc.APIClientAndSubClients.FirstOrDefault(p => p.pClientID == _AsrManifestShipmentDetails.ClientInfo.ClientID
                    && p.DestCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_AsrManifestShipmentDetails.ConsigneeInfo.CountryCode))
                    && p.OrgCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode))
                    && p.StatusId == 1);
                    if (subClient != null)
                    {
                        _AsrManifestShipmentDetails.ClientInfo.ClientID = subClient.cClientID;
                    }
                }

                if (_AsrManifestShipmentDetails.ClientInfo.ClientID == 9021604 && _AsrManifestShipmentDetails.LoadTypeID == 66)
                {
                    // InnerWork last mile account to change client country/city code to KSA/DMM
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode = "KSA";
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode = "DMM";
                }

                if (_AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode == "SA")
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode = "KSA";

                if (_AsrManifestShipmentDetails.ConsigneeInfo.CountryCode == "SA")
                    _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode = "KSA";

                bool IsCourierLoadType = false;
                string courierLoadTypes = System.Configuration.ConfigurationManager.AppSettings["CourierLoadTypes"].ToString();
                List<int> _courierLoadTypes = courierLoadTypes.Split(',').Select(Int32.Parse).ToList();
                IsCourierLoadType = _courierLoadTypes.Contains(_AsrManifestShipmentDetails.LoadTypeID);

                if (_AsrManifestShipmentDetails.BillingType != 1)
                {
                    result.HasError = true;
                    result.Message = "ASR orders billing type should be prepaid(1).";
                    return result;
                }

                if (String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode)
                    || String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode)
                    || String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ConsigneeInfo.CityCode)
                    || String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ConsigneeInfo.CountryCode))
                {
                    result.HasError = true;
                    result.Message = "Invalid CityCode or CountryCode.";
                    return result;
                }
                string AsrPLLoadTypes = System.Configuration.ConfigurationManager.AppSettings["AsrPLLoadTypes"].ToString();
                string DropOffASRLoadTypes = System.Configuration.ConfigurationManager.AppSettings["DropOffASRLoadTypes"].ToString();
                List<int> _AsrPLLoadTypes = AsrPLLoadTypes.Split(',').Select(Int32.Parse).ToList();
                List<int> _DropOffASRLoadTypes = DropOffASRLoadTypes.Split(',').Select(Int32.Parse).ToList();
                if (_AsrPLLoadTypes.Contains(_AsrManifestShipmentDetails.LoadTypeID))
                {
                    if (String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ConsigneeInfo.ParcelLockerMachineID))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErMandatoryPLIDCode");
                        return result;
                    }
                    tempResult = _AsrManifestShipmentDetails.ConsigneeInfo.CheckPLmachineID(_AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.ConsigneeInfo.ParcelLockerMachineID);

                }
                else if (_DropOffASRLoadTypes.Contains(_AsrManifestShipmentDetails.LoadTypeID))
                {
                    if (String.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ConsigneeInfo.SPLOfficeID))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErMandatorySPLOfficeIDCode");
                        return result;
                    }
                    tempResult = _AsrManifestShipmentDetails.ConsigneeInfo.CheckSPLDropOffLocation(_AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.ConsigneeInfo.SPLOfficeID);
                }
                if (tempResult.HasError)
                {
                    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, tempResult);
                    return tempResult.ConvertToAsrResult();
                }
                // Swap city code and country code as Jay requested
                string tempClientCityCode = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode;
                string tempClientCountryCode = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode;
                _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode = _AsrManifestShipmentDetails.ConsigneeInfo.CityCode;
                _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode = _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode;
                _AsrManifestShipmentDetails.ConsigneeInfo.CityCode = tempClientCityCode;
                _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode = tempClientCountryCode;

                //fastco swap
                string fastcooSwapIDs = System.Configuration.ConfigurationManager.AppSettings["FASTCOOSwap"];
                List<int> swapClient = fastcooSwapIDs.Split(',').Select(int.Parse).ToList();
                int fastcooClientId = _AsrManifestShipmentDetails.ClientInfo.ClientID;



                if (swapClient.Contains(fastcooClientId))
                {
                    string tempClientName = _AsrManifestShipmentDetails.ClientInfo.ClientContact.Name;
                    string tempClientEmail = _AsrManifestShipmentDetails.ClientInfo.ClientContact.Email;
                    string tempClientPhone = _AsrManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber;
                    string tempClientMobile = _AsrManifestShipmentDetails.ClientInfo.ClientContact.MobileNo;
                    string tempClientAddress = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.FirstAddress;
                    string tempClientNationalAddress = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.NationalAddress;
                    string tempClientPhoneAddress = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.PhoneNumber;
                    string tempClientFax = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.Fax;
                    string tempClientLocation = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.Location; // clientLocation vs consignee Near

                    string tempConsigneeName = _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeName;
                    string tempConsigneeEmail = _AsrManifestShipmentDetails.ConsigneeInfo.Email;
                    string tempConsigneePhone = _AsrManifestShipmentDetails.ConsigneeInfo.PhoneNumber;
                    string tempConsigneePhone2 = _AsrManifestShipmentDetails.ConsigneeInfo.PhoneNumber;
                    string tempConsigneeMobile = _AsrManifestShipmentDetails.ConsigneeInfo.Mobile;
                    string tempConsigneeAddress = _AsrManifestShipmentDetails.ConsigneeInfo.Address;
                    string tempConsigneeFax = _AsrManifestShipmentDetails.ConsigneeInfo.Fax;
                    string tempConsigneeNationalAddress = _AsrManifestShipmentDetails.ConsigneeInfo.NationalAddress;
                    string tempConsigneeNear = _AsrManifestShipmentDetails.ConsigneeInfo.Near;

                    // swap
                    _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeName = tempClientName;
                    _AsrManifestShipmentDetails.ConsigneeInfo.Email = tempClientEmail;
                    _AsrManifestShipmentDetails.ConsigneeInfo.PhoneNumber = tempClientPhone;
                    _AsrManifestShipmentDetails.ConsigneeInfo.Mobile = tempClientMobile;
                    _AsrManifestShipmentDetails.ConsigneeInfo.Address = tempClientAddress;
                    _AsrManifestShipmentDetails.ConsigneeInfo.NationalAddress = tempClientNationalAddress;
                    _AsrManifestShipmentDetails.ConsigneeInfo.Fax = tempClientFax;
                    _AsrManifestShipmentDetails.ConsigneeInfo.Near = tempClientLocation;
                    _AsrManifestShipmentDetails.ClientInfo.ClientContact.Name = tempConsigneeName;
                    _AsrManifestShipmentDetails.ClientInfo.ClientContact.Email = tempConsigneeEmail;
                    _AsrManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber = tempConsigneePhone;
                    _AsrManifestShipmentDetails.ClientInfo.ClientContact.MobileNo = tempConsigneeMobile;
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.FirstAddress = tempConsigneeAddress;
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.NationalAddress = tempConsigneeNationalAddress;
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.PhoneNumber = tempConsigneePhone2;
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.Fax = tempConsigneeFax;
                    _AsrManifestShipmentDetails.ClientInfo.ClientAddress.Location = tempConsigneeNear;
                }
                #region Data validation
                tempResult = _AsrManifestShipmentDetails.ClientInfo.CheckClientInfo(_AsrManifestShipmentDetails.ClientInfo, true);
                if (tempResult.HasError)
                {
                    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR, tempResult);
                    return tempResult.ConvertToAsrResult();
                }

                tempResult = _AsrManifestShipmentDetails.ConsigneeInfo.CheckConsigneeInfo(_AsrManifestShipmentDetails.ConsigneeInfo, _AsrManifestShipmentDetails.ClientInfo);
                if (tempResult.HasError)
                {
                    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, tempResult);
                    return tempResult.ConvertToAsrResult();
                }

                _AsrManifestShipmentDetails.PickUpDate = _AsrManifestShipmentDetails.PickUpDate.Date;
                string eal = System.Configuration.ConfigurationManager.AppSettings["ASREarliestAndLatestPickupHour"].ToString();
                var ealList = eal.Split(',').Select(x => int.Parse(x.Trim())).ToList();
                var earliestTime = ealList[0];
                var latestTime = ealList[1];
                if (DateTime.Now.Hour >= latestTime && _AsrManifestShipmentDetails.PickUpDate.Date == DateTime.Now.Date)
                {
                    _AsrManifestShipmentDetails.PickUpDate = _AsrManifestShipmentDetails.PickUpDate.AddDays(1);
                }
                if (DateTime.Now.Hour <= earliestTime && _AsrManifestShipmentDetails.PickUpDate.Date == DateTime.Now.Date.AddDays(-1))
                {
                    _AsrManifestShipmentDetails.PickUpDate = _AsrManifestShipmentDetails.PickUpDate.AddDays(1);
                }

                tempResult = _AsrManifestShipmentDetails.IsWaybillDetailsValid(_AsrManifestShipmentDetails, IsCourierLoadType);
                if (tempResult.HasError)
                {
                    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR, tempResult);
                    return tempResult.ConvertToAsrResult();
                }

                CheckClientLoadType(_AsrManifestShipmentDetails.ClientInfo.ClientID, _AsrManifestShipmentDetails.LoadTypeID, ref tempResult);
                if (tempResult.HasError)
                {
                    return tempResult.ConvertToAsrResult();
                }

                _AsrManifestShipmentDetails.ConsigneeInfo.CheckConsigneeData(_AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.ConsigneeInfo);
                if (_AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID == 0 || _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeID == 0)
                {
                    result.HasError = true;
                    result.Message = "Error happend while saving Asr Consignee Info, Please insert valid data.. ";
                    return result;
                }

                if (_AsrManifestShipmentDetails.WaybillNo != 0 && IsValidWBFormat(_AsrManifestShipmentDetails.WaybillNo.ToString()))
                {
                    tempResult = CheckSpecifiedWaybillNo(_AsrManifestShipmentDetails.ClientInfo.ClientID, _AsrManifestShipmentDetails.WaybillNo);
                    if (tempResult.HasError)
                        return tempResult.ConvertToAsrResult();
                }

                // Check commercial invoice
                if (_AsrManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0)
                {
                    tempResult = IsCommericalInvoiceValidBeforeWaybill(_AsrManifestShipmentDetails._CommercialInvoice, _AsrManifestShipmentDetails.ClientInfo.ClientID, "", _AsrManifestShipmentDetails.DeclareValue, _AsrManifestShipmentDetails.CurrencyID);
                    if (tempResult.HasError)
                    {
                        WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR, tempResult);
                        return tempResult.ConvertToAsrResult();
                    }
                }
                #endregion

                InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dc = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (_AsrManifestShipmentDetails.OriginWaybillNo != 0 && !IsValidWBFormat(_AsrManifestShipmentDetails.OriginWaybillNo.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Invalid OriginWaybillNo, set 0 by default.";
                    return result;
                }

                string cList = System.Configuration.ConfigurationManager.AppSettings["CheckAsrOriginWB"].ToString();
                List<int> _cClientid = cList.Split(',').Select(Int32.Parse).ToList();
                if (_cClientid.Contains(_AsrManifestShipmentDetails.ClientInfo.ClientID))
                {
                    if (_AsrManifestShipmentDetails.OriginWaybillNo == 0)
                    {
                        result.HasError = true;
                        result.Message = "OriginWaybillNo is mandatory for your ClientID, please check and pass an original Naqel WaybillNo.";
                        return result;
                    }

                    if (!IsWBExist(_AsrManifestShipmentDetails.OriginWaybillNo.ToString(), _AsrManifestShipmentDetails.ClientInfo.ClientID))
                    {
                        result.HasError = true;
                        result.Message = "OriginWaybillNo is not found under your accounts, please check and pass an valid original Naqel WaybillNo.";
                        return result;
                    }
                }

                string list = System.Configuration.ConfigurationManager.AppSettings["NoCheckRefNoClientIDs"].ToString();
                List<int> _clientid = list.Split(',').Select(Int32.Parse).ToList();
                if (string.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.RefNo)) { }

                if (!_clientid.Contains(_AsrManifestShipmentDetails.ClientInfo.ClientID))
                {
                    List<AsrWaybillInfo> waybillInfos = CheckExistWaybill(_AsrManifestShipmentDetails.ClientInfo.ClientID, _AsrManifestShipmentDetails.RefNo);
                    if (waybillInfos.Count() > 0)
                    {
                        AsrWaybillInfo waybillInfo = waybillInfos[0];
                        result.WaybillNo = waybillInfo.WaybillNo;
                        result.Key = waybillInfo.ID;
                        result.HasError = false;
                        result.Message = "Waybill already generated with RefNo: " + _AsrManifestShipmentDetails.RefNo;
                        result.PickUpDate = waybillInfo.PickUpDate;
                        return result;
                    }
                }

                // set delivery instruction and mobile as infotrack desktop needed
                if (string.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.DeliveryInstruction))
                {
                    _AsrManifestShipmentDetails.DeliveryInstruction = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.FirstAddress;
                }

                if (string.IsNullOrWhiteSpace(_AsrManifestShipmentDetails.ConsigneeInfo.Mobile))
                {
                    _AsrManifestShipmentDetails.ConsigneeInfo.Mobile = _AsrManifestShipmentDetails.ConsigneeInfo.PhoneNumber;
                }

                #region Get Suitable PickUpDate
                var AvailablePickUpDate = GetAvailablePickUpDate(_AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _AsrManifestShipmentDetails.PickUpDate);
                if (AvailablePickUpDate == null)
                {
                    result.HasError = true;
                    result.Message = "No delivery schedule for this city, contact Naqel IT for further operations!";
                    return result;
                }
                else
                {
                    _AsrManifestShipmentDetails.PickUpDate = (DateTime)AvailablePickUpDate;
                }
                #endregion

                CustomerWayBill NewWaybill = new CustomerWayBill();
                NewWaybill.ClientID = _AsrManifestShipmentDetails.ClientInfo.ClientID;
                NewWaybill.ClientAddressID = _AsrManifestShipmentDetails.ClientInfo.ClientAddressID;
                NewWaybill.ClientContactID = _AsrManifestShipmentDetails.ClientInfo.ClientContactID;
                NewWaybill.LoadTypeID = _AsrManifestShipmentDetails.LoadTypeID;
                NewWaybill.ServiceTypeID = _AsrManifestShipmentDetails.ServiceTypeID;
                NewWaybill.BillingTypeID = _AsrManifestShipmentDetails.BillingType;
                NewWaybill.IsCOD = _AsrManifestShipmentDetails.IsCOD;
                NewWaybill.ConsigneeID = _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeID;
                NewWaybill.ConsigneeAddressID = _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID;
                if (swapClient.Contains(fastcooClientId))
                {
                    NewWaybill.OriginStationID = _AsrManifestShipmentDetails.DestinationStationID;
                    NewWaybill.DestinationStationID = _AsrManifestShipmentDetails.OriginStationID;
                    NewWaybill.OriginCityCode = _AsrManifestShipmentDetails.DestinationCityCode;
                    NewWaybill.DestinationCityCode = _AsrManifestShipmentDetails.OriginCityCode;
                }
                else
                {
                    NewWaybill.OriginStationID = _AsrManifestShipmentDetails.OriginStationID;
                    NewWaybill.DestinationStationID = _AsrManifestShipmentDetails.DestinationStationID;
                    NewWaybill.OriginCityCode = _AsrManifestShipmentDetails.OriginCityCode;
                    NewWaybill.DestinationCityCode = _AsrManifestShipmentDetails.DestinationCityCode;
                }
                NewWaybill.ODADestinationID = _AsrManifestShipmentDetails.ODADestinationStationID;
                NewWaybill.CODCurrencyID = dc.Currencies.FirstOrDefault(p => p.CountryID == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_AsrManifestShipmentDetails.ConsigneeInfo.CountryCode))).ID;
                NewWaybill.PickUpDate = _AsrManifestShipmentDetails.PickUpDate;
                NewWaybill.PicesCount = _AsrManifestShipmentDetails.PicesCount;
                NewWaybill.PromisedDeliveryDateFrom = _AsrManifestShipmentDetails.PromisedDeliveryDateFrom;
                NewWaybill.PromisedDeliveryDateTo = _AsrManifestShipmentDetails.PromisedDeliveryDateTo;
                NewWaybill.InsuredValue = _AsrManifestShipmentDetails.InsuredValue;
                NewWaybill.IsInsurance = _AsrManifestShipmentDetails.InsuredValue > 0;
                NewWaybill.Weight = _AsrManifestShipmentDetails.Weight < 0.1 ? 0.1 : Math.Round(_AsrManifestShipmentDetails.Weight, 2);
                NewWaybill.Width = _AsrManifestShipmentDetails.Width;
                NewWaybill.Length = _AsrManifestShipmentDetails.Length;
                NewWaybill.Height = _AsrManifestShipmentDetails.Height;
                NewWaybill.VolumeWeight = Math.Round(_AsrManifestShipmentDetails.VolumetricWeight, 2);
                NewWaybill.BookingRefNo = "";
                NewWaybill.ManifestedTime = DateTime.Now;
                NewWaybill.DeclaredValue = Math.Round(_AsrManifestShipmentDetails.DeclareValue, 2);
                NewWaybill.GoodDesc = _AsrManifestShipmentDetails.GoodDesc;
                NewWaybill.Latitude = _AsrManifestShipmentDetails.Latitude;
                NewWaybill.Longitude = _AsrManifestShipmentDetails.Longitude;
                NewWaybill.RefNo = GlobalVar.GV.GetString(_AsrManifestShipmentDetails.RefNo, 100);
                NewWaybill.IsPrintBarcode = false;
                NewWaybill.StatusID = 1;
                NewWaybill.DeliveryInstruction = GlobalVar.GV.GetString(_AsrManifestShipmentDetails.DeliveryInstruction, 200);
                NewWaybill.CODCharge = Math.Round(_AsrManifestShipmentDetails.CODCharge, 2);
                NewWaybill.Discount = 0;
                NewWaybill.NetCharge = 0;
                NewWaybill.OnAccount = 0;
                NewWaybill.ServiceCharge = 0;
                NewWaybill.ODAStationCharge = 0;
                NewWaybill.OtherCharge = 0;
                NewWaybill.PaidAmount = 0;
                NewWaybill.SpecialCharge = 0;
                NewWaybill.StandardShipment = 0;
                NewWaybill.StorageCharge = 0;
                NewWaybill.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
                NewWaybill.IsShippingAPI = true;
                NewWaybill.PODDetail = "";
                NewWaybill.PODTypeID = null;
                NewWaybill.IsRTO = _AsrManifestShipmentDetails.ClientInfo.ClientID == 1024600 && _AsrManifestShipmentDetails.isRTO;
                NewWaybill.IsManifested = false;
                NewWaybill.GoodsVATAmount = _AsrManifestShipmentDetails.GoodsVATAmount;
                NewWaybill.IsCustomDutyPayByConsignee = _AsrManifestShipmentDetails.IsCustomDutyPayByConsignee;
                NewWaybill.Reference1 = _AsrManifestShipmentDetails.Reference1;
                NewWaybill.Reference2 = _AsrManifestShipmentDetails.Reference2;
                NewWaybill.CurrencyID = _AsrManifestShipmentDetails.CurrencyID;
                NewWaybill.IsAfterSaleReturn = true;
                NewWaybill.OriginalWaybillNo = Convert.ToInt32(_AsrManifestShipmentDetails.OriginWaybillNo);

                // Check consigneeID for High Value waybills // ASR no need for DV validation
                //if (NewWaybill.DeclaredValue > 0)
                //{
                //    double HighValueDV = 0;
                //    var cur = dc.Currencies.FirstOrDefault(p => p.ID == _AsrManifestShipmentDetails.CurrencyID);
                //    HighValueDV = _AsrManifestShipmentDetails.DeclareValue / cur.ExchangeRate;

                //    if (HighValueDV > 266.67)
                //    {
                //        string list1 = System.Configuration.ConfigurationManager.AppSettings["ParentChildClientIDs"].ToString(); //ParentChildClientIDs
                //        List<int> _clientid1 = list1.Split(',').Select(Int32.Parse).ToList();


                //        if (!_clientid1.Contains(NewWaybill.ClientID))
                //        {
                //            NewWaybill.ConsigneeNationalID = _AsrManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalID.ToString();
                //            if (NewWaybill.ConsigneeNationalID.Length != 10)
                //            {
                //                result.HasError = true;
                //                result.Message = "Error happend while saving ConsigneeNationalID, Please insert a valid ID";
                //                return result;
                //            }
                //        }
                //    }
                //}

                #region "Migration to Stored Procedure"

                try
                {
                    string spName = "APICreateCustomerWaybillASR_NewLength";
                    var w = new DynamicParameters();
                    w.Add("@WayBillNo", _AsrManifestShipmentDetails.WaybillNo != 0 ? _AsrManifestShipmentDetails.WaybillNo : NewWaybill.WayBillNo);
                    w.Add("@ClientID", NewWaybill.ClientID);
                    w.Add("@ClientAddressID", NewWaybill.ClientAddressID);
                    w.Add("@ClientContactID", NewWaybill.ClientContactID);
                    w.Add("@ServiceTypeID", NewWaybill.ServiceTypeID);
                    w.Add("@LoadTypeID", NewWaybill.LoadTypeID);
                    w.Add("@BillingTypeID", NewWaybill.BillingTypeID);
                    w.Add("@ConsigneeID", NewWaybill.ConsigneeID);
                    w.Add("@ConsigneeAddressID", NewWaybill.ConsigneeAddressID);
                    w.Add("@OriginStationID", NewWaybill.OriginStationID);
                    w.Add("@DestinationStationID", NewWaybill.DestinationStationID);
                    w.Add("@PickUpDate", NewWaybill.PickUpDate);
                    w.Add("@PicesCount", NewWaybill.PicesCount);
                    w.Add("@Weight", NewWaybill.Weight);
                    w.Add("@Width", NewWaybill.Width);
                    w.Add("@Length", NewWaybill.Length);
                    w.Add("@Height", NewWaybill.Height);
                    w.Add("@VolumeWeight", NewWaybill.VolumeWeight);
                    w.Add("@BookingRefNo", NewWaybill.BookingRefNo);
                    w.Add("@ManifestedTime", NewWaybill.ManifestedTime);
                    w.Add("@RefNo", NewWaybill.RefNo);
                    w.Add("@IsPrintBarcode", NewWaybill.IsPrintBarcode);
                    w.Add("@StatusID", NewWaybill.StatusID);
                    w.Add("@BookingID", NewWaybill.BookingID);
                    w.Add("@IsInsurance", NewWaybill.IsInsurance);
                    w.Add("@DeclaredValue", NewWaybill.DeclaredValue);
                    w.Add("@InsuredValue", NewWaybill.InsuredValue);
                    w.Add("@PODTypeID", NewWaybill.PODTypeID);
                    w.Add("@PODDetail", NewWaybill.PODDetail);
                    w.Add("@DeliveryInstruction", NewWaybill.DeliveryInstruction);
                    w.Add("@ServiceCharge", NewWaybill.ServiceCharge);
                    w.Add("@StandardShipment", NewWaybill.StandardShipment);
                    w.Add("@SpecialCharge", NewWaybill.SpecialCharge);
                    w.Add("@ODAStationCharge", NewWaybill.ODAStationCharge);
                    w.Add("@OtherCharge", NewWaybill.OtherCharge);
                    w.Add("@Discount", NewWaybill.Discount);
                    w.Add("@NetCharge", NewWaybill.NetCharge);
                    w.Add("@PaidAmount", NewWaybill.PaidAmount);
                    w.Add("@OnAccount", NewWaybill.OnAccount);
                    w.Add("@StandardTariffID", NewWaybill.StandardTariffID);
                    w.Add("@IsCOD", NewWaybill.IsCOD);
                    w.Add("@CODCharge", NewWaybill.CODCharge);
                    w.Add("@ProductTypeID", NewWaybill.ProductTypeID);
                    w.Add("@IsShippingAPI", NewWaybill.IsShippingAPI);
                    w.Add("@Contents", NewWaybill.Contents);
                    w.Add("@BatchNo", NewWaybill.BatchNo);
                    w.Add("@ODAOriginID", NewWaybill.ODAOriginID);
                    w.Add("@ODADestinationID", NewWaybill.ODADestinationID);
                    w.Add("@CreatedContactID", NewWaybill.CreatedContactID);
                    w.Add("@IsRTO", NewWaybill.IsRTO);
                    w.Add("@IsManifested", NewWaybill.IsManifested);
                    w.Add("@GoodDesc", NewWaybill.GoodDesc);
                    w.Add("@Latitude", NewWaybill.Latitude);
                    w.Add("@Longitude", NewWaybill.Longitude);
                    w.Add("@HSCode", NewWaybill.HSCode);
                    w.Add("@CustomDutyAmount", NewWaybill.CustomDutyAmount);
                    w.Add("@GoodsVATAmount", NewWaybill.GoodsVATAmount);
                    w.Add("@IsCustomDutyPayByConsignee", NewWaybill.IsCustomDutyPayByConsignee);
                    w.Add("@Reference1", NewWaybill.Reference1);
                    w.Add("@Reference2", NewWaybill.Reference2);
                    w.Add("@ConsigneeNationalID", NewWaybill.ConsigneeNationalID);
                    w.Add("@CurrencyID", NewWaybill.CurrencyID);
                    w.Add("@IsSentSMS", NewWaybill.IsSentSMS);
                    //w.Add("@SurChargeCodeID", NewWaybill.SurChargeCodeID);
                    w.Add("@OriginCityCode", NewWaybill.OriginCityCode);
                    w.Add("@DestinationCityCode", NewWaybill.DestinationCityCode);
                    w.Add("@IsAfterSaleReturn", NewWaybill.IsAfterSaleReturn);
                    w.Add("@OriginalWaybillNo", NewWaybill.OriginalWaybillNo);

                    w.Add(name: "@RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                    w.Add(name: "@CustWaybillID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    //w.Add(name: "@WaybillNo", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    using (IDbConnection db = new SqlConnection(sqlCon))
                    {
                        //GetWaybillNo on Saving
                        var returnCode = db.Execute(spName, param: w, commandType: CommandType.StoredProcedure);
                        NewWaybill.WayBillNo = w.Get<int>("@RetVal");

                        if (NewWaybill.WayBillNo == -1)
                        {
                            result.HasError = true;
                            ////result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceNoExists");
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Commercial Invoices already exist.");
                            return result;
                        }
                        if (NewWaybill.WayBillNo == -2)
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("WaybillNo generate failed, contact Naqel IT for further operations.");
                            return result;
                        }

                        NewWaybill.ID = w.Get<int>("@CustWaybillID");
                    }
                }
                catch (Exception e)
                {
                    LogException(e);
                    result.HasError = true;
                    result.Message = "an error happen when saving the waybill details code : 120";

                    GlobalVar.GV.AddErrorMessage(e, _AsrManifestShipmentDetails.ClientInfo);
                    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR, result);
                    //GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _AsrManifestShipmentDetails.ClientInfo);
                    return result;
                }
                #endregion

                // Save CommercialInvoice
                if (!result.HasError && _AsrManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0)
                {
                    _AsrManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
                    //tempResult = IsCommericalInvoiceValid(_AsrManifestShipmentDetails._CommercialInvoice, _AsrManifestShipmentDetails.ClientInfo.ClientID, "", _AsrManifestShipmentDetails.DeclareValue, _AsrManifestShipmentDetails.CurrencyID);
                    //if (tempResult.HasError)
                    //{
                    //    result.HasError = true;
                    //    result.Message = "an error happen when saving the Commerical Invoice, please make sure to pass valid values: " + tempResult.Message;
                    //    WritetoXML(_AsrManifestShipmentDetails, _AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybillForASR, tempResult);
                    //    return result;
                    //}

                    tempResult = CreateCommercialInvoice(_AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails._CommercialInvoice);
                    if (tempResult.HasError)
                        return tempResult.ConvertToAsrResult();
                }

                if (!result.HasError)
                    tempResult = WaybillSurcharge(NewWaybill.WayBillNo, _AsrManifestShipmentDetails.ClientInfo.ClientID, _AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList);
                if (tempResult.HasError)
                    return tempResult.ConvertToAsrResult();

                if (!result.HasError)
                {
                    result.WaybillNo = NewWaybill.WayBillNo;
                    result.Key = NewWaybill.ID;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");
                    result.PickUpDate = NewWaybill.PickUpDate.Date;
                }

                if (WaybillNo <= 0)
                    GlobalVar.GV.CreateShippingAPIRequest(_AsrManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
                else
                    GlobalVar.GV.CreateShippingAPIRequest(_AsrManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
                return result;
            }
            catch (Exception ex)
                {
                GlobalVar.GV.AddErrorMessage(ex, _AsrManifestShipmentDetails.ClientInfo, (_AsrManifestShipmentDetails.RefNo + "Overall"));
                LogException(ex);
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details code : 120";
                return result;
            }
        }

        [WebMethod(Description = "You can use this function to Cancel an ASR waybill in the system.")]
        public Result CancelWaybillForASR(ClientInformation ClientInfo, int WaybillNo, string CancelReason)
        {
            WritetoXMLUpdateWaybill(
                   new CancelWaybillRequest() { ClientInfo = ClientInfo, WaybillNo = WaybillNo, CancelReason = "ASR " + CancelReason },
                   ClientInfo,
                   WaybillNo.ToString(),
                   EnumList.MethodType.CancelWaybill);

            Result result = new Result();
            int EmID = -1; // 18494;

            #region Data validation
            if (!IsValidWBFormat(WaybillNo.ToString()))
            {
                result.HasError = true;
                result.WaybillNo = WaybillNo;
                result.Message = "Invalid WaybillNo.";
                return result;
            }

            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
            {
                result.WaybillNo = WaybillNo;
                return result;
            }

            if (CancelReason == null || CancelReason == "")
            {
                result.HasError = true;
                result.WaybillNo = WaybillNo;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCanceledReason");
                return result;
            }

            string sheinASRClientIDs = System.Configuration.ConfigurationManager.AppSettings["SheinASRClientIDs"].ToString();
            List<int> list_SheinASRClientIDs = sheinASRClientIDs.Split(',').Select(Int32.Parse).ToList();
            bool isAvailableToCancel = false;

            isAvailableToCancel = IsAvailableToCancelAsrWaybill(ClientInfo.ClientID, WaybillNo);
            if (!isAvailableToCancel)
            {
                result.HasError = true;
                result.WaybillNo = WaybillNo;
                result.Message = "This WaybillNo cannot cancell now or already cancelled, please contact Naqel CS team for further help.";
                return result;
            }
            #endregion

            CancelReason = GlobalVar.GV.GetString(CancelReason, 50);

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var temWb = dc.CustomerWayBills.Where(w => w.WayBillNo == WaybillNo && w.ClientID == ClientInfo.ClientID && w.StatusID == 1).FirstOrDefault();
            temWb.StatusID = 3;
            temWb.Reference2 = CancelReason;

            BusinessLayer.DContext.Tracking tempTracking = new BusinessLayer.DContext.Tracking()
            {
                WaybillID = null,
                WaybillNo = temWb.WayBillNo,
                StationID = 2499,
                Date = DateTime.Now,
                EmployID = EmID,
                IsSent = false,
                StatusID = 1,
                HasError = false,
                ErrorMessage = "",
                Comments = "",
                DBTableID = 858,
                KeyID = temWb.ID,
                TrackingTypeID = 47,
                TerminalHandlingScanStatusID = 1,
                TerminalHandlingScanStatusReasonID = 2,
                EventFinalStatusID = 4
            };

            dc.Trackings.InsertOnSubmit(tempTracking);

            try
            {
                dc.SubmitChanges();
                result.HasError = false;
            }
            catch (Exception e)
            {
                LogException(e);
                result.HasError = true;
                result.WaybillNo = WaybillNo;
                result.Message = "Cancel ASR Waybill failed, please try again later.";
                GlobalVar.GV.AddErrorMessage(e, ClientInfo);
            }

            //add the canceled waybill in Cancelwaybill table
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            CancelWaybill CanceledWaybill = new CancelWaybill();
            CanceledWaybill.WayBillNo = WaybillNo;
            CanceledWaybill.Date = DateTime.Now;
            CanceledWaybill.BookingRefNo = temWb.BookingRefNo;
            CanceledWaybill.ClientContactID = temWb.ClientContactID;

            dcMaster.CancelWaybills.InsertOnSubmit(CanceledWaybill);
            dcMaster.SubmitChanges();

            if (!result.HasError)
            {
                result.HasError = false;
                result.WaybillNo = WaybillNo;
                result.Message = "ASR Waybill Canceled Successfully.";
            }

            return result;
        }

        private bool IsAvailableToCancelAsrWaybill(int ClientID, int WaybillNo)
        {
            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var clientIDs = GetCrossTrackingClientIDs(ClientID);
            List<int> intClientIDs = clientIDs.Split(',').Select(Int32.Parse).ToList();

            int tempLoadTypeID = (from a in dc.CustomerWayBills

                                  join b in dc.PickupSheetDetails
                                  on new { A = a.WayBillNo, B = DateTime.Now.Date, C = 1 }
                                  equals new { A = b.WaybillNo, B = b.PickUpDate.Value.Date, C = b.StatusID } into joinb
                                  from b in joinb.DefaultIfEmpty()

                                  where a.StatusID == 1 && intClientIDs.Contains(a.ClientID)
                                  && b == null
                                  && a.WayBillNo == WaybillNo
                                  select a.LoadTypeID).FirstOrDefault();

            bool isPicked = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.ClientID == ClientID).Any();

            string strASRLoadtypes = System.Configuration.ConfigurationManager.AppSettings["ASRLoadTypes"].ToString();
            List<int> asrLoadTypeIDs = strASRLoadtypes.Split(',').Select(Int32.Parse).ToList();
            return asrLoadTypeIDs.Contains(tempLoadTypeID) && !isPicked;
        }

        private bool HasOFP(int WaybillNo)
        {
            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var temp = dc.Trackings.Where(t => t.StatusID == 1 && t.WaybillNo == WaybillNo && t.TrackingTypeID == 84).Any();

            return temp;
        }

        private class ForwardWaybillInfo
        {
            public int ID { get; set; }
            public int WaybillNo { get; set; }
            public string RefNo { get; set; }
            public string BookingRefNo { get; set; }
            public DateTime ManifestedTime { get; set; }
        }

        private List<ForwardWaybillInfo> CheckExistingForwardWaybill(int ClientID, string RefNo, int LoadTypeID)
        {
            List<ForwardWaybillInfo> waybillInfo = new List<ForwardWaybillInfo>();
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            string sql = @"
                select ID, WaybillNo, RefNo, BookingRefNo, ManifestedTime from CustomerWaybills 
                where StatusID = 1 
                and LoadTypeID not in (66, 136, 204, 206)
                and LoadTypeID = " + LoadTypeID + @"
                and ClientID = " + ClientID + @"
                and RefNo = '" + RefNo + @"'
                order by ID desc;";

            using (SqlConnection myConnection = new SqlConnection(con))
                try
                {
                    waybillInfo = myConnection.Query<ForwardWaybillInfo>(sql).ToList();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }

            return waybillInfo;
        }

        private class AsrWaybillInfo
        {
            public int ID { get; set; }
            public int WaybillNo { get; set; }
            public string RefNo { get; set; }
            public DateTime PickUpDate { get; set; }
        }

        private List<AsrWaybillInfo> CheckExistWaybill(int ClientID, string RefNo)
        {
            List<AsrWaybillInfo> waybillInfo = new List<AsrWaybillInfo>();
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            string sql = @"
select ID, WaybillNo, RefNo, PickUpDate from CustomerWaybills 
where StatusID = 1 
and LoadTypeID in (66, 136, 204,206) 
and ClientID = " + ClientID
+ " and RefNo = '" + RefNo + "';";
            using (SqlConnection myConnection = new SqlConnection(con))
                try
                {
                    waybillInfo = myConnection.Query<AsrWaybillInfo>(sql).ToList();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }

            return waybillInfo;
        }

        private Result CheckSpecifiedWaybillNo(int ClientID, int WaybillNo)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            Result result = new Result();
            result.HasError = false;

            if (!IsValidWBFormat(WaybillNo.ToString()))
            {
                result.HasError = true;
                result.Message = "Invalid WaybillNo";
                return result;
            }

            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                string sql_CheckWaybillRange = "select * from APIClientWaybillRange where ClientID = " + ClientID
                    + " AND FromWaybillNo <= " + WaybillNo
                    + " AND ToWaybillNo >= " + WaybillNo
                    + " AND StatusID = 1";

                using (SqlCommand command = new SqlCommand(sql_CheckWaybillRange, connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillPassWrongWaybill");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                string sql_CheckIfExist = "select * from CustomerWayBills where WaybillNo = " + WaybillNo + " AND StatusID = 1";

                using (SqlCommand command = new SqlCommand(sql_CheckIfExist, connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }
            return result;
        }

        private DateTime? GetAvailablePickUpDate(string _OrigCityCode, DateTime _FromDatetime)
        {
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var stationSchedule = dcMaster.Viw_ODA_All_Stations.Where(p => p.CityCode == _OrigCityCode && p.StatusID == 1).FirstOrDefault();
            if (stationSchedule == null) // CityCode not in list
                return null;

            string Sat = string.IsNullOrWhiteSpace(stationSchedule.Saturday) ? "No" : stationSchedule.Saturday.Trim();
            string Sun = string.IsNullOrWhiteSpace(stationSchedule.Sunday) ? "No" : stationSchedule.Sunday.Trim();
            string Mon = string.IsNullOrWhiteSpace(stationSchedule.Monday) ? "No" : stationSchedule.Monday.Trim();
            string Tue = string.IsNullOrWhiteSpace(stationSchedule.Tuesday) ? "No" : stationSchedule.Tuesday.Trim();
            string Wed = string.IsNullOrWhiteSpace(stationSchedule.Wednesday) ? "No" : stationSchedule.Wednesday.Trim();
            string Thu = string.IsNullOrWhiteSpace(stationSchedule.Thursday) ? "No" : stationSchedule.Thursday.Trim();
            string Fri = string.IsNullOrWhiteSpace(stationSchedule.Friday) ? "No" : stationSchedule.Friday.Trim();

            if (Sat != "Yes" && Sun != "Yes" && Mon != "Yes" && Tue != "Yes" && Wed != "Yes" && Thu != "Yes")
            {
                if (stationSchedule.Code == null)
                    return null;

                // City has no schedule, try to get station city schedule
                var stationOfCity = dcMaster.Cities.First(p => p.Code == _OrigCityCode && p.StatusID == 1);
                if (stationOfCity == null || stationOfCity.StationID == null)
                    return null;

                var cityOfStation = dcMaster.Stations.First(p => p.ID == stationOfCity.StationID && p.StatusID == 1);
                if (cityOfStation == null || cityOfStation.CityID == null)
                    return null;

                var refCity = dcMaster.Cities.First(p => p.ID == cityOfStation.CityID && p.StatusID == 1);
                var refScheudle = dcMaster.Viw_ODA_All_Stations.Where(p => p.CityCode == refCity.Code && p.StatusID == 1).FirstOrDefault();
                if (refScheudle == null)
                    return null;
                else
                {
                    Sat = string.IsNullOrWhiteSpace(refScheudle.Saturday) ? "No" : refScheudle.Saturday.Trim();
                    Sun = string.IsNullOrWhiteSpace(refScheudle.Sunday) ? "No" : refScheudle.Sunday.Trim();
                    Mon = string.IsNullOrWhiteSpace(refScheudle.Monday) ? "No" : refScheudle.Monday.Trim();
                    Tue = string.IsNullOrWhiteSpace(refScheudle.Tuesday) ? "No" : refScheudle.Tuesday.Trim();
                    Wed = string.IsNullOrWhiteSpace(refScheudle.Wednesday) ? "No" : refScheudle.Wednesday.Trim();
                    Thu = string.IsNullOrWhiteSpace(refScheudle.Thursday) ? "No" : refScheudle.Thursday.Trim();
                    Fri = string.IsNullOrWhiteSpace(refScheudle.Friday) ? "No" : refScheudle.Friday.Trim();
                }
            }

            Dictionary<int, bool> Schedule = new Dictionary<int, bool>()
            {
                { (int)DayOfWeek.Saturday, Sat == "Yes" },
                { (int)DayOfWeek.Sunday, Sun == "Yes" },
                { (int)DayOfWeek.Monday, Mon == "Yes" },
                { (int)DayOfWeek.Tuesday, Tue == "Yes" },
                { (int)DayOfWeek.Wednesday, Wed == "Yes" },
                { (int)DayOfWeek.Thursday, Thu == "Yes" },
                { (int)DayOfWeek.Friday, Fri == "Yes" }
            };

            return GetDateInSchedule(_FromDatetime, Schedule);
        }

        private DateTime GetDateInSchedule(DateTime _PickUpDatetime, Dictionary<int, bool> _OFDSchedule)
        {
            int requestDatetimeIdx = (int)_PickUpDatetime.DayOfWeek;
            DateTime availableDatetime = _PickUpDatetime;

            for (int i = 0; i < 7; i++)
            {
                int tempIdx = requestDatetimeIdx + i;
                tempIdx = tempIdx > 6 ? tempIdx - 7 : tempIdx;
                if (_OFDSchedule[tempIdx])
                {
                    availableDatetime = availableDatetime.AddDays(i);
                    break;
                }
            }
            return availableDatetime;
        }

        private Result WaybillSurcharge(int _WaybillNo, int _clientId, List<int> SurchargeIDList)
        {
            Result result = new Result();
            using (dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection()))
            {
                try
                {
                    foreach (var item in SurchargeIDList)
                    {
                        APIWaybillSurcharge _surcharge = new APIWaybillSurcharge();
                        _surcharge.WaybillNo = _WaybillNo;
                        _surcharge.ClientID = _clientId;
                        _surcharge.SurchargeID = item;
                        _surcharge.StatusID = 1;
                        //surchargeList.Add(_surcharge);
                        dc.APIWaybillSurcharges.InsertOnSubmit(_surcharge);
                        dc.SubmitChanges();
                    }

                }
                catch (Exception ex)
                {
                    LogException(ex);
                    result.WaybillNo = _WaybillNo;
                    result.HasError = true;
                    result.Message = "Surcharges not been inserted..";
                }
            }
            return result;
        }
        #region CommentedCreateWaybill

        //[WebMethod(Description = "You can use this function to create a new waybill in the system.")]
        //public Result CreateWaybill(ManifestShipmentDetails _ManifestShipmentDetails)
        //{
        //    Result result = new Result();

        //    //try
        //    //{
        //    WritetoXMLUpdateWaybill(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "CreateWaybill", EnumList.MethodType.CreateWaybill);
        //    result = _ManifestShipmentDetails.ClientInfo.CheckClientInfo(_ManifestShipmentDetails.ClientInfo, true);

        //    if (result.HasError)
        //    {
        //        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
        //        return result;
        //    }

        //    result = _ManifestShipmentDetails.IsWaybillDetailsValid(_ManifestShipmentDetails);
        //    if (result.HasError)
        //    {
        //        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
        //        return result;
        //    }

        //    result = CheckClientLoadType(_ManifestShipmentDetails.ClientInfo.ClientID, _ManifestShipmentDetails.LoadTypeID);
        //    if (result.HasError)
        //    {
        //        return result;
        //    }

        //    _ManifestShipmentDetails.DeliveryInstruction = GlobalVar.GV.GetString(_ManifestShipmentDetails.DeliveryInstruction, 200);
        //    CustomerWayBill NewWaybill = new CustomerWayBill();
        //    NewWaybill.ClientID = _ManifestShipmentDetails.ClientInfo.ClientID;
        //    NewWaybill.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
        //    if (NewWaybill.ClientAddressID == 0)
        //    {
        //        result.HasError = true;
        //        result.Message = "Error happend while saving Client Address, Please insert a valid address.. ";
        //        return result;
        //    }

        //    NewWaybill.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;
        //    NewWaybill.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
        //    NewWaybill.ServiceTypeID = _ManifestShipmentDetails.ServiceTypeID;
        //    NewWaybill.BillingTypeID = _ManifestShipmentDetails.BillingType;
        //    if (NewWaybill.BillingTypeID == 5)
        //        NewWaybill.IsCOD = true;
        //    else
        //        NewWaybill.IsCOD = false;

        //    _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeData(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.ConsigneeInfo);
        //    NewWaybill.ConsigneeID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeID;
        //    NewWaybill.ConsigneeAddressID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID;
        //    if (NewWaybill.ConsigneeAddressID == 0)
        //    {
        //        result.HasError = true;
        //        result.Message = "Error happend while saving Consignee Address, Please insert a valid address.. ";
        //        return result;
        //    }

        //    NewWaybill.OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode));
        //    //NewWaybill.OriginStationID = Convert.ToInt32(_ManifestShipmentDetails.OriginStationID);

        //    NewWaybill.DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode, _ManifestShipmentDetails.ConsigneeInfo.CountryCode));
        //    //NewWaybill.DestinationStationID = Convert.ToInt32(_ManifestShipmentDetails.DestinationStationID);

        //    //NewWaybill.ODAOriginID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode));

        //    if (GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).HasValue)
        //        NewWaybill.ODADestinationID = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).Value;

        //    NewWaybill.PickUpDate = DateTime.Now;
        //    NewWaybill.PicesCount = _ManifestShipmentDetails.PicesCount;


        //    NewWaybill.CurrencyID = (_ManifestShipmentDetails.CurrenyID == 0 ? 1 : _ManifestShipmentDetails.CurrenyID);

        //    if (_ManifestShipmentDetails.InsuredValue != 0)
        //    {
        //        NewWaybill.InsuredValue = _ManifestShipmentDetails.InsuredValue;
        //        NewWaybill.IsInsurance = _ManifestShipmentDetails.IsInsurance;
        //    }
        //    else
        //    {
        //        NewWaybill.InsuredValue = 0;
        //        NewWaybill.IsInsurance = false;
        //    }


        //    NewWaybill.Weight = _ManifestShipmentDetails.Weight;
        //    NewWaybill.Width = 1;
        //    NewWaybill.Length = 1;
        //    NewWaybill.Height = 1;
        //    NewWaybill.VolumeWeight = 0.0002;
        //    NewWaybill.BookingRefNo = "";
        //    NewWaybill.ManifestedTime = DateTime.Now;
        //    NewWaybill.DeclaredValue = _ManifestShipmentDetails.DeclareValue;
        //    if (NewWaybill.DeclaredValue > 0)
        //    {
        //        NewWaybill.CurrencyID = _ManifestShipmentDetails.CurrenyID;
        //        if (NewWaybill.CurrencyID == null)
        //        {
        //            result.HasError = true;
        //            result.Message = "Error happend while saving Currency, Please insert a valid Currency ID";
        //            return result;
        //        }
        //    }

        //    NewWaybill.GoodDesc = _ManifestShipmentDetails.GoodDesc;
        //    NewWaybill.Latitude = _ManifestShipmentDetails.Latitude;
        //    NewWaybill.Longitude = _ManifestShipmentDetails.Longitude;

        //    if (_ManifestShipmentDetails.RefNo != "")
        //        NewWaybill.RefNo = GlobalVar.GV.GetString(_ManifestShipmentDetails.RefNo, 100);

        //    NewWaybill.IsPrintBarcode = false;
        //    NewWaybill.StatusID = 1;
        //    NewWaybill.PODDetail = "";
        //    NewWaybill.DeliveryInstruction = _ManifestShipmentDetails.DeliveryInstruction;
        //    //NewWaybill.CODCharge = System.Math.Round(_ManifestShipmentDetails.CODCharge);
        //    NewWaybill.CODCharge = _ManifestShipmentDetails.CODCharge;
        //    NewWaybill.Discount = 0;

        //    //if (_ManifestShipmentDetails.CODCharge > 0)
        //    //    NewWaybill.IsCOD = true;
        //    //else
        //    //    NewWaybill.IsCOD = false;
        //    NewWaybill.NetCharge = 0;
        //    NewWaybill.OnAccount = 0;
        //    NewWaybill.ServiceCharge = 0;

        //    NewWaybill.ODAStationCharge = 0;
        //    NewWaybill.OtherCharge = 0;
        //    NewWaybill.PaidAmount = 0;
        //    NewWaybill.SpecialCharge = 0;
        //    NewWaybill.StandardShipment = 0;
        //    NewWaybill.StorageCharge = 0;
        //    NewWaybill.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
        //    NewWaybill.IsShippingAPI = true;

        //    if (_ManifestShipmentDetails.ClientInfo.ClientID == 1024600)
        //    {
        //        NewWaybill.IsRTO = _ManifestShipmentDetails.isRTO;
        //        NewWaybill.PODTypeID = null;
        //        NewWaybill.PODDetail = "";
        //    }
        //    else
        //        NewWaybill.IsRTO = false;
        //    NewWaybill.IsManifested = false;

        //    //if (_ManifestShipmentDetails.ODAOriginID.HasValue)
        //    //    NewWaybill.ODAOriginID = _ManifestShipmentDetails.ODAOriginID;

        //    //if (_ManifestShipmentDetails.ODADestinationID.HasValue)
        //    //    NewWaybill.ODADestinationID = _ManifestShipmentDetails.ODADestinationID;

        //    //NewWaybill.HSCode = _ManifestShipmentDetails.HSCode; // commented
        //    //NewWaybill.CustomDutyAmount = _ManifestShipmentDetails.CustomDutyAmount;
        //    NewWaybill.GoodsVATAmount = _ManifestShipmentDetails.GoodsVATAmount;
        //    NewWaybill.IsCustomDutyPayByConsignee = _ManifestShipmentDetails.IsCustomDutyPayByConsignee;
        //    NewWaybill.Reference1 = _ManifestShipmentDetails.Reference1;
        //    NewWaybill.Reference2 = _ManifestShipmentDetails.Reference2;

        //    dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

        //    double HighValueDV = 0;
        //    var cur = dc.Currencies.FirstOrDefault(p => p.ID == _ManifestShipmentDetails.CurrenyID);
        //    if (cur != null)
        //    {
        //        HighValueDV = _ManifestShipmentDetails.DeclareValue / cur.ExchangeRate;
        //    }

        //    //if(cur == null)
        //    //{
        //    //    result.HasError = true;
        //    //    result.Message = "Please insert a valid currency ID";
        //    //    return result;
        //    //}

        //    //HighValueDV = _ManifestShipmentDetails.DeclareValue / cur.ExchangeRate;

        //    if (HighValueDV > 266)
        //    {
        //        NewWaybill.ConsigneeNationalID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeNationalID.ToString();
        //        if (NewWaybill.ConsigneeNationalID.Length < 10 || NewWaybill.ConsigneeNationalID.Length > 10)
        //        {
        //            result.HasError = true;
        //            result.Message = "Error happend while saving Consignee National ID, Please insert a valid ID";
        //            return result;
        //        }
        //    }

        //    if (WaybillNo > 0)
        //        NewWaybill.WayBillNo = WaybillNo;
        //    else
        //        //   NewWaybill.WayBillNo = GlobalVar.GV.GetWaybillNo(EnumList.ProductType.Home_Delivery,_ManifestShipmentDetails.ClientInfo.ClientID);
        //        NewWaybill.WayBillNo = GlobalVar.GV.GetWaybillNoTEST(EnumList.ProductType.Home_Delivery, _ManifestShipmentDetails.ClientInfo.ClientID);

        //    if (NewWaybill.WayBillNo < 1000)
        //    {
        //        result.HasError = true;
        //        result.Message = "an error happen when saving the waybill details < 1000";
        //        return result;
        //    }

        //    Result _result = new Result();

        //    if (_ManifestShipmentDetails.DeclareValue > 0)
        //    {
        //        _ManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
        //        _result = IsCommericalInvoiceValid(_ManifestShipmentDetails._CommercialInvoice, _ManifestShipmentDetails.ClientInfo.ClientID);
        //        if (_result.HasError)
        //        {
        //            WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
        //            _result.HasError = true;
        //            _result.Message = "an error happen when saving the Commerical Invoice, please make sure to pass valid values ";
        //            return _result;
        //        }
        //    }


        //    try
        //    {
        //        //Save WB using dapper 
        //        //using (IDbConnection db = new SqlConnection(sqlCon))
        //        //{
        //        //    db.Insert<CustomerWayBill>(NewWaybill);
        //        //}

        //        dc.CustomerWayBills.InsertOnSubmit(NewWaybill);
        //        dc.SubmitChanges();

        //    }
        //    catch (Exception e)
        //    {
        //        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
        //        result.HasError = true;
        //        result.Message = "an error happen when saving the waybill details code : 120";
        //        GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetails.ClientInfo);
        //        GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _ManifestShipmentDetails.ClientInfo);
        //    }


        //    if (!result.HasError)
        //    {
        //        result.WaybillNo = NewWaybill.WayBillNo;
        //        result.Key = NewWaybill.ID;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");

        //        dcMaster = new MastersDataContext();

        //        try
        //        {
        //            if ((dcMaster.APIClientAccesses.Where(P => P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID && P.StatusID == 1 && P.IsCreateBooking == true).Any()) && _ManifestShipmentDetails.CreateBooking == true)
        //            //if (_ManifestShipmentDetails.ClientInfo.ClientID != 9016808 && _ManifestShipmentDetails.CreateBooking == true)
        //            {
        //                BookingShipmentDetails _bookingDetails = new BookingShipmentDetails();
        //                _bookingDetails.ClientInfo = _ManifestShipmentDetails.ClientInfo;
        //                _bookingDetails.ClientInfo.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
        //                _bookingDetails.ClientInfo.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;

        //                _bookingDetails.BillingType = _ManifestShipmentDetails.BillingType;
        //                _bookingDetails.PicesCount = _ManifestShipmentDetails.PicesCount;
        //                _bookingDetails.Weight = _ManifestShipmentDetails.Weight;
        //                _bookingDetails.PickUpPoint = "";
        //                _bookingDetails.SpecialInstruction = "";
        //                _bookingDetails.OriginStationID = NewWaybill.OriginStationID;
        //                _bookingDetails.DestinationStationID = NewWaybill.DestinationStationID;
        //                _bookingDetails.OfficeUpTo = DateTime.Now;
        //                _bookingDetails.PickUpReqDateTime = DateTime.Now;
        //                _bookingDetails.ContactPerson = _ManifestShipmentDetails.ClientInfo.ClientContact.Name;
        //                _bookingDetails.ContactNumber = _ManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber;
        //                _bookingDetails.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
        //                _bookingDetails.WaybillNo = result.WaybillNo;

        //                Result BookingResult = new Result();
        //                try
        //                {
        //                    BookingResult = CreateBooking(_bookingDetails);
        //                }
        //                catch
        //                {
        //                    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
        //                    BookingResult.HasError = true;
        //                }

        //                if (!BookingResult.HasError)
        //                    result.BookingRefNo = GlobalVar.GV.GetString(BookingResult.BookingRefNo, 100);
        //            }

        //        }
        //        catch (Exception ex)
        //        {

        //        }

        //        if (!_result.HasError && _ManifestShipmentDetails.DeclareValue > 0)
        //        {
        //            try
        //            {
        //                _ManifestShipmentDetails._CommercialInvoice.InvoiceNo = Convert.ToString(NewWaybill.WayBillNo);
        //                CreateCommercialInvoice(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails._CommercialInvoice);
        //            }
        //            catch (Exception ex)
        //            {
        //                _result.HasError = true;
        //                _result.Message = Convert.ToString(ex);
        //                return _result;
        //            }
        //        }

        //        try
        //        {
        //            CustomerBarCode instanceBarcode;

        //            for (int i = 1; i <= NewWaybill.PicesCount; i++)
        //            {
        //                instanceBarcode = new CustomerBarCode();
        //                instanceBarcode.BarCode = Convert.ToInt64(Convert.ToString(NewWaybill.WayBillNo) + i.ToString("D5"));
        //                instanceBarcode.CustomerWayBillsID = NewWaybill.ID;
        //                instanceBarcode.StatusID = 1;
        //                dc.CustomerBarCodes.InsertOnSubmit(instanceBarcode);
        //                dc.SubmitChanges();
        //            }
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }

        //    if (WaybillNo <= 0)
        //        GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
        //    else
        //        GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
        //    return result;
        //}
        #endregion
        private void CheckClientLoadType(int clientId, int loadTypeId, ref Result result)
        {
            using (dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection()))
            {
                if (!dcMaster.ViwLoadTypeByClients.Any(p => p.ClientID == clientId && p.ID == loadTypeId))
                {
                    result.HasError = true;
                    result.Message = "This LoadTypeId is not correct, Please provide a valid LoadType as per your agreement";
                }
            }
        }

        //Requested by Finance Team & Anil - to Validate Destination country based on Agreement Rout
        private Result CheckCorrectDSCountry(int _clientId, int _LoadTypeId, int CountryID)
        {
            // Get agreement ID 
            //Route = dcMaster.PhoneRoutes.First(P => P.PhoneNo == Phoneno).RouteID;
            Result result = new Result();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            int agreementID = 0;
            // var StationID;
            int _CountryID = 0;
            try
            {
                agreementID = dcMaster.Agreements.First(P => P.ClientID == _clientId && P.LoadTypeID == _LoadTypeId && P.StatusID == 1).ID;
                var StationID = dcMaster.AgreementRoutes.First(P => P.AgreementID == agreementID && P.StatusID == 1);
                if (StationID.DestinationStationID == -2 || StationID.DestinationStationID == -1 || StationID.DestinationStationID == 0)
                {
                    _CountryID = 1;
                }
                else
                    _CountryID = dcMaster.Cities.First(P => P.StationID == StationID.DestinationStationID && P.StatusID == 1).CountryID;

                if (_CountryID != CountryID)
                {
                    result.HasError = true;
                    result.Message = "Wrong Destination Country Code Based On your agreement";
                    return result;
                }



            }

            catch (Exception e) { }



            return result;

        }

        [WebMethod(Description = "You can use this function to create Commercial Invoice")]
        public Result CreateCommercialInvoice(ClientInformation ClientInfo, CommercialInvoice _commercialInvoice)
        {
            Result result = new Result();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
                return result;

            if (dc.CustomerWayBills.Where(P => P.WayBillNo == Convert.ToInt32(_commercialInvoice.InvoiceNo) && P.StatusID == 1).Count() <= 0)
            {
                result.HasError = true;
                result.Message = "This InvoiceNo. is not correct, Please enter a valid InvoiceNo.[WaybillNo]";
                return result;
            }

            int Currency = dc.CustomerWayBills.FirstOrDefault(P => P.WayBillNo == Convert.ToInt32(_commercialInvoice.InvoiceNo) && P.StatusID == 1).CurrencyID.Value;
            double DV = dc.CustomerWayBills.FirstOrDefault(P => P.WayBillNo == Convert.ToInt32(_commercialInvoice.InvoiceNo) && P.StatusID == 1).DeclaredValue;

            result = IsCommericalInvoiceValid(_commercialInvoice, ClientInfo.ClientID, "", DV, Currency);
            if (result.HasError)
                return result;

            try
            {
                InsertCommercialInvoice(_commercialInvoice, ClientInfo.ClientID);
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "Please check the commerical invoice and push again.";
                return result;
            }

            return result;
        }

        private void InsertCommercialInvoice(CommercialInvoice _commercialInvoice, int ClientID)
        {
            ClientCommercialInvoice oClientCommercialInvoice = new ClientCommercialInvoice();
            oClientCommercialInvoice.RefNo = _commercialInvoice.RefNo;
            oClientCommercialInvoice.InvoiceNo = _commercialInvoice.InvoiceNo;
            oClientCommercialInvoice.InvoiceDate = _commercialInvoice.InvoiceDate;
            oClientCommercialInvoice.Consignee = _commercialInvoice.Consignee;

            //var _ConsigneeAddress = GlobalVar.GV.hasSpecialChar(_commercialInvoice.ConsigneeAddress);
            //oClientCommercialInvoice.ConsigneeAddress = Convert.ToString(_ConsigneeAddress);
            oClientCommercialInvoice.ConsigneeAddress = _commercialInvoice.ConsigneeAddress;

            oClientCommercialInvoice.ConsigneeEmail = _commercialInvoice.ConsigneeEmail;
            oClientCommercialInvoice.MobileNo = _commercialInvoice.MobileNo;
            oClientCommercialInvoice.Phone = _commercialInvoice.Phone;
            oClientCommercialInvoice.TotalCost = System.Math.Round(_commercialInvoice.TotalCost, 2);
            oClientCommercialInvoice.CurrencyCode = _commercialInvoice.CurrencyCode;
            oClientCommercialInvoice.ClientID = ClientID;
            oClientCommercialInvoice.InsertedOn = System.DateTime.Now;
            oClientCommercialInvoice.StatusID = 1;

            dc.ClientCommercialInvoices.InsertOnSubmit(oClientCommercialInvoice);

            bool needRetryCCI = true;
        RETRYCCI:
            try
            {
                dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                if (needRetryCCI)
                {
                    needRetryCCI = false;
                    goto RETRYCCI;
                }
                // Save error log
                LogException(ex);
            }

            for (int i = 0; i < _commercialInvoice.CommercialInvoiceDetailList.Count; i++)
            {
                ClientCommercialInvoiceDetail oClientCommercialInvoiceDetail = new ClientCommercialInvoiceDetail();
                oClientCommercialInvoiceDetail.ClientCommercialInvoiceID = oClientCommercialInvoice.ID;
                oClientCommercialInvoiceDetail.Quantity = _commercialInvoice.CommercialInvoiceDetailList[i].Quantity;
                oClientCommercialInvoiceDetail.UnitType = _commercialInvoice.CommercialInvoiceDetailList[i].UnitType;
                oClientCommercialInvoiceDetail.CountryofManufacture = _commercialInvoice.CommercialInvoiceDetailList[i].CountryofManufacture;
                oClientCommercialInvoiceDetail.Description = _commercialInvoice.CommercialInvoiceDetailList[i].Description;
                oClientCommercialInvoiceDetail.ChineseDescription = _commercialInvoice.CommercialInvoiceDetailList[i].ChineseDescription;
                oClientCommercialInvoiceDetail.UnitCost = System.Math.Round(_commercialInvoice.CommercialInvoiceDetailList[i].UnitCost, 2);
                oClientCommercialInvoiceDetail.Amount = System.Math.Round(_commercialInvoice.CommercialInvoiceDetailList[i].Quantity * _commercialInvoice.CommercialInvoiceDetailList[i].UnitCost, 2);

                if (ClientID == 9018737)
                {
                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode))
                        oClientCommercialInvoiceDetail.CustomsCommodityCode = "";
                    else if (_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length > 50)
                        oClientCommercialInvoiceDetail.CustomsCommodityCode = _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Substring(0, 50);
                    else
                        oClientCommercialInvoiceDetail.CustomsCommodityCode = _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode;
                }
                else
                    oClientCommercialInvoiceDetail.CustomsCommodityCode = _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode;

                oClientCommercialInvoiceDetail.SKU = _commercialInvoice.CommercialInvoiceDetailList[i].SKU;
                oClientCommercialInvoiceDetail.CPC = _commercialInvoice.CommercialInvoiceDetailList[i].CPC;
                oClientCommercialInvoiceDetail.ItemWeightUnit = Convert.ToString(_commercialInvoice.CommercialInvoiceDetailList[i].ItemWeightUnit);

                oClientCommercialInvoiceDetail.Currency = _commercialInvoice.CommercialInvoiceDetailList[i].Currency;
                oClientCommercialInvoiceDetail.InsertedOn = System.DateTime.Now;
                oClientCommercialInvoiceDetail.StatusID = 1;
                dc.ClientCommercialInvoiceDetails.InsertOnSubmit(oClientCommercialInvoiceDetail);
            }

            bool needRetryCCID = true;
        RETRYCCID:
            try
            {
                dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                if (needRetryCCID)
                {
                    needRetryCCID = false;
                    goto RETRYCCID;
                }
                // Save error log
                LogException(ex);
            }
        }

        private Result IsCommericalInvoiceValid(CommercialInvoice _commercialInvoice, int ClientID, string DestCountrycode, double DeclareValue, int Currency)
        {
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //var cur = dc.Currencies.FirstOrDefault(p => p.ID == Currency);
            //double HighValueDV = DeclareValue / cur.ExchangeRate;
            Result result = new Result();

            #region Check InvoiceNo and if already exist
            if (string.IsNullOrWhiteSpace(_commercialInvoice.InvoiceNo) || _commercialInvoice.InvoiceNo.Length > 100)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceNo");
                return result;
            }

            if (dc.ClientCommercialInvoices.Where(P => P.InvoiceNo == _commercialInvoice.InvoiceNo && P.ClientID == ClientID && P.StatusID == 1).Count() > 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceNoExists");
                return result;
            }
            #endregion

            result = IsCommericalInvoiceValidBeforeWaybill(_commercialInvoice, ClientID, DestCountrycode, DeclareValue, Currency);
            return result;

            #region same as IsCommericalInvoiceValidBeforeWaybill
            /*
            #region Check Commercial Invoice Data            
            if (string.IsNullOrWhiteSpace(_commercialInvoice.RefNo) || _commercialInvoice.RefNo.Length > 100)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullRefNo");
                return result;
            }

            if ((_commercialInvoice.InvoiceDate == null) || _commercialInvoice.InvoiceDate.Year < 2017)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceDate");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.Consignee) || _commercialInvoice.Consignee.Length > 200)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullConsignee");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.ConsigneeAddress) || _commercialInvoice.ConsigneeAddress.Length > 400)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullConsigneeAddress");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.CurrencyCode) || _commercialInvoice.CurrencyCode.Length > 100)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCurrencyCode");
                return result;
            }

            if (_commercialInvoice.TotalCost == 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullTotalCost");
                return result;
            }
            #endregion

            #region Check Commercial Invoice Detail List Data
            if (_commercialInvoice.CommercialInvoiceDetailList.Count > 0)
            {
                for (int i = 0; i < _commercialInvoice.CommercialInvoiceDetailList.Count; i++)
                {
                    if (_commercialInvoice.CommercialInvoiceDetailList[i].Quantity == 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullQuantity");
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].UnitType) || _commercialInvoice.CommercialInvoiceDetailList[i].UnitType.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullUnitType");
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CountryofManufacture) || _commercialInvoice.CommercialInvoiceDetailList[i].CountryofManufacture.Length > 200)
                    {//6thstreet Validation
                        if (ClientID == 9020077 || ClientID == 9019115 || ClientID == 9025811) { }
                        else
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCountryofManufacture");
                            return result;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].Description) || _commercialInvoice.CommercialInvoiceDetailList[i].Description.Length > 400)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullDescription");
                        return result;
                    }

                    if (_commercialInvoice.CommercialInvoiceDetailList[i].UnitCost == 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullUnitCost");
                        return result;
                    }

                    if (GlobalVar.GV.ValidateClientCommercialInvoice(ClientID) == true && _commercialInvoice.CommercialInvoiceDetailList[i].ItemWeightUnit <= 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Please enter UnitWeight");
                        return result;
                    }

                    if (GlobalVar.GV.HScode_Validation(ClientID) == true
                        && (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode)
                        || _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length > 50))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCustomsCommodityCode");
                        return result;
                    }

                    //string Hs_valid = System.Configuration.ConfigurationManager.AppSettings["HSCode_Validation"].ToString();
                    //List<int> Hs_validList = Hs_valid.Split(',').Select(Int32.Parse).ToList();

                    //if (Hs_validList.Contains(ClientID))
                    //{
                    //    _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode = GlobalVar.GV.BirkenHSCode(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode);
                    //}

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].Currency) || _commercialInvoice.CommercialInvoiceDetailList[i].Currency.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCurrency");
                        return result;
                    }
                }
            }
            #endregion

            result.HasError = false;
            return result;
            */
            #endregion
        }

        private Result IsCommericalInvoiceValidBeforeWaybill(CommercialInvoice _commercialInvoice, int ClientID, string DestCountrycode, double DeclareValue, int Currency)
        {
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //var cur = dc.Currencies.FirstOrDefault(p => p.ID == Currency);
            //double HighValueDV = DeclareValue / cur.ExchangeRate;
            Result result = new Result();

            #region Check Commercial Invoice Data
            if (string.IsNullOrWhiteSpace(_commercialInvoice.RefNo) || _commercialInvoice.RefNo.Length > 100)
            {
                result.HasError = true;
                result.Message = "Check commercial invoice RefNo.";
                return result;
            }

            if ((_commercialInvoice.InvoiceDate == null) || _commercialInvoice.InvoiceDate.Year < 2017)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceDate");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.Consignee) || _commercialInvoice.Consignee.Length > 200)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullConsignee");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.ConsigneeAddress) || _commercialInvoice.ConsigneeAddress.Length > 400)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullConsigneeAddress");
                return result;
            }

            if (string.IsNullOrWhiteSpace(_commercialInvoice.CurrencyCode) || _commercialInvoice.CurrencyCode.Length > 100)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCurrencyCode");
                return result;
            }

            if (_commercialInvoice.TotalCost == 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullTotalCost");
                return result;
            }
            #endregion

            #region Check Commercial Invoice Detail List Data
            if (_commercialInvoice.CommercialInvoiceDetailList.Count > 0)
            {
                for (int i = 0; i < _commercialInvoice.CommercialInvoiceDetailList.Count; i++)
                {
                    if (_commercialInvoice.CommercialInvoiceDetailList[i].Quantity == 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullQuantity");
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].UnitType) || _commercialInvoice.CommercialInvoiceDetailList[i].UnitType.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullUnitType");
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CountryofManufacture) || _commercialInvoice.CommercialInvoiceDetailList[i].CountryofManufacture.Length > 200)
                    { //6thstreet Validation
                        if (ClientID == 9019115 || ClientID == 9025811) { }
                        else
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCountryofManufacture");
                            return result;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].Description) || _commercialInvoice.CommercialInvoiceDetailList[i].Description.Length > 400)
                    {
                        result.HasError = true;
                        result.Message = "Check commercial invoice detail description."; // GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullDescription");
                        return result;
                    }

                    if (_commercialInvoice.CommercialInvoiceDetailList[i].UnitCost == 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullUnitCost");
                        return result;
                    }

                    if (GlobalVar.GV.ValidateClientCommercialInvoice(ClientID) == true && _commercialInvoice.CommercialInvoiceDetailList[i].ItemWeightUnit <= 0)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Please enter UnitWeight");
                        return result;
                    }

                    if (GlobalVar.GV.HScode_Validation(ClientID) == true
                        && (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode)
                        || _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length > 50))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCustomsCommodityCode");
                        return result;
                    }

                    //string Hs_valid = System.Configuration.ConfigurationManager.AppSettings["HSCode_Validation"].ToString();
                    //List<int> Hs_validList = Hs_valid.Split(',').Select(Int32.Parse).ToList();

                    //if (Hs_validList.Contains(ClientID))
                    //{
                    //    _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode = GlobalVar.GV.BirkenHSCode(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode);
                    //}

                    //string list3 = System.Configuration.ConfigurationManager.AppSettings["Optional_HS_code"].ToString();
                    //List<int> ClientValidation = list3.Split(',').Select(Int32.Parse).ToList();

                    //if (!ClientValidation.Contains(ClientID)) { }
                    //if ((string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode) || _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length > 50) && ClientID != 9018737)
                    //{
                    //    if (ClientID == 9018737 && DestCountrycode == "LB")
                    //    { }
                    //    //ignore for LV shipments for 6thStreet
                    //    if (ClientValidation.Contains(ClientID)) { }
                    //    //else if (ClientID == 9019115) { }
                    //    else
                    //    {
                    //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCustomsCommodityCode");
                    //        return result;
                    //    }
                    //}

                    //is HS code required based on country 
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DapperConnectionString"].ConnectionString))
                    {
                        connection.Open();
                        string HScodeQuery = "Select 1 from CountryRequiredHScode where CountryCode = @CountryCode";

                        using (SqlCommand cm = new SqlCommand(HScodeQuery, connection))
                        {
                            cm.Parameters.AddWithValue("@CountryCode", DestCountrycode);
                            object sqlresult = cm.ExecuteScalar();
                            bool hscoderequired = sqlresult != null;

                            if (hscoderequired)
                            {
                                string excludeQuery = @"select 1 from clientexcludedhscode a inner join countryrequiredhscode b on a.countryid = b.countryid where a.clientid = @clientid and b.countrycode = @countrycode";

                                using (SqlCommand cmd = new SqlCommand(excludeQuery, connection))
                                {
                                    cmd.Parameters.AddWithValue("@ClientID", ClientID);
                                    cmd.Parameters.AddWithValue("@CountryCode", DestCountrycode);

                                    object excluderesult = cmd.ExecuteScalar();
                                    bool excludedClient = excluderesult != null;

                                    if (!excludedClient) //for unexcluded clients
                                    {
                                        if ((string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode)
                                            || (_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length < 6
                                            || _commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.Length > 12)
                                            || !_commercialInvoice.CommercialInvoiceDetailList[i].CustomsCommodityCode.All(char.IsDigit)))
                                        {
                                            result.HasError = true;
                                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCustomCommidityCode");
                                            return result;

                                        }
                                    }
                                }

                            }

                        }

                    }
                    if (_commercialInvoice.CommercialInvoiceDetailList[i].ChineseDescription.Length > 400)
                    {
                        result.HasError = true;
                        result.Message = "Your Description length must be less than 400 character..";
                        return result;
                    }

                    if (string.IsNullOrWhiteSpace(_commercialInvoice.CommercialInvoiceDetailList[i].Currency) || _commercialInvoice.CommercialInvoiceDetailList[i].Currency.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullCurrency");
                        return result;
                    }
                }
            }
            #endregion

            result.HasError = false;
            return result;
        }

        [WebMethod(Description = "You can use this function to get Commercial Invoice file by InvoiceNo/WaybillNo as Byte[]")]
        public byte[] GetCommercialInvoice(ClientInformation clientInfo, string InvoiceNo)
        {
            byte[] x = { };

            if (string.IsNullOrWhiteSpace(InvoiceNo)
                || clientInfo.CheckClientInfo(clientInfo, false).HasError
                || !IsValidWBFormat(InvoiceNo))
                return x;

            var tempClientCommercialInvoice = GetClientCommercialInvoices(clientInfo.ClientID, InvoiceNo);

            if (tempClientCommercialInvoice.Count == 0) // Invioce not belongs to clientIDs
                return x;

            //if (!IsInvoiceBelongsToClientGeneral(clientInfo.ClientID, InvoiceNo)) // Invioce not belongs to clientIDs
            //    return x;

            if (IsAsrWaybill(clientInfo.ClientID, Convert.ToInt32(InvoiceNo)))
                return GetCommercialInvoiceAsr(clientInfo, Convert.ToInt32(InvoiceNo));

            //App_Data.InfoTrackDataTableAdapters.ViwCommercialInvoiceTableAdapter adapter =
            //    new App_Data.InfoTrackDataTableAdapters.ViwCommercialInvoiceTableAdapter();
            //adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            List<rpInvoiceBarCode> lstBarCodeObj = InvoiceConnection(tempClientCommercialInvoice.First(), Convert.ToInt32(InvoiceNo));
            if (lstBarCodeObj.Count() > 0)
            {
                Report.ClientCommercialInvoice1 report = new Report.ClientCommercialInvoice1
                {
                    DataSource = lstBarCodeObj
                };

                string fileName = Server.MapPath(".") + "\\WaybillStickers\\" + InvoiceNo + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";
                report.CreateDocument();
                report.ExportToPdf(fileName);

                FileStream fileStream = File.OpenRead(fileName);
                x = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            }

            return x;
        }

        public byte[] GetCommercialInvoiceAsr(ClientInformation ClientInfo, int WaybillNo)
        {
            byte[] x = { };

            if (string.IsNullOrWhiteSpace(WaybillNo.ToString())
                || ClientInfo.CheckClientInfo(ClientInfo, false).HasError
                || !IsValidWBFormat(WaybillNo.ToString())
                || !IsAsrWaybill(ClientInfo.ClientID, WaybillNo))
                return x;

            App_Data.InfoTrackDataTableAdapters.ViwCommercialInvoiceASRTableAdapter adapter =
                new App_Data.InfoTrackDataTableAdapters.ViwCommercialInvoiceASRTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            List<rpInvoiceBarCodeAsr> lstBarCodeObj = InvoiceConnectionAsr(ClientInfo.ClientID, WaybillNo);
            if (lstBarCodeObj.Count() > 0)
            {
                Report.ClientCommercialInvoiceASR report = new Report.ClientCommercialInvoiceASR
                {
                    DataSource = lstBarCodeObj
                };

                string fileName = Server.MapPath(".") + "\\WaybillStickers\\" + WaybillNo.ToString() + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";
                report.CreateDocument();
                report.ExportToPdf(fileName);

                FileStream fileStream = File.OpenRead(fileName);
                x = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            }

            return x;
        }

        private List<rpInvoiceBarCode> InvoiceConnection(int ClientID, int WaybillNo)
        {
            List<rpInvoiceBarCode> BarCodeObj;
            //string sqlClientGroup = @"SELECT * FROM dbo.APIClientAndSubClient 
            //                WHERE pClientID = " + ClientID
            //                + @" OR cClientID = " + ClientID;
            string sqlReportData = @"SELECT ClientID, Consignee, ConsigneeAddress, ConsigneeEmail, ConsigneeMobileNo, 
ConsigneePhone, CountryofManufacture, CurrencyCode, CustomsCommodityCode, Description, 
ID, FORMAT(InvoiceDate, 'M/d/yyyy') as InvoiceDate, InvoiceNo, Quantity, RefNo, SubTotal, TotalCost, UnitCost, UnitType , Incoterm,IncotermsPlaceAndNotes
FROM dbo.ViwCommercialInvoice_API
where WaybillNo = " + WaybillNo + @"
AND ClientID = " + ClientID;

            //using (SqlConnection connection = new SqlConnection(sqlCon))
            //{
            //    int cnt = connection.Query(sqlClientGroup).Count();
            //    if (cnt > 0)
            //        sqlReportData += @" AND ClientID = " + ClientID;
            //}

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                BarCodeObj = connection.Query<rpInvoiceBarCode>(sqlReportData).ToList();
            }

            return BarCodeObj;
        }

        private List<rpInvoiceBarCodeAsr> InvoiceConnectionAsr(int ClientID, int WaybillNo)
        {
            List<rpInvoiceBarCodeAsr> BarCodeObj;
            string sqlClientGroup = @"SELECT * FROM dbo.APIClientAndSubClient 
                            WHERE pClientID = " + ClientID
                            + @" OR cClientID = " + ClientID;
            string sqlReportData = @"SELECT ClientID, Consignee, ConsigneeAddress, ConsigneeEmail, ConsigneeMobileNo, 
ConsigneePhone, CountryofManufacture, CurrencyCode, CustomsCommodityCode, Description, 
ID, InvoiceDate, InvoiceNo, Quantity, RefNo, SubTotal, TotalCost, UnitCost, UnitType ,
ClientName,ClientPhoneNumber , ClientMobile , ClientAddress ,'' AS Incoterm, '' AS IncotermsPlaceAndNotes  
FROM dbo.ViwCommercialInvoiceASR 
where InvoiceNo = '" + WaybillNo + "';";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                int cnt = connection.Query(sqlClientGroup).Count();
                if (cnt > 0)
                    sqlReportData += @" AND ClientID = " + ClientID;
            }

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                BarCodeObj = connection.Query<rpInvoiceBarCodeAsr>(sqlReportData).ToList();
            }

            return BarCodeObj;
        }

        /*[WebMethod(Description = "You can use this function to get Commercial Invoice file by InvoiceNo/WaybillNo as Byte[]")]
        public byte[] GetUAECommercialInvoice(ClientInformation clientInfo, string InvoiceNo)
        {
            byte[] x = { };

            if (InvoiceNo != null)
            {
                InfoTrack.NaqelAPI.Report.ClientCommericalInvoiceUAE report = new Report.ClientCommericalInvoiceUAE();

                if (GlobalVar.GV.PublishType != "Live")
                {
                    IEnumerable<XRControl> allReportControls = report.AllControls<XRControl>();
                    XRControl lbl = allReportControls.FirstOrDefault<XRControl>(p => p.Name == "lblCaption");
                    lbl.Text = "SAMPLE!!!!Please don't use";
                }

                InfoTrack.BusinessLayer.Report.Data.dsViwsTableAdapters.ViwCommercialInvoiceASRTableAdapter adapter =
                new InfoTrack.BusinessLayer.Report.Data.dsViwsTableAdapters.ViwCommercialInvoiceASRTableAdapter();
                report.dsViws.EnforceConstraints = false;

                dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                if (dc.ViwCommercialInvoices.Where(P => P.InvoiceNo == InvoiceNo && P.ClientID == clientInfo.ClientID).Count() > 0)
                    adapter.FillBy(report.dsViws.ViwCommercialInvoiceASR, dc.ViwCommercialInvoiceASRs.First(P => P.InvoiceNo == InvoiceNo && P.ClientID == clientInfo.ClientID).ID);

                string fileName = "";
                fileName = Server.MapPath(".") + "\\WaybillStickers\\" + InvoiceNo.ToString() + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";
                report.CreateDocument();
                report.ExportToPdf(fileName);

                FileStream fileStream = File.OpenRead(fileName);
                x = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            }

            return x;
        }
        */

        [WebMethod(Description = "You can use this function to update status of the shipment")]
        public Result ClientCallback(ClientInformation ClientInfo, Callback _callback)
        {
            Result result = new Result();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
                return result;

            result = IsClientCallbackValid(_callback, ClientInfo.ClientID);
            if (result.HasError)
                return result;

            //ClientCommercialInvoice oClientCommercialInvoice = new ClientCommercialInvoice();
            //oClientCommercialInvoice.RefNo = _commercialInvoice.RefNo;

            //dc.ClientCommercialInvoices.InsertOnSubmit(oClientCommercialInvoice);
            //dc.SubmitChanges();

            return result;
        }

        private Result IsClientCallbackValid(Callback _callback, int ClientID)
        {
            Result result = new Result();

            result.HasError = false;
            result.Message = "";

            if (string.IsNullOrWhiteSpace(_callback.docketNumber) || _callback.docketNumber.Length == 8)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNulldocketNumber");
                return result;
            }
            else if (string.IsNullOrWhiteSpace(_callback.status) || _callback.status.Length > 200)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullstatus");
                return result;
            }

            else if (string.IsNullOrWhiteSpace(_callback.remarks) || _callback.remarks.Length > 500)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullremarks");
                return result;
            }

            else if ((_callback.modifiedOn == null) || _callback.modifiedOn.Year < 2018)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullmodifiedOn");
                return result;
            }
            return result;
        }

        //[WebMethod(Description = "You can use this function to create a new waybill in the system with barcodes.")]
        public Result CreateWaybillWithPiecesBarCode(ManifestShipmentDetails _ManifestShipmentDetails, List<string> PiecesBarCodeList)
        {
            Result result = new Result();

            //_ManifestShipmentDetails.
            result = _ManifestShipmentDetails.ClientInfo.CheckClientInfo(_ManifestShipmentDetails.ClientInfo, true);

            if (result.HasError)
            {
                WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                return result;
            }

            result = _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeInfo(_ManifestShipmentDetails.ConsigneeInfo, _ManifestShipmentDetails.ClientInfo);
            if (result.HasError)
            {
                WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.RefNo, EnumList.MethodType.CreateWaybill, result);
                return result;
            }

            result = _ManifestShipmentDetails.IsWaybillDetailsValid(_ManifestShipmentDetails);
            if (result.HasError)
            {
                WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                return result;
            }

            if (_ManifestShipmentDetails.PicesCount != PiecesBarCodeList.Count)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErPiecesCountNotMatch");
                return result;
            }

            _ManifestShipmentDetails.DeliveryInstruction = GlobalVar.GV.GetString(_ManifestShipmentDetails.DeliveryInstruction, 200);
            CustomerWayBill NewWaybill = new CustomerWayBill();
            NewWaybill.ClientID = _ManifestShipmentDetails.ClientInfo.ClientID;
            NewWaybill.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
            NewWaybill.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;
            NewWaybill.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
            NewWaybill.ServiceTypeID = _ManifestShipmentDetails.ServiceTypeID;
            NewWaybill.BillingTypeID = _ManifestShipmentDetails.BillingType;
            if (NewWaybill.BillingTypeID == 5)
                NewWaybill.IsCOD = true;
            else
                NewWaybill.IsCOD = false;

            _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeData(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.ConsigneeInfo);
            NewWaybill.ConsigneeID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeID;
            NewWaybill.ConsigneeAddressID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID;

            NewWaybill.OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode));
            //NewWaybill.OriginStationID = Convert.ToInt32(_ManifestShipmentDetails.OriginStationID);

            NewWaybill.DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode, _ManifestShipmentDetails.ConsigneeInfo.CountryCode));
            //NewWaybill.DestinationStationID = Convert.ToInt32(_ManifestShipmentDetails.DestinationStationID);

            //if (GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode).HasValue)
            //    NewWaybill.ODAStationCharge = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode).Value;
            var odaStationId = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode);

            if (odaStationId.HasValue)
                NewWaybill.ODAStationCharge = odaStationId.Value;

            //if (GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).HasValue)
            //    NewWaybill.ODADestinationID = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode).Value;
            var odaDestinationId = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode);

            if (odaDestinationId.HasValue)
                NewWaybill.ODADestinationID = odaDestinationId.Value;
            NewWaybill.PickUpDate = DateTime.Now;
            NewWaybill.PicesCount = _ManifestShipmentDetails.PicesCount;
            NewWaybill.Weight = _ManifestShipmentDetails.Weight;
            NewWaybill.Width = 1;
            NewWaybill.Length = 1;
            NewWaybill.Height = 1;
            NewWaybill.VolumeWeight = 0.0002;
            NewWaybill.BookingRefNo = "";
            NewWaybill.ManifestedTime = DateTime.Now;

            if (_ManifestShipmentDetails.RefNo != "")
                NewWaybill.RefNo = GlobalVar.GV.GetString(_ManifestShipmentDetails.RefNo, 100); ;

            NewWaybill.IsPrintBarcode = false;
            NewWaybill.StatusID = 1;
            NewWaybill.IsInsurance = false;
            NewWaybill.DeclaredValue = _ManifestShipmentDetails.DeclareValue;
            NewWaybill.InsuredValue = 0;
            NewWaybill.PODDetail = "";
            NewWaybill.DeliveryInstruction = _ManifestShipmentDetails.DeliveryInstruction;
            NewWaybill.CODCharge = _ManifestShipmentDetails.CODCharge;
            NewWaybill.Discount = 0;

            //if (_ManifestShipmentDetails.CODCharge > 0)
            //    NewWaybill.IsCOD = true;
            //else
            //    NewWaybill.IsCOD = false;
            NewWaybill.NetCharge = 0;
            NewWaybill.OnAccount = 0;
            NewWaybill.ServiceCharge = 0;

            NewWaybill.ODAStationCharge = 0;
            NewWaybill.OtherCharge = 0;
            NewWaybill.PaidAmount = 0;
            NewWaybill.SpecialCharge = 0;
            NewWaybill.StandardShipment = 0;
            NewWaybill.StorageCharge = 0;
            NewWaybill.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
            NewWaybill.IsShippingAPI = true;

            if (_ManifestShipmentDetails.ClientInfo.ClientID == 1024600)
            {
                NewWaybill.IsRTO = _ManifestShipmentDetails.isRTO;
                NewWaybill.PODTypeID = null;
                NewWaybill.PODDetail = "";
            }
            else
                NewWaybill.IsRTO = false;
            NewWaybill.IsManifested = false;

            //if (_ManifestShipmentDetails.ODAOriginID.HasValue)
            //    NewWaybill.ODAOriginID = _ManifestShipmentDetails.ODAOriginID;

            //if (_ManifestShipmentDetails.ODADestinationID.HasValue)
            //    NewWaybill.ODADestinationID = _ManifestShipmentDetails.ODADestinationID;

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (WaybillNo > 0)
                NewWaybill.WayBillNo = WaybillNo;
            else
                NewWaybill.WayBillNo = GlobalVar.GV.GetWaybillNo(EnumList.ProductType.Home_Delivery);

            if (NewWaybill.WayBillNo < 1000)
            {
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details < 1000";
                return result;
            }

            try
            {
                dc.CustomerWayBills.InsertOnSubmit(NewWaybill);
                dc.SubmitChanges();
            }
            catch (Exception e)
            {
                LogException(e);
                WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details code : 120";

                GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetails.ClientInfo);
                //GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _ManifestShipmentDetails.ClientInfo);
            }

            if (!result.HasError)
            {
                result.WaybillNo = NewWaybill.WayBillNo;
                result.Key = NewWaybill.ID;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");

                if (_ManifestShipmentDetails.ClientInfo.ClientID != 9016808 && _ManifestShipmentDetails.CreateBooking == true)
                {
                    BookingShipmentDetails _bookingDetails = new BookingShipmentDetails();
                    _bookingDetails.ClientInfo = _ManifestShipmentDetails.ClientInfo;
                    _bookingDetails.ClientInfo.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
                    _bookingDetails.ClientInfo.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;

                    _bookingDetails.BillingType = _ManifestShipmentDetails.BillingType;
                    _bookingDetails.PicesCount = _ManifestShipmentDetails.PicesCount;
                    _bookingDetails.Weight = _ManifestShipmentDetails.Weight;
                    _bookingDetails.PickUpPoint = "";
                    _bookingDetails.SpecialInstruction = "";
                    _bookingDetails.OriginStationID = NewWaybill.OriginStationID;
                    _bookingDetails.DestinationStationID = NewWaybill.DestinationStationID;
                    _bookingDetails.OfficeUpTo = DateTime.Now;
                    _bookingDetails.PickUpReqDateTime = DateTime.Now;
                    _bookingDetails.ContactPerson = _ManifestShipmentDetails.ClientInfo.ClientContact.Name;
                    _bookingDetails.ContactNumber = _ManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber;
                    _bookingDetails.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
                    _bookingDetails.WaybillNo = result.WaybillNo;

                    Result BookingResult = new Result();
                    try
                    {
                        BookingResult = CreateBooking(_bookingDetails);
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                        BookingResult.HasError = true;
                    }

                    if (!BookingResult.HasError)
                        result.BookingRefNo = GlobalVar.GV.GetString(BookingResult.BookingRefNo, 100);
                }

                CustomerBarCode instanceBarcode;

                for (int i = 0; i <= NewWaybill.PicesCount - 1; i++)
                {
                    instanceBarcode = new CustomerBarCode();
                    instanceBarcode.BarCode = Convert.ToInt64(Convert.ToString(NewWaybill.WayBillNo) + i.ToString("D5"));
                    instanceBarcode.CustomerWayBillsID = NewWaybill.ID;
                    instanceBarcode.StatusID = 1;
                    instanceBarcode.CustomerPieceBarCode = PiecesBarCodeList[i];
                    dc.CustomerBarCodes.InsertOnSubmit(instanceBarcode);

                    dc.SubmitChanges();

                    // CustomerWaybillsBarCode instanceCustomerWaybillsBarCode = new  CustomerWaybillsBarCode();
                    //instanceCustomerWaybillsBarCode.StatusID = 1;
                    //instanceCustomerWaybillsBarCode.CustomerBarCodeID = instanceBarcode.ID;
                    //instanceCustomerWaybillsBarCode.PieceBarCode = PiecesBarCodeList[i];
                    //instanceCustomerWaybillsBarCode.CustomerWayBillsID = NewWaybill.ID;
                    //dc.CustomerWaybillsBarCodes.InsertOnSubmit(instanceCustomerWaybillsBarCode);
                    //dc.SubmitChanges();
                }
            }

            if (WaybillNo <= 0)
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
            else
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
            return result;
        }

        [WebMethod(Description = "")]
        public Result CancelRTOWaybill(ClientInformation ClientInfo, int OriginalWaybillID, int EmployID, int UserID)
        {
            Result result = new Result();

            if (ClientInfo.ClientID != 1024600)
            {
                result.HasError = true;
                result.Message = "You don't have a privillage to cancel the RTO.";
            }
            else
            {
                result = ClientInfo.CheckClientInfo(ClientInfo, false);
                if (result.HasError)
                    return result;

                dc = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (dc.Waybills.Where(P => P.ID == OriginalWaybillID).Count() > 0)
                {
                    Waybill instance = dc.Waybills.First(P => P.ID == OriginalWaybillID);

                    InfoTrack.BusinessLayer.GlobalVar.GV.DeveloperID = 15380;
                    InfoTrack.BusinessLayer.GlobalVar.GV.DeveloperPassword = "WSofan321";
                    InfoTrack.BusinessLayer.GlobalVar.GV.IPAddress = "InfoTrack.Desktop";
                    InfoTrack.BusinessLayer.GlobalVar.GV.IPAddress = "";


                    InfoTrack.BusinessLayer.DSet.ManifestDSTableAdapters.WaybillTableAdapter adapter = new
                        BusinessLayer.DSet.ManifestDSTableAdapters.WaybillTableAdapter();
                    InfoTrack.BusinessLayer.DSet.ManifestDS manifestDS = new BusinessLayer.DSet.ManifestDS();

                    adapter.FillByID(manifestDS.Waybill, OriginalWaybillID);

                    if (instance.IsRTO.HasValue && instance.IsRTO.Value == true)
                    {
                        if (!instance.Invoiced)
                        {
                            if (dc.CustomerWayBills.Where(P => P.WayBillNo == instance.WayBillNo &&
                                                                  P.StatusID == 1 &&
                                                                  P.ClientID != 1024600).Count() > 0)
                            {
                                if (dc.rpRTODatas.Where(P => P.WaybillID == OriginalWaybillID).Count() > 0)
                                {
                                    InfoTrack.Common.AuditInfo audit = new Common.AuditInfo();
                                    Waybill RTOWaybill = dc.Waybills.First(P => P.ID == dc.rpRTODatas.First(C => C.WaybillID == OriginalWaybillID).RTOWaybillID);
                                    CustomerWayBill customerInstance = dc.CustomerWayBills.First(P => P.WayBillNo == instance.WayBillNo &&
                                                                                                                  P.StatusID == 1 &&
                                                                                                                  P.ClientID != 1024600);
                                    instance.IsRTO = false;
                                    instance.BillingTypeID = customerInstance.BillingTypeID;
                                    instance.CollectedAmount = customerInstance.CODCharge;
                                    instance.RefNo = customerInstance.RefNo;
                                    instance.PODTypeID = customerInstance.PODTypeID != null ? customerInstance.PODTypeID : null;
                                    instance.ODADStationCharge = customerInstance.ODAStationCharge != null ? customerInstance.ODAStationCharge : 0;

                                    AutomaticManifest automaticManifest = new AutomaticManifest();
                                    automaticManifest.CalculateCharges(instance);
                                    automaticManifest.CheckAutomaticCharges(instance);
                                    automaticManifest.CalculateCharges(instance);

                                    RTOWaybill.IsCancelled = true;
                                    DeletingRowsReason deleteInstance = new DeletingRowsReason();
                                    deleteInstance.Date = DateTime.Now;
                                    deleteInstance.TableID = RTOWaybill.ID;
                                    deleteInstance.DeletingReasonID = 6;
                                    deleteInstance.EmployID = EmployID;
                                    deleteInstance.Notes = "Cancelling RTO for the main waybill : " + customerInstance.WayBillNo;
                                    deleteInstance.StatusID = 1;
                                    deleteInstance.DBTablesID = 3;

                                    //Delete RTO Record for the original Waybill
                                    if (dc.Deliveries.Where(P => P.WaybillID == OriginalWaybillID && P.StatusID == 1 && P.DeliveryStatusID == 11).Count() > 0)
                                    {
                                        Delivery deliveryInstance = dc.Deliveries.First(P => P.WaybillID == OriginalWaybillID && P.StatusID == 1 && P.DeliveryStatusID == 11);
                                        deliveryInstance.StatusID = 3;
                                        audit.InsertAudit("Delivery", InfoTrack.Common.GlobalVarCommon.OperationType.Delete, deliveryInstance.ID, UserID);
                                    }

                                    dcMaster.DeletingRowsReasons.InsertOnSubmit(deleteInstance);
                                    dcMaster.SubmitChanges();
                                    audit.InsertAudit("Waybill", InfoTrack.Common.GlobalVarCommon.OperationType.Delete, RTOWaybill.ID, UserID);

                                    if (manifestDS.Waybill.Rows.Count > 0)
                                        audit.AuditManage(instance, manifestDS.Waybill.Rows[0], false, OriginalWaybillID, UserID);
                                    result.HasError = false;
                                    result.Message = "RTO Cancelled Successfully.";
                                }
                                else
                                {
                                    result.HasError = true;
                                    result.Message = "The RTO waybill for the originial waybill is not clear in the Delivery.";
                                }
                            }
                            else
                            {
                                result.HasError = true;
                                result.Message = "Can't Cancel RTO Waybill, because there is no original copy of this waybill in the Customer Waybill. or the waybill is belong to Naqel Account";
                            }
                        }
                        else
                        {
                            result.HasError = true;
                            result.Message = "This waybill already invoiced.";
                        }
                    }
                    else
                    {
                        result.HasError = true;
                        result.Message = "This waybill is not an RTO waybill.";
                    }
                }
                else
                {
                    result.HasError = true;
                    result.Message = "Please check the waybill no.";
                }
            }

            return result;
        }

        [WebMethod(Description = "You can use this function to check the load types which you can use it.")]
        public List<ViwLoadTypeByClient> GetLoadTypeList(ClientInformation ClientInfo)
        {
            List<ViwLoadTypeByClient> LoadTypeList = new List<ViwLoadTypeByClient>();
            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                LoadTypeList = dcMaster.ViwLoadTypeByClients.Where(P => P.ClientID == ClientInfo.ClientID).ToList();
            }
            return LoadTypeList;
        }

        public Result ChangeClientPassword(ClientInformation ClientInfo, string OldPassword, string NewPassword)
        {
            Result result = new Result();

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            APIClientAccess ClientWebServiceInstance = new APIClientAccess();
            if (dcMaster.APIClientAccesses.Where(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1).Count() > 0)
            {
                APIClientAccess instance = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1);
                InfoTrack.Common.Security security = new InfoTrack.Common.Security();
                string pass = security.Decrypt(instance.ClientPassword.ToArray());
                if (instance.ClientPassword != security.Encrypt(OldPassword))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
                    return result;
                }
                else
                {
                    instance.ClientPassword = security.Encrypt(NewPassword);
                    dc.SubmitChanges();
                }
            }
            else
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
                return result;
            }

            return result;
        }

        private int GetParentClientID(int ClientID)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dcMaster = new MastersDataContext(con);
            List<int> ClientList = new List<int>();
            string sql = @"select top 1 ParentClientID from ClientIDGroup where ClientID = " + ClientID + @" and StatusID = 1;";
            using (var db = new SqlConnection(con))
            {
                ClientList = db.Query<int>(sql).ToList();
            }

            return ClientList.Count() > 0 ? ClientList[0] : ClientID;
        }

        private string GetCrossTrackingClientIDs(int ClientID)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dcMaster = new MastersDataContext(con);
            List<string> ClientStrList = new List<string>();
            string sql = @"
                    select stuff(
                    (select ',' + CONVERT(NVARCHAR(20), a.ClientID)
                    from ClientIDGroup a
                    inner join (
                    select ParentClientID 
                    from ClientIDGroup 
                    where ClientID = " + ClientID + @"
                    ) b on a.ParentClientID = b.ParentClientID
                    where a.StatusID = 1
                    FOR xml path('')
                    ), 1, 1, '')
                    ";
            using (var db = new SqlConnection(con))
            {
                ClientStrList = db.Query<string>(sql).ToList();
            }

            string tempClientIDs = (ClientStrList.Count() > 0 && ClientStrList[0] != null) ? ClientStrList[0] : ClientID.ToString();

            return tempClientIDs;
        }

        private string GetTrackingConnStr()
        {
            string con = ConfigurationManager.ConnectionStrings["TrackingConnectionString"].ConnectionString;
            string sql1 = @"select state_desc from sys.databases where name = 'ERPNaqel'";
            string sql2 = @"select state_desc from sys.databases where name = 'ERPCourierSE'";
            string dbStatusERPNaqel = "", dbStatusERPCourierSE = "";
            bool connectFailed = false;
            //bool DRconnectFailed = false;


            try
            {
                // Refer to: https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-databases-transact-sql?view=sql-server-ver15

                using (var db = new SqlConnection(con))
                {
                    dbStatusERPNaqel = db.Query<string>(sql1).ToList().First();
                    dbStatusERPCourierSE = db.Query<string>(sql2).ToList().First();
                }
            }
            catch (Exception ex)
            {
                connectFailed = true;
            }

            try
            {
                if (connectFailed)
                {
                    string con2 = ConfigurationManager.ConnectionStrings["SecDefaultsConnectionString"].ConnectionString;
                    using (var db = new SqlConnection(con2))
                    {
                        dbStatusERPNaqel = db.Query<string>(sql1).ToList().First();
                        dbStatusERPCourierSE = db.Query<string>(sql2).ToList().First();
                    }


                }
            }
            catch (Exception ex)
            {
                connectFailed = true;
            }


            if (DateTime.Now.Minute >= 58 ||
                DateTime.Now.Minute <= 5 ||
                dbStatusERPNaqel != "ONLINE" ||
                dbStatusERPCourierSE != "ONLINE" ||
                connectFailed)// add DR 125 restoration time 
                con = ConfigurationManager.ConnectionStrings["SecDefaultsConnectionString"].ConnectionString;

            //else if (dbStatusERPNaqel != "ONLINE" ||
            //                dbStatusERPCourierSE != "ONLINE" ||
            //                connectFailed)
            //    con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;


            return con;
        }

        //private string GetTrackingConnStr()
        //{
        //    string con = ConfigurationManager.ConnectionStrings["TrackingConnectionString"].ConnectionString;
        //    string dbStatusERPNaqel = "", dbStatusERPCourierSE = "";
        //    bool connectFailed = false;

        //    try
        //    {
        //        // Refer to: https://docs.microsoft.com/en-us/sql/relational-databases/system-catalog-views/sys-databases-transact-sql?view=sql-server-ver15
        //        string sql1 = @"select state_desc from sys.databases where name = 'ERPNaqel'";
        //        string sql2 = @"select state_desc from sys.databases where name = 'ERPCourierSE'";
        //        using (var db = new SqlConnection(con))
        //        {
        //            dbStatusERPNaqel = db.Query<string>(sql1).ToList().First();
        //            dbStatusERPCourierSE = db.Query<string>(sql2).ToList().First();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        connectFailed = true;
        //    }

        //    if (DateTime.Now.Minute >= 58 || DateTime.Now.Minute <= 5
        //        || dbStatusERPNaqel != "ONLINE" || dbStatusERPCourierSE != "ONLINE"
        //        || connectFailed)
        //        con = ConfigurationManager.ConnectionStrings["SecDefaultsConnectionString"].ConnectionString;

        //    return con;
        //}
        [WebMethod(Description = "You can use this function to trace your single waybill.")]
        public List<Tracking> TraceByWaybillNo(ClientInformation ClientInfo, int WaybillNo)
        {
            List<Tracking> Result = new List<Tracking>();

            #region Data Validation
            if (String.IsNullOrWhiteSpace(Convert.ToString(WaybillNo)) || !IsValidWBFormat(WaybillNo.ToString()))
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "Invalid WaybillNo format.",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }

            if (ClientInfo.ClientID != 1024600 && ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "Invalid credentials.",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }
            #endregion

            string list1 = System.Configuration.ConfigurationManager.AppSettings["TraceRTObyOriginal"].ToString();
            List<int> ClientwithRTOTracking = list1.Split(',').Select(Int32.Parse).ToList();

            string listTZ = System.Configuration.ConfigurationManager.AppSettings["TrackingWithTZ"].ToString();
            List<int> TrackingWithTZ = listTZ.Split(',').Select(Int32.Parse).ToList();
            bool IsNeedTZ = false;

            if (TrackingWithTZ.Contains(ClientInfo.ClientID))
                IsNeedTZ = true;

            List<ViwTracking> list = new List<ViwTracking>();
            string tempWbs = WaybillNo.ToString();
            string tempClientIDs = GetCrossTrackingClientIDs(ClientInfo.ClientID);
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            // Need combine RTO trackings if any
            if (ClientwithRTOTracking.Contains(ClientInfo.ClientID))
            {
                var tempRTO = dc.Waybills.Where(P => P.RefNo == WaybillNo.ToString() && P.ClientID == 1024600 && P.IsCancelled == false).ToList();

                if (tempRTO.Count() > 0)
                {
                    tempWbs = tempWbs + ", " + tempRTO.First().WayBillNo.ToString();
                    tempClientIDs += ", 1024600";
                }
            }

            string con = GetTrackingConnStr();
            using (var db = new SqlConnection(con))
            {
                string sql = @"
                select ClientID, WaybillNo, Date, StationCode, EventCode, Activity, ActivityAr, 
                HasError, ErrorMessage, Comments, RefNo, TrackingTypeID, DeliveryStatusID, DeliveryStatusMessage
                from ViwTrackingby_API9_0 where Waybillno in ("
                + tempWbs
                + @") --AND IsInternalType = 0 
                AND ClientID in ("
                + tempClientIDs
                + @")

                union
                select ClientID, WaybillNo, Date, StationCode, EventCode, Activity, ActivityAr, 
                HasError, ErrorMessage, Comments, RefNo, TrackingTypeID, DeliveryStatusID, DeliveryStatusMessage
                from ViwTrackingby_API9_0_ASR
                where WaybillNo in ("
                + tempWbs
                + ") AND ClientID in ("
                + tempClientIDs
                + @")

                union
                select ClientID, WaybillNo, '' as Date, '' as StationCode, '' as EventCode,
                Reference2 as Activity, '' as ActivityAr, 
                0 as HasError, '' as ErrorMessage, '' as Comments,
                RefNo, '' as TrackingTypeID, '' as DeliveryStatusID, '' as DeliveryStatusMessage
                from CustomerWayBills
                where LoadTypeID in (66, 136, 204 ,206)
                and StatusID = 3
                and WaybillNo in ("
                + tempWbs
                + ") AND ClientID in ("
                + tempClientIDs
                + @")

                Order by Date";

                list = db.Query<ViwTracking>(sql).ToList();
            }


            for (int i = 0; i < list.Count; i++)
            {
                var OriginalWB = dc.Waybills.FirstOrDefault(P => P.WayBillNo == WaybillNo && P.IsCancelled == false);
                DateTime trackingRecordDT = list[i].Date;

                Tracking newActivity = new Tracking
                {
                    ClientID = list[i].ClientID ?? ClientInfo.ClientID,
                    Date = trackingRecordDT,
                    Activity = Utf8Encoder.GetString(Utf8Encoder.GetBytes(list[i].Activity)), //list[i].Activity,
                    RefNo = list[i].RefNo,
                    ArabicActivity = Utf8Encoder.GetString(Utf8Encoder.GetBytes(list[i].ActivityAr ?? "")), //list[i].ActivityAr,
                    StationCode = list[i].StationCode,
                    WaybillNo = list[i].WaybillNo,
                    HasError = list[i].HasError,
                    ErrorMessage = list[i].ErrorMessage ?? "",
                    Comments = list[i].Comments ?? "",
                    ActivityCode = list[i].TrackingTypeID
                };

                if (IsNeedTZ)
                {
                    TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
                    newActivity.Date = TimeZoneInfo.ConvertTimeToUtc(trackingRecordDT, tzi);
                }

                //list[i].EventName = list[i].EventName;
                if (list[i].EventCode.HasValue)
                {
                    newActivity.EventCode = list[i].EventCode.Value;

                    if (newActivity.EventCode == 1)
                    {
                        // 9019912 - Picked up by Naqel at : RIYADH (Current one)
                        // 9030486 Pick up from Naqel Facility for AE LM
                        // 9030778 Pick up from Shein Facility for AE – SA road service
                        if (newActivity.ClientID == 9030486)
                            newActivity.Activity = "Pick up from Naqel Facility for AE LM";
                        else if (newActivity.ClientID == 9030778)
                            newActivity.Activity = "Pick up from Shein Facility for AE – SA road service";
                    }
                }
                if (list[i].DeliveryStatusID.HasValue)
                    newActivity.DeliveryStatusID = list[i].DeliveryStatusID.Value;
                if (list[i].DeliveryStatusID.HasValue)
                    newActivity.DeliveryStatusMessage = list[i].DeliveryStatusMessage;

                // If ASR Waybill already picked up, then this virtural tracking should be removed.
                if (OriginalWB != null
                    && (OriginalWB.LoadTypeID == 66 || OriginalWB.LoadTypeID == 136 || OriginalWB.LoadTypeID == 204 || OriginalWB.LoadTypeID == 206)
                    && newActivity.ActivityCode == 0
                    && newActivity.EventCode == 0
                    && newActivity.Activity.Contains("attempted"))
                    continue;

                // Remove the RTO Data Registered tracking
                if (newActivity.ClientID != ClientInfo.ClientID && newActivity.ClientID.ToString() == "1024600")
                {
                    if (newActivity.ActivityCode == 0)
                        continue;

                    newActivity.ClientID = ClientInfo.ClientID;
                    newActivity.RefNo = OriginalWB.RefNo;
                    newActivity.WaybillNo = WaybillNo;
                }

                if (IsNeedTZ)
                {
                    if (newActivity.ActivityCode == 6 && newActivity.EventCode == 164 && newActivity.DeliveryStatusID == 37) // Delivery exception - FD
                    {
                        Regex reg = new Regex(@"^202\d-\d{1,2}-\d{1,2}$");

                        string FdDate = "";
                        if (!string.IsNullOrWhiteSpace(newActivity.Comments) && reg.IsMatch(newActivity.Comments))
                        {
                            var tempDtList = newActivity.Comments.Trim().Split('-').ToList();
                            FdDate = tempDtList[0] + "-" + tempDtList[1].PadLeft(2, '0') + "-" + tempDtList[2].PadLeft(2, '0');
                        }
                        else
                            FdDate = trackingRecordDT.AddDays(1).ToString("yyyy-MM-dd");

                        newActivity.Activity = "Scheduled to <" + FdDate + "> " + newActivity.Activity;
                    }

                    if (newActivity.ActivityCode == 6) // Delivery exceptions to add CS contact number
                    {
                        newActivity.Activity += " - Please contact Naqel: 920020505";
                    }
                }

                Result.Add(newActivity);
            }

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Trace_Shipment_By_WaybillNo, WaybillNo.ToString(), 4);
            return Result;
        }

        private static readonly Encoding Utf8Encoder = Encoding.GetEncoding(
            "UTF-8",
            new EncoderReplacementFallback(string.Empty),
            new DecoderExceptionFallback()
            );

        [WebMethod(Description = "You can use this function to trace your multiple waybills.")]
        public List<Tracking> TraceByMultiWaybillNo(ClientInformation ClientInfo, List<int> WaybillNo)
        {
            List<int> _waybillNo = new List<int>();

            List<ViwTracking> TC = new List<ViwTracking>();
            List<Tracking> Result = new List<Tracking>();
            foreach (int item in WaybillNo)
            {
                if (item != 0 && IsValidWBFormat(item.ToString()))
                {
                    _waybillNo.Add(item);
                }
            }
            string WBno = string.Join(",", _waybillNo.ToArray());

            #region Data validation
            if (_waybillNo.Count() <= 0 || String.IsNullOrWhiteSpace(WBno))
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "Please provide a valid list of WaybillNo",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }

            if (WaybillNo.Count > 50)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "You can track maximum 50 WayBill in a call",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }
            //TODO: Confirm with Abeer about credencial validation  for 1024600
            if (ClientInfo.ClientID != 1024600 && ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "The username or password for this client is wrong, please make sure to pass correct credentials",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }
            #endregion

            Result = GetTrackingByMultiWaybillNos(ClientInfo.ClientID, WaybillNo);

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.TraceByMultiWaybillNo, string.Join(",", WaybillNo), 11);
            return Result;
        }

        private List<Tracking> GetTrackingByMultiWaybillNos(int ClientID, List<int> WaybillNo)
        {
            List<ViwTracking> TC = new List<ViwTracking>();
            List<Tracking> Result = new List<Tracking>();
            List<string> _waybillNoStr = new List<string>();
            List<int> _waybillNo = new List<int>();
            foreach (int item in WaybillNo)
            {
                if (item != 0 && IsValidWBFormat(item.ToString()))
                {
                    _waybillNo.Add(item);
                    _waybillNoStr.Add(item.ToString());
                }
            }
            string WBno = string.Join(",", _waybillNo.ToArray());

            // Get RTO WaybillNo
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            string WBno_RTO = "";
            string listNeedRTOTrackingClientIDs = System.Configuration.ConfigurationManager.AppSettings["TrackingWithRTO"].ToString();
            List<int> TrackingWithRTO = listNeedRTOTrackingClientIDs.Split(',').Select(Int32.Parse).ToList();
            if (TrackingWithRTO.Contains(ClientID))
            {
                var RTOWbs = dc.Waybills.Where(P => _waybillNoStr.Contains(P.RefNo) && P.ClientID == 1024600 && P.IsCancelled == false)
                  .Select(w => w.WayBillNo).ToList();

                WBno_RTO = string.Join(",", RTOWbs.ToArray());
            }

            if (ClientID == 1024600)
            {
                WBno_RTO = WBno;
            }

            string listTZ = System.Configuration.ConfigurationManager.AppSettings["TrackingWithTZ"].ToString();
            List<int> TrackingWithTZ = listTZ.Split(',').Select(Int32.Parse).ToList();
            bool IsNeedTZ = false;

            if (TrackingWithTZ.Contains(ClientID))
                IsNeedTZ = true;

            string con = GetTrackingConnStr();
            string sql_RTO = @"
                
                SELECT ClientID, WaybillNo, Date, StationCode, 

                CASE WHEN EventCode IN (1) THEN 601 -- RTO Pickup
                WHEN EventCode IN (7) THEN 603 -- RTO delivered
                WHEN EventCode IN (172) THEN 604 -- RTO delivered partially
                WHEN EventCode IN (113) THEN 605 -- RTO lost
                WHEN EventCode IN (115) THEN 606 -- RTO destroyed
                ELSE 602 -- RTO in transit 
                END AS EventCode,

                CASE WHEN EventCode IN (1) THEN '[RTO] Pickup - '
                WHEN EventCode IN (7) THEN '[RTO] delivered - '
                WHEN EventCode IN (172) THEN '[RTO] delivered partially - '
                WHEN EventCode IN (113) THEN '[RTO] lost - '
                WHEN EventCode IN (115) THEN '[RTO] destroyed - '
                ELSE '[RTO] in transit - '
                END + Activity AS Activity,

                '[RTO] ' + ActivityAr AS ActivityAr, 
                HasError, ErrorMessage, Comments, RefNo, TrackingTypeID, DeliveryStatusID, DeliveryStatusMessage

                FROM [dbo].[ViwTrackingby_API9_0] 

                WHERE ClientID = 1024600
                AND EventCode NOT IN (0, 15)

                AND WaybillNo in ("
               + WBno_RTO
               + ")";

            using (var db = new SqlConnection(con))
            {
                string tempClientIDs = GetCrossTrackingClientIDs(ClientID);
                string sql = @"
                select ClientID, WaybillNo, Date, StationCode, EventCode, Activity, ActivityAr, 
                HasError, ErrorMessage, Comments, RefNo, TrackingTypeID, DeliveryStatusID, DeliveryStatusMessage
                from ViwTrackingby_API9_0 where WaybillNo in ("
                + WBno
                + @") --AND IsInternalType = 0 
                AND ClientID in ("
                + tempClientIDs
                + @") 

                union
                select ClientID, WaybillNo, Date, StationCode, EventCode, Activity, ActivityAr, 
                HasError, ErrorMessage, Comments, RefNo, TrackingTypeID, DeliveryStatusID, DeliveryStatusMessage
                from ViwTrackingby_API9_0_ASR
                where WaybillNo in ("
                + WBno
                + ") AND ClientID in ("
                + tempClientIDs
                + @")

                union
                select ClientID, WaybillNo, '' as Date, '' as StationCode, '' as EventCode,
                Reference2 as Activity, '' as ActivityAr, 
                0 as HasError, '' as ErrorMessage, '' as Comments,
                RefNo, '' as TrackingTypeID, '' as DeliveryStatusID, '' as DeliveryStatusMessage
                from CustomerWayBills
                where LoadTypeID in (66, 136, 204, 206)
                and StatusID = 3
                and WaybillNo in ("
                + WBno
                + ") AND ClientID in ("
                + tempClientIDs
                + @")
                " + (string.IsNullOrWhiteSpace(WBno_RTO) ? "" : @"UNION" + sql_RTO)
                + @"
                Order by WaybillNo, Date";

                if (ClientID == 1024600)
                {
                    sql = sql_RTO;
                }



                TC = db.Query<ViwTracking>(sql).ToList();
            }


            var OriginalWBs = dc.Waybills.Where(P => _waybillNo.Contains(P.WayBillNo) && P.IsCancelled == false).ToList();

            for (int i = 0; i < TC.Count; i++)
            {
                DateTime trackingRecordDT = TC[i].Date;

                var OriginalWB = OriginalWBs.Where(p => p.WayBillNo == TC[i].WaybillNo).FirstOrDefault();
                Tracking newActivity = new Tracking
                {
                    ClientID = TC[i].ClientID ?? ClientID,
                    Date = trackingRecordDT,
                    Activity = TC[i].Activity == null ? "" : Utf8Encoder.GetString(Utf8Encoder.GetBytes(TC[i].Activity)),
                    RefNo = TC[i].RefNo,
                    ArabicActivity = TC[i].ActivityAr == null ? "" : Utf8Encoder.GetString(Utf8Encoder.GetBytes(TC[i].ActivityAr)),
                    StationCode = TC[i].StationCode,
                    WaybillNo = TC[i].WaybillNo,
                    HasError = TC[i].HasError,
                    ErrorMessage = TC[i].ErrorMessage ?? "",
                    Comments = TC[i].Comments ?? "",
                    ActivityCode = TC[i].TrackingTypeID
                };

                if (IsNeedTZ)
                {
                    TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");
                    newActivity.Date = TimeZoneInfo.ConvertTimeToUtc(trackingRecordDT, tzi);
                }

                if (TC[i].EventCode.HasValue)
                {
                    newActivity.EventCode = TC[i].EventCode.Value;

                    if (newActivity.EventCode == 1)
                    {
                        // 9019912 - Picked up by Naqel at : RIYADH (Current one)
                        // 9030486 Pick up from Naqel Facility for AE LM
                        // 9030778 Pick up from Shein Facility for AE – SA road service
                        if (newActivity.ClientID == 9030486)
                            newActivity.Activity = "Pick up from Naqel Facility for AE LM";
                        else if (newActivity.ClientID == 9030778)
                            newActivity.Activity = "Pick up from Shein Facility for AE – SA road service";
                    }
                }
                //TC[i].EventName = TC[i].EventName;

                //if (TC[i].EventCode.HasValue && (TC[i].EventCode == 0 || TC[i].EventCode == 27))
                //    newActivity.StationCode = "";

                if (TC[i].DeliveryStatusID.HasValue)
                    newActivity.DeliveryStatusID = TC[i].DeliveryStatusID.Value;
                if (TC[i].DeliveryStatusID.HasValue)
                    newActivity.DeliveryStatusMessage = TC[i].DeliveryStatusMessage;

                // If ASR Waybill already picked up, then this virtural tracking should be removed.
                if (OriginalWB != null
                    && (OriginalWB.LoadTypeID == 66 || OriginalWB.LoadTypeID == 136 || OriginalWB.LoadTypeID == 204 || OriginalWB.LoadTypeID == 206)
                    && newActivity.ActivityCode == 0
                    && newActivity.EventCode == 0
                    && newActivity.Activity.Contains("attempted"))
                    continue;

                // Change the RTO ClientID, WaybillNo, RefNo
                if (newActivity.ClientID.ToString() == "1024600" && ClientID != 1024600)
                {
                    var OriginWaybillInfo = OriginalWBs.Where(w => w.WayBillNo.ToString() == TC[i].RefNo).FirstOrDefault();

                    if (OriginWaybillInfo == null)
                        continue;

                    newActivity.ClientID = OriginWaybillInfo.ClientID;
                    newActivity.RefNo = OriginWaybillInfo.RefNo;
                    newActivity.WaybillNo = OriginWaybillInfo.WayBillNo;
                }


                if (IsNeedTZ)
                {
                    if (newActivity.ActivityCode == 6 && newActivity.EventCode == 164 && newActivity.DeliveryStatusID == 37) // Delivery exception - FD
                    {
                        Regex reg = new Regex(@"^202\d-\d{1,2}-\d{1,2}$");

                        string FdDate = "";
                        if (!string.IsNullOrWhiteSpace(newActivity.Comments) && reg.IsMatch(newActivity.Comments))
                        {
                            var tempDtList = newActivity.Comments.Trim().Split('-').ToList();
                            FdDate = tempDtList[0] + "-" + tempDtList[1].PadLeft(2, '0') + "-" + tempDtList[2].PadLeft(2, '0');
                        }
                        else
                            FdDate = trackingRecordDT.AddDays(1).ToString("yyyy-MM-dd");

                        newActivity.Activity = "Scheduled to <" + FdDate + "> " + newActivity.Activity;
                    }

                    if (newActivity.ActivityCode == 6) // Delivery exceptions to add CS contact number
                    {
                        newActivity.Activity += " - Please contact Naqel: 920020505";
                    }
                }

                Result.Add(newActivity);
            }

            Result = Result.OrderBy(r => r.Date).OrderBy(r => r.WaybillNo).ToList();

            return Result;
        }

        [WebMethod(Description = "You can use this function to track last status of your waybills.")]
        public List<WayBillTracking> MultiWayBillTracking(ClientInformation ClientInfo, List<int> WaybillNo)
        {
            //string con = ConfigurationManager.ConnectionStrings["TrackingConnectionString"].ConnectionString;
            string con = GetTrackingConnStr();
            List<int> _waybillNo = new List<int>();
            foreach (int item in WaybillNo)
            {
                if (item != 0)
                {
                    _waybillNo.Add(item);
                }
            }
            string WBno = string.Join(",", _waybillNo.ToArray());

            List<WayBillTracking> TC = new List<WayBillTracking>();

            if (WaybillNo.Count() <= 0 || String.IsNullOrWhiteSpace(WBno))
            {
                WayBillTracking newActivity = new WayBillTracking();
                newActivity.ErrorMessage = "Please provide a valid list of WaybillNo";
                newActivity.HasError = true;
                TC.Add(newActivity);
                return TC;
            }

            if (WaybillNo.Count > 500)
            {
                WayBillTracking newActivity = new WayBillTracking();
                newActivity.ErrorMessage = "You can track maximum 500 WayBill in a call";
                newActivity.HasError = true;
                TC.Add(newActivity);
                return TC;
            }

            if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    connection.Open();

                    //using (SqlCommand command = new SqlCommand("select * from rpShipmentLastStatus where WaybillNo in (" + WBno + ") ", connection))
                    string tempClientIDs = GetCrossTrackingClientIDs(ClientInfo.ClientID);
                    using (SqlCommand command = new SqlCommand(
                        "select * from rpShipmentLastStatus where WaybillNo in ("
                        + WBno
                        + ") AND ClientID in ("
                        + tempClientIDs
                        + @")"
                        , connection))
                    {
                        //Logger.Info(command.CommandText);
                        command.CommandTimeout = 0;

                        try
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    WayBillTracking TCF = new WayBillTracking();
                                    if (reader["ClientID"].ToString().Count() > 0)
                                        TCF.ClientID = Convert.ToInt32(reader["ClientID"].ToString());
                                    else
                                        TCF.ClientID = ClientInfo.ClientID;

                                    TCF.WaybillNo = Convert.ToInt32(reader["WaybillNo"].ToString());
                                    TCF.RefNo = reader["RefNo"].ToString();
                                    TCF.Org = reader["Origin"].ToString();
                                    TCF.Dest = reader["Destination"].ToString();
                                    TCF.PickUpDate = Convert.ToDateTime(reader["PickUpDate"]);
                                    TCF.Weight = reader["Weight"].ToString();
                                    TCF.ConsigneeName = reader["ConsigneeName"].ToString();

                                    if (reader["LastEventTime"].ToString().Count() > 0) // != null)
                                        TCF.LastEventTime = Convert.ToDateTime(reader["LastEventTime"]);
                                    else
                                        TCF.LastEventTime = DateTime.Now;

                                    TCF.LastEvent = reader["LastEvent"].ToString();
                                    TCF.HasError = false;

                                    TC.Add(TCF);
                                }
                            }
                            connection.Close();
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                WayBillTracking newActivity = new WayBillTracking();
                newActivity.PickUpDate = DateTime.Now;
                newActivity.LastEventTime = DateTime.Now;
                newActivity.ErrorMessage = "The username or password for this client is wrong., please make sure to pass correct credentials and waybillNo";
                newActivity.HasError = true;
                TC.Add(newActivity);
            }

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.MultiWayBillTracking, string.Join(",", WaybillNo), 20);
            return TC;

        }

        #region Create Range
        //[WebMethod]
        //public string GetMessage()
        //{
        //    return GlobalVar.GV.GetInfoTrackConnection();
        //}

        [WebMethod(Description = "Create Waybill Range.")]
        public WaybillRange CreateWaybillRange(ClientInformation ClientInfo)
        {
            WaybillRange result = new WaybillRange();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            int WaybillsCount = 0;
            InfoTrack.BusinessLayer.DContext.APIClientAccess aPIClientAccess = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientInfo.ClientID);
            WaybillsCount = aPIClientAccess.MaxRange;
            result = CheckBeforeCreateRange(ClientInfo, WaybillsCount);
            if (!result.HasError)
            {
                APIClientWaybillRange newrangeInstance = new APIClientWaybillRange();
                try
                {
                    string spName = "APICreateWaybillRange";
                    var w = new DynamicParameters();
                    w.Add("@ClientId", ClientInfo.ClientID);
                    w.Add(name: "@RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                    w.Add(name: "@FromWaybillNo", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    w.Add(name: "@ToWaybillNo", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    w.Add(name: "@InsertedID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    using (IDbConnection db = new SqlConnection(sqlCon))
                    {
                        //GetWaybillNo on Saving
                        var returnCode = db.Execute(spName, param: w, commandType: CommandType.StoredProcedure);
                        var retValue = w.Get<int>("@RetVal");


                        if (retValue == -1)
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceNotUsedAllWaybills");
                            return result;
                        }

                        newrangeInstance.FromWaybillNo = w.Get<int>("@FromWaybillNo");
                        newrangeInstance.ToWaybillNo = w.Get<int>("@ToWaybillNo");
                        newrangeInstance.ID = w.Get<int>("@InsertedID");

                    }

                }
                catch (Exception e)
                {
                    LogException(e);
                    result.HasError = true;
                    result.Message = "An error happen while creating waybill range.";
                    GlobalVar.GV.AddErrorMessage(e, ClientInfo);
                    GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, ClientInfo);
                }

                result.FromWaybillNo = newrangeInstance.FromWaybillNo;
                result.ToWaybillNo = newrangeInstance.ToWaybillNo;

                GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.CreateWaybillRange,
                                                  newrangeInstance.FromWaybillNo.ToString(), newrangeInstance.ID);
            }

            return result;
        }

        private WaybillRange CheckBeforeCreateRange(ClientInformation ClientInfo, int WaybillCount)
        {
            WaybillRange result = new WaybillRange();
            Result res = new Result();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            res = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (res.HasError)
            {
                result.HasError = res.HasError;
                result.Message = res.Message;
                return result;
            }

            APIClientAccess APIClient = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1);
            if (WaybillCount > APIClient.MaxRange)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceMaxWaybillCount") + APIClient.MaxRange.ToString();
                return result;
            }

            //Moved below validation to stored procedure

            //APIClientWaybillRange LastRange = new APIClientWaybillRange();

            //APIClientWaybillRange LastRange = dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1 && P.ToWaybillNo.ToString().StartsWith("2")).OrderByDescending(P=>P.ID).FirstOrDefault();

            //if (LastRange != null)
            //{
            //    //var x = from P in dcMaster.APIClientWaybillRanges
            //    //        where P.ClientID == ClientInfo.ClientID && P.StatusID == 1
            //    //        select P.ID;

            //    //int MaxID =  x.Max();
            //    //LastRange = dcMaster.APIClientWaybillRanges.First(P => P.ID == MaxID);
            //    double TotalPercentageRemaining = 0;
            //    TotalPercentageRemaining = (LastRange.TotalWaybillUsed * 100) / LastRange.TotalCount;
            //    TotalPercentageRemaining = (TotalPercentageRemaining - 100) * -1;

            //    if (TotalPercentageRemaining > APIClient.PoolPercentage)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceNotUsedAllWaybills");
            //        return result;
            //    }
            //}

            return result;
        }
        #endregion

        #region Create Range Commented
        ////[WebMethod]
        ////public string GetMessage()
        ////{
        ////    return GlobalVar.GV.GetInfoTrackConnection();
        ////}

        //[WebMethod(Description = " You can use this function to create Waybill Range")]
        //public WaybillRange CreateWaybillRange(ClientInformation ClientInfo)
        //{
        //    WaybillRange result = new WaybillRange();
        //    dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

        //    int WaybillsCount = 0;
        //    InfoTrack.BusinessLayer.DContext.APIClientAccess aPIClientAccess = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientInfo.ClientID);
        //    WaybillsCount = aPIClientAccess.MaxRange;
        //    result = CheckBeforeCreateRange(ClientInfo, WaybillsCount);
        //    if (!result.HasError)
        //    {
        //        APIClientWaybillRange newrangeInstance = new APIClientWaybillRange();
        //        newrangeInstance.ClientID = ClientInfo.ClientID;
        //        if (WaybillsCount == 0)
        //            WaybillsCount = 100;

        //        newrangeInstance.StatusID = 1;
        //        newrangeInstance.Date = DateTime.Now;
        //        newrangeInstance.TotalCount = 0;
        //        newrangeInstance.TotalWaybillUsed = 0;

        //        newrangeInstance.FromWaybillNo = GlobalVar.GV.GetWaybillNo(EnumList.ProductType.Home_Delivery);
        //        newrangeInstance.ToWaybillNo = newrangeInstance.FromWaybillNo + WaybillsCount - 1;


        //        dcMaster.APIClientWaybillRanges.InsertOnSubmit(newrangeInstance);
        //        dcMaster.SubmitChanges();

        //        result.FromWaybillNo = newrangeInstance.FromWaybillNo;
        //        result.ToWaybillNo = newrangeInstance.ToWaybillNo;

        //        GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.CreateWaybillRange,
        //                                          newrangeInstance.FromWaybillNo.ToString(), newrangeInstance.ID);
        //    }

        //    return result;
        //}

        //private WaybillRange CheckBeforeCreateRange(ClientInformation ClientInfo, int WaybillCount)
        //{
        //    WaybillRange result = new WaybillRange();
        //    Result res = new Result();
        //    dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

        //    res = ClientInfo.CheckClientInfo(ClientInfo, false);
        //    if (res.HasError)
        //    {
        //        result.HasError = res.HasError;
        //        result.Message = res.Message;
        //        return result;
        //    }

        //    APIClientAccess APIClient = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1);
        //    if (WaybillCount > APIClient.MaxRange)
        //    {
        //        result.HasError = true;
        //        result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceMaxWaybillCount") + APIClient.MaxRange.ToString();
        //        return result;
        //    }

        //    APIClientWaybillRange LastRange = new APIClientWaybillRange();
        //    if (dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1).Count() > 0)
        //    {
        //        var x = from P in dcMaster.APIClientWaybillRanges
        //                where P.ClientID == ClientInfo.ClientID && P.StatusID == 1
        //                select P.ID;

        //        int MaxID = x.Max();
        //        LastRange = dcMaster.APIClientWaybillRanges.First(P => P.ID == MaxID);
        //        double TotalPercentageRemaining = 0;
        //        TotalPercentageRemaining = (LastRange.TotalWaybillUsed * 100) / LastRange.TotalCount;
        //        TotalPercentageRemaining = (TotalPercentageRemaining - 100) * -1;

        //        if (TotalPercentageRemaining > APIClient.PoolPercentage)
        //        {
        //            result.HasError = true;
        //            result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceNotUsedAllWaybills");
        //            return result;
        //        }
        //    }

        //    return result;
        //}
        #endregion

        #region UpdateShipment
        [WebMethod(Description = "You can use this function to create waybill with given WaybillNo.")]
        public Result UpdateWaybill(ManifestShipmentDetails ManifestShipmentDetails, int WaybillNo)
        {
            Result result = new Result();

            WritetoXMLUpdateWaybill(ManifestShipmentDetails, ManifestShipmentDetails.ClientInfo, WaybillNo.ToString(), EnumList.MethodType.UpdateWaybill);

            result = ManifestShipmentDetails.ClientInfo.CheckClientInfo(ManifestShipmentDetails.ClientInfo, false);
            if (result.HasError)
            {
                WritetoXML(ManifestShipmentDetails, ManifestShipmentDetails.ClientInfo, ManifestShipmentDetails.RefNo, EnumList.MethodType.UpdateWaybill, result);
                return result;
            }

            result = CheckBeforeUpdateWaybill(ManifestShipmentDetails, WaybillNo);
            if (!result.HasError)
            {
                ManifestShipmentDetails.WaybillNo = WaybillNo;
                int request_load_type = ManifestShipmentDetails.LoadTypeID;
                this.WaybillNo = WaybillNo;
                string _fexdexAcc = System.Configuration.ConfigurationManager.AppSettings["FedexAccount"].ToString();
                List<int> _fexdexAcclist = _fexdexAcc.Split(',').Select(Int32.Parse).ToList();

                string inbound_loadtype = System.Configuration.ConfigurationManager.AppSettings["FedexInboundLoadType"].ToString();
                List<int> _inbound_loadtype = inbound_loadtype.Split(',').Select(Int32.Parse).ToList();
                string outbound_loadtype = System.Configuration.ConfigurationManager.AppSettings["FedexOutboundLoadType"].ToString();
                List<int> _outbound_loadtype = outbound_loadtype.Split(',').Select(Int32.Parse).ToList();
                // only allowed for Fedex
                if (_fexdexAcclist.Contains(ManifestShipmentDetails.ClientInfo.ClientID))

                {


                    //based on load type if inbound then refno is mandatory elif outbound ref1 will be mandatory
                    // add validation to check Reference1 against fedex account
                    //chck the piece count = count of itempiecelist
                    if (_inbound_loadtype.Contains(ManifestShipmentDetails.LoadTypeID))
                    {
                        if (string.IsNullOrEmpty(ManifestShipmentDetails.RefNo))
                        {

                            result.HasError = true;
                            result.Message = "Please Pass RefNo for inbound request";
                            return result;
                        }
                        bool isCountMatching = ManifestShipmentDetails.Itempieceslist.Count == ManifestShipmentDetails.PicesCount;
                        if (isCountMatching)
                        {
                            foreach (var item in ManifestShipmentDetails.Itempieceslist)
                            {
                                if (string.IsNullOrEmpty(item.PieceBarcode))
                                {
                                    isCountMatching = true;
                                    result.HasError = isCountMatching;
                                    result.Message = "Please Pass Piece Barcode same as of piece count";
                                    return result;  // If any PieceBarcode is empty, return false
                                }
                            }
                            ManifestShipmentDetails._CommercialInvoice.RefNo = "FedexRefno";
                        }
                        else
                        {
                            result.HasError = true;
                            result.Message = "Please Pass PieceBarCode for all Piece Item";
                            return result;
                        }
                    }

                    else if (_outbound_loadtype.Contains(ManifestShipmentDetails.LoadTypeID))
                    {
                        if (string.IsNullOrEmpty(ManifestShipmentDetails.Reference1))
                        {

                            result.HasError = true;
                            result.Message = "Please Pass Reference1 for outbound request";
                            return result;
                        }

                        //only for outbound
                        ManifestShipmentDetails.CreateBooking = true;
                        ManifestShipmentDetails._CommercialInvoice.RefNo = "FedexInvoice";
                    }
                    //if (ManifestShipmentDetails.Weight > 250)
                    //{
                    //    ManifestShipmentDetails.LoadTypeID = 7;
                    //}
                    //else if (ManifestShipmentDetails.Weight >= 10 && ManifestShipmentDetails.Weight <= 250)
                    //{
                    //    ManifestShipmentDetails.LoadTypeID = 39;
                    //}


                }


                result = CreateWaybill(ManifestShipmentDetails);
                if (result.HasError == false && Convert.ToString(result.WaybillNo) != null && result.WaybillNo > 0)
                {
                    if (_fexdexAcclist.Contains(ManifestShipmentDetails.ClientInfo.ClientID))
                    {
                        if (_inbound_loadtype.Contains(request_load_type))
                            result.HasError = UpdateCustomerBarCode(ManifestShipmentDetails);
                    }
                }
            }
            else
                WritetoXML(ManifestShipmentDetails, ManifestShipmentDetails.ClientInfo, WaybillNo.ToString(), EnumList.MethodType.UpdateWaybill, result);

            return result;
        }


        [WebMethod(Description = "You can use this function to create ASR waybill with given WaybillNo.")]
        public Result UpdateWaybillForASR(AsrManifestShipmentDetails _asrManifestShipmentDetails)
        {
            Result result = new Result();
            int waybillno = _asrManifestShipmentDetails.WaybillNo;

            WritetoXMLUpdateWaybill(_asrManifestShipmentDetails, _asrManifestShipmentDetails.ClientInfo, waybillno.ToString(), EnumList.MethodType.UpdateWaybillForASR);

            result = _asrManifestShipmentDetails.ClientInfo.CheckClientInfo(_asrManifestShipmentDetails.ClientInfo, false);
            if (result.HasError)
            {
                WritetoXML(_asrManifestShipmentDetails, _asrManifestShipmentDetails.ClientInfo, _asrManifestShipmentDetails.RefNo, EnumList.MethodType.UpdateWaybillForASR, result);
                return result;
            }

            result = CheckBeforeUpdateASRWaybill(_asrManifestShipmentDetails, waybillno);
            if (!result.HasError)
            {
                _asrManifestShipmentDetails.WaybillNo = waybillno;
                // this.WaybillNo = WaybillNo;
                result = CreateWaybillForASR(_asrManifestShipmentDetails);
            }
            else
                WritetoXML(_asrManifestShipmentDetails, _asrManifestShipmentDetails.ClientInfo, waybillno.ToString(), EnumList.MethodType.UpdateWaybillForASR, result);

            return result;
        }


        private Result CheckBeforeUpdateWaybill(ManifestShipmentDetails _ManifestShipmentDetails, int WaybillNo)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            Result result = new Result();

            //dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            //Check if the Waybill No is belonge to a ragne for the same client.
            //if (dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID &&
            //                                         P.StatusID == 1 &&
            //                                         P.FromWaybillNo <= WaybillNo &&
            //                                         P.ToWaybillNo >= WaybillNo).Count() <= 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillPassWrongWaybill");
            //    return result;
            //}

            //Check if waybill no already pass to the API Before.
            //if (dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID == 1).Count() > 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
            //    return result;
            //}


            //////////////////////////////////// NEW ////////////////////////////////////////
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("select * from APIClientWaybillRange where ClientID = " + _ManifestShipmentDetails.ClientInfo.ClientID + " AND FromWaybillNo <= " + WaybillNo +
                    " AND ToWaybillNo >= " + WaybillNo + "AND StatusID = 1 ", connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillPassWrongWaybill");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }

            ////////////////////////////////////////////// NEW //////////////////////////////
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("select * from CustomerWayBills where WaybillNo = " + WaybillNo +
                    " AND StatusID = 1 ", connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }
            ///////////////////////////////////////////////////////////////////////////////

            ////Dapper connection to check WB availability
            //using (IDbConnection db = new SqlConnection(sqlCon))
            //{
            //    string str = "select WaybillNo from CustomerWayBills where WaybillNo = " + WaybillNo + " and StatusID = 1";
            //    var WBNo = db.Query<CustomerWayBill>(str).FirstOrDefault();
            //    if (WBNo.WayBillNo > 0)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
            //        return result;
            //    }
            //}

            return result;
        }

        private Result CheckBeforeUpdateASRWaybill(AsrManifestShipmentDetails _asrManifestShipmentDetails, int WaybillNo)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            Result result = new Result();

            //dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            //Check if the Waybill No is belonge to a ragne for the same client.
            //if (dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == _ManifestShipmentDetails.ClientInfo.ClientID &&
            //                                         P.StatusID == 1 &&
            //                                         P.FromWaybillNo <= WaybillNo &&
            //                                         P.ToWaybillNo >= WaybillNo).Count() <= 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillPassWrongWaybill");
            //    return result;
            //}

            //Check if waybill no already pass to the API Before.
            //if (dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID == 1).Count() > 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
            //    return result;
            //}


            //////////////////////////////////// NEW ////////////////////////////////////////
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("select * from APIClientWaybillRange where ClientID = " + _asrManifestShipmentDetails.ClientInfo.ClientID + " AND FromWaybillNo <= " + WaybillNo +
                    " AND ToWaybillNo >= " + WaybillNo + "AND StatusID = 1 ", connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillPassWrongWaybill");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }

            ////////////////////////////////////////////// NEW //////////////////////////////
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("select * from CustomerWayBills where WaybillNo = " + WaybillNo +
                    " AND StatusID = 1 ", connection))
                {
                    //Logger.Info(command.CommandText);
                    command.CommandTimeout = 0;

                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                result.HasError = true;
                                result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
                                return result;
                            }
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        connection.Close();
                    }
                }
            }
            ///////////////////////////////////////////////////////////////////////////////

            ////Dapper connection to check WB availability
            //using (IDbConnection db = new SqlConnection(sqlCon))
            //{
            //    string str = "select WaybillNo from CustomerWayBills where WaybillNo = " + WaybillNo + " and StatusID = 1";
            //    var WBNo = db.Query<CustomerWayBill>(str).FirstOrDefault();
            //    if (WBNo.WayBillNo > 0)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GetLocalizationMessage("ErWebServiceWaybillAlreadyExists");
            //        return result;
            //    }
            //}

            return result;
        }

        #endregion
        [WebMethod(Description = "You can use this function to get the active range and the ranges which still some waybill not used in that range.")]
        public List<WaybillRange> GetActiveRanges(ClientInformation ClientInfo)
        {
            List<WaybillRange> result = new List<WaybillRange>();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                List<APIClientWaybillRange> listRange =
                    dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == ClientInfo.ClientID && P.StatusID == 1 && P.TotalCount > P.TotalWaybillUsed).ToList();

                for (int i = 0; i < listRange.Count; i++)
                {
                    APIClientWaybillRange instance = listRange[i];
                    WaybillRange newRange = new WaybillRange();
                    newRange.FromWaybillNo = instance.FromWaybillNo;
                    newRange.ToWaybillNo = instance.ToWaybillNo;
                    result.Add(newRange);
                }
            }

            return result;
        }

        [WebMethod(Description = "You can use this function to get the active range..")]
        public List<WaybillRange> GetRangesByClientId(int _clientId)
        {
            AddAPIError(_clientId, "GetRange");

            List<WaybillRange> result = new List<WaybillRange>();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            int LastwaybillNoInCustomerWaybill = 0;
            int LastwaybillNoInRange = 0;
            using (var db = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
            {
                LastwaybillNoInCustomerWaybill = db.Query<CustomerWayBill>("select top 1 Waybillno from CustomerWayBills with (NOLOCK) where ClientID =" + _clientId + " Order by waybill desc").FirstOrDefault().WayBillNo;
                LastwaybillNoInRange = db.Query<CustomerWayBill>("select top 1 Waybillno from CustomerWayBills with (NOLOCK) where ClientID =" + _clientId + " Order by waybill desc").FirstOrDefault().WayBillNo;
            }

            APIClientAccess _client = dcMaster.APIClientAccesses.FirstOrDefault(p => p.ClientID == _clientId);
            if (_client != null)
            {
                List<APIClientWaybillRange> listRange =
                    dcMaster.APIClientWaybillRanges.Where(P => P.ClientID == _client.ClientID && P.StatusID == 1 && P.TotalCount > P.TotalWaybillUsed).ToList();

                for (int i = 0; i < listRange.Count; i++)
                {
                    APIClientWaybillRange instance = listRange[i];
                    WaybillRange newRange = new WaybillRange();
                    newRange.FromWaybillNo = instance.FromWaybillNo;
                    newRange.ToWaybillNo = instance.ToWaybillNo;
                    result.Add(newRange);
                }
            }

            return result;
        }

        public string GetPathFileName()
        {
            return Server.MapPath(".");
        }

        private void WritetoXML(Object myObject, ClientInformation _ClientInfo, string reference, EnumList.MethodType methodType, Object Result)
        {
            try
            {
                string filePath = Server.MapPath(".") + "\\ErrorData\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + _ClientInfo.ClientID.ToString() + "\\";
                Directory.CreateDirectory(filePath);
                string safeReference = Regex.Replace(reference ?? "", @"[^\w-]", "");
                if (string.IsNullOrWhiteSpace(safeReference))
                    safeReference = "NoRef_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                string fileName = filePath + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + safeReference + "_" + DateTime.Now.ToFileTimeUtc() + ".xml";
                //System.IO.File.Create(fileName);

                

                FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
                StreamWriter str = new StreamWriter(fs);
                str.Close();
                fs.Close();

                XmlDocument xmlDoc = new XmlDocument();
                XmlSerializer xmlSerializer = new XmlSerializer(myObject.GetType());
                using (MemoryStream xmlStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(xmlStream, myObject);
                    xmlStream.Position = 0;
                    xmlDoc.Load(xmlStream);
                    xmlDoc.Save(fileName);
                }

                fileName = filePath + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + reference + "_" + DateTime.Now.ToFileTimeUtc() + "Error.xml";
                xmlSerializer = new XmlSerializer(Result.GetType());
                using (MemoryStream xmlStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(xmlStream, Result);
                    xmlStream.Position = 0;
                    xmlDoc.Load(xmlStream);
                    xmlDoc.Save(fileName);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                GlobalVar.GV.InsertError(ex.Message, "15380");
            }
        }

        private void WritetoXMLUpdateWaybill(Object myObject, ClientInformation _ClientInfo, string reference, EnumList.MethodType methodType)
        {
            try
            {
                string pattern = @"[^\w-]"; // Keep letters, numbers, underscores, and hyphens
                string resultRefNo = Regex.Replace(reference, pattern, "");

                string filePath = Server.MapPath(".") + "\\UpdateWaybillData\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\" + _ClientInfo.ClientID.ToString() + "\\";
                Directory.CreateDirectory(filePath);
                

                if (string.IsNullOrWhiteSpace(resultRefNo))
                    resultRefNo = "NoRef_" + Guid.NewGuid().ToString("N").Substring(0, 4);

                string fileName = filePath + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + resultRefNo + "_" + DateTime.Now.ToFileTimeUtc() + ".xml";

                

                FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
                StreamWriter str = new StreamWriter(fs);
                str.Close();
                fs.Close();

                XmlDocument xmlDoc = new XmlDocument();
                XmlSerializer xmlSerializer = new XmlSerializer(myObject.GetType());
                using (MemoryStream xmlStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(xmlStream, myObject);
                    xmlStream.Position = 0;
                    xmlDoc.Load(xmlStream);
                    xmlDoc.Save(fileName);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                GlobalVar.GV.InsertError(ex.Message, "15380");
            }
        }

        public class LogOrderData
        {
            public int WaybillNo { get; set; }
            public int ClientID { get; set; }
            public string RefNo { get; set; }
            public DateTime? ManifestedTime { get; set; } = DateTime.Now;
            public bool? IsShippingAPI { get; set; } = false;
        }

        public enum RequestType : int
        {
            CreateWaybill = 1, // will be available for CreateWaybill/CreateWaybillAlt/CreateWaybillForASR/CreateWaybillForASRAlt
            UpdateWaybill = 3,
            GetLabelSticker = 8,
            CancelWaybill = 9,
            CancelRTO = 10,
            UpdateReweight = 11
        }

        [WebMethod()]
        public LogFilesResult GetRequest(ClientInformation ClientInfo, string Secret, int WaybillNo, RequestType Method = RequestType.CreateWaybill)
        {
            LogFilesResult result = new LogFilesResult() { HasError = true };

            #region Data validation
            if (ClientInfo == null)
            {
                result.Message = "ClientInfo is required.";
                return result;
            }

            if (ClientInfo.ClientID != 9020077)
            {
                result.Message = "Your account has no permission to call this function.";
                return result;
            }

            if (!IsValidWBFormat(WaybillNo.ToString()))
            {
                result.Message = "Invalid WaybillNo.";
                return result;
            }

            Regex rg = new Regex(@"^ITLog20\d{6}[a-zA-Z]{1}$"); // e.g. ITLog20230414a
            if (string.IsNullOrWhiteSpace(Secret) || !rg.IsMatch(Secret))
            {
                result.Message = "Invalid secret.";
                return result;
            }

            string tempSecret = "ITLog" + DateTime.Now.ToString("yyyyMMdd"); // e.g. ITLog20230414
            if (Secret.Substring(0, tempSecret.Length) != tempSecret)
            {
                result.Message = "Wrong secret.";
                return result;
            }
            #endregion

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            var logOrderData = dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.IsShippingAPI == true).Select(w => new LogOrderData
            {
                WaybillNo = w.WayBillNo,
                ClientID = w.ClientID,
                RefNo = w.RefNo,
                ManifestedTime = w.ManifestedTime,
                IsShippingAPI = w.IsShippingAPI
            })
                .FirstOrDefault();

            if (logOrderData == null)
            {
                result.Message = "Waybill is not pushed by API.";
                return result;
            }

            string pattern = @"[^\w-]"; // Keep letters, numbers, underscores, and hyphens
            string resultRefNo = Regex.Replace(logOrderData.RefNo, pattern, "");

            List<RequestFileData> fileResult = new List<RequestFileData>();
            List<FileInfo> fileInfos = new List<FileInfo>() { };

            if (Method == RequestType.CreateWaybill || Method == RequestType.UpdateWaybill)
            {
                string filePathCurrentDay = Server.MapPath(".") + "\\UpdateWaybillData\\" + ((DateTime)logOrderData.ManifestedTime).ToString("yyyy-MM-dd") + "\\" + logOrderData.ClientID.ToString() + "\\";
                string filePathPreviousDay = Server.MapPath(".") + "\\UpdateWaybillData\\" + ((DateTime)logOrderData.ManifestedTime).AddDays(-1).ToString("yyyy-MM-dd") + "\\" + logOrderData.ClientID.ToString() + "\\";
                string fileName = logOrderData.ClientID.ToString() + "_CreateWaybill*_" + resultRefNo + "_*.xml";
                if (Method == RequestType.UpdateWaybill)
                    fileName = logOrderData.ClientID.ToString() + "_UpdateWaybill_" + logOrderData.WaybillNo.ToString() + "_*.xml";

                if (Directory.Exists(filePathCurrentDay))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(filePathCurrentDay);
                    fileInfos.AddRange(directoryInfo.GetFiles(fileName));
                }

                if (Directory.Exists(filePathPreviousDay))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(filePathPreviousDay);
                    fileInfos.AddRange(directoryInfo.GetFiles(fileName));
                }
            }
            else // getLabelSticker / CancelRTO / CancelWaybill
            {
                string filePathCurrentDay = Server.MapPath(".") + "\\UpdateWaybillData\\";
                string fileName = logOrderData.ClientID.ToString() + "_" + Method.ToString() + "_" + logOrderData.WaybillNo.ToString() + "_*.xml";

                DirectoryInfo directoryInfo = new DirectoryInfo(filePathCurrentDay);
                fileInfos.AddRange(directoryInfo.GetFiles(fileName, SearchOption.AllDirectories));
            }

            if (fileInfos.Count() == 0)
                result.Message = "Log file not found!";

            foreach (FileInfo foundFile in fileInfos)
            {
                var requestFile = new RequestFileData();
                string fullName = foundFile.FullName;
                requestFile.FileName = foundFile.Name;

                var content = File.ReadAllText(fullName);
                requestFile.Content = content;

                fileResult.Add(requestFile);
            }

            result.FileData = fileResult;
            result.HasError = false;
            return result;
        }

        private void SaveToLogFile(int ClientID, string RefNo, string Message)
        {
            return;
            var ClientListToRecord = new List<int>() { 9020985, 9020114, 9021332, 9021627, 9021628, 9021629, 9021630, 9021900, 9020077,
                9021625, 9020282, 9020281, 9018846, 9018772, 9018498, 9017951, 9017061, 9017218, 9018426, 9018427, 9018892, 9019266,
                9019273, 9020922, 9020923, 9020924, 9020925, 9020926, 9020969, 9020970, 9020971, 9021103, 9021136, 9021160, 9019267 };

            if (!ClientListToRecord.Contains(ClientID))
                return;

            var directory = Server.MapPath(".") + "\\RequestTimeLog\\" + ClientID.ToString() + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "\\";
            string path = directory + @"RequestLog_" + ClientID.ToString() + "_" + DateTime.Today.ToString("yyyyMMdd") + ".txt";
            Directory.CreateDirectory(directory);
            StreamWriter sw = File.AppendText(path);
            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK") + " | " + ClientID + " | " + RefNo + " | " + Message);
            sw.Dispose();
        }

        [WebMethod(Description = " Check if waybill already exists in the system.")]
        public bool IsWaybillExists(ClientInformation ClientInfo, int WaybillNo)
        {
            bool exists = false;

            Result result = new Result();
            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
                return true;

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo).Count() > 0)
                exists = true;
            return exists;
        }

        [WebMethod(Description = "You can use this function to hold shipment from delivery.")]
        public HoldingShipmentResult HoldShipmentFromDelivery(int WaybillNo, ClientInformation ClientInfo)
        {
            HoldingShipmentResult result = new HoldingShipmentResult();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcHHD = new HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {

                if (dc.Waybills.Where(P => P.WayBillNo == WaybillNo && P.DeliveryID > 0 && P.IsCancelled == false).Count() > 0 ||
                    dcHHD.OnDeliveries.Where(P => P.WaybillNo == WaybillNo).Count() > 0)
                {
                    result.ShipmentHold = false;
                    result.Notes = "Shipment Already delivered";
                }
                else
                    if (dc.rpDeliverySheets.Where(P => P.WayBillNo == WaybillNo &&
                    P.Date.Year == DateTime.Now.Year &&
                    P.Date.Month == DateTime.Now.Month &&
                    P.Date.Day == DateTime.Now.Day).Count() > 0)
                {
                    result.ShipmentHold = false;
                    result.Notes = "Shipment Already Went With Driver For Delivery.";
                }
                else
                {
                    APIClientRequestforHoldDelivery instance = new APIClientRequestforHoldDelivery();

                    instance.WaybillNo = WaybillNo;
                    instance.Date = DateTime.Now;
                    instance.StatusID = 1;

                    dc.APIClientRequestforHoldDeliveries.InsertOnSubmit(instance);
                    dc.SubmitChanges();

                    result.ShipmentHold = true;
                    result.Notes = "Shipment Hold Successfully.";
                }
            }
            else
            {
                result.ShipmentHold = false;
                result.Notes = "Check Client Info Data.";
            }

            return result;
        }

        [WebMethod(Description = "You can use this function to get Naqel Waybill No as per your Ref No.")]
        public Result GetWaybillNoByRefNo(string RefNo, ClientInformation ClientInfo)
        {
            Result result = new Result();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                if (dc.CustomerWayBills.Where(P => P.ClientID == ClientInfo.ClientID &&
                                                   P.RefNo.ToLower().ToString() == RefNo.ToLower().ToString() &&
                                                   P.StatusID == 1).Count() > 0)
                {
                    CustomerWayBill CWaybill = dc.CustomerWayBills.First(P => P.ClientID == ClientInfo.ClientID &&
                                                                                      P.RefNo.ToLower().ToString() == RefNo.ToLower().ToString() &&
                                                                                      P.StatusID == 1);

                    result.HasError = false;
                    result.WaybillNo = CWaybill.WayBillNo;
                }
                else
                    if (dc.Waybills.Where(P => P.ClientID == ClientInfo.ClientID &&
                                                   P.RefNo.ToLower().ToString() == RefNo.ToLower().ToString() &&
                                                   P.IsCancelled == false).Count() > 0)
                {
                    Waybill CWaybill = dc.Waybills.First(P => P.ClientID == ClientInfo.ClientID &&
                                                                      P.RefNo.ToLower().ToString() == RefNo.ToLower().ToString() &&
                                                                      P.IsCancelled == false);

                    result.HasError = false;
                    result.WaybillNo = CWaybill.WayBillNo;
                }
                else
                {
                    result.HasError = true;
                    result.WaybillNo = 0;
                    result.Message = "There is no shipment have this Ref No.";
                }
            }
            else
            {
                result.HasError = true;
                result.Message = "Wrong Client Information";
            }
            return result;
        }

        [WebMethod(Description = "You can use this function to create Return Waybill by using existing waybill no data.")]
        public RTOData CreateRTOWaybill(ClientInformation _ClientInfo, int WaybillNo)
        {
            AddAPIError(_ClientInfo.ClientID, "CrtRTOWB");

            RTOData returnRTO = new RTOData();
            Result result = new Result();
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            result = _ClientInfo.CheckClientInfo(_ClientInfo, false);

            //if (result.HasError)
            //    return result;

            if (!result.HasError)
            {
                if (dc.Waybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false && P.ClientID == _ClientInfo.ClientID).Count() > 0 ||
                    dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID == 1 && P.ClientID == _ClientInfo.ClientID).Count() > 0)
                {
                    #region By Waybill Table
                    //if (dc.Waybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false && P.ClientID == _ClientInfo.ClientID).Count() > 0)
                    //{
                    //     Waybill InstanceWaybill = dc.Waybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false && P.ClientID == _ClientInfo.ClientID);
                    //    ManifestShipmentDetails instance = new ManifestShipmentDetails();
                    //    instance.BillingType = InstanceWaybill.BillingTypeID;

                    //    instance.ClientInfo = new ClientInformation();
                    //    instance.ClientInfo.ClientAddress = new ClientAddress();
                    //    instance.ClientInfo.ClientContact = new ClientContact();

                    //    instance.ClientInfo.ClientID = _ClientInfo.ClientID;
                    //    instance.ClientInfo.Password = _ClientInfo.Password;
                    //    instance.ClientInfo.Version = "1.0";
                    //    //To Do instance.ClientInfo.ClientAddress.CityCode = GlobalVar.GV.GetCityCodeByStationID(

                    //    instance.CODCharge = 0;
                    //    instance.ConsigneeInfo = new ConsigneeInformation();
                    //    //To Do instance.ConsigneeInfo.Address=

                    //    instance.CreateBooking = false;
                    //    instance.DeliveryInstruction = InstanceWaybill.DeliveryInstruction;

                    //    instance.DestinationStationID = InstanceWaybill.OriginStationID;
                    //    instance.OriginStationID = InstanceWaybill.DestinationStationID;

                    //    instance.GeneratePiecesBarCodes = true;
                    //    instance.isRTO = true;
                    //    instance.LoadTypeID = InstanceWaybill.LoadTypeID;
                    //    instance.PicesCount = Convert.ToInt32(InstanceWaybill.PicesCount);
                    //    //instance.RefNo = InstanceWaybill.RefNo;
                    //    instance.ServiceTypeID = InstanceWaybill.ServiceTypeID;
                    //    instance.Weight = InstanceWaybill.Weight;

                    //    result = CreateWaybill(instance);
                    //}
                    //else

                    #endregion
                    if (dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID == 1 && P.ClientID == _ClientInfo.ClientID).Count() > 0)
                    {
                        CustomerWayBill InstanceWaybill = dc.CustomerWayBills.First(P => P.WayBillNo == WaybillNo && P.StatusID == 1 && P.ClientID == _ClientInfo.ClientID);

                        Consignee InstanceConsignee = dcMaster.Consignees.First(P => P.ID == InstanceWaybill.ConsigneeID);
                        ConsigneeDetail InstanceConsigneeDetail = dcMaster.ConsigneeDetails.First(P => P.ID == InstanceWaybill.ConsigneeAddressID);
                        Client InstanceClient = dcMaster.Clients.First(P => P.ID == InstanceWaybill.ClientID);
                        InfoTrack.BusinessLayer.DContext.ClientAddress InstanceClientAddress = dcMaster.ClientAddresses.First(P => P.ID == InstanceWaybill.ClientAddressID);
                        InfoTrack.BusinessLayer.DContext.ClientContact InstanceClientContact = dcMaster.ClientContacts.First(P => P.ID == InstanceWaybill.ClientContactID);

                        #region Old

                        ManifestShipmentDetails instance = new ManifestShipmentDetails();
                        instance.BillingType = InstanceWaybill.BillingTypeID;

                        instance.ClientInfo = new ClientInformation();
                        instance.ClientInfo.ClientAddress = new ClientAddress();
                        instance.ClientInfo.ClientContact = new ClientContact();

                        instance.ClientInfo.ClientID = _ClientInfo.ClientID;
                        instance.ClientInfo.Password = _ClientInfo.Password;
                        instance.ClientInfo.Version = "1.0";

                        //To Do instance.ClientInfo.ClientAddress.CityCode = GlobalVar.GV.GetCityCodeByStationID(
                        instance.ClientInfo.ClientAddress = new ClientAddress();
                        instance.ClientInfo.ClientAddress.CityCode = GlobalVar.GV.GetCityISOCityCodeByCityID(InstanceConsigneeDetail.CityID.Value);
                        instance.ClientInfo.ClientAddress.CountryCode = "KSA";
                        instance.ClientInfo.ClientAddress.Fax = InstanceConsigneeDetail.Fax;
                        instance.ClientInfo.ClientAddress.FirstAddress = InstanceConsigneeDetail.FirstAddress;
                        instance.ClientInfo.ClientAddress.Location = "";
                        instance.ClientInfo.ClientAddress.PhoneNumber = InstanceConsigneeDetail.PhoneNumber;
                        instance.ClientInfo.ClientAddress.POBox = "";
                        instance.ClientInfo.ClientAddress.ZipCode = "";

                        instance.ClientInfo.ClientContact = new ClientContact();
                        instance.ClientInfo.ClientContact.Email = InstanceConsignee.Email;
                        instance.ClientInfo.ClientContact.MobileNo = InstanceConsigneeDetail.Mobile;
                        instance.ClientInfo.ClientContact.Name = InstanceConsignee.Name;
                        instance.ClientInfo.ClientContact.PhoneNumber = InstanceConsigneeDetail.PhoneNumber;

                        instance.CODCharge = 0;
                        instance.ConsigneeInfo = new ConsigneeInformation();
                        //To Do instance.ConsigneeInfo.Address=
                        instance.ConsigneeInfo.Address = InstanceClientAddress.FirstAddress;
                        instance.ConsigneeInfo.CityCode = GlobalVar.GV.GetCityISOCityCodeByCityID(InstanceClientAddress.CityID);
                        instance.ConsigneeInfo.ConsigneeName = InstanceClientContact.Name;
                        instance.ConsigneeInfo.CountryCode = "KSA";
                        instance.ConsigneeInfo.Email = InstanceClient.Email;
                        instance.ConsigneeInfo.Fax = "0";
                        instance.ConsigneeInfo.Mobile = InstanceClientContact.Mobile;
                        instance.ConsigneeInfo.PhoneNumber = InstanceClientContact.PhoneNumber;

                        instance.CreateBooking = false;
                        instance.DeliveryInstruction = InstanceWaybill.DeliveryInstruction;
                        instance.GeneratePiecesBarCodes = true;
                        instance.LoadTypeID = InstanceWaybill.LoadTypeID;
                        instance.PicesCount = Convert.ToInt32(InstanceWaybill.PicesCount);
                        instance.Weight = InstanceWaybill.Weight;
                        //instance.RefNo = InstanceWaybill.RefNo.ToString();
                        instance.RefNo = InstanceWaybill.WayBillNo.ToString(); // adding old Waybill as a RefNo to new RTOWaybill
                        instance.Reference1 = InstanceWaybill.RefNo.ToString(); // adding RefNo of old Waybill to new RTOWaybill in Reference1

                        instance.isRTO = true;

                        #endregion

                        result = CreateWaybill(instance);
                        if (!result.HasError)
                        {
                            result.HasError = true;
                            result.Message = "Error creating RTO Waybill, Please try again ..";
                        }

                        // return RTO Info
                        returnRTO.ClientID = instance.ClientInfo.ClientID;
                        returnRTO.RTOWaybillNo = result.WaybillNo;
                        returnRTO.WaybillNo = instance.RefNo;

                    }
                }
                else
                {
                    result.HasError = true;
                    result.Message = "Please check the waybill no, it look waybill no is not exists or waybill no is belong to you.";
                }
            }
            else
            {
                result.HasError = true;
                result.Message = "Client Information not correct, please Check ..";
            }

            return returnRTO;
        }

        [WebMethod(Description = "You can use this function to check if the waybill delivered or not. ( True means waybill delivered to consignee, False means not delivered).")]
        public bool IsWaybillDelivered(ClientInformation _ClientInfo, int WaybillNo)
        {
            bool IsDeliverd = false;

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dc.Waybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).Count() > 0)
            {
                if (dc.Waybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).DeliveryID.HasValue)
                    IsDeliverd = true;
            }

            return IsDeliverd;
        }

        [WebMethod]
        public RouteOptimizationDataType.DefaultResult AddNewCall(RouteOptimizationDataType.NewCallRequest instance)
        {
            RouteOptimizationDataType.DefaultResult result = new RouteOptimizationDataType.DefaultResult();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.CallingHistory callingHistory = new CallingHistory();

            if (instance.ClientID != 1024600)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Client ID";
                return result;
            }

            if (instance.EmployID <= 0)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Employ No";
                return result;
            }

            if (instance.MobileNo.Length <= 5)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Mobile No";
                return result;
            }

            if (dcMaster.Employs.Where(P => P.ID == Convert.ToInt32(instance.EmployID)).Count() <= 0)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Employee ID";
                return result;
            }

            callingHistory.EmployID = instance.EmployID;
            callingHistory.MobileNo = instance.MobileNo;
            callingHistory.Date = DateTime.Now;

            dc.CallingHistories.InsertOnSubmit(callingHistory);
            dc.SubmitChanges();

            result.HasError = false;
            return result;
        }

        public class LabelStickerRequest
        {
            public ClientInformation ClientInfo { get; set; }
            public int WaybillNo { get; set; }
            public StickerSize StickerSize { get; set; }
        }

        public class UpdateReweightRequest
        {
            public ClientInformation ClientInfo { get; set; }
            public int WaybillNo { get; set; }
            public double Length { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Weight { get; set; }
        }

        public class CancelWaybillRequest
        {
            public ClientInformation ClientInfo { get; set; }
            public int WaybillNo { get; set; }
            public string CancelReason { get; set; }
        }

        public class CancelRTOPayloadRequest
        {
            public ClientInformation ClientInfo { get; set; }
            public int WaybillNo { get; set; }
        }

        public enum StickerSize : int
        {
            FourMEightInches = 1, // 4x8 inches 100x200
            FourMSixthInches = 2, // 4x6 inches 100x150
            FourMFourInches = 3, // 4x4 inches 100x100
            FourMSevenInches = 4, // 4x7 inches 100x180
            FourMSixthInchesFragile = 5,
            DunyanaLabel4x4 = 6,
            ExpressLabel4x6Inches = 7,
            FourMSixthInchesZPL=8,
            A4
        }


        public class BarcodeDetail
        {
            public string BarCode { get; set; }
            public string Description { get; set; }
        }
        [WebMethod(Description = "You can use this function to get waybill sticker file as Byte[]")]
        public byte[] GetWaybillSticker(ClientInformation clientInfo, int WaybillNo, StickerSize StickerSize)
        {
            WritetoXMLUpdateWaybill(
                new LabelStickerRequest() { ClientInfo = clientInfo, WaybillNo = WaybillNo, StickerSize = StickerSize },
                clientInfo,
                WaybillNo.ToString(),
                EnumList.MethodType.GetLabelSticker);

            byte[] x = { };

            if (!IsValidWBFormat(WaybillNo.ToString())) // Invalid WB format
                return x;

            if (clientInfo.CheckClientInfo(clientInfo, false).HasError) // Invalid API credential
                return x;

            if (!IsWBBelongsToClientGeneral(clientInfo.ClientID, new List<int>() { WaybillNo })) // WB not belongs to clientIDs
                return x;

            if (IsAsrWaybill(clientInfo.ClientID, WaybillNo))
                return GetWaybillStickerASR(clientInfo, WaybillNo, StickerSize);

            string fileName = GenerateLabelSticker(clientInfo, new List<int>() { WaybillNo }, StickerSize);
            if (fileName == "")
                return x;

            FileStream fileStream = File.OpenRead(fileName);
            x = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            fileStream.Close();
            File.Delete(fileName);
            return x;
        }

        // This function will return the sticker file path
        private string GenerateLabelSticker(ClientInformation ClientInfo, List<int> WaybillNoList, StickerSize StickerSize)
        {
            DateTime StartTime = DateTime.Now;
            //string fileName = Server.MapPath(".")
            //    + "\\WaybillStickers\\" + ClientInfo.ClientID.ToString()
            //    + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            //    + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";


            //string fileName = Server.MapPath(".")
            //    + "\\WaybillStickers\\" + ClientInfo.ClientID.ToString()
            //    + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            //    + "_" + DateTime.Now.ToFileTimeUtc() + WaybillNoList + ".pdf";

            string fileName = Server.MapPath(".") + "\\WaybillStickers\\" + ClientInfo.ClientID.ToString() + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";


            List<rpCustomerBarCode> BarCodeObj = StickerConnectionGeneral(ClientInfo.ClientID, WaybillNoList);
            List<rpCustomerBarCodeExpress> BarCodeObjExp = StickerConnectionForExpress(ClientInfo, WaybillNoList);
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (BarCodeObj[0].ConsigneeCountryName.Trim().ToLower() == "egypt")
            {
                if (StickerSize != XMLShippingService.StickerSize.FourMSixthInches)
                    return "";

                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6_EG QRreport = new Report.rpCustomerLabel4x6_EG();
                QRreport.DataSource = BarCodeObj;
                QRreport.CreateDocument();
                QRreport.ExportToPdf(fileName);
                return fileName;
            }

            //else if (ClientInfo.ClientID == 9020077)
            //{
            //    InfoTrack.NaqelAPI.Report.rpCustomerLabelA4Test QRreport = new Report.rpCustomerLabelA4Test();
            //    QRreport.DataSource = BarCodeObj;
            //    QRreport.CreateDocument();
            //    QRreport.ExportToPdf(fileName);
            //}

            if (StickerSize == XMLShippingService.StickerSize.FourMFourInches)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x4 report = new Report.rpCustomerLabel4x4();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMSixthInches)
            {
                string list1 = System.Configuration.ConfigurationManager.AppSettings["ClientIDWithQR"].ToString();
                List<int> ClientValidation = list1.Split(',').Select(Int32.Parse).ToList();

                string list2 = System.Configuration.ConfigurationManager.AppSettings["ShippingByAir"].ToString();
                List<int> ClientList2 = list2.Split(',').Select(Int32.Parse).ToList();

                string list3 = System.Configuration.ConfigurationManager.AppSettings["ShippingByRoad"].ToString();
                List<int> ClientList3 = list3.Split(',').Select(Int32.Parse).ToList();

                string loadtypeB2B = System.Configuration.ConfigurationManager.AppSettings["B2BLoadtypeIDs"].ToString();
                List<int> b2BLoadtypes = loadtypeB2B.Split(',').Select(Int32.Parse).ToList();

                string StickerNophone = System.Configuration.ConfigurationManager.AppSettings["Sticker_NoPhoneNo"].ToString();
                List<int> StickerNophoneList = StickerNophone.Split(',').Select(Int32.Parse).ToList();


                ////QR clients webcongfig
                if (StickerNophoneList.Contains(ClientInfo.ClientID))
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6NoPhone QRreport = new Report.rpCustomerLabel4x6NoPhone();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (ClientInfo.ClientID == 9020077)
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6Test QRreport = new Report.rpCustomerLabel4x6Test();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (ClientValidation.Contains(ClientInfo.ClientID))
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6QR QRreport = new Report.rpCustomerLabel4x6QR();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                //else if (ClientInfo.ClientID == 9020077)
                ////else if (ClientInfo.ClientID == 9016338 || ClientInfo.ClientID == 9023636 || ClientInfo.ClientID == 9017160)
                //{
                //    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6Carriyo CRreport = new Report.rpCustomerLabel4x6Carriyo();
                //    CRreport.DataSource = BarCodeObj;
                //    CRreport.CreateDocument();
                //    CRreport.ExportToPdf(fileName);
                //}
                else if (ClientInfo.ClientID == 9017663)
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6ALJ QRreport = new Report.rpCustomerLabel4x6ALJ();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (ClientInfo.ClientID == 9027438) // 9027438 : Honest Forwarder Technology FZE
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6_Honest QRreport = new Report.rpCustomerLabel4x6_Honest();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                // Reem code for waybill sticker Same Day
                else if (ClientInfo.ClientID == 9026040 && BarCodeObj[0].LoadTypeID == 189)
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6BySameDay QRreport = new Report.rpCustomerLabel4x6BySameDay();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (BarCodeObj[0].LoadTypeID == 34
                && (
                    (BarCodeObj[0].ClientCountryName == "UNITED ARAB EMIRATES" && BarCodeObj[0].ConsigneeCountryName == "Saudi Arabia")
                    || (BarCodeObj[0].ClientCountryName == "Saudi Arabia" && BarCodeObj[0].ConsigneeCountryName == "UNITED ARAB EMIRATES")
                ))
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6ByAir QRreport = new Report.rpCustomerLabel4x6ByAir();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (ClientList3.Contains(ClientInfo.ClientID) || (BarCodeObj[0].LoadTypeID) == 65) //add webconfig(road)
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6ByRoad QRreport = new Report.rpCustomerLabel4x6ByRoad();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (b2BLoadtypes.Contains(BarCodeObj[0].LoadTypeID))
                {
                    InfoTrack.NaqelAPI.Report.B2B_rpCustomerLabel4x6 report = new Report.B2B_rpCustomerLabel4x6();
                    report.DataSource = BarCodeObj;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }
                else if (StickerNophoneList.Contains(ClientInfo.ClientID))
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6NoPhone QRreport = new Report.rpCustomerLabel4x6NoPhone();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6 report = new Report.rpCustomerLabel4x6();
                    report.DataSource = BarCodeObj;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMSevenInches)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x7 report = new Report.rpCustomerLabel4x7();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if ((ClientInfo.ClientID == 9026040 && BarCodeObj[0].LoadTypeID == 189))
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x8SameDay QRreport = new Report.rpCustomerLabel4x8SameDay();
                QRreport.DataSource = BarCodeObj;
                QRreport.CreateDocument();
                QRreport.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMEightInches)
            {
                string BirkenstockClientID = System.Configuration.ConfigurationManager.AppSettings["BirkenstockCLientID_SameDay_Delivery"].ToString();
                List<int> BirkenstockClientIDList = BirkenstockClientID.Split(',').Select(Int32.Parse).ToList();

                string StickerNophone = System.Configuration.ConfigurationManager.AppSettings["Sticker_NoPhoneNo"].ToString();
                List<int> StickerNophoneList = StickerNophone.Split(',').Select(Int32.Parse).ToList();

                if (StickerNophoneList.Contains(ClientInfo.ClientID) && StickerSize == XMLShippingService.StickerSize.FourMEightInches)
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x8NoPhone QRreport = new Report.rpCustomerLabel4x8NoPhone();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else if (BirkenstockClientIDList.Contains(ClientInfo.ClientID) && BarCodeObj[0].Reference1 == "Express")
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6BySameDay QRreport = new Report.rpCustomerLabel4x6BySameDay();
                    QRreport.DataSource = BarCodeObj;
                    QRreport.CreateDocument();
                    QRreport.ExportToPdf(fileName);
                }
                else
                {
                    InfoTrack.NaqelAPI.Report.rpCustomerLabel4x8 report = new Report.rpCustomerLabel4x8();
                    report.DataSource = BarCodeObj;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMSixthInchesFragile)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6Fragile report = new Report.rpCustomerLabel4x6Fragile();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.A4 && (BarCodeObj[0].LoadTypeID) == 65)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabelA4ByRoad report = new Report.rpCustomerLabelA4ByRoad();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMSixthInchesZPL)
            {
                string list1 = System.Configuration.ConfigurationManager.AppSettings["ClientIDWithQR"].ToString();
                List<int> ClientValidation = list1.Split(',').Select(Int32.Parse).ToList();

                string list2 = System.Configuration.ConfigurationManager.AppSettings["ShippingByAir"].ToString();
                List<int> ClientList2 = list2.Split(',').Select(Int32.Parse).ToList();

                string list3 = System.Configuration.ConfigurationManager.AppSettings["ShippingByRoad"].ToString();
                List<int> ClientList3 = list3.Split(',').Select(Int32.Parse).ToList();

                string loadtypeB2B = System.Configuration.ConfigurationManager.AppSettings["B2BLoadtypeIDs"].ToString();
                List<int> b2BLoadtypes = loadtypeB2B.Split(',').Select(Int32.Parse).ToList();

                string StickerNophone = System.Configuration.ConfigurationManager.AppSettings["Sticker_NoPhoneNo"].ToString();
                List<int> StickerNophoneList = StickerNophone.Split(',').Select(Int32.Parse).ToList();

                XtraReport report = null;
                string fileNameBase = Server.MapPath(".") + "\\WaybillStickers\\" + ClientInfo.ClientID + "_" + DateTime.Now.ToFileTimeUtc();
                ////QR clients webcongfig
                if (StickerNophoneList.Contains(ClientInfo.ClientID))
                {
                    report = new Report.rpCustomerLabel4x6NoPhone();
            
                }
                else if (BarCodeObj[0].LoadTypeID == 34
                    && (
                        (BarCodeObj[0].ClientCountryName == "UNITED ARAB EMIRATES" && BarCodeObj[0].ConsigneeCountryName == "Saudi Arabia")
                        || (BarCodeObj[0].ClientCountryName == "Saudi Arabia" && BarCodeObj[0].ConsigneeCountryName == "UNITED ARAB EMIRATES")
                    ))
                {
                    report = new Report.rpCustomerLabel4x6ByAir();
              
                }
                else if (ClientList3.Contains(ClientInfo.ClientID) || (BarCodeObj[0].LoadTypeID) == 65) //add webconfig(road)
                {
                    report = new Report.rpCustomerLabel4x6ByRoad();
           
                }
                else if (b2BLoadtypes.Contains(BarCodeObj[0].LoadTypeID))
                {
                    report = new Report.B2B_rpCustomerLabel4x6();
                    
                }
                else if (StickerNophoneList.Contains(ClientInfo.ClientID))
                {
                    report = new Report.rpCustomerLabel4x6NoPhone();
                    
                }
                else
                {
                    report = new Report.rpCustomerLabel4x6();
                    
                }
                // Assign the data source
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                // Export to image (PNG), then convert to ZPL
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportOptions.Image.ExportMode = DevExpress.XtraPrinting.ImageExportMode.SingleFilePageByPage;
                    report.ExportOptions.Image.Resolution = 203;
                    report.ExportOptions.Image.Format = System.Drawing.Imaging.ImageFormat.Png;

                    report.ExportToImage(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    using (Bitmap image = new Bitmap(ms))
                    {
                        string zplResult = "^XA" + Image2ZPL.Convert.BitmapToZPLII(image, 0, 0) + "^XZ";

                        // Write ZPL to file
                        var zplFileName = fileNameBase + ".zpl";
                        File.WriteAllText(zplFileName, zplResult, Encoding.UTF8);
                        fileName = zplFileName;
                    }
                }


            }
            else if (BarCodeObj[0].LoadTypeID == 34
                && (
                    (BarCodeObj[0].ClientCountryName == "UNITED ARAB EMIRATES" && BarCodeObj[0].ConsigneeCountryName == "Saudi Arabia")
                    || (BarCodeObj[0].ClientCountryName == "Saudi Arabia" && BarCodeObj[0].ConsigneeCountryName == "UNITED ARAB EMIRATES")
                ))
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabelA4ByAir QRreport = new Report.rpCustomerLabelA4ByAir();
                QRreport.DataSource = BarCodeObj;
                QRreport.CreateDocument();
                QRreport.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.A4)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabelA4 report = new Report.rpCustomerLabelA4();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.DunyanaLabel4x4)
            {
                InfoTrack.NaqelAPI.Report.DunyanaLabel4x4 report = new Report.DunyanaLabel4x4();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.ExpressLabel4x6Inches)
            {
                InfoTrack.NaqelAPI.Report.rpCustomerLabel4x6Express report = new Report.rpCustomerLabel4x6Express();
                report.DataSource = BarCodeObjExp;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else
                fileName = "";

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.StickerPrinting, string.Join(",", WaybillNoList), null, StartTime, DateTime.Now);
            return fileName;
        }

        private List<rpCustomerBarCode> StickerConnectionGeneral(int ClientID, List<int> WaybillNoList)
        {
            List<rpCustomerBarCode> BarCodeObj;
            string sqlReportData = @"
            SELECT ID, LoadTypeID, WaybillNo, DeclaredValue, CurrencyID as DeclareValueCurrency, ExchangeRate,
                PicesCount, Weight, Width, Length, Height, VolumeWeight, PickUpDate, -- OriginStationID, DestinationStationID,
                DeliveryInstruction, CODCharge, InsuredValue, RefNo, Reference1,-- Reference2, 
                BarCode, CustomerPieceBarCode, 
                ServiceTypeID, BatchNo, PODType, PODTypeID, 
                ClientContactID, ProductCode, IsCOD, ConsigneeID,
                OrgCode, OrgName, DestCode, DestName,
                ClientID, ClientName,
                Name as ClientContactName,
                ClientPh as ClientContactPhoneNumber,
                ClientMobile as ClientContactMobile,
                ClientFAdd as ClientContactFirstAddress,
                ClientSAdd as ClientContactSecondAddress,
                ClientLoc as ClientContactLocation,
                ClientPO as ClientContactPOBox,
                ClientEmail as ClientContactEmail,
                ClientZip as ClientContactZipCode,
                ClientFax as ClientContactFax,
                ClientCountry as ClientCountryName,
                ClientCity as ClientCityName,
                ConName as ConsigneeName,
                ClientComName as ConsigneeCompanyName,
                ConPh as ConsigneePhoneNumber,
                ConMobile as ConsigneeMobile,
                ConFAdd as ConsigneeFirstAddress,
                ConEmail as ConsigneeEmail,
                ConFax as ConsigneeFax, -- ConsigneeNear, ConsigneeNationalID
                ConCountry as ConsigneeCountryName,
                ConCity as ConsigneeCityName,
                GoodDesc as GoodDescription,
                BusinessTypeID as ClientBusinessTypeID,
                Incoterm -- 1:DDU 3:DDP
            FROM dbo.rpCustomerWaybillwtihPieceBarCode 
            WHERE WaybillNo in (" + string.Join(",", WaybillNoList)
                + @")
                and ClientID in (" + GetCrossTrackingClientIDs(ClientID) + ")";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                BarCodeObj = connection.Query<rpCustomerBarCode>(sqlReportData).ToList();
            }

            return BarCodeObj;
        }

        private List<rpCustomerBarCodeExpress> StickerConnectionForExpress(ClientInformation clientInfo, List<int> WaybillNoList)
        {
            List<rpCustomerBarCodeExpress> BarCodeObj;

            string sqlstr = "SELECT * FROM rpCustomerWaybillwtihPieceBarCodeAPIExpress WHERE ClientID in ("
                + GetCrossTrackingClientIDs(clientInfo.ClientID)
                + ") AND WaybillNo in (" + string.Join(",", WaybillNoList) + ")";

            using (SqlConnection cnn = new SqlConnection(sqlCon))
            {
                var cmd = new SqlCommand(sqlstr, cnn);
                BarCodeObj = cnn.Query<rpCustomerBarCodeExpress>(sqlstr).ToList();
            }

            return BarCodeObj;
        }

        public double ExchangeRate(int WaybillNo, double declareValue)
        {
            double DVusd = 0;
            //string connectionString = @"server = localhost; port = 3306; database = erpnaqel; user = root; password = Amal@123456";
            //string connectionString = GetConnectionString();
            //var connection = Configuration["Logging:ConnectionStrings:ERPNaqel_Connection"];
            using (SqlConnection cnn = new SqlConnection(sqlCon))
            {
                cnn.Open();
                string sqlstr = "SELECT CurrencyID FROM rpCustomerWaybillwtihPieceBarCodeAPI WHERE WaybillNo = " + WaybillNo;
                var cmd = new SqlCommand(sqlstr, cnn);
                var currencyId = cnn.Query<int>(sqlstr).First();

                if (declareValue > 0 && currencyId > 0)
                {
                    string sqlstr1 = "SELECT ExchangeRate FROM Currency WHERE ID = " + currencyId;
                    var cmd1 = new SqlCommand(sqlstr1, cnn);
                    double Exrate = cnn.Query<double>(sqlstr1).First();
                    cnn.Close();

                    DVusd = Math.Round(declareValue / Exrate, 2);
                    return DVusd;
                }
                else // for old API versions that have no currency
                {
                    return declareValue;
                }

                //cnn.Close();
            }
            return declareValue;
        }

        public double ExchangeRate(double DeclareValue = 0, int CurrencyID = 0)
        {
            double DVusd = 0;

            if (DeclareValue > 0 && CurrencyID > 0)
            {
                using (SqlConnection connection = new SqlConnection(sqlCon))
                {
                    string sqlCurrency = @"SELECT ExchangeRate FROM Currency WHERE StatusID = 1 AND ID = " + CurrencyID;
                    double Exrate = connection.Query<double>(sqlCurrency).First();
                    DVusd = Math.Round(DeclareValue / Exrate, 2);
                    return DVusd;
                }
            }
            return DVusd;
        }

        public static double ExchangeRate(int WaybillNo)
        {
            double DVusd = 0;
            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            CustomerWayBill DVobj = dc.CustomerWayBills.FirstOrDefault(p => p.WayBillNo == WaybillNo);

            if (DVobj != null && DVobj.DeclaredValue > 0 && DVobj.CurrencyID > 0)
            {
                var cur = dc.Currencies.FirstOrDefault(p => p.ID == DVobj.CurrencyID);
                DVusd = DVobj.DeclaredValue / cur.ExchangeRate;
                return DVusd;
            }
            else if (DVobj != null && DVobj.DeclaredValue > 0 && DVobj.CurrencyID == null) // for old API versions that have no currency
            {
                return DVobj.DeclaredValue;
            }

            return 0;
        }

        private bool IsWBBelongsToClientGeneral(int ClientID, List<int> WaybillNoList)
        {
            // Get distinct clientID from give waybillNo
            List<string> ClientStrList = new List<string>();
            string sql = @"select distinct ClientID from CustomerWaybills where StatusID = 1 and WaybillNo in (" + string.Join(",", WaybillNoList) + ")";
            using (var db = new SqlConnection(sqlCon))
            {
                ClientStrList = db.Query<string>(sql).ToList();
            }

            if (ClientStrList.Count() == 0)
                return false;

            // Get all account belongs to same client
            string list0 = GetCrossTrackingClientIDs(ClientID);
            List<string> _clientid0 = list0.Split(',').ToList();

            foreach (var tempClient in ClientStrList)
            {
                if (!_clientid0.Contains(tempClient))
                    return false;
            }

            return true;
        }

        private bool IsInvoiceBelongsToClientGeneral(int ClientID, string invoiceNo)
        {
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            // Get all account belongs to same client
            string list0 = GetCrossTrackingClientIDs(ClientID);
            List<int> _clientid0 = list0.Split(',').Select(int.Parse).ToList();

            if (dc.ClientCommercialInvoices.Where(P => P.InvoiceNo == invoiceNo && P.StatusID == 1 && _clientid0.Contains(ClientID)).Count() > 0)
                return true;
            else
                return false;
        }

        private List<int> GetClientCommercialInvoices(int ClientID, string invoiceNo)
        {
            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            // Get all account belongs to same client
            string list0 = GetCrossTrackingClientIDs(ClientID);
            List<int> _clientid0 = list0.Split(',').Select(int.Parse).ToList();

            List<int> tempResult = new List<int>();
            tempResult = dc.ClientCommercialInvoices
                .Where(P => P.InvoiceNo == invoiceNo && P.StatusID == 1 && _clientid0.Contains(P.ClientID))
                .Select(p => p.ClientID).ToList();

            return tempResult;
        }

        private bool IsWBExist(string WaybillNo, int ClientID)
        {
            if (!IsValidWBFormat(WaybillNo))
                return false;

            string clientAccounts = GetCrossTrackingClientIDs(ClientID);

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            int CntWB = 0;
            string sql = @"select count(1) from CustomerWaybills
                    where WaybillNo = " + WaybillNo.Trim() + @"
                    and ClientID in (" + clientAccounts + ") and StatusID = 1;";
            using (var db = new SqlConnection(con))
            {
                CntWB = db.Query<int>(sql).ToList()[0];
            }

            return CntWB > 0;
        }

        private bool IsRefNoExist(string RefNo, int ClientID)
        {
            string clientAccounts = GetCrossTrackingClientIDs(ClientID);

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            int CntWB = 0;
            string sql = @"select count(1) from CustomerWaybills
                    where RefNo = " + "'" + RefNo.Trim() + "'" + @"
                    and ClientID in (9020077 , 9022477 ,9026333 ) and StatusID = 1;";
            using (var db = new SqlConnection(con))
            {
                CntWB = db.Query<int>(sql).ToList()[0];
            }

            return CntWB > 0;
        }

        private bool IsPickupExist(int WaybillNo, int ClientID)
        {
            if (!IsWBExist(WaybillNo.ToString(), ClientID))
                return false;

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            int CntWB = 0;
            string sql = @"select count(1) from PickUp where WaybillNo = " + WaybillNo + @" and StatusID = 1;";
            using (var db = new SqlConnection(con))
            {
                CntWB = db.Query<int>(sql).ToList()[0];
            }

            return CntWB > 0;
        }

        private bool IsValidWBFormat(string WaybillNo)
        {
            Regex reg = new Regex(@"^[1-9]{1}\d{7,8}$");
            return WaybillNo.Trim().Length > 0 && reg.IsMatch(WaybillNo.Trim());
        }

        private bool IsAsrWaybill(int ClientID, int WaybillNo)
        {
            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            int tempLoadTypeID = dc.CustomerWayBills.Where(c => c.ClientID == ClientID && c.WayBillNo == WaybillNo && c.StatusID == 1)
                .Select(c => c.LoadTypeID).FirstOrDefault();
            return tempLoadTypeID == 66 || tempLoadTypeID == 136 || tempLoadTypeID == 204 || tempLoadTypeID == 206;
        }

        private bool IsB2BWaybill(int ClientID, int WaybillNo)
        {
            string loadtypeB2B = System.Configuration.ConfigurationManager.AppSettings["B2BLoadtypeIDs"].ToString();
            List<int> b2BLoadtypes = loadtypeB2B.Split(',').Select(Int32.Parse).ToList();

            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            int tempLoadTypeID = dc.CustomerWayBills.Where(c => c.ClientID == ClientID && c.WayBillNo == WaybillNo && c.StatusID == 1)
                .Select(c => c.LoadTypeID).FirstOrDefault();
            return b2BLoadtypes.Contains(tempLoadTypeID);
        }

        private byte[] GetWaybillStickerASR(ClientInformation ClientInfo, int WaybillNo, StickerSize StickerSize)
        {
            byte[] x = { };

            string fileName = GenerateLabelStickerASR(ClientInfo, new List<int>() { WaybillNo }, StickerSize);
            if (fileName == "")
                return x;

            FileStream fileStream = File.OpenRead(fileName);
            x = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            fileStream.Close();
            return x;
        }

        private string GenerateLabelStickerASR(ClientInformation ClientInfo, List<int> WaybillNoList, StickerSize StickerSize)
        {
            DateTime StartTime = DateTime.Now;
            string fileName = Server.MapPath(".") + "\\WaybillStickers\\" + ClientInfo.ClientID + "_" + DateTime.Now.ToFileTimeUtc() + ".pdf";

            List<rpCustomerBarCodeAsr> BarCodeObj = StickerConnectionAsr(ClientInfo.ClientID, WaybillNoList);

            if (StickerSize == XMLShippingService.StickerSize.FourMSixthInches)
            {
                InfoTrack.NaqelAPI.Report.Asr_rpCustomerLabel4x6 report = new Report.Asr_rpCustomerLabel4x6();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.FourMEightInches)
            {
                InfoTrack.NaqelAPI.Report.Asr_rpCustomerLabel4x8 report = new Report.Asr_rpCustomerLabel4x8();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else if (StickerSize == XMLShippingService.StickerSize.A4)
            {
                InfoTrack.NaqelAPI.Report.Asr_rpCustomerLabelA4 report = new Report.Asr_rpCustomerLabelA4();
                report.DataSource = BarCodeObj;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            else
                fileName = "";

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.StickerPrinting, string.Join(",", WaybillNoList), null, StartTime, DateTime.Now);
            return fileName;
        }

        private List<rpCustomerBarCodeAsr> StickerConnectionAsr(int ClientID, List<int> WaybillNoList)
        {
            List<rpCustomerBarCodeAsr> BarCodeObj;

            string sqlReportData = @"SELECT * FROM dbo.rpCustomerWaybillwtihPieceBarCodeASR WHERE WaybillNo in ("
                + string.Join(",", WaybillNoList)
                + @")
                and ClientID in (" + GetCrossTrackingClientIDs(ClientID) + ")";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                BarCodeObj = connection.Query<rpCustomerBarCodeAsr>(sqlReportData).ToList();
            }

            return BarCodeObj;
        }

        public double GetDVLimit(int CurrencyID = 0, int loadTypeID = 0)
        {
            double DVLimit = 0;

            string list1 = System.Configuration.ConfigurationManager.AppSettings["DocumentLoadTypeIDs"].ToString();
            var documentLoadTypes = list1.Split(',').Select(Int32.Parse).ToList();
            var columnName = documentLoadTypes.Contains(loadTypeID) ? "DocumentLimitValue" : "LimitValue";

            if (CurrencyID > 0)
            {
                using (SqlConnection connection = new SqlConnection(sqlCon))
                {
                    string sqlCurrency = @"select " + columnName + " from DVMinLimit with(nolock) where currencyID = " + CurrencyID;
                    DVLimit = connection.Query<double>(sqlCurrency).First();
                    return DVLimit;
                }
            }
            return DVLimit;
        }

        public void LogException(Exception e)
        {
            try
            {
                string fileName = Server.MapPath(".") + "\\ErrorData\\log_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

                FileStream logFel = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Write);
                StreamWriter logFile = new StreamWriter(logFel);

                string message = "";
                message += "==============================================\n";
                message += "\n Message : " + e.Message;
                message += "\n User : -1 ";
                message += "\n Version : 1";
                message += "\n Source : " + e.Source;
                message += "\n Date : " + DateTime.Now.ToShortDateString();
                message += "\n Time : " + DateTime.Now.ToShortTimeString();
                message += "\n\n-------------------------------------------------------------------------\n";
                if (e.InnerException != null)
                {
                    message += "\n InnerException : " + e.InnerException.Message.ToString();
                    message += "\n InnerException : Stack Trace: " + e.InnerException.StackTrace.ToString();
                    message += "\n\n-------------------------------------------------------------------------\n";
                }

                message += "\n TargetSite : " + e.TargetSite;
                message += "\n Data : " + e.Data;
                message += "\n StackTrace : " + e.StackTrace + "\n";
                message += "\n ==============================================";

                logFile.WriteLine(message);
                logFile.Close();
            }
            catch { }
        }

        internal byte[] GetBuffer(XtraReport report)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                report.SaveLayout(stream);
                return stream.ToArray();
            }
        }

        [WebMethod(Description = "You can use this function to Get tracking events for your shipments..")]
        public List<TrackingDetails> SendTrackingStatus(ClientTrackingDetails _clientTrackingDetails)
        {
            AddAPIError(_clientTrackingDetails.ClientInfo.ClientID, "GetSTS");

            Result result = new Result();
            result = _clientTrackingDetails.ClientInfo.CheckClientInfo(_clientTrackingDetails.ClientInfo, false);
            List<TrackingDetails> TrackingObj = new List<TrackingDetails>();
            string sqlCon = System.Configuration.ConfigurationManager.ConnectionStrings["DapperConnectionString"].ConnectionString;
            if (!result.HasError)
            {
                using (IDbConnection db = new SqlConnection(sqlCon))
                {
                    if (_clientTrackingDetails.FromDate.HasValue && _clientTrackingDetails.ToDate.HasValue && _clientTrackingDetails.PageCount > 0)
                    {
                        string str = "select ViwTracking.WaybillNo,Waybill.RefNo,ViwTracking.Date,ViwTracking.Activity,Consignee.Name,ConsigneeDetail.FirstAddress,ConsigneeDetail.PhoneNumber,ConsigneeDetail.Mobile "
                            + " from  ViwTracking "
                            + " left join Waybill with(nolock) on ViwTracking.WaybillNo = Waybill.WaybillNo"
                            + " left join ConsigneeDetail with(nolock) on Waybill.ConsigneeAddressID = ConsigneeDetail.ID"
                            + " left join Consignee with(nolock) on Consignee.ID = ConsigneeDetail.ConsigneeID"
                            + " where Waybill.clientid = " + _clientTrackingDetails.ClientInfo.ClientID + " and ViwTracking.Date >= '" + Convert.ToDateTime(_clientTrackingDetails.FromDate)
                            + "' and ViwTracking.Date <= '" +
                            (_clientTrackingDetails.ToDate)
                            + "' ORDER BY WAYBILLNO"
                            + " OFFSET " + (_clientTrackingDetails.PageCount - 1) * 100 + " ROWS "
                            + " FETCH NEXT 100 ROWS ONLY ";

                        string str2 = " select Count(*) / 100 as NumberOfPages"
                            + " from ViwTracking left join Waybill with(nolock) on ViwTracking.WaybillNo = Waybill.WaybillNo"
                            + " left join ConsigneeDetail with(nolock) on Waybill.ConsigneeAddressID = ConsigneeDetail.ID"
                            + " left join Consignee with(nolock) on Consignee.ID = ConsigneeDetail.ConsigneeID"
                            + " where Waybill.clientid = " + _clientTrackingDetails.ClientInfo.ClientID + "and ViwTracking.Date >= '" + Convert.ToDateTime(_clientTrackingDetails.FromDate)
                            + "'and ViwTracking.Date <= '" + Convert.ToDateTime(_clientTrackingDetails.ToDate) + "'";

                        TrackingObj = db.Query<TrackingDetails>(str).ToList();

                        var TrackCount = db.Query<TrackingPageDetail>(str2).FirstOrDefault();
                        TrackingObj.ToList().ForEach(c => { c.NumberOfPages = TrackCount.NumberOfPages; });

                    }
                    else
                    {
                        result.HasError = true;
                        Console.WriteLine("Error, Please pass correct parameter  ..");
                    }
                }
            }
            else
            {
                result.HasError = true;
                Console.WriteLine("ClientInformation not correct, Please check ..");
            }

            return TrackingObj;
        }

        [WebMethod(Description = "You can use this function to Get Last tracking event for 100 shipment..")]
        public List<PODTrackingStatus> GetPODStatus(ClientInformation _clientInfo, List<int> WaybillNoList)
        {
            AddAPIError(_clientInfo.ClientID, "GetPOD");

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            List<PODTrackingStatus> Listresult = new List<PODTrackingStatus>();

            if (!_clientInfo.CheckClientInfo(_clientInfo, false).HasError)
            {
                if (WaybillNoList.Count <= 100)
                {
                    foreach (var obj in WaybillNoList)
                    {
                        PODTrackingStatus _PODobj = new PODTrackingStatus();
                        if (dcMaster.ViwTrackings.Where(P => P.WaybillNo == Convert.ToInt32(obj) && P.IsInternalType == false && P.Date <= DateTime.Now).Count() > 0)
                        {
                            var instance = dcMaster.ViwTrackings.Where(P => P.WaybillNo == Convert.ToInt32(obj) && P.IsInternalType == false && P.Date <= DateTime.Now).OrderByDescending(x => x.ID).First();
                            _PODobj.WaybillNo = instance.WaybillNo;
                            _PODobj.Date = instance.Date;
                            _PODobj.Activity = instance.Activity;
                            _PODobj.ArActivity = instance.ActivityAr;
                            Listresult.Add(_PODobj);
                        }
                        else
                        {
                            _PODobj.WaybillNo = Convert.ToInt32(obj);
                            _PODobj.Date = DateTime.Now;
                            _PODobj.Activity = "This Waybill does not have a pick-up status or did not arrived at destination";
                            _PODobj.ArActivity = "This Waybill does not have a pick-up status or did not arrived at destination";
                            Listresult.Add(_PODobj);
                        }
                    }

                }
                else
                {
                    PODTrackingStatus _PODobj = new PODTrackingStatus();
                    _PODobj.HasError = true;
                    _PODobj.ErrorMessage = "You can track maximum 100 WayBills in a call";
                    _PODobj.Date = DateTime.Now;
                    Listresult.Add(_PODobj);
                    return Listresult;
                }
            }
            else
            {
                PODTrackingStatus _PODobj = new PODTrackingStatus();

                _PODobj.HasError = true;
                _PODobj.ErrorMessage = "Your client Information is not correct..";
                Listresult.Add(_PODobj);
                return Listresult;
            }

            GlobalVar.GV.CreateShippingAPIRequest(_clientInfo, EnumList.APIRequestType.MultiWayBillTrackingAllStaus, WaybillNo.ToString(), null);

            return Listresult;
        }

        [WebMethod(Description = "You can use this function to Get Last tracking event and Event code for 100 shipment..")]
        public List<LastEventTrackingStatus> LastEventCode(ClientInformation _clientInfo, List<int> WaybillNoList)
        {
            AddAPIError(_clientInfo.ClientID, "GetLEC");

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<LastEventTrackingStatus> Listresult = new List<LastEventTrackingStatus>();

            #region Basic validation
            LastEventTrackingStatus _PODobj = new LastEventTrackingStatus()
            {
                HasError = true,
                Date = DateTime.Now
            };

            if (WaybillNoList.Count > 100 || WaybillNoList.Count == 0)
            {
                _PODobj.ErrorMessage = "You can track maximum 100 WayBills in a call";
                Listresult.Add(_PODobj);
                return Listresult;
            }

            foreach (int item in WaybillNoList)
            {
                if (!IsValidWBFormat(item.ToString()))
                {
                    _PODobj.ErrorMessage = "Invalid WaybillNo: " + item;
                    Listresult.Add(_PODobj);
                    return Listresult;
                }
            }

            if (_clientInfo.CheckClientInfo(_clientInfo, false).HasError)
            {
                _PODobj.ErrorMessage = "Your client Information is not correct..";
                Listresult.Add(_PODobj);
                return Listresult;
            }

            // Check waybills belong to this client
            var tempDistinctClientIDs = dc.CustomerWayBills.Where(w => WaybillNoList.Contains(w.WayBillNo) && w.StatusID != 3)
                .Select(w => w.ClientID)
                .Distinct()
                .ToList();
            string tempClientIDs = GetCrossTrackingClientIDs(_clientInfo.ClientID);
            foreach(var c in tempDistinctClientIDs)
            {
                if (!tempClientIDs.Contains(c.ToString()))
                {
                    _PODobj.ErrorMessage = "Some WaybillNos not belongs to your account, please update and try again";
                    Listresult.Add(_PODobj);
                    return Listresult;
                }
            }
            #endregion

            foreach (var obj in WaybillNoList)
            {
                LastEventTrackingStatus temp = new LastEventTrackingStatus()
                {
                    WaybillNo = Convert.ToInt32(obj),
                    Date = DateTime.Now,
                    Activity = "This Waybill does not have a pick-up status or did not arrived at destination",
                    ArActivity = "This Waybill does not have a pick-up status or did not arrived at destination"
                };

                var instances = dcMaster.ViwTrackings
                    .Where(P => P.WaybillNo == Convert.ToInt32(obj)
                        && P.IsInternalType == false
                        && P.Date <= DateTime.Now)
                    .ToList();

                if (instances.Any())
                {
                    var instance = instances.OrderByDescending(x => x.Date).First();
                    temp.Date = instance.Date;
                    temp.EventCode = Convert.ToInt32(instance.EventCode);
                    temp.Activity = instance.Activity;
                    temp.ArActivity = instance.ActivityAr;
                }

                Listresult.Add(temp);
            }

            GlobalVar.GV.CreateShippingAPIRequest(_clientInfo, EnumList.APIRequestType.MultiWayBillTrackingAllStaus, WaybillNo.ToString(), null);

            return Listresult;
        }

        [WebMethod(Description = "You can use this function to track no more than 500 Waybill ..")]
        public List<NewCheckPointsTrack> TraceByMultiWaybillNoNewCheckPoints(ClientInformation ClientInfo, List<int> WaybillNo)
        {
            StringBuilder sb = new StringBuilder();
            string WBno = string.Join(",", WaybillNo.ToArray());

            List<NewCheckPointsTrack> Result = new List<NewCheckPointsTrack>();

            if (WaybillNo.Count() <= 0 || String.IsNullOrWhiteSpace(Convert.ToString(WaybillNo)))
            {
                NewCheckPointsTrack newActivity = new NewCheckPointsTrack();
                newActivity.ErrorMessage = "Please provide a valid list of WaybillNo";
                newActivity.Date = DateTime.Now;
                newActivity.HasError = true;
                Result.Add(newActivity);
                return Result;
            }

            if (WaybillNo.Count > 500)
            {
                NewCheckPointsTrack newActivity = new NewCheckPointsTrack();
                newActivity.ErrorMessage = "You can track maximum 500 WayBill in a call";
                newActivity.HasError = true;
                Result.Add(newActivity);
            }
            else if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                try
                {
                    foreach (var WayBil in WaybillNo)
                    {
                        if (WayBil != 0)
                        {
                            List<ViwtrackingCustomer_API> list = new List<ViwtrackingCustomer_API>();
                            list = dcMaster.ViwtrackingCustomer_APIs.Where(P => P.WaybillNo == WayBil).OrderBy(p => p.TransDate).ToList();
                            for (int i = 0; i < list.Count; i++)
                            {
                                NewCheckPointsTrack newActivity = new NewCheckPointsTrack();
                                if (list[i].ClientID.HasValue)
                                    newActivity.ClientID = list[i].ClientID.Value;
                                else
                                    newActivity.ClientID = ClientInfo.ClientID;

                                newActivity.RefNo = list[i].RefNo;
                                newActivity.StationName = list[i].StationName;
                                newActivity.Date = Convert.ToDateTime(list[i].TransDate);
                                newActivity.Activity = list[i].Activity;
                                newActivity.WaybillNo = list[i].WaybillNo;

                                if (list[i].EventCode.HasValue)
                                    newActivity.EventCode = list[i].EventCode.Value;
                                list[i].EventCode = list[i].EventCode;

                                Result.Add(newActivity);
                            }
                        }
                    }

                }
                catch
                {
                    NewCheckPointsTrack newActivity = new NewCheckPointsTrack();
                    newActivity.ErrorMessage = "Error happend while tracking";
                    newActivity.HasError = true;
                    Result.Add(newActivity);
                }
            }

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.TraceByMultiWaybillNoNewCheckPoints, WaybillNo.ToString(), 23);
            return Result;
        }

        #region "NLogger"
        private void WritetoXMLUpdateWaybillNLog(Object myObject, ClientInformation _ClientInfo, string reference, EnumList.MethodType methodType)
        {
            try
            {
                string filename = "UpdateWaybillData/" + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + reference;
                LogEvent(myObject, filename);
            }
            catch (Exception ex)
            {
                GlobalVar.GV.InsertError(ex.Message, "15380");
            }
        }

        private void WritetoXMLNLog(Object myObject, ClientInformation _ClientInfo, string reference, EnumList.MethodType methodType, Object Result)
        {
            try
            {
                //log myObject
                string filename = "ErrorData/" + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + reference;
                LogEvent(myObject, filename);

                //log Error object
                filename = filename + "_" + "Error";
                LogEvent(Result, filename);
            }
            catch (Exception ex)
            {
                GlobalVar.GV.InsertError(ex.Message, "15380");
            }
        }

        private void LogEvent(object myObject, string fileName)
        {
            NLog.LogEventInfo logEvent = new NLog.LogEventInfo(NLog.LogLevel.Error, logger.Name, GetObjectXMLString(myObject));
            logEvent.Properties["CustomFileName"] = fileName;
            logger.Log(logEvent);
        }

        private string GetObjectXMLString(object myObject)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlSerializer xmlSerializer = new XmlSerializer(myObject.GetType());
            using (MemoryStream xmlStream = new MemoryStream())
            {
                xmlSerializer.Serialize(xmlStream, myObject);
                xmlStream.Position = 0;
                xmlDoc.Load(xmlStream);
            }
            return xmlDoc.InnerXml;
        }
        #endregion

        [WebMethod(Description = " You can use this function to Trace your RefNo status")]
        public string TraceByMultiRefNo(ClientInformation ClientInfo, List<string> RefNo)
        {
            List<IkeaTracking> TC = new List<IkeaTracking>();
            List<IkeaTrackingerror> LM = new List<IkeaTrackingerror>();



            if (RefNo.Count <= 0 || String.IsNullOrWhiteSpace(Convert.ToString(RefNo)))
            {
                IkeaTracking newActivity = new IkeaTracking();
                newActivity.Date = Convert.ToString(DateTime.Now);
                newActivity.ErrorMessage = "Please provide a valid list of RefNo";
                newActivity.HasError = true;
                TC.Add(newActivity);
                var json1 = new JavaScriptSerializer().Serialize(TC);
                return json1;
            }

            if (RefNo.Count > 500)
            {
                IkeaTracking newActivity = new IkeaTracking();
                newActivity.ErrorMessage = "You can track maximum 500 RefNo in a call";
                newActivity.HasError = true;
                TC.Add(newActivity);
                var json2 = new JavaScriptSerializer().Serialize(TC);
                return json2;
            }

            StringBuilder sb = new StringBuilder();
            string con = GetTrackingConnStr();
            List<string> _waybillNo = new List<string>();
            foreach (string item in RefNo)
            {
                if (!String.IsNullOrWhiteSpace(item))
                {
                    string s = "'" + item + "'";
                    _waybillNo.Add(s);
                }
            }
            string _refNo = string.Join(",", _waybillNo.ToArray());

            if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {

                using (SqlConnection connection = new SqlConnection(con))
                {
                    try
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("select * from ViwTracking where ClientID = " + ClientInfo.ClientID + " AND waybillno in ( select waybillno from waybill with(nolock) where RefNo in (" + _refNo + ")) and ClientID=" + ClientInfo.ClientID + " And RefNo in (" + _refNo + ")AND IsInternalType = 0 Order by Date", connection))

                        {
                            //Logger.Info(command.CommandText);
                            command.CommandTimeout = 0;



                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    IkeaTracking TCF = new IkeaTracking();
                                    IkeaTrackingerror LMF = new IkeaTrackingerror();


                                    if (reader["ClientID"].ToString().Count() > 0)
                                        TCF.ClientID = Convert.ToInt32(reader["ClientID"].ToString());
                                    else
                                        TCF.ClientID = ClientInfo.ClientID;

                                    var DObj = Convert.ToDateTime(reader["Date"]);
                                    var obj = DObj.ToString().Split(' ').ToList();
                                    TCF.Date = DateTime.Parse(obj[0]).ToString("dd-MM-yyyy");
                                    TCF.Time = DateTime.Parse(obj[1]).ToString("HH:mm");

                                    TCF.Activity = reader["Activity"].ToString();
                                    TCF.ArabicActivity = reader["ActivityAr"].ToString();

                                    if (Convert.ToInt32(reader["EventCode"].ToString()) == 0 || Convert.ToInt32(reader["EventCode"].ToString()) == 27)
                                        TCF.StationCode = "";
                                    else
                                        TCF.StationCode = reader["StationCode"].ToString();

                                    TCF.WaybillNo = Convert.ToInt32(reader["WaybillNo"].ToString());
                                    TCF.RefNo = reader["RefNo"].ToString();
                                    TCF.HasError = Convert.ToBoolean(reader["HasError"].ToString());
                                    TCF.ErrorMessage = reader["ErrorMessage"].ToString();
                                    LMF.ErrorMessage = reader["ErrorMessage"].ToString();
                                    TCF.Comments = reader["Comments"].ToString();
                                    TCF.ActivityCode = Convert.ToInt32(reader["TrackingTypeID"].ToString());

                                    TCF.EventCode = Convert.ToInt32(reader["EventCode"]);

                                    if (reader["DeliveryStatusID"].ToString().Count() > 0)
                                        TCF.DeliveryStatusID = Convert.ToInt32(reader["DeliveryStatusID"].ToString());

                                    if (reader["DeliveryStatusMessage"].ToString().Count() > 0)
                                        TCF.DeliveryStatusMessage = reader["DeliveryStatusMessage"].ToString();


                                    LM.Add(LMF);
                                    TC.Add(TCF);
                                }
                            }
                            connection.Close();
                        }


                    }
                    catch (Exception ex)
                    {
                        IkeaTrackingerror newActivity = new IkeaTrackingerror();

                        newActivity.ErrorMessage = "Kindly check after 10 mins";
                        LM.Add(newActivity);
                        LogException(ex);
                        connection.Close();

                    }

                }
            }
            else
            {
                IkeaTracking newActivity = new IkeaTracking();
                newActivity.Date = Convert.ToString(DateTime.Now);
                newActivity.ErrorMessage = "The username or password for this client is wrong, please make sure to pass correct credentials";
                newActivity.HasError = true;
                TC.Add(newActivity);
            }

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.TraceByMultiWaybillNo, WaybillNo.ToString(), 11);
            var json = new JavaScriptSerializer().Serialize(TC);
            if (TC.Count == 0)
            {
                var json2 = new JavaScriptSerializer().Serialize(LM);
                return json2;

            }
            else
            {
                return json;
            }
        }

        [WebMethod(Description = " You can use this function to Trace your RefNo status")]
        public List<Tracking> TraceByMultiRefNoAlt(ClientInformation ClientInfo, List<string> RefNo)
        {
            List<string> _refNos = new List<string>();

            List<ViwTracking> TC = new List<ViwTracking>();
            List<Tracking> Result = new List<Tracking>();
            foreach (string item in RefNo)
            {
                if (!string.IsNullOrEmpty(item.Trim()) && !item.Contains(";"))
                {
                    _refNos.Add(item.Trim());
                }
            }
            string refNos = string.Join(",", _refNos.ToArray());

            #region Data validation
            if (_refNos.Count() <= 0 || String.IsNullOrWhiteSpace(refNos))
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "Please provide a valid list of RefNo",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }

            if (RefNo.Count > 50)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "You can track maximum 50 WayBill in a call",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }

            if (ClientInfo.ClientID != 1024600 && ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "The username or password for this client is wrong, please make sure to pass correct credentials",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }
            #endregion

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var clientIDs = GetCrossTrackingClientIDs(ClientInfo.ClientID).Split(',').ToList();
            var WaybillNo = dc.CustomerWayBills.Where(x => clientIDs.Contains(x.ClientID.ToString()) && _refNos.Contains(x.RefNo)).Select(x => x.WayBillNo).ToList();
            if (WaybillNo.Count == 0)
            {
                Tracking newActivity = new Tracking
                {
                    ErrorMessage = "Please provide a valid list of RefNo",
                    HasError = true
                };
                Result.Add(newActivity);
                return Result;
            }
            Result = GetTrackingByMultiWaybillNos(ClientInfo.ClientID, WaybillNo);

            GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Trace_Shipment_By_RefNo, string.Join(",", WaybillNo), 11);
            return Result;
        }

        [WebMethod(Description = "You can use this function to create a new waybill in the system.")]
        public Result CreateWaybillAlt(ManifestShipmentDetailsAlt _ManifestShipmentDetailsAlt)
        {
            WritetoXMLUpdateWaybill(_ManifestShipmentDetailsAlt, _ManifestShipmentDetailsAlt.ClientInfo, _ManifestShipmentDetailsAlt.RefNo, EnumList.MethodType.CreateWaybillAlt);
            Result result = new Result();

            var tempCodes = GetCustomerCountryCodeAndCityCode(
                _ManifestShipmentDetailsAlt.ClientInfo.ClientID,
                _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.CountryName,
                _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ProvinceName,
                _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.CityName);

            if (tempCodes == null)
            {
                result.HasError = true;
                result.Message = "Invalid Consignee CountryName/ProvinceName/CityName, please contact Naqel team for further operations.";
                return result;
            }

            var tempManifest = new ManifestShipmentDetails()
            {
                ClientInfo = _ManifestShipmentDetailsAlt.ClientInfo,
                _CommercialInvoice = _ManifestShipmentDetailsAlt._CommercialInvoice,
                ConsigneeInfo = new ConsigneeInformation()
                {
                    CityCode = tempCodes.NaqelCityCode,
                    CountryCode = tempCodes.NaqelCountryCode,
                    Address = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.Address,
                    ConsigneeName = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeName,
                    ConsigneeNationalID = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeNationalID,
                    ConsigneePassportNo = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneePassportNo,
                    ConsigneePassportExp = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneePassportExp,
                    ConsigneeNationality = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeNationality,
                    ConsigneeBirthDate = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeBirthDate,
                    ConsigneeNationalIdExpiry = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeNationalIdExpiry,
                    District = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.District,
                    Email = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.Email,
                    Fax = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.Fax,
                    Mobile = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.Mobile,
                    PhoneNumber = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.PhoneNumber,
                    NationalAddress = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.NationalAddress,
                    Near = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ProvinceName + "|"
                        + _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.CityName + "|"
                        + _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.Near,
                    ParcelLockerMachineID = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.ParcelLockerMachineID,
                    What3Words = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.What3Words,
                    SPLOfficeID = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.SPLOfficeID,
                    consignee_serial = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.consignee_serial,
                    BuildingNo = _ManifestShipmentDetailsAlt.ConsigneeInfoAlt.BuildingNo
                },
                BillingType = _ManifestShipmentDetailsAlt.BillingType,
                RefNo = _ManifestShipmentDetailsAlt.RefNo,
                CODCharge = _ManifestShipmentDetailsAlt.CODCharge,
                CreateBooking = _ManifestShipmentDetailsAlt.CreateBooking,
                CurrenyID = _ManifestShipmentDetailsAlt.CurrenyID,
                Height = _ManifestShipmentDetailsAlt.Height,
                Width = _ManifestShipmentDetailsAlt.Width,
                Length = _ManifestShipmentDetailsAlt.Length,
                Weight = _ManifestShipmentDetailsAlt.Weight,
                VolumetricWeight = _ManifestShipmentDetailsAlt.VolumetricWeight,
                DeclareValue = _ManifestShipmentDetailsAlt.DeclareValue,
                DeliveryInstruction = _ManifestShipmentDetailsAlt.DeliveryInstruction,
                GeneratePiecesBarCodes = _ManifestShipmentDetailsAlt.GeneratePiecesBarCodes,
                GoodDesc = _ManifestShipmentDetailsAlt.GoodDesc,
                GoodsVATAmount = _ManifestShipmentDetailsAlt.GoodsVATAmount,
                InsuredValue = _ManifestShipmentDetailsAlt.InsuredValue,
                IsCustomDutyPayByConsignee = _ManifestShipmentDetailsAlt.IsCustomDutyPayByConsignee,
                isRTO = _ManifestShipmentDetailsAlt.isRTO,
                Latitude = _ManifestShipmentDetailsAlt.Latitude,
                LoadTypeID = _ManifestShipmentDetailsAlt.LoadTypeID,
                Longitude = _ManifestShipmentDetailsAlt.Longitude,
                PicesCount = _ManifestShipmentDetailsAlt.PicesCount,
                PickUpPoint = _ManifestShipmentDetailsAlt.PickUpPoint,
                PromisedDeliveryDateFrom = _ManifestShipmentDetailsAlt.PromisedDeliveryDateFrom,
                PromisedDeliveryDateTo = _ManifestShipmentDetailsAlt.PromisedDeliveryDateTo,
                Reference1 = _ManifestShipmentDetailsAlt.Reference1,
                Reference2 = _ManifestShipmentDetailsAlt.Reference2,
                Incoterm = _ManifestShipmentDetailsAlt.Incoterm,
                IncotermsPlaceAndNotes = _ManifestShipmentDetailsAlt.IncotermsPlaceAndNotes
            };

            try
            {
                result = CreateWaybill(tempManifest);
            }
            catch (Exception ex)
            {
                LogException(ex);
                result.HasError = true;
                result.Message = "Request failed. " + ex.Message;
                LogEvent(ex, "ErrorData/CreateWaybillAlt_Error_" + DateTime.Now.ToString("yyyyMMddhhmmss"));
            }

            return result;
        }

        [WebMethod(Description = "You can use this function to create an ASR waybill in the system.")]
        public AsrResult CreateWaybillForASRAlt(AsrManifestShipmentDetailsAlt _AsrManifestShipmentDetailsAlt)
        {
            WritetoXMLUpdateWaybill(_AsrManifestShipmentDetailsAlt, _AsrManifestShipmentDetailsAlt.ClientInfo, _AsrManifestShipmentDetailsAlt.RefNo, EnumList.MethodType.CreateWaybillForASRAlt);
            AsrResult result = new AsrResult();

            var tempCodes = GetCustomerCountryCodeAndCityCode(
                _AsrManifestShipmentDetailsAlt.ClientInfo.ClientID,
                _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.CountryName,
                _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ProvinceName,
                _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.CityName);

            if (tempCodes == null)
            {
                result.HasError = true;
                result.Message = "Invalid Consignee CountryName/ProvinceName/CityName, please contact Naqel team for further operations.";
                return result;
            }

            var SheinAccounts = new List<int> { 9019491, 9019912, 9017968, 9020044, 9020077 };
            if (SheinAccounts.Contains(_AsrManifestShipmentDetailsAlt.ClientInfo.ClientID))
            {
                int temp = ExtractAndSumNumbersFromGoodDesc(_AsrManifestShipmentDetailsAlt.GoodDesc);
                if (temp > 0)
                    _AsrManifestShipmentDetailsAlt.GoodDesc = "[" + temp.ToString() + " items] " + _AsrManifestShipmentDetailsAlt.GoodDesc;
            }

            var tempManifest = new AsrManifestShipmentDetails()
            {
                ClientInfo = _AsrManifestShipmentDetailsAlt.ClientInfo,
                _CommercialInvoice = _AsrManifestShipmentDetailsAlt._CommercialInvoice,
                ConsigneeInfo = new ConsigneeInformation()
                {
                    CityCode = tempCodes.NaqelCityCode,
                    CountryCode = tempCodes.NaqelCountryCode,
                    Address = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.Address,
                    ConsigneeName = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeName,
                    ConsigneeNationalID = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeNationalID,
                    ConsigneePassportNo = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneePassportNo,
                    ConsigneePassportExp = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneePassportExp,
                    ConsigneeNationality = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ConsigneeNationality,
                    District = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.District,
                    Email = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.Email,
                    Fax = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.Fax,
                    Mobile = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.Mobile,
                    PhoneNumber = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.PhoneNumber,
                    NationalAddress = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.NationalAddress,
                    Near = _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.ProvinceName + "|"
                        + _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.CityName + "|"+ _AsrManifestShipmentDetailsAlt.ConsigneeInfoAlt.Near
                },
                BillingType = _AsrManifestShipmentDetailsAlt.BillingType,
                RefNo = _AsrManifestShipmentDetailsAlt.RefNo,
                CODCharge = _AsrManifestShipmentDetailsAlt.CODCharge,
                CreateBooking = _AsrManifestShipmentDetailsAlt.CreateBooking,
                Height = _AsrManifestShipmentDetailsAlt.Height,
                Width = _AsrManifestShipmentDetailsAlt.Width,
                Length = _AsrManifestShipmentDetailsAlt.Length,
                Weight = _AsrManifestShipmentDetailsAlt.Weight,
                VolumetricWeight = _AsrManifestShipmentDetailsAlt.VolumetricWeight,
                DeclareValue = _AsrManifestShipmentDetailsAlt.DeclareValue,
                DeliveryInstruction = _AsrManifestShipmentDetailsAlt.DeliveryInstruction,
                GeneratePiecesBarCodes = _AsrManifestShipmentDetailsAlt.GeneratePiecesBarCodes,
                GoodDesc = _AsrManifestShipmentDetailsAlt.GoodDesc,
                GoodsVATAmount = _AsrManifestShipmentDetailsAlt.GoodsVATAmount,
                InsuredValue = _AsrManifestShipmentDetailsAlt.InsuredValue,
                IsCustomDutyPayByConsignee = _AsrManifestShipmentDetailsAlt.IsCustomDutyPayByConsignee,
                isRTO = _AsrManifestShipmentDetailsAlt.isRTO,
                Latitude = _AsrManifestShipmentDetailsAlt.Latitude,
                LoadTypeID = _AsrManifestShipmentDetailsAlt.LoadTypeID,
                Longitude = _AsrManifestShipmentDetailsAlt.Longitude,
                PicesCount = _AsrManifestShipmentDetailsAlt.PicesCount,
                PromisedDeliveryDateFrom = _AsrManifestShipmentDetailsAlt.PromisedDeliveryDateFrom,
                PromisedDeliveryDateTo = _AsrManifestShipmentDetailsAlt.PromisedDeliveryDateTo,
                Reference1 = _AsrManifestShipmentDetailsAlt.Reference1,
                Reference2 = _AsrManifestShipmentDetailsAlt.Reference2,
                CurrencyID = _AsrManifestShipmentDetailsAlt.CurrencyID,
                OriginWaybillNo = _AsrManifestShipmentDetailsAlt.OriginWaybillNo,
                PickUpDate = _AsrManifestShipmentDetailsAlt.PickUpDate,
                WaybillNo = _AsrManifestShipmentDetailsAlt.WaybillNo,
                WaybillSurcharge = _AsrManifestShipmentDetailsAlt.WaybillSurcharge
            };

            try
            {
                result = CreateWaybillForASR(tempManifest);
            }
            catch (Exception ex)
            {
                LogException(ex);
                result.HasError = true;
                result.Message = "Request failed. " + ex.Message;
                LogEvent(ex, "ErrorData/CreateWaybillForASRAlt_Error_" + DateTime.Now.ToString("yyyyMMddhhmmss"));
            }

            return result;
        }

        private int ExtractAndSumNumbersFromGoodDesc(string input)
        {
            // Split by '|'
            string[] parts = input.Split('|');
            int sum = 0;

            foreach (string part in parts)
            {
                // Split by '*'
                string[] splitPart = part.Split('*');
                if (splitPart.Length > 0)
                {
                    // Sum first part
                    if (int.TryParse(splitPart[1], out int number))
                    {
                        sum += number;
                    }
                }
            }

            return sum;
        }

        public class NaqelCountryAndCityCode
        {
            public string NaqelCountryCode { get; set; }
            public string NaqelCityCode { get; set; }
        }

        public NaqelCountryAndCityCode GetCustomerCountryCodeAndCityCode(int ClientID, string CountryName, string ProvinceName, string CityName)
        {
            string sql = @"select a.NaqelCountryCode, b.Code as NaqelCityCode from CustomerCityList a left join City b on a.NaqelCityID = b.ID
                where a.ClientID = (select ParentClientID from ClientIDGroup where StatusID = 1 and ClientID = " + ClientID + @") 
                and a.CountryName = '" + CountryName.Trim().Replace("'", "''") + @"'
                and a.ProvinceName = '" + ProvinceName.Trim().Replace("'", "''") + @"'
                and a.CityName = '" + CityName.Trim().Replace("'", "''") + @"'
                and a.StatusID = 1
                and b.StatusID = 1";

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            using (var db = new SqlConnection(con))
            {
                NaqelCountryAndCityCode temCode = db.Query<NaqelCountryAndCityCode>(sql).ToList().FirstOrDefault();
                return temCode;
            }
        }


        [WebMethod(Description = "You can cancel your shipment using this function.")]
        public CancelWaybillResult CancelWaybill(ClientInformation _clientInfo, int WaybillNo, string CancelReason)
        {
            WritetoXMLUpdateWaybill(
                new CancelWaybillRequest() { ClientInfo = _clientInfo, WaybillNo = WaybillNo, CancelReason = CancelReason },
                _clientInfo,
                WaybillNo.ToString(),
                EnumList.MethodType.CancelWaybill);

            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            var dcHHD = new InfoTrack.BusinessLayer.DContext.HHDDataContext();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            CancelWaybillResult Result = new CancelWaybillResult() { IsCanceled = false };

            try
            {
                if (_clientInfo.CheckClientInfo(_clientInfo, false).HasError)
                {
                    Result.Message = "Client authentication failed.";
                    return Result;
                }

                if (!IsWBExist(Convert.ToString(WaybillNo), _clientInfo.ClientID))
                {
                    string list1 = System.Configuration.ConfigurationManager.AppSettings["FirstCryAcc"].ToString();
                    List<int> FirstCryAcc = list1.Split(',').Select(Int32.Parse).ToList();

                    if (FirstCryAcc.Contains(_clientInfo.ClientID))
                    {
                        Result.IsCanceled = true;
                        Result.Message = WaybillNo.ToString() + " canceled successfully.";
                        return Result;
                    }

                    Result.Message = "The shipment already canceled or can not be found.";
                    return Result;
                }

                //if (dcHHD.PickUps.Where(P => P.WaybillNo == WaybillNo && P.StatusID == 1).Count() > 0)
                if (IsPickupExist(WaybillNo, _clientInfo.ClientID))
                {
                    Result.Message = "This shipment already picked up, cancellation can not be done.";
                    return Result;
                }

                // 1. Check waybill has booking record
                // 2. Check in the same date if customerwaybills table has more records
                // 3. if more records, cancel cutomerwaybills
                // 4. if one record, canlcel both booking and customerwaybills

                CancelReason = GlobalVar.GV.GetString(CancelReason, 50);
                if (CancelReason.Length <= 0)
                    CancelReason = "[API] Cancelled by customer";

                // Get ClientIDGroup
                string clientAccounts = GetCrossTrackingClientIDs(_clientInfo.ClientID);
                List<int> clientAccountsList = clientAccounts.Split(',').Select(int.Parse).ToList();

                var tempCustomerWaybill = dc.CustomerWayBills.Where(w => clientAccountsList.Contains(_clientInfo.ClientID)
                && w.WayBillNo == WaybillNo
                && w.StatusID == 1).First();

                var tempBooking = dc.Bookings.Where(b => clientAccountsList.Contains(_clientInfo.ClientID)
                && b.BookingDate.Value.Date == tempCustomerWaybill.ManifestedTime.Value.Date
                && b.IsCanceled == false).Count();

                // Cancel CustomerWaybills : any cancel request
                // Cancle Booking: one customerWaybills under this client and 1+ booking under this client
                if (tempBooking > 1) // has more than 1 booking records under the clientid
                {
                    var tempBookingWithWaybillNo = dc.Bookings.Where(b => b.WaybillNo == WaybillNo
                    && b.BookingDate.Value.Date == tempCustomerWaybill.ManifestedTime.Value.Date
                    && b.IsCanceled == false).FirstOrDefault();
                    // Cancel the booking with waybillno is exist
                    if (tempBookingWithWaybillNo != null)
                    {
                        Booking objBooking = dc.Bookings.Where(b => b.WaybillNo == WaybillNo).First();
                        objBooking.IsCanceled = true;
                        objBooking.CanceledReason = CancelReason;
                        dc.SubmitChanges();
                    }
                }

                int customerwaybillID = dc.CustomerWayBills.FirstOrDefault(w => w.WayBillNo == WaybillNo && w.StatusID == 1).ID;

                var tempCnt = dc.CustomerWayBills
                    .Where(w => clientAccountsList.Contains(_clientInfo.ClientID)
                        && w.ManifestedTime.Value.Date == tempCustomerWaybill.ManifestedTime.Value.Date
                        && w.StatusID == 1)
                    .Count();

                if (tempCnt <= 1)
                {
                    CustomerWayBill obj = dc.CustomerWayBills.Where(w => w.WayBillNo == WaybillNo && w.StatusID == 1).First();
                    obj.StatusID = 3;
                    obj.Reference2 = CancelReason;
                    CustomerBarCode BC = dc.CustomerBarCodes.Where(W => W.CustomerWayBillsID == customerwaybillID && W.StatusID == 1).First();
                    BC.StatusID = 3;
                    Booking objBooking = dc.Bookings.Where(b => clientAccountsList.Contains(_clientInfo.ClientID)
                    && b.BookingDate.Value.Date == tempCustomerWaybill.ManifestedTime.Value.Date
                    && b.IsCanceled == false).FirstOrDefault();
                    if (objBooking != null)
                    {
                        objBooking.IsCanceled = true;
                        objBooking.CanceledReason = CancelReason;
                    }
                    dc.SubmitChanges();
                }
                else
                {
                    CustomerWayBill obj = dc.CustomerWayBills.Where(w => w.WayBillNo == WaybillNo && w.StatusID == 1).First();
                    obj.StatusID = 3;
                    obj.Reference2 = CancelReason;

                    CustomerBarCode BC = dc.CustomerBarCodes.Where(W => W.CustomerWayBillsID == customerwaybillID && W.StatusID == 1).First();
                    BC.StatusID = 3;
                    dc.SubmitChanges();
                }

                //add the canceled waybill in Cancelwaybill table

                CancelWaybill CanceledWaybill = new CancelWaybill();
                //CanceledWaybill.ID =
                CanceledWaybill.WayBillNo = WaybillNo;
                CanceledWaybill.Date = DateTime.Now;
                CanceledWaybill.BookingRefNo = tempCustomerWaybill.BookingRefNo;
                CanceledWaybill.ClientContactID = tempCustomerWaybill.ClientContactID;


                dcMaster.CancelWaybills.InsertOnSubmit(CanceledWaybill);

                bool needRetryAddCancelledWaybill = true;
            RetryAddCancelledWaybill:
                try
                {
                    dcMaster.SubmitChanges();
                }
                catch (Exception ex)
                {
                    if (needRetryAddCancelledWaybill)
                        goto RetryAddCancelledWaybill;
                    needRetryAddCancelledWaybill = false;
                    throw ex;
                }

                Result.IsCanceled = true;
                Result.Message = WaybillNo.ToString() + " canceled successfully.";
                return Result;
            }
            catch (Exception e)
            {
                LogException(e);
                Result.Message = "Please resend the request.";
            }


            return Result;
        }

        [WebMethod(Description = "You can cancel your shipment using this function.")]
        public CancelWaybillResult CancelWaybillbyRef(ClientInformation _clientInfo, string RefNo, string CancelReason)
        {
            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var dcHHD = new InfoTrack.BusinessLayer.DContext.HHDDataContext();
            CancelWaybillResult Result = new CancelWaybillResult();

            try
            {
                if (_clientInfo.CheckClientInfo(_clientInfo, false).HasError)
                {
                    Result.IsCanceled = false;
                    Result.Message = "Client authentication failed.";
                    return Result;
                }

                if (!IsRefNoExist(Convert.ToString(RefNo), _clientInfo.ClientID))
                {
                    Result.IsCanceled = false;
                    Result.Message = "The shipment already canceled or can not be found.";
                    return Result;
                }

                // 1. Check waybill has booking record
                // 2. Check in the same date if customerwaybills table has more records
                // 3. if more records, cancel cutomerwaybills
                // 4. if one record, canlcel both booking and customerwaybills

                CancelReason = GlobalVar.GV.GetString(CancelReason, 200);

                var wbList = dc.CustomerWayBills
                    .Where(w => w.ClientID == _clientInfo.ClientID && w.RefNo == RefNo && w.StatusID == 1)
                    .Select(w => w.WayBillNo)
                    .ToList();

                foreach (var wb in wbList)
                {
                    Result = CancelWaybill(_clientInfo, wb, CancelReason);
                }

                return Result;
            }
            catch (Exception e)
            {
                LogException(e);
            }

            return Result;
        }

        [WebMethod(Description = "You can update reweight data using this function.")]
        public Result UpdateReweight(ClientInformation ClientInfo, int WaybillNo, double Length = 1, double Width = 1, double Height = 1, double Weight = 0)
        {
            AddAPIError(ClientInfo.ClientID, "ReWt");

            WritetoXMLUpdateWaybill(
                new UpdateReweightRequest() { ClientInfo = ClientInfo, WaybillNo = WaybillNo, Length = Length, Width = Width, Height = Height, Weight = Weight },
                ClientInfo,
                WaybillNo.ToString(),
                EnumList.MethodType.UpdateReweight);

            Result result = new Result();

            #region Data validation
            if (Length <= 0 || Width <= 0 || Height <= 0 || Weight <= 0)
            {
                result.HasError = true;
                result.Message = @"Invalid package size/weight.";
                return result;
            }

            if (!IsValidWBFormat(WaybillNo.ToString()))
            {
                result.HasError = true;
                result.Message = "Invalid given WaybillNo.";
                return result;
            }

            result = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (result.HasError)
            {
                return result;
            }
            #endregion

            var dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var tempOrigWaybill = dc.CustomerWayBills.Where(c => c.WayBillNo == WaybillNo && c.StatusID == 1).FirstOrDefault();

            if (tempOrigWaybill == null || !GetCrossTrackingClientIDs(ClientInfo.ClientID).Contains(tempOrigWaybill.ClientID.ToString()))
            {
                result.HasError = true;
                result.Message = "Waybill not found.";
                return result;
            }

            if (IsPickupExist(WaybillNo, ClientInfo.ClientID) || dc.Waybills.Where(x => x.ClientID == ClientInfo.ClientID && x.WayBillNo == WaybillNo && !x.IsCancelled).Any())
            {
                result.HasError = true;
                result.Message = $"This Waybill {WaybillNo} alrady picked up by Naqel ";
                return result;
            }

            // Get VolumeDivisor
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            List<int> vdList = new List<int>();
            string sql = @"select VolumeDivisor from Client where StatusID = 1 and ID = " + tempOrigWaybill.ClientID;
            using (var db = new SqlConnection(con))
            {
                vdList = db.Query<int>(sql).ToList();
            }
            if (vdList.Count() == 0)
            {
                result.HasError = true;
                result.Message = "Please contact Naqel team to update you client setting.";
                return result;
            }

            int vd = vdList[0];

            tempOrigWaybill.Length = Length;
            tempOrigWaybill.Width = Width;
            tempOrigWaybill.Height = Height;
            tempOrigWaybill.VolumeWeight = Math.Round((Length * Width * Height) / vd, 2);
            tempOrigWaybill.Weight = Weight;

            try { dc.SubmitChanges(); }
            catch (Exception e)
            {
                LogException(e);
                result.HasError = true;
                result.Message = "Error during update reweight data.";
                GlobalVar.GV.AddErrorMessage(e, ClientInfo);
                return result;
            }

            result.HasError = false;
            result.Message = "Reweight data updated successfully.";
            return result;
        }

        [WebMethod(Description = "You can use this function to get multiple waybill sticker file as Byte[]")]
        public MultiStickerResult GetMultiWaybillSticker(ClientInformation clientInfo, List<int> WaybillNumbers, StickerSize StickerSize)
        {
            MultiStickerResult result = new MultiStickerResult();

            #region Data validation
            if (WaybillNumbers.Count > 50)
            {
                result.HasError = true;
                result.Message = "Limit exceeded..!, maximum 50 waybill can be printed.";
                return result;
            }

            foreach (var wb in WaybillNumbers.Distinct())
            {
                if (!IsValidWBFormat(wb.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Invalid WaybillNo format in the list.";
                    return result;
                }
            }

            var tempResult = clientInfo.CheckClientInfo(clientInfo, false);
            if (tempResult.HasError)
            {
                result.HasError = true;
                result.Message = tempResult.Message;
                return result;
            }

            if (!IsWBBelongsToClientGeneral(clientInfo.ClientID, WaybillNumbers))
            {
                result.HasError = true;
                result.Message = "Invalid WaybillNo under current credential.";
                return result;
            }
            #endregion

            List<int> AsrWBList = new List<int>();
            List<int> B2BWBList = new List<int>();
            List<int> WBList = new List<int>();

            List<StickerSize> AsrStickerSizes = new List<StickerSize>() {
                StickerSize.FourMSixthInches,
                StickerSize.FourMEightInches,
                StickerSize.A4
            };

            foreach (var wb in WaybillNumbers.Distinct())
            {
                if (IsB2BWaybill(clientInfo.ClientID, wb) && StickerSize == XMLShippingService.StickerSize.FourMSixthInches)
                {
                    B2BWBList.Add(wb);
                    continue;
                }

                if (!IsAsrWaybill(clientInfo.ClientID, wb))
                {
                    WBList.Add(wb);
                    continue;
                }

                if (!AsrStickerSizes.Contains(StickerSize))
                {
                    result.HasError = true;
                    result.Message = "StickerSize is not supported for ASR orders.";
                    return result;
                }
                else
                    AsrWBList.Add(wb);
            }

            string ForwardStickerFileName = "";
            string AsrStickerFileName = "";
            string B2BStickerFileName = "";
            //string finalFileName = Server.MapPath(".") + "\\WaybillStickers\\"
            //    + clientInfo.ClientID.ToString() + "_finalFile_"
            //    + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()) 
            //    + DateTime.Now.ToFileTimeUtc() + ".pdf";
            string finalFileName = Server.MapPath(".") + "\\WaybillStickers\\" + clientInfo.ClientID + "_finalFile_" + DateTime.Now.ToFileTimeUtc() + ".pdf";


            using (PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor())
            {
                pdfDocumentProcessor.CreateEmptyDocument(finalFileName);

                if (AsrWBList.Count() > 0)
                {
                    AsrStickerFileName = GenerateLabelStickerASR(clientInfo, AsrWBList, StickerSize);
                    pdfDocumentProcessor.AppendDocument(AsrStickerFileName);
                    File.Delete(AsrStickerFileName);
                }

                if (B2BWBList.Count() > 0)
                {
                    B2BStickerFileName = GenerateLabelSticker(clientInfo, B2BWBList, StickerSize);
                    pdfDocumentProcessor.AppendDocument(B2BStickerFileName);
                    File.Delete(B2BStickerFileName);
                }

                if (WBList.Count() > 0)
                {
                    ForwardStickerFileName = GenerateLabelSticker(clientInfo, WBList, StickerSize);
                    pdfDocumentProcessor.AppendDocument(ForwardStickerFileName);
                    File.Delete(ForwardStickerFileName);
                }
            }

            FileStream finalFileStream = File.OpenRead(finalFileName);
            result.StickerByte = GlobalVar.GV.ConvertStreamToByteBuffer(finalFileStream);
            result.HasError = false;
            finalFileStream.Close();
            return result;
        }

        [WebMethod(Description = "You can use this function to get collected shipment list in recent 3 months.")]
        public PickupShipmentResult GetPickupOrders(ClientInformation ClientInfo, DateTime FromDatetime, DateTime ToDatetime)
        {
            PickupShipmentResult result = new PickupShipmentResult();

            #region Data validation
            string list = System.Configuration.ConfigurationManager.AppSettings["CheckPickupList"].ToString();
            List<int> ClientValidation = list.Split(',').Select(Int32.Parse).ToList();

            if (!ClientValidation.Contains(ClientInfo.ClientID))
            {
                result.HasError = true;
                result.Message = @"You have no permission to check the pickup detail.";
                return result;
            }
            if (FromDatetime == null || ToDatetime == null)
            {
                result.HasError = true;
                result.Message = @"Invalid From/To Datetime.";
                return result;
            }

            if (FromDatetime > DateTime.Now || ToDatetime > DateTime.Now)
            {
                result.HasError = true;
                result.Message = @"From/To Datetime should be early than current time.";
                return result;
            }

            if (FromDatetime > ToDatetime || FromDatetime < Convert.ToDateTime("2020-01-01") || ToDatetime < Convert.ToDateTime("2020-01-01"))
            {
                result.HasError = true;
                result.Message = @"Check the From/To Datetime.";
                return result;
            }

            var res = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (res.HasError)
            {
                result.HasError = true;
                result.Message = res.Message;
                return result;
            }
            #endregion

            List<PickupShipment> shipments;
            string sql = @"SELECT WaybillNo, RefNo, PicesCount AS PiecesCount, PickUpDate AS PickupTime 
                FROM dbo.Waybill WHERE ClientID = " + ClientInfo.ClientID + @" 
                AND PickUpDate > DATEADD(Month, -3, GETDATE())
                AND PickUpDate > '" + FromDatetime.ToString("yyyy-MM-dd HH:mm:ss") + @"' 
                AND PickUpDate < '" + ToDatetime.ToString("yyyy-MM-dd HH:mm:ss") + @"';";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                shipments = connection.Query<PickupShipment>(sql).ToList();
            }

            result.PickupShipments = shipments;
            return result;
        }

        // Will active this function later once needed.
        // [WebMethod(Description = "You can use this function to get Waybill destination airport station code.")]
        public ShipmentDestinationResult GetDestStation(string Token, List<int> WaybillNo)
        {
            ShipmentDestinationResult result = new ShipmentDestinationResult();

            #region Data Validation
            string _token = DateTime.UtcNow.ToString("yyyyMMdd_Naqel");

            if (Token != _token)
            {
                result.HasError = true;
                result.Message = "Invalid token.";
                return result;
            }

            if (WaybillNo.Count == 0)
            {
                return result;
            }

            if (WaybillNo.Count > 100)
            {
                result.HasError = true;
                result.Message = "You can track maximum 100 WayBill in a call.";
                return result;
            }

            List<int> _waybillNo = new List<int>();
            foreach (int item in WaybillNo)
            {
                if (!IsValidWBFormat(item.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Invalid WaybillNo: " + item;
                    return result;
                }
                else
                    _waybillNo.Add(item);
            }
            string WBno = string.Join(",", _waybillNo.ToArray());
            #endregion

            List<ShipmentDestination> shipments;
            string sql = @"select a.WaybillNo,
                case when b.StatusID != 1 or c.StatusID != 1 or d.StatusID != 1 then 'Invalid'
                when a.DestinationStationID in (511, 508, 517, 516, 918, 512, 502, 2345, 514, 515, 1891, 142, 507, 2555, 504) then 'JED'
                when c.CountryID = 1 then 'RUH'
                else d.Code end as Destination, 
                a.ClientID, e.Name as ClientName
                from CustomerWaybills a
                left join Station b on a.DestinationStationID = b.ID
                left join City c on b.CityID = c.ID
                left join Country d on c.CountryID = d.ID
                left join Client e on a.ClientID = e.ID
                where a.WaybillNo in (" + WBno + @"
                );";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                shipments = connection.Query<ShipmentDestination>(sql).ToList();
            }

            result.ShipmentDestinations = shipments;
            return result;
        }

        [WebMethod(Description = "You can use this function to get ASR waybill report.")]
        public ASRDetailResult GetASRDetails(ClientInformation ClientInfo, List<int> WaybillNo)
        {
            ASRDetailResult result = new ASRDetailResult();

            #region Data Validation
            string list = System.Configuration.ConfigurationManager.AppSettings["CheckASRDetailList"].ToString();
            List<int> ClientValidation = list.Split(',').Select(Int32.Parse).ToList();

            if (!ClientValidation.Contains(ClientInfo.ClientID))
            {
                result.HasError = true;
                result.Message = @"You have no permission to check the pickup detail.";
                return result;
            }

            if (WaybillNo.Count == 0)
            {
                return result;
            }

            if (WaybillNo.Count > 100)
            {
                result.HasError = true;
                result.Message = "You can track maximum 100 WayBill in a call.";
                return result;
            }

            var res = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (res.HasError)
            {
                result.HasError = true;
                result.Message = res.Message;
                return result;
            }

            List<int> _waybillNo = new List<int>();
            foreach (int item in WaybillNo)
            {
                if (!IsValidWBFormat(item.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Invalid WaybillNo: " + item;
                    return result;
                }
                else
                    _waybillNo.Add(item);
            }
            string WBno = string.Join(",", _waybillNo.ToArray());
            #endregion

            List<ASRDetail> shipments;
            string sql = @"select distinct WayBillNo as WaybillNo, BookingRefNo As ReferenceNo, 
                PicesCount AS PiecesCount, ManifestedTime AS ManifestedDate, 
                Origin, Destination, ConsigneeName, ConsigneeMobile AS PhoneNo, 
                Mobile AS ConsigneeMobile, PickUpDate, IsPickedUp, LastStatus, AttemptedCount
                from viwASR
                where ClientID = " + ClientInfo.ClientID + @"
                and WaybillNo in (" + WBno + @"
                );";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                shipments = connection.Query<ASRDetail>(sql).ToList();
            }

            result.ASRDetails = shipments;
            return result;
        }


        [WebMethod(Description = "You can get transit Days by using this function")]
        public TTresult GetTransitDays(ClientInformation ClientInfo, string Origin, string Destination, int loadtypeID)
        {

            TTresult Result = new TTresult();
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Result.Message = "Invalid credential!";
                return Result;
            }
            if (!GlobalVar.GV.IseCorrectCityCode(Origin))
            {
                Result.Message = "Invalid OriginCityCode!";
                return Result;
            }
            if (!GlobalVar.GV.IseCorrectCityCode(Destination))
            {
                Result.Message = "Invalid DestinationCityCode!";
                return Result;
            }

            if (!GlobalVar.GV.IsLoadTypeCorrect(ClientInfo, loadtypeID))
            {
                Result.Message = "Please Pass correct Loadtype!";
                return Result;
            }
            if (loadtypeID == 0)
            {
                Result.Message = "Please Pass Loadtype!";
                return Result;
            }


            //int Transitdays;
            //string sql = "exec TTDaysCount " + ClientInfo.ClientID + ",'" + origin + "','" + Destination + "'," + loadtypeID + "";
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT [dbo].fnTTCBU(@LoadTypeID,@OriginID,@DestID, @ClientID) As Days", connection);
                command.CommandType = CommandType.Text;
                command.Parameters.Add(new SqlParameter("@LoadTypeID", loadtypeID));
                command.Parameters.Add(new SqlParameter("@OriginID", GlobalVar.GV.GetStationIDByStationCode(Origin)));
                command.Parameters.Add(new SqlParameter("@DestID", GlobalVar.GV.GetStationIDByStationCode(Destination)));
                command.Parameters.Add(new SqlParameter("@ClientID", ClientInfo.ClientID));

                //command.Connection.Open();

                //command.Parameters.Add(new SqlParameter("@LoadTypeID" , loadtypeID + "@OriginID", origin + "@DestID", Destination +" @ClientID", ClientInfo.ClientID));
                //int result = command.ExecuteReader();
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                    {
                        //Result.Days = Convert.ToInt32(reader["Days"]);
                        Result.Days = Convert.ToInt32(reader["Days"]);
                    }
                connection.Close();

            }
            //using (SqlConnection connection = new SqlConnection(sqlCon))
            //{
            //    Transitdays = connection.Query<int>(sql).FirstOrDefault();
            //}
            //Result.Days = Transitdays;
            return Result;

        }

        //[WebMethod(Description = "You can get the Rate by using this function")]
        //public TTresultPrice GetRate(ClientInformation ClientInfo, float Weight, int LoadTypeID)
        //{
        //    TTresultPrice Result = new TTresultPrice();
        //    #region Data validation
        //    if (Weight <= 0)
        //    {
        //        Result.Message = "Please Pass weight!";
        //        return Result;
        //    }

        //    if (LoadTypeID <= 0)
        //    {
        //        Result.Message = "Please Pass Loadtype!";
        //        return Result;
        //    }

        //    if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
        //    {
        //        Result.Message = "Invalid credential!";
        //        return Result;
        //    }

        //    if (!GlobalVar.GV.IsLoadTypeCorrect(ClientInfo, LoadTypeID))
        //    {
        //        Result.Message = "Please Pass correct Loadtype!";
        //        return Result;
        //    }
        //    #endregion

        //    string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
        //    using (SqlConnection connection = new SqlConnection(con))
        //    {
        //        connection.Open();
        //        SqlCommand command = new SqlCommand("SELECT [dbo].Fn_GetPriceByWeight3(@LoadTypeID, @ClientID, @Weight) As Price", connection);
        //        command.CommandType = CommandType.Text;
        //        command.Parameters.Add(new SqlParameter("@LoadTypeID", LoadTypeID));
        //        command.Parameters.Add(new SqlParameter("@Weight", Weight));
        //        command.Parameters.Add(new SqlParameter("@ClientID", ClientInfo.ClientID));
        //        using (SqlDataReader reader = command.ExecuteReader())
        //            if (reader.Read())
        //            {
        //                if (float.TryParse(reader["Price"].ToString(), out float PriceVal))
        //                {
        //                    Result.Price = PriceVal;
        //                }
        //            }
        //        connection.Close();
        //    }
        //    if (Result.Price == 0)
        //    {
        //        Result.Message = "Please enter valid Weight";
        //    }
        //    return Result;
        //}
        [WebMethod(Description = "You can get the Rate by using this function")]
        public TTresultPrice GetRate(ClientInformation ClientInfo, float Weight, int LoadTypeID, DateTime FromDate, string Origin, string Destination)
        {
            TTresultPrice Result = new TTresultPrice();

            #region Data validation
            if (Weight <= 0)
            {
                Result.Message = "Please Pass Correct Weight!";
                return Result;
            }

            if (LoadTypeID <= 0)
            {
                Result.Message = "Please Pass Loadtype!";
                return Result;
            }

            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Result.Message = "Invalid credential!";
                return Result;
            }
            //get the staion ID 
            if (string.IsNullOrWhiteSpace(Origin) || !GlobalVar.GV.IsCityCodeExist(Origin, true))
            {
                Result.Message = "Invalid OriginCityCode!";
                return Result;
            }
            //get the staion ID - ( same conpet of waybill generation. 
            if (string.IsNullOrWhiteSpace(Destination) || !GlobalVar.GV.IsCityCodeExist(Destination, true))
            {
                Result.Message = "Invalid DestinationCityCode!";
                return Result;
            }

            if (!GlobalVar.GV.IsLoadTypeCorrect(ClientInfo, LoadTypeID))
            {
                Result.Message = "Please Pass correct Loadtype!";
                return Result;
            }
            #endregion

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            var originCity = dcMaster.Cities.Where(s => s.StatusID == 1 && s.DivisionID == 5 && s.Code.Trim().ToLower() == Origin.Trim().ToLower()).FirstOrDefault();
            var destinationCity = dcMaster.Cities.Where(s => s.StatusID == 1 && s.DivisionID == 5 && s.Code.Trim().ToLower() == Destination.Trim().ToLower()).FirstOrDefault();

            var doc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (doc.APIClientAndSubClients.Where(p => p.pClientID == ClientInfo.ClientID).Count() > 0)
            {
                var subClient = doc.APIClientAndSubClients.FirstOrDefault(p => p.pClientID == ClientInfo.ClientID
                && p.DestCountryId == destinationCity.CountryID
                && p.OrgCountryId == originCity.CountryID
                && p.StatusId == 1);
                if (subClient != null)
                {
                    ClientInfo.ClientID = subClient.cClientID;
                    LoadTypeID = Convert.ToInt32(subClient.LoadTypeID);
                }
            }

            if ((LoadTypeID == 34 && originCity.CountryID == destinationCity.CountryID) ||
                LoadTypeID == 36 && originCity.CountryID != destinationCity.CountryID)
            {
                Result.Message = "Wrong LoadTypeID or Origin/Destination.";
                return Result;
            }

            var originStationID = originCity.StationID.Value;
            var destinationStationID = destinationCity.StationID.Value;

            InfoTrack.Common.Shipments shipment = new InfoTrack.Common.Shipments("");
            bool IsNeedFraction = false;
            int HWStating = 250;
            var selectedClient = dcMaster.Clients.Where(c => c.ID == ClientInfo.ClientID).FirstOrDefault();
            if (selectedClient != null)
            {
                IsNeedFraction = selectedClient.ISNeedFraction ?? false;
                HWStating = selectedClient.HWStarting ?? 0;
            }
            double rate = shipment.GetShipmentValue(ClientInfo.ClientID, LoadTypeID, FromDate, originStationID, destinationStationID, Weight, IsNeedFraction, HWStating, ClientInfo.ClientID, 1, false);
            Result.Price = rate;


            return Result;
        }


        [WebMethod(Description = "You can get the detailed Rate by using this function, it is for specific EBU clients")]
        // GetRateEBU function return the price for Exp( servise type=4) clients - we support spesific clients not for  all EXP clients - clculate oda destination charg
        public TTresultPriceDetails GetRateEBU(ClientInformation ClientInfo, string LoadTypeID, string Origin, string Destination, string Weight)//SARA 
        {
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            TTresultPriceDetails Result = new TTresultPriceDetails();
            #region Data validation


            #region Solve Soap Exeption: System.InvalidOperationException: There is an error in XML document (14, 26). ---> System.FormatException: Input string was not in a correct format.



            if (string.IsNullOrEmpty(LoadTypeID) || string.IsNullOrWhiteSpace(LoadTypeID) || !int.TryParse(LoadTypeID, out int lt))// to avoid one of soap exeption
            {
                Result.Message = "Please Pass Valid Loadtype!";
                return Result;
            }

            int _LoadTypeID = int.Parse(LoadTypeID);


            if (string.IsNullOrEmpty(Weight) || string.IsNullOrWhiteSpace(Weight) || !double.TryParse(Weight, out double weight))// to avoid one of soap exeption
            {
                Result.Message = "Please Pass Valid Weight!";
                return Result;
            }

            double _Weight = double.Parse(Weight);
            #endregion

            if (_LoadTypeID <= 0)
            {
                Result.Message = "Please Pass Valid Loadtype!";
                return Result;
            }
            if (_Weight <= 0)
            {
                Result.Message = "Please Pass Valid Weight!";
                return Result;
            }



            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Result.Message = "Invalid credential!";
                return Result;
            }

            if (string.IsNullOrWhiteSpace(Origin) || !GlobalVar.GV.IseCorrectCityCode(Origin))
            {
                Result.Message = "Invalid OriginCityCode!";
                return Result;
            }

            if (string.IsNullOrWhiteSpace(Destination) || !GlobalVar.GV.IseCorrectCityCode(Destination))
            {
                Result.Message = "Invalid DestinationCityCode!";
                return Result;
            }

            if (!GlobalVar.GV.IsLoadTypeCorrect(ClientInfo, _LoadTypeID))
            {
                Result.Message = "Please Pass correct Loadtype!";
                return Result;
            }

            List<int> SupportedClient = new List<int> { 9025734, 9017669, 9017869 };
            // Make sure they are one of our supported clients
            if (!SupportedClient.Contains(ClientInfo.ClientID))
            {
                Result.Message = $"The  express account: {ClientInfo.ClientID} is not supported to use this function.";//Client is exist in our view but need to verify aggrement and add him to  SupportedClient { 9017669, 9017669 };     
                return Result;

            }

            if (dcMaster.LoadTypes.Where(s => s.ID == _LoadTypeID && s.ServiceTypeID == 4).Count() <= 0)//Make sure the loade type is express(4) 
            {
                Result.Message = "This function is for Express load Types only, For courier load types pleas use GetRate() function";// the client trys to use courier load type. this function is inly works for express load types
                return Result;
            }


            List<int> SupporteLoatTypes = new List<int> { 7, 39 };

            if (!SupporteLoatTypes.Contains(_LoadTypeID))
            {
                Result.Message = $"The lode type: {LoadTypeID} is not supported currently. This function only support 7 and 39 load types.";
                return Result;
            }

            #endregion

            // There are many loadtypes and there are diffirent concept for each , the coomn vairable betwwen the is PicesConter.
            #region Get Routs/Agreements


            int Origin_IsHub = InfoTrack.NaqelAPI.GlobalVar.GV.IsHub(Origin);
            int Origin_IsSub = InfoTrack.NaqelAPI.GlobalVar.GV.IsSub(Origin);
            int Des_IsHub = InfoTrack.NaqelAPI.GlobalVar.GV.IsHub(Destination);
            int des_IsSub = InfoTrack.NaqelAPI.GlobalVar.GV.IsSub(Destination);
            int Origin_IsAT = InfoTrack.NaqelAPI.GlobalVar.GV.IsAT(Origin);
            int des_IsAT = InfoTrack.NaqelAPI.GlobalVar.GV.IsAT(Destination);

            // create a list of ExpGetRateClients to store the routes of involved Expclient. each object identicat a rout 
            List<ViewExpGetRateClient_API> ExpRoutes = new List<ViewExpGetRateClient_API>();
            ExpRoutes = dcMaster.ViewExpGetRateClient_APIs.Where(Ec => Ec.clientID == ClientInfo.ClientID && Ec.LoadTypeID == _LoadTypeID).ToList();// some client have diffrent routes with same client Id and loade type such as 9025734

            #endregion    

            #region Common Variables
            double VATAmount = 0.15;
            int PiecesCounter = 1;

            // check if client has increasing variable or not. if yes increase the Mincharg
            var AgreementID = dcMaster.Agreements.Where(Ag => Ag.ClientID == ClientInfo.ClientID && Ag.LoadTypeID == _LoadTypeID);
            #endregion

            #region Pallet Logic
            if (_LoadTypeID == 7)
            {
                double PalletWeight = (double)ExpRoutes[0].PalletMaxWeight;
                double val = Math.Ceiling(_Weight / PalletWeight);
                PiecesCounter = (int)val;



                foreach (ViewExpGetRateClient_API Route in ExpRoutes)// for each client there are many routes with diffrint prices
                {


                    if ((Route.OriginIsHub == Origin_IsHub) && (Route.DesIsHub == Des_IsHub) && (_Weight <= Route.Toweight))// hub to hub
                    {
                        Result.Price_WithoutVAT = GlobalVar.GV.PalletRateCalculater(Route, PiecesCounter, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT}  The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;

                    }
                    else if ((Route.OriginIsHub == Origin_IsHub) && (Route.DesIsSub == des_IsSub) && (_Weight <= Route.Toweight))// hub to sub
                    {
                        Result.Price_WithoutVAT = GlobalVar.GV.PalletRateCalculater(Route, PiecesCounter, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT} The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;
                    }
                    else if ((Route.OriginIsSub == Origin_IsSub) && (Route.DesIsSub == des_IsSub) && (_Weight <= Route.Toweight))//Sub to sub 
                    {
                        Result.Price_WithoutVAT = GlobalVar.GV.PalletRateCalculater(Route, PiecesCounter, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT} The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;
                    }

                    else if ((Route.OriginIsAT == Origin_IsAT) && (Route.DesIsAT == des_IsAT) && (_Weight <= Route.Toweight))//AT to AT
                    {
                        Result.Price_WithoutVAT = GlobalVar.GV.PalletRateCalculater(Route, PiecesCounter, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT} The total cost with VAT is: {Result.Price_WithVAT}";
                        return Result;
                    }



                }


                Result.Price_WithoutVAT = 0;
                Result.Price_WithVAT = 0;
                Result.Message = "Wrong Route or Weight, please check your agreement";
                return Result;
            }
            #endregion

            #region Domestic Exp -39 Logic 
            if (_LoadTypeID == 39)
            {

                foreach (ViewExpGetRateClient_API Route in ExpRoutes)// for each client there are many routes with diffrint prices
                {


                    if ((Route.OriginIsHub == Origin_IsHub) && (Route.DesIsHub == Des_IsHub) && (_Weight <= Route.Toweight))// hub to hub
                    {


                        Result.Price_WithoutVAT = GlobalVar.GV.ExpDomesticRateCalculater(Route, _Weight, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT}  The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;


                    }
                    else if ((Route.OriginIsHub == Origin_IsHub) && (Route.DesIsSub == des_IsSub) && (_Weight <= Route.Toweight))// hub to sub
                    {


                        Result.Price_WithoutVAT = GlobalVar.GV.ExpDomesticRateCalculater(Route, _Weight, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT}  The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;

                    }
                    else if ((Route.OriginIsSub == Origin_IsSub) && (Route.DesIsSub == des_IsSub) && (_Weight <= Route.Toweight))//Sub to sub 
                    {


                        Result.Price_WithoutVAT = GlobalVar.GV.ExpDomesticRateCalculater(Route, _Weight, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT}  The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;
                    }
                    else if ((Route.OriginIsAT == Origin_IsAT) && (Route.DesIsAT == des_IsAT) && (_Weight <= Route.Toweight))//AT to AT
                    {


                        Result.Price_WithoutVAT = GlobalVar.GV.ExpDomesticRateCalculater(Route, _Weight, Destination);
                        Result.Price_WithVAT = GlobalVar.GV.RateWithTaxCalulater(Result.Price_WithoutVAT, VATAmount);
                        Result.Message = $"The cost without VAT is: {Result.Price_WithoutVAT}  The total cost with VAT is:  {Result.Price_WithVAT}";
                        return Result;
                    }


                }

                Result.Price_WithoutVAT = 0;
                Result.Price_WithVAT = 0;
                Result.Message = "Wrong Route or Weight, please check your agreement";
                return Result;



            }


            Result.Price_WithoutVAT = 0;
            Result.Price_WithVAT = 0;
            Result.Message = "Wrong Route or Weight, please check your agreement";
            return Result;


        }
        #endregion

        public AmazonTrackingResponse AmazonTrackingRequest(AmazonClientInformation Validation, string APIVersion, int TrackingNumber)
        {
            AmazonTrackingResponse Result = new AmazonTrackingResponse();
            Result.APIVersion = APIVersion;
            Result.PackageTrackingInfo.TrackingNumber = TrackingNumber;
            if (Validation.UserID != "AMZN" || Validation.Password != "12345" || APIVersion != "4.0" || TrackingNumber == 0)
            {
                Result.Message = "Please enter the valid information.";
                //Result.HasError = true;
                return Result;
            }


            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            using (var db = new SqlConnection(con))
            {
                var data = db.Query<AmazonTrackingDbModel>("SP_AmazonTracking_updated2", new { trackingNo = TrackingNumber }, commandTimeout: 200, commandType: CommandType.StoredProcedure).ToList();
                Result.PackageTrackingInfo = (from amz in data
                                              select new PackageTrackingInformation
                                              {
                                                  TrackingNumber = TrackingNumber,
                                                  packageDeliveryDate = new PackageDeliveryDate()
                                                  {
                                                      ScheduledDeliveryDate = amz.ScheduledDeliveryDate.ToShortDateString()
                                                  },
                                                  packageDestinationLocation = (from _amz4 in data
                                                                                select new PackageDestinationLocation
                                                                                {
                                                                                    City = _amz4.PackageDestinationLocationCity,
                                                                                    CountryCode = _amz4.PackageDestinationLocationCountry
                                                                                }).FirstOrDefault(),
                                                  trackingEventHistory = (from _amz in data
                                                                          select new TrackingEventHistory
                                                                          {
                                                                              TrackingEventDetail = (from _amz2 in data
                                                                                                     select new TrackingEventDetail
                                                                                                     {
                                                                                                         EventDateTime = _amz2.EventDateTime,
                                                                                                         EventReason = _amz2.EventReason,
                                                                                                         EventStatus = _amz2.EventStatus,
                                                                                                         eventLocation = (from _amz3 in data
                                                                                                                          select new EventLocation
                                                                                                                          {
                                                                                                                              City = _amz3.EventLocationCity,
                                                                                                                              CountryCode = _amz3.EventLocationCountry
                                                                                                                          }).FirstOrDefault(),
                                                                                                         PickupStoreInfo = new PickupStoreInfo()
                                                                                                         {
                                                                                                             StoreLocation = new EventLocation()
                                                                                                             {
                                                                                                                 City = _amz2.PickupStoreLocationCity,
                                                                                                                 CountryCode = _amz2.PickupStoreLocationCountry
                                                                                                             }
                                                                                                         }
                                                                                                     }).ToList()
                                                                          }).FirstOrDefault()
                                                  //packageDeliveryDate = (from amz2 in data
                                                  //                       select new PackageDeliveryDate
                                                  //                       {
                                                  //                           ScheduledDeliveryDate = amz.ScheduledDeliveryDate
                                                  //                       })
                                              }).FirstOrDefault();
            }
            return Result;
        }


        [WebMethod(Description = "You can get WaybillNo, RefNo, Content, Good Description, and Creation Date using this function")]
        public ShipmentDetailResult GetWaybillNoByDate(ClientInformation ClientInfo, string FromDatetime, string ToDatetime)
        {
            ShipmentDetailResult result = new ShipmentDetailResult();

            #region Data validation
            // add check client info ( ID & password ) 

            #region Solve Soap Exeption: System.InvalidOperationException:There is an error in XML document (13, 47). ---> System.FormatException: The string '' is not a valid AllXsd value.

            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }


            if (string.IsNullOrEmpty(FromDatetime) || string.IsNullOrWhiteSpace(FromDatetime) || !DateTime.TryParse(FromDatetime, out DateTime FD))// to avoid one of soap exeption
            {
                result.Message = "Please Pass Valid Date in FromDatetime";
                return result;
            }

            DateTime _FromDatetime = DateTime.Parse(FromDatetime);


            if (string.IsNullOrEmpty(ToDatetime) || string.IsNullOrWhiteSpace(ToDatetime) || !DateTime.TryParse(ToDatetime, out DateTime TD))// to avoid one of soap exeption
            {
                result.Message = "Please Pass Valid Date in ToDatetime";
                return result;
            }

            DateTime _ToDatetime = DateTime.Parse(ToDatetime);



            if (_FromDatetime > DateTime.Now || _ToDatetime > DateTime.Now)
            {
                result.HasError = true;
                result.Message = @"Datetime should be earlier than current time.";
                return result;
            }





            List<ShipmentDetail> shipments;
            string sql = @"SELECT WaybillNo, RefNo, Contents ,GoodDesc , ManifestedTime AS CreationDate 
                FROM dbo.Customerwaybills with(nolock) WHERE ClientID = " + ClientInfo.ClientID + @" 
                AND ManifestedTime > DATEADD(Month, -3, GETDATE())
                AND ManifestedTime > '" + FromDatetime + @"' 
                AND ManifestedTime < '" + ToDatetime + @"';";

            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                shipments = connection.Query<ShipmentDetail>(sql).ToList();

            }



            result.ShipmentDetailList = shipments;
            return result;
            #endregion
            #endregion


        }


        // Blocking create DR / RTO / Complaint as Jay & Sergey requested 20221213
        //activate

        [WebMethod(Description = "Create Delivery Request")]
        public DeliveryRequestResult CreateDeliveryRequest(ClientInformation ClientInfo, int WaybillNo, DateTime RequestDeliveryDate)
        {
            DeliveryRequestResult result = new DeliveryRequestResult() { HasError = true, WaybillNo = WaybillNo, DeliveryRequestID = 0 };

            // Daisy EmployID: 18494
            int EmID = -1; // 18494;
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }

            if (RequestDeliveryDate.Date < DateTime.Now.Date)
            {
                result.Message = "RequestDeliveryDate cannot be early than today!";
                return result;
            }

            // Check if exist in Waybill table
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dc = new DocumentDataDataContext(con);
            var wbs = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.IsCancelled == false).ToList();
            if (!wbs.Any())
            {
                result.Message = "Shipment Did Not Pickup Yet!";
                return result;
            }

            #region Prepare Request Date
            var instance = dc.ViwWaybills.FirstOrDefault(v => v.WayBillNo == WaybillNo && v.IsCancelled == false);
            // if request date = current date, then request date + 1 day
            // if current time after 16:00 and request date is tomorrow then the request date + 1 day
            // if destination country is UAE then skip Saturday and Sunday
            // if destination country neither KSA or UAE then skip Friday
            DateTime OriginRequestDate = RequestDeliveryDate.Date;

            if (OriginRequestDate.Date == DateTime.Now.Date)
                OriginRequestDate = OriginRequestDate.AddDays(1);

            TimeSpan requestTime = RequestDeliveryDate.TimeOfDay;
            if (requestTime > TimeSpan.Parse("16:00") && RequestDeliveryDate.Date == DateTime.Now.Date.AddDays(1))
                OriginRequestDate = OriginRequestDate.AddDays(1);

            if (instance.DCountryID == 3) //UAE
            {
                if (OriginRequestDate.DayOfWeek == DayOfWeek.Saturday)
                    OriginRequestDate = OriginRequestDate.AddDays(2);
                else if (OriginRequestDate.DayOfWeek == DayOfWeek.Sunday)
                    OriginRequestDate = OriginRequestDate.AddDays(1);
            }
            else if (instance.DCountryID != 1)
            {
                if (OriginRequestDate.DayOfWeek == DayOfWeek.Friday)
                    OriginRequestDate = OriginRequestDate.AddDays(1);
            }
            #endregion

            dcMaster = new MastersDataContext(con);
            var dcAudit = new UserAccountDataContext(con);

            // Check if request exist
            if (dc.Requests.Where(r => r.DeliveryDate == OriginRequestDate && r.WaybillID == wbs.First().ID && r.StatusID == 1).Any())
            {
                result.Message = "Delivery Request Already Created!";
                return result;
            }

            // Check Delivery Request Rules view
            if (dc.viwDeliveryRequestRules.Where(d => d.WaybillNo == WaybillNo).Any())
            {
                result.Message = "Shipment OFD more than 3 times or Completed Life Cycle!";
                return result;
            }

            // Check if already RTO delivered - ViwRTORequestWithPickup
            // Same as: RTORequestWaybill.RTOCategoryID = 2 and statusID = 1
            // Same as: Select WaybillNo from RTORequestWaybill where RTOCategoryID = 2 and StatusID = 1
            if (dc.ViwRTORequestWithPickups.Where(r => r.WaybillNo == WaybillNo).Any())
            {
                result.Message = "Waybill Under Process For RTO!";
                return result;
            }

            // Check if allow DR - viwNotAllowedDR
            if (dc.viwNotAllowedDRs.Where(d => d.WaybillID == wbs.First().ID).Any())
            {
                result.Message = "Waybill Completed Life Cycle!";
                return result;
            }

            // Check billing type
            if (instance.BillingTypeID == 3 || instance.BillingTypeID == 4)
            {
                result.Message = "Waybill belongs to External Billing or Free Of Cost!";
                return result;
            }
            #endregion

            // Check if RTO request created in RTORequestWaybill, then set statusID = 3,
            // and in Tracking table, set statusID = 3 for TrackingTypeID = 51
            // and in Audit table, insert operation record of cancel RTO
            var RTOList = dcMaster.RTORequestWaybills
                .Where(r => r.StatusId == 1 && r.WayBillNo == WaybillNo && r.RTOCategoryId == 1)
                .ToList();
            if (RTOList.Any())
            {
                foreach (var rto in RTOList)
                {
                    // Disable in RTORequestWaybill
                    var tempRTORequestWaybill = dcMaster.RTORequestWaybills.Where(r => r.WayBillNo == rto.WayBillNo).First();
                    tempRTORequestWaybill.StatusId = 3;
                    dc.SubmitChanges();

                    #region Disable in Tracking
                    var tempTrackingList = dc.Trackings
                        .Where(t => t.WaybillNo == WaybillNo && t.StatusID == 1 && t.TrackingTypeID == 51)
                        .ToList();
                    foreach (var tr in tempTrackingList)
                    {
                        tr.StatusID = 3;
                        dc.SubmitChanges();
                    }
                    #endregion

                    #region Add in Audit
                    Audit tempAudit = new Audit()
                    {
                        TableID = 829, // TabelID = 829 [RTORequestWaybill]
                        OperationTypeID = 3, // Delete Operation
                        UserID = EmID,
                        KeyID = rto.ID
                    };
                    dcAudit.Audits.InsertOnSubmit(tempAudit);
                    dcAudit.SubmitChanges();
                    #endregion
                }
            }

            #region Create new request
            string Division = "EXP";
            if (instance.ServiceTypeID == 7 || instance.ServiceTypeID == 8)
                Division = "COU";

            var instance1 = dcMaster.RequestAssignTos.FirstOrDefault(r => r.CityID == instance.DestCityID && r.Division.Contains(Division));
            int RequestAssignTo = 2;
            if (instance1 == null)
            {
                if (instance.ServiceTypeID == 7 || instance.ServiceTypeID == 8)
                    RequestAssignTo = 2;
                else
                    RequestAssignTo = 1;
            }
            else
                RequestAssignTo = instance1.ID;

            #region Update exist Request status
            // If Request has created before, then all previous records should be set RequestStatusID = 4
            var tempExistRequestList = dc.Requests.Where(r => r.WaybillID == instance.ID && r.RequestStatusID == 1).ToList();
            foreach (var r in tempExistRequestList)
            {
                r.RequestStatusID = 4;
                dc.SubmitChanges();
            }
            #endregion

            #region Insert new request
            Request tempRequest = new Request()
            {
                Date = DateTime.Now,
                WaybillID = instance.ID,
                CallerName = instance.ConsigneeName,
                CallerMobileNo = instance.ConsigneeMobileNo,
                ConsigneeName = instance.ConsigneeName,
                ConsigneeMobileNo = instance.ConsigneeMobileNo,
                CityID = int.Parse(instance.DestCityID.ToString()),
                DeliveryAddress = instance.DestinationName,
                RequestTimeSlotID = 3,
                DeliveryDate = OriginRequestDate,
                Comments = "Delivery Request",
                RequestTypeID = 1,
                RequestStatusID = 1,
                StatusID = 1,
                EmployID = EmID,
                SourceType = wbs.First().ClientID,
                RequestAssignToID = RequestAssignTo
            };

            dc.Requests.InsertOnSubmit(tempRequest);
            dc.SubmitChanges();
            #endregion

            #region Insert tracking 
            // by calling SpInsertTracking_DeliveryRequest, same as insert to Tracking directly
            int stationID = dcMaster.Stations.FirstOrDefault(s => s.CityID == tempRequest.CityID && s.StatusID == 1).ID;

            BusinessLayer.DContext.Tracking tempTracking = new BusinessLayer.DContext.Tracking()
            {
                WaybillID = tempRequest.WaybillID,
                WaybillNo = instance.WayBillNo,
                TrackingTypeID = 62,
                StationID = stationID,
                Date = tempRequest.Date,
                EmployID = EmID,
                IsSent = false,
                StatusID = 1,
                HasError = false,
                ErrorMessage = "",
                Comments = tempRequest.DeliveryDate.ToString("yyyy-MM-dd"),
                DBTableID = 851,
                KeyID = tempRequest.ID
            };

            dc.Trackings.InsertOnSubmit(tempTracking);
            dc.SubmitChanges();
            #endregion

            #endregion

            result.HasError = false;
            result.Message = "Delivery request created successfully.";
            result.DeliveryRequestID = tempRequest.ID;
            return result;
        }

        [WebMethod(Description = "Cancel Delivery Request")]
        public CancelDeliveryRequestResult CancelDeliveryRequest(ClientInformation ClientInfo, int WaybillNo)
        {
            CancelDeliveryRequestResult result = new CancelDeliveryRequestResult() { HasError = true, WaybillNo = WaybillNo };
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }
            #endregion

            // Daisy EmployID: 18494
            int EmID = -1; // 18494;
            string conStr = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            using (SqlConnection con = new SqlConnection(conStr))
            {
                using (SqlCommand cmd = new SqlCommand("spDeleteDeliveryRequest", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@WaybillNo", SqlDbType.Int).Value = WaybillNo;
                    cmd.Parameters.Add("@EmployID", SqlDbType.Int).Value = EmID;
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            result.HasError = false;
            result.Message = "Delivery request cancelled successfully.";

            return result;
        }

        // TODO: Delivery Request Result - check if shipment delivered in time

        // TODO: ASR Delivery Request

        // TODO: Update consignee address / phoneNo / mobileNo - should save the previous value to some log table and then update


        [WebMethod(Description = "Create RTO Request")]
        public CreateRTORequestResult CreateRTORequest(ClientInformation ClientInfo, int WaybillNo)
        {
            CreateRTORequestResult result = new CreateRTORequestResult() { HasError = true, WaybillNo = WaybillNo };
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }
            #endregion

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dcMaster = new MastersDataContext(con);
            dc = new DocumentDataDataContext(con);
            var dcPayment = new InvoicesDataContext(con);

            #region Check Waybill table data
            //Check if exist in Waybill table
            var wbs = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.IsCancelled == false).ToList();
            if (!wbs.Any())
            {
                result.Message = "Shipment Did Not Pickup Yet!";
                return result;
            }

            if (wbs.First().Weight <= 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, Please check the weight for this waybill";
                return result;
            }

            if (wbs.First().BillingTypeID != 1 && wbs.First().BillingTypeID != 5)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because the billing type for this shipment is not COD or On Account";
                return result;
            }

            if (wbs.First().Invoiced)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because this shipment already invoiced.";
                return result;
            }
            #endregion

            #region Check Delivery and Invoice related
            if (dc.Deliveries.Where(p => p.WaybillID == wbs.First().ID && p.StatusID != 3 && p.DeliveryStatusID == 5).Count() > 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because this shipment already delivered.";
                return result;
            }

            if (dc.Requests.Where(p => p.StatusID == 1 && p.WaybillID == wbs.First().ID && p.RequestStatusID == 1).Count() > 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because this shipment already under delivery request.";
                return result;
            }

            if (dcPayment.Payments.Where(p => p.StatusID == 1 && p.WaybillID == wbs.First().ID).Count() > 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because this shipment already collected the amount.";
                return result;
            }

            if (dcPayment.PaymentForDeliverySheetDetails.Where(p => p.StatusID == 1 && p.WaybillID == wbs.First().ID).Count() > 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because this shipment already collected the amount.";
                return result;
            }

            if (dcPayment.PaymentGatewayDetails.Where(
                p => p.StatusID == 1
                && p.WaybillID == wbs.First().ID
                && p.IsPayment == true
                && p.PaymentStatus == "paid")
                .Count() > 0)
            {
                result.Message = "Sorry, Can't create RTO Waybill, because payment already paid through payment gateway.";
                return result;
            }
            #endregion

            var tempRTORequestWaybillsList = dcMaster.RTORequestWaybills.Where(c => c.WayBillNo == WaybillNo && c.StatusId == 1).ToList();
            if (tempRTORequestWaybillsList.Count() > 0)
            {
                result.Message = "This Waybill was already requested RTO!";
                return result;
            }

            #region Check Status
            var statusResult = 0;
            using (SqlConnection conn = new SqlConnection(con))
            {
                conn.Open();
                SqlCommand command = new SqlCommand("spGetFinalStatusBywaybillNo", conn);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@WaybillNo", SqlDbType.Int).Value = WaybillNo;
                using (SqlDataReader reader = command.ExecuteReader())
                    if (reader.Read())
                    {
                        statusResult = Convert.ToInt32(reader["Result"]);
                    }
                conn.Close();
            }

            if (statusResult == 1)
            {
                result.Message = "RTO cannot be requested at this stage!";
                return result;
            }
            #endregion

            //Daisy EmployID: 18494
            int EmID = -1; // 18494;
            var instance = wbs.FirstOrDefault();
            using (SqlConnection conn = new SqlConnection(con))
            {
                conn.Open();
                SqlCommand cmdInsertRTORequest = new SqlCommand("spInsertRTORequest", conn);
                cmdInsertRTORequest.Parameters.Add("@WayBillNo", SqlDbType.Int).Value = Convert.ToInt32(instance.WayBillNo);
                cmdInsertRTORequest.Parameters.Add("@WaybillID", SqlDbType.Int).Value = Convert.ToInt32(instance.ID);
                cmdInsertRTORequest.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
                cmdInsertRTORequest.Parameters.Add("@ClientID", SqlDbType.Int).Value = Convert.ToInt32(instance.ClientID);
                cmdInsertRTORequest.Parameters.Add("@BillingTypeID", SqlDbType.Int).Value = Convert.ToInt32(instance.BillingTypeID);
                cmdInsertRTORequest.Parameters.Add("@ConsigneeID", SqlDbType.Int).Value = Convert.ToInt32(instance.ConsigneeID);
                cmdInsertRTORequest.Parameters.Add("@OriginStationID", SqlDbType.Int).Value = Convert.ToInt32(instance.OriginStationID);
                cmdInsertRTORequest.Parameters.Add("@DestinationStationID", SqlDbType.Int).Value = Convert.ToInt32(instance.DestinationStationID);
                cmdInsertRTORequest.Parameters.Add("@PickUpDate", SqlDbType.DateTime).Value = instance.PickUpDate;
                cmdInsertRTORequest.Parameters.Add("@PicesCount", SqlDbType.Int).Value = Convert.ToInt32(instance.PicesCount);
                cmdInsertRTORequest.Parameters.Add("@Weight", SqlDbType.Int).Value = Convert.ToInt32(instance.Weight);
                cmdInsertRTORequest.Parameters.Add("@Comment", SqlDbType.VarChar).Value = string.Empty;
                cmdInsertRTORequest.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = EmID;
                cmdInsertRTORequest.Parameters.Add("@CreatedOn", SqlDbType.DateTime).Value = DateTime.Now;
                cmdInsertRTORequest.Parameters.Add("@LastUpdatedOn", SqlDbType.DateTime).Value = DateTime.Now;
                cmdInsertRTORequest.Parameters.Add("@LastUpdatedBy", SqlDbType.Int).Value = EmID;
                cmdInsertRTORequest.Parameters.Add("@RTORequestedDate", SqlDbType.DateTime).Value = DateTime.Now;
                cmdInsertRTORequest.Parameters.Add("@RTOCategoryId", SqlDbType.Int).Value = 1;
                cmdInsertRTORequest.Parameters.Add("@RTOWaybillNo", SqlDbType.Int).Value = 0;
                cmdInsertRTORequest.Parameters.Add("@StatusId", SqlDbType.Int).Value = 1;
                cmdInsertRTORequest.Parameters.Add("@IsPrinted", SqlDbType.Bit).Value = false;

                cmdInsertRTORequest.CommandType = CommandType.StoredProcedure;
                cmdInsertRTORequest.ExecuteNonQuery();
            }

            result.HasError = false;
            result.Message = "RTO request created successfully.";
            return result;
        }

        [WebMethod(Description = "Cancel RTO Request")]
        public CancelRTOResult CancelRTORequest(ClientInformation ClientInfo, int WaybillNo)
        {
            WritetoXMLUpdateWaybill(
                new CancelRTOPayloadRequest() { ClientInfo = ClientInfo, WaybillNo = WaybillNo },
                ClientInfo,
                WaybillNo.ToString(),
                EnumList.MethodType.CancelRTO);

            CancelRTOResult result = new CancelRTOResult() { HasError = true, WaybillNo = WaybillNo };
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }
            #endregion

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dcMaster = new MastersDataContext(con);
            dc = new DocumentDataDataContext(con);

            //Check if exist in Waybill table
            var wbs = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.IsCancelled == false).ToList();
            if (!wbs.Any())
            {
                result.Message = "Shipment Did Not Pickup Yet!";
                return result;
            }

            var tempRTORequestWaybillsList = dcMaster.RTORequestWaybills.Where(c => c.WayBillNo == WaybillNo && c.StatusId == 1).ToList();
            if (tempRTORequestWaybillsList.Count() == 0)
            {
                result.Message = "No Available RTO Requests for this WaybillNo!";
                return result;
            }

            if (tempRTORequestWaybillsList[0].RTOWaybillNo.HasValue && tempRTORequestWaybillsList[0].RTOWaybillNo != 0)
            {
                result.Message = "Can not cancel at this stage, please contact Naqel team for further operation!";
                return result;
            }

            if (dc.Trackings.Where(x => x.WaybillNo == WaybillNo && x.StatusID == 1 && x.TrackingTypeID == 9 && x.NewWaybillNo.HasValue).Any())
            {
                result.Message = "Can not cancel at this stage, please contact Naqel CS team for further operation!";
                return result;
            }

            foreach (var tr in tempRTORequestWaybillsList)
            {
                // Disable exist RTO records
                tr.StatusId = 3;
                dcMaster.SubmitChanges();
            }

            //Disable tracking
            using (SqlConnection conn = new SqlConnection(con))
            {
                using (SqlCommand cmd = new SqlCommand("spUpdateTracking", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@WaybillNo", SqlDbType.Int).Value = WaybillNo;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            result.HasError = false;
            result.Message = "RTO request cancelled successfully.";

            return result;
        }

        [WebMethod(Description = "Create complaint")]
        public CompalintRequestResult CreateComplaint(ClientInformation ClientInfo, int WaybillNo, int ComplaintSubType, string ComplaintDetail)
        {
            CompalintRequestResult result = new CompalintRequestResult() { HasError = true, WaybillNo = WaybillNo };
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }

            if (ComplaintDetail.Trim().Length == 0 || ComplaintDetail.Trim().Length > 500)
            {
                result.Message = "Complaint content cannot be empty or more than 500 characters!";
                return result;
            }

            string list1 = System.Configuration.ConfigurationManager.AppSettings["AcceptedComplaintSubTypeIDs"].ToString();
            List<int> AcceptedComplaintSubTypes = list1.Split(',').Select(Int32.Parse).ToList();
            //List<int> AcceptedComplaintSubTypes = new List<int>() { 1, 2, 3, 8, 13 }; 
            //Late delivery “ attempted “ 	1
            //Late delivery “ No Attempt”	2
            //Asking extra money  3
            //Bad behavior    8
            //Language gab    13

            if (!AcceptedComplaintSubTypes.Contains(ComplaintSubType))
            {
                result.Message = "Invalid ComplaintSubTypeID!";
                return result;
            }
            #endregion

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dc = new DocumentDataDataContext(con);
            // Check if exist in Waybill table
            var wbs = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.IsCancelled == false).ToList();
            if (!wbs.Any())
            {
                result.Message = "Shipment Did Not Pickup Yet!";
                return result;
            }

            if (CheckComplaintExist(WaybillNo, ComplaintSubType, ComplaintDetail))
            {
                result.Message = "Complaint Already Exist!";
                return result;
            }

            // Daisy EmployID: 18494
            int EmID = -1; // 18494;
            string sql = @"Insert into Complaint 
                    (Date, ProductTypeID, StatusID, ComplaintSeverityID, ComplaintSourceID, RegisteredBy, ComplaintStatusID, ComplaintRefTypeID, 
                    ComplaintDetails, ComplaintTypeID, ComplaintSubTypeID, RefNo, WaybillNo, StationID, ClientID, AssignedTo, ComplaintPhoneNo)
                    values (GETDATE(), 6, 1, 1, 5, @EmID, 1, 1, 
                    @ComplaintDetail, (select ComplainTyperId from SubComplaintCateogry where StatusID = 1 and ID = @ComplaintSubType), 
                    @ComplaintSubType, @WaybillNo, @WaybillNo,
                    (select DestinationStationID from Waybill where WaybillNo = @WaybillNo), 
                    (select ClientID from Waybill where WaybillNo = @WaybillNo), 
                    (select isnull((select top 1 EmployID from ComplaintAutoAssign 
                    where StationID = (select DestinationStationID from Waybill where WaybillNo = @WaybillNo) 
                    and ProductTypeID = 6), -1)), '0')";

            var db = new SqlConnection(con);
            try
            {
                var affectedRows = db.Execute(sql,
                  new
                  {
                      EmID,
                      ComplaintDetail = ComplaintDetail.Replace("'", "''"),
                      ComplaintSubType,
                      WaybillNo
                  });

                if (affectedRows > 0)
                {
                    result.HasError = false;
                    result.Message = "Complaint uploaded successfully.";
                }
                return result;
            }
            catch (Exception ex)
            {
                LogException(ex);
                result.Message = "Compliant failed to create, try again later.";
                return result;
            }
        }

        private static bool CheckComplaintExist(int waybillNo, int complaintSubTypeID, string complaintDetail)
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            int CntC = 0;

            string sql = @"select count(1) from Complaint where WaybillNo = " + waybillNo
                     + " and ComplaintSubTypeID = " + complaintSubTypeID
                     + " and ComplaintSourceID = 5 and ComplaintDetails = '" + complaintDetail.Replace("'", "''") + @"'";

            using (var db = new SqlConnection(con))
            {
                CntC = db.Query<int>(sql).ToList()[0];
            }
            return CntC > 0;
        }

        [WebMethod(Description = "Get complaint status")]
        public CompalintResult GetComplaintStatus(ClientInformation ClientInfo, int WaybillNo)
        {
            CompalintResult result = new CompalintResult() { HasError = true, WaybillNo = WaybillNo };
            #region Data validation
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.Message = "Invalid credential!";
                return result;
            }

            if (!IsWBExist(WaybillNo.ToString(), ClientInfo.ClientID))
            {
                result.Message = "Invalid WaybillNo!";
                return result;
            }
            #endregion

            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            dc = new DocumentDataDataContext(con);

            // Check if exist in Waybill table
            var wbs = dc.Waybills.Where(w => w.WayBillNo == WaybillNo && w.IsCancelled == false).ToList();
            if (!wbs.Any())
            {
                result.Message = "Shipment Did Not Pickup Yet!";
                return result;
            }

            if (dc.Complaints.Where(c => c.WaybillNo == WaybillNo.ToString() && c.StatusID == 1).Count() == 0)
            {
                result.Message = "No Available Complaints for this WaybillNo!";
                return result;
            }

            var tempComplaint = dc.Complaints.Where(c => c.WaybillNo == WaybillNo.ToString() && c.StatusID == 1).OrderByDescending(c => c.ID).First();

            result.HasError = false;
            result.Message = "";
            result.ActionTaken = tempComplaint.CorrectiveActionTaken ?? "";
            result.PreventiveAction = tempComplaint.PreventiveActionTaken ?? "";

            return result;
        }

        #region ParcelLocker Done by Sara Almalki
        private enum MachineStatus
        {
            Approved = 1,
            PendingApproval = 2,
            Disapproved = 3,
            Active = 4,
            Disable = 5,
            UnRegistered = 6,
            Warning = 7,
            Offline = 8,
            Error = 9,
            OutofService = 10
        }

        [WebMethod(Description = "You can get Parcel Lockers Location using this function")]
        public PLResult GetParcelLockerLocations(ClientInformation ClientInfo)
        {
            PLResult Response = new PLResult
            {
                ParcelInfos = new List<ParcelInfo>() // Ensure ParcelInfos is initialized
            };

            try
            {
                // Check client information
                if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
                {
                    Response.Message = "Invalid credential!";
                    return Response;
                }

                // Fetch data from the database
                string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
                using (var db = new SqlConnection(con))
                {
                    string _sql = @"SELECT ParcelLockerID, 
                        ParcelLockerName, ParcelLockerNameAr, ParcelLockerAddress, Street,ShortAddress,zipcode,buildingnumber, 
                        Location, CityName, CityNameAr, CityCode,RegionName, Country,
                        Longitude, Latitude, 
                        ISNULL(MonOpeningHour, '') AS MonOpeningHour,
                        ISNULL(MonClosingHour, '') AS MonClosingHour,
                        ISNULL(TuesOpeningHour, '') AS TuesOpeningHour,
                        ISNULL(TuesClosingHour, '') AS TuesClosingHour,
                        ISNULL(WedOpeningHour, '') AS WedOpeningHour,
                        ISNULL(WedClosingHour, '') AS WedClosingHour,
                        ISNULL(ThurOpeningHour, '') AS ThurOpeningHour,
                        ISNULL(ThurClosingHour, '') AS ThurClosingHour,
                        ISNULL(FriOpeningHour, '') AS FriOpeningHour,
                        ISNULL(FriClosingHour, '') AS FriClosingHour,
                        ISNULL(SatOpeningHour, '') AS SatOpeningHour,
                        ISNULL(SatClosingHour, '') AS SatClosingHour,
                        ISNULL(SunOpeningHour, '') AS SunOpeningHour,
                        ISNULL(SunClosingHour, '') AS SunClosingHour

                        FROM ViwAPIParcelLockers";

                    List<ViwAPIParcelLockers> list = db.Query<ViwAPIParcelLockers>(_sql).ToList();

                    // Populate the ParcelInfos list
                    foreach (var item in list)
                    {
                        var parcelInfo = new ParcelInfo
                        {
                            ParcelLockerID = item.ParcelLockerID,
                            ParcelLockerName = item.ParcelLockerName ?? "",
                            ParcelLockerNameAr = item.ParcelLockerNameAr ?? "",
                            ParcelLockerAddress = item.ParcelLockerAddress ?? "",
                            Street = item.Street ?? "",
                            Location = item.Location ?? "",
                            Longitude = item.Longitude,
                            Latitude = item.Latitude,
                            CityCode = item.CityCode ?? "",
                            CityName = item.CityName ?? "",
                            CityNameAr = item.CityNameAr ?? "",
                            RegionName = item.RegionName ?? "",
                            ShortAddress = item.ShortAddress ?? "",
                            Buildingnumber = item.buildingnumber ?? "",
                            ZIPcode = item.zipcode ?? "",
                            Country = item.Country ?? "",
                            MonOpeningHour = item.MonOpeningHour,
                            MonClosingHour = item.MonClosingHour,
                            TuesOpeningHour = item.TuesOpeningHour,
                            TuesClosingHour = item.TuesClosingHour,
                            WedOpeningHour = item.WedOpeningHour,
                            WedClosingHour = item.WedClosingHour,
                            ThurOpeningHour = item.ThurOpeningHour,
                            ThurClosingHour = item.ThurClosingHour,
                            FriOpeningHour = item.FriOpeningHour,
                            FriClosingHour = item.FriClosingHour,
                            SatOpeningHour = item.SatOpeningHour,
                            SatClosingHour = item.SatClosingHour,
                            SunOpeningHour = item.SunOpeningHour,
                            SunClosingHour = item.SunClosingHour
                        };

                        Response.ParcelInfos.Add(parcelInfo);
                    }
                }

                return Response;
            }
            catch (Exception e)
            {
                // Log or handle the exception as needed
                Response.Message = $"An error occurred: {e.Message}";
                return Response;
            }
        }
        #endregion ParcelLocker

        #region Bullet Delivery Service Done by Sara Almalki
        [WebMethod(Description = "Bullet Delivery")]
        public BulletDeliveryResult BulletDLV(BulletDlv_Req_Details _ManifestShipmentDetailsBD)
        {
            AddAPIError(_ManifestShipmentDetailsBD.ClientInfo.ClientID, "BulletDLV");

            BulletDeliveryResult Result = new BulletDeliveryResult();

            #region Validation

            // Check the client ID and passwor
            if (_ManifestShipmentDetailsBD.ClientInfo.CheckClientInfo(_ManifestShipmentDetailsBD.ClientInfo, false).HasError)
            {
                Result.Message = "Invalid Credintial";
                return Result;
            }

            #region CityCode + CountryCode Validation
            var OriginCityCode = _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CityCode;
            var OriginCountryCode = _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CountryCode;
            var DestinationCityCode = _ManifestShipmentDetailsBD.ConsigneeInfo.CityCode;
            var DestinationCountryCode = _ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode;

            if (string.IsNullOrWhiteSpace(OriginCityCode) || string.IsNullOrWhiteSpace(OriginCountryCode))
            {
                Result.Message = "Please provide a valid Origin CityCode or CountryCode";
                return Result;
            }

            if (GlobalVar.GV.ISCityCodeValid(OriginCityCode, OriginCountryCode, true) == "")
            {
                Result.Message = "Wrong in Origin CityCode or CountryCode";
                return Result;
            }

            if (string.IsNullOrWhiteSpace(DestinationCityCode) || string.IsNullOrWhiteSpace(DestinationCountryCode))
            {
                Result.Message = "Please provide a valid Destination CityCode or CountryCode";
                return Result;
            }

            if (GlobalVar.GV.ISCityCodeValid(DestinationCityCode, DestinationCountryCode, true) == "")
            {
                Result.Message = "Please provide a valid Destination CityCode or CountryCode";
                return Result;
            }
            // Inside KSA validation
            if (_ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode != "KSA" && _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CountryCode != "KSA")
            {
                Result.HasError = true;
                Result.Message = "Wrong in Origin and Distenation country codes. Accept KSA only.";
                return Result;
            }
            //Restrict the city codes to KSA cities only
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<string> KSACities = dcMaster.Cities.Where(C => C.CountryID == 1 && C.StatusID == 1).Select(C => C.Code.ToLower()).ToList();
            if (!KSACities.Contains(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CityCode.ToLower()))
            {
                Result.HasError = true;
                Result.Message = "wrong in Origin City Code. Out of KSA cities";
                return Result;
            }
            if (!KSACities.Contains(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode.ToLower()))
            {
                Result.HasError = true;
                Result.Message = "wrong in Destination  City Code. Out of KSA cities";
                return Result;
            }
            //same city code validation
            if (_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode != _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CityCode)
            {
                Result.HasError = true;
                Result.Message = "Diffirent origin and Distenation city codes.";
                return Result;
            }
            #endregion CityCode + CountryCode Validation
            // loadtype validation
            List<int> BulletLoadTypes = new List<int> { 188, 187, 189 };
            if (!BulletLoadTypes.Contains(_ManifestShipmentDetailsBD.LoadTypeID))
            {
                Result.HasError = true;
                Result.Message = "Invalid Load Type ID. Accept 188, 187, 189";
                return Result;
            }

            //Mandatory latitude & longitude of client validation 
            if (string.IsNullOrEmpty(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Latitude) || string.IsNullOrEmpty(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Longitude))
            {
                Result.HasError = true;
                Result.Message = "Latitude and longitude of Client are mandatory";
                return Result;
            }

            //Mandatory latitude & longitude of consignee valodation 
            if (string.IsNullOrEmpty(_ManifestShipmentDetailsBD.Latitude) || string.IsNullOrEmpty(_ManifestShipmentDetailsBD.Longitude))
            {
                Result.HasError = true;
                Result.Message = "Latitude and longitude of Consignee are mandatory";
                return Result;
            }

            //Mandatory National Address valodation 
            if (string.IsNullOrEmpty(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.NationalAddress) || string.IsNullOrWhiteSpace(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.NationalAddress))
            {
                Result.HasError = true;
                Result.Message = "NationalAddress of client is mandatory";
                return Result;
            }


            //Mandatory National Address valodation 
            if (string.IsNullOrEmpty(_ManifestShipmentDetailsBD.ConsigneeInfo.NationalAddress) || string.IsNullOrWhiteSpace(_ManifestShipmentDetailsBD.ConsigneeInfo.NationalAddress))
            {
                Result.HasError = true;
                Result.Message = "NationalAddress of consignee is mandatory";
                return Result;
            }


            //Valid Cordinates format or not - client
            if (!InfoTrack.NaqelAPI.GlobalVar.GV.IsFalidCoordinatesRegEex(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Latitude, _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Longitude))
            {
                Result.HasError = true;
                Result.Message = " Wrong in format of Latitude or longitude of Client";
                return Result;
            }

            //Valid Cordinates format or not - Consognee
            if (!InfoTrack.NaqelAPI.GlobalVar.GV.IsFalidCoordinatesRegEex(_ManifestShipmentDetailsBD.Latitude, _ManifestShipmentDetailsBD.Longitude))
            {
                Result.HasError = true;
                Result.Message = " Wrong in format of Latitude or longitude of consignee";
                return Result;
            }

            //PiceCount Validation
            if (_ManifestShipmentDetailsBD.PicesCount <= 0)
            {
                Result.Message = "Check the PicesCount value";
                return Result;
            }

            // Weight Validation
            if (_ManifestShipmentDetailsBD.Weight <= 0)
            {
                Result.Message = "Check the Weight value";
                return Result;
            }

            //Volumetirc weight validation


            //Pick up time validation.
            DateTime PickUDateTime = DateTime.Now;
            if ((PickUDateTime.Hour >= 23 && _ManifestShipmentDetailsBD.LoadTypeID == 187)
                || (PickUDateTime.Hour >= 22 && _ManifestShipmentDetailsBD.LoadTypeID == 188)
                || (PickUDateTime.Hour >= 21 && _ManifestShipmentDetailsBD.LoadTypeID == 189))
            {
                Result.HasError = true;
                Result.Message = " Out of Naqel working hour";
                return Result;
            }

            #region BillingType & COD Validation
            int[] acceptBillingTypeArray = { 1, 5 }; // PrePaid, ExternalBilling, COD
            if (!acceptBillingTypeArray.Contains(_ManifestShipmentDetailsBD.BillingType))
            {
                Result.Message = "Wrong Billing Type";
                return Result;
            }

            if (_ManifestShipmentDetailsBD.CODCharge < 0)
            {
                Result.Message = "Wrong CODCharge";
                return Result;
            }

            if (_ManifestShipmentDetailsBD.BillingType == 1 && _ManifestShipmentDetailsBD.CODCharge > 0)
            {
                Result.Message = "Wrong in BillingType or CODCharge";
                return Result;
            }

            if (_ManifestShipmentDetailsBD.BillingType == 5 && _ManifestShipmentDetailsBD.CODCharge <= 0)
            {
                Result.Message = "Wrong in BillingType or CODCharge";
                return Result;
            }

            if (!GlobalVar.GV.IsBillingCorrect(_ManifestShipmentDetailsBD.BillingType, _ManifestShipmentDetailsBD.ClientInfo.ClientID))
            {
                Result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return Result;
            }
            #endregion BillingType & COD Validation

            #endregion Validation

            #region Google Distance Mattrix API Utlization (Request& Response)

            string OriginCoordinates = _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Latitude + "%2C" + _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.Longitude;
            string DestinationCoordinates = _ManifestShipmentDetailsBD.Latitude + "%2C" + _ManifestShipmentDetailsBD.Longitude;
            string APIkey = "AIzaSyALzDHbmpFmqHW_b8szroc2EzEtH05k1yc";
            string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?departure_time=now&origins={OriginCoordinates}&destinations={DestinationCoordinates}&key={APIkey}";
            //https://maps.googleapis.com/maps/api/distancematrix/json?departure_time=now&destinations=24.673663%2C46.780133&origins=24.695061%2C46.792883&units=imperial&key=AIzaSyALzDHbmpFmqHW_b8szroc2EzEtH05k1yc"

            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";

            double totalduration;
            // Get the response
            using (WebResponse response = request.GetResponse())
            {
                // Get the response stream
                using (Stream dataStream = response.GetResponseStream())
                {
                    // Read the response using a StreamReader
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        // Read the content as a string
                        string result = reader.ReadToEnd();

                        BulletDeliveryResponse ResponseData = JsonConvert.DeserializeObject<BulletDeliveryResponse>(result);
                        Element DistanceMatrixElements = new Element();
                        DistanceMatrixElements.duration = ResponseData.rows[0].elements[0].duration;
                        totalduration = DistanceMatrixElements.duration.value;//ثواني
                        totalduration = Math.Round(totalduration / 60); // الوحدة المستعملة هي الدقائق

                    }
                }
            }


            #endregion Google Distance Mattrix API Utlization (Request& Response)

            #region Bullet Waybill Creation part

            // if can be deleverd in one houre then create waybill with load type 187 -- if not send a reject error

            int loadtype = _ManifestShipmentDetailsBD.LoadTypeID;
            if (InfoTrack.NaqelAPI.GlobalVar.GV.CanBeDeliveredIn1Houre(totalduration) && loadtype == 187)
            {
                Result Inner_result = new Result();
                Inner_result = CreatBulletDeliveryeWaybill(_ManifestShipmentDetailsBD);

                Result.Message = Inner_result.Message;
                Result.HasError = Inner_result.HasError;
                Result.WaybillNo = Inner_result.WaybillNo;
                Result.BookingRefNo = Inner_result.BookingRefNo;
                Result.Key = Inner_result.Key;
                return Result;

            }
            else if (InfoTrack.NaqelAPI.GlobalVar.GV.CanBeDeliveredIn2Houre(totalduration) && loadtype == 188)
            {
                Result Inner_result = new Result();
                Inner_result = CreatBulletDeliveryeWaybill(_ManifestShipmentDetailsBD);

                Result.Message = Inner_result.Message;
                Result.HasError = Inner_result.HasError;
                Result.WaybillNo = Inner_result.WaybillNo;
                Result.BookingRefNo = Inner_result.BookingRefNo;
                Result.Key = Inner_result.Key;
                return Result;

            }
            else if (InfoTrack.NaqelAPI.GlobalVar.GV.CanBeDeliveredInSameDay(totalduration, PickUDateTime) && loadtype == 189)
            {
                Result Inner_result = new Result();
                Inner_result = CreatBulletDeliveryeWaybill(_ManifestShipmentDetailsBD);

                Result.Message = Inner_result.Message;
                Result.HasError = Inner_result.HasError;
                Result.WaybillNo = Inner_result.WaybillNo;
                Result.BookingRefNo = Inner_result.BookingRefNo;
                Result.Key = Inner_result.Key;
                return Result;
            }
            else
            {
                Result.BookingRefNo = "";
                Result.Message = "Reject to book a bullet delivery request due to driving duration condition";// the driving duration exceeds the conditions
                                                                                                              // (Loadetype187 40 minutes, Loadetype188 80 minutes,
                                                                                                              // loadeType189 the diffirence between client request and time 00:00 is less than 3 hour)
                Result.WaybillNo = 0;
                Result.HasError = true;

            }

            #endregion Bullet Waybill Creation part

            return Result;

        }
        #endregion Bullet Delivery Service Done by Sara Almalki

        #region CreatBulletDeliveryeWaybill Done By Sara Almalki
        public Result CreatBulletDeliveryeWaybill(BulletDlv_Req_Details _ManifestShipmentDetailsBD)
        {
            Result result = new Result();


            #region Data Validation

            var tempClientID = _ManifestShipmentDetailsBD.ClientInfo.ClientID;

            int tempServiceTypeID = GlobalVar.GV.GetServiceTypeID(_ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.LoadTypeID);
            _ManifestShipmentDetailsBD.ServiceTypeID = tempServiceTypeID;
            bool IsCourierLoadType = false;
            string courierLoadTypes = System.Configuration.ConfigurationManager.AppSettings["CourierLoadTypes"].ToString();
            List<int> _courierLoadTypes = courierLoadTypes.Split(',').Select(Int32.Parse).ToList();
            IsCourierLoadType = _courierLoadTypes.Contains(_ManifestShipmentDetailsBD.LoadTypeID);


            string CountrywithCOD = System.Configuration.ConfigurationManager.AppSettings["CountrywithCOD"].ToString();
            List<int> _CountrywithCOD = CountrywithCOD.Split(',').Select(Int32.Parse).ToList();
            bool is_CountrywithCOD = false;
            is_CountrywithCOD = _CountrywithCOD.Contains(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode));

            dcMaster = new MastersDataContext();

            bool ClientHasCreateBookingPermit = dcMaster.APIClientAccesses
                    .Where(P => P.ClientID == _ManifestShipmentDetailsBD.ClientInfo.ClientID
                    && P.StatusID == 1 && P.IsCreateBooking == true)
                    .Any();

            if (_ManifestShipmentDetailsBD.CreateBooking == true && !ClientHasCreateBookingPermit)
            {
                result.HasError = true;
                result.Message = "Your account has no permission to create booking, please contact Naqel team for further operation.";
                return result;
            }

            // DocumentDataDataContext dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            CheckClientLoadType(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.LoadTypeID, ref result);
            if (result.HasError)
            {
                result.HasError = true;
                result.Message = "load type issue";
                return result;
            }

            _ManifestShipmentDetailsBD.ClientInfo.CheckClientAddressAndContact(_ManifestShipmentDetailsBD.ClientInfo, true);
            if (_ManifestShipmentDetailsBD.ClientInfo.ClientAddressID == 0 || _ManifestShipmentDetailsBD.ClientInfo.ClientContactID == 0)
            {
                result.HasError = true;
                result.Message = "Error happend while saving Client Info, please insert valid data.. ";
                return result;
            }
            _ManifestShipmentDetailsBD.ConsigneeInfo.CheckConsigneeData(_ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.ConsigneeInfo, IsCourierLoadType);
            if (_ManifestShipmentDetailsBD.ConsigneeInfo.ConsigneeDetailID == 0 || _ManifestShipmentDetailsBD.ConsigneeInfo.ConsigneeID == 0)
            {
                result.HasError = true;
                result.Message = "Error happend while saving Consignee Info, please insert valid data.. ";
                return result;
            }
            #endregion

            var doc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());


            if (doc.APIClientAndSubClients.Where(p => p.pClientID == _ManifestShipmentDetailsBD.ClientInfo.ClientID).Count() > 0)
            {
                var subClient = doc.APIClientAndSubClients.FirstOrDefault(p => p.pClientID == _ManifestShipmentDetailsBD.ClientInfo.ClientID
                && p.DestCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode))
                && p.OrgCountryId == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CountryCode))
                && p.StatusId == 1);
                if (subClient != null)
                {
                    _ManifestShipmentDetailsBD.ClientInfo.ClientID = subClient.cClientID;
                    _ManifestShipmentDetailsBD.LoadTypeID = Convert.ToInt32(subClient.LoadTypeID);
                }
            }

            string list0 = System.Configuration.ConfigurationManager.AppSettings["ClientIDWithCODOnly"].ToString();
            List<int> _clientid0 = list0.Split(',').Select(Int32.Parse).ToList();
            if (_clientid0.Contains(_ManifestShipmentDetailsBD.ClientInfo.ClientID) && _ManifestShipmentDetailsBD.BillingType == 5)
            {
                result.HasError = true;
                result.Message = "Your account not support COD type.";
                return result;
            }

            string list = System.Configuration.ConfigurationManager.AppSettings["NoCheckRefNoClientIDs"].ToString();
            List<int> _clientid = list.Split(',').Select(Int32.Parse).ToList();
            if (string.IsNullOrWhiteSpace(_ManifestShipmentDetailsBD.RefNo)) { }

            else if (!_clientid.Contains(_ManifestShipmentDetailsBD.ClientInfo.ClientID))
            {

                List<ForwardWaybillInfo> waybillInfos = CheckExistingForwardWaybill(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, _ManifestShipmentDetailsBD.LoadTypeID);
                if (waybillInfos.Count() > 0)
                {
                    ForwardWaybillInfo waybillInfo = waybillInfos[0];
                    result.WaybillNo = waybillInfo.WaybillNo;
                    result.Key = waybillInfo.ID;
                    result.HasError = false;
                    result.Message = "Waybill already generated with RefNo: " + _ManifestShipmentDetailsBD.RefNo;
                    return result;
                }

            }
            DocumentDataDataContext dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            //SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Date Validation Done.");
            _ManifestShipmentDetailsBD.DeliveryInstruction = GlobalVar.GV.GetString(_ManifestShipmentDetailsBD.DeliveryInstruction, 200);
            CustomerWayBill NewWaybill = new CustomerWayBill();
            NewWaybill.ClientID = _ManifestShipmentDetailsBD.ClientInfo.ClientID;
            NewWaybill.ClientAddressID = _ManifestShipmentDetailsBD.ClientInfo.ClientAddressID;
            NewWaybill.ClientContactID = _ManifestShipmentDetailsBD.ClientInfo.ClientContactID;
            NewWaybill.LoadTypeID = _ManifestShipmentDetailsBD.LoadTypeID;
            NewWaybill.ServiceTypeID = _ManifestShipmentDetailsBD.ServiceTypeID;
            NewWaybill.BillingTypeID = _ManifestShipmentDetailsBD.BillingType;
            NewWaybill.IsCOD = _ManifestShipmentDetailsBD.BillingType == 5 || _ManifestShipmentDetailsBD.CODCharge > 0;
            NewWaybill.ConsigneeID = _ManifestShipmentDetailsBD.ConsigneeInfo.ConsigneeID;
            NewWaybill.ConsigneeAddressID = _ManifestShipmentDetailsBD.ConsigneeInfo.ConsigneeDetailID;
            NewWaybill.OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CountryCode, IsCourierLoadType), IsCourierLoadType);
            NewWaybill.DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode, _ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode, IsCourierLoadType), IsCourierLoadType);
            NewWaybill.OriginCityCode = GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.CountryCode, IsCourierLoadType).ToString();
            NewWaybill.DestinationCityCode = GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode, _ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode, IsCourierLoadType).ToString();
            NewWaybill.CODCurrencyID = dc.Currencies.FirstOrDefault(p => p.CountryID == Convert.ToInt32(GlobalVar.GV.GetCountryIDByCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CountryCode))).ID;
            NewWaybill.PickUpDate = DateTime.Now;
            NewWaybill.PicesCount = _ManifestShipmentDetailsBD.PicesCount;
            NewWaybill.PromisedDeliveryDateFrom = null;
            NewWaybill.PromisedDeliveryDateTo = null;

            //if (GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode).HasValue)
            //    NewWaybill.ODADestinationID = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode).Value;
            var odaDestinationId = GlobalVar.GV.GetODAStationByCityandCountryCode(_ManifestShipmentDetailsBD.ConsigneeInfo.CityCode);

            if (odaDestinationId.HasValue)
                NewWaybill.ODADestinationID = odaDestinationId.Value;

            if (NewWaybill.ODADestinationID == 1237 && NewWaybill.ServiceTypeID == 4 && (NewWaybill.DestinationCityCode == "26"))//
            {
                NewWaybill.ODADestinationID = null;
            }
            NewWaybill.InsuredValue = _ManifestShipmentDetailsBD.InsuredValue;
            if (_ManifestShipmentDetailsBD.ServiceTypeID == 4 && NewWaybill.DeclaredValue == 0)
            {
                NewWaybill.DeclaredValue = _ManifestShipmentDetailsBD.InsuredValue;
            }
            NewWaybill.IsInsurance = _ManifestShipmentDetailsBD.InsuredValue > 0;
            NewWaybill.Weight = _ManifestShipmentDetailsBD.Weight < 0.1 ? 0.1 : Math.Round(_ManifestShipmentDetailsBD.Weight, 2);
            NewWaybill.Width = _ManifestShipmentDetailsBD.Width;
            NewWaybill.Length = _ManifestShipmentDetailsBD.Length;
            NewWaybill.Height = _ManifestShipmentDetailsBD.Height;
            NewWaybill.VolumeWeight = Math.Round(_ManifestShipmentDetailsBD.VolumetricWeight, 2);
            NewWaybill.BookingRefNo = "";
            NewWaybill.ManifestedTime = DateTime.Now;
            NewWaybill.GoodDesc = _ManifestShipmentDetailsBD.GoodDesc;
            NewWaybill.Incoterm = null;
            NewWaybill.IncotermID = null;
            NewWaybill.IncotermsPlaceAndNotes = null;

            NewWaybill.Latitude = _ManifestShipmentDetailsBD.Latitude;
            NewWaybill.Longitude = _ManifestShipmentDetailsBD.Longitude;
            NewWaybill.RefNo = GlobalVar.GV.GetString(_ManifestShipmentDetailsBD.RefNo, 100);
            NewWaybill.IsPrintBarcode = false;
            NewWaybill.StatusID = 1;
            NewWaybill.PODDetail = "";
            NewWaybill.DeliveryInstruction = _ManifestShipmentDetailsBD.DeliveryInstruction;
            NewWaybill.CODCharge = Math.Round(_ManifestShipmentDetailsBD.CODCharge, 2);
            NewWaybill.Discount = 0;
            NewWaybill.NetCharge = 0;
            NewWaybill.OnAccount = 0;
            NewWaybill.ServiceCharge = 0;
            NewWaybill.ODAStationCharge = 0;
            NewWaybill.OtherCharge = 0;
            NewWaybill.PaidAmount = 0;
            NewWaybill.SpecialCharge = 0;
            NewWaybill.StandardShipment = 0;
            NewWaybill.StorageCharge = 0;
            NewWaybill.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
            NewWaybill.IsShippingAPI = true;
            NewWaybill.PODTypeID = null;
            NewWaybill.PODDetail = "";
            NewWaybill.IsRTO = false;
            NewWaybill.IsManifested = false;
            NewWaybill.GoodsVATAmount = 0;
            NewWaybill.IsCustomDutyPayByConsignee = false;
            NewWaybill.Reference1 = _ManifestShipmentDetailsBD.Reference1;
            NewWaybill.Reference2 = _ManifestShipmentDetailsBD.Reference2;
            NewWaybill.Reference3 = "Bullet Delivery";
            NewWaybill.ConsigneeNationalID = null;
            NewWaybill.CurrencyID = _ManifestShipmentDetailsBD.CurrenyID;

            if (WaybillNo > 0)
            {
                if (!IsValidWBFormat(WaybillNo.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Invalid given WaybillNo.";
                    return result;
                }
                NewWaybill.WayBillNo = WaybillNo;
            }


            //SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Waybill Data Prepared.");

            #region "Migration to Stored Procedure"
            //CallSaveCustomerManifest
            try
            {
                string spName = "APICreateCustomerWaybill_WithPieceBarCode_NewLength";
                var w = new DynamicParameters();
                w.Add("@WayBillNo", NewWaybill.WayBillNo);
                w.Add("@ClientID", NewWaybill.ClientID);
                w.Add("@ClientAddressID", NewWaybill.ClientAddressID);
                w.Add("@ClientContactID", NewWaybill.ClientContactID);
                w.Add("@ServiceTypeID", NewWaybill.ServiceTypeID);
                w.Add("@LoadTypeID", NewWaybill.LoadTypeID);
                w.Add("@BillingTypeID", NewWaybill.BillingTypeID);
                w.Add("@ConsigneeID", NewWaybill.ConsigneeID);
                w.Add("@ConsigneeAddressID", NewWaybill.ConsigneeAddressID);
                w.Add("@OriginStationID", NewWaybill.OriginStationID);
                w.Add("@DestinationStationID", NewWaybill.DestinationStationID);
                w.Add("@PickUpDate", NewWaybill.PickUpDate);
                w.Add("@PicesCount", NewWaybill.PicesCount);
                w.Add("@Weight", NewWaybill.Weight);
                w.Add("@Width", NewWaybill.Width);
                w.Add("@Length", NewWaybill.Length);
                w.Add("@Height", NewWaybill.Height);
                w.Add("@VolumeWeight", NewWaybill.VolumeWeight);
                w.Add("@BookingRefNo", NewWaybill.BookingRefNo);
                w.Add("@ManifestedTime", NewWaybill.ManifestedTime);
                w.Add("@RefNo", NewWaybill.RefNo);
                w.Add("@IsPrintBarcode", NewWaybill.IsPrintBarcode);
                w.Add("@StatusID", NewWaybill.StatusID);
                w.Add("@BookingID", NewWaybill.BookingID);
                w.Add("@IsInsurance", NewWaybill.IsInsurance);
                w.Add("@DeclaredValue", NewWaybill.DeclaredValue);
                w.Add("@InsuredValue", NewWaybill.InsuredValue);
                w.Add("@PODTypeID", NewWaybill.PODTypeID);
                w.Add("@PODDetail", NewWaybill.PODDetail);
                w.Add("@DeliveryInstruction", NewWaybill.DeliveryInstruction);
                w.Add("@ServiceCharge", NewWaybill.ServiceCharge);
                w.Add("@StandardShipment", NewWaybill.StandardShipment);
                w.Add("@SpecialCharge", NewWaybill.SpecialCharge);
                w.Add("@ODAStationCharge", NewWaybill.ODAStationCharge);
                w.Add("@OtherCharge", NewWaybill.OtherCharge);
                w.Add("@Discount", NewWaybill.Discount);
                w.Add("@NetCharge", NewWaybill.NetCharge);
                w.Add("@PaidAmount", NewWaybill.PaidAmount);
                w.Add("@OnAccount", NewWaybill.OnAccount);
                w.Add("@StandardTariffID", NewWaybill.StandardTariffID);
                w.Add("@IsCOD", NewWaybill.IsCOD);
                w.Add("@CODCharge", NewWaybill.CODCharge);
                w.Add("@ProductTypeID", NewWaybill.ProductTypeID);
                w.Add("@IsShippingAPI", NewWaybill.IsShippingAPI);
                w.Add("@Contents", NewWaybill.Contents);
                w.Add("@BatchNo", NewWaybill.BatchNo);
                w.Add("@ODAOriginID", NewWaybill.ODAOriginID);
                w.Add("@ODADestinationID", NewWaybill.ODADestinationID);
                w.Add("@CreatedContactID", NewWaybill.CreatedContactID);
                w.Add("@IsRTO", NewWaybill.IsRTO);
                w.Add("@IsManifested", NewWaybill.IsManifested);
                w.Add("@GoodDesc", NewWaybill.GoodDesc);
                w.Add("@Incoterm", NewWaybill.Incoterm);
                w.Add("@IncotermID", NewWaybill.IncotermID);
                w.Add("@IncotermsPlaceAndNotes", NewWaybill.IncotermsPlaceAndNotes);
                w.Add("@Latitude", NewWaybill.Latitude);
                w.Add("@Longitude", NewWaybill.Longitude);
                w.Add("@HSCode", NewWaybill.HSCode);
                w.Add("@CustomDutyAmount", NewWaybill.CustomDutyAmount);
                w.Add("@GoodsVATAmount", NewWaybill.GoodsVATAmount);
                w.Add("@IsCustomDutyPayByConsignee", NewWaybill.IsCustomDutyPayByConsignee);
                w.Add("@Reference1", NewWaybill.Reference1);
                w.Add("@Reference2", NewWaybill.Reference2);
                w.Add("@Reference3", NewWaybill.Reference3);
                w.Add("@ConsigneeNationalID", NewWaybill.ConsigneeNationalID);
                w.Add("@CurrencyID", NewWaybill.CurrencyID);
                w.Add("@CODCurrencyID", NewWaybill.CODCurrencyID);
                w.Add("@IsSentSMS", NewWaybill.IsSentSMS);
                w.Add("@PromisedDeliveryDateFrom", NewWaybill.PromisedDeliveryDateFrom);
                w.Add("@PromisedDeliveryDateTo", NewWaybill.PromisedDeliveryDateTo);
                w.Add("@OriginCityCode", NewWaybill.OriginCityCode);
                w.Add("@DestinationCityCode", NewWaybill.DestinationCityCode);
                w.Add(name: "@RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
                w.Add(name: "@CustWaybillID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                //w.Add(name: "@WaybillNo", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using (var db = new SqlConnection(sqlCon))
                {
                    //GetWaybillNo on Saving
                    var returnCode = db.Execute(spName, param: w, commandType: CommandType.StoredProcedure, commandTimeout: 60);
                    NewWaybill.WayBillNo = w.Get<int>("@RetVal");
                    SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill SP Executed With Result Of: " + NewWaybill.WayBillNo);

                    if (NewWaybill.WayBillNo == -1)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceNullInvoiceNoExists");
                        return result;
                    }

                    NewWaybill.ID = w.Get<int>("@CustWaybillID");
                }
            }
            catch (Exception e)
            {
                //SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill SP Excuted Error. Exception: " + e.Message.ToString() + "      " + e.StackTrace.ToString());
                //LogException(e);
                //WritetoXML(_ManifestShipmentDetailsBD, _ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.RefNo, EnumList.MethodType.CreateWaybill, result);
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details code : 120";
                e.Source = _ManifestShipmentDetailsBD.RefNo + e.Source;
                GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetailsBD.ClientInfo);
                // GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _ManifestShipmentDetailsBD.ClientInfo);
            }


            #endregion

            if (!result.HasError)
            {
                result.WaybillNo = NewWaybill.WayBillNo;
                result.Key = NewWaybill.ID;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");


                try
                {
                    if (_ManifestShipmentDetailsBD.CreateBooking == true && ClientHasCreateBookingPermit)
                    {
                        SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Booking Creatation Start.");
                        string tempBookingRefNo = GetClientBookingRefNoToday(_ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.FirstAddress, DateTime.Now, NewWaybill.OriginStationID);
                        result.BookingRefNo = tempBookingRefNo;

                        if (tempBookingRefNo == "")
                        {
                            BookingShipmentDetails _bookingDetails = new BookingShipmentDetails
                            {
                                ClientInfo = _ManifestShipmentDetailsBD.ClientInfo,
                                BillingType = _ManifestShipmentDetailsBD.BillingType,
                                PicesCount = _ManifestShipmentDetailsBD.PicesCount,
                                Weight = _ManifestShipmentDetailsBD.Weight,
                                PickUpPoint = _ManifestShipmentDetailsBD.ClientInfo.ClientAddress.FirstAddress.Trim(),
                                SpecialInstruction = "",
                                OriginStationID = NewWaybill.OriginStationID,
                                DestinationStationID = NewWaybill.DestinationStationID,
                                OfficeUpTo = DateTime.Now,
                                PickUpReqDateTime = DateTime.Now,
                                ContactPerson = _ManifestShipmentDetailsBD.ClientInfo.ClientContact.Name,
                                ContactNumber = _ManifestShipmentDetailsBD.ClientInfo.ClientContact.PhoneNumber,
                                LoadTypeID = _ManifestShipmentDetailsBD.LoadTypeID,
                                WaybillNo = result.WaybillNo
                            };
                            _bookingDetails.ClientInfo.ClientAddressID = _ManifestShipmentDetailsBD.ClientInfo.ClientAddressID; // TODO: remove
                            _bookingDetails.ClientInfo.ClientContactID = _ManifestShipmentDetailsBD.ClientInfo.ClientContactID; // TODO: remove

                            Result BookingResult = new Result();
                            try
                            {
                                BookingResult = CreateBooking(_bookingDetails);
                            }
                            catch (Exception e)
                            {
                                SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Booking Creatation Exception.");
                                LogException(e);
                                WritetoXML(_ManifestShipmentDetailsBD, _ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.RefNo, EnumList.MethodType.CreateWaybill, result);
                                BookingResult.HasError = true;
                            }

                            SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Booking Creatation Result: " + BookingResult.HasError);
                            if (!BookingResult.HasError)
                                result.BookingRefNo = GlobalVar.GV.GetString(BookingResult.BookingRefNo, 20);
                        }

                        CustomerWayBill _objwaybill = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
                        _objwaybill.BookingRefNo = result.BookingRefNo;
                        dc.SubmitChanges();
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }

            }

            if (WaybillNo <= 0)
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetailsBD.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
            else
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetailsBD.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
            SaveToLogFile(_ManifestShipmentDetailsBD.ClientInfo.ClientID, _ManifestShipmentDetailsBD.RefNo, "Create Waybill Request Finish.");

            CustomerWayBill objwaybill = dc.CustomerWayBills.Where(c => c.WayBillNo == NewWaybill.WayBillNo).First();
            objwaybill.BookingRefNo = result.BookingRefNo;
            dc.SubmitChanges();
            return result;
        }
        #endregion CreatBulletDeliveryeWaybill Done By Sara Almalki

        //[WebMethod(EnableSession = true)]
        //public void AWS_Tracking(ClientInformation ClientInfo, List<int> WaybillNo)
        //{
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


        //    string json = "{\"d\":#d#}";
        //    try
        //    {
        //        List<int> filteredWaybills = WaybillNo.Where(o => o != 0 && IsValidWBFormat(o.ToString())).ToList();
        //        if (filteredWaybills.Count() == 0)
        //        {
        //            throw new Exception("Please provide a valid list of WaybillNo");
        //        }
        //        if (WaybillNo.Count > 500)
        //        {
        //            throw new Exception("You can track maximum 500 WayBill in a call");
        //        }
        //        //if (ClientInfo.ClientID != 1024600
        //        //    //&& ClientInfo.CheckClientInfo(ClientInfo, false).HasError
        //        //    )
        //        //{
        //        //    throw new Exception("The username or password for this client is wrong, please make sure to pass correct credentials");
        //        //}
        //        string waybillStr = "";
        //        foreach (int item in filteredWaybills)
        //        {
        //            waybillStr = waybillStr + "{\"term\": { \"waybillNo\": " + item.ToString() + "}},";
        //        }
        //        if (waybillStr != "")
        //            waybillStr = waybillStr.Remove(waybillStr.Length - 1);
        //        JavaScriptSerializer json_serializer = new JavaScriptSerializer();
        //        Uri ur = new Uri("https://search-naqelsearch-yruziv2ai3vdz45jkto5uantim.me-south-1.es.amazonaws.com/tracking_v2/_doc/_search?filter_path=hits.hits._source");
        //        var httpWebRequest = (HttpWebRequest)WebRequest.Create(ur);
        //        httpWebRequest.ContentType = "application/json";
        //        httpWebRequest.Method = "POST";
        //        var username = "naqelsearch";
        //        var password = "cde#$532dDER";
        //        string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
        //                                       .GetBytes(username + ":" + password));
        //        httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);
        //        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        //        {
        //            string jsonParam = "{\"size\": 1000,\"query\": {\"bool\": {\"must\": [{\"term\": { \"clientID\": "
        //                + ClientInfo.ClientID.ToString() + "}},{\"bool\": { \"should\": [" + waybillStr + "]}}]}}}";
        //            streamWriter.Write(jsonParam.ToString());
        //            streamWriter.Flush();
        //            streamWriter.Close();
        //        }
        //        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        //        var result = "";
        //        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        //        {
        //            result = streamReader.ReadToEnd();
        //            var settings = new JsonSerializerSettings
        //            {
        //                NullValueHandling = NullValueHandling.Ignore,
        //                MissingMemberHandling = MissingMemberHandling.Ignore
        //            };
        //            Root rootObject = JsonConvert.DeserializeObject<Root>(result, settings); if (rootObject.hits == null)
        //            {
        //                json = json.Replace("#d#", "[]");
        //            }
        //            else
        //            if (rootObject.hits.hits.Count > 0)
        //            {
        //                List<Source> lst = rootObject.hits.hits.Select(x => x._source).ToList();
        //                List<ClientResponse> responseList = lst.Select(p => new ClientResponse()
        //                {
        //                    StationCode = p.StationCode,
        //                    //Date = p.Date.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
        //                    Date = p.Date.ToUniversalTime(),
        //                    TrackingTypeID = p.TrackingTypeID,
        //                    Activity = p.Activity,
        //                    ActivityAr = p.ActivityAr,
        //                    WaybillNo = p.WaybillNo,
        //                    ClientID = p.ClientID,
        //                    HasError = Convert.ToBoolean(p.HasError),
        //                    ErrorMessage = p.ErrorMessage,
        //                    Comments = p.Comments,
        //                    RefNo = p.RefNo,
        //                    DeliveryStatusID = p.DeliveryStatusID,
        //                    DeliveryStatusMessage = p.DeliveryStatusMessage,
        //                    EventCode = p.EventCode
        //                }).ToList();
        //                json = json.Replace("#d#", Newtonsoft.Json.JsonConvert.SerializeObject(responseList));
        //            }
        //            else
        //            {
        //                json = json.Replace("#d#", "[]");
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ClientResponse obj = new ClientResponse
        //        {
        //            HasError = true,
        //            ErrorMessage = System.Web.HttpUtility.JavaScriptStringEncode(ex.Message),
        //        };
        //        json = json.Replace("#d#", Newtonsoft.Json.JsonConvert.SerializeObject(new List<ClientResponse> { obj }));
        //    }
        //    GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Trace_Shipment_By_WaybillNo, WaybillNo.ToString(), 4);
        //    HttpContext.Current.Response.ContentType = "application/xml";
        //    HttpContext.Current.Response.Flush();
        //    HttpContext.Current.Response.Write(json);
        //    HttpContext.Current.Response.End();
        //}


        [WebMethod(Description = "You can use this function to get details in waybill sticker respons (byte, waybillno and refno")]
        public GetWaybillStickerDetailsResult GetWaybillStickerDetails(ClientInformation clientInfo, int WaybillNo, StickerSize StickerSize)
        {


            GetWaybillStickerDetailsResult x = new GetWaybillStickerDetailsResult();

            string info = @"SELECT WaybillNo, RefNo
                FROM dbo.Customerwaybills with(nolock) WHERE WaybillNo = " + WaybillNo + " And ClientID = " + clientInfo.ClientID;


            using (SqlConnection connection = new SqlConnection(sqlCon))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(info, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            x.WaybillNo = reader.GetInt32(0);
                            x.RefNo = reader.GetString(1);
                        }
                    }
                }

            }


            if (!IsValidWBFormat(WaybillNo.ToString())) // Invalid WB format
                return x;

            if (clientInfo.CheckClientInfo(clientInfo, false).HasError) // Invalid API credential
                return x;

            if (!IsWBBelongsToClientGeneral(clientInfo.ClientID, new List<int>() { WaybillNo })) // WB not belongs to clientIDs
                return x;

            //if (IsAsrWaybill(clientInfo.ClientID, WaybillNo))
            //    return GetWaybillStickerASR(clientInfo, WaybillNo, StickerSize);

            string fileName = GenerateLabelSticker(clientInfo, new List<int>() { WaybillNo }, StickerSize);
            if (fileName == "")
                return x;



            FileStream fileStream = File.OpenRead(fileName);
            x.StickerByte = GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
            fileStream.Close();
            return x;




        }


        #region SchedulWaybill API Done By Sara Almalki
        [WebMethod(Description = " ")]
        public ScheduleWaybillResult ScheduleWaybill(ScheduleWaybill ScheduleWaybillRequest)
        {
            return SchedulWaybill(ScheduleWaybillRequest);
        }

        [WebMethod(Description = " ")]
        public ScheduleWaybillResult SchedulWaybill(ScheduleWaybill ScheduleWaybillRequest)
        {

            WritetoXMLUpdateWaybill(ScheduleWaybillRequest, ScheduleWaybillRequest.ClientInfo, ScheduleWaybillRequest.WaybillNo.ToString(), EnumList.MethodType.SchedulWaybill);
            ScheduleWaybillResult Result = new ScheduleWaybillResult();

            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            #region Validation
            // Empty ClientID / PW 
            if (string.IsNullOrWhiteSpace(ScheduleWaybillRequest.ClientInfo.ClientID.ToString()) || string.IsNullOrWhiteSpace(ScheduleWaybillRequest.ClientInfo.Password))
            {
                Result.HasError = true;
                Result.Message = "Please pass valid value in ClientID/Password";
                return Result;
            }

            // Credintial Validation

            ClientInformation ClientInfo = new ClientInformation(ScheduleWaybillRequest.ClientInfo.ClientID, ScheduleWaybillRequest.ClientInfo.Password);
            ClientInfo.Version = ScheduleWaybillRequest.ClientInfo.Version;
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                Result.HasError = true;
                Result.Message = "Invalid Credintial";
                return Result;
            }

            if (ScheduleWaybillRequest.WaybillNo.Int.Count > 50)
            {
                Result.HasError = true;
                Result.Message = $"Limit exceeded..!, maximum 50 waybills.";
                return Result;

            }

            //Date Validation
            Regex PickUpReqDate = new Regex(@"^(\d{4})-(\d\d)-(\d\d)T(\d\d):(\d\d):(\d\d)$");
            if (!PickUpReqDate.IsMatch(ScheduleWaybillRequest.ScheduleDate))
            {
                Result.HasError = true;
                Result.Message = $"please pass a valid date in valid format \" YYYY-MM-DDTHH:mm:ss \" ";
                return Result;
            }
            DateTime FormatedDate;



            bool _ScheduleDate = DateTime.TryParse(ScheduleWaybillRequest.ScheduleDate, out FormatedDate);


            if ((FormatedDate == null || FormatedDate.TimeOfDay < DateTime.Now.TimeOfDay) || (FormatedDate == null || FormatedDate.Date < DateTime.Now.Date))
            {
                Result.HasError = true;
                Result.Message = $"please pass a valid date / time ";
                return Result;
            }



            // Client Waybills Validation
            //*****
            var clientwaybills = dc.CustomerWayBills
                               .Where(model => model.ClientID == ScheduleWaybillRequest.ClientInfo.ClientID && model.StatusID == 1
                                && ScheduleWaybillRequest.WaybillNo.Int.Contains(model.WayBillNo))
                               .Select(model => model.WayBillNo).Distinct()
                               .ToList();


            if (clientwaybills.Count == 0)
            {
                Result.HasError = true;
                Result.Message = $"No waybills belong to your account";
                return Result;
            }
            //Prpare a list for Invalid Waybills
            List<int> InvalidWaybills = new List<int>();

            for (int i = 0; i < ScheduleWaybillRequest.WaybillNo.Int.Count(); i++)
            {
                if (!clientwaybills.Contains(ScheduleWaybillRequest.WaybillNo.Int[i]))
                {
                    InvalidWaybills.Add(ScheduleWaybillRequest.WaybillNo.Int[i]);
                }

            }


            //Invalid Waybills

            if (InvalidWaybills.Count > 0)
            {
                Result.HasError = true;
                Result.Message = $"These Waybills are not belong to your Account: {string.Join(", ", InvalidWaybills)}";
                return Result;
            }

            // waybill that has a pickup scan ******
            var WaybillsInfo = dc.CustomerWayBills
                              .Where(model => model.ClientID == ScheduleWaybillRequest.ClientInfo.ClientID
                               && clientwaybills.Contains(model.WayBillNo)).Distinct().ToList();


            //for (int i = 0; i < WaybillsInfo.Count(); i++)
            //{
            //    var Pickedup = dc.Trackings.Where(model => model.WaybillNo == WaybillsInfo[i].WayBillNo && model.TrackingTypeID == 1).Distinct().ToList();

            //    if (Pickedup.Count() > 0)
            //    {
            //        Result.HasError = true;
            //        Result.Message = $"This Waybill \" {WaybillsInfo[i].WayBillNo} \" alrady picked up by Naqel ";
            //        return Result;
            //    }

            //}



            for (int i = 0; i < WaybillsInfo.Count(); i++)
            {

                if (IsPickupExist(WaybillsInfo[i].WayBillNo, WaybillsInfo[i].ClientID))
                {
                    Result.HasError = true;
                    Result.Message = $"This Waybill \" {WaybillsInfo[i].WayBillNo} \" alrady picked up by Naqel ";
                    return Result;
                }

            }





            #endregion Validation




            #region SchedulWaybill logic



            string lastRefNo = "", newRefNo = "";
            PrevBooking PreviouseBooking = new PrevBooking();

            for (int i = 0; i < WaybillsInfo.Count(); i++)
            {
                int waybillNo = WaybillsInfo[i].WayBillNo;

                DateTime crntDt = GlobalVar.GV.GetCurrendDate();

                var ClientActiveBookingInSameDate = dc.Bookings.Where(model => model.ClientID == ScheduleWaybillRequest.ClientInfo.ClientID
               && model.IsCanceled == false && model.PickUpReqDT == FormatedDate
               && model.OriginStationID == WaybillsInfo[i].OriginStationID).FirstOrDefault();

                var ClientActiveBookingInDiffretrntDate = dc.Bookings.Where(model => model.ClientID == ScheduleWaybillRequest.ClientInfo.ClientID
               && model.IsCanceled == false && model.OriginStationID == WaybillsInfo[i].OriginStationID).FirstOrDefault();



                // there is exist active booking with same details Same Date/ clientID/ Statuse=1/ same origin
                // => return the same refNo
                if (ClientActiveBookingInSameDate != null)
                {

                    UpdateBookingRefNo_of_waybill(WaybillsInfo[i].WayBillNo, ClientActiveBookingInSameDate.RefNo, WaybillsInfo[i].ClientID);

                    continue;

                }
                //Active booking with with same details clientID/ Statuse=1/ same origin but dffrenet but different date
                //=>cancel the prviouse,then create new booking, update customerwaybills
                else if (ClientActiveBookingInDiffretrntDate != null)
                {
                    // cancel the previous booking
                    var DeactiveBooking = dc.Bookings.Where(model => model.RefNo == WaybillsInfo[i].BookingRefNo).FirstOrDefault();
                    DeactiveBooking.IsCanceled = true;
                    dc.SubmitChanges();
                    //request body has more than one waybill with same origin -- may will be removed not sure.
                    if (PreviouseBooking.ClientID == WaybillsInfo[i].ClientID &&
                        PreviouseBooking.OriginStation == WaybillsInfo[i].OriginStationID
                        && PreviouseBooking.Date == FormatedDate)
                    {
                        newRefNo = PreviouseBooking.RefNo;
                        UpdateBookingRefNo_of_waybill(WaybillsInfo[i].WayBillNo, newRefNo, WaybillsInfo[i].ClientID);
                        continue;
                    }
                    else
                        newRefNo = GlobalVar.GV.GetBookingRefNo(lastRefNo, WaybillsInfo[i].OriginStationID, crntDt);

                    // then go to create booking then update the customerwaybills
                }
                else

                    // don't have active booking || different origin
                    newRefNo = GlobalVar.GV.GetBookingRefNo(lastRefNo, WaybillsInfo[i].OriginStationID, crntDt);




                try
                {

                    Booking booking = new Booking
                    {

                        CityID = null,
                        ClientID = WaybillsInfo[i].ClientID,
                        SourceID = 16,// should be 16
                        PickUpReqDT = FormatedDate,
                        PickedUpDate = FormatedDate,
                        BookingDate = DateTime.Now,
                        OriginStationID = WaybillsInfo[i].OriginStationID,
                        DestinationStationID = WaybillsInfo[i].DestinationStationID,
                        ClientAddressID = WaybillsInfo[i].ClientAddressID,
                        WaybillNo = null,
                        RefNo = newRefNo,
                        ClientContactID = WaybillsInfo[i].ClientContactID,
                        BillingTypeID = WaybillsInfo[i].BillingTypeID,
                        //BookingDate = isCreateBookingNextday ? crntDt.AddDays(1) : crntDt,
                        //PickUpReqDT = _BookingShipmentDetail.PickUpReqDateTime.Date < (isCreateBookingNextday ? crntDt.AddDays(1) : crntDt).Date
                        //        ? _BookingShipmentDetail.PickUpReqDateTime.AddDays(1) : _BookingShipmentDetail.PickUpReqDateTime,
                        PicesCount = WaybillsInfo[i].PicesCount,
                        Weight = Math.Round(WaybillsInfo[i].Weight, 2),
                        //PickUpPoint = _BookingShipmentDetail.PickUpPoint,
                        //SpecialInstruction = (isCreateBookingNextday ? "[+1] " : "") + _BookingShipmentDetail.SpecialInstruction,
                        // RouteID = _BookingShipmentDetail.ClientInfo.ClientAddress.RouteID,
                        OfficeUpTo = DateTime.Now,
                        // PickedUpBy=1653,
                        // ContactPerson = _BookingShipmentDetail.ContactPerson,
                        // ContactNumber = _BookingShipmentDetail.ContactNumber,
                        LoadTypeID = WaybillsInfo[i].LoadTypeID,
                        ProductTypeID = WaybillsInfo[i].ProductTypeID,
                        Width = 1,
                        Length = 1,
                        Height = 1,
                        IsEmergency = false,
                        IsInsurance = false,
                        InsuranceValue = 0,
                        InsuranceCost = 0,
                        IsPickedUp = false,
                        IsCanceled = false,
                        CourierInformed = false,
                        CurrentStatusID = (int)EnumList.BookingState.Booked,
                        IsSpecialBooking = false,
                        IsMissPickUp = false,
                        Various = false,
                        IsSync = false,
                        IsDGR = false,
                        IsApproved = false,
                        IsScheduleBooking = false,
                        IsShippingAPI = true,
                    };


                    dc.Bookings.InsertOnSubmit(booking);
                    dc.SubmitChanges();

                }
                catch (Exception e)
                {
                    LogException(e);
                    Result.Message = e.Message;
                }


                // update customerwaybill set RefNo= newRefNo where waybillNo =WaybillsInfo[i].WaybillNo
                UpdateBookingRefNo_of_waybill(WaybillsInfo[i].WayBillNo, newRefNo, WaybillsInfo[i].ClientID);

                PreviouseBooking.ClientID = WaybillsInfo[i].ClientID;
                PreviouseBooking.OriginStation = WaybillsInfo[i].OriginStationID;
                PreviouseBooking.Date = FormatedDate;
                PreviouseBooking.RefNo = newRefNo;

            }


            Result.HasError = false;
            Result.Message = "The booking has been done successfully";
            return Result;
        }
        #endregion SchedulWaybill logic
        #endregion SchedulWaybill API Done By Sara Almalki



        public class PrevBooking
        {
            public string RefNo = null;
            public int OriginStation = 0;
            public int ClientID = 0;
            public DateTime Date;

        }


        #region UpdateBookingRefNo in customerwaybills table by using ( waybillNo, RefNo,ClientID) Done By Sara Amalki
        // function to update the BookingRefNo in customerwaybill acoording to waybillNo, and clientID

        public void UpdateBookingRefNo_of_waybill(int WaybillNo, string RefNo, int clientID)
        {
            using (var db = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection()))
            {
                var wb = db.CustomerWayBills.Where(w => w.WayBillNo == WaybillNo && w.ClientID == clientID).SingleOrDefault();
                wb.BookingRefNo = RefNo;
                try
                {
                    db.SubmitChanges();
                }
                catch (Exception e)
                {

                }
            }
        }
        #endregion UpdateBookingRefNo in customerwaybills table by using ( waybillNo, RefNo,ClientID)





        [WebMethod(Description = "You can use this function to get SPL oficcess")]
        //public SPLOfficeResult GetSPLOffices(ClientInformation ClientInfo)
        //{
        //    SPLOfficeResult result = new  SPLOfficeResult();

        public SPLOfficeResult GetSPLOffices(ClientInformation ClientInfo)
        {
            List<OfficeInfo> result = new List<OfficeInfo>();
            SPLOfficeResult r = new SPLOfficeResult();




            dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            #region Data validation
            //// add check client info ( ID & password ) 

            #region Solve Soap Exeption: System.InvalidOperationException:There is an error in XML document (13, 47). ---> System.FormatException: The string '' is not a valid AllXsd value.



            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                //result.Message = "Invalid credential!";
                //return result;
                r.HasError = true;
                r.Message = "Invalid credential!";
                return r;
            }
            else


            //if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;



                List<viwSploffice> listOffices =
                    dcMaster.viwSploffices.ToList();

                for (int i = 0; i < listOffices.Count; i++)
                {
                    viwSploffice instance = listOffices[i];
                    OfficeInfo newoffice = new OfficeInfo();
                    newoffice.OfficeCode = instance.OfficeCode;
                    newoffice.Name = instance.Name ?? "";
                    newoffice.FName = instance.FName ?? "";
                    newoffice.CityName = instance.CityName ?? "";
                    newoffice.RegionName = instance.RegionName ?? "";
                    newoffice.Lat = instance.Lat ?? "";
                    newoffice.Long = instance.Long ?? "";

                    //DateTime StartWorkingHour = r.Add(instance.StartWorkingHour);
                    //newoffice.StartWorkingHour = Convert.ToDateTime(instance.StartWorkingHour);
                    //newoffice.StartWorkingHour = instance.StartWorkingHour.HasValue ? instance.StartWorkingHour.Value.ToString(@"hh\:mm\:ss") : null;
                    //newoffice.EndWorkingHour = instance.EndWorkingHour.HasValue ? instance.EndWorkingHour.Value.ToString(@"hh\:mm\:ss") : null;
                    //newoffice.SaturdayStartWorkingHour = instance.SaturdayStartWorkingHour.HasValue ? instance.SaturdayStartWorkingHour.Value.ToString(@"hh\:mm\:ss") : null;
                    //newoffice.SaturdayEndtWorkingHour = instance.SaturdayEndtWorkingHour.HasValue ? instance.SaturdayEndtWorkingHour.Value.ToString(@"hh\:mm\:ss") : null;

                    newoffice.CityCode = instance.CityCode ?? "";
                    newoffice.CityNameAr = instance.CityNameAr ?? "";
                    newoffice.Country = instance.Country ?? "";
                    newoffice.ZipCode = instance.ZipCode ?? "";
                    newoffice.District = instance.District ?? "";
                    newoffice.DistrictAr = instance.ArDistrict ?? "";

                    newoffice.Street = instance.Street ?? "";
                    newoffice.StreetAr = instance.ArStreet ?? "";

                    newoffice.BuildingNumber = instance.BuildingNumber ?? "";
                    newoffice.AdditionalNumber = instance.AdditionalNumber ?? "";

                    newoffice.MonOpeningHour = instance.MonOpeningHour ?? "";
                    newoffice.MonClosingHour = instance.MonClosingHour ?? "";
                    newoffice.TuesOpeningHour = instance.TuesOpeningHour ?? "";
                    newoffice.TuesClosingHour = instance.TuesClosingHour ?? "";
                    newoffice.WedOpeningHour = instance.WedOpeningHour ?? "";
                    newoffice.WedClosingHour = instance.WedClosingHour ?? "";
                    newoffice.ThurOpeningHour = instance.ThurOpeningHour ?? "";
                    newoffice.ThurClosingHour = instance.ThurClosingHour ?? "";
                    newoffice.FriOpeningHour = instance.FriOpeningHour ?? "";
                    newoffice.FriClosingHour = instance.FriClosingHour ?? "";
                    newoffice.SatOpeningHour = instance.SatOpeningHour ?? "";
                    newoffice.SatClosingHour = instance.SatClosingHour ?? "";
                    newoffice.SunOpeningHour = instance.SunOpeningHour ?? "";
                    newoffice.SunClosingHour = instance.SunClosingHour ?? "";

                    result.Add(newoffice);
                }
            }
            r.OfficeList = result;
            r.Message = "success";
            r.HasError = false;
            return r;

            #endregion
            #endregion
        }



        //for Gnteq x Fedex Integrationn project only :
        // allow to update billtype and CODcharge 
        [WebMethod(Description = "Update the COD chareges for FedEx")]

        public ReceivableResult UpdateCharges(ReceivableList receivable)
        {
            ReceivableResult result = new ReceivableResult();
            if (receivable.Receivables.Count > 200)
            // Check if the request list exceeds 200 items
            {
                result.HasError = true;
                result.Message = "The request list cannot contain more than 200 items.";
                return result;
            }

            // Initialize a list to hold valid receivables
            List<Receivable> validReceivables = new List<Receivable>();

            //save request log
            // save in log table ( new )
            //set max object validation 
            // update the correct request a

            string _fexdexAcc = System.Configuration.ConfigurationManager.AppSettings["FedexAccount"].ToString();
            List<int> _fexdexAcclist = _fexdexAcc.Split(',').Select(Int32.Parse).ToList();

            // only allowed for Fedex
            if (!_fexdexAcclist.Contains(receivable.ClientInfo.ClientID))
            {
                result.HasError = true;
                result.Message = "You are not allowed to UpdateCharges";
                return result;
            }

            if (string.IsNullOrWhiteSpace(receivable.ClientInfo.ClientID.ToString()) || string.IsNullOrWhiteSpace(receivable.ClientInfo.Password))
            {
                result.HasError = true;
                result.Message = "Please pass valid value in ClientID/Password";
                return result;
            }

            // Credential Validation
            ClientInformation ClientInfo = new ClientInformation(receivable.ClientInfo.ClientID, receivable.ClientInfo.Password);
            ClientInfo.Version = receivable.ClientInfo.Version;
            if (ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                result.HasError = true;
                result.Message = "Invalid Credential";
                return result;
            }

            // Iterate through each receivable item
            foreach (var receivableItem in receivable.Receivables)
            {
                WritetoXMLUpdateWaybill(receivable, receivable.ClientInfo, receivableItem.Mtn, EnumList.MethodType.UpdateCODCharge);
                string refno = receivableItem.Mtn; // Assume Mtn is Refno

                // Skip the check for RodAmount, if needed you can uncomment the validation here

                // Check if Refno exists in CustomerWayBills
                bool refnoExists = GlobalVar.GV.IsFedexRefNoExist(refno);

                // If Refno exists, add the receivable item to validReceivables list
                if (refnoExists)
                {
                    if (!GlobalVar.GV.CheckUpdateChargeAvailable(refno, receivableItem.RodAmount))
                        validReceivables.Add(receivableItem);
                    else
                        result.AlreadyUpdatedMTN.Add(refno);
                }
                else
                {

                    // If Refno does not exist, add it to the result.Mtn list for feedback
                    result.InvalidMtn.Add(refno);
                }
            }
            bool isMtnUpdated = false;
            // If there are valid receivables, update charges
            if (validReceivables.Any())
            {
                // Create a new ReceivableList with only the valid items
                ReceivableList validReceivableList = new ReceivableList
                {
                    ClientInfo = receivable.ClientInfo,
                    Receivables = validReceivables
                };

                // Call UpdatechargeByAWB with only valid receivables
                isMtnUpdated = GlobalVar.GV.UpdatechargeByAWB(validReceivableList);

            }

            // If there were no valid receivables, set the message and return
            if (!validReceivables.Any() || !isMtnUpdated)
            {
                result.HasError = true;
                result.Message = "No valid reference numbers found.";
            }
            else if (isMtnUpdated && result.InvalidMtn.Count == 0 && result.AlreadyUpdatedMTN.Count == 0)
            {
                result.HasError = false;
                result.Message = "All MTN Updated Successfully";
            }
            else if ((result.InvalidMtn.Count > 0 || result.AlreadyUpdatedMTN.Count > 0) && validReceivables.Any() && isMtnUpdated)
            {
                result.HasError = true;
                result.Message = "MTN Updated Successfully except MTNs under Invalid MTN list or Already Updated MTN List.";
            }


            return result;
        }

        public bool UpdateCustomerBarCode(ManifestShipmentDetails msd)
        {
            bool hasError = true;


            // Fetch customer waybill ID based on the dispatch number
            int customerWaybillID = GlobalVar.GV.GetCustomerWaybillIDByDispatchNumber(msd.WaybillNo);
            if (customerWaybillID == 0)
            {
                return hasError;
            }

            // Fetch barcode records for the customer waybill ID
            List<string> customerBarcodes = GlobalVar.GV.GetCustomerBarcodesByWaybillID(customerWaybillID);
            string con = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(con))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    for (int i = 0; i < msd.Itempieceslist.Count; i++)
                    {
                        string updateQuery = @"
                    UPDATE CustomerBarCode
                    SET CustomerPieceBarCode = @receivableItemID
                    WHERE barcode = @barcode";

                        SqlCommand cmd = new SqlCommand(updateQuery, connection, transaction);

                        cmd.Parameters.AddWithValue("@barcode", customerBarcodes[i]);
                        cmd.Parameters.AddWithValue("@receivableItemID", msd.Itempieceslist[i].PieceBarcode);

                        cmd.ExecuteNonQuery();
                    }

                    hasError = false;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    transaction.Rollback();

                }
            }

            return hasError;

        }

        public int AddAPIError(int ClientID, string FunctionName, string Message = "")
        {
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            // Daisy EmployID: 18494
            string sql = @"Insert into APIError 
                    (ClientID, Date, Message, Source, StackTrace)
                    values (@ClientID, GETDATE(), @Message, 'API9.0', @FunctionName)";

            var db = new SqlConnection(con);
            try
            {
                var affectedRows = db.Execute(sql,
                  new
                  {
                      ClientID,
                      FunctionName,
                      Message
                  });

                return affectedRows;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return 0;
            }
        }

        [WebMethod(Description = "You can use this function to get SPL oficcess")]
        public ChargedWeightResult GetChargedWeight(ClientInformation ClientInfo, List<int> WaybillNumbers)
        {
            List<ChargedWeightInfo> result = new List<ChargedWeightInfo>();
            ChargedWeightResult r = new ChargedWeightResult();

            #region Data validation
            if (WaybillNumbers.Count > 100)
            {
                r.HasError = true;
                r.Message = "You can track maximum 100 WayBill in a call.";
                return r;
            }

            foreach (var wb in WaybillNumbers.Distinct())
            {
                if (!IsValidWBFormat(wb.ToString()))
                {
                    r.HasError = true;
                    r.Message = "Invalid WaybillNo format in the list.";
                    return r;
                }
            }

            var tempResult = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (tempResult.HasError)
            {
                r.HasError = true;
                r.Message = tempResult.Message;
                return r;
            }

            if (!IsWBBelongsToClientGeneral(ClientInfo.ClientID, WaybillNumbers))
            {
                r.HasError = true;
                r.Message = "Invalid WaybillNo under current credential.";
                return r;
            }
            #endregion

            var wbs = string.Join(",", WaybillNumbers);

            // Fetch data from the database
            string con = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
            using (var db = new SqlConnection(con))
            {
                string _sql = @"select
                    WaybillNo,
                    case when Weight > VolumeWeight then Weight else VolumeWeight end ChargedWeight,
                    Invoiced
                    from Waybill
                    where IsCancelled = 0
                    and WayBillNo in (
                    {0}
                    )";

                result = db.Query<ChargedWeightInfo>(string.Format(_sql, wbs)).ToList();
            }

            r.ChargedWeightList = result;
            r.Message = "success";
            r.HasError = false;
            return r;
        }
        [WebMethod(Description = "You can use this function to get ROD Status")]
        public RODInfoResult GetRODStatus(ClientInformation ClientInfo, List<string> ReferenceNumbers)
        {
            var rodStatus = new RODInfoResult();

            if (ReferenceNumbers == null || ReferenceNumbers.Count == 0)
            {
                rodStatus.HasError = true;
                rodStatus.Message = "Reference numbers list cannot be null or empty.";
                return rodStatus;
            }

            if (ReferenceNumbers.Count > 100)
            {
                rodStatus.HasError = true;
                rodStatus.Message = "You can track a maximum of 100 waybills per call.";
                return rodStatus;
            }

            var validationResult = ClientInfo.CheckClientInfo(ClientInfo, false);
            if (validationResult.HasError)
            {
                rodStatus.HasError = true;
                rodStatus.Message = validationResult.Message;
                return rodStatus;
            }

            try
            {
                string fedexAccounts = ConfigurationManager.AppSettings["FedexAccount"];
                if (string.IsNullOrWhiteSpace(fedexAccounts))
                {
                    rodStatus.HasError = true;
                    rodStatus.Message = "FedexAccount configuration is missing or empty.";
                    return rodStatus;
                }

                // Parse account list safely
                var clientIds = fedexAccounts
                    .Split(',')
                    .Select(s =>
                    {
                        if (int.TryParse(s.Trim(), out int id))
                            return id;
                        return -1;
                    })
                    .Where(id => id != -1)
                    .ToList();

                if (!clientIds.Any())
                {
                    rodStatus.HasError = true;
                    rodStatus.Message = "FedexAccount configuration is not valid.";
                    return rodStatus;
                }

                string connectionString = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    rodStatus.HasError = true;
                    rodStatus.Message = "Database connection string is missing.";
                    return rodStatus;
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT
                            cw.waybillno,
                            cw.refno,
                            cw.clientid AS Client,
                            CASE 
                                WHEN cl.id IS NOT NULL THEN CAST(1 AS BIT)
                                ELSE CAST(0 AS BIT)
                            END AS RODRecieved,
                            ISNULL(CAST(cl.CODCharge AS VARCHAR), 'NA') AS RODAmount
                        FROM customerwaybills cw
                        LEFT OUTER JOIN codupdatelogs cl ON cl.waybillno = cw.waybillno
                        WHERE cw.clientid IN @ClientIds
                          AND cw.refno IN @RefNumbers;";

                    var parameters = new
                    {
                        ClientIds = clientIds,
                        RefNumbers = ReferenceNumbers
                    };

                    var rodInfoList = SqlMapper.Query<RODinfo>(connection, query, parameters).ToList();

                    rodStatus.RODInfoList = rodInfoList;
                    rodStatus.HasError = false;
                    rodStatus.Message = "Success";
                }
            }
            catch (SqlException ex)
            {
                rodStatus.HasError = true;
                rodStatus.Message = "Database error: " + ex.Message;
            }
            catch (ConfigurationErrorsException ex)
            {
                rodStatus.HasError = true;
                rodStatus.Message = "Configuration error: " + ex.Message;
            }
            catch (Exception ex)
            {
                rodStatus.HasError = true;
                rodStatus.Message = "Unexpected error: " + ex.Message;
            }

            return rodStatus;
        }

        [WebMethod(Description = "You can use this function to get Dispatch manifest Status")]
        public DispatchInfoResult GetDispatchInfo(DispatchRequest DispatchRequest)
        {
            var dispatchInfo = new DispatchInfoResult();
            var dispatchInfoListList = new List<DispatchInfoList>();

            try
            {
                if (DispatchRequest.DispatchRequests.Count > 100)
                {
                    dispatchInfo.HasError = true;
                    dispatchInfo.Message = "You can track a maximum of 100 WayBill numbers per call.";
                    return dispatchInfo;
                }

                Result result = DispatchRequest.ClientInfo.CheckClientInfo(DispatchRequest.ClientInfo, false);
                if (result.HasError)
                {
                    dispatchInfo.HasError = true;
                    dispatchInfo.Message = result.Message;
                    return dispatchInfo;
                }

                List<int> clientIds = (ConfigurationManager.AppSettings["FedexAccount"] ?? "")
                    .Split(',')
                    .Select(s => int.Parse(s))
                    .ToList();

                List<int> loadTypes = (ConfigurationManager.AppSettings["FedexOutboundLoadType"] ?? "")
                    .Split(',')
                    .Select(s => int.Parse(s))
                    .ToList();

                string connectionString = ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
                var valuesBuilder = new StringBuilder();

                foreach (var req in DispatchRequest.DispatchRequests)
                {
                    string reference = req.Refernce.Replace("'", "''");
                    string loc = req.Loc.Replace("'", "''");
                    string manifestDate = req.manifestdate.ToString("yyyy-MM-dd");

                    valuesBuilder.AppendFormat("('{0}', '{1}', '{2}'),", reference, loc, manifestDate);
                }

                if (valuesBuilder.Length == 0)
                {
                    dispatchInfo.HasError = true;
                    dispatchInfo.Message = "No input provided.";
                    return dispatchInfo;
                }

                valuesBuilder.Length--; // remove trailing comma

                using (var connection = new SqlConnection(connectionString))
                {
                    string query = $@"
                        WITH InputData (reference1, loc, manifestdate) AS (
                            SELECT * FROM (VALUES {valuesBuilder}) AS v(reference1, loc, manifestdate)
                        )
                        SELECT
                            cw.waybillno,
                            cw.bookingrefno,
                            cw.clientid,
                            ca.AddressAR AS Loc,
                            CAST(cw.ManifestedTime AS DATE) AS ManifestedTime,
                            cw.clientid AS client,
                            cw.reference1 AS DispatchNumber
                        FROM CustomerWayBills cw WITH (NOLOCK)
                        JOIN ClientAddress ca WITH (NOLOCK) ON cw.ClientAddressID = ca.ID
                        JOIN InputData i ON cw.reference1 = i.reference1
                                        AND ca.AddressAR = i.loc
                                        AND CAST(cw.ManifestedTime AS DATE) = CAST(i.manifestdate AS DATE)
                        WHERE cw.clientid IN @ClientIds
                          AND cw.loadtypeid IN @LoadTypes;";

                    dispatchInfoListList = SqlMapper.Query<DispatchInfoList>(
                        connection,
                        query,
                        new { ClientIds = clientIds, LoadTypes = loadTypes }
                    ).ToList();
                }

                if (dispatchInfoListList.Any())
                {
                    dispatchInfo.DispatchInfoList = dispatchInfoListList;
                    dispatchInfo.Message = "Success";
                    dispatchInfo.HasError = false;
                }
                else
                {
                    dispatchInfo.Message = "No Result found. Please check the data provided";
                    dispatchInfo.HasError = false;
                }
            }
            catch (SqlException ex)
            {
                dispatchInfo.HasError = true;
                dispatchInfo.Message = "Database error occurred: " + ex.Message;
            }
            catch (FormatException ex)
            {
                dispatchInfo.HasError = true;
                dispatchInfo.Message = "Configuration error: " + ex.Message;
            }
            catch (Exception ex)
            {
                dispatchInfo.HasError = true;
                dispatchInfo.Message = "An unexpected error occurred: " + ex.Message;
            }

            return dispatchInfo;
        }

    }

}