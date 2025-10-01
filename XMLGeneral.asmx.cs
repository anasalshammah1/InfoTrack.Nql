using DHL.SchemaGenerated;

//using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

using System.Web;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;

using System.Xml;
using System.Xml.Serialization;
using InfoTrack.BusinessLayer.DContext;
using System.Text;

namespace InfoTrack.NaqelAPI
{
    /// <summary>
    /// Summary description for XMLGeneral
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line.
    [System.Web.Script.Services.ScriptService]
    public class XMLGeneral : System.Web.Services.WebService
    {
        private TransferFile transferFile = new TransferFile();
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        private InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc;
        private InfoTrack.BusinessLayer.DContext.HHDDataContext dcHHD;

        [WebMethod]
        public byte[] DownloadFile(byte[] ID, string Path, string fileName)
        {
            byte[] result = new byte[1];
            if (!GlobalVar.GV.IsSecure(ID)) return result;

            string LastVersionPath = Server.MapPath(".");
            string[] x = LastVersionPath.Split('\\');

            LastVersionPath = LastVersionPath.Remove(LastVersionPath.IndexOf(x[x.Length - 1]));
            LastVersionPath += Path;
            return transferFile.ReadBinaryFile(LastVersionPath, fileName);
        }

        [WebMethod]
        public byte[] DownloadFileWithOldSize(byte[] ID, string Path, string fileName, Int64 OldFileSize)
        {
            byte[] result = new byte[1];
            if (!GlobalVar.GV.IsSecure(ID)) return result;

            string LastVersionPath = Server.MapPath(".");
            string[] x = LastVersionPath.Split('\\');

            LastVersionPath = LastVersionPath.Remove(LastVersionPath.IndexOf(x[x.Length - 1]));
            LastVersionPath += Path;

            System.IO.FileInfo fil = new System.IO.FileInfo(LastVersionPath + fileName);
            if (fil.Length != OldFileSize)
            {
                byte[] NewResult = transferFile.ReadBinaryFile(LastVersionPath, fileName);
                if (NewResult.Length != OldFileSize)
                    return NewResult;
                else
                    return result;
            }
            else
                return result;
        }

        [WebMethod]
        public string GetLastVersion(byte[] ID, int VersionTypeID)
        {
            string result = "";
            if (!GlobalVar.GV.IsSecure(ID)) return result;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcMaster.AppVersions.Where(P => P.VersionTypeID == VersionTypeID).Count() > 0)
            {
                List<AppVersion> app = dcMaster.AppVersions.Where(P => P.VersionTypeID == VersionTypeID).ToList();
                result = app.Last().Name.ToString();
            }

            return result;
        }

        //[WebMethod]
        //public string GetLastVersion(byte[] ID, int VersionTypeID)
        //{
        //    string result = "";
        //    if (!GlobalVar.GV.IsSecure(ID)) return result;

        //    App_Data.DataDataContext dc = new App_Data.DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    if (dc.AppVersions.Where(P => P.VersionTypeID == VersionTypeID).Count() > 0)
        //    {
        //        List<App_Data.AppVersion> app = dc.AppVersions.Where(P => P.VersionTypeID == VersionTypeID).ToList();
        //        result = app.Last().Name.ToString();
        //    }

        //    return result;
        //}

        //[WebMethod]
        //public void GetClientData(byte[] ID)
        //{
        //    if (!GlobalVar.GV.IsSecure(ID)) return;
        //    App_Data.GeneralDS.ViwClientsDataTable dtViwClient = new App_Data.GeneralDS.ViwClientsDataTable();
        //    InfoTrack.NaqelAPI.App_Data.GeneralDSTableAdapters.ViwClientsTableAdapter adapterViwClient = new App_Data.GeneralDSTableAdapters.ViwClientsTableAdapter();
        //    adapterViwClient.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    try
        //    {
        //        adapterViwClient.Fill(dtViwClient);
        //    }
        //    catch { }
        //    string ServerPath = Server.MapPath(".");
        //    string[] x = ServerPath.Split('\\');

        //    ServerPath = ServerPath.Remove(ServerPath.IndexOf(x[x.Length - 1]));
        //    ServerPath += @"\Applications\InfoTrack";

        //    dtViwClient.WriteXml(ServerPath + @"\Client.xml", true);

        //    ICSharpCode.SharpZipLib.Zip.FastZip fastzip = new ICSharpCode.SharpZipLib.Zip.FastZip();
        //    fastzip.CreateZip(ServerPath + @"\Client.zip", ServerPath, true, "Client.xml");
        //}

        [WebMethod]
        public void GetClientandContact(byte[] ID)
        {
            if (!GlobalVar.GV.IsSecure(ID)) return;
            App_Data.GeneralDS.ViwClientandContactDataTable dtViwClient = new App_Data.GeneralDS.ViwClientandContactDataTable();
            InfoTrack.NaqelAPI.App_Data.GeneralDSTableAdapters.ViwClientandContactTableAdapter adapterViwClient = new App_Data.GeneralDSTableAdapters.ViwClientandContactTableAdapter();
            adapterViwClient.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            try
            {
                adapterViwClient.Fill(dtViwClient);
            }
            catch { }

            string ServerPath = Server.MapPath(".");
            string[] x = ServerPath.Split('\\');

            ServerPath = ServerPath.Remove(ServerPath.IndexOf(x[x.Length - 1]));
            ServerPath += @"\Applications\InfoTrack";

            dtViwClient.WriteXml(ServerPath + @"\ClientandContact.xml", true);

            ICSharpCode.SharpZipLib.Zip.FastZip fastzip = new ICSharpCode.SharpZipLib.Zip.FastZip();
            fastzip.CreateZip(ServerPath + @"\ClientandContact.zip", ServerPath, true, "ClientandContact.xml");
        }

        [WebMethod]
        public bool CheckForInfoTrackUpdate(int AppType)
        {
            bool result = false;

            App_Data.GeneralDS.SystemVariablesDataTable dt = new App_Data.GeneralDS.SystemVariablesDataTable();
            App_Data.GeneralDSTableAdapters.SystemVariablesTableAdapter adapter = new App_Data.GeneralDSTableAdapters.SystemVariablesTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (AppType == 1)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackDesktopUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }
            else
                if (AppType == 2)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackHHDUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }

            return result;
        }

        [WebMethod]
        public bool AddConsigneeLocation(int WaybillNo, string Latitude, string Longitude, int DeliveryTimeNo)
        {
            bool Result = false;
            try
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcMaster.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                InfoTrack.BusinessLayer.DContext.ConsigneeLocation instance = new ConsigneeLocation();
                instance.WaybillNo = WaybillNo;
                instance.Latitude = Latitude;
                instance.Longitude = Longitude;
                instance.Date = DateTime.Now;
                instance.DeliveryTimeID = DeliveryTimeNo;
                instance.StatusID = 1;
                dcMaster.ConsigneeLocations.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
                Result = true;
            }
            catch { }
            return Result;
        }

        [WebMethod]
        public bool AddVote(int WaybillNo, int VoteResult, string Notes)
        {
            bool Result = false;

            try
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.Vote instance = new Vote();
                instance.WaybillNo = WaybillNo;
                instance.VoteResult = VoteResult;
                instance.Notes = Notes;
                instance.VoteDate = DateTime.Now;
                instance.StatusID = 1;
                dcMaster.Votes.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
                Result = true;
            }
            catch { }

            return Result;
        }

        [WebMethod]
        public bool AddRate(int WaybillNo, string Notes, int OverallExperience, int DeliveryTimeLiness, int TraceAndTrace, int CallCenterService, int CourierAttitude)
        {
            bool Result = false;

            try
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                Rate instance = new Rate();
                instance.WaybillNo = WaybillNo;
                instance.Notes = Notes;
                instance.RateDate = DateTime.Now;
                instance.StatusID = 1;
                dcDoc.Rates.InsertOnSubmit(instance);
                dcDoc.SubmitChanges();

                RateDetail RateDetailInstance = new RateDetail();
                RateDetailInstance.RateID = instance.ID;
                RateDetailInstance.StatusID = 1;

                RateDetailInstance.OverallExperience = OverallExperience;
                RateDetailInstance.DeliveryTimeLiness = DeliveryTimeLiness;
                RateDetailInstance.TraceAndTrace = TraceAndTrace;
                RateDetailInstance.CallCenterService = CallCenterService;
                RateDetailInstance.CourierAttitude = CourierAttitude;

                dcDoc.RateDetails.InsertOnSubmit(RateDetailInstance);
                dcDoc.SubmitChanges();
                Result = true;
            }
            catch { }

            return Result;
        }

        //public List<Dictionary<string, int>> RateNewDetailList = new List<Dictionary<string, int>>();
        [WebMethod]
        public bool AddNewRate(int WaybillNo, string Notes, List<RateNewDetail> RateNewDetailList)
        {
            bool Result = false;

            try
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                dcDoc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                Rate instance = new Rate();
                instance.WaybillNo = WaybillNo;
                instance.Notes = Notes;
                instance.RateDate = DateTime.Now;
                instance.StatusID = 1;
                dcDoc.Rates.InsertOnSubmit(instance);
                dcDoc.SubmitChanges();

                for (int i = 0; i < RateNewDetailList.Count; i++)
                {
                    InfoTrack.BusinessLayer.DContext.RateNewDetail RateNewDetailInstance = new BusinessLayer.DContext.RateNewDetail();
                    RateNewDetailInstance.RateID = instance.ID;
                    RateNewDetailInstance.StatusID = 1;

                    RateNewDetailInstance.RateTypeID = Convert.ToInt32(RateNewDetailList[i].RateTypeID);
                    RateNewDetailInstance.RateNo = Convert.ToInt32(RateNewDetailList[i].RateNo);

                    dcDoc.RateNewDetails.InsertOnSubmit(RateNewDetailInstance);
                }
                dcDoc.SubmitChanges();
                Result = true;
            }
            catch { }

            return Result;
        }

        public enum AppTypes : int
        {
            InfoTrackWindowsApp = 1,
            HHD = 2,
            InfoTrackWindowsAppDLL = 3,
            HHDDLL = 4
        }

        [WebMethod]
        public bool CheckBeforeUpdate(AppTypes appType)
        {
            bool result = false;

            App_Data.GeneralDS.SystemVariablesDataTable dt = new App_Data.GeneralDS.SystemVariablesDataTable();
            App_Data.GeneralDSTableAdapters.SystemVariablesTableAdapter adapter = new App_Data.GeneralDSTableAdapters.SystemVariablesTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            if (appType == AppTypes.InfoTrackWindowsApp)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackDesktopUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }
            else
                if (appType == AppTypes.HHD)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackHHDUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }
            else
                    if (appType == AppTypes.InfoTrackWindowsAppDLL)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackDesktopDLLUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }
            else
                        if (appType == AppTypes.HHDDLL)
            {
                adapter.FillByVariableKey(dt, "AllowToCheckInfoTrackHHDDLLUpdate");
                if (dt.Rows.Count > 0)
                    result = Convert.ToBoolean(Convert.ToInt32((dt.Rows[0] as App_Data.GeneralDS.SystemVariablesRow).Variablevalue));
            }

            return result;
        }

        [WebMethod(Description = "You can use this function to upload files to the FTP.")]
        public Result UploadToFTP(ClientInformation ClientInfo, String FTPServerAndPath, String FullPathToLocalFile, String Username, String Password)
        {
            Result result = new Result();

            try
            {
                String filename = Path.GetFileName(FullPathToLocalFile);

                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(FTPServerAndPath + "/" + filename);

                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(Username, Password);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                FileStream stream = File.OpenRead(FullPathToLocalFile);
                byte[] buffer = new byte[stream.Length];

                stream.Read(buffer, 0, buffer.Length);

                stream.Close();

                Stream reqStream = request.GetRequestStream();

                int offset = 0;
                int chunk = (buffer.Length > 2048) ? 2048 : buffer.Length;
                while (offset < buffer.Length)
                {
                    reqStream.Write(buffer, offset, chunk);
                    offset += chunk;
                    chunk = (buffer.Length - offset < chunk) ? (buffer.Length - offset) : chunk;
                }
                reqStream.Close();
                result.Message = "Uploading File Success";
            }
            catch (Exception e)
            {
                result.HasError = true;
                result.Message = e.Message;
            }

            return result;
        }

        private string GetDateTimeString()
        {
            return DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
        }

        [WebMethod(Description = "Web service provides mothed for uploading files.")]
        public string UploadFiles(byte[] fs, string fileName, string RefNo, byte[] EmpID, EnumList.FileType filetype)
        {
            if (!GlobalVar.GV.IsSecure(EmpID)) return "";
            string result = "";
            string ServerPath = Server.MapPath(".");
            //ServerPath = ServerPath.Remove(ServerPath.IndexOf("NaqelAPIGeneral"));
            ServerPath = @"E:\wwwroot\";
            //ServerPath = ServerPath.Remove(ServerPath.IndexOf("NaqelAPI"));

            //if (filetype == EnumList.FileType.Claims || filetype == EnumList.FileType.WaybillAttachments)
            //{
            if (!System.IO.Directory.Exists(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo))
                System.IO.Directory.CreateDirectory(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo);

            if (System.IO.File.Exists(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + fileName))
            {
                if (transferFile.WriteBinarFile(fs, ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\", (DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString()) + fileName).Contains("successfully"))
                    result = ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + (DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString()) + fileName;
            }
            else
                if (transferFile.WriteBinarFile(fs, ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\", fileName).Contains("successfully"))
                result = ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + fileName;
            //}
            //else
            //{
            //    if (!System.IO.Directory.Exists(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo))
            //        System.IO.Directory.CreateDirectory(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo);

            //    if (System.IO.File.Exists(ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + fileName))
            //    {
            //        if (transferFile.WriteBinarFile(fs, ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\", (DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString()) + fileName).Contains("successfully"))
            //            result = ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + (DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString()) + fileName;
            //    }
            //    else
            //        if (transferFile.WriteBinarFile(fs, ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\", fileName).Contains("successfully"))
            //            result = ServerPath + @"\InfoTrackFiles\\" + filetype + "\\" + RefNo + "\\" + fileName;
            //}
            //else
            ////if (filetype == EnumList.FileType.DHLSuccessRequest)
            //{
            //    //if (!System.IO.Directory.Exists(ServerPath + @"\InfoTrackFiles\" + EnumList.FileType.DHLSuccessRequest.ToString() + "\\" + RefNo))
            //    //    System.IO.Directory.CreateDirectory(ServerPath + @"\InfoTrackFiles\Claims\" + RefNo);

            //    if (transferFile.WriteBinarFile(fs, ServerPath + @"\InfoTrackFiles\" + filetype.ToString() + "\\", GetDateTimeString() + ".xml").Contains("successfully"))
            //        result = ServerPath + @"\InfoTrackFiles\Claims\" + RefNo + "\\" + fileName;
            //}

            return result;
        }

        /// <summary>
        /// 101 Tracking From English WebSite
        /// 102 Tracking From Mobile Application
        /// 103 Tracking From Arabic WebSite
        /// 104 Tracking For Sending Email Notifications
        /// 105 Tracking For Rajeev
        /// </summary>
        /// <param name="WaybillNo"></param>
        /// <param name="TrackingSourceID"></param>
        /// <returns></returns>
        [WebMethod]
        public NewDataSet TraceByWaybillNo(int WaybillNo, int TrackingSourceID)
        {
            NewDataSet NewDataSet = new NewDataSet();

            if (TrackingSourceID != 101 &&
                TrackingSourceID != 102 &&
                TrackingSourceID != 103 &&
                TrackingSourceID != 104 &&
                TrackingSourceID != 105)
            {
                NewDataSet._ViewSmartPhoneTrack.WayBillNo = WaybillNo;
                return NewDataSet;
            }

            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (WaybillNo > 0)
            {
                InfoTrack.BusinessLayer.DContext.TrackingHistory TrackingHistoryInstance = new TrackingHistory();
                TrackingHistoryInstance.Date = DateTime.Now;
                TrackingHistoryInstance.WaybillNo = WaybillNo.ToString();
                TrackingHistoryInstance.TrackingSourceID = TrackingSourceID;
                dcMaster.TrackingHistories.InsertOnSubmit(TrackingHistoryInstance);
                dcMaster.SubmitChanges();

                if (dcDoc.ViwWaybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).Count() > 0)
                {
                    ViwWaybill instance = dcDoc.ViwWaybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false);
                    NewDataSet._ViewSmartPhoneTrack.ID = instance.ID;
                    NewDataSet._ViewSmartPhoneTrack.WayBillNo = WaybillNo;
                    NewDataSet._ViewSmartPhoneTrack.PicesCount = instance.PicesCount;
                    NewDataSet._ViewSmartPhoneTrack.Weight = instance.Weight;
                    NewDataSet._ViewSmartPhoneTrack.Name = "";
                    NewDataSet._ViewSmartPhoneTrack.IsDelivered = instance.IsDelivered;
                }
                else
                {
                    if (dcDoc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID != 3).Count() > 0)
                    {
                        CustomerWayBill instance = dcDoc.CustomerWayBills.First(P => P.WayBillNo == WaybillNo && P.StatusID != 3);
                        NewDataSet._ViewSmartPhoneTrack.ID = instance.ID;
                        NewDataSet._ViewSmartPhoneTrack.WayBillNo = WaybillNo;
                        NewDataSet._ViewSmartPhoneTrack.PicesCount = instance.PicesCount;
                        NewDataSet._ViewSmartPhoneTrack.Weight = instance.Weight;
                        NewDataSet._ViewSmartPhoneTrack.Name = "";
                        NewDataSet._ViewSmartPhoneTrack.IsDelivered = false;
                    }
                }
            }

            List<ViwTracking> ViwTrackingList = dcMaster.ViwTrackings.Where(P => P.WaybillNo == WaybillNo).ToList();
            for (int i = 0; i < ViwTrackingList.Count; i++)
            {
                ViwTracking Crow = ViwTrackingList[i] as ViwTracking;

                NewDataSet.ViwOnlineTracking row = new NaqelAPI.NewDataSet.ViwOnlineTracking();
                row.WaybillNo = Crow.WaybillNo;
                row.StationID = 501;
                row.Activity = Crow.Activity;
                row.ID = i;
                row.TDate = Crow.Date;
                NewDataSet._ViwOnlineTracking.Add(row);
            }

            return NewDataSet;
        }

        [WebMethod]
        public TrackingResult TraceMultiRefNo(string RefNo, int TrackingSourceID)
        {
            TrackingResult x = new TrackingResult();
            if (TrackingSourceID != 101 &&
                TrackingSourceID != 102 &&
                TrackingSourceID != 103 &&
                TrackingSourceID != 104 &&
                TrackingSourceID != 105)
            {
                x.shipmentDetails.Name = "No Details";
                return x;
            }
            //GlobalVar.GV.InsertError(RefNo, "15380");
            //if (RefNo.Length > 9)
            //    RefNo = RefNo.Remove(RefNo.Length - 1);
            //GlobalVar.GV.InsertError(RefNo, "15380");
            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (!IsNumber(RefNo))
            {
                if (dcDoc.Waybills.Where(P => P.RefNo.ToLower() == RefNo.ToLower() &&
                                         P.InvoiceAccount == 9017255 &&
                                         P.IsCancelled == false).Count() > 0)
                {
                    Waybill WInstance = dcDoc.Waybills.First(P => P.RefNo.ToLower() == RefNo.ToLower() &&
                                         P.InvoiceAccount == 9017255 &&
                                         P.IsCancelled == false);
                    return TraceMultiWaybill(WInstance.WayBillNo, TrackingSourceID);
                }
                else
                {
                    int WaybillNo = -2222;
                    if (RefNo.Length > 8)
                        WaybillNo = Convert.ToInt32(RefNo.Remove(8));
                    return TraceMultiWaybill(WaybillNo, TrackingSourceID);
                }
            }
            else
            {
                int WaybillNo = Convert.ToInt32(RefNo);
                if (RefNo.Length > 8)
                    WaybillNo = Convert.ToInt32(RefNo.Remove(8));
                return TraceMultiWaybill(WaybillNo, TrackingSourceID);
            }
        }

        [WebMethod]
        public TrackingResult TraceMultiWaybill(int WaybillNo, int TrackingSourceID)
        {
            TrackingResult trackingResult = new TrackingResult();
            //TrackingResult trackingResult1 = new TrackingResult();
            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcHHD = new HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());

            ViwWaybill instance = new ViwWaybill();

            if (WaybillNo > 0)
            {
                TrackingHistory TrackingHistoryInstance = new TrackingHistory();
                TrackingHistoryInstance.Date = DateTime.Now;
                TrackingHistoryInstance.WaybillNo = WaybillNo.ToString();
                TrackingHistoryInstance.TrackingSourceID = TrackingSourceID;
                dcMaster.TrackingHistories.InsertOnSubmit(TrackingHistoryInstance);
                dcMaster.SubmitChanges();

                if (dcDoc.ViwWaybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).Count() > 0)
                {
                    instance = dcDoc.ViwWaybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false);

                    trackingResult.shipmentDetails.ID = instance.ID;
                    trackingResult.shipmentDetails.WayBillNo = WaybillNo;
                    trackingResult.shipmentDetails.PickUpDate = instance.PickUpDate;
                    trackingResult.shipmentDetails.Weight = instance.Weight;
                    trackingResult.shipmentDetails.PicesCount = instance.PicesCount;
                    trackingResult.shipmentDetails.Destination = GetDestination(instance.DestinationStationID, true);
                    trackingResult.shipmentDetails.DestinationAr = GetDestination(instance.DestinationStationID, false);
                    trackingResult.shipmentDetails.Name = "";
                    trackingResult.shipmentDetails.IsDelivered = instance.IsDelivered;

                    if (instance.IsDelivered)
                        trackingResult.shipmentDetails.StageID = 5;
                    else
                    {
                        trackingResult.shipmentDetails.StageID = 0;

                        if (dcDoc.rpDeliverySheets.Where(P => P.WayBillNo == WaybillNo).Count() > 0)
                            //&&
                            //P.Date.Year == DateTime.Now.Year &&
                            //P.Date.Month == DateTime.Now.Month &&
                            //P.Date.Day == DateTime.Now.Day).Count() > 0)
                            trackingResult.shipmentDetails.StageID = 4;
                        else
                            if (dcMaster.ViwAtDestinations.Where(P => P.WaybillNo == WaybillNo &&
                                P.StationID == instance.DestinationStationID).Count() > 0)
                            trackingResult.shipmentDetails.StageID = 3;
                        else
                                if (dcHHD.AtOriginWaybillDetails.Where(P => P.WaybillNo == WaybillNo).Count() > 0 ||
                                    dcHHD.OnLoadingWaybillDetails.Where(P => P.WaybillNo == WaybillNo).Count() > 0)
                            trackingResult.shipmentDetails.StageID = 2;
                        else
                                    if (dcHHD.PickUps.Where(P => P.WaybillNo == WaybillNo).Count() > 0)
                            trackingResult.shipmentDetails.StageID = 1;
                    }
                }
                else
                {
                    if (dcDoc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID != 3).Count() > 0)
                    {
                        CustomerWayBill customerWayBillInstance = dcDoc.CustomerWayBills.First(P => P.WayBillNo == WaybillNo && P.StatusID != 3);
                        trackingResult.shipmentDetails.ID = customerWayBillInstance.ID;
                        trackingResult.shipmentDetails.WayBillNo = WaybillNo;
                        trackingResult.shipmentDetails.PickUpDate = customerWayBillInstance.PickUpDate;
                        trackingResult.shipmentDetails.Weight = customerWayBillInstance.Weight;
                        trackingResult.shipmentDetails.PicesCount = customerWayBillInstance.PicesCount;
                        trackingResult.shipmentDetails.Destination = GetDestination(customerWayBillInstance.DestinationStationID, true);
                        trackingResult.shipmentDetails.DestinationAr = GetDestination(customerWayBillInstance.DestinationStationID, false);
                        trackingResult.shipmentDetails.Name = "";
                        trackingResult.shipmentDetails.IsDelivered = false;

                        {
                            trackingResult.shipmentDetails.StageID = 0;


                            if (dcDoc.rpDeliverySheets.Where(P => P.WayBillNo == WaybillNo).Count() > 0)
                                //&&
                                //P.Date.Year == DateTime.Now.Year &&
                                //P.Date.Month == DateTime.Now.Month &&
                                //P.Date.Day == DateTime.Now.Day).Count() > 0)
                                trackingResult.shipmentDetails.StageID = 4;
                            else
                                if (dcMaster.ViwAtDestinations.Where(P => P.WaybillNo == WaybillNo &&
                                    P.StationID == customerWayBillInstance.DestinationStationID).Count() > 0)
                                trackingResult.shipmentDetails.StageID = 3;
                            else
                                    if (dcHHD.AtOriginWaybillDetails.Where(P => P.WaybillNo == WaybillNo).Count() > 0 ||
                                        dcHHD.OnLoadingWaybillDetails.Where(P => P.WaybillNo == WaybillNo).Count() > 0)
                                trackingResult.shipmentDetails.StageID = 2;
                            else
                                        if (dcHHD.PickUps.Where(P => P.WaybillNo == WaybillNo).Count() > 0)
                                trackingResult.shipmentDetails.StageID = 1;
                        }
                    }
                }
            }

            if (instance != null && instance.WayBillNo > 0 && instance.CourierSupplierID.HasValue && instance.RefNo.Length > 6)
            {
                var trackingDetails = CreateNewrackingDetails(instance.RefNo);
                List<DHLTracking> dhlResult = GetTrackingFromDHL(trackingDetails);

                for (int i = 0; i < dhlResult.Count; i++)
                {
                    try
                    {
                        DHLTracking Crow = dhlResult[i] as DHLTracking;

                        TrackingResult.OnlineTracking row = new NaqelAPI.TrackingResult.OnlineTracking();
                        row.WaybillNo = instance.WayBillNo;
                        row.StationID = 501;
                        row.Activity = Crow.ServiceName;// +" in " + Crow.AreaCode;
                        row.ActivityAr = Crow.ServiceName;// Crow.ActivityAr;
                        row.ID = i;
                        row.TDate = Crow.Date.Value;
                        trackingResult.viwOnlineTracking.Add(row);
                    }
                    catch { }
                }
                //for (int i = 0; i < trackingResult1.viwOnlineTracking.OrderByDescending(P => P.TDate).Count(); i++)
                //{
                //    trackingResult.viwOnlineTracking.Add(trackingResult1.viwOnlineTracking[i]);
                //}
                ////trackingResult.viwOnlineTracking.OrderByDescending(P => P.TDate);    
            }
            //else
            //{
            //    for (int i = 0; i < trackingResult1.viwOnlineTracking.OrderByDescending(P => P.TDate).Count(); i++)
            //    {
            //        trackingResult.viwOnlineTracking.Add(trackingResult1.viwOnlineTracking[i]);
            //    }
            //}

            List<ViwTracking> ViwTrackingList = dcMaster.ViwTrackings.Where(P => P.WaybillNo == WaybillNo &&
                P.IsInternalType == false).OrderByDescending(P => P.Date).ToList();
            for (int i = 0; i < ViwTrackingList.Count; i++)
            {
                ViwTracking Crow = ViwTrackingList[i] as ViwTracking;

                TrackingResult.OnlineTracking row = new NaqelAPI.TrackingResult.OnlineTracking();
                row.WaybillNo = Crow.WaybillNo;
                row.StationID = 501;
                row.Activity = Crow.Activity;
                row.ActivityAr = Crow.ActivityAr;
                row.ID = i;
                row.TDate = Crow.Date;
                trackingResult.viwOnlineTracking.Add(row);
            }

            return trackingResult;
        }

        private string GetDestination(int StationID, bool EnglishLanguage)
        {
            string Result = "";

            InfoTrack.BusinessLayer.DContext.MastersDataContext dcData = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.Station stationInstance = dcData.Stations.First(P => P.ID == StationID);
            InfoTrack.BusinessLayer.DContext.City cityInstance = dcData.Cities.First(P => P.ID == stationInstance.CityID);
            InfoTrack.BusinessLayer.DContext.Country countryInstance = dcData.Countries.First(P => P.ID == cityInstance.CountryID);

            if (EnglishLanguage)
                Result = stationInstance.Name + "-" + countryInstance.Name;
            else
                Result = stationInstance.FName + "-" + countryInstance.FName;

            return Result;
        }

        private bool IsNumber(string text)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
            return regex.IsMatch(text);
        }

        //[WebMethod]
        //public NewDataSet TraceByWaybillNo1(string RefNo)
        //{
        //    //GlobalVar.GV.InsertError(RefNo, "15380");
        //    NewDataSet NewDataSet = new NewDataSet();
        //    App_Data.DataDataContext dc = new App_Data.DataDataContext();
        //    dc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

        //    //Check if the RefNo is Waybill No or Ref No for Saudi Post
        //    if (!IsNumber(RefNo))
        //    {
        //        if (dc.Waybills.Where(P => P.RefNo.ToLower() == RefNo.ToLower() &&
        //                                 P.InvoiceAccount == 9017255 &&
        //                                 P.IsCancelled == false).Count() > 0)
        //        {
        //            App_Data.Waybill WInstance = dc.Waybills.First(P => P.RefNo.ToLower() == RefNo.ToLower() &&
        //                                 P.InvoiceAccount == 9017255 &&
        //                                 P.IsCancelled == false);
        //            return TraceByWaybillNo(WInstance.WayBillNo);
        //        }
        //        else
        //        {
        //            int WaybillNo = -2222;
        //            if (RefNo.Length > 8)
        //                WaybillNo = Convert.ToInt32(RefNo.Remove(8));
        //            return TraceByWaybillNo(WaybillNo);
        //        }
        //    }
        //    else
        //    {
        //        int WaybillNo = Convert.ToInt32(RefNo);
        //        if (RefNo.Length > 8)
        //            WaybillNo = Convert.ToInt32(RefNo.Remove(8));
        //        return TraceByWaybillNo(WaybillNo);
        //    }

        //    //if (WaybillNo > 0)
        //    //{
        //    //    if (dc.ViwWaybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).Count() > 0)
        //    //    {
        //    //        App_Data.ViwWaybill instance = dc.ViwWaybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false);
        //    //        NewDataSet._ViewSmartPhoneTrack.ID = instance.ID;
        //    //        NewDataSet._ViewSmartPhoneTrack.WayBillNo = WaybillNo;
        //    //        NewDataSet._ViewSmartPhoneTrack.PicesCount = instance.PicesCount;
        //    //        NewDataSet._ViewSmartPhoneTrack.Weight = instance.Weight;
        //    //        NewDataSet._ViewSmartPhoneTrack.Name = "";
        //    //        NewDataSet._ViewSmartPhoneTrack.IsDelivered = instance.IsDelivered;
        //    //    }
        //    //    else
        //    //    {
        //    //        if (dc.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID != 3).Count() > 0)
        //    //        {
        //    //            App_Data.CustomerWayBill instance = dc.CustomerWayBills.First(P => P.WayBillNo == WaybillNo && P.StatusID != 3);
        //    //            NewDataSet._ViewSmartPhoneTrack.ID = instance.ID;
        //    //            NewDataSet._ViewSmartPhoneTrack.WayBillNo = WaybillNo;
        //    //            NewDataSet._ViewSmartPhoneTrack.PicesCount = instance.PicesCount;
        //    //            NewDataSet._ViewSmartPhoneTrack.Weight = instance.Weight;
        //    //            NewDataSet._ViewSmartPhoneTrack.Name = "";
        //    //            NewDataSet._ViewSmartPhoneTrack.IsDelivered = false;
        //    //        }
        //    //    }
        //    //}

        //    //List<App_Data.ViwTracking> ViwTrackingList = dc.ViwTrackings.Where(P => P.WaybillNo == WaybillNo).ToList();
        //    //for (int i = 0; i < ViwTrackingList.Count; i++)
        //    //{
        //    //    App_Data.ViwTracking Crow = ViwTrackingList[i] as App_Data.ViwTracking;

        //    //    NewDataSet.ViwOnlineTracking row = new NaqelAPI.NewDataSet.ViwOnlineTracking();
        //    //    row.WaybillNo = Crow.WaybillNo;
        //    //    row.StationID = 501;
        //    //    row.Activity = Crow.Activity;
        //    //    row.ID = i;
        //    //    row.TDate = Crow.Date;
        //    //    NewDataSet._ViwOnlineTracking.Add(row);
        //    //}

        //    //return NewDataSet;
        //}

        [WebMethod]
        public LocationDetails GetLocationDetails(int WaybillNo)
        {
            LocationDetails Result = new LocationDetails();
            Result.HasLocation = false;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            if (dcMaster.ConsigneeLocations.Where(P => P.WaybillNo == WaybillNo && P.StatusID != 3).Count() > 0)
            {
                ConsigneeLocation instance = dcMaster.ConsigneeLocations.First(P => P.WaybillNo == WaybillNo && P.StatusID != 3);

                Result.HasLocation = true;
                Result.Latitude = instance.Latitude;
                Result.Longitude = instance.Longitude;

                Result.ConsigneeLocation = instance.Latitude + "," + instance.Longitude;
            }

            return Result;
        }

        //[WebMethod]
        //public App_Data.SMSSendingStatus SendSMS(byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        //{
        //    //SendSMSResponse result = new SendSMSResponse();
        //    //string result = "";
        //    App_Data.SMSSendingStatus result = new App_Data.SMSSendingStatus();
        //    result.ID = 0;
        //    result.Name = "Message not Send, Check the value for ID";
        //    if (!GlobalVar.GV.IsSecure(ID)) return result;

        //    SendSMSRequest request = new SendSMSRequest
        //    {
        //        Username = "naqel",
        //        Password = "123456",
        //        Tagname = "NAQEL",
        //        Message = message,
        //        RecepientNumber = MobileNo,
        //        ReplacementList = string.Empty,
        //        VariableList = string.Empty,
        //        SendDateTime = 0
        //    };

        //    InfoTrack.SMS.YamamahSMS sms = new YamamahSMS();
        //    var res = sms.SendPOSTSMS(request);

        //    result.ID = res.Status;
        //    result.Name = res.StatusDescription;

        //    AddSMSSentMessage(ID, message, Purpose, res.Status, MobileNo, RefNo, 0);

        //    return result;
        //}

        private void AddSMSSentMessage(byte[] EmployID, string Message, EnumList.PurposeList Purpose, int Status, string MobileNo, string RefNo, int MessageID, string Balance)
        {
            try
            {
                SMSSentMessage NewMessage = new SMSSentMessage();

                InfoTrack.Common.Security sec = new Common.Security();
                string dec = "-1";
                try { dec = sec.Decrypt(EmployID); }
                catch { }

                NewMessage.EmployID = Convert.ToInt32(dec);
                NewMessage.Date = DateTime.Now;
                NewMessage.Message = Message;
                NewMessage.PurposeID = Convert.ToInt32(Purpose);
                NewMessage.SMSSendingStatusID = Status;
                NewMessage.StatusID = 1;
                NewMessage.MobileNo = MobileNo;
                NewMessage.RefNo = RefNo;
                NewMessage.MessageID = MessageID;
                NewMessage.Balance = Balance;

                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());

                dcDoc.SMSSentMessages.InsertOnSubmit(NewMessage);
                dcDoc.SubmitChanges();
            }
            catch (Exception ee) { GlobalVar.GV.InsertError(ee.Message, GlobalVar.GV.GetEmpID(EmployID).ToString()); }
        }

        [WebMethod]
        public string GetSMSBalance()
        {
            string result = "";
            //var orc = new Otsdc.OtsdcRestClient("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");
            //Otsdc.BalanceDetail inc = new Otsdc.BalanceDetail();
            //inc = orc.GetBalance("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");
            //result = inc.Balance.ToString() + " - " + inc.CurrencyCode + " - " + inc.message;

            try
            {
                System.Net.WebRequest request = System.Net.WebRequest.Create("http://global.myvaluefirst.com/smpp/creditstatus.jsp?");
                request.Method = "POST";
                string postData = "user=naqelhttp&password=40931http";
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                System.IO.Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                System.Net.WebResponse response = request.GetResponse();
                Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
                dataStream = response.GetResponseStream();
                System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();

                reader.Close();
                dataStream.Close();
                response.Close();

                result = responseFromServer;

            }
            catch { }

            return result;
        }

        //[WebMethod]
        //public void SendByNewCompany()
        //{
        //    var instance = new ValueFirst.ValueFirstRestClient(); //Otsdc.OtsdcRestClient("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");
        //    ValueFirst.SendSmsMessageResult sendSmsMessageResult = new ValueFirst.SendSmsMessageResult();
        //    try
        //    {
        //        sendSmsMessageResult = instance.SendMessage("966535638988", "Finish");
        //    }
        //    catch { }
        //}

        //[WebMethod]
        //public void SendSMSbyOTS1()
        //{
        //    App_Data.SMSSendingStatus result = new App_Data.SMSSendingStatus();
        //    var orc = new Otsdc.OtsdcRestClient("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");
        //    Otsdc.SendSmsMessageResult sendSmsMessageResult = new Otsdc.SendSmsMessageResult();
        //    try
        //    {
        //        sendSmsMessageResult = orc.SendSmsMessage("966535638988", "test", "Naqel");
        //    }
        //    catch (Exception exc)
        //    {
        //    }
        //}

        //byte[] ID,
        [WebMethod]
        public SMSSendingStatus SendSMSbyOTS(byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        {
            SMSSendingStatus result = new SMSSendingStatus();
            if (Purpose == EnumList.PurposeList.GPSTracking && message.ToLower().Contains("your password is :"))
            { }
            else
                //result = SendSMSbyValueFirst(ID, MobileNo, message, Purpose, RefNo);
                result = SendSMSbyDCSME(ID, MobileNo, message, Purpose, RefNo);
            return result;

            //result.ID = 105;
            //result.Name = "Message not Send, Check the value for IDs";
            //if (!GlobalVar.GV.IsSecure(ID))
            //{
            //    result.Name = "Check ID Value";
            //    return result;
            //}

            //MobileNo = GetMobileNo(MobileNo);
            //if (MobileNo != "")
            //{
            //    //try
            //    //{
            //    var orc = new Otsdc.OtsdcRestClient("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");

            //    Otsdc.SendSmsMessageResult sendSmsMessageResult = new Otsdc.SendSmsMessageResult();
            //    try
            //    {
            //        sendSmsMessageResult = orc.SendSmsMessage(MobileNo, message, "Naqel");
            //        result.ID = GetSMSStatusID(Convert.ToInt32(sendSmsMessageResult.MessageID));
            //        AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, Convert.ToInt32(sendSmsMessageResult.MessageID), sendSmsMessageResult.Balance.ToString());
            //        result.Name = "Your message has been sent Successfully";
            //    }
            //    catch (Exception exc)
            //    {
            //        result.Name = exc.Message;
            //        GlobalVar.GV.InsertError(exc.Message, "15380");
            //        GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
            //    }

            //    //}
            //    //catch (Exception exc)
            //    //{
            //    //    result.ID = 60;
            //    //    result.Name = exc.Message;
            //    //}
            //}

            //return result;
        }

        //[WebMethod]
        //public SMSSendingStatus SendSMSTest(string MobileNo, string message, string RefNo)
        //{
        //    SMSSendingStatus result = new SMSSendingStatus();
        //    result.ID = 105;
        //    result.Name = "Message not Send, Check the value for IDs";

        //    MobileNo = GetMobileNo(MobileNo);
        //    if (MobileNo != "")
        //    {
        //        try
        //        {
        //            System.Net.WebRequest request = System.Net.WebRequest.Create("http://103.15.179.45:8085/MessagingGateway/SendTransSMS?");
        //            request.Method = "POST";
        //            string postData = "";

        //            message = @"بسم الله الرحمن الرحيم";
        //            message += " http://www.naqelexpress.com";
        //            message = GetUnicodeMessage(message);

        //            postData = "Username=Demointer&Password=NaqelInfoTrackSMS@018&MessageType=uni&Mobile=" + MobileNo + "&SenderID=Naqel&Message=" + message;
        //            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
        //            request.ContentType = "application/x-www-form-urlencoded";
        //            request.ContentLength = byteArray.Length;
        //            System.IO.Stream dataStream = request.GetRequestStream();
        //            dataStream.Write(byteArray, 0, byteArray.Length);
        //            dataStream.Close();
        //            System.Net.WebResponse response = request.GetResponse();
        //            Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
        //            dataStream = response.GetResponseStream();
        //            System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
        //            string responseFromServer = reader.ReadToEnd();

        //            reader.Close();
        //            dataStream.Close();
        //            response.Close();

        //            if (responseFromServer == "Sent.")
        //            {
        //                result.ID = 101;
        //                result.Name = "Your message has been sent Successfully";
        //            }
        //            else
        //            {
        //                result.ID = 103;
        //                result.Name = responseFromServer;
        //            }
        //            //AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, 0, "0");

        //        }
        //        catch (Exception exc)
        //        {
        //            result.Name = exc.Message;
        //            GlobalVar.GV.InsertError(exc.Message, "15380");
        //            GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
        //        }
        //    }

        //    return result;
        //}

        public SMSSendingStatus SendSMSbyDCSME(byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        {
            SMSSendingStatus result = new SMSSendingStatus();
            result.ID = 105;
            result.Name = "Message not Send, Check the value for IDs";
            if (!GlobalVar.GV.IsSecure(ID))
            {
                result.Name = "Check ID Value";
                return result;
            }

            MobileNo = GetMobileNo(MobileNo);
            if (MobileNo != "")
            {
                try
                {
                    System.Net.WebRequest request = System.Net.WebRequest.Create("http://103.15.179.45:8085/MessagingGateway/SendTransSMS?");
                    request.Method = "POST";
                    string postData = "";//

                    string UMessage = GetUnicodeMessage(message);

                    postData = "Username=Demointer&Password=NaqelInfoTrackSMS@018&MessageType=uni&Mobile=" + MobileNo + "&SenderID=Naqel&Message=" + UMessage;
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = byteArray.Length;
                    System.IO.Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    System.Net.WebResponse response = request.GetResponse();
                    Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
                    dataStream = response.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();

                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    if (responseFromServer == "Sent.")
                    {
                        result.ID = 101;
                        result.Name = "Your message has been sent Successfully";
                    }
                    else
                    {
                        result.ID = 103;
                        result.Name = responseFromServer;
                    }
                    AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, 0, "0");

                }
                catch (Exception exc)
                {
                    result.Name = exc.Message;
                    GlobalVar.GV.InsertError(exc.Message, "15380");
                    GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
                }
            }

            return result;
        }

        private string GetUnicodeMessage(string Message)
        {
            string msg = "";
            Encoding enc = Encoding.BigEndianUnicode;
            byte[] bytes = enc.GetBytes(Message);
            for (int i = 0; i < bytes.Length; i++)
                msg += String.Format("{0:X2}", bytes[i]);

            return msg;
        }

        #region Old

        //foreach (char c in asciiString)
        //{
        //    int tmp = c;
        //    string msg = String.Format("{0:X2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
        //    if (msg.Length == 2)
        //        msg = "00" + msg;
        //    else
        //        if (msg.Length == 3)
        //        msg = "0" + msg;
        //    hex += msg;
        //}



        //string input =asciiString;
        //Encoding enc = Encoding.BigEndianUnicode;
        //byte[] bytes = enc.GetBytes(asciiString);
        //char[] values = input.ToCharArray();
        //foreach (char letter in values)
        //{
        //    // Get the integral value of the character.
        //    int value = Convert.ToInt32(letter);
        //    // Convert the decimal value to a hexadecimal value in string form.
        //    string hexOutput = String.Format("{0:X}", value);
        //    Console.WriteLine("Hexadecimal value of {0} is {1}", letter, hexOutput);
        //}

        #endregion

        //public string Utf16ToUtf8(string message)
        //{
        //    // Get UTF16 bytes and convert UTF16 bytes to UTF8 bytes
        //    //byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
        //    //byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

        //    // Return UTF8 bytes as ANSI string
        //    //return Encoding.Default.GetString(utf16Bytes);

        //    string x = "";
        //    Encoding enc = Encoding.Unicode;
        //    byte[] bytes = enc.GetBytes(message);
        //    for (int i = 0; i < bytes.Length; i++)
        //    {
        //        StringBuilder ss = new StringBuilder();
        //        x += ss.Append("{0:X2}", bytes[i]);
        //    }
        //    //Console.Write("{0:X2} ", bytes[i]);
        //}

        //[WebMethod]

        public SMSSendingStatus SendSMSbyValueFirst(byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        {
            SMSSendingStatus result = new SMSSendingStatus();
            result.ID = 105;
            result.Name = "Message not Send, Check the value for IDs";
            if (!GlobalVar.GV.IsSecure(ID))
            {
                result.Name = "Check ID Value";
                return result;
            }

            MobileNo = GetMobileNo(MobileNo);
            if (MobileNo != "")
            {
                try
                {
                    System.Net.WebRequest request = System.Net.WebRequest.Create("http://global.myvaluefirst.com/smpp/sendsms?");
                    request.Method = "POST";
                    string postData = "username=naqelhttp&password=40931http&to=" + MobileNo + "&from=Naqel&text=" + message + "&coding=3";
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = byteArray.Length;
                    System.IO.Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    System.Net.WebResponse response = request.GetResponse();
                    Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
                    dataStream = response.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();

                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    if (responseFromServer == "Sent.")
                    {
                        result.ID = 101;
                        result.Name = "Your message has been sent Successfully";
                    }
                    else
                    {
                        result.ID = 103;
                        result.Name = responseFromServer;
                    }
                    AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, 0, "0");

                }
                catch (Exception exc)
                {
                    result.Name = exc.Message;
                    GlobalVar.GV.InsertError(exc.Message, "15380");
                    GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
                }
            }

            return result;
        }

        public SMSSendingStatus SendOTP(byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        {
            SMSSendingStatus result = new SMSSendingStatus();
            result.ID = 105;
            result.Name = "Message not Send, Check the value for IDs";
            if (!GlobalVar.GV.IsSecure(ID))
            {
                result.Name = "Check ID Value";
                return result;
            }

            MobileNo = GetMobileNo(MobileNo);
            if (MobileNo != "")
            {
                try
                {
                    System.Net.WebRequest request = System.Net.WebRequest.Create("http://global.myvaluefirst.com/smpp/sendsms?");
                    request.Method = "POST";
                    string postData = "username=naqelotp&password=4@93!otp&to=" + MobileNo + "&from=Naqel&text=" + message + "&coding=3";
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = byteArray.Length;
                    System.IO.Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    System.Net.WebResponse response = request.GetResponse();
                    Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
                    dataStream = response.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();

                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    if (responseFromServer == "Sent.")
                    {
                        result.ID = 101;
                        result.Name = "Your message has been sent Successfully";
                    }
                    else
                    {
                        result.ID = 103;
                        result.Name = responseFromServer;
                    }
                    AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, 0, "0");

                }
                catch (Exception exc)
                {
                    result.Name = exc.Message;
                    GlobalVar.GV.InsertError(exc.Message, "15380");
                    GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
                }
            }

            return result;
        }

        [WebMethod]
        public void SendSMS(int ID)
        {
            InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc = new BusinessLayer.DContext.DocumentDataDataContext();
            dcDoc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            if (dcDoc.SMSNotSentMessages.Where(P => P.ID == ID && P.IsSent == false).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.SMSNotSentMessage instance = dcDoc.SMSNotSentMessages.First(P => P.ID == ID);
                InfoTrack.Common.Security security = new InfoTrack.Common.Security();
                SMSSendingStatus result;
                if (instance.PriorityNo == 0)
                {
                    result = SendOTP(security.Encrypt(instance.EmployID.ToString()), instance.MobileNo, instance.Message, GlobalVar.GV.GetPurposeID(instance.PurposeID), instance.RefNo);
                }
                else
                {
                    result = SendSMSbyValueFirst(security.Encrypt(instance.EmployID.ToString()), instance.MobileNo, instance.Message, GlobalVar.GV.GetPurposeID(instance.PurposeID), instance.RefNo);
                }
                instance.IsSent = true;
                dcDoc.SubmitChanges();
            }
        }

        [WebMethod]
        public SMSSendingStatus GetSaudiPostCities()//byte[] ID, string MobileNo, string message, EnumList.PurposeList Purpose, string RefNo)
        {
            SMSSendingStatus result = new SMSSendingStatus();
            result.ID = 105;
            result.Name = "Message not Send, Check the value for IDs";
            /*if (!GlobalVar.GV.IsSecure(ID))
            {
                result.Name = "Check ID Value";
                return result;
            }*/

            //MobileNo = GetMobileNo(MobileNo);
            //if (MobileNo != "")
            {
                try
                {
                    System.Net.WebRequest request = System.Net.WebRequest.Create("http://saudipost.api.mashery.com/v3.1/lookup/regions?");
                    request.Method = "POST";
                    string postData = "language=A&format=XML&encode=utf8&api_key=2c9wy24jftnvhp49z29apxqz";
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                    //request.ContentType = "application/x-www-form-urlencoded";
                    //request.Headers.Add("X-Originating-Ip: 94.99.189.153");

                    request.ContentLength = byteArray.Length;
                    System.IO.Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    System.Net.WebResponse response = request.GetResponse();
                    Console.WriteLine(((System.Net.HttpWebResponse)response).StatusDescription);
                    dataStream = response.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();

                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    if (responseFromServer == "Sent.")
                    {
                        result.ID = 101;
                        result.Name = "Your message has been sent Successfully";
                    }
                    else
                    {
                        result.ID = 103;
                        result.Name = responseFromServer;
                    }
                    //AddSMSSentMessage(ID, message, Purpose, result.ID, MobileNo, RefNo, 0, "0");

                }
                catch (Exception exc)
                {
                    result.Name = exc.Message;
                    GlobalVar.GV.InsertError(exc.Message, "15380");
                    GlobalVar.GV.InsertError(exc.InnerException.Message, "15380");
                }
            }

            return result;
        }

        [WebMethod]
        public void AutoManifest(ClientInformation ClientInfo, int? WaybillNo, int? PickUpBy, int? InvoiceAccount, int? InvoiceAccountSl)
        {
            //if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                AutomaticManifest instance = new AutomaticManifest();
                instance.ManfiestWaybills(WaybillNo, PickUpBy, InvoiceAccount, InvoiceAccountSl);
            }
        }

        private string GetMobileNo(string MobileNo)
        {
            string NewMobileNo = "";
            if (MobileNo.Length == 9)
                MobileNo = "0" + MobileNo;
            if (MobileNo.Length > 9)
            {
                int j = 0;
                for (int i = MobileNo.Length - 1; i > 0; i--)
                {
                    if (j == 9)
                        break;
                    NewMobileNo = MobileNo[i] + NewMobileNo;
                    j++;
                }

                NewMobileNo = "966" + NewMobileNo;
            }
            return NewMobileNo;
        }

        [WebMethod]
        public int GetSMSStatusID(int MessageID)
        {
            int result = 102;

            var orc = new Otsdc.OtsdcRestClient("Oz5SEJ1lxN20KA1Zmlzx8EWmSorn");
            var getSmsMessageStatusResult = orc.GetSmsMessageStatus(MessageID.ToString());

            if (getSmsMessageStatusResult.Status.HasValue)
            {
                if (getSmsMessageStatusResult.Status.Value == Otsdc.SmsMessageStatus.Sent)
                    result = 101;
                else
                    if (getSmsMessageStatusResult.Status.Value == Otsdc.SmsMessageStatus.Queued)
                    result = 102;
                else
                        if (getSmsMessageStatusResult.Status.Value == Otsdc.SmsMessageStatus.Rejected)
                    result = 103;
                else
                            if (getSmsMessageStatusResult.Status.Value == Otsdc.SmsMessageStatus.Failed)
                    result = 104;
            }

            return result;
        }

        [WebMethod]
        public List<InfoTrack.BusinessLayer.DContext.FilesRequired> GetFilesRequired(AppTypes type)
        {
            dcMaster = new MastersDataContext();
            dcMaster.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            List<FilesRequired> ListFilesRequired = dcMaster.FilesRequireds.Where(P => P.StatusID == 1 && P.AppTypes == Convert.ToInt32(type)).ToList();
            return ListFilesRequired;
        }

        [WebMethod]
        public void TrackingByPartner(int NaqelWaybillNo, string PartnerWaybillNo)
        {
            if (NaqelWaybillNo > 0)
            {

            }
            else
            {
                var trackingDetails = CreateNewrackingDetails(PartnerWaybillNo);
                SendTrackingDetailsRequest(trackingDetails);
            }
        }

        private KnownTrackingRequest CreateNewrackingDetails(string PartnerWaybillNo)
        {
            var TrackingRequest = new KnownTrackingRequest();
            TrackingRequest.Request = new Request1()
            {
                ServiceHeader = new ServiceHeader1()
                {
                    SiteID = "naqelexpress", //Valid Site ID
                    Password = "zZeNuwltLX", //Valid Password 
                    MessageReference = "1234567890123456789012345678901", //Message Reference - used for tracking meesages
                    MessageTime = DateTime.Now
                }
            };

            TrackingRequest.LanguageCode = "EN";
            TrackingRequest.PiecesEnabled = KnownTrackingRequestPiecesEnabled.S;
            TrackingRequest.ItemsElementName = new ItemsChoiceType[]
            {
                ItemsChoiceType.AWBNumber
            };
            TrackingRequest.Items = new string[] { PartnerWaybillNo };
            TrackingRequest.LevelOfDetails = LevelOfDetails.ALL_CHECK_POINTS;

            return TrackingRequest;
        }

        private void SendTrackingDetailsRequest(KnownTrackingRequest trackingdetails)
        {
            // Create a request for the URL. 
            //WebRequest request = WebRequest.Create("https://xmlpitest-ea.dhl.com/XMLShippingServlet"); // Demo
            WebRequest request = WebRequest.Create("https://xmlpi-ea.dhl.com/XMLShippingServlet"); // Live

            // If required by the server, set the credentials.
            // request.Credentials = CredentialCache.DefaultCredentials;

            // Wrap the request stream with a text-based writer
            request.Method = "POST";        // Post method
            request.ContentType = "text/xml";

            var stream = request.GetRequestStream();
            StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

            // Write the XML text into the stream
            var soapWriter = new XmlSerializer(typeof(KnownTrackingRequest));

            //add namespaces and/or prefixes ( e.g " <req:KnownTrackingRequest xmlns:req="http://www.dhl.com"> ... </req:KnownTrackingRequest>"
            var ns = new XmlSerializerNamespaces();
            ns.Add("req", "http://www.dhl.com");
            soapWriter.Serialize(writer, trackingdetails, ns);
            writer.Close();

            //WritetoXML(trackingdetails, true, "0");

            // Get the response.
            WebResponse response = request.GetResponse();

            // Display the status.
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);

            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            // Display the content.
            //Console.WriteLine(responseFromServer);
            //WritetoXML(responseFromServer, false, "0");
            //System.Windows.Forms.MessageBox.Show(responseFromServer);

            try
            {
                var ser = new XmlSerializer(typeof(TrackingResponse));
                var wrapper = (TrackingResponse)ser.Deserialize(new StringReader(responseFromServer));

                if (wrapper.AWBInfo.Count() > 0)
                {

                    //dcMas = new DataDataContext();
                    //dcData.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                    //dcData.RemoveOldDHLTracking(wrapper.AWBInfo[0].AWBNumber);

                    if (wrapper.AWBInfo[0].Status.ActionStatus.ToLower().Contains("success"))
                    {
                        dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                        for (int i = 0; i < wrapper.AWBInfo[0].ShipmentInfo.Items.Count(); i++)
                        {
                            try
                            {
                                DHLTracking instance = new DHLTracking();
                                ShipmentEvent CurrentShipmentEvent = wrapper.AWBInfo[0].ShipmentInfo.Items[i] as ShipmentEvent;

                                instance.WaybillNo = wrapper.AWBInfo[0].AWBNumber;
                                instance.Date = new DateTime(CurrentShipmentEvent.Date.Year, CurrentShipmentEvent.Date.Month, CurrentShipmentEvent.Date.Day, CurrentShipmentEvent.Time.Hour, CurrentShipmentEvent.Time.Minute, CurrentShipmentEvent.Time.Second);
                                instance.AreaCode = CurrentShipmentEvent.ServiceArea.ServiceAreaCode;
                                instance.AreaName = CurrentShipmentEvent.ServiceArea.Description;
                                instance.ServiceCode = CurrentShipmentEvent.ServiceEvent.EventCode;
                                instance.ServiceName = CurrentShipmentEvent.ServiceEvent.Description;
                                instance.Signatory = CurrentShipmentEvent.Signatory;
                                instance.StatusID = 1;

                                dcMaster.DHLTrackings.InsertOnSubmit(instance);
                            }
                            catch { }
                        }
                        dcMaster.SubmitChanges();
                    }
                }

                //MessageBox.Show("Waybill No : " + wrapper.AirwayBillNumber);
                //WritetoXML(responseFromServer, false, wrapper.AirwayBillNumber);
            }
            catch
            {

            }
            // Clean up the streams and the response.
            reader.Close();
            response.Close();
        }

        private List<DHLTracking> GetTrackingFromDHL(KnownTrackingRequest trackingdetails)
        {
            List<DHLTracking> result = new List<DHLTracking>();

            WebRequest request = WebRequest.Create("https://xmlpi-ea.dhl.com/XMLShippingServlet");
            request.Method = "POST";
            request.ContentType = "text/xml";
            var stream = request.GetRequestStream();
            StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
            var soapWriter = new XmlSerializer(typeof(KnownTrackingRequest));
            var ns = new XmlSerializerNamespaces();
            ns.Add("req", "http://www.dhl.com");
            soapWriter.Serialize(writer, trackingdetails, ns);
            writer.Close();
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            try
            {
                var ser = new XmlSerializer(typeof(TrackingResponse));
                var wrapper = (TrackingResponse)ser.Deserialize(new StringReader(responseFromServer));

                if (wrapper.AWBInfo.Count() > 0)
                {
                    //App_Data.DataDataContext dcData = new App_Data.DataDataContext();
                    //dcData.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                    //dcData.RemoveOldDHLTracking(wrapper.AWBInfo[0].AWBNumber);

                    if (wrapper.AWBInfo[0].Status.ActionStatus.ToLower().Contains("success"))
                    {
                        dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                        for (int i = 0; i < wrapper.AWBInfo[0].ShipmentInfo.Items.Count(); i++)
                        {
                            try
                            {
                                DHLTracking instance = new DHLTracking();
                                ShipmentEvent CurrentShipmentEvent = wrapper.AWBInfo[0].ShipmentInfo.Items[i] as ShipmentEvent;

                                instance.WaybillNo = wrapper.AWBInfo[0].AWBNumber;
                                instance.Date = new DateTime(CurrentShipmentEvent.Date.Year, CurrentShipmentEvent.Date.Month, CurrentShipmentEvent.Date.Day, CurrentShipmentEvent.Time.Hour, CurrentShipmentEvent.Time.Minute, CurrentShipmentEvent.Time.Second);
                                instance.AreaCode = CurrentShipmentEvent.ServiceArea.ServiceAreaCode;
                                instance.AreaName = CurrentShipmentEvent.ServiceArea.Description;
                                instance.ServiceCode = CurrentShipmentEvent.ServiceEvent.EventCode;
                                instance.ServiceName = CurrentShipmentEvent.ServiceEvent.Description;
                                instance.Signatory = CurrentShipmentEvent.Signatory;
                                instance.StatusID = 1;

                                //dcData.DHLTrackings.InsertOnSubmit(instance);
                                result.Add(instance);
                            }
                            catch { }
                        }
                        //dcData.SubmitChanges();
                    }
                }
            }
            catch
            {

            }
            reader.Close();
            response.Close();
            return result;
        }

        [WebMethod]
        public string GetSystemVariables(byte[] EmployID, string VariableKey)
        {
            string Variablevalue = "";
            if (!GlobalVar.GV.IsSecure(EmployID)) return Variablevalue;
            InfoTrack.BusinessLayer.DContext.UserAccountDataContext dc = new UserAccountDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dc.SystemVariables.Where(P => P.VariableKey.ToLower() == VariableKey.ToLower()).Count() > 0)
                Variablevalue = dc.SystemVariables.First(P => P.VariableKey.ToLower() == VariableKey.ToLower()).Variablevalue;

            return Variablevalue;
        }

        [WebMethod]
        public bool AddNewUnManifestedWaybill(byte[] EmployID, int WaybillNo, string Path, int StationID)
        {
            bool Result = false;
            if (!GlobalVar.GV.IsSecure(EmployID)) return false;

            try
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.WaybillScanned instance = new WaybillScanned();
                instance.Date = DateTime.Now;
                instance.WaybillNo = WaybillNo;
                instance.Path = Path;
                instance.StationID = StationID;
                instance.IsManifested = false;
                instance.StatusID = 1;

                dcDoc.WaybillScanneds.InsertOnSubmit(instance);
                dcDoc.SubmitChanges();

                Result = true;
            }
            catch { }

            return Result;
        }

        [WebMethod]
        public bool AddDeliverySheetScan(byte[] EmployID, int DeliverySheetID, string Path, int StationID, int PageNo)
        {
            bool Result = false;
            if (!GlobalVar.GV.IsSecure(EmployID)) return false;

            try
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                InfoTrack.BusinessLayer.DContext.DeliverySheetScanned instance = new DeliverySheetScanned();
                instance.Date = DateTime.Now;
                instance.DeliverySheetID = DeliverySheetID;
                instance.PageNo = PageNo;
                instance.Path = Path;
                instance.StationID = StationID;
                instance.StatusID = 1;

                dcDoc.DeliverySheetScanneds.InsertOnSubmit(instance);
                dcDoc.SubmitChanges();

                Result = true;
            }
            catch { }

            return Result;
        }

        [WebMethod]
        public bool AddApplicationExecuation(byte[] EmployID, int ApplicationID, DateTime startExecuation, DateTime endExecuation)
        {
            bool result = false;
            if (!GlobalVar.GV.IsSecure(EmployID)) return false;
            int empID = Convert.ToInt32(GlobalVar.GV.security.Decrypt(EmployID));

            ApplicationExecution instance = new ApplicationExecution();
            instance.EmployID = empID;
            instance.ApplicationID = ApplicationID;
            instance.Date = DateTime.Now;
            instance.StartExecuation = startExecuation;
            instance.EndExecuation = endExecuation;

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster.ApplicationExecutions.InsertOnSubmit(instance);
            dcMaster.SubmitChanges();

            return result;
        }

        //[WebMethod]
        //public bool RequestNotifications(InfoTrack.NaqelAPI.GeneralClass.RequestNotificationDetails requestNotificationDetail)
        //{
        //    bool result = false;

        //    App_Data.DataDataContext dcData = new App_Data.DataDataContext();
        //    App_Data.RequestNotification instance = new App_Data.RequestNotification();

        //    instance.Date = DateTime.Now;
        //    instance.WaybillNo = requestNotificationDetail.WaybillNo;
        //    instance.EMail = requestNotificationDetail.EMail;
        //    instance.MobileNo = requestNotificationDetail.MobileNo;
        //    instance.RequesterName = requestNotificationDetail.RequesterName;
        //    instance.StatusID = 1;
        //    int notificationType = 1;
        //    if (requestNotificationDetail.NotificationType == "EMail")
        //        notificationType = 2;
        //    instance.NotificationType = notificationType;

        //    dcData.RequestNotifications.InsertOnSubmit(instance);
        //    dcData.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
        //    dcData.SubmitChanges();
        //    result = true;
        //    return result;
        //}

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public bool RequestNotifications(string WaybillNo, string RequesterName, string NotificationType, string MobileNo, string EMail, string LanguageID)
        {
            bool result = false;

            dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.RequestNotification instance = new BusinessLayer.DContext.RequestNotification();

            instance.Date = DateTime.Now;
            instance.WaybillNo = WaybillNo;
            instance.EMail = EMail;
            instance.MobileNo = MobileNo;
            instance.RequesterName = RequesterName;
            instance.StatusID = 1;
            int notificationType = 1;
            if (NotificationType != "SMS")
                notificationType = 2;
            instance.LanguageID = 1;
            if (LanguageID != "English")
                instance.LanguageID = 2;
            instance.NotificationType = notificationType;
            dcDoc.RequestNotifications.InsertOnSubmit(instance);
            dcDoc.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            if (notificationType == 1 && instance.MobileNo.ToString().Length > 5)
            {
                if (dcDoc.RequestNotifications.Where(P => P.WaybillNo == instance.WaybillNo &&
                                                         P.StatusID == 1 &&
                                                         P.MobileNo == instance.MobileNo).Count() <= 0)
                    dcDoc.SubmitChanges();
            }
            else
                dcDoc.SubmitChanges();

            result = true;
            return result;
        }
    }

    public class NewDataSet
    {
        public ViewSmartPhoneTrack _ViewSmartPhoneTrack = new ViewSmartPhoneTrack();
        public List<ViwOnlineTracking> _ViwOnlineTracking = new List<ViwOnlineTracking>();

        public class ViewSmartPhoneTrack
        {
            public int ID = 0;
            public int WayBillNo = 0;
            public double PicesCount = 0;
            public double Weight = 0;
            public string Name = "";
            public bool IsDelivered = false;

        }

        public class ViwOnlineTracking
        {
            public int WaybillNo = 0;
            public int StationID = 0;
            public string Activity = "";
            public int ID = 0;
            public DateTime TDate = new DateTime();
        }
    }

    public class TrackingResult
    {
        public ShipmentDetails shipmentDetails = new ShipmentDetails();
        public List<OnlineTracking> viwOnlineTracking = new List<OnlineTracking>();

        public class ShipmentDetails
        {
            public int ID = 0;
            public int WayBillNo = 0;
            public DateTime PickUpDate = new DateTime();
            public double Weight = 0;
            public double PicesCount = 0;
            public string Destination = "";
            public string DestinationAr = "";
            public string Name = "";
            public bool IsDelivered = false;
            public int StageID = -1;
        }

        public class OnlineTracking
        {
            public int WaybillNo = 0;
            public int StationID = 0;
            public string Activity = "";
            public string ActivityAr = "";
            public int ID = 0;
            public DateTime TDate = new DateTime();
        }
    }

    public class RateNewDetail
    {
        public int RateTypeID;
        public int RateNo;
    }
}