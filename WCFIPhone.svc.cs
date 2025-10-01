using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using InfoTrack.BusinessLayer.DContext;
using System.Xml;
using RestSharp.Serializers;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFIPhone" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WCFIPhone.svc or WCFIPhone.svc.cs at the Solution Explorer and start debugging.

    //[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class WCFIPhone : IWCFIPhone
    {
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        private InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc;

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.CountryResult> GetCountries()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.CountryResult> ResultList = new List<IPhoneDataTypes.CountryResult>();

            MastersDataContext dc = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<Country> countryList = dc.Countries.Where(P => P.StatusID != 3 && P.CountryCode != null).ToList();
            for (int i = 0; i < countryList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.CountryResult instance = new IPhoneDataTypes.CountryResult();
                instance.ID = countryList[i].ID;
                instance.Code = countryList[i].Code;
                instance.Name = countryList[i].Name;
                instance.FName = countryList[i].FName;
                instance.CountryCode = countryList[i].CountryCode;
                instance.FlagPath = countryList[i].FlagPath;

                ResultList.Add(instance);
            }

            return ResultList;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.StationResult> GetStations()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.StationResult> ResultList = new List<IPhoneDataTypes.StationResult>();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<InfoTrack.BusinessLayer.DContext.ViwStationByCountry> stationList = new List<ViwStationByCountry>();

            stationList = dcMaster.ViwStationByCountries.Where(P => P.StatusID != 3 &&
                                                     P.Code != "" &&
                                                     P.Name != "" &&
                                                     P.FName != "" &&
                                                     P.ID > 0).ToList();
            for (int i = 0; i < stationList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.StationResult instance = new IPhoneDataTypes.StationResult();
                instance.ID = stationList[i].ID;
                instance.Code = stationList[i].Code;
                instance.Name = stationList[i].Name;
                instance.FName = stationList[i].FName;
                instance.CountryID = stationList[i].CountryID.Value;
                ResultList.Add(instance);
            }

            return ResultList;
        }

        //public List<InfoTrack.NaqelAPI.IPhoneDataTypes.City> GetCities()
        //{
        //    List<InfoTrack.NaqelAPI.IPhoneDataTypes.City> ResultList = new List<IPhoneDataTypes.City>();

        //    MastersDataContext dc = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    List<City> cityList = new List<City>();

        //    cityList = dc.Cities.Where(P => P.StatusID != 3 &&
        //                                    P.Name != "" &&
        //                                    P.FName != "" &&
        //                                    P.ID > 0 &&
        //                                    (P.CountryID == 1 || P.CountryID == 3)).ToList();
        //    for (int i = 0; i < cityList.Count; i++)
        //    {
        //        InfoTrack.NaqelAPI.IPhoneDataTypes.City instance = new IPhoneDataTypes.City();
        //        instance.ID = cityList[i].ID;
        //        instance.Name = cityList[i].Name;
        //        instance.FName = cityList[i].FName;
        //        instance.CountryID = cityList[i].CountryID;
        //        ResultList.Add(instance);
        //    }

        //    return ResultList;
        //}

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintTypeResult> GetComplaintType()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintTypeResult> ResultList = new List<IPhoneDataTypes.ComplaintTypeResult>();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<ComplaintType> stationList = new List<ComplaintType>();

            stationList = dcMaster.ComplaintTypes.Where(P => P.StatusID != 3 &&
                                                       P.ShowInApp == true &&
                                                       P.ID > 0).ToList();
            for (int i = 0; i < stationList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintTypeResult instance = new IPhoneDataTypes.ComplaintTypeResult();
                instance.ID = stationList[i].ID;
                instance.Name = stationList[i].Name;
                instance.FName = stationList[i].FName;
                ResultList.Add(instance);
            }

            return ResultList;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.LoadTypeResult> GetLoadType()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.LoadTypeResult> ResultList = new List<IPhoneDataTypes.LoadTypeResult>();
            //App_Data.DocumentDataDataContext dc = new App_Data.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<LoadType> loadTypeList = dc.LoadTypes.Where(P => P.StatusID != 3 &&
                                                                  P.Name != "" &&
                                                                  P.FName != "" &&
                                                                  (P.ID == 35 ||
                                                                  P.ID == 36)).ToList();
            for (int i = 0; i < loadTypeList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.LoadTypeResult instance = new IPhoneDataTypes.LoadTypeResult();
                instance.ID = loadTypeList[i].ID;
                instance.Name = loadTypeList[i].Name;
                instance.FName = loadTypeList[i].FName;

                ResultList.Add(instance);
            }

            return ResultList;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.IPhoneTrackingResult> TraceByWaybillNo(IPhoneDataTypes.TrackingDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.TraceByWaybillNo, instance.AppTypeID, instance.AppVersion, instance.WaybillNo.ToString(), DateTime.Now);
            if (instance.WaybillNo.ToString().Length > 8)
                instance.WaybillNo = Convert.ToInt32(instance.WaybillNo.ToString().Remove(8));
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.IPhoneTrackingResult> ResultList = new List<IPhoneDataTypes.IPhoneTrackingResult>();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<InfoTrack.BusinessLayer.DContext.ViwTrackingForSmartPhone> ViwTrackingInstance = dcMaster.ViwTrackingForSmartPhones.Where(P => P.WaybillNo == Convert.ToInt32(instance.WaybillNo)).OrderByDescending(P => P.Date).ToList();
            for (int i = 0; i < ViwTrackingInstance.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.IPhoneTrackingResult IPhoneTrackingInstance = new IPhoneDataTypes.IPhoneTrackingResult();
                //instance.Date = ViwTrackingInstance[i].Date.ToUniversalTime();
                IPhoneTrackingInstance.Date = DateTime.SpecifyKind(ViwTrackingInstance[i].Date, DateTimeKind.Utc);
                IPhoneTrackingInstance.Activity = ViwTrackingInstance[i].Activity;
                IPhoneTrackingInstance.ActivityFName = ViwTrackingInstance[i].ActivityAr;
                IPhoneTrackingInstance.StationCode = ViwTrackingInstance[i].StationCode;
                IPhoneTrackingInstance.StationName = ViwTrackingInstance[i].StationName;
                IPhoneTrackingInstance.StationFName = ViwTrackingInstance[i].StationFName;
                IPhoneTrackingInstance.EventCode = ViwTrackingInstance[i].EventCode.Value;
                IPhoneTrackingInstance.TrackingTypeID = ViwTrackingInstance[i].TrackingTypeID;
                IPhoneTrackingInstance.ImageURL = ViwTrackingInstance[i].ImageURL;
                IPhoneTrackingInstance.WaybillNo = ViwTrackingInstance[i].WaybillNo;
                IPhoneTrackingInstance.OrgName = ViwTrackingInstance[i].OrgName;
                IPhoneTrackingInstance.OrgFName = ViwTrackingInstance[i].OrgFNam;
                IPhoneTrackingInstance.DestName = ViwTrackingInstance[i].DestName;
                IPhoneTrackingInstance.DestFName = ViwTrackingInstance[i].DestFName;
                if (ViwTrackingInstance[i].Weight.HasValue)
                    IPhoneTrackingInstance.Weight = ViwTrackingInstance[i].Weight.Value;
                else
                    IPhoneTrackingInstance.Weight = 0;
                if (ViwTrackingInstance[i].PicesCount.HasValue)
                    IPhoneTrackingInstance.PiecesCount = ViwTrackingInstance[i].PicesCount.Value;
                else
                    IPhoneTrackingInstance.PiecesCount = 0;
                if (ViwTrackingInstance[i].PickUpDate.HasValue)
                    IPhoneTrackingInstance.PickupDate = ViwTrackingInstance[i].PickUpDate.Value;
                else
                    IPhoneTrackingInstance.PickupDate = DateTime.Now;
                if (ViwTrackingInstance[i].IsDelivered == 1)
                    IPhoneTrackingInstance.IsDelivered = true;
                else
                    IPhoneTrackingInstance.IsDelivered = false;
                ResultList.Add(IPhoneTrackingInstance);
            }

            return ResultList;
        }

        public IPhoneDataTypes.RateResult GetRate(IPhoneDataTypes.GetRateDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.GetRate, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            IPhoneDataTypes.RateResult result = new IPhoneDataTypes.RateResult();
            InfoTrack.Common.GlobalVarCommon.GV.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrack.Common.Shipment ship = new Common.Shipment(GlobalVar.GV.GetInfoTrackConnection());
            result.Rate = ship.GetShipmentValue(0, instance.LoadType, DateTime.Now, instance.FromCity, instance.ToCity, instance.Weight, false, 0, 0, 2, false);

            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            int days = (int)dcDoc.GetExpectedDeliveryDays(instance.FromCity, instance.ToCity, instance.LoadType);

            if (instance.LanguageID == 1)
                result.TransitTime = "Your shipment will be delivered within " + days + " business days.";
            else
                result.TransitTime = "سيتم توصيل شحنتك في خلال " + days + " ايام عمل";

            return result;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.OurLocationsResult> GetLocationsList()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.OurLocationsResult> ResultList = new List<IPhoneDataTypes.OurLocationsResult>();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<InfoTrack.BusinessLayer.DContext.ViwOurLocation> ourLocationList = dcMaster.ViwOurLocations.ToList();
            for (int i = 0; i < ourLocationList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.OurLocationsResult instance = new IPhoneDataTypes.OurLocationsResult();
                instance.ID = ourLocationList[i].ID;
                instance.Name = ourLocationList[i].LocationName;
                instance.FName = ourLocationList[i].LocationFName;
                instance.CountryName = ourLocationList[i].CountryName;
                instance.CountryFName = ourLocationList[i].CountryFName;
                instance.CityName = ourLocationList[i].CityName;
                instance.CityFName = ourLocationList[i].CityFName;

                instance.Latitude = ourLocationList[i].Latitude;
                instance.Longitude = ourLocationList[i].Longitude;

                instance.NationalAddressName = ourLocationList[i].NatioanlAddressName;
                instance.NationalAddressFName = ourLocationList[i].NatioanlAddressFName;
                instance.OpeningTime = ourLocationList[i].OpeningTime;
                instance.ClosingTime = ourLocationList[i].ClosingTime;
                if (ourLocationList[i].ID == 1 ||
                    ourLocationList[i].ID == 2 ||
                    ourLocationList[i].ID == 17)
                    instance.IsRetailOutlet = true;
                else
                    instance.IsRetailOutlet = false;

                ResultList.Add(instance);
            }

            return ResultList;
        }

        //public App_Data.OurLocation GetLocationDetails(string LocationID)
        //{
        //    App_Data.OurLocation instance = new OurLocation();

        //    if (Convert.ToInt32(LocationID) > 0)
        //    {
        //        App_Data.DataDataContext dc = new DataDataContext();
        //        dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //        if (dc.OurLocations.Where(P => P.ID == Convert.ToInt32(LocationID)).Count() > 0)
        //            instance = dc.OurLocations.First(P => P.ID == Convert.ToInt32(LocationID));
        //    }

        //    return instance;
        //}

        //public IPhoneDataTypes.FleetDetails GetFleetDetails(string FleetNo)
        //{
        //    com.naqelexpress.livetrack.GPSService service = new com.naqelexpress.livetrack.GPSService();
        //    string result = service.GetFleetDetails(FleetNo);

        //    IPhoneDataTypes.FleetDetails instance = new IPhoneDataTypes.FleetDetails();

        //    JavaScriptSerializer x = new JavaScriptSerializer();
        //    //var m = x.DeserializeObject(result);

        //    var m = x.Deserialize<IPhoneDataTypes.FleetDetails[]>(result);

        //    IEnumerable enumerable = m as IEnumerable;
        //    if (enumerable != null)
        //    {
        //        foreach (object element in enumerable)
        //        {
        //            instance = element as IPhoneDataTypes.FleetDetails;

        //            //byte[] bytes = Convert.FromBase64String(instance.Photo);

        //            //System.Drawing.Image image;
        //            //using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
        //            //{
        //            //    image = System.Drawing.Image.FromStream(ms);
        //            //    instance.Photo1 = image;
        //            //}

        //            return instance;
        //        }
        //    }


        //    return instance;
        //}

        public InfoTrack.NaqelAPI.IPhoneDataTypes.AppMobileVerificationResult SignUp(IPhoneDataTypes.SignUpDetailsRequest instance)
        {
            InfoTrack.NaqelAPI.IPhoneDataTypes.AppMobileVerificationResult Results = new IPhoneDataTypes.AppMobileVerificationResult();
            Results.Date = DateTime.Now;
            instance.Name = System.Web.HttpUtility.UrlDecode(instance.Name);
            instance.MobileNo = System.Web.HttpUtility.UrlDecode(instance.MobileNo);
            instance.Password = System.Web.HttpUtility.UrlDecode(instance.Password);
            //instance.LanguageID = System.Web.HttpUtility.UrlDecode(instance.LanguageID.ToString());

            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (dc.AppClients.Where(P => P.MobileNo == instance.MobileNo).Count() > 0)
            {
                Results.HasError = true;
                if (instance.LanguageID == 1)
                    Results.ErrorMessage = "There is a user already signed up with this Mobile No, please change the Mobile No or use forgot password option to retrieve the password.";
                else
                    Results.ErrorMessage = "هناك مستخدم أخر إستخدم رقم الجوال المدخل، يرجى تغيير رقم الجوال أو إستخدام خيار إرسال كلمة المرور";
            }
            else
            {
                InfoTrack.BusinessLayer.DContext.AppClient appClientInstance = new AppClient();
                appClientInstance.Date = DateTime.Now;
                appClientInstance.Name = instance.Name;
                appClientInstance.MobileNo = instance.MobileNo;
                appClientInstance.Password = instance.Password;
                appClientInstance.GetNotifications = true;
                appClientInstance.LanguageID = Convert.ToInt32(instance.LanguageID);
                dc.AppClients.InsertOnSubmit(appClientInstance);
                dc.SubmitChanges();

                InfoTrack.BusinessLayer.DContext.AppMobileVerification verificationInstance = new AppMobileVerification();
                verificationInstance.AppClientID = appClientInstance.ID;
                verificationInstance.Date = DateTime.Now;
                Random rnd = new Random();
                int code = rnd.Next(1000, 9999);
                verificationInstance.MobileNoVerificationCode = code;
                verificationInstance.IsUsed = false;

                dc.AppMobileVerifications.InsertOnSubmit(verificationInstance);
                dc.SubmitChanges();

                Results.AppClientID = verificationInstance.AppClientID;
                Results.Date = verificationInstance.Date;
                Results.MobileNoVerificationCode = verificationInstance.MobileNoVerificationCode;

                InfoTrack.Common.Security sec = new Common.Security();
                byte[] ID = sec.Encrypt("15380");
                XMLGeneral sms = new XMLGeneral();
                sms.SendSMSbyOTS(ID, instance.MobileNo, "Your Verification Code is : " + verificationInstance.MobileNoVerificationCode, EnumList.PurposeList.NoAnswer, "");

                if (instance.DeviceToken != "")
                {
                    InfoTrack.BusinessLayer.DContext.AppDeviceToken appDeviceToken = new AppDeviceToken();
                    appDeviceToken.AppClientID = verificationInstance.AppClientID;
                    appDeviceToken.AppTypeID = instance.AppTypeID;
                    appDeviceToken.AppVersion = instance.AppVersion;
                    appDeviceToken.StatusID = 1;
                    appDeviceToken.DeviceToken = instance.DeviceToken;
                    dc.AppDeviceTokens.InsertOnSubmit(appDeviceToken);
                    dc.SubmitChanges();
                }
            }

            return Results;
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.DefaultDetailsResult ResendPassword(IPhoneDataTypes.MobileVerificationRequest instance)
        {
            InfoTrack.NaqelAPI.IPhoneDataTypes.DefaultDetailsResult result = new IPhoneDataTypes.DefaultDetailsResult();
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (dc.AppClients.Where(P => P.MobileNo == instance.MobileNo).Count() > 0)
            {
                AppClient AppClientInstance = dc.AppClients.First(P => P.MobileNo == instance.MobileNo);

                InfoTrack.Common.Security sec = new Common.Security();
                byte[] ID = sec.Encrypt("15380");
                XMLGeneral sms = new XMLGeneral();
                sms.SendSMSbyOTS(ID, instance.MobileNo, "Your Password is : " + AppClientInstance.Password, EnumList.PurposeList.NoAnswer, "");
            }
            else
            {
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Mobile No is wrong, please check the Moile No";
                else
                    result.ErrorMessage = "رقم الجوال المدخل خاطئ يرجى التأكد من رقم الجوال";
            }
            return result;
        }

        public void VerifiedMobileNo(InfoTrack.NaqelAPI.IPhoneDataTypes.VerifiedMobleNoRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.VerifiedMobileNo, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dc.AppClients.Where(P => P.ID == instance.AppClientID && P.MobileNo == instance.MobileNo).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.AppClient AppClientInstance = dc.AppClients.First(P => P.ID == instance.AppClientID && P.MobileNo == instance.MobileNo);
                AppClientInstance.IsMobileVerified = true;
                dc.SubmitChanges();
            }
        }

        private void WritetoXML(Object myObject, AppRequestTypeEnum requestType)
        {
            try
            {
                XMLShippingService service = new XMLShippingService();

                string fileName = service.GetPathFileName();
                fileName += "\\ErrorData\\" + DateTime.Now.ToFileTimeUtc() + ".xml";
                //Server.MapPath(".") + "\\ErrorData\\" + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + reference + "_" + DateTime.Now.ToFileTimeUtc() + ".xml";
                //System.IO.File.Create(fileName);

                FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
                StreamWriter str = new StreamWriter(fs);
                str.Close();
                fs.Close();

                XmlDocument xmlDoc = new XmlDocument();
                XmlSerializer xmlSerializer = new XmlSerializer();
                using (MemoryStream xmlStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(myObject);
                    xmlStream.Position = 0;
                    xmlDoc.Load(xmlStream);
                    xmlDoc.Save(fileName);
                }

                //fileName = "";
                ////        Server.MapPath(".") + "\\ErrorData\\" + _ClientInfo.ClientID.ToString() + "_" + methodType.ToString() + "_" + reference + "_" + DateTime.Now.ToFileTimeUtc() + "Error.xml";
                //xmlSerializer = new XmlSerializer();
                //using (MemoryStream xmlStream = new MemoryStream())
                //{
                //    xmlSerializer.Serialize(xmlStream, Result);
                //    xmlStream.Position = 0;
                //    xmlDoc.Load(xmlStream);
                //    xmlDoc.Save(fileName);
                //}
            }
            catch (Exception ex)
            {
                GlobalVar.GV.InsertError(ex.Message, "15380");
            }
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.AppClientDetailsResult CheckPassword(IPhoneDataTypes.MobileVerificationRequest instance)
        {
            InfoTrack.NaqelAPI.IPhoneDataTypes.AppClientDetailsResult result = new IPhoneDataTypes.AppClientDetailsResult();
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (dc.AppClients.Where(P => P.MobileNo == instance.MobileNo).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.AppClient AppClientInstance = dc.AppClients.First(P => P.MobileNo == instance.MobileNo);
                result.ID = AppClientInstance.ID;
                result.UserName = AppClientInstance.Name;
                result.LanguageID = AppClientInstance.LanguageID;
                result.Password = AppClientInstance.Password;
                result.Date = AppClientInstance.Date;
                result.isMobileVerified = AppClientInstance.IsMobileVerified;
                //if (AppClientInstance.ClientID.HasValue)
                //    result.AppClientID = AppClientInstance.ClientID.Value;
                //else
                //    result.AppClientID = 0;
                if (AppClientInstance.EMail != null)
                    result.EMail = AppClientInstance.EMail;
                RegisterRequest(AppClientInstance.ID, AppRequestTypeEnum.CheckPassword, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            }
            else
            {
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Please check the Mobile No";
                else
                    result.ErrorMessage = "يرجى التأكد من رقم الجوال";
            }
            return result;
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult SyncAccount(IPhoneDataTypes.AccountDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.SyncAccount, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult syncResult = new IPhoneDataTypes.SyncResult();
            syncResult.HasError = false;
            syncResult.ID = 0;

            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext();
            dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (dc.AppClients.Where(P => P.ID == Convert.ToInt32(instance.AppClientID)).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.AppClient AppClientInstance = dc.AppClients.First(P => P.ID == Convert.ToInt32(instance.AppClientID));
                AppClientInstance.Password = instance.Password;
                AppClientInstance.Name = instance.Name;
                if (AppClientInstance.EMail != null)
                    AppClientInstance.EMail = instance.EMail;
                AppClientInstance.LanguageID = instance.LanguageID;
                AppClientInstance.GetNotifications = instance.NeedNotification;
                dc.SubmitChanges();
            }
            return syncResult;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> GetWaybillListByMobileNo(IPhoneDataTypes.TrackingDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.GetWaybillListByMobileNo, instance.AppTypeID, instance.AppVersion, instance.MobileNo, DateTime.Now);
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> result = new List<IPhoneDataTypes.WaybillListResult>();
            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<ViwWaybillForSmartPhone> waybillList = dcDoc.ViwWaybillForSmartPhones.Where(P => P.ConsigneeMobileNo.EndsWith(instance.MobileNo) ||
                                                                                  P.ConsigneeMobileNo.EndsWith(instance.MobileNo.Remove(0, 3))).OrderByDescending(c => c.PickUpDate).Take(10).ToList();

            for (int i = 0; i < waybillList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult WaybillListInstances = new IPhoneDataTypes.WaybillListResult();
                WaybillListInstances.Date = waybillList[i].PickUpDate;
                WaybillListInstances.OriginName = waybillList[i].OriginName;
                WaybillListInstances.OriginFName = waybillList[i].OriginFName;
                WaybillListInstances.DestinationName = waybillList[i].DestinationName;
                WaybillListInstances.DestinationFName = waybillList[i].DestinationFName;
                WaybillListInstances.WaybillNo = waybillList[i].WayBillNo;
                result.Add(WaybillListInstances);
            }

            return result;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> GetWaybillListByReference(IPhoneDataTypes.TrackingDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.GetWaybillListByReference, instance.AppTypeID, instance.AppVersion, instance.Reference, DateTime.Now);
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> result = new List<IPhoneDataTypes.WaybillListResult>();
            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<ViwWaybill> waybillList = dcDoc.ViwWaybills.Where(P => P.RefNo.ToLower().Contains(instance.Reference.ToLower())).OrderByDescending(c => c.PickUpDate).Take(5).ToList();

            for (int i = 0; i < waybillList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult WaybillListInstances = new IPhoneDataTypes.WaybillListResult();
                WaybillListInstances.Date = waybillList[i].PickUpDate;
                WaybillListInstances.OriginName = waybillList[i].OriginName;
                WaybillListInstances.OriginFName = waybillList[i].OriginFName;
                WaybillListInstances.DestinationName = waybillList[i].DestinationName;
                WaybillListInstances.DestinationFName = waybillList[i].DestinationFName;
                WaybillListInstances.WaybillNo = waybillList[i].WayBillNo;
                result.Add(WaybillListInstances);
            }

            return result;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.AdvResult> GetAdvs()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.AdvResult> result = new List<IPhoneDataTypes.AdvResult>();

            //App_Data.DataDataContext dc = new App_Data.DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<AppAdv> advList = dc.AppAdvs.Where(P => P.StatusID != 3).ToList();
            for (int i = 0; i < advList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.AdvResult instance = new IPhoneDataTypes.AdvResult();
                instance.ID = advList[i].ID;
                instance.ImageURL = advList[i].ImageUrl;
                instance.Link = advList[i].Link;
                instance.TypeID = advList[i].AdvTypeID;
                result.Add(instance);
            }

            return result;
        }

        public List<InfoTrack.NaqelAPI.IPhoneDataTypes.NewsResult> GetNews()
        {
            List<InfoTrack.NaqelAPI.IPhoneDataTypes.NewsResult> result = new List<IPhoneDataTypes.NewsResult>();

            InfoTrack.BusinessLayer.DContext.AppDataDataContext dc = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<AppNew> newsList = dc.AppNews.Where(P => P.StatusID != 3).OrderByDescending(c => c.Date).Take(20).ToList();
            for (int i = 0; i < newsList.Count; i++)
            {
                InfoTrack.NaqelAPI.IPhoneDataTypes.NewsResult instance = new IPhoneDataTypes.NewsResult();
                instance.ID = newsList[i].ID;
                instance.Name = newsList[i].Name;
                instance.FName = newsList[i].FName;
                instance.Date = newsList[i].Date;
                instance.ImageURL = newsList[i].ImageUrl;
                instance.NewsDetailsEn = newsList[i].NewsDetailsEn;
                instance.NewsDetailsAr = newsList[i].NewsDetailsAr;
                result.Add(instance);
            }

            return result;
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult AddComplaint(InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintDataRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.AddComplaint, instance.AppTypeID, instance.AppVersion, instance.WaybillNo.ToString(), DateTime.Now);
            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult result = new IPhoneDataTypes.SyncResult();
            Complaint ComplaintInstance = new Complaint();

            if (dcDoc.Waybills.Where(P => P.WayBillNo == instance.WaybillNo && P.IsCancelled == false).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.Waybill waybillInstance = dcDoc.Waybills.First(P => P.WayBillNo == instance.WaybillNo && P.IsCancelled == false);

                ComplaintInstance.Date = DateTime.Now;
                ComplaintInstance.ProductTypeID = waybillInstance.ProductTypeID.HasValue ? waybillInstance.ProductTypeID.Value : 7;
                ComplaintInstance.ComplaintDetails = instance.ComplaintDetails;
                ComplaintInstance.StationID = waybillInstance.DestinationStationID;
                ComplaintInstance.ComplaintTypeID = instance.ComplaintTypeID;
                ComplaintInstance.ComplaintStatusID = 1;
                ComplaintInstance.ComplaintRefTypeID = 1;
                ComplaintInstance.RefNo = instance.WaybillNo.ToString();

                ComplaintInstance.AssignedTo = -1;
                ComplaintInstance.StatusID = 1;
                ComplaintInstance.ClientID = waybillInstance.ClientID;
                ComplaintInstance.ComplaintSeverityID = 1;
                ComplaintInstance.AppClientID = instance.AppClientID;
                dcDoc.Complaints.InsertOnSubmit(ComplaintInstance);
                dcDoc.SubmitChanges();
                result.ID = ComplaintInstance.ID;
            }
            else
            {
                result.ID = 0;
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Waybill No is Wrong";
                else
                    result.ErrorMessage = "رقم البوليصة خاطئ";
            }

            return result;
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.CreatingAccountResult CreateClientAddress(InfoTrack.NaqelAPI.IPhoneDataTypes.ClientDetailsRequest instance)
        {
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.CreateClientAddress, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            InfoTrack.NaqelAPI.IPhoneDataTypes.CreatingAccountResult result = new IPhoneDataTypes.CreatingAccountResult();
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            int cityid = 501;
            if (instance.CityID <= 0)
            {
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Please select the city.";
                else
                    result.ErrorMessage = "يرجى إختيار المدينة";
                return result;
            }
            else
            {
                if (dcMaster.Stations.Where(P => P.ID == instance.CityID).Count() > 0)
                {
                    cityid = dcMaster.Stations.First(P => P.ID == instance.CityID).CityID.Value;
                }
                else
                {
                    result.HasError = true;
                    if (instance.LanguageID == 1)
                        result.ErrorMessage = "Please select the city";
                    else
                        result.ErrorMessage = "يرجى إختيار المدينة";
                    return result;
                }
            }

            if (instance.AppClientID <= 0)
            {
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Something went wrong Error Code 404";
                else
                    result.ErrorMessage = "Something went wrong Error Code 404";
                return result;
            }

            InfoTrack.BusinessLayer.DContext.Client clientInstance = new BusinessLayer.DContext.Client();
            InfoTrack.BusinessLayer.DContext.ClientAddress clientAddressInstance = new BusinessLayer.DContext.ClientAddress();
            InfoTrack.BusinessLayer.DContext.ClientContact clientContactInstance = new BusinessLayer.DContext.ClientContact();
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dcAppData = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.AppClient appClient = dcAppData.AppClients.First(P => P.ID == instance.AppClientID);

            if (appClient.ClientID.HasValue && appClient.ClientID.Value > 0)
                clientInstance = dcMaster.Clients.First(P => P.ID == appClient.ClientID.Value);
            else
            {
                clientInstance.TitleID = 1;
                clientInstance.Name = appClient.Name;
                clientInstance.FName = appClient.Name;
                if (appClient.EMail != null)
                    clientInstance.Email = instance.EMail;
                else
                    clientInstance.Email = "";
                clientInstance.Date = DateTime.Now;
                clientInstance.IsSupplier = false;
                clientInstance.IsShipper = false;
                clientInstance.IsCashCustomer = true;
                clientInstance.IsCorporate = false;
                clientInstance.StatusID = 1;
                clientInstance.ISNeedFraction = false;
                clientInstance.UserID = 1;
                clientInstance.IsSentInvoice = false;
                clientInstance.HalfMonthInvoicing = false;
                clientInstance.IsOnlineAccess = false;
                clientInstance.ID = GetNextCashClientID();
                dcMaster.Clients.InsertOnSubmit(clientInstance);
                dcMaster.SubmitChanges();

                appClient = dcAppData.AppClients.First(P => P.ID == instance.AppClientID);
                appClient.ClientID = clientInstance.ID;
                dcAppData.SubmitChanges();
            }

            clientAddressInstance.PhoneNumber = instance.MobileNo;
            clientAddressInstance.FirstAddress = instance.AddressFirstLine;
            clientAddressInstance.SecondAddress = instance.AddressSecondLine;
            clientAddressInstance.ForPickUp = true;
            clientAddressInstance.ForInvoice = false;
            clientAddressInstance.ForSales = false;
            clientAddressInstance.ForDelivery = true;
            clientAddressInstance.ForMarketing = false;
            clientAddressInstance.ForOthers = false;
            clientAddressInstance.ClientID = clientInstance.ID;

            clientAddressInstance.CityID = cityid;
            clientAddressInstance.RouteID = 0;
            clientAddressInstance.StatusID = 1;

            dcMaster.ClientAddresses.InsertOnSubmit(clientAddressInstance);
            dcMaster.SubmitChanges();

            clientContactInstance.Name = appClient.Name;
            clientContactInstance.FName = appClient.Name;
            //if (instance.EMail != null)
            //    clientContactInstance.Email = instance.EMail;
            //else
            clientContactInstance.Email = "";
            clientContactInstance.PhoneNumber = appClient.MobileNo;
            clientContactInstance.ForPickUp = true;
            clientContactInstance.ForInvoice = false;
            clientContactInstance.ForSales = false;
            clientContactInstance.ForDelivery = true;
            clientContactInstance.ForMarketing = false;
            clientContactInstance.ForOthers = false;
            clientContactInstance.ClientAddressID = clientAddressInstance.ID;
            clientContactInstance.StatusID = 1;
            clientContactInstance.Mobile = instance.MobileNo;

            dcMaster.ClientContacts.InsertOnSubmit(clientContactInstance);
            dcMaster.SubmitChanges();

            result.ClientID = clientInstance.ID;
            result.ClientAddressID = clientAddressInstance.ID;
            result.ClientContactID = clientContactInstance.ID;
            result.HasError = false;
            result.ErrorMessage = "";

            return result;
        }

        public InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult CreateBooking(InfoTrack.NaqelAPI.IPhoneDataTypes.BookingDetailsRequest instance)
        {            
            InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult result = new IPhoneDataTypes.SyncResult();
            InfoTrack.BusinessLayer.DContext.Booking bookingInstance = new BusinessLayer.DContext.Booking();
            InfoTrack.BusinessLayer.DContext.AppClient appClientInstance = new AppClient();
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dcAppData = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (instance.AppClientID <= 0)
            {
                result.HasError = true;
                result.ErrorMessage = instance.LanguageID == 1 ? "Something went wrong Error Code 405" : "Something went wrong Error Code 405";
                return result;
            }
            else
            {
                if (dcAppData.AppClients.Where(P => P.ID == instance.AppClientID).Count() > 0)
                {
                    appClientInstance = dcAppData.AppClients.First(P => P.ID == instance.AppClientID);
                }
                else
                {
                    result.HasError = true;
                    result.ErrorMessage = instance.LanguageID == 1 ? "Something went wrong Error Code 406" : "Something went wrong Error Code 406";
                    return result;
                }
            }

            bookingInstance.ClientID = instance.ClientID;
            bookingInstance.ClientAddressID = instance.ClientAddressID;
            bookingInstance.ClientContactID = instance.ClientContactID;
            bookingInstance.BillingTypeID = 2;
            bookingInstance.BookingDate = DateTime.Now;
            bookingInstance.PickUpReqDT = instance.PickupDate;
            bookingInstance.OfficeUpTo = instance.ClosingTime;
            bookingInstance.PicesCount = instance.PiecesCount;
            bookingInstance.Weight = instance.Weight;
            bookingInstance.Width = 1;
            bookingInstance.Length = 1;
            bookingInstance.Height = 1;
            bookingInstance.SpecialInstruction = instance.PickupNotes;
            bookingInstance.OriginStationID = GlobalVar.GV.GetStationByCity(instance.FromCity);
            bookingInstance.DestinationStationID = GlobalVar.GV.GetStationByCity(instance.ToCity);
            bookingInstance.IsEmergency = false;
            bookingInstance.IsInsurance = false;
            bookingInstance.InsuranceValue = 0;
            bookingInstance.InsuranceCost = 0;
            bookingInstance.EmployID = -1;
            //bookingInstance.PickedUpEnteredBy = -1;
            //bookingInstance.OfficeUpTo = instance.PickupDate;
            bookingInstance.OfficeUpTo = DateTime.Now;
            bookingInstance.IsPickedUp = false;
            bookingInstance.IsCanceled = false;
            bookingInstance.CourierInformed = false;
            bookingInstance.CurrentStatusID = 1;
            bookingInstance.IsSpecialBooking = false;
            bookingInstance.IsMissPickUp = false;
            bookingInstance.IsSync = false;

            bookingInstance.ContactNumber = appClientInstance.MobileNo;
            bookingInstance.ContactPerson = appClientInstance.Name;
            bookingInstance.LoadTypeID = instance.LoadType;
            bookingInstance.ProductTypeID = 6;
            bookingInstance.IsDGR = false;
            bookingInstance.IsMultiPickUpLocation = false;
            bookingInstance.IsScheduleBooking = false;
            bookingInstance.IsShippingAPI = true;
            bookingInstance.PickedUpEnteredBy = 1;
            bookingInstance.EmployID = 1;
            bookingInstance.RefNo = GetBookingRefNo("", instance.FromCity, DateTime.Now,0);
            DocumentDataDataContext dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcDoc.Bookings.InsertOnSubmit(bookingInstance);
            dcDoc.SubmitChanges();
            RegisterRequest(instance.AppClientID, AppRequestTypeEnum.CreateBooking, instance.AppTypeID, instance.AppVersion, "", DateTime.Now);
            result.ID = bookingInstance.ID;
            result.RefNo = bookingInstance.RefNo;
            result.HasError = false;
            result.ErrorMessage = "";

            return result;
        }

        private string GetBookingRefNo(string max, int stationID, DateTime currentdt, int MaxID)
        {
            string refNo = "";
            
            if (max == "")
            {
                DocumentDataDataContext dcDocu = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                int x = dcDocu.Bookings.OrderByDescending(P => P.ID).FirstOrDefault().ID;
                max = dcDocu.Bookings.First(P => P.ID == x).RefNo;
            }

            if (max.Length > 0)
            {
                string b = max.Remove(0, 6 + stationID.ToString().Length);
                refNo = Convert.ToString(Convert.ToInt32(b) + 1);
                refNo = max.Remove(6) + stationID.ToString().PadLeft(2, '0') + refNo.PadLeft(4, '0');
            }
            else
                refNo = currentdt.Year.ToString().Remove(0, 2) + currentdt.Month.ToString().PadLeft(2, '0') + currentdt.Day.ToString().PadLeft(2, '0') + stationID.ToString().PadLeft(2, '0') + "0001";

            if (GetBookingRefNoCount(refNo) > 0)
                refNo = GetBookingRefNo(refNo, stationID, currentdt,MaxID);

            return refNo;
        }

        private int GetBookingRefNoCount(string RefNo)
        {
            int count = 0;

            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDocu = new InfoTrack.BusinessLayer.DContext.DocumentDataDataContext();
            count = dcDocu.Bookings.Where(P => P.RefNo == RefNo).Count();

            return count;
        }

        private enum AppRequestTypeEnum : int
        {
            TraceByWaybillNo = 1,
            GetRate = 2,
            VerifiedMobileNo = 3,
            CheckPassword = 4,
            SyncAccount = 5,
            GetWaybillListByMobileNo = 6,
            GetWaybillListByReference = 7,
            AddComplaint = 8,
            CreateClientAddress = 9,
            CreateBooking = 10
        }

        private void RegisterRequest(int appClientID, AppRequestTypeEnum appRequestType, int appTypeID, string version, string reference, DateTime date)
        {
            InfoTrack.BusinessLayer.DContext.AppDataDataContext dcData = new AppDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.AppRequestHistory instance = new AppRequestHistory();
            try
            {
                instance.AppClientID = appClientID;
                instance.AppRequestTypeID = Convert.ToInt32(appRequestType);
                instance.AppTypeID = appTypeID;
                instance.Date = date;
                instance.Version = version;
                instance.Reference = reference;

                dcData.AppRequestHistories.InsertOnSubmit(instance);
                dcData.SubmitChanges();
            }
            catch (Exception ex)
            {
                WritetoXML(instance, appRequestType);
            }
        }

        private int GetNextCashClientID()
        {
            int result = 0;
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcM = new InfoTrack.BusinessLayer.DContext.MastersDataContext();
            var query = from p in dcM.Clients
                        where p.IsCorporate == false
                        select p.ID;

            result = query.Max();
            if (dcM.Clients.Where(P => P.ID == result + 1).Count() > 0)
                GetNextCashClientID();
            return result + 1;
        }

        //public CurrentLocation GetCurrentLOcationByWaybill(GetCurrentLOcationByWaybillRequest instance)
        //{
        //    ServiceReference1.CurrentLocation result = new ServiceReference1.CurrentLocation();

        //    BasicHttpBinding binding = new BasicHttpBinding();
        //    EndpointAddress address = new EndpointAddress("http://livetrack.naqelexpress.com/IOSAPI/GPSSmartPhoneService.svc");
        //    ServiceReference1.GPSSmartPhoneServiceClient x = new ServiceReference1.GPSSmartPhoneServiceClient(binding, address);

        //    using (System.ServiceModel.ServiceHost host = new ServiceHost(typeof(ServiceReference1.GPSSmartPhoneServiceClient)))
        //    {
        //        ServiceMetadataBehavior serviceMetadataBehavior = new ServiceMetadataBehavior()
        //        {
        //            HttpGetEnabled = true,
        //        };

        //        host.Description.Behaviors.Add(serviceMetadataBehavior);
        //        host.AddServiceEndpoint(typeof(ServiceReference1.IGPSSmartPhoneService), new WebHttpBinding(), "GPSSmartPhoneServiceClient");
        //        host.Open();

        //    }
        //        x.Open();
        //    result = x.GetCurrentLOcationByWaybillforAndroid(instance.WaybillNo);

        //    return result;
        //}

        //public IPhoneDataTypes.GetCurrentLOcationByWaybillResult GetCurrentLOcationByWaybill(IPhoneDataTypes.GetCurrentLOcationByWaybillRequest instance)
        //{
        //    IPhoneDataTypes.GetCurrentLOcationByWaybillResult result = new IPhoneDataTypes.GetCurrentLOcationByWaybillResult();
        //    //com.naqelexpress.livetrack.GPSService gPSService = new com.naqelexpress.livetrack.GPSService();
        //    string resultString = GetCor(instance);
        //    string[] x = resultString.Split(',');

        //    if (x.Length > 1)
        //    {
        //        if (x[1].Length > 3)
        //        {
        //            result.Latitude = x[1];
        //            result.Longitude = x[2].ToString().Remove(x[2].Length - 3);
        //        }
        //    }

        //    return result;
        //}

        //private string GetCor(IPhoneDataTypes.GetCurrentLOcationByWaybillRequest instance)
        //{
        //    com.naqelexpress.livetrack.GPSService gPSService = new com.naqelexpress.livetrack.GPSService();
        //    string resultString = gPSService.GetCurrentLOcationByWaybill(instance.WaybillNo.ToString());
        //    return resultString;

        //}

        //CurrentLocation GetCurrentLOcationByWaybill(string WaybillNo)
        //{
        //    CurrentLocation result = new CurrentLocation();

        //    ServiceReference1.GPSSmartPhoneServiceClient x = new ServiceReference1.GPSSmartPhoneServiceClient();
        //    result = x.GetCurrentLOcationByWaybillforAndroid(WaybillNo);

        //    return result;
        //}

        //ServiceReference1.CurrentLocation GetCurrentLOcationByWaybill(string WaybillNo)
        //{
        //    ServiceReference1.CurrentLocation result = new ServiceReference1.CurrentLocation();

        //    ServiceReference1.GPSSmartPhoneServiceClient x = new ServiceReference1.GPSSmartPhoneServiceClient();
        //    result = x.GetCurrentLOcationByWaybillforAndroid(WaybillNo);

        //    return result;
        //}
    }
}