using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using InfoTrack.NaqelAPI.App_Data;
using InfoTrack.BusinessLayer.DContext;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFShippingService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WCFShippingService.svc or WCFShippingService.svc.cs at the Solution Explorer and start debugging.
    public class WCFShippingService : IWCFShippingService
    {
        InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDoc;

        public List<Tracking> TraceByWaybillNo(string ClientID, string Password, string WaybillNo)
        {
            ClientInformation ClientInfo = new ClientInformation(Convert.ToInt32(ClientID), Password);
            List<Tracking> TrackingList = new List<Tracking>();
            ClientInfo.Version = "1.0";
            
            if (WaybillNo.Length>8)
                WaybillNo = WaybillNo.Remove(8, WaybillNo.Length - 8);

            if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                List<ViwTracking> t = new List<ViwTracking>();
                if (ClientInfo.ClientID == 1024600)
                    t = dcMaster.ViwTrackings.Where(P => P.WaybillNo == Convert.ToInt32(WaybillNo)).OrderBy(P => P.Date).ToList();
                else
                    t = dcMaster.ViwTrackings.Where(P => P.WaybillNo == Convert.ToInt32(WaybillNo) && P.ClientID == Convert.ToInt32(ClientID)).OrderBy(P => P.Date).ToList();
                for (int i = 0; i < t.Count; i++)
                {
                    Tracking newActivity = new Tracking();
                    if (t[i].ClientID.HasValue)
                        newActivity.ClientID = t[i].ClientID.Value;
                    else
                        newActivity.ClientID = 1024600;
                    newActivity.Date = t[i].Date;

                    newActivity.Activity = t[i].Activity;
                    newActivity.StationCode = t[i].StationCode;
                    newActivity.WaybillNo = t[i].WaybillNo;
                    newActivity.HasError = t[i].HasError;
                    newActivity.ErrorMessage = t[i].ErrorMessage;
                    newActivity.Comments = t[i].Comments;
                    newActivity.RefNo = t[i].RefNo;

                    TrackingList.Add(newActivity);
                }

                GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Trace_Shipment_By_WaybillNo, WaybillNo, null);
            }

            return TrackingList;
        }

        public List<Tracking> TraceByRefNo(string ClientID, string Password, string RefNo)
        {
            ClientInformation ClientInfo = new ClientInformation(Convert.ToInt32(ClientID), Password);
            List<Tracking> TrackingList = new List<Tracking>();
            ClientInfo.Version = "1.0";

            if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
                List<ViwTracking> t = new List<ViwTracking>();

                //if (ClientInfo.ClientID == 1024600)
                //    t = dc.ViwTrackings.Where(P => P.RefNo.ToLower() == RefNo.ToLower()).OrderBy(P => P.Date).ToList();
                //else
                    t = dcMaster.ViwTrackings.Where(P => P.RefNo.ToLower() == RefNo.ToLower() && P.ClientID == Convert.ToInt32(ClientID)).OrderBy(P => P.Date).ToList();

                for (int i = 0; i < t.Count; i++)
                {
                    Tracking newActivity = new Tracking();
                    if (t[i].ClientID.HasValue)
                        newActivity.ClientID = t[i].ClientID.Value;
                    else
                        newActivity.ClientID = 1024600;
                    newActivity.Date = t[i].Date;
                    newActivity.Activity = t[i].Activity;
                    newActivity.StationCode = t[i].StationCode;
                    newActivity.WaybillNo = t[i].WaybillNo;
                    newActivity.HasError = t[i].HasError;
                    newActivity.ErrorMessage = t[i].ErrorMessage;
                    newActivity.Comments = t[i].Comments;
                    newActivity.RefNo = t[i].RefNo;

                    TrackingList.Add(newActivity);
                }
                GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Trace_Shipment_By_RefNo, RefNo, null);
            }

            return TrackingList;
        }

        public List<WaybillDetail> GetShipmentDetailsByWaybillNo(string ClientID, string Password, string WaybillNo)
        {
            ClientInformation ClientInfo = new ClientInformation(Convert.ToInt32(ClientID), Password);
            List<WaybillDetail> WaybillDetail = new List<WaybillDetail>();
            ClientInfo.Version = "1.0";
            
            if (WaybillNo.Length > 8)
                WaybillNo = WaybillNo.Remove(8, WaybillNo.Length - 8);

            if (ClientInfo.ClientID == 1024600 || !ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                List<ViwWaybillDetail> t = new List<ViwWaybillDetail>();
                if (ClientInfo.ClientID == 1024600)
                    t = dcDoc.ViwWaybillDetails.Where(P => P.WayBillNo == Convert.ToInt32(WaybillNo)).ToList();
                else
                    t = dcDoc.ViwWaybillDetails.Where(P => P.WayBillNo == Convert.ToInt32(WaybillNo) && P.ClientID == Convert.ToInt32(ClientID)).ToList();

                for (int i = 0; i < 1; i++)
                {
                    WaybillDetail ShipmentDetails = new WaybillDetail();
                    ShipmentDetails.WaybillNo = t[i].WayBillNo.ToString();
                    ShipmentDetails.RefNo = t[i].RefNo;
                    ShipmentDetails.SenderCompanyName = t[i].SenderCompanyName;
                    ShipmentDetails.SenderPhoneNumber1 = t[i].SenderPhoneNumber1;
                    ShipmentDetails.SenderAddress = t[i].SenderAddress;
                    ShipmentDetails.SenderName = t[i].SenderName;
                    ShipmentDetails.SenderPhoneNumber2 = t[i].SenderPhoneNumber2;
                    ShipmentDetails.SenderMobile = t[i].SenderMobile;
                    ShipmentDetails.ReceiverName = t[i].ReceiverName;
                    ShipmentDetails.ReceiverPhoneNumber = t[i].ReceiverPhoneNumber;
                    ShipmentDetails.ReceiverAddress = t[i].ReceiverAddress;
                    ShipmentDetails.ReceiverMobile = t[i].ReceiverMobile;
                    ShipmentDetails.ShipmentOrigin = t[i].ShipmentOrigin;
                    ShipmentDetails.ShipmentDestination = t[i].ShipmentDestination;
                   // ShipmentDetails.Weight = t[i].
                    WaybillDetail.Add(ShipmentDetails);
                }
                GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Get_Shipment_Details_ByWaybillNo, WaybillNo, null);
            }

            return WaybillDetail;
        }

        public List<WaybillDetail> GetShipmentDetailsByRefNo(string ClientID, string Password, string RefNo)
        {
            ClientInformation ClientInfo = new ClientInformation(Convert.ToInt32(ClientID), Password);
            List<WaybillDetail> WaybillDetail = new List<WaybillDetail>();
            ClientInfo.Version = "1.0";

            if (!ClientInfo.CheckClientInfo(ClientInfo, false).HasError)
            {
                dcDoc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                List<ViwWaybillDetail> t = new List<ViwWaybillDetail>();
                //if (ClientInfo.ClientID == 1024600)
                //    t = dc.ViwWaybillDetails.Where(P => P.RefNo.ToLower() == RefNo.ToLower()).ToList();
                //else
                    t = dcDoc.ViwWaybillDetails.Where(P => P.RefNo.ToLower() == RefNo.ToLower() && P.ClientID == Convert.ToInt32(ClientID)).ToList();

                for (int i = 0; i < 1; i++)
                {
                    WaybillDetail ShipmentDetails = new WaybillDetail();
                    ShipmentDetails.WaybillNo = t[i].WayBillNo.ToString();
                    ShipmentDetails.RefNo = t[i].RefNo;
                    ShipmentDetails.SenderCompanyName = t[i].SenderCompanyName;
                    ShipmentDetails.SenderPhoneNumber1 = t[i].SenderPhoneNumber1;
                    ShipmentDetails.SenderAddress = t[i].SenderAddress;
                    ShipmentDetails.SenderName = t[i].SenderName;
                    ShipmentDetails.SenderPhoneNumber2 = t[i].SenderPhoneNumber2;
                    ShipmentDetails.SenderMobile = t[i].SenderMobile;
                    ShipmentDetails.ReceiverName = t[i].ReceiverName;
                    ShipmentDetails.ReceiverPhoneNumber = t[i].ReceiverPhoneNumber;
                    ShipmentDetails.ReceiverAddress = t[i].ReceiverAddress;
                    ShipmentDetails.ReceiverMobile = t[i].ReceiverMobile;
                    ShipmentDetails.ShipmentOrigin = t[i].ShipmentOrigin;
                    ShipmentDetails.ShipmentDestination = t[i].ShipmentDestination;

                    WaybillDetail.Add(ShipmentDetails);
                }
                GlobalVar.GV.CreateShippingAPIRequest(ClientInfo, EnumList.APIRequestType.Get_Shipment_Details_ByRefNo, RefNo, null);
            }

            return WaybillDetail;
        }

        //public void RequestNotifications(InfoTrack.NaqelAPI.GeneralClass.RequestNotificationDetails requestNotificationDetail)
        //{
        //    GlobalVar.GV.InsertError("test", "15380");
        //    App_Data.DataDataContext dcData = new DataDataContext();
        //    App_Data.RequestNotification instance = new RequestNotification();

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
        //}
    }
}