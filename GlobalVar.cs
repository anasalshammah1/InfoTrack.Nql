using InfoTrack.BusinessLayer;
using InfoTrack.BusinessLayer.DContext;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using static InfoTrack.NaqelAPI.EnumList;
using Dapper;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.Data;
using static InfoTrack.NaqelAPI.ManifestShipmentDetails;

namespace InfoTrack.NaqelAPI
{
    internal class GlobalVar
    {
        private InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDocment;
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;

        public string XMLShippingServiceVersion = "5.0";
        public string IWCFIPhoneVersion = "1.4";
        public string WCFRouteOptimizationVersion = "2.0";
        public string XMLGeneralVersion = "3.0";
        private string DBName = System.Configuration.ConfigurationManager.AppSettings["DBName"].ToString();
        public string PublishType = System.Configuration.ConfigurationManager.AppSettings["PublishType"].ToString();

        InfoTrack.Security.SystemDBAccess.AccessType DBAccessType
        {
            get
            {
                string accType = System.Configuration.ConfigurationManager.AppSettings["AccessType"].ToString();
                if (accType == "WebApplication")
                    return Security.SystemDBAccess.AccessType.WebApplication;
                else
                    return Security.SystemDBAccess.AccessType.WindowsApplication;
            }
        }

        //SARA -- IsHub(), IsSub(), IsAT(): to identify the Route of clients ( Hub to hub, hub to sub, sub to sub ...ect)
        public int IsHub(string CityCode)
        {
            int IsHub = 0;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.Cities.Where(p => p.Code.Trim().ToLower() == CityCode.Trim().ToLower() && p.StatusID == 1 && p.IsHub == true).Count() > 0)
            {
                IsHub = 1;
                return IsHub;
            }
            else
            {
                return IsHub;
            }
        }

        //SARA -- IsHub(), IsSub(), IsAT(): to identify the Route of clients ( Hub to hub, hub to sub, sub to sub ...ect)
        public int IsSub(string CityCode)//SARA
        {
            int IsSub = 0;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.Cities.Where(p => p.Code.Trim().ToLower() == CityCode.Trim().ToLower() && p.StatusID == 1 && p.IsHub == false).Count() > 0)
            {
                IsSub = 1;
                return IsSub;
            }
            else
            {
                return IsSub;
            }
        }

        //SARA -- IsHub(), IsSub(), IsAT(): to identify the Route of clients ( Hub to hub, hub to sub, sub to sub ...ect)
        public int IsAT(string CityCode)
        {
            int IsAT = 0;
            if (IsHub(CityCode) == 1 || IsSub(CityCode) == 1)
                IsAT = 1;

            return IsAT;

        }

        public double RateWithTaxCalulater(double itemPrice, double TaxAmount)
        {

            var totalPrice = itemPrice + (itemPrice * TaxAmount);
            totalPrice = Math.Round(totalPrice, 2);
            return totalPrice;

        }

        public double PalletRateCalculater(ViewExpGetRateClient_API Route, int? Pices, string DesCityCod) // to check if it is ODA then add charges
        {
            //dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            double IncreasePercentage = 0;
            double? _MinCharg = Route.MinCharge;
            double? fuelCharge = Route.FSCPercentage;
            double _Amount = (double)Route.Amount;
            double SubRate = 0;
            double Rate = 0;
            double ODACharge = 0;
            if (fuelCharge.Equals(null))
            {
                fuelCharge = 0;
            }



            if (dcMaster.ViewExpGetRateClient_APIs.Where(Inc => Inc.AgreementID == Route.AgreementID && Route.AgreementPercentage != null).Count() > 0)
            {
                List<ViewExpGetRateClient_API> IncreasedAgreements = new List<ViewExpGetRateClient_API>();

                IncreasedAgreements = dcMaster.ViewExpGetRateClient_APIs.Where(Ag => Ag.AgreementID == Route.AgreementID).ToList();
                foreach (ViewExpGetRateClient_API IncAgr in IncreasedAgreements)
                {                // يحفظ الاسعار الجديد

                    IncreasePercentage = (double)(IncAgr.AgreementPercentage / 100);
                    _MinCharg = _MinCharg + _MinCharg * IncreasePercentage;
                    _Amount = _Amount + _Amount * IncreasePercentage;

                }



            }
            // معادلة الباليت 
            SubRate = Math.Ceiling((double)(_Amount * Pices));// service charg in waybill table
            fuelCharge = SubRate * (fuelCharge / 100);
            Rate = (double)(SubRate + fuelCharge);
            Rate = Math.Round(Rate, 2);

            // check if DesCity is ODA  then add extra charge 
            if (dcMaster.View_EXP_ODACharge_By_CityCodes.Where(C => C.CityCode == DesCityCod).Count() > 0)
            {
                ODACharge = dcMaster.View_EXP_ODACharge_By_CityCodes.Where(c => c.CityCode == DesCityCod).Select(c => c.ExpOdaNewCharge).FirstOrDefault();
                Rate = Rate + ODACharge;
            }
            return Rate;

        }

        public double ExpDomesticRateCalculater(ViewExpGetRateClient_API Route, double Weight, string DesCityCod)
        {
            Weight = Math.Ceiling(Weight);
            double IncreasePercentage = 0;
            double? _MinCharg = Route.MinCharge;
            double? fuelCharge = Route.FSCPercentage;
            double? _Amount = Route.Amount;
            double? _MinWeight = Route.MinWeight;
            double SubRate = 0;
            double Rate = 0;
            double ODACharge = 0;
            if (fuelCharge.Equals(null))
            {
                fuelCharge = 0;
            }
            if (dcMaster.ViewExpGetRateClient_APIs.Where(Inc => Inc.AgreementID == Route.AgreementID && Route.FSCPercentage != null).Count() > 0)
            {
                List<ViewExpGetRateClient_API> IncreasedAgreements = new List<ViewExpGetRateClient_API>();

                IncreasedAgreements = dcMaster.ViewExpGetRateClient_APIs.Where(Ag => Ag.AgreementID == Route.AgreementID).ToList();
                foreach (ViewExpGetRateClient_API IncAgr in IncreasedAgreements)
                {                // يحفظ الاسعار الجديد

                    IncreasePercentage = (double)(IncAgr.AgreementPercentage / 100);
                    _MinCharg = _MinCharg + _MinCharg * IncreasePercentage;
                    _Amount = _Amount + _Amount * IncreasePercentage;

                }
            }
            if (Weight <= Route.MinWeight)
            {
                Rate = (double)(_MinCharg + _MinCharg * (fuelCharge / 100));
            }
            else
            {
                double ExtralWeight = (double)(Weight - _MinWeight);
                double ExtraCharge = (double)(ExtralWeight * _Amount);
                SubRate = Math.Round((double)(_MinCharg + ExtraCharge));//service charge in waybill table
                fuelCharge = SubRate * (fuelCharge / 100);
                Rate = (double)(SubRate + fuelCharge);
                Rate = Math.Round(Rate, 2);
            }

            // check if DesCity is ODA  then add extra charge 
            if (dcMaster.View_EXP_ODACharge_By_CityCodes.Where(C => C.CityCode == DesCityCod).Count() > 0)
            {
                ODACharge = dcMaster.View_EXP_ODACharge_By_CityCodes.Where(c => c.CityCode == DesCityCod).Select(c => c.ExpOdaNewCharge).FirstOrDefault();
                Rate = Rate + ODACharge;
            }
            return Rate;

        }

        public enum ServiceTypes : int
        {
            Cargo = 1,
            LineHaul = 2,
            Document = 3,
            Express = 4,
            International = 6,
            InternationalCourier = 7,
            DomesticCourier = 8
        }

        public enum Incoterm : int
        {
            DDP = 1,
            DDU = 2,
            DDS = 3
        }

        public enum LoadTypes : int
        {
            Express = 1,
            Pallet = 4,
            ExpressIntl = 30,
            CourierIntl = 32,
            Document = 35,
            NonDocument = 36,
            ExpressDomestic = 39
        }

        public InfoTrack.Common.GlobalVarCommon GVCommon = new Common.GlobalVarCommon();
        public InfoTrack.Common.Security security = new Common.Security();

        public string PicPath
        {
            get { return System.Configuration.ConfigurationManager.ConnectionStrings["PicPath"].ToString(); }
        }

        public void SendEmail()
        {
            Common.MailShooting mail = new Common.MailShooting("naqelerrors@gmail.com", "sofansoft", "smtp.gmail.com", 587);
            mail.SendEmail("sofansoft@gmail.com", "test", "test");
        }

        public string GetDapperConnection()
        {
            string DapperString = System.Configuration.ConfigurationManager.ConnectionStrings["DapperConnectionString"].ConnectionString;
            return DapperString;
        }

        private string defaultconnection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
        private string DefaultConnection
        {
            get { return GlobalVar.GV.defaultconnection; }
            set { GlobalVar.GV.defaultconnection = value; }
        }

     
        private string defaultseconnection = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
        private string DefaultSEConnection
        {
            get { return GlobalVar.GV.defaultseconnection; }
            set { GlobalVar.GV.defaultseconnection = value; }
        }

        private string IPAddress = "";
        public string GetInfoTrackConnection()
        {
            if (GlobalVar.GV.DefaultConnection != "")
                return GlobalVar.GV.DefaultConnection;
            else
            {
                InfoTrack.Security.SystemDBAccess dbAccess = new InfoTrack.Security.SystemDBAccess();
                GlobalVar.GV.DefaultConnection = dbAccess.GetDataBaseConnectionString(GlobalVar.GV.DBName, 15380, "WSofan321", Convert.ToInt32(DBAccessType), "InfoTrack.NaqelAPI", IPAddress);
                return GlobalVar.GV.DefaultConnection;
            }
        }

        public string GetInfoTrackSEConnection()
        {
            if (GlobalVar.GV.DefaultSEConnection != "")
                return GlobalVar.GV.DefaultSEConnection;
            else
            {
                InfoTrack.Security.SystemDBAccess dbAccess = new InfoTrack.Security.SystemDBAccess();
                GlobalVar.GV.DefaultSEConnection = dbAccess.GetDataBaseConnectionString("ERPCourierSE", 15380, "WSofan321", Convert.ToInt32(DBAccessType), "InfoTrack.NaqelAPI", IPAddress);
                return DefaultSEConnection;
            }
        }

        public DateTime GetCurrendDate()
        {
            DateTime value = new DateTime();
            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            value = dcMaster.CurrentTime().Value;
            return value;
        }

        internal string GetBookingRefNo(string maxRefNo, int stationID, DateTime currentdt)
        {
            string refNo = "";

            if (maxRefNo.Length > 0)
            {
                string b = maxRefNo.Substring(maxRefNo.Length - 4, 4);
                refNo = Convert.ToString(Convert.ToInt32(b) + 1);

                refNo = maxRefNo.Substring(0, 6) + stationID.ToString().PadLeft(2, '0') + refNo.PadLeft(4, '0');
            }
            else
                refNo = currentdt.ToString("yyMMdd") + stationID.ToString().PadLeft(2, '0') + "0001";

            if (GetBookingRefNoCount(refNo) > 0)
                refNo = GetBookingRefNo(refNo, stationID, currentdt);

            return refNo;
        }

        internal int GetBookingRefNoCount(string RefNo)
        {
            int count = 0;
            dcDocment = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            count = dcDocment.Bookings.Where(P => P.RefNo == RefNo).Count();

            return count;
        }

        internal string GetFromToday(DateTime currentdt)
        {
            string result = "";
            result = currentdt.Year.ToString().Remove(0, 2) + currentdt.Month.ToString().PadLeft(2, '0') + currentdt.Day.ToString().PadLeft(2, '0');
            return result;
        }

        private CultureInfo cultureinfo;
        public CultureInfo CultInfo
        {
            get { return cultureinfo; }
            set { cultureinfo = value; }
        }

        public string GetLocalizationMessage(string key)
        {
            if (key == "") return "";
            string str = InfoTrack.Localization.Messages.getMessage(key, CultInfo);

            return str;
        }

        internal string GetString(string inputValue, int CharsCount)
        {
            string result = "";
            //result = "\"" + inputValue.Replace("'", "''") + "\"";
            result = inputValue;
            if (result.Length >= CharsCount)
                result = result.Remove(CharsCount);

            return result;
        }

        internal bool IsNumber(string text)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
            return regex.IsMatch(text);
        }

        internal int GetRouteID(string Phoneno, string Mobile)
        {
            int Route = 0;
            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dcMaster.PhoneRoutes.Where(P => P.PhoneNo == Phoneno).Count() > 0)
                Route = dcMaster.PhoneRoutes.First(P => P.PhoneNo == Phoneno).RouteID;

            if (Route == 0 && Mobile != "0")
            {
                if (dcMaster.PhoneRoutes.Where(P => P.PhoneNo == Mobile).Count() > 0)
                    Route = dcMaster.PhoneRoutes.First(P => P.PhoneNo == Mobile).RouteID;
            }

            return Route;
        }

        public PurposeList GetPurposeID(int ID)
        {
            switch (ID)
            {
                case 1:
                    return PurposeList.SharingLocationSMS;
                case 2:
                    return PurposeList.OCCSummary;
                case 3:
                    return PurposeList.VehicleBreakDown;


                case 8:
                    return PurposeList.Rating;
                case 9:
                    return PurposeList.NoAnswer;
                case 16:
                    return PurposeList.ForgotPassword;
                case 17:
                    return PurposeList.RefuseDelivery;
                case 18:
                    return PurposeList.ExpectedOutForDeliveryEnglishSMS;
                default:
                    return PurposeList.ForgotPassword;
            }
        }

        private static GlobalVar gv;
        public static GlobalVar GV
        {
            get
            {
                if (gv == null)
                {
                    gv = new GlobalVar();
                    gv.init();
                }
                return gv;
            }
            set { gv = value; }
        }

        public void init()
        {
            InfoTrack.BusinessLayer.GlobalVar.GV.accessType = GlobalVar.GV.DBAccessType == Security.SystemDBAccess.AccessType.WebApplication ? BusinessLayer.GlobalVar.AccessType.WebApplication : BusinessLayer.GlobalVar.AccessType.WindowsApplication;
            InfoTrack.BusinessLayer.GlobalVar.GV.InfoTrackDBName = GlobalVar.GV.DBName;
            InfoTrack.BusinessLayer.GlobalVar.GV.DeveloperID = 15380;
            InfoTrack.BusinessLayer.GlobalVar.GV.DeveloperPassword = "WSofan321";
            InfoTrack.BusinessLayer.GlobalVar.GV.IPAddress = "";
        }

        internal void CreateShippingAPIRequest(ClientInformation ClientInfo, EnumList.APIRequestType apiType, string Ref, int? Key)
        {
            try
            {
                // dcMaster = new  BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                // APIRequest instance = new  APIRequest();
                XmlShippingDal dAL = new XmlShippingDal();
                // instance.ClientID = ClientInfo.ClientID;
                // instance.APIRequestTypeID = Convert.ToInt32(apiType);
                //  instance.RefNo = Ref;
                if (Key != null)
                    //     instance.KeyID = Key;
                    dAL.InsertShippingAPIRequest(ClientInfo.ClientID, Convert.ToInt32(apiType), Ref, Key);
                // dcMaster.APIRequests.InsertOnSubmit(instance);
                //  dcMaster.SubmitChanges();
            }
            catch (Exception e) { AddErrorMessage(e, ClientInfo); }
        }

        internal void CreateShippingAPIRequest(ClientInformation ClientInfo, EnumList.APIRequestType apiType, string Ref, int? Key, DateTime StartTime, DateTime EndTime)
        {
            try
            {
                dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                APIRequest instance = new APIRequest();
                instance.ClientID = ClientInfo.ClientID;
                instance.APIRequestTypeID = Convert.ToInt32(apiType);
                instance.RefNo = Ref;
                instance.StartTime = StartTime;
                instance.EndTime = EndTime;

                if (Key != null)
                    instance.KeyID = Key;

                dcMaster.APIRequests.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
            }
            catch (Exception e) { AddErrorMessage(e, ClientInfo); }
        }

        internal void AddErrorMessage(Exception e, ClientInformation clientinfo)
        {
            try
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                APIError instance = new APIError();
                instance.ClientID = clientinfo.ClientID;
                instance.Date = GlobalVar.GV.GetCurrendDate();

                instance.Message = e.Message;
                //if (instance.Message.Length > 500)
                //    instance.Message = instance.Message.Remove(500);
                instance.Source = e.Source;

                //if (instance.Source != null && instance.Source.Length > 500)
                //    instance.Source = instance.Source.Remove(500);

                instance.StackTrace = e.StackTrace;
                //if (instance.StackTrace != null && instance.StackTrace.Length > 3500)
                //    instance.StackTrace = instance.StackTrace.Remove(3500);

                dcMaster.APIErrors.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
            }
            catch { }
        }

        internal void AddErrorMessage1(string e, ClientInformation clientinfo)
        {
            try
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                APIError instance = new APIError();
                instance.ClientID = clientinfo.ClientID;
                instance.Date = GlobalVar.GV.GetCurrendDate();

                instance.Message = e;
                //if (instance.Message.Length > 500)
                //    instance.Message = instance.Message.Remove(500);
                instance.Source = e;

                //if (instance.Source != null && instance.Source.Length > 500)
                //    instance.Source = instance.Source.Remove(500);

                instance.StackTrace = e;
                //if (instance.StackTrace != null && instance.StackTrace.Length > 3500)
                //    instance.StackTrace = instance.StackTrace.Remove(3500);

                dcMaster.APIErrors.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
            }
            catch { }
        }

        //public int GetWaybillNo(EnumList.ProductType ProductTypeID)
        //{
        //    int WayBillNumber = 0;
        //    dc = new  DataDataContext();
        //    int WaybillCount = 0;

        //    if (ProductTypeID == EnumList.ProductType.Home_Delivery)
        //    {
        //        var y = from p in dc.CustomerWayBills
        //                where p.ProductTypeID == Convert.ToInt32(ProductTypeID)
        //                select p.WayBillNo;

        //        WaybillCount = dc.CustomerWayBills.Select(P => P.ProductTypeID == Convert.ToInt32(ProductTypeID)).Count();
        //        WaybillCount = y.Count();

        //        int Count = 1;
        //        while (Count != 0)
        //        {
        //            var x = from p in dc.CustomerWayBills
        //                    where p.ProductTypeID == Convert.ToInt32(ProductTypeID) &&
        //                          p.WayBillNo.ToString().StartsWith(Convert.ToInt32(EnumList.ProductType.Home_Delivery).ToString())
        //                    select p.WayBillNo;
        //            WayBillNumber = x.Max();
        //            WayBillNumber = WayBillNumber + 1;
        //            Count = dc.CustomerWayBills.Where(p => p.WayBillNo == WayBillNumber).Count();
        //        }
        //    }
        //    return WayBillNumber;
        //}

        public int GetWaybillNo(EnumList.ProductType ProductTypeID, int ClientID)
        {
            int lastwaybillno = 0;

            if (ProductTypeID == EnumList.ProductType.Home_Delivery)
            {
                InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dc
                    = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster
                    = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                //InfoTrack.BusinessLayer.DContext.apiclien

                var x2 = from P in dcMaster.APIClientWaybillRanges
                         where P.ClientID == ClientID
                         select P;
                lastwaybillno = x2.ToList().Last().ToWaybillNo;

                //var x = (from p in dc.CustomerWayBills
                //         where p.ProductTypeID == Convert.ToInt32(ProductTypeID) &&
                //               p.WayBillNo.ToString().StartsWith(Convert.ToInt32(EnumList.ProductType.Home_Delivery).ToString())
                //               && p.ClientID == ClientID && p.WayBillNo >= x2.ToList().Last().FromWaybillNo
                //         select p.WayBillNo).ToList();

                var x = (from p in dc.CustomerWayBills
                         where p.ProductTypeID == Convert.ToInt32(ProductTypeID)
                               && p.ClientID == ClientID && p.WayBillNo >= x2.ToList().Last().FromWaybillNo && p.StatusID == 1
                         select p.WayBillNo).ToList();

                int LastCustomerWaybillNo = 0;

                if (x.Count > 0)
                    LastCustomerWaybillNo = x.Max();
                if (LastCustomerWaybillNo == 0)
                    LastCustomerWaybillNo = x2.ToList().Last().FromWaybillNo;
                else
                    LastCustomerWaybillNo = LastCustomerWaybillNo + 1;

                if (LastCustomerWaybillNo <= lastwaybillno)
                    lastwaybillno = LastCustomerWaybillNo;
                else
                    lastwaybillno = 0;

            }

            return lastwaybillno;
        }

        public int GetWaybillNo(EnumList.ProductType ProductTypeID)
        {
            int lastwaybillno = 0;

            //ANIL
            if (ProductTypeID == EnumList.ProductType.Home_Delivery)
            {
                using (var db = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
                {
                    APIClientWaybillRange range =
                        db.Query<APIClientWaybillRange>("Select MAX(ToWaybillNo) as ToWaybillNo from APIClientWaybillRange with (NOLOCK) ").FirstOrDefault();
                    if (range != null)
                        lastwaybillno = range.ToWaybillNo;

                    InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dc = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                    InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                    //InfoTrack.BusinessLayer.DContext.apiclien
                    /* if (dcMaster.APIClientWaybillRanges.Count() > 0)
                     {
                         var x2 = from P in dcMaster.APIClientWaybillRanges
                                  select P.ToWaybillNo;
                         lastwaybillno = x2.Max();
                     }
                     */

                    int Count = 1;
                    while (Count != 0)
                    {

                        CustomerWayBill maxCustomerwaybill =
                               db.Query<CustomerWayBill>("Select MAX(WayBillNo) as WaybillNo from CustomerWayBills with (NOLOCK)" +
                               " where ProductTypeID =" + Convert.ToInt32(ProductTypeID).ToString() + " and convert(varchar,waybillno) like '7%' ").FirstOrDefault();// 7=(EnumList.ProductType.Express

                        /*var x = from p in dc.CustomerWayBills
                            where p.ProductTypeID == Convert.ToInt32(ProductTypeID) &&
                                  p.WayBillNo.ToString().StartsWith(Convert.ToInt32(EnumList.ProductType.Express).ToString())
                            select p.WayBillNo;*/

                        int LastCustomerWaybillNo = maxCustomerwaybill.WayBillNo;
                        if (LastCustomerWaybillNo > lastwaybillno)
                            lastwaybillno = LastCustomerWaybillNo;
                        lastwaybillno += 1;

                        Count = db.Query<CustomerWayBill>("select top 1 Waybillno from CustomerWayBills with (NOLOCK) where WayBillNo =" + lastwaybillno).Count();
                    }
                }
            }


            return lastwaybillno;

        }

        public int GetWaybillNoTEST(EnumList.ProductType ProductTypeID, int ClientId)
        {
            int lastwaybillno = 0;

            //ANIL
            if (ProductTypeID == EnumList.ProductType.Home_Delivery)
            {
                using (var db = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
                {
                    APIClientWaybillRange PendingRange = db.Query<APIClientWaybillRange>("select * from APIClientWaybillRange where clientid = " + ClientId.ToString() + " and statusid = 1 and (TotalCount - TotalWaybillUsed) > 1 order by FromWaybillNo").LastOrDefault();

                    if (PendingRange == null)
                    {
                        var newrage = CreateWaybillRangeInternal(ClientId);
                        lastwaybillno = newrage.FromWaybillNo;
                    }
                    else
                    {
                        CustomerWayBill CusWaybill = db.Query<CustomerWayBill>("select Max(WaybillNo) as WaybillNo from CustomerWaybills where WaybillNo >= " + PendingRange.FromWaybillNo.ToString() + " and WaybillNo <= " + PendingRange.ToWaybillNo.ToString() + " and ClientId = " + ClientId.ToString()).FirstOrDefault();

                        if (CusWaybill == null)
                        {
                            lastwaybillno = PendingRange.FromWaybillNo;
                            return lastwaybillno;
                        }

                        lastwaybillno = CusWaybill.WayBillNo + 1;
                        if (PendingRange.ToWaybillNo < lastwaybillno)
                        {
                            var newrage = CreateWaybillRangeInternal(ClientId);
                            lastwaybillno = newrage.FromWaybillNo;
                        }
                        else if (lastwaybillno == 1)
                            lastwaybillno = PendingRange.FromWaybillNo;
                        //return lastwaybillno;
                    }
                }
            }
            return lastwaybillno;
        }

        public WaybillRange CreateWaybillRangeInternal(int _clientId)
        {
            WaybillRange result = new WaybillRange();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            int WaybillsCount = 0;
            InfoTrack.BusinessLayer.DContext.APIClientAccess aPIClientAccess = dcMaster.APIClientAccesses.First(P => P.ClientID == _clientId);
            WaybillsCount = aPIClientAccess.MaxRange;

            APIClientWaybillRange newrangeInstance = new APIClientWaybillRange();
            newrangeInstance.ClientID = _clientId;
            if (WaybillsCount == 0)
                WaybillsCount = 1000;

            newrangeInstance.StatusID = 1;
            newrangeInstance.Date = DateTime.Now;
            newrangeInstance.TotalCount = 0;
            newrangeInstance.TotalWaybillUsed = 0;

            newrangeInstance.FromWaybillNo = GlobalVar.GV.GetWaybillNo(EnumList.ProductType.Home_Delivery);
            newrangeInstance.ToWaybillNo = newrangeInstance.FromWaybillNo + WaybillsCount - 1;


            dcMaster.APIClientWaybillRanges.InsertOnSubmit(newrangeInstance);
            dcMaster.SubmitChanges();

            result.FromWaybillNo = newrangeInstance.FromWaybillNo;
            result.ToWaybillNo = newrangeInstance.ToWaybillNo;

            //GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.CreateWaybillRange,
            //                                  newrangeInstance.FromWaybillNo.ToString(), newrangeInstance.ID);


            return result;
        }

        internal int GetServiceTypeID(ClientInformation ClientInfo, int LoadType)
        {
            int result = 0;

            var dcMasterContext = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMasterContext.ViwLoadTypeByClients.Where(P => P.ClientID == ClientInfo.ClientID && P.ID == LoadType).Count() > 0)
                result = dcMasterContext.ViwLoadTypeByClients.First(P => P.ClientID == ClientInfo.ClientID && P.ID == LoadType).ServiceTypeID;

            return result;
        }

        internal int GetProductTypeID(ClientInformation ClientInfo, int LoadType)
        {
            int result = 0;
            var dcMasterContext = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMasterContext.LoadTypes.Where(P => P.ID == LoadType).Count() > 0)
                result = dcMasterContext.LoadTypes.First(P => P.ID == LoadType).ProductTypeID.Value;

            return result;
        }

        public string GetEmpID(byte[] str)
        {
            string result = "";
            try
            {
                InfoTrack.Common.Security sec = new Common.Security();
                result = sec.Decrypt(str);
            }
            catch { result = ""; }
            return result;
        }

        public bool IsSecure(byte[] str)
        {
            bool reslut = false;
            string dec = "";
            try
            {
                InfoTrack.Common.Security sec = new Common.Security();
                dec = sec.Decrypt(str);
                reslut = true;
            }
            catch { reslut = false; }

            return reslut;
        }

        public void InsertError(string ErrorText, string UserID)
        {
            ErrorText = " User ID : " + UserID + " Web Service Version : " + XMLShippingServiceVersion + " -  " + ErrorText;
            //if (ErrorText.Contains("A generic error occurred in GDI")) return;
            //try
            {
                InfoTrack.NaqelAPI.App_Data.InfoTrackSEDataTableAdapters.messTableAdapter mAdapter =
                    new App_Data.InfoTrackSEDataTableAdapters.messTableAdapter();
                mAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                mAdapter.Insert(ErrorText);
            }
            //catch { }
        }

        #region Validation Data

        public bool IsStationCorrect(int StationID)
        {
            bool result = false;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.Stations.Where(P => P.ID == StationID && P.StatusID == 1).Count() > 0)
                result = true;

            return result;
        }

        //public bool IsCityCorrect(int CityID)
        //{
        //    bool result = false;

        //     DataDataContext dc = new  DataDataContext(GlobalVar.GV.GetERPNaqelConnection());
        //    if (dc.Cities.Where(P => P.ID == CityID && P.StatusID = 1).Count() > 0)
        //        result = true;

        //    return result;
        //}

        public bool IsCityCodeExist(string CityCode, bool? IsCourierLoadType = false)
        {
            if (string.IsNullOrWhiteSpace(CityCode))
                return false;

            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //if (dc.Cities.Where(P => P.ISOCityCode.Trim().ToLower().Contains(CityCode.Trim().ToLower()) && P.StatusID == 1).Count() > 0)
            //    result = true;
            if (IsCourierLoadType == true && dc.Cities.Where(P => P.StatusID == 1 && P.DivisionID == 5 && P.Code.Trim().ToLower() == CityCode.Trim().ToLower()).Count() > 0)
                return true;
            else if (IsCourierLoadType == false && dc.Cities.Where(P => P.StatusID == 1 && P.Code.Trim().ToLower() == CityCode.Trim().ToLower()).Count() > 0)
                return true;
            else if (dc.ViwODAStationAPIs.Where(P => P.StatusID == 1 && P.ISOCityCode.Trim().ToLower().Contains(CityCode.Trim().ToLower())).Count() > 0)
                return true;

            return false;
        }

        public bool IseCorrectCityCode(string CityCode)
        {
            if (string.IsNullOrWhiteSpace(CityCode))
                return false;
            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dc.Cities.Where(P => P.Code.Trim().ToLower() == CityCode.Trim().ToLower() && P.StatusID == 1).Count() > 0)
                return true;
            return false;
        }

        //public bool IsDDUClient (int ClientID)
        //{
        //    InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

        //    if (dc.DDU_Clients.Where(P =>P.ClientID == ClientID && P.StatusID== 1 ).Count()>0)
        //        return true;

        //    return false;
        //}

        public bool IsCorrectIncoterm(string incoterm)
        {

            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dc.IncoTermTypes.Where(P => P.Code.Trim().ToLower() == incoterm.Trim().ToLower() && P.StatusID == 1).Count() > 0)
                return true;
            return false;
        }

        public int GetIncotermID(string incoterm)
        {
            int IncoID = 0;

            try
            {
                InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                IncoID = dc.IncoTermTypes.First(P => P.Code.Trim().ToLower() == incoterm.Trim().ToLower() && P.StatusID == 1).ID;


                return IncoID;
            }

            catch (Exception e)
            {
                IncoID = 0;

            }
            return IncoID;

        }

        public bool IsCountryCodeExist(string CountryCode)
        {
            if (string.IsNullOrWhiteSpace(CountryCode))
                return false;

            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dc.Countries.Where(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).Count() > 0)
                return true;

            return false;
        }

        public bool IsBillingCorrect(int BillingTypeID, int ClientID)
        {
            bool result = false;

            if (new List<int>() { 1, 2, 5, 6 }.Contains(BillingTypeID))
            {
                result = true;
                return result;
            }

            if (ClientID == 1024600 && BillingTypeID == 3)
            {
                result = true;
                return result;
            }
            return result;
        }

        public bool IsLoadTypeCorrect(ClientInformation ClientInfo, int LoadTypeID)
        {
            bool result = false;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.ViwLoadTypeByClients.Where(P => P.ClientID == ClientInfo.ClientID && P.ID == LoadTypeID).Any())
                result = true;

            return result;
        }

        //public Result IsRefNoCorrect(int _clientID, string _refNo)
        //{
        //    Result result = new Result();
        //    InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dc = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

        //    if (dc.CustomerWayBills.Where(p => p.ClientID == _clientID && p.RefNo == _refNo).Count() > 0)
        //    {
        //        var waybillInstance= dc.CustomerWayBills.FirstOrDefault(p => p.ClientID == _clientID && p.RefNo == _refNo);
        //        result.WaybillNo = waybillInstance.WayBillNo;
        //        result.Key = waybillInstance.ID;
        //        result.HasError = false;
        //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");
        //    }
        //    else
        //        result.HasError = false;

        //    return result;
        //}
        #endregion

        internal int GetStationByCity(int CityID, bool IsCourierLoadType = false)
        {
            int _stationID = 0;

            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (IsCourierLoadType && dcData.Cities.Where(P => P.ID == CityID && P.StatusID == 1 && P.DivisionID == 5 && P.StationID != null).Any())
                _stationID = dcData.Cities.First(P => P.ID == CityID && P.StatusID == 1 && P.DivisionID == 5 && P.StationID != null).StationID.Value;
            else
            {
                if (dcData.Stations.Where(P => P.CityID == CityID && P.StatusID == 1).Any())
                    _stationID = dcData.Stations.First(P => P.CityID == CityID && P.StatusID == 1).ID;
                else if (dcData.Cities.Where(P => P.ID == CityID && P.StatusID == 1 && P.StationID != null).Any())
                    _stationID = dcData.Cities.First(P => P.ID == CityID && P.StatusID == 1 && P.StationID != null).StationID.Value;
            }


            return _stationID;
        }

        internal int? GetODAStationByCityandCountryCode(string CityCode)
        {
            int _stationID = 0;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.ViwODAStationAPIs.Where(P => P.ISOCityCode.ToLower() == CityCode.ToLower()).Any())
                _stationID = dcMaster.ViwODAStationAPIs.First(P => P.ISOCityCode.ToLower() == CityCode.ToLower()).ID;

            if (_stationID > 0)
                return _stationID;
            else
                return null;
        }

        internal string GetCityCodeByStationID(int StationID)
        {
            string result = "";

            //InfoTrack.NaqelAPI. DataDataContext dcData = new  DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Stations.Where(P => P.ID == StationID).Any())
            {
                InfoTrack.BusinessLayer.DContext.Station instance = dcData.Stations.First(P => P.ID == StationID);
                if (instance.CityID.HasValue)
                    result = dcData.Cities.First(P => P.ID == instance.CityID.Value).Code;
            }

            return result;
        }

        internal string GetCityISOCityCodeByCityID(int CityID)
        {
            string result = "";

            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Cities.Where(P => P.ID == CityID).Any())
                result = dcData.Cities.First(P => P.ID == CityID).ISOCityCode;

            return result;
        }

        internal string GetCityCodeByCityID(int CityID)
        {
            string result = "";

            //InfoTrack.NaqelAPI. DataDataContext dcData = new  DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Cities.Where(P => P.ID == CityID).Count() > 0)
                result = dcData.Cities.First(P => P.ID == CityID).Code;

            return result;
        }

        internal string GetCountryCodeByCityID(int CityID)
        {
            string result = "";

            //InfoTrack.NaqelAPI. DataDataContext dcData = new  DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Countries.Where(P => P.ID == dcData.Cities.First(c => c.ID == CityID).CountryID).Count() > 0)
                result = dcData.Countries.First(P => P.ID == dcData.Cities.First(c => c.ID == CityID).CountryID).Code;

            return result;
        }

        internal int GetCityIDByCityCode(string CityCode, string CountryCode, bool? IsCourierLoadType = false)
        {
            int _cityID = 0;
            int CountryID = 0;

            if (string.IsNullOrWhiteSpace(CountryCode) || string.IsNullOrWhiteSpace(CityCode))
                return _cityID;

            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Countries.Where(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).Any())
                CountryID = dcData.Countries.First(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).ID;
            else
                return _cityID;

            //if (dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.ISOCityCode.Trim().ToLower().Contains(ISOCityCode.Trim().ToLower())).Any())
            //    _cityID = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.ISOCityCode.Trim().ToLower() == ISOCityCode.Trim().ToLower()).ID;
            if (IsCourierLoadType == true && dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.DivisionID == 5 && P.Code.Trim().ToLower().Contains(CityCode.Trim().ToLower())).Any())
                _cityID = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.DivisionID == 5 && P.Code.Trim().ToLower() == CityCode.Trim().ToLower()).ID;
            else if (IsCourierLoadType == false && dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.Code.Trim().ToLower().Contains(CityCode.Trim().ToLower())).Any())
                _cityID = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.Code.Trim().ToLower() == CityCode.Trim().ToLower()).ID;
            else if (dcData.ViwODAStationAPIs.Where(P => P.ISOCityCode.Trim().ToLower().Contains(CityCode.Trim().ToLower()) && P.CountryID == CountryID).Any())
                _cityID = dcData.ViwODAStationAPIs.First(P => P.ISOCityCode.Trim().ToLower().Contains(CityCode.Trim().ToLower()) && P.CountryID == CountryID).CityID;

            return _cityID;
        }

        public byte[] ConvertStreamToByteBuffer(System.IO.Stream theStream)
        {
            int b1;
            System.IO.MemoryStream tempStream = new System.IO.MemoryStream();
            while ((b1 = theStream.ReadByte()) != -1)
            {
                tempStream.WriteByte(((byte)b1));
            }
            return tempStream.ToArray();
        }

        public bool CheckCurrency(int currId)
        {
            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcData = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //bool result;
            if (dcData.Currencies.Where(p => p.ID == currId).Count() > 0)
            {
                return false;
            }
            return true;
        }

        public bool consigneeID_Validation(int tempclientID)
        {
            bool result = false;
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.consigneeID_HSCodes.Where(p => p.ClientID == tempclientID && p.isConsigneeIDRequired == true && p.StatusID == true).Count() > 0)
            {
                result = true;

            }
            return result;
        }

        public bool HScode_Validation(int tempclientID)
        {
            bool result = false;
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.consigneeID_HSCodes.Where(p => p.ClientID == tempclientID && p.isHsCodeRequired == true && p.StatusID == true).Count() > 0)
            {
                result = true;
            }
            return result;
        }

        public bool ValidateClientCommercialInvoice(int tempclientID)
        {
            bool result = false;
            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcData = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.ValidateClientCommercialInvoices.Where(p => p.ClientID == tempclientID && p.StatusID == 1 && p.CheckUnitWeight == true).Count() > 0)
            {
                result = true;

            }
            return result;
        }

        internal string ISCityCodeValid(string cityCode, string CountryCode, bool? IsCourierLoadType = false)
        {
            string _citycode = "";
            int CountryID = 0;

            if (CountryCode != null && CountryCode != "" && cityCode != null && cityCode != "")
            {
                //InfoTrack.NaqelAPI. DataDataContext dcData = new  DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                if (dcData.Countries.Where(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).Any())
                    CountryID = dcData.Countries.First(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).ID;

                if (CountryID > 0)
                {
                    if (IsCourierLoadType == true && dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.DivisionID == 5 && P.Code.Trim().ToLower().Contains(cityCode.Trim().ToLower())).Any())
                        _citycode = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.DivisionID == 5 && P.Code.Trim().ToLower().Contains(cityCode.Trim().ToLower())).Code;
                    else if (IsCourierLoadType == false && dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.Code.Trim().ToLower().Contains(cityCode.Trim().ToLower())).Any())
                        _citycode = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.Code.Trim().ToLower().Contains(cityCode.Trim().ToLower())).Code;
                    else
                    {
                        List<InfoTrack.BusinessLayer.DContext.ViwODAStationAPI> x = dcData.ViwODAStationAPIs.ToList();
                        if (dcData.ViwODAStationAPIs.Where(P => P.ISOCityCode.Trim().ToLower().Contains(cityCode.Trim().ToLower()) && P.CountryID == CountryID).Any())
                            _citycode = dcData.ViwODAStationAPIs.First(P => P.ISOCityCode.Trim().ToLower().Contains(cityCode.Trim().ToLower()) && P.CountryID == CountryID).ISOCityCode;
                    }
                }
            }

            return _citycode;
        }

        internal int GetStationIDByStationCode(string StationCode)
        {
            int _StationID = 0;


            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            //if (dcData.Cities.Where(P => P.StatusID == 1 && P.CountryID == CountryID && P.ISOCityCode.Trim().ToLower().Contains(ISOCityCode.Trim().ToLower())).Any())
            //    _cityID = dcData.Cities.First(P => P.StatusID == 1 && P.CountryID == CountryID && P.ISOCityCode.Trim().ToLower() == ISOCityCode.Trim().ToLower()).ID;

            _StationID = dcData.Stations.First(P => P.StatusID == 1 && P.Code.Trim().ToLower() == StationCode.Trim().ToLower()).ID;

            return _StationID;
        }


        //public bool hasSpecialChar(string input)
        //{
        //    bool res = false;
        //    var regexItem = new Regex("^[a-zA-Z0-9 ]*$");

        //    if (regexItem.IsMatch(input))
        //    {
        //        return res;
        //    }
        //    else res = true;

        //    return res;
        //}

        public string hasSpecialChar(string input)
        {
            var regexItem = new Regex("^[a-zA-Z0-9]*$");

            if (regexItem.IsMatch(input))
            {
                return input;
            }
            else
            {
                //input = regexItem.Replace(input, string.Empty);
                return Regex.Replace(input, "[^a-zA-Z0-9_.]+", " ", RegexOptions.Compiled);
                //return input;
            }
        }

        public int GetCountryIDByCountryCode(string CountryCode)
        {
            int _countryId;

            if (CountryCode != null && CountryCode != "")
            {
                // DataDataContext dc = new  DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                if (dc.Countries.Where(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).Count() > 0)
                    return _countryId = dc.Countries.FirstOrDefault(P => P.Code.Trim().ToLower() == CountryCode.Trim().ToLower() && P.StatusID == 1).ID;
            }

            return 0;
        }

        public int GetDistrictID(string district)
        {
            int result = 0;

            if (string.IsNullOrWhiteSpace(district))
                return result;

            InfoTrack.BusinessLayer.DContext.MastersDataContext dcc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcc.Districts.Where(P => P.Name.Trim().ToLower() == district.Trim().ToLower() && P.StationID >= 0 && P.StatusID == 1).Count() > 0)
                result = dcc.Districts.FirstOrDefault(P => P.Name.Trim().ToLower() == district.Trim().ToLower() && P.StationID >= 0 && P.StatusID == 1).ID;

            return result;
        }

        //public string GetToken()
        //{


        //    var login = new Login() { userNameOrEmailAddress = "naqelintegrationusr", password = "x3VCyxV3BHi5pGz" };//add in web config
        //    var payload = new Login
        //    {

        //        userNameOrEmailAddress = "naqelintegrationusr",
        //        password = "x3VCyxV3BHi5pGz"
        //    };

        //    var stringPayload = JsonConvert.SerializeObject(payload);
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //    // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
        //    var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
        //    HttpClient client = new HttpClient();
        //    client.DefaultRequestHeaders.Add("Abp.TenantId", "4");
        //    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
        //    client.BaseAddress = new Uri("https://app.parcelat.com/");
        //    //++

        //    //++
        //    string newToken = "";
        //    var response = client.PostAsync("api/TokenAuth/Authenticate", httpContent).Result;
        //    // response.EnsureSuccessStatusCode();
        //    if (response.IsSuccessStatusCode)
        //    {

        //        var result = response.Content.ReadAsStringAsync().Result;
        //        Token token = JsonConvert.DeserializeObject<Token>(result);
        //        newToken = token.result.accessToken;
        //    }
        //    else
        //    {
        //        // Response is Failed 
        //        newToken = "";
        //    }
        //    // return URI of the created resource.
        //    return newToken;
        //}

        public bool IsFalidCoordinatesRegEex(string Latitude, string longitude)//Done by Sara Almalki
        {
            string latitudePattern = @"^[0-9]{2,2}(?:\.[0-9]{1,14})$";
            //@"^([-+] ?\d{ 1,2}[.]\d +)$";
            //string latitudePattern = @"^[-+]?([1-8]?\d{1,2}(\.\d+)?|90(\.0+)?)$";

            string longitudePattern = @"^[0-9]{2,3}(?:\.[0-9]{1,14})$";
            //@"^[-+]?(180(\.0+)?|1[0-7]?\d(\.\d+)?|\d{1,2}(\.\d+)?)$";


            Match latitudematch = Regex.Match(Latitude, latitudePattern);
            Match longitudmatch = Regex.Match(longitude, longitudePattern);


            if (latitudematch.Success && longitudmatch.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Bullet Delivery Functions Done By Sara Almalki
        public bool CanBeDeliveredIn1Houre(double DrivingDuration)
        {
            bool CanBeDeliveredIn1Houre;
            // 20 miutes for Courier search time , Driving time by Courier to Store ,  Order Handover time.
            // 40 minutes for driving; 
            // Total Duration =60

            if (DrivingDuration < 40)
                CanBeDeliveredIn1Houre = true;
            else
                CanBeDeliveredIn1Houre = false;


            return CanBeDeliveredIn1Houre;

        }

        public bool CanBeDeliveredIn2Houre(double DrivingDuration)
        {
            // 40 miutes for Courier search time , Driving time by Courier to Store ,  Order Handover time.
            // 80 minutes for driving. 
            // Total Duration = 120.
            bool CanBeDeliveredIn2Houre;
            if (DrivingDuration < 80)
                CanBeDeliveredIn2Houre = true;
            else
                CanBeDeliveredIn2Houre = false;


            return CanBeDeliveredIn2Houre;

        }

        public bool CanBeDeliveredInSameDay(double DrivingDuration, DateTime Time)// ماهو شرط المقارنة 
        {
            // 75 miutes for Courier search time , Driving time by Courier to Store ,  Order Handover time.
            // Driving : **Depends on order time placing. Difference until 00:00 should be not less than 3 hours.

            bool CanBeDeliveredInSameDay;
            DateTime Midnight = DateTime.Today;
            int CheckDeliveryAbility = Time.Hour - Midnight.Hour;

            if (CheckDeliveryAbility > 3 && DrivingDuration < 1365) //1440 minutes -75 minutes
                CanBeDeliveredInSameDay = true;
            else
                CanBeDeliveredInSameDay = false;


            return CanBeDeliveredInSameDay;

        }
        #endregion Bullet Delivery Functions

        public string BirkenHSCode(string incorrectHsCode)
        {
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dcMaster.BirkenstockHSCodes.Where(hs => hs.Incorrect == incorrectHsCode && hs.statusid == 1).Count() > 0)
            {
                var CorrectHsCode = dcMaster.BirkenstockHSCodes.Where(hs => hs.Incorrect == incorrectHsCode && hs.statusid == 1).Select(hs => hs.correct);
                string CorrectHsCodeString = CorrectHsCode.FirstOrDefault();
                return CorrectHsCodeString;
            }
            else
                return incorrectHsCode;
        }


        public bool IsCurseName(string consigneeName , int clientID)
        {
           // bool isCurse = false;

            // Define the connection string (replace with your actual connection string)
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spWordFilteration", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters to the stored procedure
                    cmd.Parameters.AddWithValue("@ConsigneeName", consigneeName);
                    cmd.Parameters.AddWithValue("@ClientID", clientID);

                    // Add a return parameter to capture the return value from the stored procedure
                    SqlParameter returnParameter = new SqlParameter();
                    returnParameter.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnParameter);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Get the return value from the stored procedure
                    int result = (int)returnParameter.Value;

                    // Return true if the consignee name contains a curse word, otherwise false
                    return result == 1;
                }
            }



        }


        public bool isValidPcs( int PcsCount , List<ItemPieces> items)
        {
            bool Valid = true;

            foreach (var i in items)
            {
                if ( string.IsNullOrEmpty(i.PieceBarcode))
                {
                    Valid = false;
                    return Valid;

                }

            }
            if (items.Count != ( PcsCount-1 ) )
            {
                Valid = false;
                return Valid;

            }

            return Valid; 
        }
    }
}
