using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWCFShipping" in both code and config file together.
    [ServiceContract]
    public interface IWCFShipping
    {
        /*[OperationContract]
        void DoWork();

        [OperationContract]
        Result CreateWaybill(ManifestShipmentDetails _ManifestShipmentDetails);*/

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "TraceByWaybillNo/{WaybillNo}")]
        List<Tracking> TraceByWaybillNo(string WaybillNo);
    }
}