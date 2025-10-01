using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWCFShippingService" in both code and config file together.
    [ServiceContract]
    public interface IWCFShippingService
    {
        //TraceByWaybillNo/9016888/490945/78431490
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "TraceByWaybillNo/{ClientID}/{Password}/{WaybillNo}")]
        List<Tracking> TraceByWaybillNo(string ClientID, string Password, string WaybillNo);

        //TraceByRefNo/9016888/490945/4070007064
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "TraceByRefNo/{ClientID}/{Password}/{RefNo}")]
        List<Tracking> TraceByRefNo(string ClientID, string Password, string RefNo);

        //GetShipmentDetailsByWaybillNo/9016888/490945/78431490
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetShipmentDetailsByWaybillNo/{ClientID}/{Password}/{WaybillNo}")]
        List<WaybillDetail> GetShipmentDetailsByWaybillNo(string ClientID, string Password, string WaybillNo);

        //GetShipmentDetailsByRefNo/9016888/490945/4070007064
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetShipmentDetailsByRefNo/{ClientID}/{Password}/{RefNo}")]
        List<WaybillDetail> GetShipmentDetailsByRefNo(string ClientID, string Password, string RefNo);

        ////GetShipmentDetailsByRefNo/9016888/490945/4070007064
        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "RequestNotifications/{requestNotificationDetail}")]
        //void RequestNotifications(InfoTrack.NaqelAPI.GeneralClass.RequestNotificationDetails requestNotificationDetail);

        //[OperationContract]
        //Result CreateWaybill(ManifestShipmentDetails _ManifestShipmentDetails);
    }
}